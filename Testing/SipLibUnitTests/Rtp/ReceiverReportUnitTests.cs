/////////////////////////////////////////////////////////////////////////////////////
//  File:   ReceiverReportUnitTests.cs                              28 Sep 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

using SipLib.Rtp;

namespace SipLibUnitTests;

[Trait("Category", "unit")]
public class ReceiverReportUnitTests
{
    [Fact]
    public void ReceiverReportParsing1()
    {
        ReceiverReport Rr1 = BuildReceiverReport();
        byte[] Rr1Bytes = Rr1.ToByteArray();
        ReceiverReport Rr2 = ReceiverReport.Parse(Rr1Bytes, 0);
        Assert.True(Rr2 != null, "Rr2 is null");
        Assert.True(CompareReceiverReports(Rr2, Rr1) == true, "CompareReceiverReports returned false");
    }

    public static bool CompareReceiverReports(ReceiverReport Rr2, ReceiverReport Rr1)
    {
        bool Success = true;
        Assert.True(Rr2.Header.Count == Rr1.Header.Count, "Header.Count mismatch");
        Assert.True(Rr2.Header.PacketType == Rr1.Header.PacketType, "PacketType mismatch");
        Assert.True(Rr2.Header.Length == Rr1.Header.Length, "Header.Length mismatch");

        List<ReportBlock> Rbs1 = Rr1.GetReportBlocks;
        List<ReportBlock> Rbs2 = Rr2.GetReportBlocks;
        Assert.True(Rbs2.Count == Rbs1.Count, "ReportBlock count mismatch");

        int i;
        for (i = 0; i < Rbs1.Count; i++)
        {
            Assert.True(Rbs2[i].SSRC == Rbs1[i].SSRC, $"SSRC mismatch at {i}");
            Assert.True(Rbs2[i].FractionLost == Rbs1[i].FractionLost, $"FractionLost mismatch at {i}");
            Assert.True(Rbs2[i].CumulativePacketsLost == Rbs1[i].CumulativePacketsLost,
                $"CumulativePacketsLost mismatch at {i}");
            Assert.True(Rbs2[i].HighestSequenceNumberReceived == Rbs1[i].HighestSequenceNumberReceived,
                $"HighestSequenceNumberReceived mismatch at {i}");
            Assert.True(Rbs2[i].InterarrivalJitter == Rbs1[i].InterarrivalJitter,
                $"InterarrivalJitter at {i}");
            Assert.True(Rbs2[i].LastSR == Rbs1[i].LastSR, $"LastSR mismatch at {i}");
            Assert.True(Rbs2[i].Dlsr == Rbs1[i].Dlsr, $"Dlsr mismatch at {i}");
        }

        return Success;
    }

    public static ReceiverReport BuildReceiverReport()
    {
        ReceiverReport Rr1 = new ReceiverReport();
        Rr1.SSRC = 12345;
        ReportBlock Rb1 = new ReportBlock();
        Rb1.SSRC = 54321;
        Rb1.FractionLost = 10;
        Rb1.CumulativePacketsLost = 5;
        Rb1.HighestSequenceNumberReceived = 8888;
        Rb1.InterarrivalJitter = 44;
        Rb1.LastSR = 1;
        Rb1.Dlsr = 5000;
        Rr1.AddReportBlock(Rb1);

        ReportBlock Rb2 = new ReportBlock();
        Rb2.SSRC = 123456;
        Rb2.FractionLost = 5;
        Rb2.CumulativePacketsLost = 10;
        Rb2.HighestSequenceNumberReceived = 1111;
        Rb2.InterarrivalJitter = 11;
        Rb2.LastSR = 2;
        Rb2.Dlsr = 6000;
        Rr1.AddReportBlock(Rb2);

        return Rr1;
    }

}
