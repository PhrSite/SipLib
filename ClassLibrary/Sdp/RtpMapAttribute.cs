/////////////////////////////////////////////////////////////////////////////////////
//  File:   RtpMapAttribute.cs                                      28 Dec 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Sdp;

/// <summary>
/// Class for represent an SDP rtpmap attribute. See Section 6.6 of RFC 8866.
/// </summary>
public class RtpMapAttribute
{
    /// <summary>
    /// Represents the payload type number. For instance, 0 = PCMU by default.
    /// </summary>
    /// <value></value>
    public int PayloadType;

    /// <summary>
    /// Specifies the encoding-name parameter. For example: PCMU
    /// </summary>
    /// <value></value>
    public string? EncodingName;

    /// <summary>
    /// Specifies the clock rate or sample rate.
    /// </summary>
    /// <value></value>
    public int ClockRate;

    /// <summary>
    /// Specifies the number of channels. If not specified then the number of channels is 1.
    /// A value of 0 indicates that the number of channels is not set. This is the default case.
    /// </summary>
    /// <value></value>
    public int Channels = 0;

    /// <summary>
    /// Constructor
    /// </summary>
    public RtpMapAttribute()
    {
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="payloadType">Payload type.</param>
    /// <param name="encodingName">Encoding name. For example "PCMU"</param>
    /// <param name="clockRate">Clock rate in samples/second.</param>
    public RtpMapAttribute(int payloadType, string encodingName, int clockRate)
    {
        PayloadType = payloadType;
        EncodingName = encodingName;
        ClockRate = clockRate;
    }

    /// <summary>
    /// Parses an attribute value string and returns a new RtpMapAttribute object.
    /// </summary>
    /// <param name="attrValue">Attribute value string. For example, if the SDP media description has an
    /// attribute line line "a=rtpmap 0 PCMU/8000", then the attribute value string would be "0 PCMU/8000".</param>
    /// <returns>Returns a new RtpMapAttribute object if successful or null if a formatting error is detected.</returns>
    public static RtpMapAttribute? ParseRtpMap(string attrValue)
    {
        RtpMapAttribute rtpMap = new RtpMapAttribute();
        if (string.IsNullOrEmpty(attrValue) == true)
            return null;

        int index = attrValue.IndexOf(' ');
        if (index < 1 || (index + 1) >= attrValue.Length)
            return null;

        if (int.TryParse(attrValue.Substring(0, index), out rtpMap.PayloadType) == false)
            return null;

        string? str = attrValue.Substring(index + 1);
        string[] strparams = str.Split('/');
        if (strparams.Length < 2)
            return null;

        rtpMap.EncodingName = strparams[0];
        if (int.TryParse(strparams[1], out rtpMap.ClockRate) == false)
            return null;

        if (strparams.Length == 3)
        {   // Get the number of channels
            if (int.TryParse(strparams[2], out rtpMap.Channels) == false)
                return null;
        }

        return rtpMap;
    }

    /// <summary>
    /// Converts this object into a full SDP attribute line. For example: "a=rtpmap: 0 PCMU/8000".
    /// </summary>
    /// <returns>Returns a full SDP attribute line.</returns>
    public override string ToString()
    {
        if (Channels == 0)
            return $"a=rtpmap:{PayloadType} {EncodingName}/{ClockRate}\r\n";
        else
            return $"a=rtpmap:{PayloadType} {EncodingName}/{ClockRate}/{Channels}\r\n";
    }
}
