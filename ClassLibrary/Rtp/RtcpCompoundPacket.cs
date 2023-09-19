/////////////////////////////////////////////////////////////////////////////////////
//  File:   RtcpCompoundPacket.cs                                   19 Sep 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Rtp;

/// <summary>
/// Class for building and sending compound RTCP packets and for parsing and processing compound RTCP packets
/// received from the network. See RFC 3550.
/// </summary>
public class RtcpCompoundPacket
{
    /// <summary>
    /// List of Sender Reports. Initialized to an empty list.
    /// </summary>
    public List<SenderReport> SenderReports = new List<SenderReport>();

    /// <summary>
    /// List of Receiver Reports. Initialized to an empty list.
    /// </summary>
    public List<ReceiverReport> ReceiverReports = new List<ReceiverReport>();

    /// <summary>
    /// List of Session Description (SDES) packets. Initialized to an empty 
    /// list.
    /// </summary>
    public List<SdesPacket> SdesPackets = new List<SdesPacket>();

    /// <summary>
    /// List of BYE packets. Initialized to an empty list.
    /// </summary>
    public List<ByePacket> ByePackets = new List<ByePacket>();

    /// <summary>
    /// Creates an empty RTCP compound packet. Add Sender Reports, Receiver Reports, SDES packets or BYE
    /// packets to the appropriate lists and then call ToByteArray() to build a compound RTCP packet to send
    /// over the network as a byte array.
    /// </summary>
    public RtcpCompoundPacket()
    {
    }

    /// <summary>
    /// Builds a RtcpCompoundPacket object by parsing a byte array that was received from the network.
    /// </summary>
    /// <param name="Bytes">Input byte array to parse.</param>
    public RtcpCompoundPacket(byte[] Bytes)
    {
        if (Bytes.Length < RtcpHeader.HeaderLength)
            return;

        RtcpHeader Header;
        bool Done = false;
        int CurIdx = 0;
        int PacketLen = 0;

        while (Done == false)
        {
            Header = new RtcpHeader(Bytes, CurIdx);
            switch (Header.PacketType)
            {
                case RtcpPacketType.SenderReport:
                    SenderReports.Add(new SenderReport(Bytes, CurIdx));
                    break;
                case RtcpPacketType.ReceiverReport:
                    ReceiverReports.Add(new ReceiverReport(Bytes, CurIdx));
                    break;
                case RtcpPacketType.SourceDescriptionReport:
                    SdesPackets.Add(new SdesPacket(Bytes, CurIdx));
                    break;
                case RtcpPacketType.ByePacket:
                    ByePackets.Add(new ByePacket(Bytes, CurIdx));
                    break;
                default:
                    // This is an error condition so stop trying to parse the
                    // compound packet.
                    Done = true;
                    break;
            }

            if (Done == false)
            {
                PacketLen = (Header.Length + 1) * 4;
                if (CurIdx + PacketLen >= Bytes.Length)
                    Done = true;
                else
                    CurIdx += PacketLen;
            }
        } // end while done == false
    }

    /// <summary>
    /// Converts this object to a byte array so that the compound RTCP packet can be sent over the network.
    /// </summary>
    /// <returns>Returns a byte array. Return null if there are no Sender Reports, Receiver Reports, SDES packets or BYE packets to send.
    /// </returns>
    public byte[] ToByteArray()
    {
        if (SenderReports.Count == 0 && ReceiverReports.Count == 0)
            return null;

        List<byte[]> Packets = new List<byte[]>();
        byte[] RetPacket = null;

        foreach (SenderReport Sr in SenderReports)
            Packets.Add(Sr.ToByteArray());

        foreach (ReceiverReport Rr in ReceiverReports)
            Packets.Add(Rr.ToByteArray());

        foreach (SdesPacket Sp in SdesPackets)
            Packets.Add(Sp.ToByteArray());

        foreach (ByePacket Bp in ByePackets)
            Packets.Add(Bp.ToByteArray());

        if (Packets.Count == 0)
            return null;

        // Figure out how much total storage is required.
        int TotalBytes = 0;
        foreach (byte[] Ary in Packets)
            TotalBytes += Ary.Length;

        if (TotalBytes == 0)
            return null;

        RetPacket = new byte[TotalBytes];
        int CurIdx = 0;
        foreach (byte[] Ary1 in Packets)
        {
            Array.ConstrainedCopy(Ary1, 0, RetPacket, CurIdx, Ary1.Length);
            CurIdx += Ary1.Length;
        }

        return RetPacket;
    }
}
