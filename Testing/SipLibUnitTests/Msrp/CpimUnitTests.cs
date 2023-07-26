/////////////////////////////////////////////////////////////////////////////////////
//  File:   CpimUnitTests.cs                                        21 Jul 23 PHR
/////////////////////////////////////////////////////////////////////////////////////


using System.Text;

namespace SipLibUnitTests;
using SipLib.Core;
using SipLib.Msrp;

[Trait("Category", "unit")]
public class CpimUnitTests
{
    private const string CRLF = "\r\n";

    // See Section 5.1 of RFC 3862. The Content-Type was changed to text/plain.
    private string CpmMessage5_1 =
        "From: MR SANDERS <im:piglet@100akerwood.com>" + CRLF +
        "To: Depressed Donkey <im:eeyore@100akerwood.com>" + CRLF +
        "DateTime: 2000-12-13T13:40:00-08:00" + CRLF +
        "Subject: the weather will be fine today" + CRLF +
        "Subject:;lang=fr beau temps prevu pour aujourd’hui" + CRLF +
        "NS: MyFeatures <mid:MessageFeatures@id.foo.com>" + CRLF +
        "Require: MyFeatures.VitalMessageOption" + CRLF +
        "MyFeatures.VitalMessageOption: Confirmation-requested" + CRLF +
        "MyFeatures.WackyMessageOption: Use-silly-font" + CRLF + CRLF +
        "Content-Type: text/plain; charset=utf-8" + CRLF +
        "Content-ID: <1234567890@foo.com>" + CRLF + CRLF +
        "Here is the text of my message.";

    [Fact]
    public void TestCpimMessage5_1()
    {
        byte[] MsgBytes = Encoding.UTF8.GetBytes(CpmMessage5_1);
        CpimMessage cpimMessage = CpimMessage.ParseCpimBytes(MsgBytes);

        // Spot check some of the fields
        Assert.True(cpimMessage != null, "Failed to parse CpmMessage5_1");
        Assert.True(cpimMessage.From.Name == "MR SANDERS", "The From name is incorrect");
        Assert.True(cpimMessage.From.URI.ToString() == "im:piglet@100akerwood.com",
            "The From URI is incorrect");
        Assert.True(cpimMessage.Subject[0] == "the weather will be fine today", 
            "The first Subject header is incorrect");
        Assert.True(cpimMessage.Subject[1] == ";lang=fr beau temps prevu pour aujourd’hui",
            "The second Subject header is incorrect");
        Assert.True(cpimMessage.NS[0] == "MyFeatures <mid:MessageFeatures@id.foo.com>",
            "The NS header is incorrect");
        Assert.True(cpimMessage.NonStandardHeaders[0] == "MyFeatures.VitalMessageOption: Confirmation-requested",
            "The first Non-Standard Header is incorrect");
        Assert.True(cpimMessage.NonStandardHeaders[1] == "MyFeatures.WackyMessageOption: Use-silly-font",
            "The second Non-Standard Header is incorrect");
        Assert.True(cpimMessage.ContentType == "text/plain; charset=utf-8", "The Content-Type is incorrect");
        Assert.True(cpimMessage.ContentID == "<1234567890@foo.com>", "The Content-ID is incorrect");
        Assert.True(cpimMessage.Body == "Here is the text of my message.", "The Body is incorrect");

        string strMsg = cpimMessage.ToString();
        Console.WriteLine(strMsg);
    }

    private string CpmMessage5_1NoBody =
        "From: MR SANDERS <im:piglet@100akerwood.com>" + CRLF +
        "To: Depressed Donkey <im:eeyore@100akerwood.com>" + CRLF +
        "DateTime: 2000-12-13T13:40:00-08:00" + CRLF +
        "Subject: the weather will be fine today" + CRLF +
        "Subject:;lang=fr beau temps prevu pour aujourd’hui" + CRLF +
        "NS: MyFeatures <mid:MessageFeatures@id.foo.com>" + CRLF +
        "Require: MyFeatures.VitalMessageOption" + CRLF +
        "MyFeatures.VitalMessageOption: Confirmation-requested" + CRLF +
        "MyFeatures.WackyMessageOption: Use-silly-font" + CRLF + CRLF +
        "Content-Type: text/plain; charset=utf-8" + CRLF +
        "Content-ID: <1234567890@foo.com>" + CRLF + CRLF;
 

    [Fact]
    public void TestCpimMessage5_1NoBody()
    {
        byte[] MsgBytes = Encoding.UTF8.GetBytes(CpmMessage5_1NoBody);
        CpimMessage cpimMessage = CpimMessage.ParseCpimBytes(MsgBytes);
        Assert.NotNull(cpimMessage);

        Assert.True(string.IsNullOrEmpty(cpimMessage.Body) == true, "The Body is not null or empty");
        Assert.True(cpimMessage.From != null, "The From header is null");
        Assert.True(cpimMessage.ContentType == "text/plain; charset=utf-8", "The Content-Type is wrong");
       
    }
}
