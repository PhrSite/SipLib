/////////////////////////////////////////////////////////////////////////////////////
//  File:	SenderInfo.cs                                           19 Sep 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Rtp;

/// <summary>
/// Class for the Sender Info part of a Sender Report RTCP packet.
/// </summary>
public class SenderInfo
{
    private const int SEND_INFO_BLOCK_LENGTH = 20;
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
        m_PacketBytes = new byte[SEND_INFO_BLOCK_LENGTH];
    }

    /// <summary>
    /// Constructs a new SenderInfo object. Use this constructor when parsing a received SenderReport RTCP
    /// packet.
    /// </summary>
    /// <param name="Bytes">Byte array containing a SenderInfo block.</param>
    /// <param name="StartIdx">Starting index of the SenderInfo block in the byte array.</param>
    public SenderInfo(byte[] Bytes, int StartIdx)
    {
        m_PacketBytes = new byte[SEND_INFO_BLOCK_LENGTH];
        Array.ConstrainedCopy(Bytes, StartIdx, m_PacketBytes, 0, SEND_INFO_BLOCK_LENGTH);
    }

    /// <summary>
    /// Gets the length of the SendInfo block.
    /// </summary>
    public Int32 SenderInfoLength
    {
        get { return SEND_INFO_BLOCK_LENGTH; }
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
    public UInt32 SenderPacketCount
    {
        get { return RtpUtils.GetDWord(m_PacketBytes, SenderPcktCntIdx); }
        set { RtpUtils.SetDWord(m_PacketBytes, SenderPcktCntIdx, value); }
    }

    /// <summary>
    /// Gets or sets the Sender Octet Count field.
    /// </summary>
    public UInt32 SenderOctetCount
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
    public void LoadBytes(byte[] Dest, Int32 StartIdx)
    {
        Array.ConstrainedCopy(m_PacketBytes, 0, Dest, StartIdx, m_PacketBytes.Length);
    }
}
