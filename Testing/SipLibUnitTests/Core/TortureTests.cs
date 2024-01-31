#region License
//-----------------------------------------------------------------------------
// Author(s):
// Aaron Clauson
// 
// History:
// 
//
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//-----------------------------------------------------------------------------
#endregion

/////////////////////////////////////////////////////////////////////////////////////
//	Revised:	13 Nov 22 PHR -- Initial version.
//                -- Added tests for all 49 test cases in RFC 4475.
//                -- Commented out all tests for IPv6 for now
//              29 Nov 22 PHR -- Uncommented all tests for IPv6 and got them working
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLibUnitTests.Core;
using System.Net;
using System.Net.Sockets;
using System.Text;

using SipLib.Core;
using SipLib.Body;
using SipLib.Sdp;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;

/// <summary>
/// Torture tests from RFC4475 https://tools.ietf.org/html/rfc4475
/// Tests must be extracted from the base64 blob at the bottom of the RFC:
/// $ cat torture.b64 | base64 -d > torture.tar.gz  
/// $ tar zxvf torture.tar.gz
/// Which gives the dat files needed.
/// Cutting and pasting is no good as things like white space getting interpreted as end of line screws up
/// intent of the tests.
/// </summary>
[Trait("Category", "unit")]
public class SIPTortureTests
{
    /// <summary>
    /// Default constructor required by xUnit
    /// </summary>
    /// <param name="output"></param>
    public SIPTortureTests(Xunit.Abstractions.ITestOutputHelper output)
    {
    }

    /// <summary>
    /// Specifies the path to the files containing the test SIP messages.
    /// Change this if the project location or the location of the test files
    /// change.
    /// </summary>
    private const string Path = @"..\..\..\rfc4475tests\";

    /// <summary>
    /// Helper function to read a test case SIP message file into a string.
    /// </summary>
    /// <param name="strFileName">File name to read</param>
    /// <returns>Returns a string containing a SIP message if successful</returns>
    private string GetRawData(string strFileName)
    {
        string strFilePath = $"{Path}{strFileName}";
        Assert.True(File.Exists(strFilePath), $"The {strFileName} torture test input file was missing.");
        string raw = File.ReadAllText(strFilePath);
        return raw;
    }

    /// <summary>
    /// Helper function that reads a test file containing a SIP request message
    /// and parses the request message.
    /// Use this function when the parsing of the SIP request is expected to be
    /// successful.
    /// </summary>
    /// <param name="strFileName">Name of a file containing a SIP request message
    /// </param>
    /// <returns>Returns a SIPRequest if successful</returns>
    private SIPRequest DoRequestExpectedSuccess(string strFileName)
    {
        string raw = GetRawData(strFileName);
        if (string.IsNullOrEmpty(raw) == true)
            return null;

        SIPMessage sipMessage = SIPMessage.ParseSIPMessage(Encoding.UTF8.GetBytes(raw), null, null);
        Assert.NotNull(sipMessage);
        SIPRequest Req = SIPRequest.ParseSIPRequest(raw);
        Assert.NotNull(Req);
        return Req;
    }

    /// <summary>
    /// Helper function that reads a test file containing a SIP request message
    /// and parses the request message.
    /// Use this function when the parsing of the SIP request is expected fail.
    /// </summary>
    /// <param name="strFileName">Name of a file containing a SIP request message
    /// </param>
    private void DoRequestExpectedFailure(string strFileName)
    {
        string raw = GetRawData(strFileName);
        if (string.IsNullOrEmpty(raw) == true)
            return;

        SIPMessage sipMessage = SIPMessage.ParseSIPMessage(Encoding.UTF8.GetBytes(raw), null, null);
        Assert.NotNull(sipMessage);
        // Expect failure
        Assert.Throws<SIPValidationException>(() => SIPRequest.ParseSIPRequest(raw));
    }

    /// <summary>
    /// Helper function that reads a test file containing a SIP response message
    /// and parses the response message.
    /// Use this function when the parsing of the SIP response is expected to be
    /// successful.
    /// </summary>
    /// <param name="strFileName">Name of a file containing a SIP response message
    /// </param>
    /// <returns>Returns a SIPResponse if successful</returns>
    private SIPResponse DoResponseExpectSuccess(string strFile)
    {
        string raw = GetRawData(strFile);
        if (string.IsNullOrEmpty(raw) == true)
            return null;

        SIPMessage sipMessageBuffer = SIPMessage.ParseSIPMessage(Encoding.UTF8.GetBytes(raw), null, null);
        SIPResponse Res = SIPResponse.ParseSIPResponse(raw);
        Assert.NotNull(sipMessageBuffer);
        Assert.NotNull(Res);
        return Res;
    }

    /// <summary>
    /// Helper function that reads a test file containing a SIP response message
    /// and parses the response message.
    /// Use this function when the parsing of the SIP response is expected fail.
    /// </summary>
    /// <param name="strFileName">Name of a file containing a SIP response message
    /// </param>
    private void DoResponseExpectFailure(string strFile)
    {
        string raw = GetRawData(strFile);
        if (string.IsNullOrEmpty(raw) == true)
            return;

        SIPMessage sipMessage = SIPMessage.ParseSIPMessage(Encoding.UTF8.GetBytes(raw), null, null);
        Assert.NotNull(sipMessage);

        // Expect a SIPValidationException
        Assert.Throws<SIPValidationException>(() => SIPResponse.ParseSIPResponse(raw));
    }

    /// <summary>
    /// Torture test 3.1.1.1 or RFC 4475 with file wsinv.dat.
    /// </summary>
    /// <remarks>
    /// The SIPRequest.ParseSIPRequest() function fails to parse the SIP request 
    /// in the original test file called wsinv.dat. The reason for this
    /// failure is Via headers like (after unfolding the folded lines):
    ///     Via: SIP / 2.0 / UDP 192.0.2.2; branch=xxxxx
    /// The whitespace in the sent-protocol field (SIP/2.0/UDP) cause the proble.
    /// 
    /// In RFC 3261, the ABNF for the sent-protocol field of the Via header is:
    ///     sent-protocol     =  protocol-name SLASH protocol-version SLASH transport
    ///     protocol-name     =  "SIP" / token
    ///     protocol-version  =  token
    ///     transport         =  "UDP" / "TCP" / "TLS" / "SCTP" / other-transport
    ///     
    /// My interpretation of this ABNF specification is that the sent-protocol should
    /// not contain whitespace. Hence, the parsing of the Via headers0 in the original
    /// file should should fail.
    /// 
    /// The file called wsinfFixed.dat is a copy of the original file (wsinf.dat) with
    /// the whitespace removed in the sent-protcol field of the Via headers. If this
    /// file is used then this test passes.
    /// </remarks>
    //[Fact(Skip = "Bit trickier to pass than anticipated.")]
    [Fact]
    public void ShortTorturousInvite()
    {
        DoRequestExpectedSuccess("wsinvFixed.dat");
    }

    /// <summary>
    /// Torture test 3.1.1.2. with file intmeth.dat.
    /// Wide range of valid charactes with an unknown request method
    /// </summary>
    [Fact]
    public void WideRangeValidCharacters()
    {
        DoRequestExpectedSuccess("intmeth.dat");
    }

    /// <summary>
    /// Torture test 3.1.1.3. with file esc01.dat.
    /// Wide range of valid charactes with an unknown request method
    /// </summary>
    [Fact]
    public void ValidEscapeMechanism()
    {
        DoRequestExpectedSuccess("esc01.dat");
    }

    /// <summary>
    /// Torture test 3.1.1.4. with file escnull.dat.
    /// REGISTER request with several escaped nulls in URIs
    /// </summary>
    [Fact]
    public void EscapedNullsInUris()
    {
        DoRequestExpectedSuccess("escnull.dat");
    }

    /// <summary>
    /// Torture test 3.1.1.5. with file esc02.dat.
    /// Reguest contains % characters that are not escapes
    /// </summary>
    [Fact]
    public void UsePercentWhenNotAnEscape()
    {
        DoRequestExpectedSuccess("esc02.dat");
    }

    /// <summary>
    /// Torture test 3.1.1.6. with file lwsdisp.dat.
    /// Message with No LWS between Display Name and <
    /// </summary>
    [Fact]
    public void NoLws()
    {
        DoRequestExpectedSuccess("lwsdisp.dat");
    }

    /// <summary>
    /// Torture test 3.1.1.7. with file longreq.dat.
    /// This well-formed request contains header fields with many values and
    /// values that are very long.
    /// </summary>
    [Fact]
    public void LongValuesInHeaderFields()
    {
        DoRequestExpectedSuccess("longreq.dat");
    }

    /// <summary>
    /// Torture test 3.1.1.8. with file dblreq.dat.
    /// This message contains a single SIP REGISTER request, which ostensibly
    /// arrived over UDP in a single datagram.
    /// </summary>
    [Fact]
    public void ExtraTrailingOctetsUDP()
    {
        DoRequestExpectedSuccess("dblreq.dat");
    }

    /// <summary>
    /// Torture test 3.1.1.9. with file semiuri.dat.
    /// This request has a semicolon-separated parameter contained in the
    /// "user" part of the Request-URI
    /// </summary>
    [Fact]
    public void SemicolonSeparatedParametersInURI()
    {
        DoRequestExpectedSuccess("semiuri.dat");
    }

    /// <summary>
    /// Torture test 3.1.1.10. with file transports.dat.
    /// This request contains Via header field values with all known
    /// transport types and exercises the transport extension mechanism.
    /// Parsers must accept this message as well formed.
    /// </summary>
    [Fact]
    public void VariedUnknownTransportTypes()
    {
        DoRequestExpectedSuccess("transports.dat");
    }

    /// <summary>
    /// Torture test 3.1.1.11. with file mpart01.dat.
    /// This MESSAGE request contains two body parts.
    /// This test only tests the basic message parsing.
    /// </summary>
    [Fact]
    public void MultipartMIMEMessage()
    {
        DoRequestExpectedSuccess("mpart01.dat");
    }

    /// <summary>
    /// Tests binary body contents parsing.
    /// </summary>
    [Fact]
    public void BinaryBodyParsing()
    {
        string strPath = Path + "mpart01.dat";
        byte[] MsgBytes = File.ReadAllBytes(strPath);
        SIPMessage Msg = SIPMessage.ParseSIPMessage(MsgBytes, null, null);
        Assert.NotNull(Msg);
        SIPRequest Req = SIPRequest.ParseSIPRequest(Msg);
        Assert.NotNull(Req);
        List<MessageContentsContainer> Scc = BodyParser.ParseSipBody(Req.RawBuffer, Req.Header.
            ContentType);
        Assert.True(Scc.Count == 2, "The SIP contents count is incorrect");

        Assert.True(Scc[0].IsBinaryContents == true, "The contents are not binary for the first " +
            "contents block");
        Assert.True(Scc[0].ContentType == "text/plain", "The Content-Type is incorrect for " +
            "the first contents block");
        Assert.True(Scc[0].ContentTransferEncoding == "binary", "The first Content-Transfer-" +
            "Encoding value is incorrect");
        Assert.True(Encoding.UTF8.GetString(Scc[0].BinaryContents).ToString().Trim() == "Hello",
            "The first contents is incorrect");

        Assert.True(Scc[1].IsBinaryContents == true, "The contents are not binary for the second " +
            "contents block");
        Assert.True(Scc[1].ContentType == "application/octet-stream", 
            "The Content-Type is incorrect for the second contents block");
        Assert.True(Scc[1].ContentTransferEncoding == "binary", "The second Content-Transfer-" +
            "Encoding value is incorrect");

        byte[] Bc = Scc[1].BinaryContents;
        Assert.True(Bc.Length == BinaryBody.Length, "The second binary content length is incorrect");
        for (int i=0; i < Bc.Length; i++)
        {
            Assert.True(Bc[i] == BinaryBody[i], $"The value at {i} does not match");
        }

    }

    /// <summary>
    /// See Section 3.1.1.11 of RFC 4475
    /// </summary>
    private static byte[] BinaryBody =
    {
        0x30, 0x82, 0x01, 0x52, 0x06, 0x09, 0x2A, 0x86,
        0x48, 0x86, 0xF7, 0x0D, 0x01, 0x07, 0x02, 0xA0, 0x82, 0x01, 0x43, 0x30, 0x82, 0x01, 0x3F, 0x02,
        0x01, 0x01, 0x31, 0x09, 0x30, 0x07, 0x06, 0x05, 0x2B, 0x0E, 0x03, 0x02, 0x1A, 0x30, 0x0B, 0x06,
        0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x07, 0x01, 0x31, 0x82, 0x01, 0x20, 0x30, 0x82,
        0x01, 0x1C, 0x02, 0x01, 0x01, 0x30, 0x7C, 0x30, 0x70, 0x31, 0x0B, 0x30, 0x09, 0x06, 0x03, 0x55,
        0x04, 0x06, 0x13, 0x02, 0x55, 0x53, 0x31, 0x13, 0x30, 0x11, 0x06, 0x03, 0x55, 0x04, 0x08, 0x13,
        0x0A, 0x43, 0x61, 0x6C, 0x69, 0x66, 0x6F, 0x72, 0x6E, 0x69, 0x61, 0x31, 0x11, 0x30, 0x0F, 0x06,
        0x03, 0x55, 0x04, 0x07, 0x13, 0x08, 0x53, 0x61, 0x6E, 0x20, 0x4A, 0x6F, 0x73, 0x65, 0x31, 0x0E,
        0x30, 0x0C, 0x06, 0x03, 0x55, 0x04, 0x0A, 0x13, 0x05, 0x73, 0x69, 0x70, 0x69, 0x74, 0x31, 0x29,
        0x30, 0x27, 0x06, 0x03, 0x55, 0x04, 0x0B, 0x13, 0x20, 0x53, 0x69, 0x70, 0x69, 0x74, 0x20, 0x54,
        0x65, 0x73, 0x74, 0x20, 0x43, 0x65, 0x72, 0x74, 0x69, 0x66, 0x69, 0x63, 0x61, 0x74, 0x65, 0x20,
        0x41, 0x75, 0x74, 0x68, 0x6F, 0x72, 0x69, 0x74, 0x79, 0x02, 0x08, 0x01, 0x95, 0x00, 0x71, 0x02,
        0x33, 0x01, 0x13, 0x30, 0x07, 0x06, 0x05, 0x2B, 0x0E, 0x03, 0x02, 0x1A, 0x30, 0x0D, 0x06, 0x09,
        0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x01, 0x05, 0x00, 0x04, 0x81, 0x80, 0x8E, 0xF4,
        0x66, 0xF9, 0x48, 0xF0, 0x52, 0x2D, 0xD2, 0xE5, 0x97, 0x8E, 0x9D, 0x95, 0xAA, 0xE9, 0xF2, 0xFE,
        0x15, 0xA0, 0x66, 0x59, 0x71, 0x62, 0x92, 0xE8, 0xDA, 0x2A, 0xA8, 0xD8, 0x35, 0x0A, 0x68, 0xCE,
        0xFF, 0xAE, 0x3C, 0xBD, 0x2B, 0xFF, 0x16, 0x75, 0xDD, 0xD5, 0x64, 0x8E, 0x59, 0x3D, 0xD6, 0x47,
        0x28, 0xF2, 0x62, 0x20, 0xF7, 0xE9, 0x41, 0x74, 0x9E, 0x33, 0x0D, 0x9A, 0x15, 0xED, 0xAB, 0xDB,
        0x93, 0xD1, 0x0C, 0x42, 0x10, 0x2E, 0x7B, 0x72, 0x89, 0xD2, 0x9C, 0xC0, 0xC9, 0xAE, 0x2E, 0xFB,
        0xC7, 0xC0, 0xCF, 0xF9, 0x17, 0x2F, 0x3B, 0x02, 0x7E, 0x4F, 0xC0, 0x27, 0xE1, 0x54, 0x6D, 0xE4,
        0xB6, 0xAA, 0x3A, 0xBB, 0x3E, 0x66, 0xCC, 0xCB, 0x5D, 0xD6, 0xC6, 0x4B, 0x83, 0x83, 0x14, 0x9C,
        0xB8, 0xE6, 0xFF, 0x18, 0x2D, 0x94, 0x4F, 0xE5, 0x7B, 0x65, 0xBC, 0x99, 0xD0, 0x05
    };

    private const string CRLF = "\r\n";

    /// <summary>
    /// See Section 3.1.1.8 of RFC 4475
    /// </summary>
    private string strInviteWithSdp =
        "INVITE sip:joe@example.com SIP/2.0" + CRLF +
        "t: sip:joe@example.com" + CRLF +
        "From: sip:caller@example.net; tag=141334" + CRLF +
        "Max-Forwards: 8" + CRLF +
        "Call-ID: dblreq.0ha0isnda977644900765@192.0.2.15" + CRLF +
        "CSeq: 8 INVITE" + CRLF +
        "Via: SIP/2.0/UDP 192.0.2.15;branch=z9hG4bKkdjuw380234" + CRLF +
        "Content-Type: application/sdp" + CRLF +
        "Content-Length: 150" + CRLF + 
        CRLF +
        "v=0" + CRLF +
        "o=mhandley 29739 7272939 IN IP4 192.0.2.15" + CRLF +
        "s=-" + CRLF +
        "c=IN IP4 192.0.2.15" + CRLF +
        "t=0 0" + CRLF +
        "m=audio 49217 RTP/AVP 0 12" + CRLF +
        "m=video 3227 RTP/AVP 31" + CRLF +
        "a=rtpmap:31 LPC" + CRLF;

    [Fact]
    public void SimpleTextContents()
    {
        SIPRequest Req = SIPRequest.ParseSIPRequest(strInviteWithSdp);
        byte[] ReqBytes = Encoding.UTF8.GetBytes(strInviteWithSdp);
        Assert.NotNull(Req);
        List<MessageContentsContainer> Contents = BodyParser.ParseSipBody(ReqBytes,
            Req.Header.ContentType);

        Assert.True(Contents.Count == 1, "The contents count is incorrect");
        Assert.True(Contents[0].IsBinaryContents == false, "The IsBinaryContents value is " +
            "incorrect");
        Assert.True(Contents[0].ContentType == "application/sdp", "The Content-Type is incorrect");
        SipLib.Sdp.Sdp sdp = SipLib.Sdp.Sdp.ParseSDP(Contents[0].StringContents);
        Assert.NotNull(sdp);
    }

    /// <summary>
    /// Torture test 3.1.1.12. with file unreason.dat.
    /// This 200 response contains a reason phrase other than "OK".
    /// </summary>
    [Fact]
    public void UnusualReasonPhrase()
    {
        DoResponseExpectSuccess("unreason.dat");
    }

    /// <summary>
    /// Torture test 3.1.1.13. with file noreason.dat.
    /// This well-formed response contains no reason phrase. A parser must 
    /// accept this message.
    /// </summary>
    [Fact]
    public void EmptyReasonPhrase()
    {
        DoResponseExpectSuccess("noreason.dat");
    }

    /// <summary>
    /// Torture test 3.1.2.1. with file badinv01.dat.
    /// The Via header field of this request contains additional semicolons
    /// and commas without parameters or values.The Contact header field
    /// contains additional semicolons without parameters. This message is
    /// syntactically invalid.
    /// </summary>
    [Fact]
    public void ExtraneousHeaderFieldSeparators()
    {
        // Invalid request, so expect a SIPValidationException
        DoRequestExpectedFailure("badinv01.dat");
    }

    /// <summary>
    /// Torture test 3.1.2.2. with file clerr.dat.
    /// This is a request message with a Content Length that is larger than
    /// the actual length of the body.
    /// </summary>
    [Fact]
    public void ContentLengthLargerThanMessage()
    {
        SIPRequest Req = DoRequestExpectedSuccess("clerr.dat");
        Assert.NotNull(Req.Body);
        Assert.False(Req.Header.ContentLength == Req.Body.Length);
    }

    /// <summary>
    /// Torture test 3.1.2.3. with file ncl.dat.
    /// This request has a negative value for Content-Length. For UDP, the parser should
    /// return an error.
    /// </summary>
    [Fact]
    public void NegativeContentLength()
    {
        SIPRequest Req = DoRequestExpectedSuccess("ncl.dat");
        Assert.NotNull(Req.Body);
        Assert.False(Req.Header.ContentLength >= 0);
    }

    /// <summary>
    /// Torture test 3.1.2.4. with file scalar02.dat.
    /// This request has a negative value for Content-Length. For UDP, the parser should
    /// return an error.
    /// </summary>
    [Fact]
    public void RequestScalarFieldsWithOverlargeValues()
    {
        // Expect a SIPValidationException because of the values that are out of range
        DoRequestExpectedFailure("scalar02.dat");
    }

    /// <summary>
    /// Torture test 3.1.2.5. with file scalarlg.dat.
    /// This response contains several scalar header field values outside
    /// their legal range.
    /// </summary>
    [Fact]
    public void ResponseScalarFieldsWithOverlargeValues()
    {
        // Expect a SIPValidationException because of the values that are out of range
        DoResponseExpectFailure("scalarlg.dat");
    }

    /// <summary>
    /// Torture test 3.1.2.6. with file quotbal.dat.
    /// This is a request with an unterminated quote in the display name of
    /// the To field.An element receiving this request should return a 400
    /// Bad Request error.
    /// </summary>
    [Fact]
    public void UnterminatedQuotedStringInDisplayName()
    {
        // But I think the parser should be able to handle this case
        // because the display name is not a required field for handling
        // requests.
        SIPRequest Req = DoRequestExpectedSuccess("quotbal.dat");
        Assert.NotNull(Req);
    }

    /// <summary>
    /// Torture test 3.1.2.7. with file ltgtruri.dat.
    /// This INVITE request is invalid because the Request-URI has been
    /// enclosed within in "<>".
    /// </summary>
    [Fact]
    public void AngleBracketsEnclosingRequestURI()
    {
        DoRequestExpectedFailure("ltgtruri.dat");
    }

    /// <summary>
    /// Torture test 3.1.2.8. with file lwsruri.dat.
    /// This INVITE has illegal LWS within the Request-URI.
    /// An element could attempt to ignore the embedded LWS for those schemes
    /// (like SIP) where doing so would not introduce ambiguity.
    /// </summary>
    [Fact]
    public void MalformedSIPRequestURI()
    {
        // But I think the parser should be able to handle this case.
        // See Summary comment above
        SIPRequest Req = DoRequestExpectedSuccess("lwsruri.dat");
        Assert.NotNull(Req);
    }

    /// <summary>
    /// Torture test 3.1.2.9. with file lwsstart.dat.
    /// This INVITE has illegal multiple SP characters between elements of
    /// the start line.
    /// 
    /// An element that is liberal in what it accepts may ignore these extra SP
    /// characters when processing the request.
    /// </summary>
    [Fact]
    public void MultipleSPSeparatingRequestLineElements()
    {
        // But I think the parser should be able to handle this case.
        // See Summary comment above
        SIPRequest Req = DoRequestExpectedSuccess("lwsstart.dat");
        Assert.NotNull(Req);
    }

    /// <summary>
    /// Torture test 3.1.2.10. with file trws.dat.
    /// This OPTIONS request contains SP characters between the SIP-Version
    /// field and the CRLF terminating the Request-Line.
    /// 
    /// An element that is liberal in what it accepts may ignore these extra SP
    /// characters when processing the request.
    /// </summary>
    [Fact]
    public void SPCharactersAtEndOfRequestLine()
    {
        // But I think the parser should be able to handle this case.
        // See Summary comment above
        SIPRequest Req = DoRequestExpectedSuccess("trws.dat");
        Assert.NotNull(Req);
    }

    /// <summary>
    /// Torture test 3.1.2.11. with file escruri.dat.
    /// This INVITE is malformed, as the SIP Request-URI contains escaped headers.
    /// 
    /// An element that is liberal in what it accepts may ignore these extra SP
    /// characters when processing the request.
    /// </summary>
    [Fact]
    public void EscapedHeadersInSIPRequestURI()
    {
        // I think the parser should be able to handle this case. See Summary comment above
        SIPRequest Req = DoRequestExpectedSuccess("escruri.dat");
        Assert.NotNull(Req);
    }

    /// <summary>
    /// Torture test 3.1.2.12. with file baddate.dat.
    /// This INVITE is invalid, as it contains a non-GMT time zone in the SIP
    /// Date header field.
    /// 
    /// An element wishing  to be liberal in what it accepts could ignore this value altogether
    /// if it wasn’t going to use the Date header field anyway.
    /// </summary>
    [Fact]
    public void InvalidTimeZoneInDateHeaderField()
    {
        // I think the parser should be able to handle this case. See Summary comment above
        SIPRequest Req = DoRequestExpectedSuccess("baddate.dat");
        Assert.NotNull(Req);
    }

    /// <summary>
    /// Torture test 3.1.2.13. with file regbadct.dat.
    /// This REGISTER request is malformed. The SIP URI contained in the
    /// Contact Header field has an escaped header
    /// 
    /// An element choosing to be liberal in what it
    /// accepts could infer the angle brackets since there is no ambiguity in
    /// this example.
    /// </summary>
    [Fact]
    public void FailureToEncloseNameAddrURIAngleBrackets()
    {
        // I think the parser should be able to handle this case. See Summary comment above
        SIPRequest Req = DoRequestExpectedSuccess("regbadct.dat");
        Assert.NotNull(Req);
    }

    /// <summary>
    /// Torture test 3.1.2.14. with file badaspec.dat.
    /// This request is malformed, since the addr-spec in the To header field
    /// contains spaces.
    /// 
    /// Elements attempting to be liberal may ignore the spaces.
    /// </summary>
    [Fact]
    public void SpacesWithinAddrSpec()
    {
        // I think the parser should be able to handle this case. See Summary comment above
        SIPRequest Req = DoRequestExpectedSuccess("badaspec.dat");
        Assert.NotNull(Req);
    }

    /// <summary>
    /// Torture test 3.1.2.15. with file baddn.dat.
    /// This OPTIONS request is malformed, since the display names in the To
    /// and From header fields contain non-token characters but are unquoted.
    /// 
    /// An element may attempt to be liberal in what it receives and infer
    /// the missing quotes.
    /// </summary>
    [Fact]
    public void NonTokenCharactersInDisplayName()
    {
        // I think the parser should be able to handle this case. See Summary comment above
        SIPRequest Req = DoRequestExpectedSuccess("baddn.dat");
        Assert.NotNull(Req);
    }

    /// <summary>
    /// Torture test 3.1.2.16. with file badvers.dat.
    /// To an element implementing [RFC3261], this request is malformed due
    /// to its high version number.
    /// </summary>
    [Fact]
    public void UnknownProtocolVersion()
    {
        SIPRequest Req = DoRequestExpectedSuccess("badvers.dat");
        Assert.NotNull(Req);

        SIPValidationFieldsEnum errorField;
        string errorMessage;
        // Expect failure
        Assert.False(Req.IsValid(out errorField, out errorMessage));
    }

    /// <summary>
    /// Torture test 3.1.2.17. with file mismatch01.dat.
    /// This request has mismatching values for the method in the start line
    /// and the CSeq header field.
    /// </summary>
    [Fact]
    public void StartLineAndCSeqMethodMismatch()
    {
        SIPRequest Req = DoRequestExpectedSuccess("mismatch01.dat");
        Assert.NotNull(Req);

        SIPValidationFieldsEnum errorField;
        string errorMessage;
        // Expect failure
        Assert.False(Req.IsValid(out errorField, out errorMessage));
    }

    /// <summary>
    /// Torture test 3.1.2.18. with file mismatch02.dat.
    /// This message has an unknown method in the start line, and a CSeq
    /// method tag that does not match.
    /// </summary>
    [Fact]
    public void UnknownMethodWithCSeqMethodMismatch()
    {
        SIPRequest Req = DoRequestExpectedSuccess("mismatch02.dat");
        Assert.NotNull(Req);

        SIPValidationFieldsEnum errorField;
        string errorMessage;
        // Expect failure
        Assert.False(Req.IsValid(out errorField, out errorMessage));
    }

    /// <summary>
    /// Torture test 3.1.2.19. with file bigcode.dat.
    /// This response has a response code larger than 699. An element
    /// receiving this response should simply drop it.
    /// </summary>
    [Fact]
    public void OverlargeResponseCode()
    {
        string raw = GetRawData("bigcode.dat");
        if (string.IsNullOrEmpty(raw) == true)
            return;

        SIPResponse Res = DoResponseExpectSuccess("bigcode.dat");
        Assert.NotNull(Res);
        
        // Note: The logic that processes this response code will simply ignore it.
    }

    /// <summary>
    /// Torture test 3.2.1 with file badbranch.dat.
    /// This request indicates support for RFC 3261-style transaction
    /// identifiers by providing the z9hG4bK prefix to the branch parameter,
    /// but it provides no identifier. A parser must not break when
    /// receiving this message.
    /// </summary>
    [Fact]
    public void MissingTransactionIdentifier()
    {
        DoRequestExpectedSuccess("badbranch.dat");
    }

    /// <summary>
    /// Torture test 3.3.1 with file insuf.dat.
    /// This request contains no Call-ID, From, or To header fields.
    /// </summary>
    [Fact]
    public void MissingRequiredHeaderFields()
    {
        // Expect to be able to parse this message
        SIPRequest Req = DoRequestExpectedSuccess("insuf.dat");
        Assert.NotNull(Req);

        /// The check for validity should fail
        SIPValidationFieldsEnum errorField;
        string errorMessage;
        Assert.False(Req.IsValid(out errorField, out errorMessage));
    }

    /// <summary>
    /// Torture test 3.3.2 with file unkscm.dat.
    /// This OPTIONS contains an unknown URI scheme in the Request-URI.
    /// </summary>
    [Fact]
    public void RequestURIWithUnknownScheme()
    {
        DoRequestExpectedFailure("unkscm.dat");
    }

    /// <summary>
    /// Torture test 3.3.3 with file novelsc.dat.
    /// This OPTIONS contains an Request-URI with an IANA-registered scheme
    /// that does not commonly appear in Request-URIs of SIP requests.
    /// </summary>
    [Fact]
    public void RequestURIWithKnownButAtypicalScheme()
    {
        DoRequestExpectedFailure("novelsc.dat");
    }

    /// <summary>
    /// Torture test 3.3.4 with file unksm2.dat.
    /// This message contains registered schemes in the To, From, and Contact
    /// header fields of a request.
    /// </summary>
    [Fact]
    public void UnknownURISchemesInHeaderFields()
    {
        DoRequestExpectedFailure("unksm2.dat");
    }

    /// <summary>
    /// Torture test 3.3.5 with file bext01.dat.
    /// This request tests proper implementation of SIP’s Proxy-Require and
    /// Require extension mechanisms.
    /// </summary>
    [Fact]
    public void ProxyRequireAndRequire()
    {
        // Should be able to parse this message
        SIPRequest Req = DoRequestExpectedSuccess("bext01.dat");
        Assert.NotNull(Req);

        // Verify proper parsing of the Require and Proxy-Require headers
        Assert.NotNull(Req.Header.ProxyRequire);
        Assert.NotNull(Req.Header.Require);
        Assert.True(Req.Header.ProxyRequire == "noProxiesSupportThis, norDoAnyProxiesSupportThis");
        Assert.True(Req.Header.Require == "nothingSupportsThis, nothingSupportsThisEither");
    }

    /// <summary>
    /// Torture test 3.3.6 with file invut.dat.
    /// This INVITE request contains a body of unknown type. It is
    /// syntactically valid. A parser must not fail when receiving it.
    /// </summary>
    [Fact]
    public void UnknownContentType()
    {
        DoRequestExpectedSuccess("invut.dat");
    }

    /// <summary>
    /// Torture test 3.3.7 with file regaut01.dat.
    /// This REGISTER request contains an Authorization header field with an
    /// unknown scheme.
    /// </summary>
    [Fact]
    public void UnknownAuthorizationScheme()
    {
        SIPRequest Req = DoRequestExpectedSuccess("regaut01.dat");
        Assert.NotNull(Req);

        // Verify that the Authentication header could at least be parsed.
        Assert.NotNull(Req.Header.AuthenticationHeader);
    }

    /// <summary>
    /// Torture test 3.3.8 with file multi01.dat.
    /// The message contains a request with multiple Call-ID, To, From, Max-
    /// Forwards, and CSeq values. An element receiving this request must
    /// not break.
    /// </summary>
    [Fact]
    public void MultipleValuesInSingleValueRequiredFields()
    {
        SIPRequest Req = DoRequestExpectedSuccess("multi01.dat");
        Assert.NotNull(Req);
    }

    /// <summary>
    /// Torture test 3.3.9 with file mcl01.dat.
    /// Multiple conflicting Content-Length header field values appear in
    /// this request.
    /// </summary>
    [Fact]
    public void MultipleContentLengthValues()
    {
        // Should be able to parse this message as if it were received over
        // UDP
        SIPRequest Req = DoRequestExpectedSuccess("mcl01.dat");
        Assert.NotNull(Req);

        // Note: This message will cause problems if received over a
        // TCP or TLS streaming connection
    }

    /// <summary>
    /// Torture test 3.3.10 with file bcast.dat.
    /// This message is a response with a 2nd Via header field value’s sentby
    /// containing 255.255.255.255. The message is well formed; parsers
    /// must not fail when receiving it.
    /// </summary>
    [Fact]
    public void OKResponseWithBroadcastViaHeaderField()
    {
        SIPResponse Res = DoResponseExpectSuccess("bcast.dat");
        Assert.NotNull(Res);
    }

    /// <summary>
    /// Torture test 3.3.11 with file zeromf.dat.
    /// This is a legal SIP request with the Max-Forwards header field value
    /// set to zero.
    /// </summary>
    [Fact]
    public void MaxForwardsOfZero()
    {
        SIPRequest Req = DoRequestExpectedSuccess("zeromf.dat");
        Assert.NotNull(Req);
    }

    /// <summary>
    /// Torture test 3.3.12 with file cparam01.dat.
    /// This register request contains a contact where the ’unknownparam’
    /// parameter must be interpreted as a contact-param and not a url-param.
    [Fact]
    public void REGISTERWithContactHeaderParameter()
    {
        SIPRequest Req = DoRequestExpectedSuccess("cparam01.dat");
        Assert.NotNull(Req);

        Assert.True(Req.Header.Contact[0].ContactParameters.Has("unknownparam"));
    }

    /// <summary>
    /// Torture test 3.3.13 with file cparam02.dat.
    /// This register request contains a contact where the URI has an unknown
    /// parameter.
    /// </summary>
    [Fact]
    public void REGISTERWithAUrlParameter()
    {
        SIPRequest Req = DoRequestExpectedSuccess("cparam02.dat");
        Assert.NotNull(Req);

        Assert.NotNull(Req.Header.Contact);
        Assert.True(Req.Header.Contact.Count == 1);
        Assert.True(Req.Header.Contact[0].ContactURI.Parameters.Has("unknownparam"));
    }

    /// <summary>
    /// Torture test 3.3.14 with file regescrt.dat.
    /// This register request contains a contact where the URI has an escaped
    /// header.
    /// </summary>
    [Fact]
    public void REGISTERWithAURLEscapedHeader()
    {
        SIPRequest Req = DoRequestExpectedSuccess("regescrt.dat");
        Assert.NotNull(Req);

        Assert.NotNull(Req.Header.Contact);
        Assert.True(Req.Header.Contact.Count == 1);
        Assert.True(Req.Header.Contact[0].ContactURI.Headers.Has("Route"));
        string strRoute = Req.Header.Contact[0].ContactURI.Headers.Get("Route");
        Assert.NotNull(strRoute);
        // Just verify that it is escaped
        Assert.True(strRoute.IndexOf("%") >= 0);
    }

    /// <summary>
    /// Torture test 3.3.15 with file sdp01.dat.
    /// This request indicates that the response must contain a body in an
    /// unknown type.
    /// </summary>
    [Fact]
    public void UnacceptableAcceptOffering()
    {
        SIPRequest Req = DoRequestExpectedSuccess("sdp01.dat");
        Assert.NotNull(Req);
        // Really don't care about this case because if the INVITE
        // contains an SDP body, most UAs will ignore the Accept header and respond with an SDP
        // body.
    }

    /// <summary>
    /// Torture test 3.3.16 with file inv2543.dat.
    /// This is a legal message per RFC 2543 (and several bis versions) that
    /// should be accepted by RFC 3261 elements that want to maintain
    /// backwards compatibility.
    /// </summary>
    [Fact]
    public void INVITEWithRFC2543Syntax()
    {
        // Should be able to at least parse an RFC 2543 INVITE
        DoRequestExpectedSuccess("inv2543.dat");
    }


    //rj2: RFC5118 (SIP) Torture Test Messages for IPv6
    //https://tools.ietf.org/html/rfc5118

    /// <summary>
    /// 4.1.  Valid SIP Message with an IPv6 Reference
    /// The request below is well-formatted according to the grammar in
    /// [RFC3261].  An IPv6 reference appears in the Request-URI(R-URI), Via
    /// header field, and Contact header field.
    /// </summary>
    [Fact]
    [Trait("Category", "IPv6Torture")]
    public void RFC5118_4_1()
    {
        string sipMsg =
            "REGISTER sip:[2001:db8::10] SIP/2.0" + CRLF +
            "To: sip:user@example.com" + CRLF +
            "From: sip:user@example.com;tag=81x2" + CRLF +
            "Via: SIP/2.0/UDP [2001:db8::9:1];branch=z9hG4bKas3-111" + CRLF +
            "Call-ID: SSG9559905523997077@hlau_4100" + CRLF +
            "Max-Forwards: 70" + CRLF +
            "Contact: \"Caller\" <sip:caller@[2001:db8::1]>" + CRLF +
            "CSeq: 98176 REGISTER" + CRLF +
            "Content-Length: 0";

        SIPMessage sipMessageBuffer = SIPMessage.ParseSIPMessage(Encoding.UTF8.GetBytes(sipMsg), null, null);
        Assert.True(sipMessageBuffer != null, "The SIP message not parsed correctly.");
        SIPRequest sipRequest = SIPRequest.ParseSIPRequest(sipMessageBuffer);
        Assert.Equal(SIPMethodsEnum.REGISTER, sipRequest.Method);
        IPAddress ip6;
        Assert.NotEmpty(sipRequest.Header.Vias.Via);
        Assert.True(IPAddress.TryParse(sipRequest.Header.Vias.TopViaHeader.ReceivedFromAddress, out ip6));
        Assert.Equal(AddressFamily.InterNetworkV6, ip6.AddressFamily);
        Assert.NotEmpty(sipRequest.Header.Contact);
        Assert.True(IPAddress.TryParse(sipRequest.Header.Contact[0].ContactURI.HostAddress, out ip6));
        Assert.Equal(AddressFamily.InterNetworkV6, ip6.AddressFamily);
        Assert.True(IPAddress.TryParse(sipRequest.URI.HostAddress, out ip6));
        Assert.Equal(AddressFamily.InterNetworkV6, ip6.AddressFamily);
    }

    /// <summary>
    /// 4.2.  Invalid SIP Message with an IPv6 Reference
    /// The request below is not well-formatted according to the grammar in
    /// [RFC3261].  The IPv6 reference in the R-URI does not contain the
    /// mandated delimiters for an IPv6 reference("[" and "]").
    /// A SIP implementation receiving this request should respond with a 400
    /// Bad Request error.
    /// </summary>
    [Fact]
    [Trait("Category", "IPv6Torture")]
    public void RFC5118_4_2()
    {
        string sipMsg =
            "REGISTER sip:2001:db8::10 SIP/2.0" + CRLF +
            "To: sip:user@example.com" + CRLF +
            "From: sip:user@example.com;tag=81x2" + CRLF +
            "Via: SIP/2.0/UDP [2001:db8::9:1];branch=z9hG4bKas3-111" + CRLF +
            "Call-ID: SSG9559905523997077@hlau_4100" + CRLF +
            "Max-Forwards: 70" + CRLF +
            "Contact: \"Caller\" <sip:caller@[2001:db8::1]>" + CRLF +
            "CSeq: 98176 REGISTER" + CRLF +
            "Content-Length: 0";

        SIPMessage sipMessageBuffer = SIPMessage.ParseSIPMessage(Encoding.UTF8.GetBytes(sipMsg), null, null);
        Assert.True(sipMessageBuffer != null, "The SIP message not parsed correctly.");
        Assert.Throws<SIPValidationException>(() => SIPRequest.ParseSIPRequest(sipMessageBuffer));
    }

    /// <summary>
    /// 4.3.  Port Ambiguous in a SIP URI
    /// From a parsing perspective, the request below is well-formed.
    /// However, from a semantic point of view, it will not yield the desired
    /// result.Implementations must ensure that when a raw IPv6 address
    /// appears in a SIP URI, then a port number, if required, appears
    /// outside the closing "]" delimiting the IPv6 reference.  Raw IPv6
    /// addresses can occur in many header fields, including the Contact,
    /// Route, and Record-Route header fields.They also can appear as the
    /// result of the "sent-by" production rule of the Via header field.
    /// Implementers are urged to consult the ABNF in [RFC3261] for a
    /// complete list of fields where a SIP URI can appear.
    /// </summary>
    [Fact]
    [Trait("Category", "IPv6Torture")]
    public void RFC5118_4_3()
    {
        string sipMsg =
            "REGISTER sip:[2001:db8::10:5070] SIP/2.0" + CRLF +
            "To: sip:user@example.com" + CRLF +
            "From: sip:user@example.com;tag=81x2" + CRLF +
            "Via: SIP/2.0/UDP [2001:db8::9:1];branch=z9hG4bKas3-111" + CRLF +
            "Call-ID: SSG9559905523997077@hlau_4100" + CRLF +
            "Contact: \"Caller\" <sip:caller@[2001:db8::1]>" + CRLF +
            "Max-Forwards: 70" + CRLF +
            "CSeq: 98176 REGISTER" + CRLF +
            "Content-Length: 0";

        //parsing is correct, but port is ambiguous, 
        //intention was to target port 5070
        //but that's nothing a program can find out

        SIPMessage sipMessageBuffer = SIPMessage.ParseSIPMessage(Encoding.UTF8.GetBytes(sipMsg), null, null);
        Assert.True(sipMessageBuffer != null, "The SIP message not parsed correctly.");
        SIPRequest sipRequest = SIPRequest.ParseSIPRequest(sipMessageBuffer);
        Assert.Equal(SIPMethodsEnum.REGISTER, sipRequest.Method);
        IPAddress ip6;
        Assert.NotEmpty(sipRequest.Header.Vias.Via);
        Assert.True(IPAddress.TryParse(sipRequest.Header.Vias.TopViaHeader.ReceivedFromAddress, out ip6));
        Assert.Equal(AddressFamily.InterNetworkV6, ip6.AddressFamily);
        Assert.NotEmpty(sipRequest.Header.Contact);
        Assert.True(IPAddress.TryParse(sipRequest.Header.Contact[0].ContactURI.HostAddress, out ip6));
        Assert.Equal(AddressFamily.InterNetworkV6, ip6.AddressFamily);
        Assert.True(IPAddress.TryParse(sipRequest.URI.HostAddress, out ip6));
        Assert.Equal(AddressFamily.InterNetworkV6, ip6.AddressFamily);
    }

    /// <summary>
    /// 4.4.  Port Unambiguous in a SIP URI
    /// In contrast to the example in Section 4.3, the following REGISTER
    /// request leaves no ambiguity whatsoever on where the IPv6 address ends
    /// and the port number begins.This REGISTER request is well formatted
    /// per the grammar in [RFC3261].
    /// </summary>
    [Fact]
    [Trait("Category", "IPv6Torture")]
    public void RFC5118_4_4()
    {
        string sipMsg =
            "REGISTER sip:[2001:db8::10]:5070 SIP/2.0" + CRLF +
            "To: sip:user@example.com" + CRLF +
            "From: sip:user@example.com;tag=81x2" + CRLF +
            "Via: SIP/2.0/UDP [2001:db8::9:1];branch=z9hG4bKas3-111" + CRLF +
            "Call-ID: SSG9559905523997077@hlau_4100" + CRLF +
            "Contact: \"Caller\" <sip:caller@[2001:db8::1]>" + CRLF +
            "Max-Forwards: 70" + CRLF +
            "CSeq: 98176 REGISTER" + CRLF +
            "Content-Length: 0";


        SIPMessage sipMessageBuffer = SIPMessage.ParseSIPMessage(Encoding.UTF8.GetBytes(sipMsg), null, null);
        Assert.True(sipMessageBuffer != null, "The SIP message not parsed correctly.");
        SIPRequest sipRequest = SIPRequest.ParseSIPRequest(sipMessageBuffer);
        Assert.Equal("5070", sipRequest.URI.HostPort);
        Assert.Equal(SIPMethodsEnum.REGISTER, sipRequest.Method);
        IPAddress ip6;
        Assert.NotEmpty(sipRequest.Header.Vias.Via);
        Assert.True(IPAddress.TryParse(sipRequest.Header.Vias.TopViaHeader.ReceivedFromAddress, out ip6));
        Assert.Equal(AddressFamily.InterNetworkV6, ip6.AddressFamily);
        Assert.NotEmpty(sipRequest.Header.Contact);
        Assert.True(IPAddress.TryParse(sipRequest.Header.Contact[0].ContactURI.HostAddress, out ip6));
        Assert.Equal(AddressFamily.InterNetworkV6, ip6.AddressFamily);
        Assert.True(IPAddress.TryParse(sipRequest.URI.HostAddress, out ip6));
        Assert.Equal(AddressFamily.InterNetworkV6, ip6.AddressFamily);
    }

    /// <summary>
    /// 4.5.  IPv6 Reference Delimiters in Via Header
    /// The request below contains an IPv6 address in the Via "received"
    /// parameter.The IPv6 address is delimited by "[" and "]".  Even
    /// though this is not a valid request based on a strict interpretation
    /// of the grammar in [RFC3261], robust implementations must nonetheless
    /// be able to parse the topmost Via header field and continue processing
    /// the request.
    /// </summary>
    [Fact]
    [Trait("Category", "IPv6Torture")]
    public void RFC5118_4_5_1()
    {
        string sipMsg =
            "BYE sip:[2001:db8::10] SIP/2.0" + CRLF +
            "To: sip:user@example.com;tag=bd76ya" + CRLF +
            "From: sip:user@example.com;tag=81x2" + CRLF +
            "Via: SIP/2.0/UDP [2001:db8::9:1];received=[2001:db8::9:255];branch=z9hG4bKas3-111" + CRLF +
            "Call-ID: SSG9559905523997077@hlau_4100" + CRLF +
            "Max-Forwards: 70" + CRLF +
            "CSeq: 321 BYE" + CRLF +
            "Content-Length: 0";

        SIPMessage sipMessageBuffer = SIPMessage.ParseSIPMessage(Encoding.UTF8.GetBytes(sipMsg), null, null);
        Assert.True(sipMessageBuffer != null, "The SIP message not parsed correctly.");
        SIPRequest sipRequest = SIPRequest.ParseSIPRequest(sipMessageBuffer);
        Assert.Equal(SIPMethodsEnum.BYE, sipRequest.Method);
        IPAddress ip6;
        Assert.NotEmpty(sipRequest.Header.Vias.Via);
        Assert.True(IPAddress.TryParse(sipRequest.Header.Vias.TopViaHeader.ReceivedFromAddress, out ip6));
        Assert.Equal(AddressFamily.InterNetworkV6, ip6.AddressFamily);
        Assert.True(IPAddress.TryParse(sipRequest.Header.Vias.TopViaHeader.ReceivedFromIPAddress, out ip6));
        Assert.Equal(AddressFamily.InterNetworkV6, ip6.AddressFamily);
        Assert.True(IPAddress.TryParse(sipRequest.URI.HostAddress, out ip6));
        Assert.Equal(AddressFamily.InterNetworkV6, ip6.AddressFamily);
    }

    /// <summary>
    /// 4.5.  IPv6 Reference Delimiters in Via Header
    /// The OPTIONS request below contains an IPv6 address in the Via
    /// "received" parameter without the adorning "[" and "]".  This request
    /// is valid according to the grammar in [RFC3261].
    /// </summary>
    [Fact]
    [Trait("Category", "IPv6Torture")]
    public void RFC5118_4_5_2()
    {
        string sipMsg =
            "OPTIONS sip:[2001:db8::10] SIP/2.0" + CRLF +
            "To: sip:user @example.com" + CRLF +
            "From: sip:user @example.com; tag=81x2" + CRLF +
            "Via: SIP/2.0/UDP [2001:db8::9:1];received=2001:db8::9:255;branch=z9hG4bKas3" + CRLF +
            "Call-ID: SSG95523997077 @hlau_4100" + CRLF +
            "Max-Forwards: 70" + CRLF +
            "Contact: \"Caller\" <sip:caller@[2001:db8::9:1]>" + CRLF +
            "CSeq: 921 OPTIONS" + CRLF +
            "Content-Length: 0";

        SIPMessage sipMessageBuffer = SIPMessage.ParseSIPMessage(Encoding.UTF8.GetBytes(sipMsg), null, null);
        Assert.True(sipMessageBuffer != null, "The SIP message not parsed correctly.");
        SIPRequest sipRequest = SIPRequest.ParseSIPRequest(sipMessageBuffer);
        Assert.Equal(SIPMethodsEnum.OPTIONS, sipRequest.Method);
        IPAddress ip6;
        Assert.NotEmpty(sipRequest.Header.Vias.Via);
        Assert.True(IPAddress.TryParse(sipRequest.Header.Vias.TopViaHeader.ReceivedFromAddress, out ip6));
        Assert.Equal(AddressFamily.InterNetworkV6, ip6.AddressFamily);
        Assert.True(IPAddress.TryParse(sipRequest.Header.Vias.TopViaHeader.ReceivedFromIPAddress, out ip6));
        Assert.Equal(AddressFamily.InterNetworkV6, ip6.AddressFamily);
        Assert.NotEmpty(sipRequest.Header.Contact);
        Assert.True(IPAddress.TryParse(sipRequest.Header.Contact[0].ContactURI.HostAddress, out ip6));
        Assert.Equal(AddressFamily.InterNetworkV6, ip6.AddressFamily);
        Assert.True(IPAddress.TryParse(sipRequest.URI.HostAddress, out ip6));
        Assert.Equal(AddressFamily.InterNetworkV6, ip6.AddressFamily);
    }

    /// <summary>
    /// 4.6.  SIP Request with IPv6 Addresses in Session Description Protocol
    /// This request below is valid and well-formed according to the grammar
    /// in [RFC3261].  Note that the IPv6 addresses in the SDP[RFC4566] body
    /// do not have the delimiting "[" and "]".
    /// </summary>
    [Fact]
    [Trait("Category", "IPv6Torture")]
    public void RFC5118_4_6()
    {
        string sipMsg =
            "INVITE sip:user@[2001:db8::10] SIP/2.0" + CRLF +
            "To: sip:user@[2001:db8::10]" + CRLF +
            "From: sip:user@example.com;tag=81x2" + CRLF +
            "Via: SIP/2.0/UDP [2001:db8::20];branch=z9hG4bKas3-111" + CRLF +
            "Call-ID: SSG9559905523997077@hlau_4100" + CRLF +
            "Contact: \"Caller\" <sip:caller@[2001:db8::20]>" + CRLF +
            "CSeq: 8612 INVITE" + CRLF +
            "Max-Forwards: 70" + CRLF +
            "Content-Type: application/sdp" + CRLF +
            "Content-Length: 268" + CRLF +
            CRLF +
            "v=0" + CRLF +
            "o=assistant 971731711378798081 0 IN IP6 2001:db8::20" + CRLF +
            "s=Live video feed for today's meeting" + CRLF +
            "c=IN IP6 2001:db8::20" + CRLF +
            "t=3338481189 3370017201" + CRLF +
            "m=audio 6000 RTP/AVP 2" + CRLF +
            "a=rtpmap:2 G726-32/8000" + CRLF +
            "m=video 6024 RTP/AVP 107" + CRLF +
            "a=rtpmap:107 H263-1998/90000";

        SIPMessage sipMessageBuffer = SIPMessage.ParseSIPMessage(Encoding.UTF8.GetBytes(sipMsg), null, null);
        Assert.True(sipMessageBuffer != null, "The SIP message not parsed correctly.");
        SIPRequest sipRequest = SIPRequest.ParseSIPRequest(sipMessageBuffer);
        Assert.Equal(SIPMethodsEnum.INVITE, sipRequest.Method);
        IPAddress ip6;
        Assert.NotEmpty(sipRequest.Header.Vias.Via);
        Assert.True(IPAddress.TryParse(sipRequest.Header.Vias.TopViaHeader.ReceivedFromAddress, out ip6));
        Assert.Equal(AddressFamily.InterNetworkV6, ip6.AddressFamily);
        Assert.NotEmpty(sipRequest.Header.Contact);
        Assert.True(IPAddress.TryParse(sipRequest.Header.Contact[0].ContactURI.HostAddress, out ip6));
        Assert.Equal(AddressFamily.InterNetworkV6, ip6.AddressFamily);
        Assert.True(IPAddress.TryParse(sipRequest.URI.HostAddress, out ip6));
        Assert.Equal(AddressFamily.InterNetworkV6, ip6.AddressFamily);
        Assert.False(string.IsNullOrWhiteSpace(sipRequest.Body));
        SipLib.Sdp.Sdp sdp = SipLib.Sdp.Sdp.ParseSDP(sipRequest.Body);

        Assert.NotNull(sdp);
        Assert.NotNull(sdp.ConnectionData);
        Assert.Equal(AddressFamily.InterNetworkV6, ip6.AddressFamily);
        Assert.NotEmpty(sdp.Media);
    }

    /// <summary>
    /// 4.7.  Multiple IP Addresses in SIP Headers
    /// The request below is valid and well-formed according to the grammar
    /// in [RFC3261].  The Via list contains a mix of IPv4 addresses and IPv6
    /// references.
    /// </summary>
    [Fact]
    [Trait("Category", "IPv6Torture")]
    public void RFC5118_4_7()
    {
        string sipMsg =
            "BYE sip:user@host.example.net SIP/2.0" + CRLF +
            "Via: SIP/2.0/UDP [2001:db8::9:1]:6050;branch=z9hG4bKas3-111" + CRLF +
            "Via: SIP/2.0/UDP 192.0.2.1;branch=z9hG4bKjhja8781hjuaij65144" + CRLF +
            "Via: SIP/2.0/TCP [2001:db8::9:255];branch=z9hG4bK451jj;received=192.0.2.200" + CRLF +
            "Call-ID: 997077@lau_4100" + CRLF +
            "Max-Forwards: 70" + CRLF +
            "CSeq: 89187 BYE" + CRLF +
            "To: sip:user@example.net;tag=9817--94" + CRLF +
            "From: sip:user@example.com;tag=81x2" + CRLF +
            "Content-Length: 0";

        SIPMessage sipMessageBuffer = SIPMessage.ParseSIPMessage(Encoding.UTF8.GetBytes(sipMsg), null, null);
        Assert.True(sipMessageBuffer != null, "The SIP message not parsed correctly.");
        SIPRequest sipRequest = SIPRequest.ParseSIPRequest(sipMessageBuffer);
        Assert.Equal(SIPMethodsEnum.BYE, sipRequest.Method);
        IPAddress ip6, ip4;
        Assert.NotEmpty(sipRequest.Header.Vias.Via);
        Assert.True(sipRequest.Header.Vias.Length == 3);
        Assert.True(IPAddress.TryParse(sipRequest.Header.Vias.TopViaHeader.ReceivedFromAddress, out ip6));
        Assert.Equal(AddressFamily.InterNetworkV6, ip6.AddressFamily);
        Assert.Equal(SIPProtocolsEnum.udp, sipRequest.Header.Vias.TopViaHeader.Transport);
        Assert.Equal(6050, sipRequest.Header.Vias.TopViaHeader.Port);
        Assert.True(IPAddress.TryParse(sipRequest.Header.Vias.BottomViaHeader.Host, out ip6));
        Assert.Equal(AddressFamily.InterNetworkV6, ip6.AddressFamily);
        Assert.True(IPAddress.TryParse(sipRequest.Header.Vias.BottomViaHeader.ReceivedFromIPAddress, out ip4));
        Assert.Equal(AddressFamily.InterNetwork, ip4.AddressFamily);
        Assert.Equal(SIPProtocolsEnum.tcp, sipRequest.Header.Vias.BottomViaHeader.Transport);
        sipRequest.Header.Vias.PopTopViaHeader();
        Assert.True(IPAddress.TryParse(sipRequest.Header.Vias.TopViaHeader.ReceivedFromAddress, out ip4));
        Assert.Equal(AddressFamily.InterNetwork, ip4.AddressFamily);
        Assert.Equal(SIPProtocolsEnum.udp, sipRequest.Header.Vias.TopViaHeader.Transport);
        Assert.False(IPAddress.TryParse(sipRequest.URI.HostAddress, out ip6));
    }

    /// <summary>
    /// 4.8.  Multiple IP Addresses in SDP
    /// The request below is valid and well-formed according to the grammar
    /// in [RFC3261].  The SDP contains multiple media lines, and each media
    /// line is identified by a different network connection address.
    /// </summary>
    [Fact]
    [Trait("Category", "IPv6Torture")]
    public void RFC5118_4_8()
    {
        string sipMsg =
            "INVITE sip:user@[2001:db8::10] SIP/2.0" + CRLF +
            "To: sip:user@[2001:db8::10]" + CRLF +
            "From: sip:user@example.com;tag=81x2" + CRLF +
            "Via: SIP/2.0/UDP [2001:db8::9:1];branch=z9hG4bKas3-111" + CRLF +
            "Call-ID: SSG9559905523997077@hlau_4100" + CRLF +
            "Contact: \"Caller\" <sip:caller@[2001:db8::9:1]>" + CRLF +
            "Max-Forwards: 70" + CRLF +
            "CSeq: 8912 INVITE" + CRLF +
            "Content-Type: application/sdp" + CRLF +
            "Content-Length: 181" + CRLF +
            CRLF +
            "v=0" + CRLF +
            "o=bob 280744730 28977631 IN IP4 host.example.com" + CRLF +
            "s=" + CRLF +
            "t=0 0" + CRLF +
            "m=audio 22334 RTP/AVP 0" + CRLF +
            "c=IN IP4 192.0.2.1" + CRLF +
            "m=video 6024 RTP/AVP 107" + CRLF +
            "c=IN IP6 2001:db8::1" + CRLF +
            "a=rtpmap:107 H263-1998/90000";

        SIPMessage sipMessageBuffer = SIPMessage.ParseSIPMessage(Encoding.UTF8.GetBytes(sipMsg), null, null);
        Assert.True(sipMessageBuffer != null, "The SIP message not parsed correctly.");
        SIPRequest sipRequest = SIPRequest.ParseSIPRequest(sipMessageBuffer);
        Assert.Equal(SIPMethodsEnum.INVITE, sipRequest.Method);
        IPAddress ip6;
        Assert.NotEmpty(sipRequest.Header.Vias.Via);
        Assert.True(IPAddress.TryParse(sipRequest.Header.Vias.TopViaHeader.ReceivedFromAddress, out ip6));
        Assert.Equal(AddressFamily.InterNetworkV6, ip6.AddressFamily);
        Assert.NotEmpty(sipRequest.Header.Contact);
        Assert.True(IPAddress.TryParse(sipRequest.Header.Contact[0].ContactURI.HostAddress, out ip6));
        Assert.Equal(AddressFamily.InterNetworkV6, ip6.AddressFamily);
        Assert.True(IPAddress.TryParse(sipRequest.URI.HostAddress, out ip6));
        Assert.Equal(AddressFamily.InterNetworkV6, ip6.AddressFamily);
        Assert.False(string.IsNullOrWhiteSpace(sipRequest.Body));
        Sdp sdp = Sdp.ParseSDP(sipRequest.Body);
        Assert.NotNull(sdp);
        //Assert.NotNull(sdp.Connection);
        //Assert.True(IPAddress.TryParse(sdp.Connection.ConnectionAddress, out ip4));
        //Assert.Equal(AddressFamily.InterNetwork, ip4.AddressFamily);
        Assert.NotEmpty(sdp.Media);
        Assert.NotNull(sdp.Media[0].ConnectionData);
        Assert.Equal(AddressFamily.InterNetwork, sdp.Media[0].ConnectionData.Address.AddressFamily);
        Assert.NotNull(sdp.Media[1].ConnectionData);
        Assert.Equal(AddressFamily.InterNetworkV6, sdp.Media[1].ConnectionData.Address.AddressFamily);
    }

    /// <summary>
    /// 4.9.  IPv4-Mapped IPv6 Addresses
    /// The message below is well-formed according to the grammar in
    /// [RFC3261].  The Via list contains two Via headers, both of which
    /// include an IPv4-mapped IPv6 address.An IPv4-mapped IPv6 address
    /// also appears in the Contact header and the SDP.The topmost Via
    /// header includes a port number that is appropriately delimited by "]".
    /// </summary>
    [Fact]
    [Trait("Category", "IPv6Torture")]
    public void RFC5118_4_9()
    {
        string sipMsg =
            "INVITE sip:user@example.com SIP/2.0" + CRLF +
            "To: sip:user@example.com" + CRLF +
            "From: sip:user@east.example.com;tag=81x2" + CRLF +
            "Via: SIP/2.0/UDP [::ffff:192.0.2.10]:19823;branch=z9hG4bKbh19" + CRLF +
            "Via: SIP/2.0/UDP [::ffff:192.0.2.2];branch=z9hG4bKas3-111" + CRLF +
            "Call-ID: SSG9559905523997077@hlau_4100" + CRLF +
            "Contact: \"T. desk phone\" <sip:ted@[::ffff:192.0.2.2]>" + CRLF +
            "CSeq: 612 INVITE" + CRLF +
            "Max-Forwards: 70" + CRLF +
            "Content-Type: application/sdp" + CRLF +
            "Content-Length: 236" + CRLF +
            CRLF +
            "v=0" + CRLF +
            "o=assistant 971731711378798081 0 IN IP6 ::ffff:192.0.2.2" + CRLF +
            "s=Call me soon, please!" + CRLF +
            "c=IN IP6 ::ffff:192.0.2.2" + CRLF +
            "t=3338481189 3370017201" + CRLF +
            "m=audio 6000 RTP/AVP 2" + CRLF +
            "a=rtpmap:2 G726-32/8000" + CRLF +
            "m=video 6024 RTP/AVP 107" + CRLF +
            "a=rtpmap:107 H263-1998/90000";

        SIPMessage sipMessageBuffer = SIPMessage.ParseSIPMessage(Encoding.UTF8.GetBytes(sipMsg), null, null);
        Assert.True(sipMessageBuffer != null, "The SIP message not parsed correctly.");
        SIPRequest sipRequest = SIPRequest.ParseSIPRequest(sipMessageBuffer);
        Assert.Equal(SIPMethodsEnum.INVITE, sipRequest.Method);
        IPAddress ip6;
        Assert.NotEmpty(sipRequest.Header.Vias.Via);
        Assert.True(IPAddress.TryParse(sipRequest.Header.Vias.TopViaHeader.Host, out ip6));
        Assert.Equal(AddressFamily.InterNetworkV6, ip6.AddressFamily);
        Assert.Equal(19823, sipRequest.Header.Vias.TopViaHeader.Port);
        Assert.True(IPAddress.TryParse(sipRequest.Header.Vias.BottomViaHeader.ReceivedFromAddress, out ip6));
        Assert.Equal(AddressFamily.InterNetworkV6, ip6.AddressFamily);
        Assert.NotEmpty(sipRequest.Header.Contact);
        Assert.True(IPAddress.TryParse(sipRequest.Header.Contact[0].ContactURI.HostAddress, out ip6));
        Assert.Equal(AddressFamily.InterNetworkV6, ip6.AddressFamily);
        Assert.False(IPAddress.TryParse(sipRequest.URI.HostAddress, out ip6));
        Assert.False(string.IsNullOrWhiteSpace(sipRequest.Body));
        SipLib.Sdp.Sdp sdp = SipLib.Sdp.Sdp.ParseSDP(sipRequest.Body);
        Assert.NotNull(sdp);
        Assert.NotNull(sdp.ConnectionData);
        Assert.Equal(AddressFamily.InterNetworkV6, sdp.ConnectionData.Address.AddressFamily);
        Assert.NotEmpty(sdp.Media);
    }

    /// <summary>
    /// 4.10.  IPv6 Reference Bug in RFC 3261 ABNF
    /// The message below includes an extra colon in the IPv6 reference.  A
    /// SIP implementation receiving such a message may exhibit robustness by
    /// successfully parsing the IPv6 reference(it can choose to ignore the
    /// extra colon when parsing the IPv6 reference.If the SIP
    /// implementation is acting in the role of a proxy, it may additionally
    /// serialize the message without the extra colon to aid the next
    /// downstream server).
    /// </summary>
    [Fact]
    [Trait("Category", "IPv6Torture")]
    public void RFC5118_4_10_1()
    {
        string sipMsg =
            "OPTIONS sip:user@[2001:db8:::192.0.2.1] SIP/2.0" + CRLF +
            "To: sip:user@[2001:db8:::192.0.2.1]" + CRLF +
            "From: sip:user@example.com;tag=810x2" + CRLF +
            "Via: SIP/2.0/UDP lab1.east.example.com;branch=z9hG4bKas3-111" + CRLF +
            "Call-ID: G9559905523997077@hlau_4100" + CRLF +
            "CSeq: 689 OPTIONS" + CRLF +
            "Max-Forwards: 70" + CRLF +
            "Content-Length: 0";

        SIPMessage sipMessageBuffer = SIPMessage.ParseSIPMessage(Encoding.UTF8.GetBytes(sipMsg), null, null);
        Assert.True(sipMessageBuffer != null, "The SIP message not parsed correctly.");
        SIPRequest sipRequest = SIPRequest.ParseSIPRequest(sipMessageBuffer);
        Assert.Equal(SIPMethodsEnum.OPTIONS, sipRequest.Method);
        IPAddress ip6;
        Assert.NotEmpty(sipRequest.Header.Vias.Via);
        Assert.False(IPAddress.TryParse(sipRequest.Header.Vias.TopViaHeader.ReceivedFromAddress, out ip6));
        Assert.True(IPAddress.TryParse(sipRequest.URI.HostAddress, out ip6));
        Assert.Equal(AddressFamily.InterNetworkV6, ip6.AddressFamily);
    }

    /// <summary>
    /// 4.10.  IPv6 Reference Bug in RFC 3261 ABNF
    /// The next message has the correct syntax for the IPv6 reference in the
    /// R-URI.
    /// </summary>
    [Fact]
    [Trait("Category", "IPv6Torture")]
    public void RFC5118_4_10_2()
    {
        string sipMsg =
            "OPTIONS sip:user@[2001:db8::192.0.2.1] SIP/2.0" + CRLF +
            "To: sip:user@[2001:db8::192.0.2.1]" + CRLF +
            "From: sip:user@example.com;tag=810x2" + CRLF +
            "Via: SIP/2.0/UDP lab1.east.example.com;branch=z9hG4bKas3-111" + CRLF +
            "Call-ID: G9559905523997077@hlau_4100" + CRLF +
            "CSeq: 689 OPTIONS" + CRLF +
            "Max-Forwards: 70" + CRLF +
            "Content-Length: 0";

        SIPMessage sipMessageBuffer = SIPMessage.ParseSIPMessage(Encoding.UTF8.GetBytes(sipMsg), null, null);
        Assert.True(sipMessageBuffer != null, "The SIP message not parsed correctly.");
        SIPRequest sipRequest = SIPRequest.ParseSIPRequest(sipMessageBuffer);
        Assert.Equal(SIPMethodsEnum.OPTIONS, sipRequest.Method);
        IPAddress ip6;
        Assert.NotEmpty(sipRequest.Header.Vias.Via);
        Assert.False(IPAddress.TryParse(sipRequest.Header.Vias.TopViaHeader.ReceivedFromAddress, out ip6));
        Assert.True(IPAddress.TryParse(sipRequest.URI.HostAddress, out ip6));
        Assert.Equal(AddressFamily.InterNetworkV6, ip6.AddressFamily);
    }
}
