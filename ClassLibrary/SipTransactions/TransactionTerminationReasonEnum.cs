/////////////////////////////////////////////////////////////////////////////////////
//  File:   TransactionTerminationReasonEnum.cs                     3 Aug 24 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Transactions;

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
    /// A 200 OK or other 2XX response was received
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

    /// <summary>
    /// Used for client INVITE requests. An client INVITE request was sent. Then the client sent a
    /// client CANCEL but the CANCEL transaction failed so the original client INVITE transaction was
    /// forcefully terminated by the transaction manager.
    /// </summary>
    CancelRequestFailed,
}
