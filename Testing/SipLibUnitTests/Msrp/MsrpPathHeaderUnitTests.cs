/////////////////////////////////////////////////////////////////////////////////////
//  File:   MsrpPathHeaderUnitTests.cs                              24 Jul 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLibUnitTests;
using SipLib.Msrp;
using SipLib.Core;

[Trait("Category", "unit")]
public class MsrpPathHeaderUnitTests
{
    [Fact]
    public void BasicSinglePathParsing()
    {
        string MsrpPathHdr = "msrp://8185553333@192.168.1.79:4321/abcd;tcp";
        MsrpPathHeader pathHeader = MsrpPathHeader.ParseMsrpPathHeader(MsrpPathHdr);
        Assert.NotNull(pathHeader);
        Assert.True(pathHeader.MsrpUris.Count == 1, "The number of MsrpUris is wrong");
        Assert.True(pathHeader.MsrpUris[0].uri.User == "8185553333", "The User is wrong");
    }

    [Fact]
    public void BasicMultiplePathParsing()
    {
        string MsrpPathHdr = "msrp://8185553333@192.168.1.79:4321/abcd;tcp " +
            "msrp://8185554444@192.168.1.79:4321/abcd;tcp";
        MsrpPathHeader pathHeader = MsrpPathHeader.ParseMsrpPathHeader(MsrpPathHdr);
        Assert.NotNull(pathHeader);
        Assert.True(pathHeader.MsrpUris.Count == 2, "The number of MsrpUris is wrong");
        Assert.True(pathHeader.MsrpUris[0].uri.User == "8185553333", "The first User is wrong");
        Assert.True(pathHeader.MsrpUris[1].uri.User == "8185554444", "The second User is wrong");
    }

    [Fact]
    public void InvalidMsrpUriPath()
    {
        string MsrpPathHdr = "msrp://8185553333@192.168.1.79:4321/abcd;tcp " +
            "msrp://8185554444@192.168.1.79:4322";
        MsrpPathHeader pathHeader = MsrpPathHeader.ParseMsrpPathHeader(MsrpPathHdr);
        Assert.Null(pathHeader);
    }

    [Fact]
    public void BasicToString()
    {
        MsrpUri uri1 = new MsrpUri();
        uri1.uri = new SIPURI("8185553333", "192.168.1.79:4321", null, SIPSchemesEnum.msrp, 
            SIPProtocolsEnum.tcp);
        uri1.Transport = "tcp";
        uri1.SessionID = "abcd";

        MsrpUri uri2 = new MsrpUri();
        uri2.uri = new SIPURI("8185554444", "192.168.1.79:4321", null, SIPSchemesEnum.msrp,
            SIPProtocolsEnum.tcp);
        uri2.Transport = "tcp";
        uri2.SessionID = "abcd";

        MsrpPathHeader Mph = new MsrpPathHeader();
        Mph.MsrpUris.Add(uri1);
        Mph.MsrpUris.Add(uri2);

        string MsrpPathHdr = "msrp://8185553333@192.168.1.79:4321/abcd;tcp " +
            "msrp://8185554444@192.168.1.79:4321/abcd;tcp";
        string strMph = Mph.ToString();
        Assert.True(strMph == MsrpPathHdr, "ToString() failed");
    }
}
