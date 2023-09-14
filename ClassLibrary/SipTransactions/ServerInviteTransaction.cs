/////////////////////////////////////////////////////////////////////////////////////
//  File:   ServerInviteTransaction.cs                              5 Sep 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

using SipLib.Channels;
using SipLib.Core;
using System.Net;

namespace SipLib.Transactions;

/// <summary>
/// Class for managing a SIP server INVITE transaction. See Section 17.2.1 of RFC 3261.
/// </summary>
public class ServerInviteTransaction : SipTransactionBase
{
    private SIPResponse m_InitialResponse = null;
    private DateTime m_TimerGStartTime = DateTime.MinValue;
    private int m_CurrentTimerGInterval = SipTimers.TimerG;
    private DateTime m_TimerHStartTime = DateTime.MinValue;
    private DateTime m_TimerIStartTime = DateTime.MinValue;

    /// <summary>
    /// Constructor. The transaction is not started until the StartTransaction method is called by the
    /// transport layer.
    /// </summary>
    /// <param name="request">SIP request that was received by the server.</param>
    /// <param name="remoteEndPoint">IP endpoint of the remote client that sent the request.</param>
    /// <param name="transactionComplete">Notification callback. Called when the transaction is completed or
    /// terminated. May be null if a notification is not required.</param>
    /// <param name="TransportManager">Transport from which the request was received.</param>
    /// <param name="ResponseToSend">Initial response to send to the client.  Will be sent when the transport
    /// layer calls the StartTransaction() method.</param>
    public ServerInviteTransaction(SIPRequest request, IPEndPoint remoteEndPoint, SipTransactionCompleteDelegate
        transactionComplete, SipTransport TransportManager, SIPResponse ResponseToSend) :
        base(request, remoteEndPoint, transactionComplete, TransportManager)
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
            {   // Provisional response
                State = TransactionStateEnum.Proceeding;
                StateStartTime = DateTime.Now;
            }
            else if (LastSipResponseSent.StatusCode >= 200 && LastSipResponseSent.StatusCode <= 299)
            {
                State = TransactionStateEnum.Terminated;
                Terminated = true;
            }
            else
                // The transaction user already sent a 300 - 699 final response
                EnterCompletedState();
        }

        return Terminated;
    }

    private void EnterCompletedState()
    {
        DateTime Now = DateTime.Now;
        m_TimerGStartTime = Now;
        m_TimerHStartTime = Now;
        StateStartTime = Now;
        State = TransactionStateEnum.Completed;
    }

    /// <summary>
    /// Called periodically by the SIP transport to check for timeouts and resulting state changes.
    /// </summary>
    /// <returns>Returns true if the transaction has been terminated.</returns>
    public override bool DoTimedEvents()
    {
        bool Terminated = false;
        TransactionStateEnum CurrentState = State;
        if (CurrentState == TransactionStateEnum.Terminated)
            return true;

        DateTime Now = DateTime.Now;

        lock (StateLockObj)
        {
            // Check for transport errors.
            if (TransportManager.SipChannel.IsConnectionEstablished(RemoteEndPoint) == false)
            {
                State = TransactionStateEnum.Terminated;
                TerminationReason = TransactionTerminationReasonEnum.ConnectionFailure;
                TransactionComplete?.Invoke(Request, null, RemoteEndPoint, TransportManager, this);
                CompletionSemaphore.Release();
                Terminated = true;
            }
            else if (CurrentState == TransactionStateEnum.Completed)
            {   // Waiting for an ACK request from the client
                if (TransportManager.SipChannel.GetProtocol() == SIPProtocolsEnum.udp)
                {
                    if ((Now - m_TimerGStartTime).TotalMilliseconds > m_CurrentTimerGInterval)
                    {   // Resend the last sent response to the client
                        TransportManager.SendSipResponse(LastSipResponseSent, RemoteEndPoint);
                        if (m_CurrentTimerGInterval > SipTimers.T2)
                            m_CurrentTimerGInterval = SipTimers.T2;
                        else
                            m_CurrentTimerGInterval *= 2;
                    }
                }

                if ((Now - m_TimerHStartTime).TotalMilliseconds > SipTimers.TimerH)
                {
                    State = TransactionStateEnum.Terminated;
                    TerminationReason = TransactionTerminationReasonEnum.NoFinalResponseReceived;
                    // Notify the transaction user
                    TransactionComplete?.Invoke(Request, null, RemoteEndPoint, TransportManager, this);
                    CompletionSemaphore.Release();
                    Terminated = true;
                }
            }
            else if (CurrentState == TransactionStateEnum.Confirmed)
            {
                if (TransportManager.SipChannel.GetProtocol() == SIPProtocolsEnum.udp)
                {   // Not necessary to notify the transaction user.
                    if ((Now - m_TimerIStartTime).TotalMilliseconds > SipTimers.TimerI)
                    {
                        State = TransactionStateEnum.Terminated;
                        Terminated = true;
                    }
                }
                else
                    Terminated = true;
            }
        }

        return Terminated;
    }

    /// <summary>
    /// Sends a response to the INVITE request. The transaction user must use this method to send a
    /// response.
    /// </summary>
    /// <param name="response">SIP response to send</param>
    public void SendResponse(SIPResponse response)
    {
        if (State != TransactionStateEnum.Proceeding)
            return;

        lock (StateLockObj)
        {
            LastSipResponseSent = response;
            TransportManager.SendSipResponse(response, RemoteEndPoint);
            if (response.StatusCode >= 200 && response.StatusCode <= 299)
            {
                State = TransactionStateEnum.Terminated;
                StateStartTime = DateTime.Now;
            }
            else if (response.StatusCode >= 300)
            {   // The transaction user sent a non-200 final response
                EnterCompletedState();
            }

            // Else its an interim response (100 - 199)
        }
    }

    /// <summary>
    /// Handles a SIP request message for this transaction. See Figure 7 of RFC 3261.
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
            if ((CurrentState == TransactionStateEnum.Proceeding || CurrentState == TransactionStateEnum.
                Completed) && Request.Method == SIPMethodsEnum.INVITE)
                TransportManager.SendSipResponse(LastSipResponseSent, RemoteEndPoint);
            else if (CurrentState == TransactionStateEnum.Completed && Request.Method == SIPMethodsEnum.ACK)
            {
                if (TransportManager.SipChannel.GetProtocol() == SIPProtocolsEnum.udp)
                {
                    m_TimerIStartTime = DateTime.Now;
                    State = TransactionStateEnum.Confirmed;
                }
                else
                {   // For TCP and TLS, the value for Timer I is 0 milliseconds, so just terminate the
                    // transaction.
                    State = TransactionStateEnum.Terminated;
                    // Not necessary to notify the transaction user
                    Terminated = true;
                }
            }
        }

        return Terminated;
    }
}
