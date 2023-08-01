/////////////////////////////////////////////////////////////////////////////////////
//  File:   MsrpMessageUnitTests.cs                                 26 Jul 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLibUnitTests;

using SipLib.Msrp;
using System.IO;
using System.Text;
using Veds;

[Trait("Category", "unit")]
public class MsrpMessageUnitTests
{
    /// <summary>
    /// Specifies the path to the files containing the test SIP messages. Change this if the project
    /// location or the location of the test files change.
    /// </summary>
     private const string Path = @"..\..\..\MsrpMessages\";

    [Fact]
    public void MsrpRequestMessage1()
    {
        byte[] MsgBytes = GetTestFile("MsrpRequestMessage1.txt");
        MsrpMessage msrpMessage = MsrpMessage.ParseMsrpMessage(MsgBytes);
        Assert.NotNull(msrpMessage);

        Assert.True(msrpMessage.MessageType == MsrpMessageType.Request, "The MessageType is wrong");
        Assert.True(msrpMessage.CompletionStatus == MsrpCompletionStatus.Complete, "CompletionStatus is wrong");
        Assert.True(msrpMessage.RequestMethod == "SEND", "The RequestMethod is wrong");
        Assert.True(msrpMessage.ByteRange.Start == 1, "ByteRange.Start is wrong");
        Assert.True(msrpMessage.ByteRange.End == 16, "ByteRange.End is wrong");
        Assert.True(msrpMessage.ByteRange.Total == 16, "BYteRange.Total is wrong");
        Assert.True(msrpMessage.TransactionID == "d93kswow", "The TransactionID is wrong");
        Assert.True(msrpMessage.MessageID == "12339sdqwer", "The MessageID is wrong");
        Assert.True(msrpMessage.ContentType == "text/plain", "The ContentType is wrong");

        Assert.True(msrpMessage.Body != null, "The Body is null");
        string strBody = Encoding.UTF8.GetString(msrpMessage.Body);
        Assert.True(strBody.Trim() == "Hi, I'm Alice!", "The Body is wrong");
    }

    [Fact]
    public void MsrpRequestMessage1_ToByteArray()
    {
        byte[] MsgBytes = GetTestFile("MsrpRequestMessage1.txt");
        MsrpMessage msrpMessage = MsrpMessage.ParseMsrpMessage(MsgBytes);
        Assert.NotNull(msrpMessage);

        byte[] bytes = msrpMessage.ToByteArray();
        MsrpMessage msrpMessageResult = MsrpMessage.ParseMsrpMessage(bytes);
        Assert.NotNull(msrpMessageResult);

        Assert.True(msrpMessage.TransactionID == msrpMessageResult.TransactionID, "TransactionID mismatch");
        Assert.True(msrpMessage.MessageType == msrpMessageResult.MessageType, "MessageType mismatch");
        Assert.True(msrpMessage.MessageID == msrpMessageResult.MessageID, "MessageID mismatch");
        Assert.True(msrpMessage.ToPath.MsrpUris[0].ToString() == msrpMessageResult.ToPath.MsrpUris[0].
            ToString(), "To-Path mismatch");
        Assert.True(msrpMessage.FromPath.MsrpUris[0].ToString() == msrpMessageResult.FromPath.MsrpUris[0].
            ToString(), "From-Path mismatch");
        Assert.True(msrpMessage.ContentType == msrpMessageResult.ContentType, "ContentType mismatch");

        string strResultBody = Encoding.UTF8.GetString(msrpMessageResult.Body);
        Assert.True(strResultBody.Trim() == "Hi, I'm Alice!", "The Body is wrong");
    }

    [Fact]
    public void MsrpResponseMessage1()
    {
        byte[] MsgBytes = GetTestFile("MsrpResponseMessage1.txt");
        MsrpMessage msrpMessage = MsrpMessage.ParseMsrpMessage(MsgBytes);
        Assert.NotNull(msrpMessage);

        Assert.True(msrpMessage.MessageType == MsrpMessageType.Response, "The MessageType is wrong");
        Assert.True(msrpMessage.ResponseCode == 200, "The ResponseCode is wrong");
        Assert.True(msrpMessage.ResponseText == "OK", "The ResponseText is wrong");
        Assert.True(msrpMessage.TransactionID == "dkei38sd", "The TransactionID is wrong");
        Assert.True(msrpMessage.ToPath.MsrpUris.Count == 1, "The To-Path count is wrong");
        Assert.True(msrpMessage.FromPath.MsrpUris.Count == 1, "The From-Path count is wrong");
    }

    [Fact]
    public void MsrpResponseMessage1_ToByteArray()
    {
        byte[] MsgBytes = GetTestFile("MsrpResponseMessage1.txt");
        MsrpMessage orig = MsrpMessage.ParseMsrpMessage(MsgBytes);
        Assert.NotNull(orig);

        byte[] bytes = orig.ToByteArray();
        MsrpMessage result = MsrpMessage.ParseMsrpMessage(bytes);
        Assert.NotNull(result);

        Assert.True(orig.MessageType == result.MessageType, "MessageType mismatch");
        Assert.True(orig.ResponseCode == result.ResponseCode, "ResponseCode mismatch");
        Assert.True(orig.ResponseText == result.ResponseText, "ResponseText mismatch");
        Assert.True(orig.TransactionID == result.TransactionID, "TransactionID mismatch");
        Assert.True(orig.ToPath.MsrpUris[0].ToString() == result.ToPath.MsrpUris[0].ToString(),
            "To-Path mismatch");
        Assert.True(orig.FromPath.MsrpUris[0].ToString() == result.FromPath.MsrpUris[0].ToString(),
            "From-Path mismach");
    }

    [Fact]
    public void MsrpPositiveReport1()
    {
        byte[] MsgBytes = GetTestFile("MsrpPositiveReport1.txt");
        MsrpMessage msrpMessage = MsrpMessage.ParseMsrpMessage(MsgBytes);
        Assert.NotNull(msrpMessage);

        Assert.True(msrpMessage.MessageType == MsrpMessageType.Request, "The MessageType is wrong");
        Assert.True(msrpMessage.MessageID == "12339sdqwer", "The MessageID is wrong");
        Assert.True(msrpMessage.SuccessReport == "yes", "SuccessReport is wrong");
        Assert.True(msrpMessage.FailureReport == "no", "FailureReport is wrong");
        Assert.True(msrpMessage.ContentType == "text/html", "ContentType is wrong");
        Assert.True(msrpMessage.Body != null, "The Body is null");
    }

    [Fact]
    public void MsrpPositiveReport_ToByteArray()
    {
        byte[] MsgBytes = GetTestFile("MsrpPositiveReport1.txt");
        MsrpMessage orig = MsrpMessage.ParseMsrpMessage(MsgBytes);
        Assert.NotNull(orig);

        byte[] bytes = orig.ToByteArray();
        MsrpMessage result = MsrpMessage.ParseMsrpMessage(bytes);

        Assert.True(orig.MessageType == result.MessageType, "MessageType mismatch");
        Assert.True(orig.RequestMethod == result.RequestMethod, "RequestMethod mismatch");
        Assert.True(orig.SuccessReport == result.SuccessReport, "SuccessReport mismatch");
        Assert.True(orig.FailureReport == result.FailureReport, "FailureReport mismatch");
        Assert.True(orig.ContentType == result.ContentType, "ContentType mismatch");
        Assert.True(orig.Body.Length == result.Body.Length, "Body length mismatch");
    }

    [Fact]
    public void MsrpReportRequest1()
    {
        byte[] MsgBytes = GetTestFile("MsrpReportRequest1.txt");
        MsrpMessage msrpMessage = MsrpMessage.ParseMsrpMessage(MsgBytes);
        Assert.NotNull(msrpMessage);

        Assert.True(msrpMessage.TransactionID == "dkei38sd", "TransactionID is wrong");
        Assert.True(msrpMessage.MessageType == MsrpMessageType.Request, "The MessageType is wrong");
        Assert.True(msrpMessage.RequestMethod == "REPORT", "RequestMethod is wrong");
        Assert.True(msrpMessage.MessageID == "12339sdqwer", "MessageID is wrong");
        Assert.True(msrpMessage.Status.Namespace == "000", "Namespace is wrong");
        Assert.True(msrpMessage.Status.StatusCode == 200, "StatusCode is wrong");
        Assert.True(msrpMessage.Status.Comment == "OK", "Comment is wrong");
        Assert.True(msrpMessage.Body == null, "The Body is not null");
    }

    [Fact]
    public void MsrpReportRequest_ToByteArray()
    {
        byte[] MsgBytes = GetTestFile("MsrpReportRequest1.txt");
        MsrpMessage orig = MsrpMessage.ParseMsrpMessage(MsgBytes);
        Assert.NotNull(orig);

        byte[] bytes = orig.ToByteArray();
        MsrpMessage result = MsrpMessage.ParseMsrpMessage(bytes);

        Assert.True(orig.TransactionID == result.TransactionID, "TransactionID mismatch");
        Assert.True(orig.MessageType == result.MessageType, "MessageType mismatch");
        Assert.True(orig.RequestMethod == result.RequestMethod, "RequestMethod mismatch");
        Assert.True(orig.Status.Namespace == result.Status.Namespace, "Namespace mismatch");
        Assert.True(orig.Status.StatusCode == result.Status.StatusCode, "StatusCode mismatch");
        Assert.True(orig.Status.Comment == result.Status.Comment, "Comment mismatch");
    }

    [Fact]
    public void MsrpUnknownRequestMethod()
    {
        byte[] MsgBytes = GetTestFile("MsrpUnknownRequestMethod.txt");
        MsrpMessage msrpMessage = MsrpMessage.ParseMsrpMessage(MsgBytes);
        Assert.Null(msrpMessage);   // Expect failure
    }

    [Fact]
    public void MsrpNicknameRequest1()
    {
        byte[] MsgBytes = GetTestFile("MsrpNicknameRequest1.txt");
        MsrpMessage msrpMessage = MsrpMessage.ParseMsrpMessage(MsgBytes);
        Assert.NotNull(msrpMessage);

        Assert.True(msrpMessage.MessageType == MsrpMessageType.Request, "MessageType is wrong");
        Assert.True(msrpMessage.RequestMethod == "NICKNAME", "RequestMethod is wrong");
        Assert.True(msrpMessage.UseNickname == "\"Alice the great\"", "UseNickname is wrong");
    }

    [Fact]
    public void MsrpNicknameRequest_ToByteArray()
    {
        byte[] MsgBytes = GetTestFile("MsrpNicknameRequest1.txt");
        MsrpMessage orig = MsrpMessage.ParseMsrpMessage(MsgBytes);
        Assert.NotNull(orig);

        byte[] bytes = orig.ToByteArray();
        MsrpMessage result = MsrpMessage.ParseMsrpMessage(bytes);
        Assert.True(orig.MessageType == result.MessageType, "MessageType mismatch");
        Assert.True(orig.RequestMethod == result.RequestMethod, "RequestMethod mismatch");
        Assert.True(orig.UseNickname == result.UseNickname, "UseNickname mismatch");
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
