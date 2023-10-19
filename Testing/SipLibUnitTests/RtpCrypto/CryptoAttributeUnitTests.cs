/////////////////////////////////////////////////////////////////////////////////////
//  File:   CryptoAttributeUnitTests.cs                             23 Sep 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLibUnitTests.RtpCrypto;
using SipLib.RtpCrypto;
using SipLib.Sdp;

[Trait("Category", "unit")]
public class CryptoAttributeUnitTests
{
    private const string CRLF = "\r\n";

    [Fact]
    public void AudioAndVideoCryptoAttributes()
    {
        // See page 8 of RFC 4568
        string strSdp =
            "v=0" + CRLF +
            "o=jdoe 2890844526 2890842807 IN IP4 10.47.16.5" + CRLF +
            "s=SDP Seminar" + CRLF +
            "i=A Seminar on the session description protocol" + CRLF +
            "u = http://www.example.com/seminars/sdp.pdf" + CRLF +
            "e=j.doe @example.com(Jane Doe)" + CRLF +
            "c=IN IP4 161.44.17.12/127" + CRLF +
            "t=2873397496 2873404696" + CRLF +
            "m=video 51372 RTP/SAVP 31" + CRLF +
            "a=crypto:1 AES_CM_128_HMAC_SHA1_80 " +
            "inline:d0RmdmcmVCspeEc3QGZiNWpVLFJhQX1cfHAwJSoj|2^20|1:32 " + CRLF +
            "m=audio 49170 RTP/SAVP 0" + CRLF +
            "a=crypto:1 AES_CM_128_HMAC_SHA1_32 " +
            "inline:NzB4d1BINUAvLEw6UzF3WSJ+PSdFcGdUJShpX1Zj|2^20|1:32" + CRLF +
            "m=application 32416 udp wb" + CRLF +
            "a=orient:portrait" + CRLF;

        Sdp sdp = Sdp.ParseSDP(strSdp);
        Assert.NotNull(sdp);

        MediaDescription VideoMd = sdp.GetMediaType("video");
        Assert.NotNull(VideoMd);
        List<SdpAttribute> Attrs = VideoMd.GetNamedAttributes("crypto");
        Assert.True(Attrs.Count == 1, "Video crypto attribute count is wrong");
        CryptoAttribute VideoCrypto = CryptoAttribute.Parse(Attrs[0].Value);
        Assert.True(VideoCrypto != null, "VideoCrypto is null");
        Assert.True(VideoCrypto.CryptoSuite == CryptoSuites.AES_CM_128_HMAC_SHA1_80,
            "The video CryptoSuite is wrong");
        Assert.True(VideoCrypto.Tag == 1, "The VideoCrypto Tag is wrong");

        MediaDescription AudioMd = sdp.GetMediaType("audio");
        Assert.NotNull(AudioMd);
        Attrs = AudioMd.GetNamedAttributes("crypto");
        Assert.True(Attrs.Count == 1, "Audio crypto attribute count is wrong");
        CryptoAttribute AudioCrypto = CryptoAttribute.Parse(Attrs[0].Value);
        Assert.True(AudioCrypto != null, "AudioCrypto is null");
        Assert.True(AudioCrypto.CryptoSuite == CryptoSuites.AES_CM_128_HMAC_SHA1_32,
            "The audio CryptoSuite is wrong");
        Assert.True(AudioCrypto.Tag == 1, "The AudioCrypto Tag is wrong");
    }

    [Fact]
    public void MultipleCryptoAttributes()
    {
        // See page 27 of RFC 4568
        string strSdp =
            "v = 0" + CRLF +
            "o=sam 2890844526 2890842807 IN IP4 10.47.16.5" + CRLF +
            "s=SRTP Discussion" + CRLF +
            "i=A discussion of Secure RTP" + CRLF +
            "u = http://www.example.com/seminars/srtp.pdf" + CRLF +
            "e=marge@example.com (Marge Simpson)" + CRLF +
            "c=IN IP4 168.2.17.12" + CRLF +
            "t=2873397496 2873404696" + CRLF +
            "m=audio 49170 RTP/SAVP 0" + CRLF +
            "a=crypto:1 AES_CM_128_HMAC_SHA1_80 " +
            "inline:WVNfX19zZW1jdGwgKCkgewkyMjA7fQp9CnVubGVz|2^20|1:4 " +
            "FEC_ORDER=FEC_SRTP" + CRLF +
            "a = crypto:2 F8_128_HMAC_SHA1_80 " +
            "inline:MTIzNDU2Nzg5QUJDREUwMTIzNDU2Nzg5QUJjZGVm|2^20|1:4;" +
            "inline:QUJjZGVmMTIzNDU2Nzg5QUJDREUwMTIzNDU2Nzg5|2^20|2:4 " +
            "FEC_ORDER=FEC_SRTP" + CRLF;

        Sdp sdp = Sdp.ParseSDP(strSdp);
        Assert.NotNull(sdp);
        MediaDescription AudioMd = sdp.GetMediaType("audio");
        Assert.True(AudioMd != null, "The AudioMd is null");
        List<SdpAttribute> Attrs = AudioMd.GetNamedAttributes("crypto");
        Assert.True(Attrs.Count == 2, "The Count is wrong");

        CryptoAttribute Ca0 = CryptoAttribute.Parse(Attrs[0].Value);
        Assert.True(Ca0 != null, "The first CryptoAttribute is null");
        Assert.True(Ca0.CryptoSuite == CryptoSuites.AES_CM_128_HMAC_SHA1_80, "The first CryptoSuite is wrong");
        Assert.True(Ca0.InlineParameters.Count == 1, "The first InlineParameters.Count is wrong");
        Assert.True(Ca0.FEC_ORDER == "FEC_SRTP", "The first FEC_Order is wrong");

        CryptoAttribute Ca1 = CryptoAttribute.Parse(Attrs[1].Value);
        Assert.True(Ca1 != null, "The second CryptoAttribute is null");
        Assert.True(Ca1.CryptoSuite == CryptoSuites.F8_128_HMAC_SHA1_80, "The second CryptoSuite is wrong");
        Assert.True(Ca1.InlineParameters.Count == 2, "The second InlineParameters.Count is wrong");
        Assert.True(Ca1.FEC_ORDER == "FEC_SRTP", "The first FEC_Order is wrong");
    }
}
