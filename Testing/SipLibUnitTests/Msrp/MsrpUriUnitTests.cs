/////////////////////////////////////////////////////////////////////////////////////
//  File:   MsrpUriUnitTests.cs                                     21 Jul 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLibUnitTests.Msrp;
using SipLib.Core;
using SipLib.Msrp;

[Trait("Category", "unit")]
public class MsrpUriUnitTests
{

    [Fact]
    public void TestBasicParsing()
    {
        string strMsrpUri = "msrp://8185553333@192.168.1.79:4321/abcd;tcp";
        MsrpUri msrpUri = MsrpUri.ParseMsrpUri(strMsrpUri);
        Assert.NotNull(msrpUri);

        Assert.True(msrpUri.uri.Scheme == SIPSchemesEnum.msrp, "The URI scheme is wrong");
        Assert.True(msrpUri.uri.User == "8185553333", "The URI user part is wrong");
        Assert.True(msrpUri.uri.HostAddress == "192.168.1.79", "The HostAddress is wrong");
        Assert.True(msrpUri.uri.HostPort == "4321", "The HostPort is wrong");
        Assert.True(msrpUri.SessionID == "abcd", "The SessionID is wrong");
        Assert.True(msrpUri.Transport == "tcp", "The Transport is wrong");
    }

    [Fact]
    public void TestIPv6Parsing()
    {
        string strMsrpUri = "msrps://8185553333@[2600:1700:7a40:4740:35b5:3256:a4d9:e7e1]:4321/abcd;tls";
        MsrpUri msrpUri = MsrpUri.ParseMsrpUri(strMsrpUri);
        Assert.NotNull(msrpUri);

        Assert.True(msrpUri.uri.Scheme == SIPSchemesEnum.msrps, "The URI scheme is wrong");
        Assert.True(msrpUri.uri.User == "8185553333", "The URI user part is wrong");
        Assert.True(msrpUri.uri.HostAddress == "[2600:1700:7a40:4740:35b5:3256:a4d9:e7e1]", "The HostAddress is wrong");
        Assert.True(msrpUri.uri.HostPort == "4321", "The HostPort is wrong");
        Assert.True(msrpUri.SessionID == "abcd", "The SessionID is wrong");
        Assert.True(msrpUri.Transport == "tls", "The Transport is wrong");

        string strMsrpUri1 = msrpUri.ToString();
        Assert.True(strMsrpUri1 == strMsrpUri, "MsrpUri.ToString() failed");
    }

    [Fact]
    public void TestFqdnUri()
    {
        string strMsrpUri = "msrps://8185553333@abc.com/abcd;tls";
        MsrpUri msrpUri = MsrpUri.ParseMsrpUri(strMsrpUri);
        Assert.NotNull(msrpUri);
        Assert.True(msrpUri.uri.Scheme == SIPSchemesEnum.msrps, "The URI scheme is wrong");
        Assert.True(msrpUri.uri.User == "8185553333", "The URI user part is wrong");
        Assert.True(msrpUri.uri.HostAddress == "abc.com", "The HostAddress is wrong");
        Assert.True(msrpUri.Transport == "tls", "The Transport is wrong");
    }

    [Fact]
    public void TestToIPv6SIPEndPoint()
    {
        string strMsrpUri = "msrps://8185553333@[2600:1700:7a40:4740:35b5:3256:a4d9:e7e1]:4321/abcd;tls";
        MsrpUri msrpUri = MsrpUri.ParseMsrpUri(strMsrpUri);
        Assert.NotNull(msrpUri);

        SIPEndPoint sipEndPoint = msrpUri.uri.ToSIPEndPoint();
        Assert.NotNull(sipEndPoint);
        Assert.True(sipEndPoint.GetIPEndPoint().Address.ToString() ==
            "2600:1700:7a40:4740:35b5:3256:a4d9:e7e1", "The IP address is wrong");
    }
}
