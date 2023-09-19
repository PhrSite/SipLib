/////////////////////////////////////////////////////////////////////////////////////
//  File:   ReportBlock.cs                                          19 Sep 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Rtp;

/// <summary>
/// Class for building or parsing the report block portion of a RTCP Sender Report or Receiver Report.
/// </summary>
public class ReportBlock
{
    private const int REPORT_BLOCK_LENGTH = 24;
    private byte[] m_PacketBytes = null;

    private const int SsrcIdx = 0;
    private const int FractionLostIdx = 4;
    private const int CumulativePacketsLostIdx = 5;
    private const int HighestSeqNumberIdx = 8;
    private const int JitterIdx = 12;
    private const int LastSrIdx = 16;
    private const int DlsrIdx = 20;

    /// <summary>
    /// Constructs a new ReportBlock object. Use this constructor when building a new ReportBlock object to
    /// include in a Sender Report or a Receiver Report to send.
    /// </summary>
    public ReportBlock()
    {
        m_PacketBytes = new byte[REPORT_BLOCK_LENGTH];
    }

    /// <summary>
    /// Constructs a ReportBlock object from a byte array. Use this constructor when parsing a ReportBlock
    /// received in an RTCP packet.
    /// </summary>
    /// <param name="Bytes">Array containing the Report Block bytes. Must be long enough to include a complete
    /// ReportBlock object beginning at the StartIdx position.</param>
    /// <param name="StartIdx">Index of the start of the ReportBlock object.</param>
    public ReportBlock(byte[] Bytes, Int32 StartIdx)
    {
        m_PacketBytes = new byte[REPORT_BLOCK_LENGTH];
        Array.ConstrainedCopy(Bytes, StartIdx, m_PacketBytes, 0, 
            REPORT_BLOCK_LENGTH);
    }

    /// <summary>
    /// Gets the length of a Report Block.
    /// </summary>
    public static Int32 ReportBlockLength
    {
        get { return REPORT_BLOCK_LENGTH; }
    }

    /// <summary>
    /// Sets or gets the SSRC of the source.
    /// </summary>
    public uint SSRC
    {
        get { return RtpUtils.GetDWord(m_PacketBytes, SsrcIdx);  }
        set { RtpUtils.SetDWord(m_PacketBytes, SsrcIdx, value);  }
    }

    /// <summary>
    /// Gets or sets the Fraction Lost field.
    /// </summary>
    public byte FractionLost
    {
        get { return m_PacketBytes[FractionLostIdx]; }
        set { m_PacketBytes[FractionLostIdx] = value; }
    }

    /// <summary>
    /// Gets or sets the Cumulative Number of Packets Lost field.
    /// </summary>
    public uint CumulativePacketsLost
    {
        get { return RtpUtils.Get3Bytes(m_PacketBytes, CumulativePacketsLostIdx); }
        set { RtpUtils.Set3Bytes(m_PacketBytes, CumulativePacketsLostIdx, value); }
    }

    /// <summary>
    /// Gets or sets the Extended Highest Sequence Number Received field.
    /// </summary>
    public uint HighestSequenceNumberReceived
    {
        get { return RtpUtils.GetDWord(m_PacketBytes, HighestSeqNumberIdx); }
        set { RtpUtils.SetDWord(m_PacketBytes, HighestSeqNumberIdx, value); }
    }

    /// <summary>
    /// Gets or sets the Inter-arrival Jitter field.
    /// </summary>
    public uint InterarrivalJitter
    {
        get { return RtpUtils.GetDWord(m_PacketBytes, JitterIdx); }
        set { RtpUtils.SetDWord(m_PacketBytes, JitterIdx, value); }
    }

    /// <summary>
    /// Gets or sets the Last SR Timestamp (LSR) field.
    /// </summary>
    public uint LastSR
    {
        get { return RtpUtils.GetDWord(m_PacketBytes, LastSrIdx); }
        set { RtpUtils.SetDWord(m_PacketBytes, LastSrIdx, value); }
    }

    /// <summary>
    /// Gets or sets the Delay Since Last SR (DLSR) field.
    /// </summary>
    public uint Dlsr
    {
        get { return RtpUtils.GetDWord(m_PacketBytes, DlsrIdx); }
        set { RtpUtils.SetDWord(m_PacketBytes, DlsrIdx, value); }
    }

    /// <summary>
    /// Loads this object into a destination byte array.
    /// </summary>
    /// <param name="Dest">The destination byte array. Must be long enough to hold this object beginning at the
    /// StartIdx position.</param>
    /// <param name="StartIdx">Index in the destination to start loading the bytes into.</param>
    public void LoadBytes(byte[] Dest, Int32 StartIdx)
    {
        Array.ConstrainedCopy(m_PacketBytes, 0, Dest, StartIdx, m_PacketBytes.Length);
    }
}
