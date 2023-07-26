/////////////////////////////////////////////////////////////////////////////////////
//  File:   MsrpStatusHeaderUnitTests.cs                            24 Jul 24 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLibUnitTests;
using SipLib.Msrp;

[Trait("Category", "unit")]
public class MsrpStatusHeaderUnitTests
{
    [Fact]
    public void BasicParsing()
    {
        string strStatus = "000 200 OK";
        MsrpStatusHeader header = MsrpStatusHeader.ParseStatusHeader(strStatus);
        Assert.NotNull(header);
        Assert.True(header.Namespace == "000", "The Namespace is wrong");
        Assert.True(header.StatusCode == 200, "The StatusCode is wrong");
        Assert.True(header.Comment == "OK", "The Comment is wrong");
    }

    [Fact]
    public void MultiWordComment()
    {
        string strStatus = "000 400 Bad Request";
        MsrpStatusHeader header = MsrpStatusHeader.ParseStatusHeader(strStatus);
        Assert.NotNull(header);
        Assert.True(header.Namespace == "000", "The Namespace is wrong");
        Assert.True(header.StatusCode == 400, "The StatusCode is wrong");
        Assert.True(header.Comment == "Bad Request");
    }

    [Fact]
    public void InvalidStatusCode()
    {
        string strStatus = "000 NonInteger OK";
        MsrpStatusHeader header = MsrpStatusHeader.ParseStatusHeader(strStatus);
        Assert.Null(header);
    }

    [Fact]
    public void InvalidFieldCount()
    {
        string strStatus = "000";
        MsrpStatusHeader header = MsrpStatusHeader.ParseStatusHeader(strStatus);
        Assert.Null(header);
    }

    [Fact]
    public void ToStringTest()
    {
        MsrpStatusHeader header = new MsrpStatusHeader() 
        {
            StatusCode = 400,
            Namespace = "000",
            Comment = "Bad Request"
        };

        string strStatus = "000 400 Bad Request";
        Assert.True(header.ToString() == strStatus);
    }
}
