/////////////////////////////////////////////////////////////////////////////////////
//  File:   RttParametersUnitTests.cs                               16 Oct 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

using SipLib.RealTimeText;
using SipLib.Sdp;
namespace SipLibUnitTests.RealTimeText;

[Trait("Category", "unit")]
public class RttParametersUnitTests
{
    private const string CRLF = "\r\n";

    [Fact]
    public void ParseNoRedundancy()
    {
        string strRttSdp =
            "v=0" + CRLF +
            "o=jdoe 2890844526 1 IN IP4 10.47.16.5" + CRLF +
            "s=SDP Seminar" + CRLF +
            "i=Session Information" + CRLF +
            "c=IN IP4 224.2.17.12/127" + CRLF +
            "t=2873397496 2873404696" + CRLF +
            "m=text 11000 RTP/AVP 98" + CRLF +
            "a=rtpmap:98 t140/1000";

        Sdp RttSdp = Sdp.ParseSDP(strRttSdp);
        Assert.True(RttSdp != null);
        Assert.True(RttSdp.Media.Count == 1, "Media.Count is wrong");

        RttParameters rttParameters = RttParameters.FromMediaDescription(RttSdp.Media[0]);
        Assert.NotNull(rttParameters);
        Assert.True(rttParameters.RedundancyLevel == 0, "RedundancyLevel is wrong");
        Assert.True(rttParameters.T140PayloadType == 98, "T140PayloadType is wrong");
    }

    [Fact]
    public void ParseRedundancy2Levels()
    {
        string strRttSdp =
            "v=0" + CRLF +
            "o=jdoe 2890844526 1 IN IP4 10.47.16.5" + CRLF +
            "s=SDP Seminar" + CRLF +
            "i=Session Information" + CRLF +
            "c=IN IP4 224.2.17.12/127" + CRLF +
            "t=2873397496 2873404696" + CRLF +
            "m=text 11000 RTP/AVP 98 99" + CRLF +
            "a=rtpmap:98 t140/1000" + CRLF +
            "a=rtpmap:99 red/1000" + CRLF +
            "a=fmtp:99 98/98/98" + CRLF +
            "a=fmtp:98 cps=30" + CRLF +
            "a=rtt-mixer";

        Sdp RttSdp = Sdp.ParseSDP(strRttSdp);
        Assert.True(RttSdp != null);
        Assert.True(RttSdp.Media.Count == 1, "Media.Count is wrong");

        RttParameters rttParameters = RttParameters.FromMediaDescription(RttSdp.Media[0]);
        Assert.NotNull(rttParameters);
        Assert.True(rttParameters.RedundancyLevel == 2, "RedundancyLevel is wrong");
        Assert.True(rttParameters.T140PayloadType == 98, "T140PayloadType is wrong");
        Assert.True(rttParameters.RedundancyPayloadType == 99, "RedundancyPayloadType is wrong");
        Assert.True(rttParameters.RttMixerAware == true, "RttMixerAware is wrong");
        Assert.True(rttParameters.Cps == 30, "Cps is wrong");
    }

    [Fact]
    public void TestToMediaDescription()
    {
        RttParameters rttParams1 = new RttParameters();
        rttParams1.Cps = 30;
        MediaDescription rttMd = rttParams1.ToMediaDescription(5000);

        string strRttMd = rttMd.ToString();
        RttParameters rttParams2 = RttParameters.FromMediaDescription(rttMd);
        Assert.NotNull (rttParams2);

        Assert.True(rttParams1.RedundancyLevel == rttParams2.RedundancyLevel, "RedundancyLevel mismatch");
        Assert.True(rttParams1.T140PayloadType == rttParams2.T140PayloadType, "T140PayloadType mismatch");
        Assert.True(rttParams1.RedundancyPayloadType == rttParams2.RedundancyPayloadType, 
            "RedundancyPayloadType mismatch");
        Assert.True(rttParams1.Cps == rttParams2.Cps, "Cps mismatch");
        Assert.True(rttParams1.RttMixerAware == rttParams2.RttMixerAware, "RttMixerAware mismatch");
    }
}
