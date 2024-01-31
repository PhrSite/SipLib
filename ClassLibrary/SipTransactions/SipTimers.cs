/////////////////////////////////////////////////////////////////////////////////////
//  File:   SipTimers.cs                                            1 Sep 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Transactions;

/// <summary>
/// Defines the default timer intervals for SIP transactions defined in RFC 3261.
/// </summary>
public static class SipTimers
{
    /// <summary>
    /// Value of the SIP defined timer T1 in milliseconds and is the time for the first retransmit.
    /// Should not need to be adjusted in normal circumstances.
    /// </summary>
    /// <value></value>
    public static int T1 = 500;

    /// <summary>
    /// Value of the SIP defined timer T2 in milliseconds and is the maximum time between retransmits.
    /// Should not need to be adjusted in normal circumstances.
    /// </summary>
    /// <value></value>
    public static int T2 = 4000;

    /// <summary>
    /// The SIP T4 timer in milliseconds represents the amount of time the network will take to clear
    /// messages between client and server transactions.
    /// </summary>
    /// <value></value>
    public static int T4 = 5000;

    /// <summary>
    /// Value of the SIP defined timer T6 in milliseconds and is the period after which a transaction 
    /// has timed out. Should not need to be adjusted in normal circumstances.
    /// </summary>
    /// <value></value>
    public static int T6 = 64 * T1;

    /// <summary>
    /// Interval in milliseconds that a client INVITE transaction can remain in the Calling state. 
    /// See Section 17.1.1.2 and Figure 5 of RFC 3261.
    /// </summary>
    /// <value></value>
    public static int TimerB = 64 * T1;

    /// <summary>
    /// Interval in milliseconds that a client INVITE transaction can remain in the Completed state.
    /// See Section 17.1.1.2 and Figure 5 of RFC 3261.
    /// </summary>
    /// <value></value>
    public static int TimerD = 32000;

    /// <summary>
    /// Used for server INVITE transactions in the Completed state when the transport protocol is UDP.
    /// Defines the interval in milliseconds at which the last sent response is retransmitted.
    /// </summary>
    /// <value></value>
    public static int TimerG = Math.Min(2 * T1, T2);

    /// <summary>
    /// Used for server INVITE transactions in the Completed state. Specifies the interval in milliseconds
    /// that the server stops trying to retransmit the last sent final response.
    /// </summary>
    /// <value></value>
    public static int TimerH = 64 * T1;

    /// <summary>
    /// Used for server INVITE transactions in the Confirmed state when the transport protocol is UDP.
    /// Specifies the interval in milliseconds that the server transaction remains in the Confirmed state.
    /// </summary>
    /// <value></value>
    public static int TimerI = T4;

    /// <summary>
    /// Used for server non-INVITE transactions in the Completed state when the transport protocol is UDP.
    /// The units are milliseconds.
    /// </summary>
    /// <value></value>
    public static int TimerJ = 64 * T1;
}
