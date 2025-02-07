/////////////////////////////////////////////////////////////////////////////////////
//  File:   SIPFrag.cs                                              4 Feb 25 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Core;

/// <summary>
/// Class for parsing or creating a SIPFRAG (SIP Fragment) body use in a NOTIFY request for a Refer subscription.
/// See Section 2.4.5 of RFC 3515.
/// </summary>
public class SIPFrag
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="status">Status enumeration value</param>
    /// <param name="reasonPhrase">Reason phrase string.</param>
    public SIPFrag(SIPResponseStatusCodesEnum status, string reasonPhrase)
    {
        Status = status;
        ReasonPhrase = reasonPhrase;
    }

    /// <summary>
    /// Gets or sets the Status field.
    /// </summary>
    public SIPResponseStatusCodesEnum Status { get; set; } = SIPResponseStatusCodesEnum.None;

    /// <summary>
    /// Gets or sets the reason phrase
    /// </summary>
    public string ReasonPhrase { get; set; } = string.Empty;

    /// <summary>
    /// Parses a SIP fragment string and creates a new SIPFrag object.
    /// </summary>
    /// <param name="sipfragString">Input SIP fragment. The format of this string must be the same as the first line of a
    /// SIP response message. For example: SIP/2.0 200 Ok</param>
    /// <returns>Returns a new SIPFrag object if no errors occured. Returns a new SIPFrag object with a Status property
    /// equal to SIPResponseStatusCodesEnum.None if the input string is not valid.</returns>
    public static SIPFrag ParseSIPFrag(string sipfragString)
    {
        SIPFrag frag = new SIPFrag(SIPResponseStatusCodesEnum.None, "Unknown");
        if (string.IsNullOrEmpty(sipfragString) == true)
            return frag;

        sipfragString = sipfragString.Replace("\r\n", "").Trim();

        try
        {
            int firstSpacePosn = sipfragString.IndexOf(" ");
            string version = sipfragString.Substring(0, firstSpacePosn).Trim();
            sipfragString = sipfragString.Substring(firstSpacePosn).Trim();
            int StatusCode = Convert.ToInt32(sipfragString.Substring(0, 3));
            frag.Status = SIPResponseStatusCodes.GetStatusTypeForCode(StatusCode);
            frag.ReasonPhrase = sipfragString.Substring(3).Trim();
        }
        catch (Exception)
        { 
            frag.Status= SIPResponseStatusCodesEnum.None;
            frag.ReasonPhrase = "Unknown";
        }

        return frag;
    }

    /// <summary>
    /// Converts this object into a SIPFRAG string for inclusion in the body of a NOTIFY request for the "refer" subscribe/notify
    /// event package.
    /// </summary>
    /// <returns>Returns a string value to put in the body of a NOTIFY body.</returns>
    /// <exception cref="ArgumentException">Thrown if the Status property or the ReasonPhrase property is not valid.</exception>
    public override string ToString()
    {
        if (Status == SIPResponseStatusCodesEnum.None)
            throw new ArgumentException("The Status property is not valid");

        if (string.IsNullOrEmpty(ReasonPhrase) == true)
            throw new ArgumentException("The ReasonPhrase is null or empty");

        int statusCode = (int)Status;
        return $"SIP/2.0 {statusCode} {ReasonPhrase}";
    }
}
