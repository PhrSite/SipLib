/////////////////////////////////////////////////////////////////////////////////////
//  File:   RttUas.cs                                               27 Aug 24 PHR
/////////////////////////////////////////////////////////////////////////////////////

using System.Net;
using SipLib.Body;
using SipLib.Core;
using SipLib.RealTimeText;
using SipLib.Rtp;
using SipLib.Sdp;
using SipLib.Transactions;

namespace RttServer;

/// <summary>
/// Delegate for the InviteReceived event of the RttUas class.
/// </summary>
/// <param name="from">Source of the INVITE request</param>
internal delegate void InviteReceivedDelegate(string from);

/// <summary>
/// Delegate type for the CharactersReceived event of the RttUas class.
/// </summary>
/// <param name="RxChars">Contains the character or characters received.</param>
/// <param name="Source">Indicates the source of the sender</param>
internal delegate void CharactersReceivedDelegate(string RxChars, string Source);

/// <summary>
/// Delegate type for the Error event of the RttUas class.
/// </summary>
/// <param name="errorMsg">Describes the error that occurred</param>
internal delegate void ErrorDelegate(string errorMsg);

/// <summary>
/// Implements a simple User Agent Server that handles text (RTT) media calls.
/// </summary>
internal class RttUas
{
    private const int RttPort = 9002;

    private SipTransport m_SipTransport;
    private IPAddress m_localAddress;
    private string m_userName;
    private bool m_Started = false;

    private RtpChannel? m_RtpChannel = null;
    private RttSender? m_RttSender = null;
    private RttReceiver? m_RttReceiver = null;
    private SIPRequest? m_Invite = null;
    private SIPResponse? m_OkResponse = null;
    private CallStateEnum m_CallState = CallStateEnum.Idle;

    /// <summary>
    /// Event that is fired when an INVITE request is received
    /// </summary>
    public event InviteReceivedDelegate? InviteReceived = null;

    /// <summary>
    /// Event that is fired when one or more characters are received.
    /// </summary>
    public event CharactersReceivedDelegate? CharactersReceived = null;

    /// <summary>
    /// Event that is fired when a BYE request is received.
    /// </summary>
    public event Action? ByeReceived = null;

    /// <summary>
    /// Event that is fired if an error is detected.
    /// </summary>
    public event ErrorDelegate? Error = null;

    /// <summary>
    /// Constructor. Hook the events of the new RttUac object, then call the Start() method. Then call
    /// the Call() method to start a call.
    /// </summary>
    /// <param name="sipTransport">SipTransport to use. Must be started</param>
    /// <param name="userName">User name</param>
    public RttUas(SipTransport sipTransport, string userName)
    {
        m_SipTransport = sipTransport;
        m_userName = userName;
        m_localAddress = m_SipTransport.SipChannel.SIPChannelEndPoint!.GetIPEndPoint().Address;
    }

    /// <summary>
    /// Hooks the events that are needed and gets ready to receive a call.
    /// </summary>
    public void Start()
    {
        m_SipTransport.SipRequestReceived += OnSipRequestReceived;
        m_Started = true;
    }

    private void OnSipRequestReceived(SIPRequest sipRequest, SIPEndPoint remoteEndPoint, SipTransport sipTransportManager)
    {
        switch (sipRequest.Method)
        {
            case SIPMethodsEnum.INVITE:
                ProcessInviteRequest(sipRequest, remoteEndPoint, sipTransportManager);
                break;
            case SIPMethodsEnum.CANCEL:
                ProcessCancelRequest(sipRequest, remoteEndPoint, sipTransportManager);
                break;

            case SIPMethodsEnum.ACK:
                if (m_Invite != null && sipRequest.Header.CallId == m_Invite.Header.CallId)
                {   // The ACK is for the OK that was sent by this UAS.

                }
                break;
            case SIPMethodsEnum.BYE:
                ProcessByeRequest(sipRequest, remoteEndPoint, sipTransportManager);
                break;
            default:
                SendMethodNotAllowed(sipRequest, remoteEndPoint, sipTransportManager);
                break;
        }
    }

    private void ProcessInviteRequest(SIPRequest sipRequest, SIPEndPoint remoteEndPoint, SipTransport sipTransportManager)
    {
        InviteReceived?.Invoke($"INVITE received from {remoteEndPoint.GetIPEndPoint()}");

        if (m_CallState != CallStateEnum.Idle)
        {
            SIPResponse busyResponse = SipUtils.BuildResponse(sipRequest, SIPResponseStatusCodesEnum.BusyHere,
                "Busy Here", sipTransportManager.SipChannel, m_userName);
            // Fire and forget
            sipTransportManager.StartServerInviteTransaction(sipRequest, remoteEndPoint.GetIPEndPoint(),
                null, busyResponse);

            Error?.Invoke("486 Busy Here sent");
            return;
        }

        string? strSdp = sipRequest.GetContentsOfType(ContentTypes.Sdp);
        if (strSdp == null)
        {
            SIPResponse badRequest = SipUtils.BuildResponse(sipRequest, SIPResponseStatusCodesEnum.BadRequest,
                "Bad Request -- No SDP", sipTransportManager.SipChannel, m_userName);
            // Fire and forget
            sipTransportManager.StartServerInviteTransaction(sipRequest, remoteEndPoint.GetIPEndPoint(),
                null, badRequest);
            Error?.Invoke("No SDP in the INVITE request");
            return;
        }

        Sdp OfferedSdp = Sdp.ParseSDP(strSdp);
        MediaDescription? OfferedRttMd = OfferedSdp.GetMediaType("text");
        if (OfferedRttMd == null)
        {
            SIPResponse badRequest = SipUtils.BuildResponse(sipRequest, SIPResponseStatusCodesEnum.BadRequest,
                "Bad Request -- No text media offered", sipTransportManager.SipChannel, m_userName);
            // Fire and forget
            sipTransportManager.StartServerInviteTransaction(sipRequest, remoteEndPoint.GetIPEndPoint(),
                null, badRequest);
            Error?.Invoke("Not text media offered");
            return;
        }

        // Build the SDP to answer with
        RttParameters? OfferedRttParams = RttParameters.FromMediaDescription(OfferedRttMd);
        if (OfferedRttParams == null)
        {
            SIPResponse badRequest = SipUtils.BuildResponse(sipRequest, SIPResponseStatusCodesEnum.BadRequest,
                "Bad Request -- Invalid RTT attributes", sipTransportManager.SipChannel, m_userName);
            // Fire and forget
            sipTransportManager.StartServerInviteTransaction(sipRequest, remoteEndPoint.GetIPEndPoint(),
                null, badRequest);

            Error?.Invoke("Invalid RTT attributes offered");
            return;
        }

        Sdp AnswerSdp = new Sdp(m_localAddress, m_userName);
        MediaDescription AnswerRttMd = OfferedRttParams.ToMediaDescription(RttPort);
        AnswerSdp.Media.Add(AnswerRttMd);

        SIPResponse OkResponse = SipUtils.BuildOkToInvite(sipRequest, sipTransportManager.SipChannel,
            AnswerSdp.ToString(), ContentTypes.Sdp);
        sipTransportManager.StartServerInviteTransaction(sipRequest, remoteEndPoint.GetIPEndPoint(),
            null, OkResponse);

        m_Invite = sipRequest;
        m_OkResponse = OkResponse;

        // Assume that the transaction will complete normally
        (m_RtpChannel, string? ErrorText) = RtpChannel.CreateFromSdp(true, OfferedSdp, OfferedRttMd,
            AnswerSdp, AnswerRttMd, false, m_userName);

        m_RttReceiver = new RttReceiver(OfferedRttParams, m_RtpChannel!, "RttUac");
        m_RttReceiver.RttCharactersReceived += OnRttCharactersReceived;
        m_RttSender = new RttSender(OfferedRttParams, m_RtpChannel!.Send);
        m_RtpChannel.StartListening();
        m_RttSender.Start();

        m_CallState = CallStateEnum.Answered;
    }

    /// <summary>
    /// Sends one or more characters
    /// </summary>
    /// <param name="str"></param>
    public void SendRtt(string str)
    {
        if (m_RttSender == null)
            return;

        m_RttSender.SendMessage(str);
    }

    private void OnRttCharactersReceived(string RxChars, string Source)
    {
        CharactersReceived?.Invoke(RxChars, Source);       
    }

    /// <summary>
    /// The RttUas class either sends a 200 OK or rejects the call with an error response. It does
    /// not send in interim response so it will never receive a CANCEL request.
    /// </summary>
    /// <param name="sipRequest"></param>
    /// <param name="remoteEndPoint"></param>
    /// <param name="sipTransportManager"></param>
    private void ProcessCancelRequest(SIPRequest sipRequest, SIPEndPoint remoteEndPoint, SipTransport sipTransportManager)
    {
    }

    private void ProcessByeRequest(SIPRequest sipRequest, SIPEndPoint remoteEndPoint, SipTransport sipTransportManager)
    {
        if (m_CallState != CallStateEnum.Answered)
        {
            Error?.Invoke("BYE request received but no call on-line");
            return;
        }

        ByeReceived?.Invoke();

        SIPResponse OkResponse = SipUtils.BuildOkToByeOrCancel(sipRequest, remoteEndPoint);
        // Fire and forget
        sipTransportManager.StartServerNonInviteTransaction(sipRequest, remoteEndPoint.GetIPEndPoint(),
            null, OkResponse);

        if (m_RtpChannel != null)
        {
            m_RtpChannel.Shutdown();
            m_RtpChannel = null;
        }

        m_Invite = null;
        m_CallState = CallStateEnum.Idle;
    }

    private void SendMethodNotAllowed(SIPRequest sipRequest, SIPEndPoint remoteEndPoint, SipTransport sipTransportManager)
    {
        SIPResponse sipResponse = SipUtils.BuildResponse(sipRequest, SIPResponseStatusCodesEnum.MethodNotAllowed,
            "Service Unavailable", sipTransportManager.SipChannel, m_userName);

        // Just fire and forget.
        if (sipRequest.Method == SIPMethodsEnum.INVITE)
            sipTransportManager.StartServerInviteTransaction(sipRequest, remoteEndPoint.GetIPEndPoint(), null,
                sipResponse);
        else
            sipTransportManager.StartServerNonInviteTransaction(sipRequest, remoteEndPoint.GetIPEndPoint(),
                null, sipResponse);
    }

    /// <summary>
    /// Stops this UAS. This method must be called before attempting to exit the application.
    /// If there is a call in progress then the call is terminated.
    /// </summary>
    public async Task Stop()
    {
        if (m_Started == false)
            return;

        if (m_RtpChannel != null)
        {
            m_RtpChannel.Shutdown();
            m_RtpChannel = null;
        }

        if (m_CallState == CallStateEnum.Idle)
            return;

        // There is a call online so terminate it.
        if (m_Invite != null && m_OkResponse != null)
        {
            IPEndPoint remIPEndPoint = m_Invite.Header.Contact![0]!.ContactURI!.ToSIPEndPoint()!.GetIPEndPoint();
            SIPRequest byeRequest = SipUtils.BuildByeRequest(m_Invite, m_SipTransport.SipChannel,
                remIPEndPoint, true, m_Invite.Header.CSeq, m_OkResponse);
            
            await m_SipTransport.StartClientNonInviteTransaction(byeRequest, remIPEndPoint, null, 500).
                WaitForCompletionAsync();
        }
    }
}

internal enum CallStateEnum
{
    Idle,
    Answered,
}

