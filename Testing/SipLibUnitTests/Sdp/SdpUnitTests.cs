//////////////////////////////////////////////////////////////////////////////////////
//  File: SdpUnitTests.cs                                           21 Nov 22 PHR
//////////////////////////////////////////////////////////////////////////////////////

namespace SipLibUnitTests.Sdp; 
using System.Net;
using SipLib.Sdp;

[Trait("Category", "unit")]
public class SdpUnitTests
{
    public SdpUnitTests(Xunit.Abstractions.ITestOutputHelper output)
    {
    }

    private const string CRLF = "\r\n";
    private const string AudioVideoSdp =
        "v=0" + CRLF +
        "o=jdoe 2890844526 1 IN IP4 10.47.16.5" + CRLF +
        "s=SDP Seminar" + CRLF +
        "i=Session Information" + CRLF +
        "u=http://www.example.com/seminars/sdp.pdf" + CRLF +
        "e=j.doe@example.com (Jane Doe)" + CRLF +
        "p=1 818 555 3333" + CRLF +
        "b=CT:500" + CRLF +
        "c=IN IP4 224.2.17.12/127" + CRLF +
        "t=2873397496 2873404696" + CRLF +
        "a=recvonly" + CRLF +
        "m=audio 49170 RTP/AVP 0" + CRLF +
        "b=AS:64" + CRLF +
        "m=video 51372 RTP/AVP 99" + CRLF +
        "a=rtpmap:99 h263-1998/90000" + CRLF;

    [Fact]
    public void TestBasicParsing()
    {
        Sdp sdp = Sdp.ParseSDP(AudioVideoSdp);
        Assert.NotNull(sdp);

        Assert.True(sdp.Version == 0, "The SDP version number is incorrect");
        Assert.True(sdp.SessionName == "SDP Seminar", "The SDP session name is incorrect");
        Assert.True(sdp.SessionInformation == "Session Information", "The session information " +
            "is incorrect");
        Assert.True(sdp.Uri == "http://www.example.com/seminars/sdp.pdf", "The URI parameter is " +
            "incorrect");
        Assert.True(sdp.Email == "j.doe@example.com (Jane Doe)", "The Email parameter is incorrect");
        Assert.True(sdp.PhoneNumber == "1 818 555 3333", "The phone number parameter is incorrect");
        Assert.True(sdp.Timing == "2873397496 2873404696", "The timing parameter is incorrect");
        Assert.True(sdp.Bandwidth == "CT:500", "The bandwidth parameter is incorrect");

        Assert.NotNull(sdp.ConnectionData);
        Assert.True(sdp.ConnectionData.Address.ToString() == "224.2.17.12", "The c= address " +
            "is incorrect");

        Assert.True(sdp.Media.Count == 2, "Incorrect number of media blocks");
        Assert.True(sdp.Media[0].MediaType == "audio", "The first media type is incorrect");
        Assert.True(sdp.Media[0].Bandwidth == "AS:64", "The first media bandwidth is incorrect");
        Assert.True(sdp.Media[1].MediaType == "video", "The second media type is incorrect");
    }

    [Fact]
    public void TestConstructor()
    {
        Sdp sdp = new Sdp(IPAddress.Parse("10.22.54.1"), "Ua1");
        Assert.True(sdp.ConnectionData != null, "The ConnectionData object is null");
        Assert.True(sdp.ConnectionData.Address.ToString() == "10.22.54.1", "The ConnectionData +" +
            "IP address is incorrect");
        Assert.True(sdp.Origin.UserName == "Ua1", "The Origin UserName is incorrect");
        Assert.True(sdp.Timing == "0 0", "The t= parameter is incorrect");
        Assert.True(sdp.Media != null, "The Media element is null");
        Assert.True(sdp.Media.Count == 0, "The media count is not 0");
    }

    [Fact]
    public void TestCreateCopy()
    {
        Sdp sdp1 = Sdp.ParseSDP(AudioVideoSdp);
        Sdp sdp2 = sdp1.CreateCopy();

        Assert.True(sdp2.Version == 0, "The SDP version number is incorrect");
        Assert.True(sdp2.SessionName == "SDP Seminar", "The SDP session name is incorrect");
        Assert.True(sdp2.SessionInformation == "Session Information", "The session information " +
            "is incorrect");
        Assert.True(sdp2.Uri == "http://www.example.com/seminars/sdp.pdf", "The URI parameter is " +
            "incorrect");
        Assert.True(sdp2.Email == "j.doe@example.com (Jane Doe)", "The Email parameter is incorrect");
        Assert.True(sdp2.PhoneNumber == "1 818 555 3333", "The phone number parameter is incorrect");
        Assert.True(sdp2.Timing == "2873397496 2873404696", "The timing parameter is incorrect");
        Assert.True(sdp2.Bandwidth == "CT:500", "The bandwidth parameter is incorrect");

        Assert.NotNull(sdp2.ConnectionData);
        Assert.True(sdp2.ConnectionData.Address.ToString() == "224.2.17.12", "The c= address is incorrect");

        Assert.True(sdp2.Media.Count == 2, "Incorrect number of media blocks");
        Assert.True(sdp2.Media[0].MediaType == "audio", "The first media type is incorrect");
        Assert.True(sdp2.Media[0].Bandwidth == "AS:64", "The first media bandwidth is incorrect");
        Assert.True(sdp2.Media[1].MediaType == "video", "The second media type is incorrect");
    }

    private const string InvalidSdp =
        "v=0" + CRLF +
        "o=jdoe 2890844526 1 IN IP4 " + CRLF +  // Missing the IP Address
        "s=SDP Seminar" + CRLF +
        "i=Session Information" + CRLF +
        "u=http://www.example.com/seminars/sdp.pdf" + CRLF +
        "e=j.doe@example.com (Jane Doe)" + CRLF +
        "p=1 818 555 3333" + CRLF +
        "b=CT:500" + CRLF +
        "c=IN IP4 224.2.17.12/127" + CRLF +
        "t=2873397496 2873404696" + CRLF +
        "a=recvonly" + CRLF +
        "m=audio 49170 RTP/AVP 0" + CRLF +
        "b=AS:64" + CRLF +
        "m=video 51372 RTP/AVP 99" + CRLF +
        "a=rtpmap:99 h263-1998/90000" + CRLF;

    [Fact]
    public void TestInvalidSdp()
    {
        Assert.Throws<ArgumentException>(() => Sdp.ParseSDP(InvalidSdp));
    }

    [Fact]
    public void TestGetMediaType()
    {
        Sdp sdp1 = Sdp.ParseSDP(AudioVideoSdp);
        MediaDescription AudioMd = sdp1.GetMediaType("audio");
        Assert.True(AudioMd != null, "Failed to get the audio media");
        Assert.True(AudioMd.MediaType == "audio", "The AudioMd media type is incorrect");
        Assert.True(AudioMd.Port == 49170, "The AudioMd port is incorrect");
        MediaDescription VideoMd = sdp1.GetMediaType("video");
        Assert.True(VideoMd != null, "Failed to get the video media type");

        MediaDescription TextMd = sdp1.GetMediaType("text");
        Assert.True(TextMd == null, "The text media type is not null");
    }

    [Fact]
    public void TestGetMediaIndex()
    {
        Sdp sdp1 = Sdp.ParseSDP(AudioVideoSdp);
        int AudioIdx = sdp1.GetMediaTypeIndex("audio");
        Assert.True(AudioIdx == 0, "The audio media type index is incorrect");
        int VideoIdx = sdp1.GetMediaTypeIndex("video");
        Assert.True(VideoIdx == 1, "The video media type index is incorrect");
        int TextIdx = sdp1.GetMediaTypeIndex("text");
        Assert.True(TextIdx == -1, "The text media type index is incorrect");
    }

    [Fact]
    public void TestHasMultiMedia()
    {
        Sdp sdp1 = Sdp.ParseSDP(AudioVideoSdp);
        Assert.True(sdp1.HasMultiMedia() == true, "HasMultiMedia failed");
    }

    [Fact]
    public void TestGetAudioConnectionData()
    {
        Sdp sdp1 = Sdp.ParseSDP(AudioVideoSdp);
        string strIpAddress = null;
        int Port = 0;
        Assert.True(sdp1.GetAudioConnectionData(ref strIpAddress, ref Port) == true,
            "GetAudioConnectionData() failed");
        Assert.True(strIpAddress == "224.2.17.12", "The IP address is incorrect");
        Assert.True(Port == 49170, "The port number is incorrect");
    }

    [Fact]
    public void TestGetNamedAttribute()
    {
        Sdp sdp1 = Sdp.ParseSDP(AudioVideoSdp);
        SdpAttribute Attr = sdp1.GetNamedAttribute("recvonly");
        Assert.NotNull(Attr);
        Assert.True(Attr.Attribute == "recvonly");
        Attr = sdp1.GetNamedAttribute("Unknown");
        Assert.True(Attr == null, "GetNamedAttribute return a non-null value");
    }

    private const string MultipleConnectionDataLines =
        "v=0" + CRLF +
        "o=jdoe 2890844526 1 IN IP4 10.47.16.5" + CRLF +
        "s=SDP Seminar" + CRLF +
        "i=Session Information" + CRLF +
        "c=IN IP4 10.3.4.5" + CRLF +
        "t=0 0" + CRLF +
        "a=recvonly" + CRLF +
        "m=audio 49170 RTP/AVP 0" + CRLF +
        "c=IN IP4 10.2.3.4" + CRLF +
        "b=AS:64" + CRLF +
        "m=video 51372 RTP/AVP 99" + CRLF +
        "a=rtpmap:99 h263-1998/90000" + CRLF;

    [Fact]
    public void TestGetMediaIp()
    {
        Sdp sdp = Sdp.ParseSDP(MultipleConnectionDataLines);
        Assert.NotNull(sdp);

        IPAddress AudioIp = Sdp.GetMediaIPAddr(sdp, sdp.GetMediaType("audio"));
        Assert.True(AudioIp != null, "The AudioIp is null");
        Assert.True(AudioIp.ToString() == "10.2.3.4", "The audio IP address is incorrect");

        IPAddress VideoIp = Sdp.GetMediaIPAddr(sdp, sdp.GetMediaType("video"));
        Assert.True(VideoIp != null, "The VideoIp is null");
        Assert.True(VideoIp.ToString() == "10.3.4.5", "The video IP address is incorrect");
    }

    [Fact]
    public void TestGetMediaEndPoint()
    {
        Sdp sdp = Sdp.ParseSDP(MultipleConnectionDataLines);
        Assert.NotNull(sdp);
        IPEndPoint AudioIpe = Sdp.GetMediaEndPoint(sdp, sdp.GetMediaType("audio"));
        Assert.NotNull(AudioIpe);
        Assert.True(AudioIpe.ToString() == "10.2.3.4:49170", "The audio endpoint is incorrect");
    }
}
