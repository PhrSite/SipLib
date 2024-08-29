/////////////////////////////////////////////////////////////////////////////////////
//  File:   RttReceiver.cs                                          12 Oct 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

using SipLib.Rtp;
using System.Text;

namespace SipLib.RealTimeText;

/// <summary>
/// Delegate type for the RttCharactersReceived event of the RttReceiver class.
/// </summary>
/// <param name="RxChars">Received text.</param>
/// <param name="Source">Identifies the source of the received characters</param>
public delegate void RttCharactersReceivedDelegate(string RxChars, string Source);

/// <summary>
/// This class handles Real Time Protocol (RTP) packets containing Real Time Text (RTT, RFC 4103)
/// and notifies the user of this class when complete messages are available. A complete message may 
/// be one character or several characters.
/// <para>
/// This class handles RTT redundancy as specified in Section 4.2 of RFC 4103 and is capable of recovering
/// the original message even if there are dropped RTP packets.
/// </para>
/// <para>This class supports receiving characters from a mixer-aware remote endpoint as described in
/// RFC 9071 RTP-Mixer Formatting of Multiparty Real-Time Text.
/// </para>
/// </summary>
public class RttReceiver
{
    private RttParameters m_Params;
    private bool m_FirstPacketReceived = false;
    private ushort m_RtpSeq = 0;

    private RtpChannel? m_RtpChannel = null;
    private Dictionary<uint, string>? m_Contributors = null;

    private string m_Source = "Caller";

    /// <summary>
    /// Event that is fired when at least one character is received
    /// </summary>
    /// <value></value>
    public event RttCharactersReceivedDelegate? RttCharactersReceived = null;

    /// <summary>
    /// Constructor. This constructor is for testing only. Use the constructor that takes a RtpChannel
    /// parameter for actual application.
    /// </summary>
    /// <param name="rttParams">RTT session parameters from the SDP media description block.</param>
    public RttReceiver(RttParameters rttParams)
    {
        m_Params = rttParams;
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="rttParams">RTT session parameters from the SDP media description block.</param>
    /// <param name="rtpChannel">RtpChannel for RTT that this object will receive RTP packets from</param>
    /// <param name="source">Identifies the remote source that will be sending characters</param>
    public RttReceiver(RttParameters rttParams, RtpChannel rtpChannel, string source)
    {
        m_Params = rttParams;
        m_RtpChannel = rtpChannel;
        m_RtpChannel.RtpPacketReceived += ProcessRtpPacket;
        m_RtpChannel.RtcpPacketReceived += ProcessRtcpPacket;
        m_Source = source;
    }

    /// <summary>
    /// Retrieves the names of each conference member so that the characters received can be associated
    /// with the name of the sender.
    /// </summary>
    /// <param name="rtcpCompoundPacket"></param>
    private void ProcessRtcpPacket(RtcpCompoundPacket rtcpCompoundPacket)
    {
        Dictionary<uint, string> csrcs = new Dictionary<uint, string>();
        foreach (SdesPacket sdesPacket in rtcpCompoundPacket.SdesPackets)
        {
            foreach (SdesChunk chunk in sdesPacket.Chunks)
            {
                foreach (SdesItem item in chunk.Items)
                {
                    if (item.ItemType == SdesItemType.NAME)
                    {
                        if (csrcs.ContainsKey(chunk.SSRC) == false)
                            csrcs.Add(chunk.SSRC, item.Payload);
                    }
                }
            }

            m_Contributors = csrcs;
        }
    }

    private string GetSource(uint ssrc)
    {
        if (m_Contributors == null || m_Contributors.Count == 0)
            return m_Source;
        else
        {
            if (m_Contributors.ContainsKey(ssrc) == true)
                return m_Contributors[ssrc];
            else
                return m_Source;
        }
    }

    /// <summary>
    /// Processes an RTP packet containing RTT media. This method fires the RttCharactersReceived event
    /// when characters a detected.
    /// </summary>
    /// <param name="rtpPacket">Input RTP packet to process.</param>
    public void ProcessRtpPacket(RtpPacket rtpPacket)
    {
        int Pt = rtpPacket.PayloadType;
        byte[] TextBytes = rtpPacket.GetPayloadBytes();

        ushort CurrentSeq = rtpPacket.SequenceNumber;
        ushort SeqDiff = 0;
        int MissedPackets = 0;

        if (rtpPacket.CsrcCount == 0)
            Ssrc = rtpPacket.SSRC;
        else
            Ssrc = rtpPacket.GetCSRC(0);

        string source = GetSource(Ssrc);

        if (m_FirstPacketReceived == false)
        {
            if (rtpPacket.Marker == true)
                // The marker bit will be set for the first packet sent by the remote end point.
                MissedPackets = 0;
            else
                // The first packet has not been received and the marker bit is not set so this means that
                // the first packet was missed. Assume 1 missed packet, but don't know for sure.
                MissedPackets = 1;

            m_FirstPacketReceived = true;
        }
        else
        {   // Check for missed packets.
            SeqDiff = CalculateSequenceDifference(m_RtpSeq, CurrentSeq);
            if (SeqDiff > 1)
                MissedPackets = SeqDiff - 1;
        }

        m_RtpSeq = CurrentSeq;

        if (TextBytes == null || TextBytes.Length == 0)
            // This is an empty packet. With no new primary text and no redundant data.
            // Empty packets may be sent periodically.
            return;

        string strNewText = null;

        if (Pt == m_Params.T140PayloadType)
        {   // This RTP packet only contains new text.
            strNewText = Encoding.UTF8.GetString(TextBytes);
            if (strNewText != RttUtils.ByteOrderMarker.ToString())
                // Notify the user that at least one character has been received
                RttCharactersReceived?.Invoke(FixLineEnding(strNewText), source);
            // Else ignore the Byte Order Marker (BOM)
            return;
        }

        if (Pt != m_Params.RedundancyPayloadType)
            return;     // Error: unknown payload type

        // This RTP packet may contain a mixture of redundant and new text.
        int CurrentIndex = 0;
        List<RttRedundantBlock> RedundantBlocks = GetRedundantBlocks(TextBytes, ref CurrentIndex);
        // Calculate the total number of bytes in all of the redundant blocks
        int RedundantBytes = 0;
        foreach (RttRedundantBlock Rrb1 in RedundantBlocks)
            RedundantBytes += Rrb1.BlockLength;

        int NewTextIndex = CurrentIndex + RedundantBytes;
        int NewTextLength = TextBytes.Length - NewTextIndex; ;

        if (NewTextLength > 0 && NewTextIndex < TextBytes.Length)
        {
            byte[] NewText = new byte[NewTextLength];
            Array.ConstrainedCopy(TextBytes, NewTextIndex, NewText, 0, NewTextLength);
            strNewText = Encoding.UTF8.GetString(NewText);
        }

        if (strNewText == null && MissedPackets == 0)
        {   // If strNewText is null, then there was no new primary text sent with this packet. This
            // packet only contains redundant data but the redundant data is not needed because no packets
            // have been missed.
            return;
        }

        byte[] RedBytes = null;
        if (RedundantBytes > 0)
        {   // There are one or more redundant blocks so get the bytes for each block.
            foreach (RttRedundantBlock Rbr2 in RedundantBlocks)
            {
                if (Rbr2.BlockLength > 0)
                {
                    RedBytes = new byte[Rbr2.BlockLength];
                    Array.ConstrainedCopy(TextBytes, CurrentIndex, RedBytes, 0, Rbr2.BlockLength);
                    Rbr2.PayloadBytes = RedBytes;
                    CurrentIndex = CurrentIndex + Rbr2.BlockLength;
                }
                else
                    Rbr2.PayloadBytes = null;
            }
        }

        if (MissedPackets > 0)
        {   // There were missed packets. Try to reconstruct the message from the new primary text and
            // the redundant blocks in this packet.
            int RedundantBlocksToUse = 0;
            if (MissedPackets > RedundantBlocks.Count)
                RedundantBlocksToUse = RedundantBlocks.Count;
            else
                RedundantBlocksToUse = MissedPackets;

            string str;
            int StartIndex = RedundantBlocks.Count - RedundantBlocksToUse;

            int i;
            for (i = StartIndex; i < RedundantBlocks.Count; i++)
            {
                if (RedundantBlocks[i].BlockLength > 0 && RedundantBlocks[i].PayloadBytes != null)
                {
                    str = Encoding.UTF8.GetString(RedundantBlocks[i].PayloadBytes!);
                    // The oldest redundant data is sent first.
                    strNewText = str + strNewText;
                }
            }
        }

        if (strNewText != null && strNewText.Length > 0)
        {
            if (strNewText == RttUtils.ByteOrderMarker.ToString())
                return;     // Ignore the BOM character

            // Notify the user that at least one character has been received
            RttCharactersReceived?.Invoke(FixLineEnding(strNewText), source);
        }
    }

    /// <summary>
    /// The SSRC or CSRC of the source of the most recent characters. Taken from the RTP packet.
    /// </summary>
    /// <value></value>
    public uint Ssrc { private set; get; } = 0;

    private List<RttRedundantBlock> GetRedundantBlocks(byte[] TextBytes, ref int CurrentIndex)
    {
        List<RttRedundantBlock> Blks = new List<RttRedundantBlock>();
        bool Done = false;
        RttRedundantBlock Rrb = null;

        while (Done == false)
        {
            if ((TextBytes[CurrentIndex] & RttRedundantBlock.RTT_MARKER_MASK) == RttRedundantBlock.RTT_MARKER_MASK)
            {   // This is the start of a new header block.
                Rrb = new RttRedundantBlock(TextBytes, CurrentIndex);
                CurrentIndex += RttRedundantBlock.RED_HEADER_LENGTH;
                Blks.Add(Rrb);
            }
            else
            {   // This byte signals the last block.
                Done = true;
                CurrentIndex += 1;
            }

            if (Done == false && CurrentIndex >= TextBytes.Length)
                Done = true;
        }

        return Blks;
    }

    private ushort CalculateSequenceDifference(ushort Seq1, ushort Seq2)
    {
        ushort Diff = 0;
        if (Seq2 >= Seq1)
            Diff = Convert.ToUInt16((Seq2 - Seq1) & 0xffff);
        else
            Diff = Convert.ToUInt16((ushort.MaxValue - Seq1 + Seq2) & 0xffff);

        return Diff;
    }

    private string FixLineEnding(string RxChars)
    {
        return RxChars.Replace(RttUtils.strUtf8LineSeparator, "\n");
    }
}
