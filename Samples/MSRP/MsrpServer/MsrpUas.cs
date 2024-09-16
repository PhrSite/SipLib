/////////////////////////////////////////////////////////////////////////////////////
//  File:   MsrpUas.cs                                              4 Sep 24 PHR
/////////////////////////////////////////////////////////////////////////////////////

using System.Net;
using System.Text;
using System.Security.Cryptography.X509Certificates;

using SipLib.Body;
using SipLib.Core;
using SipLib.Msrp;
using SipLib.Sdp;
using SipLib.Transactions;

namespace MsrpServer;

/// <summary>
/// Delegate for the InviteReceived event of the RttUas class.
/// </summary>
/// <param name="from">Source of the INVITE request</param>
internal delegate void InviteReceivedDelegate(string from);

/// <summary>
/// Delegate type for the MessageReceived event of the MsrpUas class.
/// </summary>
/// <param name="message">Contents of the message that was received</param>
internal delegate void MessageReceivedDelegate(string message);

/// <summary>
/// Delegate type for the Error event of the RttUas class.
/// </summary>
/// <param name="errorMsg">Describes the error that occurred</param>
internal delegate void ErrorDelegate(string errorMsg);

/// <summary>
/// Implements a simple User Agent Server that handles MSRP (message) media calls.
/// </summary>
internal class MsrpUas
{
    private const int MsrpPort = 2855;

    private SipTransport m_SipTransport;
    private IPAddress m_localAddress;
    private string m_userName;
    private bool m_Started = false;

    private SIPRequest? m_Invite = null;
    private SIPResponse? m_OkResponse = null;
    private CallStateEnum m_CallState = CallStateEnum.Idle;
    private MsrpConnection? m_MsrpConnection = null;
    private X509Certificate2 m_MyCertificate;

    /// <summary>
    /// Event that is fired when an INVITE request is received
    /// </summary>
    public event InviteReceivedDelegate? InviteReceived = null;

    /// <summary>
    /// Event that is fired when a message is received 
    /// </summary>
    public event MsrpTextMessageReceivedDelegate? TextMessageReceived = null;

    /// <summary>
    /// Event that is fired when a BYE request is received.
    /// </summary>
    public event Action? ByeReceived = null;

    /// <summary>
    /// Event that is fired if an error is detected.
    /// </summary>
    public event ErrorDelegate? Error = null;

    /// <summary>
    /// Constructor. Hook the events of the new MsrpUas object, then call the Start() method.
    /// </summary>
    /// <param name="sipTransport">SipTransport to use. Must be started</param>
    /// <param name="userName">User name</param>
    /// <param name="myCertificate">X.509 certificate for the MsrpConnection</param>
    public MsrpUas(SipTransport sipTransport, string userName, X509Certificate2 myCertificate)
    {
        m_SipTransport = sipTransport;
        m_userName = userName;
        m_localAddress = m_SipTransport.SipChannel.SIPChannelEndPoint!.GetIPEndPoint().Address;
        m_MyCertificate = myCertificate;
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

        SIPResponse trying = SipUtils.BuildResponse(sipRequest, SIPResponseStatusCodesEnum.Trying,
            "Trying", sipTransportManager.SipChannel, m_userName);
        sipTransportManager.SendSipResponse(trying, remoteEndPoint.GetIPEndPoint());

        SIPResponse ringing = SipUtils.BuildResponse(sipRequest, SIPResponseStatusCodesEnum.Ringing,
            "Ringing", sipTransportManager.SipChannel, m_userName);
        sipTransportManager.SendSipResponse(ringing, remoteEndPoint.GetIPEndPoint());

        Sdp OfferedSdp = Sdp.ParseSDP(strSdp);
        MediaDescription? OfferedMsrpMd = OfferedSdp.GetMediaType("message");
        if (OfferedMsrpMd == null)
        {
            SIPResponse badRequest = SipUtils.BuildResponse(sipRequest, SIPResponseStatusCodesEnum.BadRequest,
                "Bad Request -- No message media offered", sipTransportManager.SipChannel, m_userName);
            // Fire and forget
            sipTransportManager.StartServerInviteTransaction(sipRequest, remoteEndPoint.GetIPEndPoint(),
                null, badRequest);
            Error?.Invoke("No MSRP (message) media offered");
            return;
        }

        // Build the SDP to answer with
        Sdp AnswerSdp = new Sdp(m_localAddress, m_userName);
        MediaDescription AnswerMd = SdpUtils.CreateMsrpMediaDescription(m_localAddress, MsrpPort,
            false, SetupType.passive, null, m_userName);
        AnswerSdp.Media.Add(AnswerMd);

        (m_MsrpConnection, string? ErrorText) = MsrpConnection.CreateFromSdp(OfferedMsrpMd,
            AnswerMd, true, m_MyCertificate);

        if (m_MsrpConnection == null)
        {
            Error?.Invoke($"Error: {ErrorText}");
            return;
        }

        m_MsrpConnection.Start();
        m_MsrpConnection.MsrpTextMessageReceived += OnMsrpTextMessageReceived;

        SIPResponse OkResponse = SipUtils.BuildOkToInvite(sipRequest, sipTransportManager.SipChannel,
            AnswerSdp.ToString(), ContentTypes.Sdp);
        sipTransportManager.StartServerInviteTransaction(sipRequest, remoteEndPoint.GetIPEndPoint(),
            null, OkResponse);

        m_Invite = sipRequest;
        m_OkResponse = OkResponse;

        m_CallState = CallStateEnum.Answered;
    }

    private void OnMsrpTextMessageReceived(string message, string from)
    {
       TextMessageReceived?.Invoke(message, from);
    }

    /// <summary>
    /// Sends a text/plain MSRP message
    /// </summary>
    /// <param name="str">Message to send</param>
    public void Send(string str)
    {
        if (m_MsrpConnection == null)
            return;

        m_MsrpConnection.SendMsrpMessage("text/plain", Encoding.UTF8.GetBytes(str));
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

        if (m_MsrpConnection != null)
        {
            m_MsrpConnection.Shutdown();
            m_MsrpConnection = null;
        }

        m_Invite = null;
        m_CallState = CallStateEnum.Idle;
    }

    private void SendMethodNotAllowed(SIPRequest sipRequest, SIPEndPoint remoteEndPoint, SipTransport sipTransportManager)
    {
        SIPResponse sipResponse = SipUtils.BuildResponse(sipRequest, SIPResponseStatusCodesEnum.MethodNotAllowed,
            "Method Not Allowed", sipTransportManager.SipChannel, m_userName);

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

        if (m_MsrpConnection != null)
        {
            m_MsrpConnection.Shutdown();
            m_MsrpConnection = null;
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

