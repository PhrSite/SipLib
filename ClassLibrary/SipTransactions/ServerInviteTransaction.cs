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

    /// <summary>
    /// Constructor. The transaction is not started until the StartTransaction method is called by the
    /// transport layer.
    /// </summary>
    /// <param name="request">SIP request that was received by the server.</param>
    /// <param name="remoteEndPoint">IP endpoint of the remote client that sent the request.</param>
    /// <param name="transactionComplete">Notification callback. Called when the transaction is completed or
    /// terminated. May be null if a notification is not required.</param>
    /// <param name="TransportManager">Transport from which the request was received.</param>
    /// <param name="ResponseToSend">Initial response to send to the client. If null, then this
    /// class will send a 100 Trying response when the transport layer calls the StartTransaction() method.</param>
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
    public override void StartTransaction()
    {
      
    }
}
