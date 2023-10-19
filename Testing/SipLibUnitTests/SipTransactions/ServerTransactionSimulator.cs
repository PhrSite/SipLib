/////////////////////////////////////////////////////////////////////////////////////
//  File:   ServerTransport.cs                                      12 Sep 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

using System.Net;
using SipLib.Channels;
using SipLib.Core;
using SipLib.Sdp;
using SipLib.SipTransactions;
using SipLib.Transactions;

namespace SipLibUnitTests.SipTransactions;

/// <summary>
/// 
/// </summary>
internal class ServerTransactionSimulator
{
    private SipTransport m_Transport;
    private ServerInviteTransaction m_InviteTransaction = null;
    private bool m_AnswerInvite;
    private bool m_AnswerOptions;
    private IPEndPoint m_ServerIpe;

    public ServerTransactionSimulator(IPEndPoint ServerIpe, bool AnswerInvite, bool AnswerOptions)
    {
        m_ServerIpe = ServerIpe;
        m_AnswerInvite = AnswerInvite;
        m_AnswerOptions = AnswerOptions;
        SIPTCPChannel ServerChannel = new SIPTCPChannel(ServerIpe, "Server");
        m_Transport = new SipTransport(ServerChannel);
        m_Transport.SipRequestReceived += OnSipRequestReceived;
        m_Transport.Start();
    }

    public int TransactionCount
    {
        get { return m_Transport.TransactionCount; }
    }

    public void Shutdown()
    {
        m_Transport.Shutdown();
    }

    private void OnSipRequestReceived(SIPRequest sipRequest, SIPEndPoint remoteEndPoint,
        SipTransport sipTransport)
    {
        switch (sipRequest.Method)
        {
            case SIPMethodsEnum.OPTIONS:
                ProcessOptions(sipRequest, remoteEndPoint, sipTransport);
                break;
            case SIPMethodsEnum.INVITE:
                ProcessInvite(sipRequest, remoteEndPoint, sipTransport);
                break;
            case SIPMethodsEnum.ACK:
                ProcessAck(sipRequest, remoteEndPoint, sipTransport);
                break;
            case SIPMethodsEnum.BYE:
                ProcessBye(sipRequest, remoteEndPoint, sipTransport);
                break;
            case SIPMethodsEnum.CANCEL:
                ProcessCancel(sipRequest, remoteEndPoint, sipTransport);
                break;

        }
    }

    private void ProcessOptions(SIPRequest sipRequest, SIPEndPoint remoteEndPoint,
        SipTransport sipTransport)
    {
        if (m_AnswerOptions == false)
            return;

        IPEndPoint RemIpe = remoteEndPoint.GetIPEndPoint();
        SIPResponse OkResponse = SipUtils.BuildResponse(sipRequest, SIPResponseStatusCodesEnum.Ok,
            "OK", sipTransport.SipChannel, null);
        ServerNonInviteTransaction Snit = sipTransport.StartServerNonInviteTransaction(sipRequest, RemIpe,
            null, OkResponse);
    }

    private void ProcessInvite(SIPRequest sipRequest, SIPEndPoint remoteEndPoint, SipTransport sipTransport)
    {
        IPEndPoint RemIpe = remoteEndPoint.GetIPEndPoint();
        SIPResponse TryingResp = SipUtils.BuildResponse(sipRequest, SIPResponseStatusCodesEnum.Trying,
            "Trying", sipTransport.SipChannel, null);

        //m_InviteTransaction = new ServerInviteTransaction(sipRequest, RemIpe, null, sipTransport, TryingResp);
        //sipTransport.StartSipTransaction(m_InviteTransaction);
        m_InviteTransaction = sipTransport.StartServerInviteTransaction(sipRequest, RemIpe, null,
            TryingResp);

        SIPResponse RingingResponse = SipUtils.BuildResponse(sipRequest, SIPResponseStatusCodesEnum.Ringing,
            "Ringing", sipTransport.SipChannel, null);
        m_InviteTransaction.SendResponse(RingingResponse);

        if (m_AnswerInvite == true)
        {
            SIPResponse OkResp = SipUtils.BuildResponse(sipRequest, SIPResponseStatusCodesEnum.Ok, "OK",
                sipTransport.SipChannel, null);
            SipLib.Sdp.Sdp AudioSdp = SdpUtils.BuildSimpleAudioSdp(m_ServerIpe.Address, 7000, "Server");
            OkResp.Header.ContentType = "application/sdp";
            OkResp.Body = AudioSdp.ToString();
            OkResp.Header.ContentLength = OkResp.Body.Length;
            m_InviteTransaction.SendResponse(OkResp);
        }
    }

    private void ProcessAck(SIPRequest sipRequest, SIPEndPoint remoteEndPoint,
        SipTransport sipTransport)
    {

    }

    private void ProcessBye(SIPRequest sipRequest, SIPEndPoint remoteEndPoint,
        SipTransport sipTransport)
    {
        SIPResponse ByeResp = SipUtils.BuildResponse(sipRequest, SIPResponseStatusCodesEnum.Ok, "OK",
            sipTransport.SipChannel, null);
        ServerNonInviteTransaction Snit = sipTransport.StartServerNonInviteTransaction(sipRequest,
            remoteEndPoint.GetIPEndPoint(), null, ByeResp);
    }

    private void ProcessCancel(SIPRequest sipRequest, SIPEndPoint remoteEndPoint,
        SipTransport sipTransport)
    {
        if (m_InviteTransaction == null)
            return;

        SIPResponse OkResponse = SipUtils.BuildResponse(sipRequest, SIPResponseStatusCodesEnum.Ok, "OK",
            sipTransport.SipChannel, null);
        ServerNonInviteTransaction Snit = sipTransport.StartServerNonInviteTransaction(sipRequest,
            remoteEndPoint.GetIPEndPoint(), null, OkResponse);

        SIPResponse TermResp = SipUtils.BuildResponse(m_InviteTransaction.Request, 
            SIPResponseStatusCodesEnum.RequestTerminated, "Request Terminated", sipTransport.SipChannel, null);
        m_InviteTransaction.SendResponse(TermResp);

    }
}
