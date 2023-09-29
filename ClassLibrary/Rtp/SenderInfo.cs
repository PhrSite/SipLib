/////////////////////////////////////////////////////////////////////////////////////
//  File:	SenderInfo.cs                                           29 Sep 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Rtp;

/// <summary>
/// Class for the Sender Info part of a Sender Report RTCP packet. See Section 6.4.1 of RFC 3550.
/// </summary>
public class SenderInfo
{
    /// <summary>
    /// Fixed length of a SenderInfo block
    /// </summary>
    public const int SENDER_INFO_BLOCK_LENGTH = 20;

    private byte[] m_PacketBytes = null;

    private const int NtpIdx = 0;
    private const int RtpTimestampIdx = 8;
    private const int SenderPcktCntIdx = 12;
    private const int SenderOctetCntIdx = 16;

    /// <summary>
    /// Constructs a new SenderInfo object. Use this constructor when building
    /// a new RTCP packet for to send a Sender Report.
    /// </summary>
    public SenderInfo()
    {
        m_PacketBytes = new byte[SENDER_INFO_BLOCK_LENGTH];
    }

    /// <summary>
    /// Parses a byte array and returns a SenderInfo object.
    /// </summary>
    /// <param name="Bytes">Input byte array</param>
    /// <param name="StartIdx">Starting index of the SenderInfo data in the input byte array.</param>
    /// <returns>Returns a SenderInfo object if successful or null if an error occurred.</returns>
    public static SenderInfo Parse(byte[] Bytes, int StartIdx)
    {
        SenderInfo Si = new SenderInfo();
        if (Bytes == null || (Bytes.Length - StartIdx) < SENDER_INFO_BLOCK_LENGTH)
            return null;

        Si.m_PacketBytes = new byte[SENDER_INFO_BLOCK_LENGTH];
        Array.ConstrainedCopy(Bytes, StartIdx, Si.m_PacketBytes, 0, SENDER_INFO_BLOCK_LENGTH);
        return Si;
    }

    /// <summary>
    /// Gets the length of the SendInfo block.
    /// </summary>
    public int SenderInfoLength
    {
        get { return SENDER_INFO_BLOCK_LENGTH; }
    }

    /// <summary>
    /// Gets UTC time from the NTP timestamp or sets the NTP timestamp field from the UTC time.
    /// </summary>
    public DateTime NTP
    {
        get
        {
            ulong NtpTs = RtpUtils.Get8ByteWord(m_PacketBytes, NtpIdx);
            DateTime Dt = RtpUtils.NtpTimeStampToDateTime(NtpTs);
            return RtpUtils.NtpTimeStampToDateTime(NtpTs);
        }
        set
        {
            ulong NtpTs = RtpUtils.DateTimeToNtpTimestamp(value);
            RtpUtils.Set8ByteWord(m_PacketBytes, NtpIdx, NtpTs);
        }
    }

    /// <summary>
    /// Gets or sets the RTP Timestamp field.
    /// </summary>
    public uint RtpTimestamp
    {
        get { return RtpUtils.GetDWord(m_PacketBytes, RtpTimestampIdx); }
        set { RtpUtils.SetDWord(m_PacketBytes, RtpTimestampIdx, value); }
    }

    /// <summary>
    /// Gets or sets the Sender Packet Count field.
    /// </summary>
    public uint SenderPacketCount
    {
        get { return RtpUtils.GetDWord(m_PacketBytes, SenderPcktCntIdx); }
        set { RtpUtils.SetDWord(m_PacketBytes, SenderPcktCntIdx, value); }
    }

    /// <summary>
    /// Gets or sets the Sender Octet Count field.
    /// </summary>
    public uint SenderOctetCount
    {
        get { return RtpUtils.GetDWord(m_PacketBytes, SenderOctetCntIdx); }
        set { RtpUtils.SetDWord(m_PacketBytes, SenderOctetCntIdx, value); }
    }

    /// <summary>
    /// Loads this object into a destination byte array.
    /// </summary>
    /// <param name="Dest">The destination byte array. Must be long enough to hold this object beginning at
    /// the StartIdx position.</param>
    /// <param name="StartIdx">Index in the destination to start loading the bytes into.</param>
    public void LoadBytes(byte[] Dest, int StartIdx)
    {
        Array.ConstrainedCopy(m_PacketBytes, 0, Dest, StartIdx, m_PacketBytes.Length);
    }
}
