/////////////////////////////////////////////////////////////////////////////////////
//  File:   DtmfPacketUnitTests.cs                                  5 Jan 24 PHR
/////////////////////////////////////////////////////////////////////////////////////

using SipLib.Rtp;

namespace SipLibUnitTests.Rtp;

[Trait("Category", "unit")]
public class DtmfPacketUnitTests
{
    [Fact]
    public void TestVolumeSetting()
    {
        DtmfPacket dtmfPacket = new DtmfPacket();
        dtmfPacket.Volume = -20;
        Assert.True(dtmfPacket.Volume == -20, "The Volume is incorrect");
    }

    [Fact]
    public void TestVolumeMinAndMax()
    {
        DtmfPacket dtmfPacket = new DtmfPacket();
        dtmfPacket.Volume = 10;
        Assert.True(dtmfPacket.Volume == 0, "The maximum Volume is incorrect");

        dtmfPacket.Volume = -100;
        Assert.True(dtmfPacket.Volume == -63, "The minimum Volume is incorrect");
    }

    [Fact]
    public void TestEflagAndVolume()
    {
        DtmfPacket dtmfPacket = new DtmfPacket();
        dtmfPacket.Eflag = true;
        Assert.True(dtmfPacket.Eflag == true, "The Eflag setting is wrong");

        dtmfPacket.Volume = -20;
        Assert.True(dtmfPacket.Volume == -20, "The Volume is wrong");
        Assert.True(dtmfPacket.Eflag == true, "The E flag is wrong after setting the volume");

        dtmfPacket.Eflag = false;
        Assert.True(dtmfPacket.Eflag == false, "The E flag is not false");

        Assert.True(dtmfPacket.Volume == -20, "The Volume is wrong after clearing the E flag");
    }

    [Fact]
    public void TestDuration()
    {
        DtmfPacket dtmfPacket = new DtmfPacket();
        dtmfPacket.Duration = 160;
        Assert.True(dtmfPacket.Duration == 160, "The Duration is wrong");
    }
}
