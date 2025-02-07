/////////////////////////////////////////////////////////////////////////////////////
//  File:   SIPFragUnitTests.cs                                     4 Feb 25 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLibUnitTests.Core;
using SipLib.Core;

/// <summary>
/// Unit tests for the SIPFrag class.
/// </summary>
[Trait("Category", "unit")]
public class SIPFragUnitTests
{
    public SIPFragUnitTests(Xunit.Abstractions.ITestOutputHelper output)
    {
    }

    [Fact]
    public void NormalParseCase1()
    {
        SIPFrag sipFrag = SIPFrag.ParseSIPFrag("SIP/2.0 200 Ok");
        Assert.True(sipFrag.Status == SIPResponseStatusCodesEnum.Ok);
        Assert.True(sipFrag.ReasonPhrase == "Ok");
    }

    [Fact]
    public void NormalParseCaseWithCRLF()
    {
        SIPFrag sipFrag = SIPFrag.ParseSIPFrag("SIP/2.0 200 Ok\r\n");
        Assert.True(sipFrag.Status == SIPResponseStatusCodesEnum.Ok);
        Assert.True(sipFrag.ReasonPhrase == "Ok");
    }

    [Fact]
    public void ExtraSpacesParseCase1()
    {
        SIPFrag sipFrag = SIPFrag.ParseSIPFrag("SIP/2.0   200   Ok");
        Assert.True(sipFrag.Status == SIPResponseStatusCodesEnum.Ok);
        Assert.True(sipFrag.ReasonPhrase == "Ok");
    }

    [Fact]
    public void NoReasonPhrase1()
    {
        SIPFrag sipFrag = SIPFrag.ParseSIPFrag("SIP/2.0 200");
        Assert.True(sipFrag.Status == SIPResponseStatusCodesEnum.Ok);
        Assert.True(string.IsNullOrEmpty(sipFrag.ReasonPhrase) == true);
    }

    [Fact]
    public void NoStatusCode1()
    {
        SIPFrag sipFrag = SIPFrag.ParseSIPFrag("SIP/2.0");
        Assert.True(sipFrag.Status == SIPResponseStatusCodesEnum.None);
        Assert.True(sipFrag.ReasonPhrase == "Unknown");
    }

    [Fact]
    public void ParseEmptyString()
    {
        SIPFrag sipFrag = SIPFrag.ParseSIPFrag("");
        Assert.True(sipFrag.Status == SIPResponseStatusCodesEnum.None);
        Assert.True(sipFrag.ReasonPhrase == "Unknown");
    }

    [Fact]
    public void ParseUnknownStatus()
    {
        SIPFrag sipFrag = SIPFrag.ParseSIPFrag("SIP/2.0 900 Ok\r\n");
        int StatusCode = (int) sipFrag.Status;
        Assert.True(StatusCode == 900);
    }

    [Fact]
    public void ToStringNormal1()
    {
        SIPFrag sipFrag = new SIPFrag(SIPResponseStatusCodesEnum.Ok, "Ok");
        string strSipFrag = sipFrag.ToString();
        Assert.True(strSipFrag == "SIP/2.0 200 Ok");
    }

    [Fact]
    public void TestToStringError()
    {
        SIPFrag sipFrag = new SIPFrag(SIPResponseStatusCodesEnum.None, "Unknown");
        Assert.Throws<ArgumentException>(() => sipFrag.ToString());
    }

}
