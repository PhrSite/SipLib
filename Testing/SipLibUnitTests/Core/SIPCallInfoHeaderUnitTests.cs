/////////////////////////////////////////////////////////////////////////////////////
//  File: SIPCallInfoHeaderUnitTests.cs                             14 Nov 22 PHR
/////////////////////////////////////////////////////////////////////////////////////

using SipLib.Core;

namespace SipLibUnitTests
{
    /// <summary>
    /// Unit tests for the Call-Info header
    /// </summary>
    [Trait("Category", "unit")]
    public class SIPCallInfoHeaderUnitTests
    {
        public SIPCallInfoHeaderUnitTests(Xunit.Abstractions.ITestOutputHelper output)
        {
        }

        private static string CRLF = "\r\n";

        [Fact]
        public void BasicHttpHeader()
        {
            string strHeader = "<http://www.example.com/alice/photo.jpg>;purpose=icon";
            List<SIPCallInfoHeader> Cih = SIPCallInfoHeader.ParseCallInfoHeader(strHeader);
            Assert.NotNull(Cih);
            Assert.True(Cih.Count == 1);
            SIPURI Uri = Cih[0].CallInfoField.URI;
            Assert.True(Uri.Scheme == SIPSchemesEnum.http, "The URI scheme is incorrect");
            Assert.True(Uri.Host == "//www.example.com/alice/photo.jpg", "The host is incorrect");
            Assert.True(Cih[0].CallInfoField.Parameters.Has("purpose") == true, "The header " +
                "has no purpose parameter");
            string strPur = Cih[0].CallInfoField.Parameters.Get("purpose");
            Assert.True(strPur == "icon", "The purpose parameter is incorrect");
        }

        [Fact]
        public void TwoBasicHttpHeadersSameLine()
        {
            string strHeader = "<http://www.example.com/alice/photo.jpg>;" +
                "purpose=icon,<http://www.example.com/alice/> ;purpose=info";
            List<SIPCallInfoHeader> Cih = SIPCallInfoHeader.ParseCallInfoHeader(strHeader);
            Assert.NotNull(Cih);
            Assert.True(Cih.Count == 2, "The header count is incorrect");
            SIPURI Uri = Cih[0].CallInfoField.URI;
            Assert.True(Uri.Scheme == SIPSchemesEnum.http, "The first URI scheme is incorrect");
            Assert.True(Uri.Host == "//www.example.com/alice/photo.jpg", "The first host is " +
                "incorrect");
            Assert.True(Cih[0].CallInfoField.Parameters.Has("purpose") == true, "The first " +
                "header has no purpose parameter");
            string strPur = Cih[0].CallInfoField.Parameters.Get("purpose");
            Assert.True(strPur == "icon", "The first header purpose parameter is incorrect");
            Uri = Cih[1].CallInfoField.URI;
            Assert.True(Uri.Scheme == SIPSchemesEnum.http, "The second URI scheme is incorrect");
            Assert.True(Uri.Host == "//www.example.com/alice/", "The second hos is incorrect");
            Assert.True(Cih[1].CallInfoField.Parameters.Has("purpose") == true, "The second " +
                "header has no purpose parameter");
            strPur = Cih[1].CallInfoField.Parameters.Get("purpose");
            Assert.True(strPur == "info", "The second header purpose parameter is incorrect");
        }

        /// <summary>
        /// See Section 7 of RFC 7852.
        /// Contains folded header lines
        /// </summary>
        private string InviteWithCallInfo =
            "INVITE urn:service:sos SIP/2.0" + CRLF +
            "Via: SIPS/2.0/TLS server.example.com; branch=z9hG4bK74bf9" + CRLF +
            "Max-Forwards: 70" + CRLF +
            "To: <urn:service:sos>" + CRLF +
            "From: Hannes Tschofenig<sips:hannes @example.com>; tag=9fxced76sl" + CRLF +
            "Call-ID: 3848276298220188511@example.com" + CRLF +
            "Call-Info: <http://wwww.example.com/hannes/photo.jpg>;purpose=icon," +
            " <http://www.example.com/hannes/> ;purpose=info," + CRLF +
            "Call-Info: <cid:1234567890@atlanta.example.com>;purpose=EmergencyCallData.ProviderInfo," + CRLF +
            "Call-Info: <cid:0123456789@atlanta.example.com>" +
            " ;purpose=EmergencyCallData.DeviceInfo" + CRLF +
            "Geolocation: <https://ls.example.net:9768/357yc6s64ceyoiuy5ax3o>" + CRLF +
            "Geolocation-Routing: yes" + CRLF +
            "Accept: application/sdp, application/pidf+xml," +
            " application/EmergencyCallData.ProviderInfo+xml" + CRLF +
            "CSeq: 31862 INVITE" + CRLF +
            "Contact: <sips:hannes @example.com>" + CRLF +
            "Content-Type: multipart/mixed; boundary=boundary1" + CRLF +
            "Content-Length: 0" + CRLF;

        [Fact]
        public void TestCallInfoWithLineFolding()
        {
            SIPRequest Req = SIPRequest.ParseSIPRequest(InviteWithCallInfo);
            Assert.NotNull(Req);
            List<SIPCallInfoHeader> Cih = Req.Header.CallInfo;
            Assert.True(Cih.Count == 4, "The number of Call-Info headers is incorrect");
            Assert.True(Cih[2].CallInfoField.URI.Scheme == SIPSchemesEnum.cid, "The third " +
                "URI scheme is incorrect");
            Assert.True(Cih[2].CallInfoField.Parameters.Get("purpose") ==
                "EmergencyCallData.ProviderInfo", "The purpose parameter of the third " +
                "header is incomplete");
        }
    }
}
