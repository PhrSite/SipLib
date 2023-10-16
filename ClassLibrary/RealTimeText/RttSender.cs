/////////////////////////////////////////////////////////////////////////////////////
//  File:   RttSender.cs                                            12 Oct 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

using System.Collections.Concurrent;
using System.Text;
using SipLib.Rtp;

namespace SipLib.RealTimeText;

/// <summary>
/// Delegate for a function that sends an RTP packet containing RTT data over an established RTT channel.
/// </summary>
/// <param name="rtpPckt">Complete RTP packet to send</param>
public delegate void RttRtpSendDelegate(RtpPacket rtpPckt);

/// <summary>
/// This class manages the sending side of a RTT media session. See RFC 4103. This class manages the 
/// transmission of redundant or non-redundant RTP packets containing RTT payloads based on the timing
/// requirements specified in RFC 4103.
/// </summary>
public class RttSender
{
    private RttParameters m_Params = null;
    private RttRtpSendDelegate Sender = null;

    private const int MAX_REDUNDANCY_LEVELS = 5;

    private ConcurrentQueue<string> m_TxMessages = new ConcurrentQueue<string>();

    private CancellationTokenSource m_CancellationTokenSource = new CancellationTokenSource();

    private Semaphore m_SendSemaphore = new Semaphore(0, int.MaxValue);
    private Task m_SenderTask = null;
    private List<RttRedundantBlock> m_RedundantBlocks = new List<RttRedundantBlock>(MAX_REDUNDANCY_LEVELS);

    private ushort m_SequenceNumber = 0;
    private uint m_TimeStamp = 0;
    private DateTime m_LastMessageSent = DateTime.Now;
    private uint m_SSRC = 0;
    private uint m_MessageStartTime = (uint)System.Environment.TickCount;
    private const uint SEND_IDLE_TIME = 300;

    private static Random m_Rnd = new Random();

    /// <summary>
    /// Constructs a new RttTxHndlr object.
    /// </summary>
    /// <param name="Rp">Contains the RTT media session parameters.</param>
    /// <param name="sender">Delegate to use to send RTP packets.</param>
    public RttSender(RttParameters Rp, RttRtpSendDelegate sender)
    {
        m_Params = Rp;
        Sender = sender;

        // Initialize the list or redunant block information even though redundancy may not be required.
        for (int i = 0; i < MAX_REDUNDANCY_LEVELS; i++)
            m_RedundantBlocks.Add(new RttRedundantBlock());

        m_SSRC = Convert.ToUInt32(m_Rnd.Next());
    }

    /// <summary>
    /// Gets or sets the RttParameters. If setting, do so before calling Start().
    /// </summary>
    public RttParameters Params
    {
        get { return m_Params; }
        set { m_Params = value; }
    }

    /// <summary>
    /// Starts the sender task
    /// </summary>
    public void Start()
    {
        if (m_SenderTask != null)
            m_SenderTask = Task.Factory.StartNew(() => { SenderTask(m_CancellationTokenSource.Token); });
    }

    /// <summary>
    /// Stops the sender task
    /// </summary>
    public void Stop()
    {
        m_CancellationTokenSource.Cancel();
        m_SenderTask = null;
    }

    /// <summary>
    /// Enqueues characters to send.
    /// </summary>
    /// <param name="message">Contains at least one character to send</param>
    public void EnqueueMessage(string message)
    {
        m_TxMessages.Enqueue(message);

        if (m_Params.Cps == 0)
            m_SendSemaphore.Release();
    }

    private string DequeueMessage()
    {
        string message = null;
        m_TxMessages.TryDequeue(out message);
        return message;
    }

    private Task SenderTask(CancellationToken token)
    {
        string strNewText = null;
        int MsPerChar = 0;

        try
        {
            while (token.IsCancellationRequested == false)
            {
                m_SendSemaphore.WaitOne((int)SEND_IDLE_TIME);

                strNewText = DequeueMessage();
                if (strNewText != null)
                {   // There is new text to send.
                    strNewText = RttUtils.FixRttLineEnding(strNewText);
                    m_MessageStartTime = (uint)System.Environment.TickCount;

                    if (m_Params.Cps > 0)
                        MsPerChar = 1000 / m_Params.Cps;
                    else
                        MsPerChar = 0;		// No delay between characters

                    if (MsPerChar > 0)
                    {   // Send 1 character at a time with a delay between each character.
                        for (int i = 0; i < strNewText.Length; i++)
                        {
                            Send(strNewText[i].ToString());
                            m_SendSemaphore.WaitOne(MsPerChar);
                            m_LastMessageSent = DateTime.Now;
                        }
                    }
                    else
                    {   // Send the whole string at once.
                        Send(strNewText);
                        m_LastMessageSent = DateTime.Now;
                        m_SendSemaphore.Release();
                    }
                }
                else
                   // No new text to send. See if its necessary to send an empty text block
                    SendEmptyTextBlock();
            }
        }
        catch (TaskCanceledException) { }

        return Task.CompletedTask;
    }

    /// <summary>
    /// This is a helper method that gets called periodically. It determines if its necessary to send an
    /// empty RTP packet with no new text in it. This is necessary only when there is no new text available
    /// and redundancy is being used and there is still redundant data to send.
    /// </summary>
    private void SendEmptyTextBlock()
    {
        int RedundancyLevel = m_Params.RedundancyLevel;
        if (RedundancyLevel == 0)
            return;     // Not using redundancy so don't send an empty block.

        if ((DateTime.Now - m_LastMessageSent).TotalMilliseconds < SEND_IDLE_TIME)
            return;

        // Its time but see if there is anything in the redundant block history that needs to be sent.
        int RedByteCount = 0;
        int i;
        for (i = 0; i < RedundancyLevel; i++)
            RedByteCount += m_RedundantBlocks[i].BlockLength;

        if (RedByteCount > 0)
            Send("");
    }

    /// <summary>
    /// Sends the specified string by building a complete RTP packet.
    /// </summary>
    /// <param name="strText">String to send. May be an empty string but must not be null.</param>
    private void Send(string strText)
    {
        m_TimeStamp = (uint)System.Environment.TickCount;

        if (m_Params.RedundancyLevel == 0)
        {
            SendWithNoRedundancy(strText);
            return;
        }

        int i;

        // The extra 1 is for the empty end of header marker byte.
        int RedBlockHeaderBytes = m_Params.RedundancyLevel * RttRedundantBlock.RED_HEADER_LENGTH + 1;
        int TotalRedBytes = 0;
        for (i = 0; i < m_Params.RedundancyLevel; i++)
            TotalRedBytes += m_RedundantBlocks[i].BlockLength;

        int TotalCharBytes = Encoding.UTF8.GetByteCount(strText);
        int TotalRtpLength = RtpPacket.MIN_PACKET_LENGTH + RedBlockHeaderBytes + TotalRedBytes + 
            TotalCharBytes;

        byte[] TextBytes = null;
        if (TotalCharBytes > 0)
            TextBytes = Encoding.UTF8.GetBytes(strText);

        byte[] RtpBytes = new byte[TotalRtpLength];
        RtpPacket Rp = new RtpPacket(RtpBytes);
        Rp.SSRC = m_SSRC;

        if (TotalCharBytes > 0)
            Rp.Marker = true;

        Rp.SequenceNumber = m_SequenceNumber;
        Rp.Timestamp = m_TimeStamp;
        int CurrentIndex = Rp.HeaderLength;
        Rp.PayloadType = m_Params.RedundancyPayloadType;

        // Set the payload type for each redundant block header and copy
        // in the redundant block headers.
        for (i = 0; i < m_Params.RedundancyLevel; i++)
        {
            m_RedundantBlocks[i].T140PayloadType = Convert.ToByte(m_Params.T140PayloadType & 0x00ff);
            Array.ConstrainedCopy(m_RedundantBlocks[i].GetRedundantPayloadHeader(), 0, RtpBytes, CurrentIndex, 
                RttRedundantBlock.RED_HEADER_LENGTH);
            CurrentIndex += RttRedundantBlock.RED_HEADER_LENGTH;
        } // end for i

        // Set the last redundant block header byte.
        RtpBytes[CurrentIndex++] = Convert.ToByte(m_Params.T140PayloadType & 0x00ff);

        // Copy in the redundant bytes if there are any.
        for (i = 0; i < m_Params.RedundancyLevel; i++)
        {
            if (m_RedundantBlocks[i].BlockLength > 0 && m_RedundantBlocks[i].PayloadBytes != null)
            {
                Array.ConstrainedCopy(m_RedundantBlocks[i].PayloadBytes, 0, RtpBytes, CurrentIndex, m_RedundantBlocks[i].
                    BlockLength);
                CurrentIndex += m_RedundantBlocks[i].BlockLength;
            }
        } // end for i

        // Copy in the new text bytes if there are any.
        if (TotalCharBytes > 0)
            Array.ConstrainedCopy(TextBytes, 0, RtpBytes, CurrentIndex, TotalCharBytes);

        Sender?.Invoke(Rp);

        // Shift the redundant blocks and add the new text at the end of the list.
        for (i = 1; i < m_Params.RedundancyLevel; i++)
        {
            m_RedundantBlocks[i - 1].BlockLength = m_RedundantBlocks[i].BlockLength;
            m_RedundantBlocks[i - 1].T140PayloadType = m_RedundantBlocks[i].T140PayloadType;
            m_RedundantBlocks[i - 1].TimeOffset = m_RedundantBlocks[i].TimeOffset;
            m_RedundantBlocks[i - 1].PayloadBytes = m_RedundantBlocks[i].PayloadBytes;
        } // end for i

        int Idx = m_Params.RedundancyLevel - 1;
        m_RedundantBlocks[Idx].TimeOffset = Convert.ToUInt16(ElapsedTime(m_MessageStartTime, m_TimeStamp) & 0xffff);

        if (TotalCharBytes > 0)
        {
            m_RedundantBlocks[Idx].BlockLength = Convert.ToUInt16(TotalCharBytes & 0xffff);
            m_RedundantBlocks[Idx].PayloadBytes = TextBytes;
            // Don't care about the payload type because that will be set when // this block is sent.
        }
        else
        {
            m_RedundantBlocks[Idx].BlockLength = 0;
            m_RedundantBlocks[Idx].PayloadBytes = null;
        }

        m_SequenceNumber += 1;
    }

    /// <summary>
    /// Calculates the difference between two millisecond time ticks accounting for wrap around. The wrap
    /// around interval is about 42 days.
    /// </summary>
    /// <param name="Time1">The earlier time.</param>
    /// <param name="Time2">The later time.</param>
    /// <returns>Returns the difference in millisecond time ticks</returns>
    private uint ElapsedTime(uint Time1, uint Time2)
    {
        if (Time2 >= Time1)
            return Time2 - Time1;
        else
            return uint.MaxValue - Time1 + Time2;
    }

    /// <summary>
    /// Sends new text when not using redundancy.
    /// </summary>
    /// <param name="strNewText">New text message to send.</param>
    private void SendWithNoRedundancy(string strNewText)
    {
        byte[] TextBytes = Encoding.UTF8.GetBytes(strNewText);
        byte[] RtpBytes = new byte[TextBytes.Length + RtpPacket.MIN_PACKET_LENGTH];
        RtpPacket Rp = new RtpPacket(RtpBytes);
        Rp.SSRC = m_SSRC;

        Rp.Marker = true;
        Rp.SequenceNumber = m_SequenceNumber;
        Rp.Timestamp = m_TimeStamp;
        Array.ConstrainedCopy(TextBytes, 0, RtpBytes, RtpPacket.MIN_PACKET_LENGTH, TextBytes.Length);

        Rp.PayloadType = m_Params.T140PayloadType;
        Sender?.Invoke(Rp);
        m_SequenceNumber += 1;
    }
}
