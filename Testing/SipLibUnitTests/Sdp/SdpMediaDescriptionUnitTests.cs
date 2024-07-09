//////////////////////////////////////////////////////////////////////////////////////
//  File: SdpMediaDescriptionUnitTests.cs                           19 Nov 22 PHR
//////////////////////////////////////////////////////////////////////////////////////

using SipLib.Media;
using SipLib.Sdp;
using System.Net;

namespace SipLibUnitTests.Sdp;

[Trait("Category", "unit")]
public class SdpMediaDescriptionUnitTests
{
    public SdpMediaDescriptionUnitTests(Xunit.Abstractions.ITestOutputHelper output)
    {
    }

    [Fact]
    public void TestBasicMediaDescriptionParsing()
    {
        MediaDescription Md = MediaDescription.ParseMediaDescriptionLine("audio 7000 RTP/AVP 0");
        Assert.True(Md.MediaType == "audio", "The MediaType is incorrect");
        Assert.True(Md.Port == 7000, "The port number is incorrect");
        Assert.True(Md.Transport == "RTP/AVP", "The transport type is incorrect");
        Assert.True(Md.PayloadTypes.Count == 1, "The number of format numbers is incorrect");
        Assert.True(Md.PayloadTypes[0] == 0, "The media format number is incorrect");
    }

    [Fact]
    public void TestMultipleMediaFormats()
    {
        MediaDescription Md = MediaDescription.ParseMediaDescriptionLine("audio 7000 RTP/AVP 0 8");
        Assert.True(Md.PayloadTypes.Count == 2, "The number of media format numbers " +
            "is incorrect");
        Assert.True(Md.PayloadTypes[0] == 0, "The first media format number is incorrect");
        Assert.True(Md.PayloadTypes[1] == 8, "The second media format number is incorrect");
    }

    [Fact]
    public void TestInvalidMediaDescription()
    {
        Assert.Throws<ArgumentException>(() => MediaDescription.ParseMediaDescriptionLine(
            "audio 7000 RTP/AVP"));
    }

    [Fact]
    public void TestCreateCopy()
    {
        MediaDescription Md1 = MediaDescription.ParseMediaDescriptionLine("audio 7000 RTP/AVP 0 8");
        MediaDescription Md2 = Md1.CreateCopy();
        Assert.True(Md1.MediaType == Md2.MediaType, "The media types do not match");
        Assert.True(Md1.Port == Md2.Port, "The port numbers do not match");
        Assert.True(Md1.Transport == Md2.Transport, "The Transport types do not match");
        Assert.True(Md1.PayloadTypes.Count == Md2.PayloadTypes.Count, "The " +
            "number of media format numbers do not match");
        for (int i= 0; i < Md1.PayloadTypes.Count; i++)
        {
            Assert.True(Md1.PayloadTypes[i] == Md2.PayloadTypes[i],
                $"The media format numbers at index {i} do not match");
        }

    }

    [Fact]
    public void TestParseMediaDescriptionString()
    {
        string strMediaBlock =
            "m=audio 49230 RTP/AVP 96 97 98\r\n" +
            "a=rtpmap:96 L8/8000\r\n" +
            "a=rtpmap:97 L16/8000\r\n" +
            "a=rtpmap:98 L16/11025/2";

        MediaDescription Md = MediaDescription.ParseMediaDescriptionString(strMediaBlock);
        Assert.NotNull(Md);
        Assert.True(Md.Port == 49230, "The Port is wrong");
        Assert.True(Md.PayloadTypes.Count == 3, "MediaFormatNumbers.Count is wrong");

        CheckRtpMap(Md, 96, "L8", 8000, 0);
        CheckRtpMap(Md, 97, "L16", 8000, 0);
        CheckRtpMap(Md, 98, "L16", 11025, 2);
    }

    private void CheckRtpMap(MediaDescription Md, int payloadType, string encodingName, int clockRate, int channels)
    {
        RtpMapAttribute rtpMapAttribute = Md.GetRtpMapForPayloadType(payloadType);
        Assert.True(rtpMapAttribute != null, $"Cannot find the rtpmap for PayloadType = {payloadType}");
        Assert.True(rtpMapAttribute.EncodingName == encodingName, $"EncodingName for PayloadType = {payloadType} is wrong");
        Assert.True(rtpMapAttribute.ClockRate == clockRate, $"ClockRate for codec type = {payloadType} is wrong");
        Assert.True(rtpMapAttribute.Channels == channels, $"Channels for codec type = {payloadType} is wrong");
    }

    [Fact]
    public void TestBuildAudioAnswerSdp()
    {
        List<string> SupportedAudioCodecs = new List<string>() {"PCMU", "PCMA" };
        List<string> SupportedVideoCodecs = new List<string>() { "H264" };
        MediaPortSettings portSettings = new MediaPortSettings();
        string fingerprint = "ab:cd:ef:12:34:56:78:90";     // A dummy value
        MediaPortManager Mpm = new MediaPortManager(portSettings);

        SdpAnswerSettings AnswerSettings = new SdpAnswerSettings(SupportedAudioCodecs, SupportedVideoCodecs,
            "TestServer", fingerprint, Mpm);

        // Creates a MediaDescription for audio with payload types of 0 and 101 (PCMU and telephone-event).
        SipLib.Sdp.Sdp OfferedSdp = SdpUtils.BuildSimpleAudioSdp(IPAddress.Parse("192.168.1.80"), 5000, "Remote");
        SipLib.Sdp.Sdp AnsweredSdp = SipLib.Sdp.Sdp.BuildAnswerSdp(OfferedSdp, IPAddress.Parse("192.168.1.79"),
            AnswerSettings);
        Assert.True(AnsweredSdp != null, "The AnsweredSdp is null");
        MediaDescription AnsAudioMd = AnsweredSdp.GetMediaType("audio");
        Assert.True(AnsAudioMd != null, "The answered audio media description is null");
        RtpMapAttribute AnsAudioRtpMapAttr = AnsAudioMd.GetRtpMapForPayloadType(0);
        Assert.True(AnsAudioRtpMapAttr != null, "The answered RtpMapAttribute for payload type 0 is null");

        RtpMapAttribute AnsTelEventRtpMap = AnsAudioMd.GetRtpMapForPayloadType(101);
        Assert.True(AnsTelEventRtpMap != null, "The answered RtpMapAttribute for payload type 101 is null");
    }
}
