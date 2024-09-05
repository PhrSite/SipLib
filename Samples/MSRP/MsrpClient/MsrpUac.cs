/////////////////////////////////////////////////////////////////////////////////////
//  File:   MsrpUac.cs                                              1 Sep 24 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace MsrpClient;
using SipLib.Core;
using System.Net;
using System.Text;

using SipLib.Transactions;
using SipLib.Sdp;
using SipLib.Body;
using SipLib.Msrp;

/// <summary>
/// Delegate type for the InterimResponseReceived and the CallRejected events of the MsrpUac class.
/// </summary>
/// <param name="status">Indicates the interim response or the reason that the call was rejected.</param>
internal delegate void ResponseReceivedDelegate(SIPResponseStatusCodesEnum status);

/// <summary>
/// Delegate type for the Error event of the RttUac class.
/// </summary>
/// <param name="errorMsg">Describes the error that occurred</param>
internal delegate void ErrorDelegate(string errorMsg);

/// <summary>
/// Delegate type for the MessageReceived event of the MsrpUac class.
/// </summary>
/// <param name="message">Contents of the message that was received</param>
internal delegate void MessageReceivedDelegate(string message);

/// <summary>
/// Implements a simple User Agent Client (UAC) that generates a MSRP (message) media call request.
/// </summary>
internal class MsrpUac
{
    private const int MsrpPort = 5000;

    private SipTransport m_SipTransport;
    private IPAddress m_localAddress;
    private string m_userName;
    private bool m_Started = false;

    private Sdp? m_OfferedSdp = null;
    private SIPRequest? m_Invite = null;
    private MsrpConnection? m_MsrpConnection = null;

    private SIPResponse? m_OkResponse = null;
    private IPEndPoint? m_remoteEndPoint = null;
    private ClientInviteTransaction? m_ClientInviteTransaction = null;

    private CallStateEnum m_CallState = CallStateEnum.Connecting;

    /// <summary>
    /// Event that is fired when an interim SIP response (100 - 199) is received in response to
    /// the INVITE request.
    /// </summary>
    public event ResponseReceivedDelegate? InterimResponseReceived = null;

    /// <summary>
    /// Event that is fired if a timeout occurs when attempting to connect to the server
    /// </summary>
    public event Action? ConnectionTimeout = null;

    /// <summary>
    /// Event that is fired if the server rejects the INVITE request sent by this client.
    /// </summary>
    public event ResponseReceivedDelegate? CallRejected = null;

    /// <summary>
    /// Event that is fired if a 200 OK response is received
    /// </summary>
    public event Action? OkReceived = null;

    /// <summary>
    /// Event that is fired when a BYE request is received from the server
    /// </summary>
    public event Action? ByeReceived = null;

    /// <summary>
    /// Event that is fired when an error occurs
    /// </summary>
    public event ErrorDelegate? Error = null;

    /// <summary>
    /// Event that is fired when a message is received 
    /// </summary>
    public event MsrpTextMessageReceivedDelegate? TextMessageReceived = null;

    /// <summary>
    /// Constructor. Hook the events of the new MsrpUac object, then call the Start() method. Then call
    /// the Call() method to start a call.
    /// </summary>
    /// <param name="sipTransport">SipTransport to use. Must be started</param>
    /// <param name="userName">User name</param>
    public MsrpUac(SipTransport sipTransport, string userName)
    {
        m_SipTransport = sipTransport;
        m_userName = userName;
        m_localAddress = m_SipTransport.SipChannel.SIPChannelEndPoint!.GetIPEndPoint().Address;
    }

    /// <summary>
    /// Hooks the events that are needed and gets ready to send a call.
    /// </summary>
    public void Start()
    {
        // Hook the events that are needed
        m_SipTransport.SipRequestReceived += OnSipRequestReceived;
        m_Started = true;
        return;
    }

    private void OnSipRequestReceived(SIPRequest sipRequest, SIPEndPoint remoteEndPoint, SipTransport sipTransportManager)
    {
        switch (sipRequest.Method)
        {
            case SIPMethodsEnum.BYE:
                ProcessByeRequest(sipRequest, remoteEndPoint, sipTransportManager);
                break;
            default:
                // This user agent client does not accept any incoming requests other than BYE
                SendMethodNotAllowed(sipRequest, remoteEndPoint, sipTransportManager);
                break;
        }
    }

    private void ProcessByeRequest(SIPRequest sipRequest, SIPEndPoint remoteEndPoint, SipTransport sipTransportManager)
    {
        SIPResponse byeResponse = SipUtils.BuildResponse(sipRequest, SIPResponseStatusCodesEnum.Ok, "OK",
            sipTransportManager.SipChannel, m_userName);
        // Fire and forget
        sipTransportManager.StartServerNonInviteTransaction(sipRequest, remoteEndPoint.GetIPEndPoint(),
            null, byeResponse);
        m_CallState = CallStateEnum.Terminated;
        ByeReceived?.Invoke();
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
    /// Stops this UAC. This method must be called before attempting to exit the application.
    /// If there is a call in progress then the call is terminated.
    /// </summary>
    public async Task Stop()
    {
        if (m_Started == false)
            return;

        // If there is a call in progress then stop it
        if (m_CallState == CallStateEnum.InterimResponseReceived || m_CallState == CallStateEnum.Answered)
        {
            SIPRequest request;
            if (m_CallState == CallStateEnum.InterimResponseReceived)
                request = SipUtils.BuildCancelRequest(m_Invite!, m_SipTransport.SipChannel, m_remoteEndPoint!,
                    m_Invite!.Header.CSeq);
            else
                request = SipUtils.BuildByeRequest(m_Invite!, m_SipTransport.SipChannel, m_remoteEndPoint!, true,
                    m_Invite!.Header.CSeq, m_OkResponse!);

            await m_SipTransport.StartClientNonInviteTransaction(request, m_remoteEndPoint!, null).WaitForCompletionAsync();

            if (m_CallState == CallStateEnum.InterimResponseReceived && m_ClientInviteTransaction != null)
            {   // Wait for the client INVITE transaction to complete
                await m_ClientInviteTransaction.WaitForCompletionAsync();
                m_ClientInviteTransaction = null;
            }
        }

        if (m_MsrpConnection != null)
        {
            m_MsrpConnection.Shutdown();
            m_MsrpConnection = null;
        }
    }

    /// <summary>
    /// Builds and sends an INVITE request.
    /// </summary>
    /// <param name="remIPEndPoint">Specifies where to send the INVITE request.</param>
    /// <returns>Returns true if the INVITE transaction is started or false if an error occured</returns>
    public bool Call(IPEndPoint remIPEndPoint)
    {
        if (m_Started == false)
        {
            Error?.Invoke("Error: Call Start() before calling this method");
            return false;
        }

        m_remoteEndPoint = remIPEndPoint;
        string sipScheme = m_SipTransport.SipChannel.IsTLS == true ? "sips" : "sip";
        SIPURI remoteUri = SIPURI.ParseSIPURI($"{sipScheme}:{remIPEndPoint};transport=tcp");
        m_Invite = SIPRequest.CreateBasicRequest(SIPMethodsEnum.INVITE, remoteUri, remoteUri, "MsrpUas",
            m_SipTransport.SipChannel.SIPChannelContactURI, m_userName);

        // Build an SDP offer containing RTT (media type = "text")
        Sdp sdp = new Sdp(m_localAddress, m_userName);
        MediaDescription MsrpMd = SdpUtils.CreateMsrpMediaDescription(m_localAddress, MsrpPort, m_SipTransport.SipChannel.IsTLS);
        sdp.Media.Add(MsrpMd);

        m_OfferedSdp = sdp;     // Save it for later

        // Add the SDP offer to the INVITE request.
        SipBodyBuilder Sbb = new SipBodyBuilder();
        Sbb.AddContent(ContentTypes.Sdp, sdp.ToString(), null, null);
        Sbb.AttachMessageBody(m_Invite);

        m_ClientInviteTransaction = m_SipTransport.StartClientInvite(m_Invite, remIPEndPoint, OnClientInviteComplete,
            OnSipResponseReceived);
        return true;
    }

    /// <summary>
    /// This method is called for interim responses only
    /// </summary>
    /// <param name="Response"></param>
    /// <param name="RemoteEndPoint"></param>
    /// <param name="Transaction"></param>
    private void OnSipResponseReceived(SIPResponse Response, IPEndPoint RemoteEndPoint, SipTransactionBase
        Transaction)
    {
        InterimResponseReceived?.Invoke(Response.Status);
        m_CallState = CallStateEnum.InterimResponseReceived;
    }

    private void OnClientInviteComplete(SIPRequest sipRequest, SIPResponse? sipResponse, IPEndPoint remoteEndPoint,
        SipTransport sipTransport, SipTransactionBase Transaction)
    {
        if (sipResponse == null)
        {
            ConnectionTimeout?.Invoke();
            m_CallState = CallStateEnum.Terminated;
            return;
        }

        if (sipResponse.Status != SIPResponseStatusCodesEnum.Ok)
        {
            CallRejected?.Invoke(sipResponse.Status);
            m_CallState = CallStateEnum.Terminated;
            return;
        }

        OkReceived?.Invoke();

        // Get the answered SDP
        string? strSdp = sipResponse.GetContentsOfType(ContentTypes.Sdp);
        if (strSdp == null)
        {
            Error?.Invoke("INVITE request has no SDP");
            return;
        }

        Sdp sdp = Sdp.ParseSDP(strSdp);
        MediaDescription? msrpMd = sdp.GetMediaType("message");
        if (msrpMd == null || msrpMd.Port == 0)
        {
            Error?.Invoke("No MSRP (message) media offered");
            return;
        }

        (m_MsrpConnection, string? ErrorTxt) = MsrpConnection.CreateFromSdp(m_OfferedSdp!, m_OfferedSdp!.Media[0],
            sdp, msrpMd, false, null);

        if (ErrorTxt != null || m_MsrpConnection == null)
        {
            Error?.Invoke($"{ErrorTxt}");
            return;
        }

        m_MsrpConnection.MsrpTextMessageReceived += OnMsrpTextMessageReceived;
        m_MsrpConnection.Start();
        m_CallState = CallStateEnum.Answered;
    }

    private void OnMsrpTextMessageReceived(string message, string from)
    {
        TextMessageReceived?.Invoke(message, from);
    }

    public void Send(string message)
    {
        if (m_Started == false || m_MsrpConnection == null)
            return;

        m_MsrpConnection.SendMsrpMessage("text/plain", Encoding.UTF8.GetBytes(message));
    }

}

internal enum CallStateEnum
{
    Connecting,
    InterimResponseReceived,
    Answered,
    Terminated
}

