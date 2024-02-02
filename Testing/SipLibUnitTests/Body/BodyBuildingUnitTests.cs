/////////////////////////////////////////////////////////////////////////////////////
//  File:   BodyBuildingUnitTests.cs                                1 Feb 24 PHR
/////////////////////////////////////////////////////////////////////////////////////


namespace SipLibUnitTests.Body;

using SipLib.Body;
using SipLib.Core;
using SipLib.Sdp;
using System.Net;
using System.Text;

using Pidf;
using Ng911Lib;
using Ng911Lib.Utilities;

[Trait("Category", "unit")]
public class BodyBuildingUnitTests
{
    IPAddress m_LocalIp = IPAddress.Parse("192.168.1.1");
    int m_LocalSipPort = 5060;
    IPEndPoint localEp;
    SIPEndPoint m_LocalSipEndpoint;
    SIPURI localSipUri;
    IPAddress m_RemoteIp = IPAddress.Parse("192.168.1.2");
    int m_RemoteSipPort = 5060;
    IPEndPoint m_RemoteSipEndpoint;
    SIPURI ReqUri;

    public BodyBuildingUnitTests(Xunit.Abstractions.ITestOutputHelper output)
    {
        localEp = new IPEndPoint(m_LocalIp, m_LocalSipPort);
        m_LocalSipEndpoint = new SIPEndPoint(SIPProtocolsEnum.tcp, localEp);
        localSipUri = new SIPURI(SIPSchemesEnum.sip, m_LocalSipEndpoint);
        m_RemoteSipEndpoint = new IPEndPoint(m_RemoteIp, m_RemoteSipPort);
        ReqUri = new SIPURI(SIPSchemesEnum.sip, new SIPEndPoint(m_RemoteSipEndpoint));

    }

    [Fact]
    public void TestAddSdp()
    {
        SIPRequest Invite = SipUtils.CreateBasicRequest(SIPMethodsEnum.INVITE, ReqUri, ReqUri, null, localSipUri, "Test");
        Sdp OfferedSdp = new Sdp(m_LocalIp, "Test");
        OfferedSdp.Media.Add(SdpUtils.CreateAudioMediaDescription(6000));

        SipBodyBuilder Sbb = new SipBodyBuilder();
        Sbb.AddContent("application/sdp", OfferedSdp.ToString(), null, null);
        Sbb.AttachMessageBody(Invite);

        byte[] inviteBytes = Encoding.UTF8.GetBytes(Invite.ToString());
        SIPMessage recvMsg = SIPMessage.ParseSIPMessage(inviteBytes, m_LocalSipEndpoint, null);
        SIPRequest recvInvite = SIPRequest.ParseSIPRequest(recvMsg);

        string strSdp = recvInvite.GetContentsOfType(SipLib.Body.ContentTypes.Sdp);
        Assert.True(strSdp != null, "SDP not found");
        Sdp recvSdp = Sdp.ParseSDP(strSdp);
        Assert.True(recvSdp != null, "recvSdp is null");
        Assert.True(recvSdp.Media[0].Port == 6000, "The Port is wrong");
    }

    [Fact]
    public void TestAddSdpAndPidf()
    {
        SIPRequest Invite = SipUtils.CreateBasicRequest(SIPMethodsEnum.INVITE, ReqUri, ReqUri, null, localSipUri, "Test");
        Sdp OfferedSdp = new Sdp(m_LocalIp, "Test");
        OfferedSdp.Media.Add(SdpUtils.CreateAudioMediaDescription(6000));

        SipBodyBuilder Sbb = new SipBodyBuilder();
        Sbb.AddContent(SipLib.Body.ContentTypes.Sdp, OfferedSdp.ToString(), null, null);

        Presence presence = Presence.CreateDevicePresence("123");
        presence.device.geopriv.LocationInfo.Circle = new Circle(42.5463, -73.2512, 100);
        string strPresence = XmlHelper.SerializeToString(presence);
        Sbb.AddContent(SipLib.Body.ContentTypes.Pidf, strPresence, null, null);

        Sbb.AttachMessageBody(Invite);

        byte[] inviteBytes = Encoding.UTF8.GetBytes(Invite.ToString());
        SIPMessage recvMsg = SIPMessage.ParseSIPMessage(inviteBytes, m_LocalSipEndpoint, null);
        SIPRequest recvInvite = SIPRequest.ParseSIPRequest(recvMsg);

        string strSdp = recvInvite.GetContentsOfType(SipLib.Body.ContentTypes.Sdp);
        Assert.True(strSdp != null, "SDP not found");
        Sdp recvSdp = Sdp.ParseSDP(strSdp);
        Assert.True(recvSdp != null, "recvSdp is null");
        Assert.True(recvSdp.Media[0].Port == 6000, "The Port is wrong");

        strPresence = recvInvite.GetContentsOfType(SipLib.Body.ContentTypes.Pidf);
        Assert.True(strPresence != null, "Pidf not found");
        Presence recvPresence = XmlHelper.DeserializeFromString<Presence>(strPresence);
        Assert.True(recvPresence != null, "recvPresence is null");

    }

}
