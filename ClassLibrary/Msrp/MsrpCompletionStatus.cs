/////////////////////////////////////////////////////////////////////////////////////
//  File:   MsrpMessageStatus.cs                                    26 Jul 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Msrp;

/// <summary>
/// Enumeration that defines the possible status conditions for a single MSRP message.
/// </summary>
public enum MsrpCompletionStatus
{
    /// <summary>
    /// The transaction message contains the complete message or is the last MSRP message in a
    /// collection of message chunks. The end line of the message ended with a "$" character.
    /// </summary>
    Complete,

    /// <summary>
    /// The transaction message contains only a chunk of the an entire MSRP message. The end line of
    /// the message ended with a "+" character.
    /// </summary>
    Continuation,

    /// <summary>
    /// The message contains contents that were truncated by the sender. The end line of the message
    /// ended with a "#" character
    /// </summary>
    Truncated,

    /// <summary>
    /// The status of the message unknown because the transaction message is not properly terminated.
    /// </summary>
    Unknown
}
