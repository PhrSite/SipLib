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
        Assert.True(Sa.Params.Values.Count == 1, "The number of paraeters is incorrect");
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
}
