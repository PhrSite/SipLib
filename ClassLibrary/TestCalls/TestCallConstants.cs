/////////////////////////////////////////////////////////////////////////////////////
//  File:   TestCallConstants.cs                                    25 Mar 25 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.TestCalls;

/// <summary>
/// Constants relating to NG9-1-1 test calls
/// </summary>
public class TestCallConstants
{
    /// <summary>
    /// URN value that identifies an NG9-1-1 test call. If an INVITE is for a test call the Request URI (RURI) will
    /// contain this string or the string for a subservice such as urn:service:test.sos.fire.
    /// </summary>
    public const string TestCallUrnValue = "service:test.sos";

    /// <summary>
    /// Name of the loopback media attribute
    /// </summary>
    public const string LoopbackAttributeName = "loopback";

    /// <summary>
    /// Name of the loopback source media attribute
    /// </summary>
    public const string LoopbackSourceAttributeName = "loopback-source";

    /// <summary>
    /// Name of the loopback mirror media attribute
    /// </summary>
    public const string LoopbackMirrorAttributeName = "loopback-mirror";

    /// <summary>
    /// Value of the loopback media attribute for RTP media loopback.
    /// </summary>
    public const string RtpMediaLoopbackAttributeValue = "rtp-media-loopback";

    /// <summary>
    /// Value of the loopback media attribute for RTP packet loopback.
    /// </summary>
    public const string RtpPacketLoopbackAttributeValue = "rtp-pck_loopback";

    /// <summary>
    /// Encoder name for encapsulated RTP packet loopback.
    /// </summary>
    public const string EncapsulatedRtpLoopback = "encaprtp";

    /// <summary>
    /// Encoder name for direct RTP packet loopback.
    /// </summary>
    public const string DirectRtpLoopback = "rtploopback";
}
