/////////////////////////////////////////////////////////////////////////////////////
//  File:   ServerNonInviteTransaction.cs                           10 Sep 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

using SipLib.Channels;
using SipLib.Core;
using SipLib.Transactions;
using System.Net;
using System.Transactions;

namespace SipLib.SipTransactions;

/// <summary>
/// Class for handling a server non-INVITE transaction. See Section 17.2.2 of RFC 3261.
/// </summary>
public class ServerNonInviteTransaction : SipTransactionBase
{
    private SIPResponse m_InitialResponse = null;
    private DateTime m_TimerJStartTime = DateTime.MinValue;

    /// <summary>
    /// Constructor. The transaction is not started until the StartTransaction method is called by the
    /// transport layer.
    /// </summary>
    /// <param name="request">SIP request that was received by the server.</param>
    /// <param name="remoteEndPoint">IP endpoint of the remote client that sent the request.</param>
    /// <param name="transactionComplete">Notification callback. Called when the transaction is completed or
    /// terminated. May be null if a notification is not required.</param>
    /// <param name="TransportManager">Transport from which the request was received.</param>
    /// <param name="ResponseToSend">Initial response to send to the client. Will be sent when the transport
    /// layer calls the StartTransaction() method.</param>
    public ServerNonInviteTransaction(SIPRequest request, IPEndPoint remoteEndPoint, 
        SipTransactionCompleteDelegate transactionComplete, SipTransport TransportManager, SIPResponse 
        ResponseToSend) : base(request, remoteEndPoint, transactionComplete, TransportManager)
    {
        m_InitialResponse = ResponseToSend;
        TransactionID = GetServerTransactionID(request);
    }

    /// <summary>
    /// Called by the SipTransport class to start the transaction.
    /// </summary>
    /// <returns>Returns true if the transaction has been immediately terminated.</returns>
    public override bool StartTransaction()
    {
        bool Terminated = false;

        lock (StateLockObj)
        {
            LastSipResponseSent = m_InitialResponse;
            TransportManager.SendSipResponse(LastSipResponseSent, RemoteEndPoint);
            if (LastSipResponseSent.StatusCode < 200)
            {   // Its a provisional response
                StateStartTime = DateTime.Now;
                State = TransactionStateEnum.Proceeding;
            }
            else
                // A final response was sent
                Terminated = EnterCompletedOrTerminateState();
        }

        return Terminated;
    }

    private bool EnterCompletedOrTerminateState()
    {
        bool Terminated = false;
        if (TransportManager.SipChannel.GetProtocol() == SIPProtocolsEnum.udp)
        {
            DateTime Now = DateTime.Now;
            m_TimerJStartTime = Now;
            StateStartTime = Now;
            State = TransactionStateEnum.Completed;
        }
        else
        {   // For TCP and TLS, Timer J is 0 milliseconds so just terminate the transaction.
            // No need to notify the transaction user.
            State = TransactionStateEnum.Terminated;
            Terminated = true;
        }

        return Terminated;
    }

    /// <summary>
    /// Called periodically by the SIP transport to check for timeouts and resulting state changes.
    /// </summary>
    /// <returns>Returns true if the transaction has been terminated.</returns>
    public override bool DoTimedEvents()
    {
        bool Terminated = false;
        lock (StateLockObj)
        {
            if (TransportManager.SipChannel.IsConnectionEstablished(RemoteEndPoint) == false)
            {
                Terminated = true;
                TerminationReason = TransactionTerminationReasonEnum.ConnectionFailure;
                TransactionComplete?.Invoke(Request, null, RemoteEndPoint, TransportManager);
                CompletionSemaphore.Release();
            }
            else
            {
                TransactionStateEnum CurrentState = State;
                DateTime Now = DateTime.Now;
                if (State == TransactionStateEnum.Completed)
                {
                    if ((Now - m_TimerJStartTime).TotalMilliseconds > SipTimers.TimerJ)
                    {
                        State = TransactionStateEnum.Terminated;
                        Terminated = true;
                    }
                }
            }
        }

        return Terminated;
    }

    /// <summary>
    /// Handles a SIP request message for this transaction. See Figure 8 of RFC 3261.
    /// </summary>
    /// <param name="Request">SIP request that was received from the remote endpoint</param>
    /// <param name="remoteEndPoint">Remote endpoint that sent the request</param>
    /// <returns>Returns true if the transaction has been terminated.</returns>
    public override bool HandleSipRequest(SIPRequest Request, IPEndPoint remoteEndPoint)
    {
        bool Terminated = false;
        RequestReceived?.Invoke(Request, remoteEndPoint, this);

        lock (StateLockObj)
        {
            TransactionStateEnum CurrentState = State;
            if (CurrentState != TransactionStateEnum.Terminated)
                TransportManager.SendSipResponse(LastSipResponseSent, remoteEndPoint);
        }

        return Terminated;
    }

    /// <summary>
    /// Sends a response to the non-INVITE request. The transaction user must use this method to send a
    /// response.
    /// </summary>
    /// <param name="response">SIP response to send</param>
    public void SendResponse(SIPResponse response)
    {
        lock (StateLockObj)
        {
            TransactionStateEnum CurrentState = State;
            if (CurrentState == TransactionStateEnum.Proceeding)
            {
                LastSipResponseSent = response;
                TransportManager.SendSipResponse(response, RemoteEndPoint);
                if (response.StatusCode >= 300)
                    EnterCompletedOrTerminateState();
            }
        }
    }
}
