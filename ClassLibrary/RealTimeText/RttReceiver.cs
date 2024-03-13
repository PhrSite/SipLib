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
/// <param name="Ssrc">SSRC or CSRC of the source of the text from the RTP packet</param>
public delegate void RttCharactersReceivedDelegate(string RxChars, uint Ssrc);

/// <summary>
/// This class handles Real Time Protocol (RTP) packets containing Real Time Text (RTT, RFC 4103)
/// and notifies the user of this class when complete messages are available. A complete message may 
/// be one character or several characters.
/// This class handles RTT redundancy as specified in Section 4.2 of RFC 4103 and is capable of recovering
/// the original message even if there are dropped RTP packets.
/// </summary>
public class RttReceiver
{
    private RttParameters m_Params;
    private bool m_FirstPacketReceived = false;
    private ushort m_RtpSeq = 0;

    /// <summary>
    /// Event that is fired when at least one character is received
    /// </summary>
    /// <value></value>
    public event RttCharactersReceivedDelegate? RttCharactersReceived = null;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="rttParams">RTT session parameters from the SDP media description block.</param>
    public RttReceiver(RttParameters rttParams)
    {
        m_Params = rttParams;
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
                RttCharactersReceived?.Invoke(strNewText, Ssrc);
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
            RttCharactersReceived?.Invoke(strNewText, Ssrc);
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
            if ((TextBytes[CurrentIndex] & RttRedundantBlock.RTT_MARKER_MASK) ==
                RttRedundantBlock.RTT_MARKER_MASK)
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
}
