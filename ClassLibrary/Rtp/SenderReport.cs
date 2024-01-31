/////////////////////////////////////////////////////////////////////////////////////
//  File:   SenderReport.cs                                         29 Sep 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Rtp;

/// <summary>
/// Class for building and parsing RTCP Sender Report packets.
/// </summary>
public class SenderReport
{
    private RtcpHeader m_Header = null;
    private SenderInfo m_SenderInfo = null;
    private List<ReportBlock> m_Reports = new List<ReportBlock>();
    private const int SSRCIdx = 4;
    private const int SenderInfoIdx = 8;
    private uint m_SSRC = 0;

    /// <summary>
    /// Parses a byte array and creates a SenderReport object.
    /// </summary>
    /// <param name="Bytes">Input byte array</param>
    /// <returns>Returns a SenderReport object if successful or null if an error occurred.</returns>
    public static SenderReport Parse(byte[] Bytes)
    {
        return Parse(Bytes, 0);
    }

    /// <summary>
    /// Parses a byte array and creates a SenderReport object.
    /// </summary>
    /// <param name="Bytes">Input byte array</param>
    /// <param name="StartIdx">Starting index of the SenderReport data in the input byte array</param>
    /// <returns>Returns a SenderReport object if successful or null if an error occurred.</returns>
    public static SenderReport Parse(byte[] Bytes, int StartIdx)
    {
        int MinSenderReportLength = RtcpHeader.RTCP_HEADER_LENGTH + 4 + SenderInfo.SENDER_INFO_BLOCK_LENGTH;
        if (Bytes.Length - StartIdx < MinSenderReportLength)
            return null;    // Error: Input array too short.

        SenderReport Sr = new SenderReport();
        Sr.m_Header = new RtcpHeader(Bytes, StartIdx);
        Sr.m_SSRC = RtpUtils.GetDWord(Bytes, SSRCIdx + StartIdx);
        Sr.m_SenderInfo = SenderInfo.Parse(Bytes, SenderInfoIdx + StartIdx);
        int FixedBlockLen = RtcpHeader.HeaderLength + 4 + Sr.m_SenderInfo.SenderInfoLength;
        int BlockBytes = (Sr.m_Header.Length + 1) * 4;

        int ReportBlockCnt = Sr.m_Header.Count;
        int BytesRemaining = BlockBytes - FixedBlockLen;
        int CurIdx = FixedBlockLen + StartIdx;
        ReportBlock Rb = null;

        if (ReportBlockCnt > 0)
        {
            while (BytesRemaining >= ReportBlock.ReportBlockLength)
            {
                Rb = ReportBlock.Parse(Bytes, CurIdx);
                if (Rb == null) 
                    break;
                Sr.m_Reports.Add(Rb);
                CurIdx += ReportBlock.ReportBlockLength;
                BytesRemaining -= ReportBlock.ReportBlockLength;
            }
        }

        return Sr;
    }

    /// <summary>
    /// Constructs a new SenderReport object. Use this constructor when building a new SenderReport RTCP packet
    /// to send.
    /// </summary>
    public SenderReport()
    {
        m_Header = new RtcpHeader();
        m_SSRC = 0;
        m_Header.PacketType = RtcpPacketType.SenderReport;
        m_SenderInfo = new SenderInfo();
    }

    /// <summary>
    /// Gets the RTCP header.
    /// </summary>
    /// <value></value>
    public RtcpHeader Header
    {
        get { return m_Header; }
    }

    /// <summary>
    /// Gets or sets the SSRC of the sender.
    /// </summary>
    /// <value></value>
    public uint SSRC
    {
        get { return m_SSRC; }
        set { m_SSRC = value; }
    }

    /// <summary>
    /// Calculates the total number of bytes necessary to hold this object in a byte array.
    /// </summary>
    /// <returns>The number of bytes required for this object.</returns>
    public int GetTotalBytes()
    {
        int Len = RtcpHeader.HeaderLength + 4 + m_SenderInfo.SenderInfoLength;
        foreach (ReportBlock Rb in m_Reports)
            Len += ReportBlock.ReportBlockLength;

        return Len;
    }

    /// <summary>
    /// Gets the SenderInfo object.
    /// </summary>
    /// <value></value>
    public SenderInfo SenderInfo
    {
        get { return m_SenderInfo; }
    }

    /// <summary>
    /// Adds a new ReportBlock object to the list of ReportBlocks.
    /// </summary>
    /// <param name="Rb">ReportBlock to add to the list of report blocks</param>
    public void AddReportBlock(ReportBlock Rb)
    {
        m_Reports.Add(Rb);
    }

    /// <summary>
    /// Gets the list Report Blocks for this SenderReport.
    /// </summary>
    /// <value></value>
    public List<ReportBlock> GetReportBlocks
    {
        get { return m_Reports; }
    }

    /// <summary>
    /// Converts this RTCP SenderReport object to a byte array so that it can sent over the network.
    /// </summary>
    /// <returns>Returns a byte array containing an RTCP Sender Report.</returns>
    public byte[] ToByteArray()
    {
        byte[] RetVal = null;
        int Len = GetTotalBytes();
        m_Header.Length = Convert.ToUInt16(Len / 4 - 1);
        m_Header.Count = 1;

        RetVal = new byte[Len];
        int CurIdx = 0;
        m_Header.LoadBytes(RetVal, CurIdx);
        CurIdx += RtcpHeader.HeaderLength;
        RtpUtils.SetDWord(RetVal, SSRCIdx, m_SSRC);
        CurIdx += 4;

        m_SenderInfo.LoadBytes(RetVal, CurIdx);
        CurIdx += m_SenderInfo.SenderInfoLength;

        foreach (ReportBlock Rb in m_Reports)
        {
            Rb.LoadBytes(RetVal, CurIdx);
            CurIdx += ReportBlock.ReportBlockLength;
        }

        return RetVal;
    }

    /// <summary>
    /// Loads this object into a destination byte array.
    /// </summary>
    /// <param name="Dest">The destination byte array. This array must be long enough to hold this object
    /// beginning at the StartIdx position.</param>
    /// <param name="StartIdx">Index in Dest to load the bytes into.</param>
    public void LoadBytes(byte[] Dest, int StartIdx)
    {
        byte[] PcktBytes = ToByteArray();
        Array.ConstrainedCopy(PcktBytes, 0, Dest, StartIdx, PcktBytes.Length);
    }
}
