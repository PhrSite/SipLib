/////////////////////////////////////////////////////////////////////////////////////
//  File:   SipTransaction.cs                                       29 Aug 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

using SipLib.Core;
using System.Net;
using SipLib.Channels;

namespace SipLib.Transactions;

/// <summary>
/// Data class for a single SIP transaction.
/// </summary>
public class ClientNonInviteTransaction : SipTransactionBase
{
    /// <summary>
    /// 
    /// </summary>
    public int FinalResponseTimeoutMs;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="request">SIP request to end</param>
    /// <param name="remoteEndPoint">Destination to send the request to</param>
    /// <param name="transactionComplete">Notification callback. May be null if a notification is
    /// not required.</param>
    /// <param name="TransportManager">SipTransportManager that is managing this transaction</param>
    /// <param name="finalResponseTimeoutMs">Number of milliseconds to wait for a final response</param>
    public ClientNonInviteTransaction(SIPRequest request, IPEndPoint remoteEndPoint, SipTransactionCompleteDelegate
        transactionComplete, SipTransportManager TransportManager, int finalResponseTimeoutMs) :
        base(request, remoteEndPoint, transactionComplete, TransportManager)
    {
        FinalResponseTimeoutMs = finalResponseTimeoutMs;
        TransactionID = GetClientTransactionID(request);


    }
}
