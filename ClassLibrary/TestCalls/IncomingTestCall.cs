/////////////////////////////////////////////////////////////////////////////////////
//  File:   IncomingTestCall.cs                                     26 Mar 25 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.TestCalls;

using SipLib.Core;
using SipLib.Sdp;
using SipLib.Body;
using SipLib.Threading;
using SipLib.Transactions;
using SipLib.Rtp;
using SipLib.Media;
using System.Net;
using System.Threading.Tasks;

/// <summary>
/// Delegate type for the TestCallEnded event of the IncomingTestCall class.
/// </summary>
/// <param name="callId"></param>
public delegate void TestCallEndedDelegateType(string callId);

/// <summary>
/// Class for a handling a single NG9-1-1 test call. See Section 9 of NENA-STA-010.3b and RFC 6849.
/// </summary>
public class IncomingTestCall : QueuedActionWorkerTask
{

    private SipTransport m_Transport;
    private SIPRequest m_Invite;
    private SdpAnswerSettings m_AnswerSettings;
    private IncomingTestCallSettings m_TestCallSettings;
    private SIPEndPoint m_RemoteSipEndPoint;
    private string CallId;
    private bool m_CallEnded = false;
    private int m_LastSequenceNumber;
    private SIPResponse m_OkResponse;

    private Sdp? m_OfferedSdp = null;
    private Sdp? m_AnsweredSdp = null;
    private List<TestCallParams> m_ParamsList = new List<TestCallParams>();
    private DateTime m_CallStartTime = DateTime.Now;

    /// <summary>
    /// Event that is fired with the incoming test call has ended.
    /// </summary>
    public event TestCallEndedDelegateType? TestCallEnded = null; 

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="invite">INVITE request for a test call.</param>
    /// <param name="remoteEndPoint">Remote endpoint that sent the INVITE request.</param>
    /// <param name="transport">SipTransport that the INVITE request was received on.</param>
    /// <param name="answerSettings">Settings that determine how to answer the test call INVITE requet.</param>
    /// <param name="testCallSettings">Settings that determine how to handle the test call.</param>
    public IncomingTestCall(SIPRequest invite, SIPEndPoint remoteEndPoint, SipTransport transport, SdpAnswerSettings answerSettings, IncomingTestCallSettings
        testCallSettings) : base(5)
    {
        m_Invite = invite;
        m_RemoteSipEndPoint = remoteEndPoint;
        m_Transport = transport;
        m_AnswerSettings = answerSettings;
        m_TestCallSettings = testCallSettings;
        CallId = invite.Header.CallId;
        m_LastSequenceNumber = m_Invite.Header.CSeq;
        m_OkResponse = new SIPResponse(SIPResponseStatusCodesEnum.Ok, "OK", m_Transport.SipChannel.SIPChannelEndPoint);

    }

    internal void StartCall()
    {
        //Start();
        //EnqueueWork(() => { DoStartCall(); });
        DoStartCall();
    }

    private void DoStartCall()
    {
        // The INVITE and its SDP have already been validated by TestCallIsValid()
        string strSdp = m_Invite.GetContentsOfType(ContentTypes.Sdp)!;
        m_OfferedSdp = Sdp.ParseSDP(strSdp);
        m_AnsweredSdp = Sdp.BuildAnswerSdp(m_OfferedSdp, m_Transport.SipChannel.SIPChannelEndPoint.GetIPEndPoint().Address, 
            m_AnswerSettings);
        
        foreach (MediaDescription OfferedMd in m_OfferedSdp.Media)
        {
            MediaDescription? answerMd = m_AnsweredSdp.GetMediaType(OfferedMd.MediaType);
            if (answerMd == null)
                continue;

            answerMd.Attributes.Add(new SdpAttribute(TestCallConstants.LoopbackMirrorAttributeName, null));
            SdpAttribute loopbackAttr = OfferedMd.GetNamedAttribute(TestCallConstants.LoopbackAttributeName)!;
            if (loopbackAttr.Value!.Contains(TestCallConstants.RtpPacketLoopbackAttributeValue) == true)
            {   // RTP packet loopback. Figure out whether to use encaprtp or direct rtploopback
                answerMd.Attributes.Add(new SdpAttribute(TestCallConstants.LoopbackAttributeName, TestCallConstants.
                    RtpPacketLoopbackAttributeValue));
                RtpMapAttribute? map = OfferedMd.GetRtpMapForCodecType(TestCallConstants.EncapsulatedRtpLoopback);
                if (map == null)
                    map = OfferedMd.GetRtpMapForCodecType(TestCallConstants.DirectRtpLoopback);

                if (map != null)
                {
                    answerMd.RtpMapAttributes.Add(map);
                    answerMd.PayloadTypes.Add(map.PayloadType);
                }
                else
                    // Error, neither encaprtp nor rtploopback was offered so reject this media type.
                    answerMd.Port = 0;   
            }
            else
            {   // Media loopback
                answerMd.Attributes.Add(new SdpAttribute(TestCallConstants.LoopbackAttributeName, TestCallConstants.
                    RtpMediaLoopbackAttributeValue));
                // For media loopback, no need to do anything else
            }
        } // end foreach

        SIPResponse OkResponse = SipUtils.BuildOkToInvite(m_Invite, m_Transport.SipChannel, m_AnsweredSdp.ToString(),
            ContentTypes.Sdp);
        // Assume sending the OK response will be successful
        m_Transport.StartServerInviteTransaction(m_Invite, m_RemoteSipEndPoint.GetIPEndPoint(), null, OkResponse);
        m_CallStartTime = DateTime.Now;

        // Set up the RtpChannels
        RtpChannel? rtpChannel = null;
        string? error = null;
        for (int i = 0; i < m_AnsweredSdp.Media.Count; i++)
        {
            if (m_AnsweredSdp.Media[i].Port == 0)
                continue;   // This media type was rejected

            (rtpChannel, error) = RtpChannel.CreateFromSdp(true, m_OfferedSdp, m_OfferedSdp.Media[i], m_AnsweredSdp,
                m_AnsweredSdp.Media[i], false, null);
            if (rtpChannel == null)
            {
                // TODO: handle this error
                continue;
            }

            MediaDescription AnsweredMd = m_AnsweredSdp.Media[i];
            switch (AnsweredMd.MediaType)
            {
                case MediaTypes.Audio:
                    m_AudioRtpChannel = rtpChannel;
                    m_AudioRtpChannel.RtpPacketReceived += OnAudioRtpPacketReceived;
                    m_AudioTestCallParams = new TestCallParams(AnsweredMd, rtpChannel);
                    m_ParamsList.Add(m_AudioTestCallParams);
                    break;
                case MediaTypes.RTT:
                    m_RttRtpChannel = rtpChannel;
                    m_RttRtpChannel.RtpPacketReceived += OnRttRtpPacketReceived;
                    m_RttTestCallParams = new TestCallParams(AnsweredMd, rtpChannel);
                    m_ParamsList.Add(m_RttTestCallParams);
                    break;
                case MediaTypes.Video:
                    m_VideoRtpChannel = rtpChannel;
                    m_VideoRtpChannel.RtpPacketReceived += OnVideoRtpPacketReceived;
                    m_VideoTestCallParams = new TestCallParams(AnsweredMd, rtpChannel);
                    m_ParamsList.Add(m_VideoTestCallParams);
                    break;
            }

            rtpChannel.StartListening();
        }

        Start();
    }

    private RtpChannel? m_AudioRtpChannel = null;
    private TestCallParams m_AudioTestCallParams = new TestCallParams();
    private void OnAudioRtpPacketReceived(RtpPacket packet)
    {
        if (m_AudioRtpChannel == null)
            return;

        SendMirroredRtpPacket(packet, m_AudioRtpChannel, m_AudioTestCallParams);
    }

    private RtpChannel? m_RttRtpChannel = null;
    private TestCallParams m_RttTestCallParams = new TestCallParams();
    private void OnRttRtpPacketReceived(RtpPacket packet)
    {
        if (m_RttRtpChannel == null)
            return;

        SendMirroredRtpPacket(packet, m_RttRtpChannel, m_RttTestCallParams);
    }

    private RtpChannel? m_VideoRtpChannel = null;
    private TestCallParams m_VideoTestCallParams = new TestCallParams();
    private void OnVideoRtpPacketReceived(RtpPacket packet)
    {
        if (m_VideoRtpChannel == null)
            return;

        SendMirroredRtpPacket(packet, m_VideoRtpChannel, m_VideoTestCallParams);
    }

    private void SendMirroredRtpPacket(RtpPacket receivedPacket, RtpChannel channel, TestCallParams testCallParams)
    {
        if (testCallParams.IsMediaLoopback == true)
            SendMediaLoopbackPacket(receivedPacket, channel, testCallParams);
        else
        {   // Its RTP packet loopback
            if (testCallParams.PacketLoopbackType == PacketLoopbackTypeEnum.DirectPacketLoopback)
                SendMediaLoopbackPacket(receivedPacket, channel, testCallParams);
            else if (testCallParams.PacketLoopbackType == PacketLoopbackTypeEnum.EncapsulatedPacketLoopback)
                SendEncapsulatedRtpPacket(receivedPacket, channel, testCallParams);
        }

        testCallParams.IncrementPacketsReceived();

        EnqueueWork(() => DoTimedEvents());     // Reduce the sampling latency
    }

    private void SendMediaLoopbackPacket(RtpPacket receivedPacket, RtpChannel channel, TestCallParams testCallParams)
    {
        RtpPacket packet = new RtpPacket(receivedPacket.PayloadLength);
        packet.PayloadType = testCallParams.PayloadType;
        packet.SequenceNumber = testCallParams.SequenceNumber++;
        packet.Timestamp = testCallParams.Timestamp;
        testCallParams.Timestamp = (testCallParams.Timestamp + testCallParams.ClockRate) % uint.MaxValue;
        packet.SSRC = testCallParams.SSRC;
        if (receivedPacket.Payload != null)
            packet.SetPayloadBytes(receivedPacket.Payload);
        channel.Send(packet);
    }

    private void SendEncapsulatedRtpPacket(RtpPacket receivedPacket, RtpChannel channel, TestCallParams testCallParams)
    {
        int ReceivedPacketLength = receivedPacket.HeaderLength + receivedPacket.PayloadLength;
        RtpPacket packet = new RtpPacket(ReceivedPacketLength + 4);
        packet.PayloadType = testCallParams.PayloadType;
        packet.SequenceNumber = testCallParams.SequenceNumber++;
        packet.Timestamp = testCallParams.Timestamp;
        testCallParams.Timestamp = (testCallParams.Timestamp + testCallParams.ClockRate) % uint.MaxValue;
        packet.SSRC = testCallParams.SSRC;

        // Load in the received timestamp. Realistically, this is equal to the timestamp that the RTP packet is mirrored
        byte[] payload = new byte[ReceivedPacketLength + 4];
        RtpUtils.SetDWord(payload, 0, packet.Timestamp);
        // Load the entire received RTP packet
        Array.Copy(receivedPacket.PacketBytes!, 0, payload, 4, receivedPacket.PayloadLength);
        packet.SetPayloadBytes(payload);

        channel.Send(packet);
    }

    /// <summary>
    /// This method must be called after the test call has ended or when the application is shutting down.
    /// </summary>
    /// <returns></returns>
    public override async Task Shutdown()
    {
        await DoShutdown();
    }

    private async Task DoShutdown()
    {
        if (m_CallEnded == false)
        {
            DoEndCall();
        }

        if (m_AudioRtpChannel != null)
        {
            m_AudioRtpChannel.Shutdown();
            m_AudioRtpChannel.RtpPacketReceived -= OnAudioRtpPacketReceived;
            m_AudioRtpChannel = null;
        }

        if (m_RttRtpChannel != null)
        {
            m_RttRtpChannel.Shutdown();
            m_RttRtpChannel.RtpPacketReceived -= OnRttRtpPacketReceived;
            m_RttRtpChannel = null;
        }

        if (m_VideoRtpChannel != null)
        {
            m_VideoRtpChannel.Shutdown();
            m_VideoRtpChannel.RtpPacketReceived -= OnVideoRtpPacketReceived;
            m_VideoRtpChannel = null;
        }

        await base.Shutdown();
    }

    /// <summary>
    /// 
    /// </summary>
    protected override void DoTimedEvents()
    {
        if (m_CallEnded == true)
            return;

        // Check to see if its time to terminate the call
        DateTime Now = DateTime.Now;
        bool Done = false;
        if (m_TestCallSettings.DurationUnits == TestCallDurationUnitsEnum.DurationUnitsPackets)
        {
            bool PacketCountsReached = true;
            // For debug only
            if (m_ParamsList.Count == 0)
            {

            }

            foreach (TestCallParams tcp in m_ParamsList)
            {
                if (tcp.PacketsReceived < m_TestCallSettings.DurationPackets)
                    PacketCountsReached = false;
            }

            if (PacketCountsReached == false)
            {   // Check for a timeout
                if ((Now - m_CallStartTime).TotalMilliseconds > 1000)
                {   // A timeout has occurred
                    Done = true;

                    // TODO: Log this error
                }
            }
            else
                Done = true;
        }
        else
        {   // Call duration in minutes
            //if ((Now - m_CallStartTime).TotalMinutes >= m_TestCallSettings.DurationMinutes)
            //    Done = true;
            if ((Now - m_CallStartTime).TotalMilliseconds >= (m_TestCallSettings.DurationMinutes * 60000))
                Done = true;
        }

        if (Done == true)
        {   // Its time to end the call.
            DoEndCall();
        }
    }

    internal void ProcessByeRequest(SIPRequest sipRequest, SIPEndPoint remoteEndPoint, SipTransport sipTransport)
    {
        EnqueueWork(() =>
        {
            SIPResponse ByeResponse = SipUtils.BuildOkToByeOrCancel(sipRequest, remoteEndPoint);
            // Fire and forget.
            m_CallEnded = true;
            sipTransport.StartServerNonInviteTransaction(sipRequest, remoteEndPoint.GetIPEndPoint(), null!, ByeResponse);
            TestCallEnded?.Invoke(CallId);
        });
    }

    private ManualResetEventSlim m_Event = new ManualResetEventSlim();

    internal void EndCall()
    {
        EnqueueWork(() => DoEndCall());
    }

    private void DoEndCall()
    {
        if (m_CallEnded == true)
            return;

        m_CallEnded = true;
        SIPRequest ByeRequest = SipUtils.BuildByeRequest(m_Invite, m_Transport.SipChannel, m_RemoteSipEndPoint.GetIPEndPoint(), true,
            m_LastSequenceNumber, m_OkResponse);
        ClientNonInviteTransaction Cnit = m_Transport.StartClientNonInviteTransaction(ByeRequest, m_RemoteSipEndPoint.GetIPEndPoint(), 
            ByeCompleteCallback, 1000);
        m_Event.Wait();

        TestCallEnded?.Invoke(CallId);
    }

    private void ByeCompleteCallback(SIPRequest sipRequest, SIPResponse? sipResponse,
        IPEndPoint remoteEndPoint, SipTransport sipTransport, SipTransactionBase Transaction)
    {
        m_Event.Set();
    }

    /// <summary>
    /// Tests to see if an incoming test call is valid.
    /// </summary>
    /// <param name="invite">INVITE request.</param>
    /// <returns>Returns true if the INVITE request is a valid test call. Returns false if it is not.</returns>
    public static bool TestCallIsValid(SIPRequest invite)
    {
        Sdp? sdp = null;
        try
        {
            sdp = Sdp.ParseSDP(invite.GetContentsOfType(ContentTypes.Sdp)!);
        }
        catch
        {   // No SDP or invalid SDP
            return false;
        }

        if (sdp == null || sdp.Media.Count == 0)
            return false;

        // Make sure that each media description has the correct attributes
        foreach (MediaDescription Md in sdp.Media)
        {
            SdpAttribute? sourceAttr = Md.GetNamedAttribute(TestCallConstants.LoopbackSourceAttributeName);
            if (sourceAttr == null)
                // The caller must be the loopback source
                return false;

            SdpAttribute? loopbackAttr = Md.GetNamedAttribute(TestCallConstants.LoopbackAttributeName);
            if (loopbackAttr == null || string.IsNullOrEmpty(loopbackAttr.Value))
                // No a=loopback attribute or the loopback attribute value is null or empty
                return false;

            // Either packet loopback or media loopback or both must be offered.
            if (loopbackAttr.Value.IndexOf(TestCallConstants.RtpPacketLoopbackAttributeValue) < 0 &&
                loopbackAttr.Value.IndexOf(TestCallConstants.RtpMediaLoopbackAttributeValue) < 0)
                return false;

            // If only RTP packet loopback is offered, then an encoding type of either encaprtp or rtploopback or both
            // must be offered.
            if (loopbackAttr.Value.Contains(TestCallConstants.RtpPacketLoopbackAttributeValue) == true &&
                loopbackAttr.Value.Contains(TestCallConstants.RtpMediaLoopbackAttributeValue) == false)
            {
                RtpMapAttribute? directRtpMap = Md.GetRtpMapForCodecType(TestCallConstants.DirectRtpLoopback);
                RtpMapAttribute? encapsulatedRtpMap = Md.GetRtpMapForCodecType(TestCallConstants.EncapsulatedRtpLoopback);
                if (directRtpMap == null && encapsulatedRtpMap == null)
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Returns true if the SIP request is an INVITE for an NG9-1-1 test call
    /// </summary>
    /// <param name="sipRequest">Incoming SIP Request</param>
    /// <returns>Returns true if the INVITE is for a NG9-1-1 test call or false if it is not.</returns>
    public static bool IsNg911TestCall(SIPRequest sipRequest)
    {
        if (sipRequest.Method != SIPMethodsEnum.INVITE || sipRequest.URI is null)
            return false;

        if (sipRequest.URI.Scheme != SIPSchemesEnum.urn || sipRequest.URI.Host == null)
            return false;

        if (sipRequest.URI.Host.IndexOf(TestCallConstants.TestCallUrnValue) >= 0)
            return true;
        else
            return false;
    }

}
