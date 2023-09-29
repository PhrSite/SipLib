/////////////////////////////////////////////////////////////////////////////////////
//  File:   RtcpCompoundPacketUnitTests.cs                          29 Sep 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLibUnitTests;

using SipLib.Rtp;

[Trait("Category", "unit")]
public class RtcpCompoundPacketUnitTests
{

    [Fact]
    public void RtcpCompoundPacketParsing1()
    {
        RtcpCompoundPacket Rcp1 = BuildRtcpCompoundPacket();
        byte[] Rcp1Bytes = Rcp1.ToByteArray();
        RtcpCompoundPacket Rcp2 = RtcpCompoundPacket.Parse(Rcp1Bytes);
        Assert.True(Rcp2 != null, "Rcp2 is null");
        Assert.True(Rcp2.SenderReports.Count == 1, "Rcp2.SenderReports.Count is wrong");
        Assert.True(SenderReportUnitTests.CompareSenderReports(Rcp2.SenderReports[0], Rcp1.SenderReports[0]) ==
            true, "SenderReport mismatch");
        Assert.True(Rcp2.SdesPackets.Count == 1, "Rcp2.SdesPackets.Count is wrong");
        Assert.True(SdesPacketUnitTests.CompareSdesPackets(Rcp2.SdesPackets[0], Rcp1.SdesPackets[0]) == true,
            "SdesPacket mismatch");
        Assert.True(Rcp2.ByePackets.Count == 1, "Rcp2.ByePackets.Cound is wrong");
        Assert.True(ByePacketUnitTests.CompareByePackets(Rcp2.ByePackets[0], Rcp1.ByePackets[0]) == true,
            "ByePacket mismatch");
    }

    private RtcpCompoundPacket BuildRtcpCompoundPacket()
    {
        RtcpCompoundPacket Rcp = new RtcpCompoundPacket();
        Rcp.SenderReports.Add(SenderReportUnitTests.BuildSenderReport());
        Rcp.SdesPackets.Add(SdesPacketUnitTests.BuildSdesPacket());
        Rcp.ByePackets.Add(ByePacketUnitTests.BuildByePacket());
        return Rcp;
    }

    private bool CompareRtcpCompoundPackete(RtcpCompoundPacket Rcp1, RtcpCompoundPacket Rcp2)
    {
        bool Success = true;


        return Success;
    }
}
