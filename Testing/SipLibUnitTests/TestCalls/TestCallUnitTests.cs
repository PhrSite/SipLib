/////////////////////////////////////////////////////////////////////////////////////
//  File:   TestCallUnitTests.cs                                    17 Apr 25 PHR
/////////////////////////////////////////////////////////////////////////////////////

using SipLib.Sdp;
using SipLib.Core;
using SipLib.TestCalls;

namespace SipLibUnitTests.TestCalls;

[Trait("Category", "unit")]
public class TestCallUnitTests
{
    public TestCallUnitTests(Xunit.Abstractions.ITestOutputHelper output)
    {
    }

    private static string CRLF = "\r\n";

    private static string ValidTestCallInvite =
        "INVITE urn:service:test.sos SIP/2.0" + CRLF +
        "Via: SIP/2.0/UDP 192.168.1.86:5060;branch=z9hG4bK-11455-1-0" + CRLF +
        "From: sipp <sip:sipp@192.168.1.86:5060>;tag=1" + CRLF +
        "To: sut <sip:service@192.168.1.84:5060>" + CRLF +
        "Call-ID: 1-11455@192.168.1.86" + CRLF +
        "Max-Forwards: 70" + CRLF +
        "Cseq: 1 INVITE" + CRLF +
        "Contact: sip:sipp@192.168.1.86:5060" + CRLF +
        "Content-Type: application/sdp" + CRLF +
        "Content-Length: 185" + CRLF + CRLF +
        "v=0" + CRLF +
        "o=user1 53655765 2353687637 IN IP4 192.168.1.86" + CRLF +
        "s=-" + CRLF +
        "t=0 0" + CRLF +
        "c=IN IP4 192.168.1.86" + CRLF +
        "m=audio 6000 RTP/AVP 0" + CRLF +
        "a=rtpmap:0 PCMU/8000" + CRLF +
        "a=loopback:rtp-media-loopback" + CRLF +
        "a=loopback-source";

    private SIPRequest ParseRequest(string strRequest)
    {
        SIPRequest request = null;
        try
        {
            request =  SIPRequest.ParseSIPRequest(strRequest);
        }
        catch
        {
            return null;
        }

        return request;
    }

    [Fact]
    public void IsTestCall()
    {
        SIPRequest invite = ParseRequest(ValidTestCallInvite);
        Assert.True(invite != null, "Invalid SIPRequest");
        bool IsTestCall = IncomingTestCall.IsNg911TestCall(invite);
        Assert.True(IsTestCall == true, "IsNg911TestCall returned false");
    }

    [Fact]
    public void TestCallIsValid()
    {
        SIPRequest invite = ParseRequest(ValidTestCallInvite);
        Assert.True(invite != null, "Invalid SIPRequest");
        bool IsValidTestCall = IncomingTestCall.TestCallIsValid(invite);
        Assert.True(IsValidTestCall == true, "TestCallIsValid returned false");
    }

    private static string NotATestCall =
        "INVITE urn:service:sos SIP/2.0" + CRLF +
        "Via: SIP/2.0/UDP 192.168.1.86:5060;branch=z9hG4bK-11455-1-0" + CRLF +
        "From: sipp <sip:sipp@192.168.1.86:5060>;tag=1" + CRLF +
        "To: sut <sip:service@192.168.1.84:5060>" + CRLF +
        "Call-ID: 1-11455@192.168.1.86" + CRLF +
        "Max-Forwards: 70" + CRLF +
        "Cseq: 1 INVITE" + CRLF +
        "Contact: sip:sipp@192.168.1.86:5060" + CRLF +
        "Content-Type: application/sdp" + CRLF +
        "Content-Length: 185" + CRLF + CRLF +
        "v=0" + CRLF +
        "o=user1 53655765 2353687637 IN IP4 192.168.1.86" + CRLF +
        "s=-" + CRLF +
        "t=0 0" + CRLF +
        "c=IN IP4 192.168.1.86" + CRLF +
        "m=audio 6000 RTP/AVP 0" + CRLF +
        "a=rtpmap:0 PCMU/8000" + CRLF +
        "a=loopback:rtp-media-loopback" + CRLF +
        "a=loopback-source";

    [Fact]
    public void NonTestCall()
    {
        SIPRequest invite = ParseRequest(NotATestCall);
        Assert.True(invite != null, "Invalid SIPRequest");
        bool IsTestCall = IncomingTestCall.IsNg911TestCall(invite);
        Assert.True(IsTestCall == false, "IsNg911TestCall returned true");
    }

    private static string LoopbackMirror =
        "INVITE urn:service:test.sos SIP/2.0" + CRLF +
        "Via: SIP/2.0/UDP 192.168.1.86:5060;branch=z9hG4bK-11455-1-0" + CRLF +
        "From: sipp <sip:sipp@192.168.1.86:5060>;tag=1" + CRLF +
        "To: sut <sip:service@192.168.1.84:5060>" + CRLF +
        "Call-ID: 1-11455@192.168.1.86" + CRLF +
        "Max-Forwards: 70" + CRLF +
        "Cseq: 1 INVITE" + CRLF +
        "Contact: sip:sipp@192.168.1.86:5060" + CRLF +
        "Content-Type: application/sdp" + CRLF +
        "Content-Length: 185" + CRLF + CRLF +
        "v=0" + CRLF +
        "o=user1 53655765 2353687637 IN IP4 192.168.1.86" + CRLF +
        "s=-" + CRLF +
        "t=0 0" + CRLF +
        "c=IN IP4 192.168.1.86" + CRLF +
        "m=audio 6000 RTP/AVP 0" + CRLF +
        "a=rtpmap:0 PCMU/8000" + CRLF +
        "a=loopback:rtp-media-loopback" + CRLF +
        "a=loopback-mirror";

    [Fact]
    public void IsMirror()
    {
        SIPRequest invite = ParseRequest(LoopbackMirror);
        Assert.True(invite != null, "Invalid SIPRequest");
        bool IsValidTestCall = IncomingTestCall.TestCallIsValid(invite);
        Assert.True(IsValidTestCall == false, "TestCallIsValid returned true, should be false");
    }

    private static string strMissingLoopbackAttribute =
        "INVITE urn:service:test.sos SIP/2.0" + CRLF +
        "Via: SIP/2.0/UDP 192.168.1.86:5060;branch=z9hG4bK-11455-1-0" + CRLF +
        "From: sipp <sip:sipp@192.168.1.86:5060>;tag=1" + CRLF +
        "To: sut <sip:service@192.168.1.84:5060>" + CRLF +
        "Call-ID: 1-11455@192.168.1.86" + CRLF +
        "Max-Forwards: 70" + CRLF +
        "Cseq: 1 INVITE" + CRLF +
        "Contact: sip:sipp@192.168.1.86:5060" + CRLF +
        "Content-Type: application/sdp" + CRLF +
        "Content-Length: 185" + CRLF + CRLF +
        "v=0" + CRLF +
        "o=user1 53655765 2353687637 IN IP4 192.168.1.86" + CRLF +
        "s=-" + CRLF +
        "t=0 0" + CRLF +
        "c=IN IP4 192.168.1.86" + CRLF +
        "m=audio 6000 RTP/AVP 0" + CRLF +
        "a=rtpmap:0 PCMU/8000" + CRLF +
        "a=loopback-source";

    [Fact]
    public void MissingLoopbackAttribute()
    {
        SIPRequest invite = ParseRequest(strMissingLoopbackAttribute);
        Assert.True(invite != null, "Invalid SIPRequest");
        bool IsValidTestCall = IncomingTestCall.TestCallIsValid(invite);
        Assert.True(IsValidTestCall == false, "TestCallIsValid returned true, should be false");
    }

    private static string strMissingSourceOrMirror =
        "INVITE urn:service:test.sos SIP/2.0" + CRLF +
        "Via: SIP/2.0/UDP 192.168.1.86:5060;branch=z9hG4bK-11455-1-0" + CRLF +
        "From: sipp <sip:sipp@192.168.1.86:5060>;tag=1" + CRLF +
        "To: sut <sip:service@192.168.1.84:5060>" + CRLF +
        "Call-ID: 1-11455@192.168.1.86" + CRLF +
        "Max-Forwards: 70" + CRLF +
        "Cseq: 1 INVITE" + CRLF +
        "Contact: sip:sipp@192.168.1.86:5060" + CRLF +
        "Content-Type: application/sdp" + CRLF +
        "Content-Length: 185" + CRLF + CRLF +
        "v=0" + CRLF +
        "o=user1 53655765 2353687637 IN IP4 192.168.1.86" + CRLF +
        "s=-" + CRLF +
        "t=0 0" + CRLF +
        "c=IN IP4 192.168.1.86" + CRLF +
        "m=audio 6000 RTP/AVP 0" + CRLF +
        "a=rtpmap:0 PCMU/8000" + CRLF +
        "a=loopback-source";

    [Fact]
    public void MissingSourceOrMirror()
    {
        SIPRequest invite = ParseRequest(strMissingSourceOrMirror);
        Assert.True(invite != null, "Invalid SIPRequest");
        bool IsValidTestCall = IncomingTestCall.TestCallIsValid(invite);
        Assert.True(IsValidTestCall == false, "TestCallIsValid returned true, should be false");
    }

    private static string strNoMedia =
        "INVITE urn:service:test.sos SIP/2.0" + CRLF +
        "Via: SIP/2.0/UDP 192.168.1.86:5060;branch=z9hG4bK-11455-1-0" + CRLF +
        "From: sipp <sip:sipp@192.168.1.86:5060>;tag=1" + CRLF +
        "To: sut <sip:service@192.168.1.84:5060>" + CRLF +
        "Call-ID: 1-11455@192.168.1.86" + CRLF +
        "Max-Forwards: 70" + CRLF +
        "Cseq: 1 INVITE" + CRLF +
        "Contact: sip:sipp@192.168.1.86:5060" + CRLF +
        "Content-Type: application/sdp" + CRLF +
        "Content-Length: 185" + CRLF + CRLF +
        "v=0" + CRLF +
        "o=user1 53655765 2353687637 IN IP4 192.168.1.86" + CRLF +
        "s=-" + CRLF +
        "t=0 0" + CRLF +
        "c=IN IP4 192.168.1.86";

    [Fact]
    public void NoMediaBlock()
    {
        SIPRequest invite = ParseRequest(strNoMedia);
        Assert.True(invite != null, "Invalid SIPRequest");
        bool IsValidTestCall = IncomingTestCall.TestCallIsValid(invite);
        Assert.True(IsValidTestCall == false, "TestCallIsValid returned true, should be false");
    }
}
