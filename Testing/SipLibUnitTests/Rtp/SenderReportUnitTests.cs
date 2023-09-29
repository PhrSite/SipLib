/////////////////////////////////////////////////////////////////////////////////////
//  File:   SenderReportUnitTests.cs                                29 Sep 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLibUnitTests;

using SipLib.Rtp;

[Trait("Category", "unit")]
public class SenderReportUnitTests
{
    [Fact]
    public void SenderReportParsing1()
    {
        SenderReport Sr1 = BuildSenderReport();
        byte[] Sr1Bytes = Sr1.ToByteArray();
        SenderReport Sr2 = SenderReport.Parse(Sr1Bytes);
        Assert.True(Sr2 != null, "Sr2 is null");
        Assert.True(CompareSenderReports(Sr1, Sr2), "SenderReport mismatch");
    }

    public static SenderReport BuildSenderReport()
    {
        SenderReport Sr1 = new SenderReport();
        Sr1.SSRC = 0x5a5a5a5a;
        DateTime NtpDt = DateTime.Now;
        Sr1.SenderInfo.NTP = NtpDt;
        Sr1.SenderInfo.RtpTimestamp = 0x12345678;
        Sr1.SenderInfo.SenderPacketCount = 1234;
        Sr1.SenderInfo.SenderOctetCount = Sr1.SenderInfo.SenderPacketCount *
            172;

        ReportBlock Rb1 = new ReportBlock();
        Rb1.SSRC = 54321;
        Rb1.FractionLost = 10;
        Rb1.CumulativePacketsLost = 5;
        Rb1.HighestSequenceNumberReceived = 8888;
        Rb1.InterarrivalJitter = 44;
        Rb1.LastSR = 1;
        Rb1.Dlsr = 5000;
        Sr1.AddReportBlock(Rb1);

        ReportBlock Rb2 = new ReportBlock();
        Rb2.SSRC = 123456;
        Rb2.FractionLost = 5;
        Rb2.CumulativePacketsLost = 10;
        Rb2.HighestSequenceNumberReceived = 1111;
        Rb2.InterarrivalJitter = 11;
        Rb2.LastSR = 2;
        Rb2.Dlsr = 6000;
        Sr1.AddReportBlock(Rb2);

        return Sr1;
    }

    public static bool CompareSenderReports(SenderReport Sr2, SenderReport Sr1)
    {
        bool Success = true;
        Assert.True(Sr2.SSRC == Sr1.SSRC, "SSRC mismatch");

        TimeSpan NtpDiffTs = Sr2.SenderInfo.NTP - Sr1.SenderInfo.NTP;
        Assert.True(NtpDiffTs.TotalMilliseconds < 10, "NTP mismatch");
        Assert.True(Sr2.SenderInfo.RtpTimestamp == Sr1.SenderInfo.RtpTimestamp, "RtpTimestamp mismatch");
        Assert.True(Sr2.SenderInfo.SenderOctetCount == Sr1.SenderInfo.SenderOctetCount,
            "SenderOctetCount mismatch");
        Assert.True(Sr2.SenderInfo.SenderPacketCount == Sr1.SenderInfo.SenderPacketCount);
        Assert.True(Sr2.Header.Count == Sr1.Header.Count, "Header.Count mismatch");
        Assert.True(Sr2.Header.PacketType == Sr1.Header.PacketType, "PacketTypeMismatch");
        Assert.True(Sr2.Header.Length == Sr1.Header.Length, "Header.Length mismatch");
        List<ReportBlock> Rbs1 = Sr1.GetReportBlocks;
        List<ReportBlock> Rbs2 = Sr2.GetReportBlocks;
        Assert.True(Rbs1.Count == Rbs2.Count, "ReportBlocks.Count mismatch");

        int i;
        for (i = 0; i < Rbs1.Count; i++)
        {
            Assert.True(Rbs2[i].SSRC == Rbs1[i].SSRC, $"Rbs2[{i}].SSRC mismatch");
            Assert.True(Rbs2[i].FractionLost == Rbs1[i].FractionLost, $"Rbs2[{i}] FractionLost mismatch");
            Assert.True(Rbs2[i].CumulativePacketsLost == Rbs1[i].CumulativePacketsLost,
                $"Rbs2[{i}].CumulativePacketsLost mismatch");
            Assert.True(Rbs2[i].HighestSequenceNumberReceived == Rbs1[i].HighestSequenceNumberReceived,
                $"Rbs2[{i}].HighestSequenceNumberReceived mismatch");
            Assert.True(Rbs2[i].InterarrivalJitter == Rbs1[i].InterarrivalJitter,
                $"Rbs2[{i}].InterarrivalJitter mismatch");
            Assert.True(Rbs2[i].LastSR == Rbs1[i].LastSR, $"Rbs2[{i}].LastSR mismatch");
            Assert.True(Rbs2[i].Dlsr == Rbs1[i].Dlsr, $"Rbs3[{i}].Dlsr mismatch");
        }

        return Success;
    }
}
