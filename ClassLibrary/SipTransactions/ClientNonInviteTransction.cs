/////////////////////////////////////////////////////////////////////////////////////
//  File:   SipTransaction.cs                                       29 Aug 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

using SipLib.Core;
using System.Net;
using SipLib.Channels;

namespace SipLib.Transactions;

/// <summary>
/// Class for managing a single SIP client non-INVITE transaction. See Section 17.1.2 of RFC 3261.
/// </summary>
public class ClientNonInviteTransaction : SipTransactionBase
{
    private int m_FinalResponseTimeoutMs;

    /// <summary>
    /// Length of time in milliseconds to wait in the Completed state if the transport protocol is UDP.
    /// </summary>
    private int TimerKIntervalMs = SipTimers.T4;

    /// <summary>
    /// Constructor. The transaction is not started until the StartTransaction() method is called by
    /// the transport layer.
    /// </summary>
    /// <param name="request">SIP request to send</param>
    /// <param name="remoteEndPoint">Destination to send the request to</param>
    /// <param name="transactionComplete">Notification callback. Called when the transaction is completed or
    /// terminated. May be null if a notification is
    /// not required.</param>
    /// <param name="TransportManager">SipTransportManager that is managing this transaction</param>
    /// <param name="finalResponseTimeoutMs">Number of milliseconds to wait for a final response.
    /// This corresponds to Timer F shown in Figure 6 of RFC 3261.</param>
    public ClientNonInviteTransaction(SIPRequest request, IPEndPoint remoteEndPoint, SipTransactionCompleteDelegate
        transactionComplete, SipTransport TransportManager, int finalResponseTimeoutMs) :
        base(request, remoteEndPoint, transactionComplete, TransportManager)
    {
        m_FinalResponseTimeoutMs = finalResponseTimeoutMs;
        TransactionID = GetClientTransactionID(request);
    }

    /// <summary>
    /// Called by the SipTransport to start the transaction.
    /// </summary>
    /// <returns>Returns true if the transaction has been immediately terminated.</returns>
    public override bool StartTransaction()
    {
        lock (StateLockObj)
        {
            State = TransactionStateEnum.Trying;
            DateTime Now = DateTime.Now;
            RequestSentTime = Now;
            StateStartTime = Now;
            NumAttempts = 0;
            TransportManager.SendSipRequest(Request, RemoteEndPoint);
        }

        return false;
    }

    /// <summary>
    /// Handles a SIP response message for this transaction
    /// </summary>
    /// <param name="Response">SIP request message that was received from the remote endpoint</param>
    /// <param name="remoteEndPoint">Remote endpoint that sent the response</param>
    /// <returns>Returns true if the transaction has been terminated.</returns>
    public override bool HandleSipResponse(SIPResponse Response, IPEndPoint remoteEndPoint)
    {
        ResponseReceived?.Invoke(Response, remoteEndPoint, this);
        bool Terminated = false;

        lock (StateLockObj)
        {
            LastReceivedResponse = Response;
            if (Response.StatusCode >= 100 && Response.StatusCode <= 199)
            {
                if (State == TransactionStateEnum.Trying)
                {
                    // Now wait for a final response (200 - 699) or a timeout to occur.
                    State = TransactionStateEnum.Proceeding;
                    StateStartTime = DateTime.Now;
                }

                // Else, its OK to ignore provisional responses when not in the Trying state.
            }
            else
            {   // Its a final response, complete the transaction
                TerminationReason = TransactionTerminationReasonEnum.FinalResponseReceived;
                if (State != TransactionStateEnum.Completed)
                {   // Entering the Completed state so notify the transaction user.
                    State = TransactionStateEnum.Completed;
                    StateStartTime = DateTime.Now;
                    if (TransportManager.SipChannel.GetProtocol() != SIPProtocolsEnum.udp)
                    {   // For TCP and TLS, the value for the Timer F interval is 0 milliseconds, so terminate
                        // the transaction.
                        State = TransactionStateEnum.Terminated;
                        Terminated = true;
                    }
                    // For UDP, must stay in the Completed state for TimerFIntervalMs.

                    NotifyTransactionUser(Request, Response, RemoteEndPoint);
                }
            }
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
        DateTime Now = DateTime.Now;

        lock (StateLockObj)
        {
            if (State == TransactionStateEnum.Trying)
            {
                if ((Now - RequestSentTime).TotalMilliseconds > T1IntervalMs)
                {   // A timeout has occurred, try again?
                    NumAttempts += 1;
                    if (NumAttempts >= MaxAttempts)
                    {   // The transaction failed so notify the transaction user
                        State = TransactionStateEnum.Terminated;
                        TerminationReason = TransactionTerminationReasonEnum.NoResponseReceived;
                        NotifyTransactionUser(Request, null, RemoteEndPoint);
                        Terminated = true;
                    }
                    else
                    {   // Send the request again
                        RequestSentTime = Now;
                        TransportManager.SendSipRequest(Request, RemoteEndPoint);
                    }
                }
            }
            else if (State == TransactionStateEnum.Proceeding)
            {   // Waiting for a final response, check for a timeout
                if ((Now - StateStartTime).TotalMilliseconds > m_FinalResponseTimeoutMs)
                {   // A timeout occurred and a final response never received
                    TerminationReason = TransactionTerminationReasonEnum.NoFinalResponseReceived;
                    State = TransactionStateEnum.Terminated;
                    NotifyTransactionUser(Request, null, RemoteEndPoint);
                    Terminated = true;
                }
            }
            else if (State == TransactionStateEnum.Completed)
            {
                if ((Now - StateStartTime).TotalMilliseconds > TimerKIntervalMs)
                {   // No need to notify the transaction user
                    State = TransactionStateEnum.Terminated;
                    Terminated = true;
                }
            }
        }

        return Terminated;
    }
}
