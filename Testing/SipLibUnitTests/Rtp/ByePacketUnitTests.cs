/////////////////////////////////////////////////////////////////////////////////////
//  File:   ByePacketUnitTests.cs                                   28 Sep 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLibUnitTests;
using SipLib.Rtp;

[Trait("Category", "unit")]
public class ByePacketUnitTests
{
    [Fact]
    public void ByePacketParsing1()
    {
        ByePacket Bp1 = BuildByePacket();
        byte[] Bp1Bytes = Bp1.ToByteArray();
        ByePacket Bp2 = ByePacket.Parse(Bp1Bytes, 0);
        Assert.True(Bp2 != null, "Bp2 is null");
        Assert.True(CompareByePackets(Bp2, Bp1) == true, "ByePackets do not match");
    }

    public static ByePacket BuildByePacket()
    {
        List<uint> Ssrcs = new List<uint>();
        Ssrcs.Add(1234567);
        ByePacket Bp1 = new ByePacket(Ssrcs, "Caller disconnect");
        return Bp1;
    }

    public static bool CompareByePackets(ByePacket Bp2, ByePacket Bp1)
    {
        bool Success = true;
        Assert.True(Bp2.Header.PacketType == Bp1.Header.PacketType, "PacketType mismatch");
        Assert.True(Bp2.Header.Count == Bp1.Header.Count, "Header.Count mismatch");
        Assert.True(Bp2.SSRCs.Count == Bp1.SSRCs.Count, "SSRCs.Count mismatch");

        for (int i = 0; i < Bp2.SSRCs.Count; i++)
        {
            Assert.True(Bp2.SSRCs[i] == Bp1.SSRCs[i], $"SSRCs[{i}] mismatch");
        }

        Assert.True(Bp2.Reason == Bp1.Reason, "Reason mismatch");

        return Success;
    }


}
