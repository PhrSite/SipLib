/////////////////////////////////////////////////////////////////////////////////////
//  File:   TransactionStateEnum.cs                                 3 Aug 24 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Transactions;

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
    Trying,

    /// <summary>
    /// The transaction was forcefully terminated because the transaction is a client INVITE transaction
    /// and a CANCEL request was sent but the CANCEL request transaction failed.
    /// </summary>
    ForceTerminated
}

