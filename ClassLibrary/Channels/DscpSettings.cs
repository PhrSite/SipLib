/////////////////////////////////////////////////////////////////////////////////////
//  File:   DscpSettings.cs                                         20 Jul 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Channels;

/// <summary>
/// Static class that defines the default Differentiated Services Code Point (DSCP) values to use for
/// different media types. See RFC 2475 and RFC 3260 for DSCP. The default values are those specified in
/// Section 2.7 of NENA STA-010.3
/// </summary>
/// <remarks>The DSCP is a 6-bit field that is located in the upper 6 bits of the Type of Service (TOS)
/// field of the IPv4 header or the upper 6 bits Traffic Class byte of the IPv6 header.</remarks>
public static class DscpSettings
{
    /// <summary>
    /// DSCP value for audio media.
    /// </summary>
    public static uint AudioDscp = 0x0b;
    /// <summary>
    /// DSCP value for Real Time Text (RTT) media.
    /// </summary>
    public static uint RTTDscp = 0x07;
    /// <summary>
    /// DSCP value for MSRP media.
    /// </summary>
    public static uint MSRPDscp = 0x07;
    /// <summary>
    /// DSCP value for for video media.
    /// </summary>
    public static uint VideoDscp = 0x0f;
    /// <summary>
    /// DSCP value for SIP signaling.
    /// </summary>
    public static uint SipSignalingDscp = 0x03;
}
