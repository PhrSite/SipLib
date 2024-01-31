/////////////////////////////////////////////////////////////////////////////////////
//  File:   RtcpHeader.cs                                           19 Sep 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Rtp;

/// <summary>
/// Enumeration for the RTCP packet type. 
/// </summary>
public enum RtcpPacketType
{
    /// <summary>
    /// Sender Report. See Section 6.4.1 of RFC 3550.
    /// </summary>
    SenderReport = 200,
    /// <summary>
    /// Receiver Report. See Section 6.4.2 of RFC 3550.
    /// </summary>
    ReceiverReport = 201,
    /// <summary>
    /// Source description report (SDES). See Section 6.5 of RFC 3550.
    /// </summary>
    SourceDescriptionReport = 202,
    /// <summary>
    /// BYE Packet. See Section 6.6 of RFC 3550.
    /// </summary>
    ByePacket = 203,
}

/// <summary>
/// Class for creating and parsing Real Time Control Protocol (RTCP) headers.
/// </summary>
public class RtcpHeader
{
    /// <summary>
    /// Length of a RTCP header
    /// </summary>
    /// <value></value>
    public const int RTCP_HEADER_LENGTH = 4;

    private byte[] m_PacketBytes = null;
    /// <summary>
    /// The first byte contains the V, P, Count fields.
    /// </summary>
    private const int RTCP_VERSION_OFFSET = 0;
    private const int RTCP_PADDING_OFFSET = RTCP_VERSION_OFFSET;
    private const int RTCP_COUNT_OFFSET = RTCP_VERSION_OFFSET;

    private const int RTCP_PACKET_TYPE_OFFSET = 1;
    private const int RTCP_LENGTH_OFFSET = 2;

    /// <summary>
    /// Creates a new RtcpHeader for building RTCP packets.
    /// </summary>
    public RtcpHeader()
    {
        m_PacketBytes = new byte[RTCP_HEADER_LENGTH];
        Version = 2;
    }

    /// <summary>
    /// Constructs a new RtcpHeader object from a byte array. Use this constructor when parsing a received
    /// RTCP packet.
    /// </summary>
    /// <param name="Bytes">Byte array containing the RTCP header. Note: There must be at least 4 bytes between
    /// the StartIdx and the end of this byte array.</param>
    /// <param name="StartIdx">Index of the start of the RTCP header.</param>
    public RtcpHeader(byte[] Bytes, int StartIdx)
    {
        m_PacketBytes = new byte[RTCP_HEADER_LENGTH];
        Array.ConstrainedCopy(Bytes, StartIdx, m_PacketBytes, 0, RTCP_HEADER_LENGTH);
    }

    /// <summary>
    /// Gets the fixed header length or an RTCP packet header.
    /// </summary>
    /// <value></value>
    public static Int32 HeaderLength
    {
        get { return RTCP_HEADER_LENGTH; }
    }

    /// <summary>
    /// Gets the Version field.
    /// </summary>
    /// <value></value>
    public int Version
    {
        private set
        {   // Sets the 2 bit Version field to a fixed value of 2.
            m_PacketBytes[RTCP_VERSION_OFFSET] &= 0x3f;
            m_PacketBytes[RTCP_VERSION_OFFSET] |= 0x80;
        }
        get
        {
            return m_PacketBytes[RTCP_VERSION_OFFSET] >> 6;
        }
    }

    private const byte PaddingSetMask = 0x20;
    private const byte PaddingClearMask = 0xdf;

    /// <summary>
    /// Gets or sets the Padding bit.
    /// </summary>
    /// <value></value>
    public bool Padding
    {
        set
        {
            if (value == true)
                m_PacketBytes[RTCP_PADDING_OFFSET] |= PaddingSetMask;
            else
                m_PacketBytes[RTCP_PADDING_OFFSET] &= PaddingClearMask;
        }
        get
        {
            if ((m_PacketBytes[RTCP_PADDING_OFFSET] & PaddingSetMask) ==
                    PaddingSetMask)
                    return true;
            else
                    return false;
        }
    }

    /// <summary>
    /// Sets or gets the Padding bit value as an int
    /// </summary>
    /// <value></value>
    public int PaddingBit
    {
        get { return Padding == true ? 1 : 0; }
        set { Padding = value == 1 ? true : false; }
    }

    private byte CountMask = 0x01f;

    /// <summary>
    /// Gets or sets the Count field.
    /// </summary>
    /// <value></value>
    public int Count
    {
        get { return m_PacketBytes[RTCP_COUNT_OFFSET] & CountMask; }
        set
        {
            m_PacketBytes[RTCP_VERSION_OFFSET] &= Convert.ToByte(~CountMask & 0xff);
            m_PacketBytes[RTCP_COUNT_OFFSET] |= Convert.ToByte(value & CountMask);
        }
    }

    /// <summary>
    /// Gets or sets the PT (Packet Type) field.
    /// </summary>
    /// <value></value>
    public RtcpPacketType PacketType
    {
        get { return (RtcpPacketType)m_PacketBytes[RTCP_PACKET_TYPE_OFFSET]; }
        set { m_PacketBytes[RTCP_PACKET_TYPE_OFFSET] = Convert.ToByte(value); }
    }

    /// <summary>
    /// Gets or sets the Length field.
    /// </summary>
    /// <value></value>
    public ushort Length
    {
        get { return RtpUtils.GetWord(m_PacketBytes, RTCP_LENGTH_OFFSET); }
        set { RtpUtils.SetWord(m_PacketBytes, RTCP_LENGTH_OFFSET, value); }
    }

    /// <summary>
    /// Loads the byte array containing the RTCP header block into a destination byte array.
    /// </summary>
    /// <param name="Dest">Destination array for the RTCP header bytes. This byte array must be long enough to
    /// hold this object beginning at the StartIdx position.</param>
    /// <param name="StartIdx">Starting index to load the header bytes at.</param>
    public void LoadBytes(byte[] Dest, int StartIdx)
    {
        Array.ConstrainedCopy(m_PacketBytes, 0, Dest, StartIdx, m_PacketBytes.Length);
    }
}
