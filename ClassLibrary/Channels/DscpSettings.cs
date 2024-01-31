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
    /// <value></value>
    public static uint AudioDscp = 0x0b;
    /// <summary>
    /// DSCP value for Real Time Text (RTT) media.
    /// </summary>
    /// <value></value>
    public static uint RTTDscp = 0x07;
    /// <summary>
    /// DSCP value for MSRP media.
    /// </summary>
    /// <value></value>
    public static uint MSRPDscp = 0x07;
    /// <summary>
    /// DSCP value for for video media.
    /// </summary>
    /// <value></value>
    public static uint VideoDscp = 0x0f;
    /// <summary>
    /// DSCP value for SIP signaling.
    /// </summary>
    /// <value></value>
    public static uint SipSignalingDscp = 0x03;

    /// <summary>
    /// Gets the DSCP setting for a media type.
    /// </summary>
    /// <param name="mediaType">Input media type. Must be one of: audio, text (for RTT), message (for MSRP),
    /// or video</param>
    /// <returns>Returns the configured DSCP value to use</returns>
    public static uint GetDscpForMediaType(string mediaType)
    {
        uint DscpSetting = AudioDscp;
        switch (mediaType)
        {
            case "audio":
                DscpSetting = AudioDscp;
                break;
            case "text":
                DscpSetting = RTTDscp;
                break;
            case "message":
                DscpSetting = MSRPDscp;
                break;
            case "video":
                DscpSetting = VideoDscp;
                break;
            default:
                DscpSetting = AudioDscp;
                break;
        }

        return DscpSetting;
    }
}
