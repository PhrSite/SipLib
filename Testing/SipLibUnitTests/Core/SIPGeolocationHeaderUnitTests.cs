/////////////////////////////////////////////////////////////////////////////////////
//  File:   SIPGeolocationHeaderUnitTests.cs                        13 Nov 22 PHR
/////////////////////////////////////////////////////////////////////////////////////

using SipLib.Core;
using static System.Net.Mime.MediaTypeNames;
using System.ComponentModel;
using System.Reflection.Metadata;
using System.Runtime.Intrinsics.Arm;

namespace SipLibUnitTests
{
    /// <summary>
    /// Unit tests for the Geolocation header
    /// </summary>
    [Trait("Category", "unit")]
    public class SIPGeolocationHeaderUnitTests
    {
        public SIPGeolocationHeaderUnitTests(Xunit.Abstractions.ITestOutputHelper output)
        {
        }

        private static string CRLF = "\r\n";

        /// <summary>
        /// Tests the ability to parse a Geolocation with a cid URI scheme
        /// </summary>
        [Fact]
        public void BasicCidHeader()
        {
            string strHeaderVal = "cid:target123@atlanta.example.com";
            List<SIPGeolocationHeader> Glh = SIPGeolocationHeader.ParseGeolocationHeader(strHeaderVal);
            Assert.NotNull(Glh);
            Assert.True(Glh.Count == 1);
            SIPGeolocationHeader Gl = Glh[0];
            Assert.True(Gl.GeolocationField.URI.Scheme == SIPSchemesEnum.cid, "The scheme is incorrect.");
            Assert.True(Gl.GeolocationField.URI.Host == "atlanta.example.com", "The URI host " +
                "name is incorrect.");
            Assert.True(Gl.GeolocationField.URI.User == "target123", "The URI user name is " +
                "incorrect.");
        }

        /// <summary>
        /// Tests the ability to parse a Geolocation with a http URI scheme
        /// </summary>
        [Fact]
        public void BasicHttpHeader()
        {
            string strHeaderVal = "http:target123@atlanta.example.com";
            List<SIPGeolocationHeader> Glh = SIPGeolocationHeader.ParseGeolocationHeader(strHeaderVal);
            Assert.NotNull(Glh);
            Assert.True(Glh.Count == 1);
            SIPGeolocationHeader Gl = Glh[0];
            Assert.True(Gl.GeolocationField.URI.Scheme == SIPSchemesEnum.http, "The scheme is incorrect.");
            Assert.True(Gl.GeolocationField.URI.Host == "atlanta.example.com", "The URI host " +
                "name is incorrect.");
            Assert.True(Gl.GeolocationField.URI.User == "target123", "The URI user name is " +
                "incorrect.");
        }

        /// <summary>
        /// Tests the ability to parse a Geolocation with a https URI scheme
        /// </summary>
        [Fact]
        public void BasicHttpsHeader()
        {
            string strHeaderVal = "https:target123@atlanta.example.com";
            List<SIPGeolocationHeader> Glh = SIPGeolocationHeader.ParseGeolocationHeader(strHeaderVal);
            Assert.NotNull(Glh);
            Assert.True(Glh.Count == 1);
            SIPGeolocationHeader Gl = Glh[0];
            Assert.True(Gl.GeolocationField.URI.Scheme == SIPSchemesEnum.https, "The scheme is incorrect.");
            Assert.True(Gl.GeolocationField.URI.Host == "atlanta.example.com", "The URI host " +
                "name is incorrect.");
            Assert.True(Gl.GeolocationField.URI.User == "target123", "The URI user name is " +
                "incorrect.");
        }

        /// <summary>
        /// Tests the ability to parse a Geolocation with a sip URI scheme
        /// </summary>
        [Fact]
        public void BasicSipHeader()
        {
            string strHeaderVal = "sip:target123@atlanta.example.com";
            List<SIPGeolocationHeader> Glh = SIPGeolocationHeader.ParseGeolocationHeader(strHeaderVal);
            Assert.NotNull(Glh);
            Assert.True(Glh.Count == 1);
            SIPGeolocationHeader Gl = Glh[0];
            Assert.True(Gl.GeolocationField.URI.Scheme == SIPSchemesEnum.sip, "The scheme is incorrect.");
            Assert.True(Gl.GeolocationField.URI.Host == "atlanta.example.com", "The URI host " +
                "name is incorrect.");
            Assert.True(Gl.GeolocationField.URI.User == "target123", "The URI user name is " +
                "incorrect.");
        }

        /// <summary>
        /// Tests the ability to parse a Geolocation with a sip URI scheme
        /// </summary>
        [Fact]
        public void BasicSipsHeader()
        {
            string strHeaderVal = "sips:target123@atlanta.example.com";
            List<SIPGeolocationHeader> Glh = SIPGeolocationHeader.ParseGeolocationHeader(strHeaderVal);
            Assert.NotNull(Glh);
            Assert.True(Glh.Count == 1);
            SIPGeolocationHeader Gl = Glh[0];
            Assert.True(Gl.GeolocationField.URI.Scheme == SIPSchemesEnum.sips, "The scheme is incorrect.");
            Assert.True(Gl.GeolocationField.URI.Host == "atlanta.example.com", "The URI host " +
                "name is incorrect.");
            Assert.True(Gl.GeolocationField.URI.User == "target123", "The URI user name is " +
                "incorrect.");
        }

        private static string strCidInHeader =
            "INVITE sips:bob @biloxi.example.com SIP/2.0" + CRLF +
            "Via: SIPS/2.0/TLS pc33.atlanta.example.com; branch=z9hG4bK74bf9" + CRLF +
            "Max-Forwards: 70" + CRLF +
            "To: Bob<sips:bob@biloxi.example.com>" + CRLF +
            "From: Alice<sips:alice@atlanta.example.com>;tag=9fxced76sl" + CRLF +
            "Call-ID: 3848276298220188511@atlanta.example.com" + CRLF +
            "Geolocation: <cid:target123@atlanta.example.com>" + CRLF +
            "Geolocation-Routing: no" + CRLF +
            "Accept: application/sdp, application/pidf+xml" + CRLF +
            "CSeq: 31862 INVITE" + CRLF +
            "Contact: <sips:alice@atlanta.example.com>" + CRLF +
            "Content-Length: 0";    // Don't care about the body

        /// <summary>
        /// Tests the parsing of a SIP INVITE request with a Geolocation header with
        /// a cid URI scheme.
        /// </summary>
        [Fact]
        public void CidInAHeader()
        {
            SIPRequest Req = SIPRequest.ParseSIPRequest(strCidInHeader);
            Assert.NotNull(Req);
            Assert.NotNull(Req.Header?.Geolocation);
            Assert.True(Req.Header.Geolocation.Count == 1, "The number of Geolocation headers +" +
                "is incorrect");
        }

        private static string strMultiGeoHeaders =
            "INVITE sips:bob@biloxi.example.com SIP/2.0" + CRLF +
            "Via: SIPS/2.0/TLS pc33.atlanta.example.com; branch=z9hG4bK74bf9" + CRLF +
            "Max-Forwards: 70" + CRLF +
            "To: Bob<sips:bob@biloxi.example.com>" + CRLF +
            "From: Alice<sips:alice@atlanta.example.com>;tag=9fxced76sl" + CRLF +
            "Call-ID: 3848276298220188511@atlanta.example.com" + CRLF +
            "Geolocation: <cid:target123@atlanta.example.com>, <https:target123@atlanta.example.com>" + CRLF +
            "Geolocation: <sip:target123@atlanta.example.com>" + CRLF +
            "Geolocation-Error: 200" + CRLF +
            "Geolocation-Routing: yes" + CRLF +
            "Accept: application/sdp, application/pidf+xml" + CRLF +
            "CSeq: 31862 INVITE" + CRLF +
            "Contact: <sips:alice@atlanta.example.com>" + CRLF +
            "Content-Length: 0";    // Don't care about the body

        /// <summary>
        /// This case tests the ability to parse multiple Geolocation headers in an
        /// INVITE request. Two are in the same header line and one is on a separate
        /// Geolocation header line.
        /// </summary>
        [Fact]
        public void MultipleGeolocationHeaders()
        {
            SIPRequest Req = SIPRequest.ParseSIPRequest(strMultiGeoHeaders);
            Assert.NotNull(Req);
            Assert.NotNull(Req.Header?.Geolocation);
            Assert.True(Req.Header.Geolocation.Count == 3, "The number of Geolocation headers +" +
                "is incorrect");
            Assert.True(Req.Header.Geolocation[0].GeolocationField.URI.Scheme == SIPSchemesEnum.cid,
                "The first Geolocation URI scheme is incorrect");
            Assert.True(Req.Header.Geolocation[1].GeolocationField.URI.Scheme == SIPSchemesEnum.https,
                "The second Geolocation URI scheme is incorrect");
            Assert.True(Req.Header.Geolocation[2].GeolocationField.URI.Scheme == SIPSchemesEnum.sip,
                "The third Geolocation URI scheme is incorrect.");
        }

        /// <summary>
        /// Tests the parsing of a Geolocation-Routing header.
        /// </summary>
        [Fact]
        public void GeolocationRouting()
        {
            SIPRequest Req = SIPRequest.ParseSIPRequest(strMultiGeoHeaders);
            Assert.NotNull(Req);
            Assert.NotNull(Req.Header.GeolocationRouting);
            Assert.True(Req.Header.GeolocationRouting == "yes", "The Geolocation-Header value " +
                "is incorrect.");
        }

        /// <summary>
        /// Tests the parsing of a Geolocation-Error header.
        /// </summary>
        [Fact]
        public void GeolocationError()
        {
            SIPRequest Req = SIPRequest.ParseSIPRequest(strMultiGeoHeaders);
            Assert.NotNull(Req);
            Assert.NotNull(Req.Header.GeolocationError);
            Assert.True(Req.Header.GeolocationError == "200", "The Geolocation-Error value " +
                "is incorrect.");
        }
    }
}
