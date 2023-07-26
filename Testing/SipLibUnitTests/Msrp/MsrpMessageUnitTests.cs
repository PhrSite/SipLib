/////////////////////////////////////////////////////////////////////////////////////
//  File:   MsrpMessageUnitTests.cs                                 26 Jul 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLibUnitTests;

using SipLib.Msrp;
using System.IO;
using System.Text;

[Trait("Category", "unit")]
public class MsrpMessageUnitTests
{
    /// <summary>
    /// Specifies the path to the files containing the test SIP messages. Change this if the project
    /// location or the location of the test files change.
    /// </summary>
    private const string Path = @"\_MyProjects\SipLib\Testing\SipLibUnitTests\MsrpMessages\";

    [Fact]
    public void MsrpMessage1()
    {
        byte[] MsgBytes = GetTestFile("MsrpMessage1.txt");
        MsrpMessage msrpMessage = MsrpMessage.ParseMsrpMessage(MsgBytes, MsrpCompletionStatus.Complete);
        Assert.NotNull(msrpMessage);

        Assert.True(msrpMessage.TransactionID == "d93kswow", "The TransactionID is wroing");
        Assert.True(msrpMessage.MessageID == "12339sdqwer", "The MessageID is wrong");

        Assert.True(msrpMessage.Contents != null, "The Contents is null");
        string strContents = Encoding.UTF8.GetString(msrpMessage.Contents);
        Assert.True(strContents.Trim() == "Hi, I'm Alice!", "The Contents are wrong");
        
    }

    private byte[] GetTestFile(string FileName)
    {
        byte[] FileBytes = null;
        string FilePath = $"{Path}{FileName}";
        Assert.True(File.Exists(FilePath), $"The {FileName} test input file was missing.");
        FileBytes = File.ReadAllBytes(FilePath);
        return FileBytes;
    }

}
