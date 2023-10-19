//////////////////////////////////////////////////////////////////////////////////////
//  File: SdpMediaDescriptionUnitTests.cs                           19 Nov 22 PHR
//////////////////////////////////////////////////////////////////////////////////////

using SipLib.Sdp;

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
        MediaDescription Md = MediaDescription.ParseMediaDescription("audio 7000 RTP/AVP 0");
        Assert.True(Md.MediaType == "audio", "The MediaType is incorrect");
        Assert.True(Md.Port == 7000, "The port number is incorrect");
        Assert.True(Md.Transport == "RTP/AVP", "The transport type is incorrect");
        Assert.True(Md.MediaFormatNumbers.Count == 1, "The number of format numbers is incorrect");
        Assert.True(Md.MediaFormatNumbers[0] == "0", "The media format number is incorrect");
    }

    [Fact]
    public void TestMultipleMediaFormats()
    {
        MediaDescription Md = MediaDescription.ParseMediaDescription("audio 7000 RTP/AVP 0 8");
        Assert.True(Md.MediaFormatNumbers.Count == 2, "The number of media format numbers " +
            "is incorrect");
        Assert.True(Md.MediaFormatNumbers[0] == "0", "The first media format number is " +
            "incorrect");
        Assert.True(Md.MediaFormatNumbers[1] == "8", "The second media format number is " +
            "incorrect");
    }

    [Fact]
    public void TestInvalidMediaDescription()
    {
        Assert.Throws<ArgumentException>(() => MediaDescription.ParseMediaDescription(
            "audio 7000 RTP/AVP"));
    }

    [Fact]
    public void TestCreateCopy()
    {
        MediaDescription Md1 = MediaDescription.ParseMediaDescription("audio 7000 RTP/AVP 0 8");
        MediaDescription Md2 = Md1.CreateCopy();
        Assert.True(Md1.MediaType == Md2.MediaType, "The media types do not match");
        Assert.True(Md1.Port == Md2.Port, "The port numbers do not match");
        Assert.True(Md1.Transport == Md2.Transport, "The Transport types do not match");
        Assert.True(Md1.MediaFormatNumbers.Count == Md2.MediaFormatNumbers.Count, "The " +
            "number of media format numbers do not match");
        for (int i= 0; i < Md1.MediaFormatNumbers.Count; i++)
        {
            Assert.True(Md1.MediaFormatNumbers[i] == Md2.MediaFormatNumbers[i],
                $"The media format numbers at index {i} do not match");
        }

    }
}
