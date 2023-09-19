/////////////////////////////////////////////////////////////////////////////////////
//	File:	RtpPacket.cs                                            19 Sep 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Rtp;

/// <summary>
/// Class for handling Real Time Protocol (RTP) network packets. See RFC 3550.
/// </summary>
public class RtpPacket
{
    /// <summary>
    /// Minimum RTP packet length. This assumes that CSRCs are included.
    /// </summary>
    public const int MIN_PACKET_LENGTH = 12;

    private byte[] m_PacketBytes = null;

    /// <summary>
    /// Constructs a RTP packet containing only a RTP packet header without a payload.
    /// </summary>
    public RtpPacket()
    {
            m_PacketBytes = new byte[MIN_PACKET_LENGTH];
            Version = 2;
    }

    /// <summary>
    /// Constructs a RTP packet from an array of bytes.
    /// </summary>
    /// <param name="SrcBytes">Byte array to "attach" to. Must be at least MIN_PACKET_LENGTH bytes long.</param>
    /// <exception cref="ArgumentException">Thrown if the SrcBytes array is less than MIN_PACKET_LENGTH bytes
    /// in length.</exception>
    // <exception cref="ArgumentNullException">If SrcBytes is null.</exception>
    public RtpPacket(byte[] SrcBytes)
    {
        if (SrcBytes == null)
            throw new ArgumentNullException("SrcBytes is null");

        if (SrcBytes.Length < MIN_PACKET_LENGTH)
            throw new ArgumentException("Array length too short. Must be at " +
                "least RTP_PACKET_LENGTH bytes long.");

        m_PacketBytes = SrcBytes;
        Version = 2;
    }

    /// <summary>
    /// The first byte contains the V, P, X and CC fields.
    /// </summary>
    private const int RTP_VERSION_OFFSET = 0;

    /// <summary>
    /// Gets the Version field.
    /// </summary>
    public int Version
    {
        private set
        {	// Sets the 2 bit Version field to a fixed value of 2.
            m_PacketBytes[RTP_VERSION_OFFSET] &= 0x3f;
            m_PacketBytes[RTP_VERSION_OFFSET] |= 0x80;
        }
        get
        {
            return m_PacketBytes[RTP_VERSION_OFFSET] >> 6;
        }
    }

    // Need a set and clear mask because the ~ operator is not defined for a byte in C#
    private const byte PaddingSetMask = 0x20;
    private const byte PaddingClearMask = 0xdf;

    /// <summary>
    /// Gets or sets the Padding bit.
    /// </summary>
    public bool Padding
    {
        set
        {
            if (value == true)
                m_PacketBytes[RTP_VERSION_OFFSET] |= PaddingSetMask;
            else
                m_PacketBytes[RTP_VERSION_OFFSET] &= PaddingClearMask;
        }
        get
        {
                if ((m_PacketBytes[RTP_VERSION_OFFSET] & PaddingSetMask) == 
                PaddingSetMask)
                return true;
            else
                return false;
        }
    }

    private const int CsrcMask = 0x0f;

    /// <summary>
    /// Gets or sets the CSRC count.
    /// </summary>
    public int CsrcCount
    {
        set
        {
            m_PacketBytes[RTP_VERSION_OFFSET] |= Convert.ToByte(value & CsrcMask);
        }
        get
        {
            return m_PacketBytes[RTP_VERSION_OFFSET] & CsrcMask;
        }
    }

    /// <summary>
    /// Length of each CSRC in bytes
    /// </summary>
    public const int CSRC_LENGTH = 4;

    /// <summary>
    /// Gets a list of CSRCs (contributing source identifiers) for this RTP packet.
    /// </summary>
    public List<uint> CSRCs
    {
        get
        {
            if (CsrcCount == 0)
                return null;

            List<uint> Result = new List<uint>();
            int Idx = MIN_PACKET_LENGTH;
            int Cnt = CsrcCount;
            for (int i=0; i < Cnt; i++)
            {
                Result.Add(RtpUtils.GetDWord(m_PacketBytes, Idx));
                Idx += CSRC_LENGTH;
            }

            return Result;
        }
    }

    private int CsrcStartIdx = MIN_PACKET_LENGTH;

    /// <summary>
    /// Gets a CSRC specified by its index.
    /// </summary>
    /// <param name="Idx">Index of the CSRC to get.</param>
    /// <returns>Returns the specified CSRC. Returns 0 if the index is out
    /// range.</returns>
    public uint GetCSRC(int Idx)
    {
        if (Idx >= CsrcCount)
            return 0;   // Error
        else
            return RtpUtils.GetDWord(m_PacketBytes, CsrcStartIdx + Idx * CSRC_LENGTH);
    }

    /// <summary>
    /// Sets a CSRC value.
    /// </summary>
    /// <param name="Idx">Index of the CSRC to set.</param>
    /// <param name="CSRC">CSRC value.</param>
    public void SetCSRC(int Idx, uint CSRC)
    {
        if (Idx >= CsrcCount)
            return; // Error
        else
            RtpUtils.SetDWord(m_PacketBytes, CsrcStartIdx + Idx * 4, CSRC);
    }

    /// <summary>
    /// Gets or sets the payload of the RTP packet.
    /// </summary>
    // <exception cref="ArgumentException">Thrown by if the input packet is longer than the allocated payload
    // length for this RTP packet.</exception>
    public byte[] Payload
    {
        get
        {
            if (PayloadLength == 0)
                return null;
            else
            {
                byte[] Pl = new byte[PayloadLength];
                Array.ConstrainedCopy(m_PacketBytes, HeaderLength, Pl, 0, PayloadLength);                    
                return Pl;
            }
        }
        set
        {
            if (value.Length > PayloadLength)
                throw new ArgumentException("The array is too long.");

            Array.ConstrainedCopy(value, 0, m_PacketBytes, HeaderLength, value.Length);
        }
    }

    /// <summary>
    /// Gets the length of the RTP packet header.
    /// </summary>
    public int HeaderLength
    {
        get
        {
            return MIN_PACKET_LENGTH + CsrcCount * CSRC_LENGTH;
        }
    }

    // Index to the byte that contains the M bit and the PT fields.
    private const int MARKER_PAYLOAD_INDEX = 1;
    private const byte MARKER_SET_MASK = 0x80;
    private const byte MARKER_CLEAR_MASK = 0x7f;

    /// <summary>
    /// Gets or sets the M (marker) bit.
    /// </summary>
    public bool Marker
    {
        set
        {
            if (value == true)
                m_PacketBytes[MARKER_PAYLOAD_INDEX] |= MARKER_SET_MASK;
            else
                m_PacketBytes[MARKER_PAYLOAD_INDEX] &= MARKER_CLEAR_MASK;
        }
        get
        {
            if ((m_PacketBytes[MARKER_PAYLOAD_INDEX] & MARKER_SET_MASK) == MARKER_SET_MASK)
                return true;
            else
                return false;
        }
    }

    private byte PayloadTypeMask = 0x7f;

    /// <summary>
    /// Gets or sets the Payload Type (PT) field in the RTP header. The Payload Type must be between 0 and 127.
    /// </summary>
    public int PayloadType
    {
        set
        {
            m_PacketBytes[MARKER_PAYLOAD_INDEX] |= Convert.ToByte(value & PayloadTypeMask);
        }
        get { return m_PacketBytes[MARKER_PAYLOAD_INDEX] & PayloadTypeMask; }
    }

    private const int SEQUENCE_NUMBER_INDEX = 2;

    /// <summary>
    /// Gets or sets the Sequence Number field of the RTP packet header.
    /// </summary>
    public ushort SequenceNumber
    {
        set { RtpUtils.SetWord(m_PacketBytes, SEQUENCE_NUMBER_INDEX, value); }
        get { return RtpUtils.GetWord(m_PacketBytes, SEQUENCE_NUMBER_INDEX); }
    }

    private const int TIMESTAMP_INDEX = 4;

    /// <summary>
    /// Gets or sets the Timestamp field of the RTP packet header.
    /// </summary>
    public uint Timestamp
    {
        set { RtpUtils.SetDWord(m_PacketBytes, TIMESTAMP_INDEX, value); }
        get { return RtpUtils.GetDWord(m_PacketBytes, TIMESTAMP_INDEX); }
    }

    private const int SSRC_INDEX = 8;

    /// <summary>
    /// Gets or sets the synchronization source identifier (SSRC).
    /// </summary>
    public uint SSRC
    {
        set { RtpUtils.SetDWord(m_PacketBytes, SSRC_INDEX, value); }
        get { return RtpUtils.GetDWord(m_PacketBytes, SSRC_INDEX); }
    }

    /// <summary>
    /// Gets the number of bytes in the RTP packet payload.
    /// </summary>
    public Int32 PayloadLength
    {
        get { return m_PacketBytes.Length - HeaderLength; }
    }

    /// <summary>
    /// Strips out the RTP packet header and returns new byte array containing the packet payload.
    /// </summary>
    /// <returns>Returns null if there is no payload or a new byte array containing the payload bytes if there
    /// is a payload for the RTP packet.
    /// </returns>
    public byte[] GetPayloadBytes()
    {
        int Len = PayloadLength;
        if (Len <= 0)
            return null;

        byte[] PayloadBytes = new byte[Len];
        Array.ConstrainedCopy(m_PacketBytes, HeaderLength, PayloadBytes, 0, Len);
        return PayloadBytes;
    }

    /// <summary>
    /// Gets only the bytes from the header portion of the RTP packet.
    /// </summary>
    /// <returns>Returns a byte array containing the header bytes.</returns>
    public byte[] GetHeaderBytes()
    {
        byte[] HdrBytes = new byte[HeaderLength];
        Array.Copy(m_PacketBytes, HdrBytes, HeaderLength);
        return HdrBytes;
    }
}