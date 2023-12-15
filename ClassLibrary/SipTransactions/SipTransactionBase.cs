/////////////////////////////////////////////////////////////////////////////////////
//  File:   SipTransactionBase.cs                                   31 Aug 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

using SipLib.Core;
using System.Net;
using SipLib.Channels;

namespace SipLib.Transactions;

/// <summary>
/// Delegate type for the RequestReceived event of all SipTransactionBase derived classes.
/// </summary>
/// <param name="Request">SIPRequest that the transaction handled.</param>
/// <param name="RemoteEndPoint">Remote endpoint that sent the request.</param>
/// <param name="Transaction">Transaction that fired the event.</param>
public delegate void TransactionRequestReceivedDelegate(SIPRequest Request, IPEndPoint RemoteEndPoint,
    SipTransactionBase Transaction);

/// <summary>
/// Delegate type for the ResponseReceived event of all SipTransactionBase derived classes.
/// </summary>
/// <param name="Response">SIPResponse that the transaction handled.</param>
/// <param name="RemoteEndPoint">Remote endpoint that sent the response.</param>
/// <param name="Transaction">Transaction that fired the event.</param>
public delegate void TransactionResponseReceivedDelegate(SIPResponse Response, IPEndPoint RemoteEndPoint,
    SipTransactionBase Transaction);

/// <summary>
/// Base class for SIP transactions. See Section 17 of RFC 3261.
/// </summary>
public class SipTransactionBase
{
    /// <summary>
    /// Event that is fired when the transaction receives a SIP request.
    /// </summary>
    public TransactionRequestReceivedDelegate RequestReceived = null;

    /// <summary>
    /// Event that is fired when the transaction receives a SIP response.
    /// </summary>
    public TransactionResponseReceivedDelegate ResponseReceived = null;

    /// <summary>
    /// SIP T1 timer in milliseconds.
    /// </summary>
    protected int T1IntervalMs = SipTimers.T1;

    /// <summary>
    /// Time that the transaction started.
    /// </summary>
    protected DateTime TransactionStartTime = DateTime.Now;

    /// <summary>
    /// Time that the request was sent.
    /// </summary>
    protected DateTime RequestSentTime;

    /// <summary>
    /// Current state of the transaction.
    /// </summary>
    protected TransactionStateEnum State;

    /// <summary>
    /// Time that the transaction entered the current state.
    /// </summary>
    protected DateTime StateStartTime = DateTime.Now;

    /// <summary>
    /// Maximum number of transmission attempts for a request
    /// </summary>
    protected const int MaxAttempts = 3;

    /// <summary>
    /// Number of transmission attempts made so far
    /// </summary>
    protected int NumAttempts = 0;

    /// <summary>
    /// Transport manager to use for sending messages
    /// </summary>
    protected SipTransport m_transportManager = null;

    /// <summary>
    /// Gets the SipTransportManager that is managing this transaction
    /// </summary>
    protected SipTransport TransportManager
    {
        get { return m_transportManager; }
    }

    /// <summary>
    /// SIPRequest for the transaction.
    /// </summary>
    public SIPRequest Request = null;

    /// <summary>
    /// Method to call when the transaction either completes or times out
    /// </summary>
    protected SipTransactionCompleteDelegate TransactionComplete = null;

    /// <summary>
    /// Endpoint to send the request to if the transaction is a client transaction or the source of a
    /// request if the transaction is a server transaction.
    /// </summary>
    public IPEndPoint RemoteEndPoint = null;

    /// <summary>
    /// The most recent SIPResponse that was sent to a client if this transaction is a server transaction.
    /// Not used for client transactions.
    /// </summary>
    protected SIPResponse LastSipResponseSent = null;

    private string m_TransactionID = null;

    /// <summary>
    /// Gets the TransactionID that uniquely identifies the transaction
    /// </summary>
    public string TransactionID
    {
        get { return m_TransactionID; }
        protected set { m_TransactionID = value; }
    }

    /// <summary>
    /// Gets the reason that the transaction was terminated.
    /// </summary>
    public TransactionTerminationReasonEnum TerminationReason { get; protected set; } = 
        TransactionTerminationReasonEnum.NoResponseReceived;

    /// <summary>
    /// Gets the last response received for a client transaction. Will be null if a response was never
    /// received.
    /// </summary>
    public SIPResponse LastReceivedResponse { get; protected set; } = null;

    /// <summary>
    /// Semaphore to signal when a transaction is completed or terminated.
    /// </summary>
    protected SemaphoreSlim CompletionSemaphore = new SemaphoreSlim(0);

    /// <summary>
    /// Used by derived classes for locking the state variables
    /// </summary>
    protected object StateLockObj = new object();

    /// <summary>
    /// Asynchronously waits for the transaction to complete.
    /// </summary>
    /// <returns></returns>
    public async Task<SipTransactionBase> WaitForCompletionAsync()
    {
        await CompletionSemaphore.WaitAsync();
        return this;
    }

    /// <summary>
    /// Transaction base class constructor
    /// </summary>
    /// <param name="request">SIP request to send for client transactions or the request that was received
    /// for server transactions</param>
    /// <param name="remoteEndPoint">Destination to send the request to for client transactions or the
    /// source of the request for server transactions</param>
    /// <param name="transactionComplete">Notification callback. May be null if a notification is
    /// not required..</param>
    /// <param name="TransportManager">SipTransportManager that is managing this transaction</param>
    public SipTransactionBase(SIPRequest request, IPEndPoint remoteEndPoint, SipTransactionCompleteDelegate
        transactionComplete, SipTransport TransportManager)
    {
        Request = request;
        RemoteEndPoint = remoteEndPoint;
        TransactionComplete = transactionComplete;
        m_transportManager = TransportManager;
    }

    /// <summary>
    /// Notifies the transaction user that the transaction has either terminated or timed out.
    /// </summary>
    /// <param name="Request">Request relating to the transaction</param>
    /// <param name="Response">Response that was received. May be null if a response was never received</param>
    /// <param name="RemoteEndPoint">Remote endpoint for the transaction.</param>
    protected void NotifyTransactionUser(SIPRequest Request, SIPResponse Response, IPEndPoint RemoteEndPoint)
    {
        TransactionComplete?.Invoke(Request, Response, RemoteEndPoint, TransportManager, this);
        CompletionSemaphore.Release();
    }

    /// <summary>
    /// Calculates the TransactionID for a client transaction. See Section 17.1.3 of RFC 3261.
    /// </summary>
    /// <param name="request">Request that was sent.</param>
    /// <returns>Returns the TransactionID for the request.</returns>
    protected static string GetClientTransactionID(SIPRequest request)
    {
        return request.Header.Vias.TopViaHeader.Branch + request.Header.CSeqMethod.ToString();
    }

    /// <summary>
    /// Calculates the TransactionID for a client transaction given the response. See Section 17.1.3 of RFC 3261.
    /// This method is used for calculating the transaction ID of a SIP response message so that it may
    /// be matched to an existing transaction.
    /// </summary>
    /// <param name="response"></param>
    /// <returns></returns>
    public static string GetClientTransactionID(SIPResponse response)
    {
        return response.Header.Vias.TopViaHeader.Branch + response.Header.CSeqMethod.ToString();
    }

    /// <summary>
    /// Gets the TransactionID for a server transaction. See Section 17.2.3 of RFC 3261.
    /// </summary>
    /// <param name="request">Request that was received</param>
    public static string GetServerTransactionID(SIPRequest request)
    {
        SIPViaHeader Svh = request.Header.Vias.TopViaHeader;
        string method;
        if (request.Method == SIPMethodsEnum.ACK)
            method = SIPMethodsEnum.INVITE.ToString();
        else
            method = request.Method.ToString();
        return Svh.Branch + Svh.Host + method;
    }

    /// <summary>
    /// Handles a SIP request message for this transaction
    /// </summary>
    /// <param name="Request">SIP request that was received from the remote endpoint</param>
    /// <param name="remoteEndPoint">Remote endpoint that sent the request</param>
    /// <returns>Returns true if the transaction has been terminated.</returns>
    public virtual bool HandleSipRequest(SIPRequest Request, IPEndPoint remoteEndPoint)
    {
        return false;
    }

    /// <summary>
    /// Handles a SIP response message for this transaction
    /// </summary>
    /// <param name="Response">SIP request message that was received from the remote endpoint</param>
    /// <param name="remoteEndPoint">Remote endpoint that sent the response</param>
    /// <returns>Returns true if the transaction has been terminated.</returns>
    public virtual bool HandleSipResponse(SIPResponse Response, IPEndPoint remoteEndPoint)
    {
        return false;
    }

    /// <summary>
    /// Called periodically by the SIP transport to check for timeouts and resulting state changes.
    /// </summary>
    /// <returns>Returns true if the transaction has been terminated.</returns>
    public virtual bool DoTimedEvents()
    {
        return false;
    }

    /// <summary>
    /// Called by the SipTransport to start the transaction.
    /// </summary>
    /// <returns>Returns true if the transaction has been immediately terminated.</returns>
    public virtual bool StartTransaction()
    {
        return false;
    }
}

/// <summary>
/// SIP transaction states
/// </summary>
public enum TransactionStateEnum
{
    /// <summary>
    /// The INVITE request has been sent but a provisional resonse has not been received yet. Used for client
    /// INVITE transactions
    /// </summary>
    Calling,
    /// <summary>
    /// The transaction has been comleted. 
    /// </summary>
    Completed,
    /// <summary>
    /// Used for server INVITE transactions. An ACK was received while the transaction was in the Completed state
    /// </summary>
    Confirmed,
    /// <summary>
    /// A provisional response (100 - 199) has been received. Used for the client INVITE and client non-INVITE
    /// transactions.
    /// </summary>
    Proceeding,
    /// <summary>
    /// The transaction has been terminated.
    /// </summary>
    Terminated,
    /// <summary>
    /// The request has been sent but a provisional response has not been received yet. Used by the client
    /// non-INVITE transactions.
    /// </summary>
    Trying
}

/// <summary>
/// Enumeration of the reasons that a transaction was terminated 
/// </summary>
public enum TransactionTerminationReasonEnum
{
    /// <summary>
    /// No response was received for the request.
    /// </summary>
    NoResponseReceived,

    /// <summary>
    /// A 200 OK or other 2XX response was reveived
    /// </summary>
    OkReceived,

    /// <summary>
    /// A final response code (300 - 699) was received
    /// </summary>
    FinalResponseReceived,

    /// <summary>
    /// For client non-INVITE transactions, an interim response was received but a final response was never
    /// received.
    /// </summary>
    NoFinalResponseReceived,

    /// <summary>
    /// A connection failure for a TCP or TLS connection was detected. Does not apply to a UDP.
    /// </summary>
    ConnectionFailure,

    /// <summary>
    /// For server INVITE transactions. Indicates that the server sent a 300 - 699 final response code
    /// to the client, but the client never sent an ACK request.
    /// </summary>
    AckToFinalResponseNotReceived,
}