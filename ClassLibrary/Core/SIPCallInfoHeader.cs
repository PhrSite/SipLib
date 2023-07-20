//////////////////////////////////////////////////////////////////////////////////////
//	File:	SIPCallInfoHeader.cs										7 Nov 22 PHR
//////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Core;

/// <bnf>
/// Call-Info = "Call-Info" HCOLON info *(COMMA info)
/// info = LAQUOT absoluteURI RAQUOT* (SEMI info-param)
/// info-param = ( "purpose" EQUAL( "icon" / "info"
///             / "card" / token ) ) / generic-param
/// </bnf>

/// <summary>
/// Class for a SIP Call-Info header. See Sections 20.9 and 25.1 of RFC 3261.
/// </summary>
public class SIPCallInfoHeader
{
    private SIPUserField m_Field = new SIPUserField();

    /// <summary>
    /// Gets or sets the SIPUserField object used to represent this Call-Info
    /// header.
    /// </summary>
    public SIPUserField CallInfoField
    {
        get { return m_Field; }
        set { m_Field = value; }
    }

    /// <summary>
    /// Defines the purpose header parameter for Call-Info.
    /// </summary>
    public const string PURPOSE_PARAMETER_STRING = "purpose";

    private SIPCallInfoHeader()
    { }

    /// <summary>
    /// Constructs a new Call-Info header object.
    /// </summary>
    /// <param name="Uri">Value field of the Call-Info header.</param>
    /// <param name="PurposeStr">Purpose string of the header.</param>
    public SIPCallInfoHeader(SIPURI Uri, string PurposeStr)
    {
        m_Field.URI = Uri;
        if (string.IsNullOrEmpty(PurposeStr) == false)
            m_Field.Parameters.Set(PURPOSE_PARAMETER_STRING, PurposeStr);
    }

    /// <summary>
    /// Parses a Call-Info header value string and returns a list of SIPCallInfoHeader objects. Note: 
    /// a Call-Info value string may contain multiple Call-Info headers with the headers separated by
    /// commas.
    /// </summary>
    /// <param name="HeaderStr">Call-Info header value string to parse.</param>
    /// <returns>Returns a list of SIPCallInfoHeader objects that contains one or more objects if
    /// succussful. Returns an empty list is an error occurred.
    /// </returns>
    /// <exception cref="SIPValidationException">Thrown if a validation error
    /// occured while parsing the SIP user field portion of the header.</exception>
    // <exception cref="Exception">Thrown if a unexpected error occurred.</exception>
    public static List<SIPCallInfoHeader> ParseCallInfoHeader(string HeaderStr)
    {
        List<SIPCallInfoHeader> Hdrs = new List<SIPCallInfoHeader>();
        string[] Headers = SIPParameters.GetKeyValuePairsFromQuoted(HeaderStr, ',');
        if (Headers == null || Headers.Length == 0)
            return Hdrs;

        foreach (string strHdr in Headers)
        {
            SIPCallInfoHeader Sci = new SIPCallInfoHeader();
            try
            {
                Sci.m_Field = SIPUserField.ParseSIPUserField(strHdr);
                Hdrs.Add(Sci);
            }
            catch (SIPValidationException)
            {
                throw;
            }
            catch (Exception)
            {
                throw;
            }
        }

        return Hdrs;
    }

    /// <summary>
    /// Converts this object into a string for use as a header value.
    /// </summary>
    /// <returns>Returns the string value of this object.</returns>
    public override string ToString()
    {
        return m_Field.ToString();
    }
}
