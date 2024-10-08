﻿/////////////////////////////////////////////////////////////////////////////////////
//  File:   MsrpMessageUnitTests.cs                                 26 Jul 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLibUnitTests.Msrp;

using SipLib.Body;
using SipLib.Msrp;
using System.IO;
using System.Text;

[Trait("Category", "unit")]
public class MsrpMessageUnitTests
{
    /// <summary>
    /// Specifies the path to the files containing the test MSRP messages. Change this if the project
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
    public void MsrpRequestMessage1_ImageJpeg()
    {
        byte[] MsgBytes = GetTestFile("MsrpRequestMessage1.txt");
        MsrpMessage msrpMessage1 = MsrpMessage.ParseMsrpMessage(MsgBytes);
        Assert.NotNull(msrpMessage1);

        byte[] CarCrashBytes = GetTestFile("CarCrashPicture.jpg");
        msrpMessage1.ContentType = "image/jpeg";
        msrpMessage1.Body = CarCrashBytes;
        msrpMessage1.ByteRange.Start = 1;
        msrpMessage1.ByteRange.End = CarCrashBytes.Length - 1;
        msrpMessage1.ByteRange.Total = CarCrashBytes.Length - 1;

        byte[] msrpMessage2Bytes = msrpMessage1.ToByteArray();
        MsrpMessage msrpMessage2 = MsrpMessage.ParseMsrpMessage(msrpMessage2Bytes);
        Assert.NotNull(msrpMessage2);
        Assert.True(msrpMessage2.ContentType == "image/jpeg", "The ContentType is wrong");
        Assert.True(msrpMessage2.Body.Length == CarCrashBytes.Length, "The Body length is wrong");
        bool BodyMatches = true;
        for (int i = 0; i < msrpMessage2.Body.Length; i++)
        {
            if (msrpMessage2.Body[i] != CarCrashBytes[i])
            {
                BodyMatches = false;
                break;
            }
        }

        Assert.True(BodyMatches == true, "The message Body is wrong");
    }

    [Fact]
    public void MsrpRequestMessage1_MultipartMixed()
    {
        byte[] MsgBytes = GetTestFile("MsrpRequestMessage1.txt");
        MsrpMessage msrpMessage1 = MsrpMessage.ParseMsrpMessage(MsgBytes);
        Assert.NotNull(msrpMessage1);

        byte[] CarCrashBytes = GetTestFile("CarCrashPicture.jpg");
        List<MessageContentsContainer> contents1 = new List<MessageContentsContainer>();
        MessageContentsContainer TextContents = new MessageContentsContainer();
        TextContents.ContentType = "text/plain";
        TextContents.IsBinaryContents = false;
        TextContents.StringContents = "Here is a picture of a car crash";
        contents1.Add(TextContents);

        MessageContentsContainer ImageContents = new MessageContentsContainer();
        ImageContents.ContentType = "image/jpeg";
        ImageContents.IsBinaryContents = true;
        ImageContents.BinaryContents = CarCrashBytes;
        contents1.Add(ImageContents);

        string BoundaryString = "boundary1";
        msrpMessage1.ContentType = $"multipart/mixed; boundary={BoundaryString}";
        msrpMessage1.Body = MultipartBinaryBodyBuilder.ToByteArray(contents1, BoundaryString);
        msrpMessage1.ByteRange.Start = 1;
        msrpMessage1.ByteRange.End = msrpMessage1.Body.Length - 1;
        msrpMessage1.ByteRange.Total = msrpMessage1.Body.Length - 1;

        byte[] msrpMessage2Bytes = msrpMessage1.ToByteArray();
        string msrpMessage2String = Encoding.UTF8.GetString(msrpMessage2Bytes);

        MsrpMessage msrpMessage2 = MsrpMessage.ParseMsrpMessage(msrpMessage2Bytes);
        Assert.NotNull(msrpMessage2);

        Assert.True(msrpMessage2.ContentType.Contains("multipart") == true, "The ContentType is wrong");
        //List<SipContentsContainer> contents2 = BodyParser.ProcessMultiPartContents(msrpMessage2Bytes,
        //    msrpMessage2.ContentType);
        List<MessageContentsContainer> contents2 = BodyParser.ProcessMultiPartContents(msrpMessage2.Body,
            msrpMessage2.ContentType);
        Assert.True(contents2.Count == 2, "The contents count is wrong");

        Assert.True(contents2[0].ContentType == "text/plain", "The first ContentType is wrong");
        Assert.True(contents2[0].StringContents == "Here is a picture of a car crash", 
            "The first StringContents is wrong");

        Assert.True(contents2[1].ContentType == "image/jpeg", "The second ContentType is wrong");
        Assert.True(contents2[1].IsBinaryContents == true, "The second IsBinaryContents is wrong");
        Assert.True(contents2[1].BinaryContents.Length == CarCrashBytes.Length, "The second BinaryContents " +
            "length is wrong");
        //Assert.True(contents2[1].BinaryContents.Equals(CarCrashBytes) == true, "BinaryContents mismatch");
        bool Mismatch = false;
        for (int i = 0; i < CarCrashBytes.Length; i++)
        {
            if (contents2[1].BinaryContents[i] != CarCrashBytes[i])
                Mismatch = true;
        }

        Assert.False(Mismatch == true, "BinaryContents mismatch");
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
