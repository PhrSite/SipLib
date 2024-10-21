/////////////////////////////////////////////////////////////////////////////////////
//  File:   MediaTypes.cs                                           17 Oct 24 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Media;

/// <summary>
/// Static class that defines constant strings for each media type
/// </summary>
public static class MediaTypes
{
    /// <summary>
    /// Audio media
    /// </summary>
    public const string Audio = "audio";

    /// <summary>
    /// Video media
    /// </summary>
    public const string Video = "video";

    /// <summary>
    /// Real Time Text (RTT) media. See RFC 4103
    /// </summary>
    public const string RTT = "text";

    /// <summary>
    /// Message Session Relay Protocol. See RFC 4975.
    /// </summary>
    public const string MSRP = "message";
}
