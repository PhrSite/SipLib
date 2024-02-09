/////////////////////////////////////////////////////////////////////////////////////
//  File:   ReceiverReport.cs                                       19 Sep 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

using System.Security.Cryptography;

namespace SipLib.Rtp;

/// <summary>
/// Class for building and parsing RTCP Receiver Report packets. See Section 6.4.2 of RFC 3550.
/// </summary>
public class ReceiverReport
{
    private RtcpHeader? m_Header = null;
    private uint m_Ssrc = 0;
    private int SSRC_LENGTH = 4;
    private int SsrcIdx = 4;

    private List<ReportBlock> m_Reports = new List<ReportBlock>();

    /// <summary>
    /// Constructs a new ReceiverReport object. Use this constructor when building a Receiver RTCP message to
    /// send.
    /// </summary>
    public ReceiverReport()
    {
        m_Header = new RtcpHeader();
        m_Header.PacketType = RtcpPacketType.ReceiverReport;
    }

    /// <summary>
    /// Gets or sets the SSRC field.
    /// </summary>
    /// <value></value>
    public uint SSRC
    {
        get { return m_Ssrc; }
        set { m_Ssrc = value; }
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
    /// Calculates the total number of bytes necessary to hold this object in a byte array.
    /// </summary>
    /// <returns>The number of bytes required for this object.</returns>
    public int GetTotalBytes()
    {
        int TotalLength = RtcpHeader.HeaderLength + SSRC_LENGTH + m_Reports.Count * ReportBlock.ReportBlockLength;
        return TotalLength;
    }

    /// <summary>
    /// Parses a byte array containing a ReceiverReport into a ReceiverReport object.
    /// </summary>
    /// <param name="Bytes">Input byte array containing a ReceiverReport.</param>
    /// <param name="StartIdx">Index of the first byte containing the header for the Receiver Report.</param>
    /// <returns>Returns a new ReceiverReport object if successful or null if an error occurred.</returns>
    public static ReceiverReport? Parse(byte[] Bytes, int StartIdx)
    {
        ReceiverReport Rr = new ReceiverReport();
        Rr.m_Header = new RtcpHeader(Bytes, StartIdx);
        int CurIdx = StartIdx + RtcpHeader.HeaderLength;
        Rr.m_Ssrc = RtpUtils.GetDWord(Bytes, CurIdx);
        CurIdx += Rr.SSRC_LENGTH;
        int ReportBlockCnt = Rr.m_Header.Count;

        if (CurIdx + ReportBlockCnt * ReportBlock.ReportBlockLength > Bytes.Length)
            return null;     // Error: Input array is too short.

        if (Rr.m_Header.Count > 0)
        {
            ReportBlock Rb = null;
            for (int i = 0; i < ReportBlockCnt; i++)
            {
                Rb = ReportBlock.Parse(Bytes, CurIdx);
                if (Rb == null)
                    break;
                Rr.m_Reports.Add(Rb);
                CurIdx += ReportBlock.ReportBlockLength;
            }
        }
         
        return Rr;
    }

    /// <summary>
    /// Gets the RTCP header.
    /// </summary>
    /// <value></value>
    public RtcpHeader? Header
    {
        get { return m_Header; }
    }

    /// <summary>
    /// Converts this RTCP SenderReport object to a byte array so that it can be sent over the network.
    /// </summary>
    /// <returns>Returns a byte array containing an RTCP Sender Report./// </returns>
    public byte[] ToByteArray()
    {
        byte[] RetVal = null;
        int Len = GetTotalBytes();
        RetVal = new byte[Len];
        int CurIdx = 0;
        m_Header.Length = Convert.ToUInt16(Len / 4 - 1);
        m_Header.Count = m_Reports.Count;
        m_Header.LoadBytes(RetVal, CurIdx);
        RtpUtils.SetDWord(RetVal, SsrcIdx, m_Ssrc);
        CurIdx = RtcpHeader.HeaderLength + SSRC_LENGTH;

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
    /// beginning at the StartIdx position. </param>
    /// <param name="StartIdx">Index in Dest to load the bytes into.</param>
    public void LoadBytes(byte[] Dest, Int32 StartIdx)
    {
        byte[] PcktBytes = ToByteArray();
        Array.ConstrainedCopy(PcktBytes, 0, Dest, StartIdx, PcktBytes.Length);
    }
}
