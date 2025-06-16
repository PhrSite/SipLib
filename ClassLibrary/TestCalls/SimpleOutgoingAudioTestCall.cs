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

/// <summary>
/// This class creates and sends a single NG9-1-1 audio test call to a test call target and reports the results of the test call.
/// The format of the audio sent is G.711 Mu-Law (PCMU) and each sample is silence.
/// <para>
/// To use this class, call the constructor and then call the DoTestCall() method. DoTestCall is an awaitable Task that
/// returns the results of the test call.
/// </para>
/// <para>See <a href= "~/articles/SipLibTestCalls.md#UsingSimpleOutgointAudioTestCall">Using The SimpleOutgoingAudioTestCall Class</a> </para>
/// </summary>
public class SimpleOutgoingAudioTestCall
{
    private SipTransport m_Transport;
    private SIPURI m_ToSipUri;
    private int m_LocalRtpAudioPort;
    private string m_CallId = string.Empty;
    private OutgoingTestCallResults m_Results;
    private int m_MaxTestCallDurationSeconds;
    private bool m_ByeReceived = false;

    private IPEndPoint m_TargetEndPoint;
    private IPAddress? m_LocalIpAddress;
    private SIPURI m_LocalContactURI;
    private string m_SdpUaName;
    private SIPURI m_ruri;

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
    /// parameter is optional. The call duration is measured from the time that this class receives the OK response
    /// from the test call target..
    /// If the test call duration exceeds this limit then this class sends a BYE request to terminate the call.
    /// The default is int.MaxVal.</param>
    public SimpleOutgoingAudioTestCall(SIPURI toSipUri, SipTransport transport, int localRtpAudioPort, 
        int maxTestCallDurationSeconds = int.MaxValue)
    {
        m_ToSipUri = toSipUri;
        m_Transport = transport;
        m_LocalRtpAudioPort = localRtpAudioPort;
        m_MaxTestCallDurationSeconds = maxTestCallDurationSeconds;
        m_Results = new OutgoingTestCallResults();

        m_TargetEndPoint = m_ToSipUri.ToSIPEndPoint()!.GetIPEndPoint();
        m_LocalIpAddress = m_Transport.SipChannel.SIPChannelContactURI?.ToSIPEndPoint()?.Address;
        if (m_Transport.SipChannel.SIPChannelContactURI is null || m_LocalIpAddress == null)
            throw new ArgumentException("The SIPChannelContactURI is null or does not contain an IP address");

        m_LocalContactURI = m_Transport.SipChannel.SIPChannelContactURI;
        m_SdpUaName = m_LocalContactURI.User == null ? "TestCaller" : m_LocalContactURI.User;
        m_ruri = SIPURI.ParseSIPURI("urn:" + TestCallConstants.TestCallUrnValue);
    }

    /// <summary>
    /// Starts a test call.
    /// </summary>
    /// <returns>Returns the results of the test call when the test call is terminated.</returns>
    public async Task<OutgoingTestCallResults> DoTestCall()
    {
        m_Results = new OutgoingTestCallResults();
        m_ByeReceived = false;

        // Build an INVITE request for the test call
        SIPRequest invite = SIPRequest.CreateBasicRequest(SIPMethodsEnum.INVITE, m_ruri, m_ToSipUri, m_ToSipUri.User,
            m_LocalContactURI, m_LocalContactURI.User);
        Sdp audioSdp = SdpUtils.BuildSimpleAudioSdp(m_LocalIpAddress, m_LocalRtpAudioPort, m_SdpUaName);

        // Add the attributes required for a test call
        audioSdp.Media[0].Attributes.Add(new SdpAttribute(TestCallConstants.LoopbackAttributeName, 
            TestCallConstants.RtpMediaLoopbackAttributeValue));
        audioSdp.Media[0].Attributes.Add(new SdpAttribute(TestCallConstants.LoopbackSourceAttributeName, null));

        SipBodyBuilder builder = new SipBodyBuilder();
        builder.AddContent(ContentTypes.Sdp, audioSdp.ToString(), null, null);
        builder.AttachMessageBody(invite);

        m_CallId = invite.Header.CallId;

        m_Transport.SipRequestReceived += OnSipRequestReceived;

        ClientInviteTransaction Cit = m_Transport.StartClientInvite(invite, m_TargetEndPoint, null, null);
        SipTransactionBase transactionBase = await Cit.WaitForCompletionAsync();
        m_Results.CallStartTime = DateTime.Now;
        if (transactionBase.LastReceivedResponse == null)
        {
            SetFailureReason("No response received to the INVITE request.");
            m_Results.CallStopTime = DateTime.Now;
            return m_Results;
        }

        if (transactionBase.LastReceivedResponse.Status != SIPResponseStatusCodesEnum.Ok)
        {
            SetFailureReason($"Test call target returned response code {transactionBase.LastReceivedResponse.StatusCode}");
            return m_Results;
        }

        // Make sure that the OK response has an SDP body
        SIPResponse OkResponse = transactionBase.LastReceivedResponse;
        string? strAnsweredSdp = OkResponse.GetContentsOfType(ContentTypes.Sdp);
        if (string.IsNullOrEmpty(strAnsweredSdp) == true)
        {
            SetFailureReason("The test call target did not provide a SDP block in the OK response.");
            return m_Results;
        }

        Sdp AnsweredSdp;
        try
        {
            AnsweredSdp = Sdp.ParseSDP(strAnsweredSdp);
        }
        catch (Exception Ex)
        {
            SetFailureReason($"An exception occurred while parsing the SDP from the test call target. The " +
                $"exception message is: {Ex.Message}");
            return m_Results;
        }

        MediaDescription answeredMediaDescription = AnsweredSdp.Media[0];
        (RtpChannel? rtpChannel, string? error) = RtpChannel.CreateFromSdp(false, audioSdp, audioSdp.Media[0], AnsweredSdp,
            answeredMediaDescription, false, null);
        if (rtpChannel == null)
        {
            // Terminate the call to the test call target
            await SendByeRequest(invite, m_TargetEndPoint, OkResponse);
            SetFailureReason($"Error creating an RTP channel for the test call. Error message = {error}");
            return m_Results;
        }

        rtpChannel.RtpPacketSent += OnRtpPacketSent;
        rtpChannel.RtpPacketReceived += OnRtpPacketReceived;
        rtpChannel.StartListening();

        AudioSource audioSource = new AudioSource(answeredMediaDescription, new PcmuEncoder(), rtpChannel);
        SilenceAudioSampleSource Sass = new SilenceAudioSampleSource();
        audioSource.SetAudioSampleSource(Sass);
        Sass.Start();

        // Wait for the BYE request from the test call server
        bool TestCallTimeExceeded = false;
        while (m_ByeReceived == false && TestCallTimeExceeded == false)
        {
            await Task.Delay(10);

            DateTime Now = DateTime.Now;
            if ((Now - m_Results.CallStartTime).TotalSeconds > m_MaxTestCallDurationSeconds)
                // The call has lasted too long.
                TestCallTimeExceeded = true;
        }

        if (TestCallTimeExceeded == true)
        {
            await SendByeRequest(invite, m_TargetEndPoint, OkResponse);
            SetFailureReason("Maximum test call duration exceeded.");
            return m_Results;
        }

        Sass.Stop();
        audioSource.ClearAudioSampleSource();
        rtpChannel.Shutdown();

        // Unhook the event handlers
        m_Transport.SipRequestReceived -= OnSipRequestReceived;
        rtpChannel.RtpPacketSent += OnRtpPacketSent;
        rtpChannel.RtpPacketReceived += OnRtpPacketReceived;

        m_Results.Success = true;
        return m_Results;
    }

    private void SetFailureReason(string reason)
    {
        m_Results.Success = false;
        m_Results.FailureReason = reason;
        m_Transport.SipRequestReceived -= OnSipRequestReceived;
    }

    private async Task SendByeRequest(SIPRequest invite, IPEndPoint TargetEndPoint, SIPResponse OkResponse)
    {
        SIPRequest ByeRequest = SipUtils.BuildByeRequest(invite, m_Transport.SipChannel, TargetEndPoint, false,
            invite.Header.CSeq, OkResponse);
        await m_Transport.StartClientNonInviteTransaction(ByeRequest, TargetEndPoint, null, 500).WaitForCompletionAsync();
    }

    private void OnRtpPacketReceived(RtpPacket rtpPacket)
    {
        m_Results.PacketsReceived += 1;
    }

    private void OnRtpPacketSent(RtpPacket rtpPacket)
    {
        m_Results.PacketsSent += 1;
    }

    private void OnSipRequestReceived(SIPRequest sipRequest, SIPEndPoint remoteEndPoint, SipTransport sipTransportManager)
    {
        if (sipRequest.Header.CallId != m_CallId)
            return;     // Request is not for this call

        if (sipRequest.Method == SIPMethodsEnum.BYE)
        {
            SIPResponse OkResponse = SipUtils.BuildOkToByeOrCancel(sipRequest, remoteEndPoint);
            m_Transport.SipChannel.Send(remoteEndPoint.GetIPEndPoint(), OkResponse.ToByteArray());
            m_Results.CallStopTime = DateTime.Now;
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
