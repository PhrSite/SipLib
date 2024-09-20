/////////////////////////////////////////////////////////////////////////////////////
//  File:   DtmfPacket.cs                                           5 Jan 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Rtp;

/// <summary>
/// Class for representing a DTMF event packet RTP payload. See Section 2.3 of RFC 4733.
/// </summary>
public class DtmfPacket
{
    private byte[] m_PacketBytes;

    /// <summary>
    /// Length of a DTMF packet
    /// </summary>
    /// <value></value>
    public const int DTMF_PACKET_LENGTH = 4;

    /// <summary>
    /// Constructor
    /// </summary>
    public DtmfPacket()
    {
        m_PacketBytes = new byte[DTMF_PACKET_LENGTH];
        Volume = DEFAULT_VOLUME_DBM;
    }

    /// <summary>
    /// Parses a byte array into a DtmfPacket object
    /// </summary>
    /// <param name="packet">Input byte array</param>
    /// <param name="Offset">Index in the input byte array that contains the DtmfPacket bytes.
    /// packet.Length - Offset must be greater than or equal to DTMF_PACKET_LENGTH</param>
    /// <returns>Returns a new DtmfPacket object</returns>
    public static DtmfPacket Parse(byte[] packet, int Offset)
    {
        if (packet == null || packet.Length - Offset < DTMF_PACKET_LENGTH)
            throw new ArgumentException("The input packet is null or too short");

        DtmfPacket dtmfPacket = new DtmfPacket();
        Array.ConstrainedCopy(packet, Offset, dtmfPacket.m_PacketBytes, 0, DTMF_PACKET_LENGTH);
        return dtmfPacket;
    }

    private const int EVENT_CODE_INDEX = 0;

    /// <summary>
    /// Gets or sets the DTMF event value.
    /// </summary>
    /// <value></value>
    public DtmfEventEnum Event
    {
        set { m_PacketBytes[EVENT_CODE_INDEX] = (byte)value; }
        get { return (DtmfEventEnum)m_PacketBytes[EVENT_CODE_INDEX]; }
    }

    private const int E_FLAG_INDEX = 1;
    private const byte E_FLAG_SET_MASK = 0x80;
    private const byte E_FLAG_CLEAR_MASK = 0x7f;

    /// <summary>
    /// Gets or sets the E flag value. The E flag is 1 (true) if the packet indicates the end of a DTMF
    /// event.
    /// </summary>
    /// <value></value>
    public bool Eflag
    {
        get { return (m_PacketBytes[E_FLAG_INDEX] & E_FLAG_SET_MASK) == E_FLAG_SET_MASK ? true : false; }
        set
        {
            m_PacketBytes[E_FLAG_INDEX] = value == true ? (byte)(m_PacketBytes[E_FLAG_INDEX] | E_FLAG_SET_MASK) :
                (byte)(m_PacketBytes[E_FLAG_INDEX] & E_FLAG_CLEAR_MASK);
        }
    }

    /// <summary>
    /// Default DTMF tone volume in dBm.
    /// </summary>
    /// <value></value>
    public const int DEFAULT_VOLUME_DBM = -13;
    private const int VOLUME_INDEX = 1;
    private const byte VOLUME_MASK = 0x3f;

    /// <summary>
    /// Gets or sets the DTMF tone volume in dBm. The allowable range is from 0 - -63 dBm.
    /// This value defaults to DEFAULT_VOLUME_DBM, which is -13 dBm.
    /// </summary>
    /// <value></value>
    public int Volume
    {
        get
        {
            int Temp = m_PacketBytes[VOLUME_INDEX] & VOLUME_MASK;
            return Temp * -1;
        }
        set
        {
            if (value > 0) value = 0;
            else if (value < -63) value = -63;

            // Clear the bits in the volume field
            m_PacketBytes[VOLUME_INDEX] = (byte) (m_PacketBytes[VOLUME_INDEX] & ~VOLUME_MASK);
            // Store the volume as a positive number.
            m_PacketBytes[VOLUME_INDEX] = (byte) (m_PacketBytes[VOLUME_INDEX] | ((value * -1) & VOLUME_MASK));
        }
    }

    private const int DURATION_INDEX = 2;

    /// <summary>
    /// Gets or sets the Duration of the DTMF event. The Duration is in RTP packet Timestamp units.
    /// </summary>
    /// <value></value>
    public ushort Duration
    {
        get
        {
            return RtpUtils.GetWord(m_PacketBytes, DURATION_INDEX);
        }
        set
        {
            RtpUtils.SetWord(m_PacketBytes, DURATION_INDEX, value);
        }
    }

    /// <summary>
    /// Gets the packet bytes.
    /// </summary>
    /// <returns>Returns a byte array of length DTMF_PACKET_LENGTH</returns>
    public byte[] GetPacketBytes()
    {
        return m_PacketBytes;
    }
}

/// <summary>
/// Enumeration of DTMF event codes. See Section 3.2 of RFC 4733.
/// </summary>
public enum DtmfEventEnum : byte
{
    /// <summary></summary>
    Zero,
    /// <summary></summary>
    One,
    /// <summary></summary>
    Two,
    /// <summary></summary>
    Three,
    /// <summary></summary>
    Four,
    /// <summary></summary>
    Five,
    /// <summary></summary>
    Six,
    /// <summary></summary>
    Seven,
    /// <summary></summary>
    Eight,
    /// <summary></summary>
    Nine,
    /// <summary></summary>
    Asterisk,
    /// <summary></summary>
    Pound,
    /// <summary></summary>
    A,
    /// <summary></summary>
    B,
    /// <summary></summary>
    C,
    /// <summary></summary>
    D,
    /// <summary></summary>
    E,
    /// <summary></summary>
    F
}
