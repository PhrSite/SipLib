/////////////////////////////////////////////////////////////////////////////////////
//  File:   OutgoingTestCallResults.cs                              18 Apr 25 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.TestCalls;

/// <summary>
/// Class for reporting the results of an NG9-1-1 test call that uses only one media type. The media type is typically audio.
/// </summary>
public class OutgoingTestCallResults
{
    /// <summary>
    /// If true then the test call was successful, else the call failed.
    /// </summary>
    public bool Success = false;

    /// <summary>
    /// Text describing the reason that the test call failed. Valid only if Success is false.
    /// </summary>
    public string FailureReason = string.Empty;

    /// <summary>
    /// Number of RTP packets sent by the outgoing test call (client) to the test call target. Valid only if Success is true.
    /// </summary>
    public int PacketsSent = 0;

    /// <summary>
    /// Number of RTP packets received from the test call target. Valid only if Success is true. For a successful test, this
    /// value may not exactly match the value of PacketsSent. For example, for a test call client that terminates the call after 3 packets,
    /// the value of PacketsReceived will be at least 3. The value of PacketsSent may be 4. The reasons for possible different
    /// values PacketsSent and PacketsReceived network latency and the asynchronous nature of Task execution.
    /// </summary>
    public int PacketsReceived = 0;

    /// <summary>
    /// Time that the test call started. This is the time at which the test call client received an OK response to the INVITE
    /// request for the test call. Valid only if Success is true.
    /// </summary>
    public DateTime CallStartTime = DateTime.Now;

    /// <summary>
    /// Time that the test call ended. This is the time at which the test call client received a BYE request from the test
    /// call target. Valid only if Success is true.
    /// </summary>
    public DateTime CallStopTime = DateTime.Now;
}
