/////////////////////////////////////////////////////////////////////////////////////
//  File:   CpimUnitTests.cs                                        21 Jul 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

using System.Text;

namespace SipLibUnitTests.Core;
using SipLib.Msrp;

[Trait("Category", "unit")]
public class CpimUnitTests
{
    private const string CRLF = "\r\n";

    // See Section 5.1 of RFC 3862. The Content-Type was changed to text/plain.
    private string CpimMessage5_1 =
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
        byte[] MsgBytes = Encoding.UTF8.GetBytes(CpimMessage5_1);
        CpimMessage cpimMessage = CpimMessage.ParseCpimBytes(MsgBytes);
        ValidateCpimMessage5_1(cpimMessage);
    }

    [Fact]
    public void TestCpimMessage5_1_ToByteArray()
    {
        byte[] MsgBytes = Encoding.UTF8.GetBytes(CpimMessage5_1);
        CpimMessage cpimMessage1 = CpimMessage.ParseCpimBytes(MsgBytes);

        byte[] cpimBytes = cpimMessage1.ToByteArray();
        CpimMessage cpimMessage2 = CpimMessage.ParseCpimBytes(cpimBytes);
        ValidateCpimMessage5_1(cpimMessage2);
    }

    private void ValidateCpimMessage5_1(CpimMessage cpimMessage)
    {
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

        string strBody = Encoding.UTF8.GetString(cpimMessage.Body);
        Assert.True(strBody == "Here is the text of my message.", "The Body is incorrect");
    }

    [Fact]
    public void TestCpimMessage5_1_ImageJpeg()
    {
        byte[] MsgBytes = Encoding.UTF8.GetBytes(CpimMessage5_1);
        CpimMessage cpimMessage1 = CpimMessage.ParseCpimBytes(MsgBytes);
        Assert.NotNull(cpimMessage1);

        byte[] CarCrashBytes = GetTestFile("CarCrashPicture.jpg");
        cpimMessage1.ContentType = "image/jpeg";
        cpimMessage1.ContentID = null;
        cpimMessage1.Body = CarCrashBytes;
        cpimMessage1.Subject = new List<string>();
        cpimMessage1.Subject.Add("Here is a picture of my car crash");

        byte[] cpimMessage2Bytes = cpimMessage1.ToByteArray();
        CpimMessage cpimMessage2 = CpimMessage.ParseCpimBytes(cpimMessage2Bytes);
        Assert.NotNull(cpimMessage2);

        Assert.True(cpimMessage2.ContentType == "image/jpeg", "The ContentType is wrong");
        Assert.True(cpimMessage2.Body.Length == CarCrashBytes.Length, "The Body length is wrong");
        bool BodyMatches = true;
        for (int i = 0; i < CarCrashBytes.Length; i++)
        {
            if (CarCrashBytes[i] != cpimMessage2.Body[i])
            {
                BodyMatches = false;
                break;
            }
        }

        Assert.True(BodyMatches == true, "The message Body does not match");

        Assert.True(cpimMessage2.Subject[0] == "Here is a picture of my car crash", "The Subject does not match");
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
        Assert.True(cpimMessage.Body == null, "The Body is not null");
        Assert.True(cpimMessage.From != null, "The From header is null");
        Assert.True(cpimMessage.ContentType == "text/plain; charset=utf-8", "The Content-Type is wrong");
    }

    /// <summary>
    /// Specifies the path to the files containing the test SIP messages. Change this if the project
    /// location or the location of the test files change.
    /// </summary>
    private const string Path = @"..\..\..\MsrpMessages\";

    private byte[] GetTestFile(string FileName)
    {
        byte[] FileBytes = null;
        string FilePath = $"{Path}{FileName}";
        Assert.True(File.Exists(FilePath), $"The {FileName} test input file was missing.");
        FileBytes = File.ReadAllBytes(FilePath);
        return FileBytes;
    }
}
