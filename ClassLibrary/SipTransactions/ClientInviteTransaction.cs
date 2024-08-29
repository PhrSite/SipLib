/////////////////////////////////////////////////////////////////////////////////////
//  File:   ClientInviteTransaction.cs                              2 Sep 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

using SipLib.Core;
using System.Net;
using SipLib.Channels;

namespace SipLib.Transactions;

/// <summary>
/// Class for managing a single SIP client INVITE transaction. See Section 17.1.1 of RFC 3261.
/// </summary>
public class ClientInviteTransaction : SipTransactionBase
{
    /// <summary>
    /// Constructor. The transaction is not started until the StartTransaction method is called by the
    /// transport layer.
    /// </summary>
    /// <param name="request">SIP INVITE request that will be send by the client.</param>
    /// <param name="remoteEndPoint">Destination to send the request to</param>
    /// <param name="transactionComplete">Notification callback. Called when the transaction is completed or
    /// terminated. May be null if a notification is not required.</param>
    /// <param name="TransportManager">SipTransport that is managing this transaction</param>
    public ClientInviteTransaction(SIPRequest request, IPEndPoint remoteEndPoint, SipTransactionCompleteDelegate
        transactionComplete, SipTransport TransportManager) :
        base(request, remoteEndPoint, transactionComplete, TransportManager)
    {
        TransactionID = GetClientTransactionID(request);
    }

    /// <summary>
    /// Called by the SipTransport class to start the transaction.
    /// </summary>
    /// <returns>Returns true if the transaction has been immediately terminated.</returns>
    internal override bool StartTransaction()
    {
        lock (StateLockObj)
        {
            State = TransactionStateEnum.Calling;
            DateTime Now = DateTime.Now;
            TransactionStartTime = Now;
            RequestSentTime = Now;
            StateStartTime = Now;
            TransportManager.SendSipRequest(Request, RemoteEndPoint);
        }

        return false;
    }

    /// <summary>
    /// Handles a SIP response message for this transaction. See Figure 5 of RFC 3261.
    /// </summary>
    /// <param name="Response">SIP response message that was received from the remote endpoint</param>
    /// <param name="remoteEndPoint">Remote endpoint that sent the response</param>
    /// <returns>Returns true if the transaction has been terminated.</returns>
    internal override bool HandleSipResponse(SIPResponse Response, IPEndPoint remoteEndPoint)
    {
        bool Terminated = false;
        if (Response.StatusCode > 100 &&  Response.StatusCode < 200)
            // Notify the transaction user of provisional responses except 100 Trying
            ResponseReceived?.Invoke(Response, remoteEndPoint, this);

        lock (StateLockObj)
        {
            LastReceivedResponse = Response;
            if (Response.StatusCode >= 100 && Response.StatusCode <= 199)
            {
                if (State == TransactionStateEnum.Calling)
                {   // Now wait for a final response (200 - 699) or a timeout to occur.
                    State = TransactionStateEnum.Proceeding;
                    StateStartTime = DateTime.Now;
                }
                // Else, its OK to ignore provisional responses when not in the Calling state.
            }
            else if (Response.StatusCode >= 200 && Response.StatusCode <= 299)
            {   // Its a 200 OK or other 2XX response. The transaction user must build and send the ACK request
                // for the 200 OK request.
                SIPRequest AckOkReq = SipUtils.BuildAckRequest(Response, m_transportManager.SipChannel);
                TransportManager.SendSipRequest(AckOkReq, RemoteEndPoint);

                State = TransactionStateEnum.Terminated;
                StateStartTime = DateTime.Now;
                TerminationReason = TransactionTerminationReasonEnum.OkReceived;
                NotifyTransactionUser(Request, Response, RemoteEndPoint);
                Terminated = true;
            }
            else
            {   // Its a final response (300 - 699), complete the transaction so send an ACK request.
                SIPRequest AckReq = SipUtils.BuildAckRequest(Response, m_transportManager.SipChannel);
                TransportManager.SendSipRequest(AckReq, RemoteEndPoint);

                if (State != TransactionStateEnum.Completed)
                {   // Entering the Completed state so notify the transaction user.
                    State = TransactionStateEnum.Completed;
                    StateStartTime = DateTime.Now;
                    TerminationReason = TransactionTerminationReasonEnum.FinalResponseReceived;
                    NotifyTransactionUser(Request, Response, RemoteEndPoint);

                    if (TransportManager.SipChannel.GetProtocol() != SIPProtocolsEnum.udp)
                    {   // For TCP and TLS, the value for the Timer F interval is 0 milliseconds, so terminate
                        // the transaction.
                        State = TransactionStateEnum.Terminated;
                        Terminated = true;
                    }
                    // For UDP, must stay in the Completed state for TimerFIntervalMs.
                }
            }
        }

        return Terminated;
    }

    /// <summary>
    /// Called periodically by the SIP transport to check for timeouts and resulting state changes.
    /// </summary>
    /// <returns>Returns true if the transaction has been terminated.</returns>
    internal override bool DoTimedEvents()
    {
        bool Terminated = false;
        DateTime Now = DateTime.Now;

        lock (StateLockObj)
        {
            if (State == TransactionStateEnum.Calling)
            {
                if ((Now - StateStartTime).TotalMilliseconds > SipTimers.TimerB)
                {   // The Calling state timed out because no response was received
                    State = TransactionStateEnum.Terminated;
                    TerminationReason = TransactionTerminationReasonEnum.NoResponseReceived;
                    NotifyTransactionUser(Request, null, RemoteEndPoint);
                    return true;
                }

                if ((Now - RequestSentTime).TotalMilliseconds > T1IntervalMs)
                {   // A timeout has occurred, try again
                    RequestSentTime = Now;
                    TransportManager.SendSipRequest(Request, RemoteEndPoint);
                    T1IntervalMs = T1IntervalMs * 2;
                }
            }
            else if (State == TransactionStateEnum.Proceeding)
            {   // Waiting for a final response

                // Check for transport errors.
                if (TransportManager.SipChannel.IsConnectionEstablished(RemoteEndPoint) == false)
                {
                    State = TransactionStateEnum.Terminated;
                    Terminated = true;
                    TerminationReason = TransactionTerminationReasonEnum.ConnectionFailure;
                    NotifyTransactionUser(Request, null, RemoteEndPoint);
                }
            }
            else if (State == TransactionStateEnum.Completed)
            {
                // This state is entered only if using UDP so there is no connection to fail.

                if ((Now - StateStartTime).TotalMilliseconds > SipTimers.TimerD)
                {   // No need to notify the transaction user
                    State = TransactionStateEnum.Terminated;
                    Terminated = true;
                    // In this case, there is no need to notify the transaction user
                }
            }
            else if (State == TransactionStateEnum.ForceTerminated)
                Terminated = true;
        }

        return Terminated;
    }

    /// <summary>
    /// Cancels the client INVITE request transaction by building and sending a CANCEL request.
    /// </summary>
    /// <returns>Returns true if a CANCEL request is sent. Returns false if a CANCEL request was not
    /// sent because the INVITE transaction is not in the Proceeding state. If false is returned then
    /// the caller must wait until an interim response is received and then try again later.</returns>
    public bool CancelInvite()
    {
        bool Result = false;
        lock (StateLockObj)
        {
            if (State == TransactionStateEnum.Proceeding)
            {   // See Section 9.1 of RFC 3261. If a provisional response has been received then its
                // OK to send a CANCEL request. 
                Result = true;
                SIPRequest cancelRequest = SipUtils.BuildCancelRequest(Request,
                    TransportManager.SipChannel, RemoteEndPoint, Request.Header.CSeq);
                TransportManager.StartClientNonInviteTransaction(cancelRequest, RemoteEndPoint, 
                    OnCancelTransactionComplete, 500);
            }
            // For any other state of the client INVITE transaction, a CANCEL request must not be sent.
        }

        return Result;
    }

    /// <summary>
    /// Called when a client CANCEL request has been completed or failed.
    /// </summary>
    /// <param name="sipRequest">The CANCEL request that was sent.</param>
    /// <param name="sipResponse">If not null, then contains the response message. If null then the
    /// CANCEL transaction failed.</param>
    /// <param name="remoteEndPoint">The IP endpoint that sent the response</param>
    /// <param name="sipTransport">The SipTransport that was used to send the CANCEL request</param>
    /// <param name="Transaction">The transaction object for the CANCEL request</param>
    private void OnCancelTransactionComplete(SIPRequest sipRequest, SIPResponse? sipResponse,
        IPEndPoint remoteEndPoint, SipTransport sipTransport, SipTransactionBase Transaction)
    {
        if (sipResponse  == null)
        {
            if (sipResponse.Status == SIPResponseStatusCodesEnum.Ok)
            {   // The CANCEL transaction was successful. The server should send a 487 Request Terminated
                // response for the original client INVITE transaction (this object).
                // No action is required here.
            }
            else
            {
                ForceTerminateTransacton();
            }    
        }
        else
        {   // The transaction for the CANCEL request failed
            ForceTerminateTransacton();
        }
    }

    private void ForceTerminateTransacton()
    {
        lock (StateLockObj)
        {
            State = TransactionStateEnum.ForceTerminated;
        }
    }
}
