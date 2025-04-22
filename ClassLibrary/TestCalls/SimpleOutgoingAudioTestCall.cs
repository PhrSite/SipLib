/////////////////////////////////////////////////////////////////////////////////////
//  File:   SimpleOutgoingAudioTestCall.cs                          18 Apr 25 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.TestCalls;
using SipLib.Transactions;
using SipLib.Core;
using SipLib.Rtp;
using SipLib.Sdp;
using SipLib.Body;
using SipLib.Media;
using System.Net;
using System.ComponentModel;

/// <summary>
/// This class creates and sends a single NG9-1-1 audio test call to a test call target and reports the results of the test call.
/// The format of the audio sent is G.711 Mu-Law (PCMU) and each sample is silence.
/// <para>
/// To use this class, call the constructor and then call the DoTestCall() method. DoTestCall is an awaitable Task that
/// returns the results of the test call.
/// </para>
/// </summary>
public class SimpleOutgoingAudioTestCall
{
    private SipTransport m_Transport;
    private SIPURI m_ToSipUri;
    private int m_LocalRtpAudioPort;
    private string m_CallId = string.Empty;

    private int m_PacketsSent = 0;
    private int m_PacketsReceived = 0;

    private DateTime m_CallStartTime = DateTime.Now;
    private DateTime m_CallStopTime = DateTime.Now;
    private int m_MaxTestCallDurationSeconds;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="toSipUri">SIPURI of the test call target to send the test call to. The host must be an IP
    /// endpoint (IPv4 or IPv6) that contains an IP address and a SIP port number.</param>
    /// <param name="transport">SipTransport to use to communicate with the test call target. The SIPURI of the test
    /// call target must be reachable from this SipTransport.</param>
    /// <param name="localRtpAudioPort">Local RTP port number to use for receiving and sending RTP packets. If
    /// multiple test calls are being performed concurrently on the specified SipTransport, then each test call must
    /// have a unique local RTP port.</param>
    /// <param name="maxTestCallDurationSeconds">Specifies the maximum duration of a test call in seconds. This 
    /// parameter is optional. The default is int.MaxVal.</param>
    public SimpleOutgoingAudioTestCall(SIPURI toSipUri, SipTransport transport, int localRtpAudioPort, 
        int maxTestCallDurationSeconds = int.MaxValue)
    {
        m_ToSipUri = toSipUri;
        m_Transport = transport;
        m_LocalRtpAudioPort = localRtpAudioPort;
        m_MaxTestCallDurationSeconds = maxTestCallDurationSeconds;
    }

    /// <summary>
    /// Starts the test call.
    /// </summary>
    /// <returns>Returns the results of the test call.</returns>
    public async Task<OutgoingTestCallResults> DoTestCall()
    {
        OutgoingTestCallResults results = new OutgoingTestCallResults();
        IPEndPoint TargetEndPoint = m_ToSipUri.ToSIPEndPoint()!.GetIPEndPoint();
        IPAddress? localIpAddress = m_Transport.SipChannel.SIPChannelContactURI?.ToSIPEndPoint()?.Address;
        if (m_Transport.SipChannel.SIPChannelContactURI is null || localIpAddress == null)
        {
            results.Success = false;
            results.FailureReason = "The SIPChannelContactURI is null or does not contain an IP address";
            return results;
        }

        SIPURI LocalContactURI = m_Transport.SipChannel.SIPChannelContactURI;
        string SdpUaName = LocalContactURI.User == null ? "TestCaller" : LocalContactURI.User;

        // Build an INVITE request for the test call
        SIPURI ruri = SIPURI.ParseSIPURI("urn:" + TestCallConstants.TestCallUrnValue);
        SIPRequest invite = SIPRequest.CreateBasicRequest(SIPMethodsEnum.INVITE, ruri, m_ToSipUri, m_ToSipUri.User,
            LocalContactURI, LocalContactURI.User);
        Sdp audioSdp = SdpUtils.BuildSimpleAudioSdp(localIpAddress, m_LocalRtpAudioPort, SdpUaName);

        // Add the attributes required for a test call
        audioSdp.Media[0].Attributes.Add(new SdpAttribute(TestCallConstants.LoopbackAttributeName, 
            TestCallConstants.RtpMediaLoopbackAttributeValue));
        audioSdp.Media[0].Attributes.Add(new SdpAttribute(TestCallConstants.LoopbackSourceAttributeName, null));

        SipBodyBuilder builder = new SipBodyBuilder();
        builder.AddContent(ContentTypes.Sdp, audioSdp.ToString(), null, null);
        builder.AttachMessageBody(invite);

        m_CallId = invite.Header.CallId;

        m_Transport.SipRequestReceived += OnSipRequestReceived;

        ClientInviteTransaction Cit = m_Transport.StartClientInvite(invite, TargetEndPoint, null, null);
        SipTransactionBase transactionBase = await Cit.WaitForCompletionAsync();
        m_CallStartTime = DateTime.Now;
        if (transactionBase.LastReceivedResponse == null)
        {
            results.Success = false;
            results.FailureReason = "No response received to the INVITE request.";
            m_Transport.SipRequestReceived -= OnSipRequestReceived;
            return results;
        }

        if (transactionBase.LastReceivedResponse.Status != SIPResponseStatusCodesEnum.Ok)
        {
            results.Success = false;
            results.FailureReason = $"Test call target returned response code {transactionBase.LastReceivedResponse.StatusCode}";
            m_Transport.SipRequestReceived -= OnSipRequestReceived;
            return results;
        }

        // Make sure that the OK response has an SDP body
        SIPResponse OkResponse = transactionBase.LastReceivedResponse;
        string? strAnsweredSdp = OkResponse.GetContentsOfType(ContentTypes.Sdp);
        if (string.IsNullOrEmpty(strAnsweredSdp) == true)
        {
            results.Success = false;
            results.FailureReason = "The test call target did not provide a SDP block in the OK response.";
            m_Transport.SipRequestReceived -= OnSipRequestReceived;
            return results;
        }

        Sdp AnsweredSdp;
        try
        {
            AnsweredSdp = Sdp.ParseSDP(strAnsweredSdp);
        }
        catch (Exception Ex)
        {
            results.Success = false;
            results.FailureReason = $"An exception occurred while parsing the SDP from the test call target. The " +
                $"exception message is: {Ex.Message}";
            m_Transport.SipRequestReceived -= OnSipRequestReceived;
            return results;
        }

        MediaDescription answeredMediaDescription = AnsweredSdp.Media[0];
        (RtpChannel? rtpChannel, string? error) = RtpChannel.CreateFromSdp(false, audioSdp, audioSdp.Media[0], AnsweredSdp,
            answeredMediaDescription, false, null);
        if (rtpChannel == null)
        {
            results.Success = false;
            results.FailureReason = $"Error creating an RTP channel for the test call. Error message = {error}";
            // Terminate the call to the test call target
            await SendByeRequest(invite, TargetEndPoint, OkResponse);
            m_Transport.SipRequestReceived -= OnSipRequestReceived;
            return results;
        }

        rtpChannel.RtpPacketSent += OnRtpPacketSent;
        rtpChannel.RtpPacketReceived += OnRtpPacketReceived;
        rtpChannel.StartListening();

        AudioSource audioSource = new AudioSource(answeredMediaDescription, new PcmuEncoder(), rtpChannel);
        FileAudioSource fileAudioSource = new FileAudioSource(new AudioSampleData(new short[8000], 8000), null);
        fileAudioSource.AudioSamplesReady += audioSource.SendAudioSamples;
        fileAudioSource.Start();
        audioSource.Start();

        // Wait for the BYE request from the test call server
        bool TestCallTimeExceeded = false;
        while (m_ByeReceived == false && TestCallTimeExceeded == false)
        {
            await Task.Delay(10);

            DateTime Now = DateTime.Now;
            if ((Now - m_CallStartTime).TotalSeconds > m_MaxTestCallDurationSeconds)
                // The call has lasted too long.
                TestCallTimeExceeded = true;
        }

        if (TestCallTimeExceeded == true)
        {
            await SendByeRequest(invite, TargetEndPoint, OkResponse);
            results.Success = false;
            results.FailureReason = "Maximum test call duration exceeded.";
            return results;
        }

        fileAudioSource.Stop();
        audioSource.Stop();
        rtpChannel.Shutdown();

        // Unhook the event handlers
        m_Transport.SipRequestReceived -= OnSipRequestReceived;
        rtpChannel.RtpPacketSent += OnRtpPacketSent;
        rtpChannel.RtpPacketReceived += OnRtpPacketReceived;

        results.Success = true;
        results.PacketsSent = m_PacketsSent;
        results.PacketsReceived = m_PacketsReceived;
        results.CallStartTime = m_CallStartTime;
        results.CallStopTime = m_CallStopTime;
        return results;
    }

    private async Task SendByeRequest(SIPRequest invite, IPEndPoint TargetEndPoint, SIPResponse OkResponse)
    {
        SIPRequest ByeRequest = SipUtils.BuildByeRequest(invite, m_Transport.SipChannel, TargetEndPoint, false,
            invite.Header.CSeq, OkResponse);
        await m_Transport.StartClientNonInviteTransaction(ByeRequest, TargetEndPoint, null, 500).WaitForCompletionAsync();
    }

    private void OnRtpPacketReceived(RtpPacket rtpPacket)
    {
        m_PacketsReceived += 1;
    }

    private void OnRtpPacketSent(RtpPacket rtpPacket)
    {
        m_PacketsSent += 1;
    }

    private bool m_ByeReceived = false;

    private void OnSipRequestReceived(SIPRequest sipRequest, SIPEndPoint remoteEndPoint, SipTransport sipTransportManager)
    {
        if (sipRequest.Header.CallId != m_CallId)
            return;     // Request is not for this call

        if (sipRequest.Method == SIPMethodsEnum.BYE)
        {
            SIPResponse OkResponse = SipUtils.BuildOkToByeOrCancel(sipRequest, remoteEndPoint);
            m_Transport.SipChannel.Send(remoteEndPoint.GetIPEndPoint(), OkResponse.ToByteArray());
            m_CallStopTime = DateTime.Now;
            m_ByeReceived = true;
        }
        else
        {   
            SIPResponse response = SipUtils.BuildResponse(sipRequest, SIPResponseStatusCodesEnum.MethodNotAllowed,
                "Method Not Allowed", sipTransportManager.SipChannel, null);
            m_Transport.SipChannel.Send(remoteEndPoint.GetIPEndPoint(), response.ToByteArray());
        }
    }
}
