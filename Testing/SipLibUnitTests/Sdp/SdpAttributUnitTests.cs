//////////////////////////////////////////////////////////////////////////////////////
//  File: SdpAttributeUnitTests.cs                                  20 Nov 22 PHR
//////////////////////////////////////////////////////////////////////////////////////

using SipLib;
using SipLib.Sdp;

namespace SipLibUnitTests.Sdp;

[Trait("Category", "unit")]
public class SdpAttributeUnitTests
{
    public SdpAttributeUnitTests(Xunit.Abstractions.ITestOutputHelper output)
    {
    }

    [Fact]
    public void TestBasicConstructor()
    {
        SdpAttribute Sa = new SdpAttribute("setup", "active");
        Assert.True(Sa.Attribute == "setup", "The attribute name is incorrect");
        Assert.True(Sa.Value == "active", "The attribute value is incorrect");
    }

    [Fact]
    public void TestBasicParsing()
    {
        SdpAttribute Sa = SdpAttribute.ParseSdpAttribute("recvonly");
        Assert.NotNull(Sa);
        Assert.True(Sa.Attribute == "recvonly", "The attribute name is incorrect");
        Assert.True(Sa.Value == "", "The attribute value is incorrect");
    }

    [Fact]
    public void TestParsingWithAttributeParameters()
    {
        SdpAttribute Sa = SdpAttribute.ParseSdpAttribute("rtpmap:99 h263-1998/90000");
        Assert.NotNull(Sa);
        Assert.True(Sa.Attribute == "rtpmap", "The attribute name is incorrect");
        Assert.True(Sa.Value == "99", "The attribute value is incorrect");
        Assert.True(Sa.Params.Values.Count == 1, "The number of parameters is incorrect");
        Assert.True(Sa.Params.ContainsKey("h263-1998/90000") == true, "The parameter name is incorrect");
        Assert.True(Sa.Params["h263-1998/90000"] == null, "The parameter value is incorrect");
    }

    [Fact]
    public void TestToStringWithParameters()
    {
        SdpAttribute Sa = SdpAttribute.ParseSdpAttribute("rtpmap:99 h263-1998/90000");
        string str = Sa.ToString();
        Assert.Equal("a=rtpmap:99 h263-1998/90000\r\n", str);
    }

    // 9 Nov 25 PHR
    [Fact]
    public void FmtpMultiParametersNoSpace()
    {
        SdpAttribute attribute = SdpAttribute.ParseSdpAttribute("fmtp:97 profile-level-id=42e01f;packetization-mode=1");
        Assert.NotNull(attribute);

        Assert.True(attribute.Attribute == "fmtp", "The attribute name is wrong");
        Assert.True(attribute.Params.Count == 2, "The parameter count is wrong");
        Assert.True(attribute.Params["profile-level-id"] == "42e01f", "The profile-level-id parameter is missing or wrong");
        Assert.True(attribute.Params["packetization-mode"] == "1", "The packetization-mode parameter is missing or wrong");
    }

    // 9 Nov 25 PHR
    [Fact]
    public void FmtpMultiParametersWithSpace()
    {
        SdpAttribute attribute = SdpAttribute.ParseSdpAttribute("fmtp:97 profile-level-id=42e01f; packetization-mode=1");
        Assert.NotNull(attribute);

        Assert.True(attribute.Attribute == "fmtp", "The attribute name is wrong");
        Assert.True(attribute.Params.Count == 2, "The parameter count is wrong");
        Assert.True(attribute.Params["profile-level-id"] == "42e01f", "The profile-level-id parameter is missing or wrong");
        Assert.True(attribute.Params["packetization-mode"] == "1", "The packetization-mode parameter is missing or wrong");
    }

    // 9 Nov 25 PHR
    [Fact]
    public void FmtpWithSingleParameter()
    {
        SdpAttribute attribute = SdpAttribute.ParseSdpAttribute("fmtp:97 profile-level-id=42e01f");
        Assert.NotNull(attribute);

        Assert.True(attribute.Attribute == "fmtp", "The attribute name is wrong");
        Assert.True(attribute.Params.Count == 1, "The parameter count is wrong");
        Assert.True(attribute.Params["profile-level-id"] == "42e01f", "The profile-level-id parameter is missing or wrong");
    }

    // 9 Nov 25 PHR
    [Fact]
    public void RtcpAttributeWithParameters()
    {
        SdpAttribute sdpAttribute = SdpAttribute.ParseSdpAttribute("rtcp:50015 IN IP4 192.168.1.64");
        Assert.NotNull(sdpAttribute);

        Assert.True(sdpAttribute.Attribute == "rtcp", "The attribute name is wrong");
        Assert.True(sdpAttribute.Value == "50015", "The Value is wrong");
        Assert.True(sdpAttribute.Params.Count == 3, "The parameter count is wrong");
        Assert.True(sdpAttribute.Params.ContainsKey("IN") == true, "The IN parameter is missing");
        Assert.True(sdpAttribute.Params.ContainsKey("IP4") == true, "The IP4 parameter is missing");
        Assert.True(sdpAttribute.Params.ContainsKey("192.168.1.64") == true, "The 192.168.1.64 parameter is missing");
    }

    // 9 Nov 26 PHR
    [Fact]
    public void SdpAttributeWithParametersInConstructor()
    {
        // Note: The constructor does not parse the parameters
        SdpAttribute sdpAttribute = new SdpAttribute("rtcp", "50015 IN IP4 192.168.1.64");
        Assert.True(sdpAttribute.Attribute == "rtcp", "The attribute name is wrong");
        Assert.True(sdpAttribute.Value == "50015 IN IP4 192.168.1.64", "The attribute value is wrong");
        Assert.True(sdpAttribute.Params.Count == 0, "The parameter count is wrong");
    }
}
