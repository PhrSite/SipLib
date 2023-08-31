/////////////////////////////////////////////////////////////////////////////////////
//  File:   SipTransactionBase.cs                                   31 Aug 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

using SipLib.Core;
using System.Net;
using SipLib.Channels;

namespace SipLib.Transactions;

/// <summary>
/// Base class for SIP transactions
/// </summary>
public class SipTransactionBase
{
    /// <summary>
    /// Default value for the SIP T1 timer for the UDP transport protocol in milliseconds
    /// </summary>
    protected const int UdpDefaultT1IntervalMs = 500;
    /// <summary>
    /// Default value for the SIP T1 timer for the TCP transport protocol in milliseconds
    /// </summary>
    protected const int TcpDefaultT1IntervalMs = 500;
    /// <summary>
    /// Default value for the SIP T1 timer for the TLS transport protocol in milliseconds
    /// </summary>
    protected const int TlsDefaultT1IntervalMs = 1000;

    /// <summary>
    /// SIP T1 timer in milliseconds.
    /// </summary>
    public int T1IntervalMs = UdpDefaultT1IntervalMs;

    /// <summary>
    /// Time that the request was sent.
    /// </summary>
    public DateTime RequestSendTime;

    /// <summary>
    /// Current state of the transaction.
    /// </summary>
    public TransactionStateEnum State;

    /// <summary>
    /// Time that the transaction entered the current state.
    /// </summary>
    public DateTime StartStartTime = DateTime.Now;

    /// <summary>
    /// Maximum number of transmission attempts for a request
    /// </summary>
    public const int MaxAttempts = 3;

    /// <summary>
    /// Number of transmission attempts made so far
    /// </summary>
    public int NumAttempts = 0;

    /// <summary>
    /// Transport manager to use for sending messages
    /// </summary>
    protected SipTransportManager m_transportManager = null;

    /// <summary>
    /// Gets the SipTransportManager that is managing this transaction
    /// </summary>
    public SipTransportManager TransportManager
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
    public SipTransactionCompleteDelegate TransactionComplete = null;

    /// <summary>
    /// Endpoint to send the request to.
    /// </summary>
    public IPEndPoint RemoteEndPoint = null;

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
    /// Constructor
    /// </summary>
    /// <param name="request">SIP request to send for client transactions or the request that was received
    /// for server transactions</param>
    /// <param name="remoteEndPoint">Destination to send the request to for client transactions or the
    /// source of the request for server transactions</param>
    /// <param name="transactionComplete">Notification callback. May be null if a notification is
    /// not required..</param>
    /// <param name="TransportManager">SipTransportManager that is managing this transaction</param>
    public SipTransactionBase(SIPRequest request, IPEndPoint remoteEndPoint, SipTransactionCompleteDelegate
        transactionComplete, SipTransportManager TransportManager)
    {
        Request = request;
        RemoteEndPoint = remoteEndPoint;
        TransactionComplete = transactionComplete;
        m_transportManager = TransportManager;

        switch (this.m_transportManager.SipChannel.GetProtocol())
        {
            case SIPProtocolsEnum.udp:
                T1IntervalMs = UdpDefaultT1IntervalMs;
                break;
            case SIPProtocolsEnum.tcp:
                T1IntervalMs = TcpDefaultT1IntervalMs;
                break;
            case SIPProtocolsEnum.tls:
                T1IntervalMs = TlsDefaultT1IntervalMs;
                break;
            default:
                T1IntervalMs = UdpDefaultT1IntervalMs;
                break;
        }

    }

    /// <summary>
    /// Gets the TransactionID for a client transaction. See Section 17.1.3 of RFC 3261.
    /// </summary>
    /// <param name="request">Request that was sent.</param>
    public static string GetClientTransactionID(SIPRequest request)
    {
        return request.Header.Vias.TopViaHeader.Branch + request.Header.CSeqMethod.ToString();
    }

    /// <summary>
    /// Gets the TransactionID for a server transaction. See Section 17.2.3 of RFC 3261.
    /// </summary>
    /// <param name="request">Request that was received</param>
    public static string SetServerTransactionID(SIPRequest request)
    {
        SIPViaHeader Svh = request.Header.Vias.TopViaHeader;
        return Svh.Branch + Svh.Host + request.Method.ToString();
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