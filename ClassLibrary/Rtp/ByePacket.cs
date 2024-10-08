﻿/////////////////////////////////////////////////////////////////////////////////////
//  File:   BytePacket.cs                                           28 Sep 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

using System.Text;

namespace SipLib.Rtp;

/// <summary>
/// Class for building and parsing RTCP BYE packets. See Section 6.6 of RFC 3550.
/// </summary>
public class ByePacket
{
    private RtcpHeader? m_Header;
    private List<uint> m_SsrcList = new List<uint>();
    private string? m_Reason = null;

    /// <summary>
    /// Constructor
    /// </summary>
    public ByePacket()
    {
    }

    /// <summary>
    /// Parses a received byte array into a ByePacket object
    /// </summary>
    /// <param name="Bytes">Received input byte array</param>
    /// <param name="StartIdx">Start index of the ByePacket in the array</param>
    /// <returns>Returns a new ByePacket or null if a parsing error occurred</returns>
    public static ByePacket? Parse(byte[] Bytes, int StartIdx)
    {
        ByePacket Bp = new ByePacket();
        if (StartIdx + RtcpHeader.HeaderLength > Bytes.Length)
            // Error: The input byte array is too short.
            return null;

        Bp.m_Header = new RtcpHeader(Bytes, StartIdx);
        int TotalBytes = (Bp.m_Header.Length + 1) * 4;
        if (StartIdx + TotalBytes > Bytes.Length)
            // Error: The input byte array is too short.
            return null;

        int CurIdx = RtcpHeader.HeaderLength + StartIdx;
        int i;
        for (i = 0; i < Bp.m_Header.Count; i++)
        {
            if (CurIdx + 4 > Bytes.Length)
                return Bp;

            Bp.m_SsrcList.Add(RtpUtils.GetDWord(Bytes, CurIdx));
            CurIdx += 4;
        }

        // Get the length of the Reason string;
        if (CurIdx >= Bytes.Length)
            return Bp;

        int ReasonLen = Bytes[CurIdx++] & 0xff;
        if (CurIdx + ReasonLen > Bytes.Length)
            return Bp;

        byte[] ReasonBytes = new byte[ReasonLen];
        Array.ConstrainedCopy(Bytes, CurIdx, ReasonBytes, 0, ReasonLen);
        Bp.m_Reason = Encoding.UTF8.GetString(ReasonBytes);

        return Bp;
    }

    /// <summary>
    /// Gets the reason for the BYE packet. Returns null if this packet is not valid.
    /// </summary>
    /// <value></value>
    public string? Reason
    {
        get { return m_Reason; }
    }

    /// <summary>
    /// Gets the list of SSRCs. Always returns a non-null value. If the list is empty then this packet is not
    /// valid.
    /// </summary>
    /// <value></value>
    public List<uint> SSRCs
    {
        get { return m_SsrcList; }
    }

    /// <summary>
    /// Gets the RtcpHeader for this packet.
    /// </summary>
    /// <value></value>
    public RtcpHeader? Header
    {
        get { return m_Header; }
    }

    /// <summary>
    /// Constructs a new BYE packet for sending.
    /// </summary>
    /// <param name="Ssrcs">Contains a list of SSRC identifies that the BYE packet pertains to. The list
    /// must contain at least one SSRC.</param>
    /// <param name="Reason">A string that describes the reason for leaving.</param>
    public ByePacket(List<uint> Ssrcs, string Reason)
    {
        if (Ssrcs.Count > 31)
            throw new ArgumentException(string.Format(
                "The Ssrcs argument must contain 31 SSRCs or less. Actual count = {0}", Ssrcs.Count));

        if (Reason.Length > 255)
            throw new ArgumentException(string.Format(
                "The Reason must be 255 characters of less. Actual length = {0}", Reason.Length));

        m_Header = new RtcpHeader();
        m_Header.PacketType = RtcpPacketType.ByePacket;
        m_Reason = Reason;
        m_SsrcList = Ssrcs;
    }

    /// <summary>
    /// Converts this object to a byte array.
    /// </summary>
    /// <returns>Returns a byte array containing the binary version of this object. The returned byte array
    /// is padded so that it contains a whole number of 4-byte words.</returns>
    public byte[] ToByteArray()
    {
        byte[] ReasonBytes = Encoding.UTF8.GetBytes(m_Reason);
        int RequiredLen = RtcpHeader.HeaderLength + 4 * m_SsrcList.Count + 1 + ReasonBytes.Length;
        if (RequiredLen % 4 != 0)
            RequiredLen += 4 - (RequiredLen % 4);   // Pad to a 4-byte boundary

        byte[] PcktBytes = new byte[RequiredLen];
        m_Header.Count = m_SsrcList.Count;
        m_Header.Length = (ushort) (RequiredLen / 4 - 1);
        m_Header.LoadBytes(PcktBytes, 0);
        int CurIdx = RtcpHeader.HeaderLength;

        for (int i=0; i < m_SsrcList.Count; i++)
        {
            RtpUtils.SetDWord(PcktBytes, CurIdx, m_SsrcList[i]);
            CurIdx += 4;
        }

        PcktBytes[CurIdx++] = (byte) ReasonBytes.Length;
        Array.ConstrainedCopy(ReasonBytes, 0, PcktBytes, CurIdx, ReasonBytes.Length);

        return PcktBytes;
    }
}
