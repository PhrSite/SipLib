/////////////////////////////////////////////////////////////////////////////////////
//  File:   RttUtils.cs                                             12 Oct 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

using System.Text;

namespace SipLib.RealTimeText;

/// <summary>
/// Provides various helper utility functions and definitions for Real Time Text (RTT, RFC 4103)
/// </summary>
public class RttUtils
{
    /// <summary>
    /// Byte Order Marker character
    /// </summary>
    public const char ByteOrderMarker = (char)0xfeff;

    /// <summary>
    /// Unicode line separator character encoded as an UTF-8 byte sequence
    /// </summary>
    public static byte[] Utf8LineSeparator = { 0xe2, 0x80, 0xa8 };

    /// <summary>
    /// String containing a Unicode line separator character.
    /// </summary>
    public static string strUtf8LineSeparator = Encoding.UTF8.GetString(Utf8LineSeparator);

    /// <summary>
    /// Replaces Windows/Linux line endings with the preferred Unicode new line separator per ITU-T 
    /// Recommendation T.140. The Windows line ending is CRLF. The Linux line ending is LF.
    /// </summary>
    /// <param name="InStr">Input string containing a string with Windows or Linux line endings</param>
    /// <returns>Returns a string with the T.140 line ending.</returns>
    public static string FixRttLineEnding(string InStr)
    {
        return InStr.Replace("\r\n", strUtf8LineSeparator).Replace("\n", strUtf8LineSeparator);
    }

    /// <summary>
    /// Default payload type for t140 text
    /// </summary>
    public const int DefaultT140PayloadType = 98;

    /// <summary>
    /// Default payload type for redundant text (red). A value of 0 means that redundancy is not being used.
    /// </summary>
    public const int DefaultRedundantPayloadType = 99;

    /// <summary>
    /// Default number of redundancy levels. A value of 0 indicates that redundancy is not being used.
    /// </summary>
    public const int DefaultRedundancyLevel = 3;

    /// <summary>
    /// Default number of characters per second. A value of 0 specifies that there is no limit to the number
    /// of characters per second that may be sent.
    /// </summary>
    public const int DefaultCps = 0;

    /// <summary>
    /// Defines the attribute for the RTT media block that indicates that a UA is RTT mixer aware as defined
    /// in RFC 9071.
    /// </summary>
    public const string MixerAttribute = "rtt-mixer";


}
