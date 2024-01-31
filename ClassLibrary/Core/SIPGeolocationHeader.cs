//////////////////////////////////////////////////////////////////////////////////////
//  File:   SIPGeolocationHeader.cs                                     7 Nov 22 PHR
//////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Core;

/// <bnf>
/// message-header      =/ Geolocation-header
///                         ; (message-header from RFC 3261)
/// Geolocation-header  = "Geolocation" HCOLON locationValue
///                         * (COMMA locationValue )
/// locationValue       = LAQUOT locationURI RAQUOT
///                         * (SEMI geoloc-param)
/// locationURI         = sip-URI / sips-URI / pres-URI
///                         / http-URI / https-URI
///                         / cid-url ; (from RFC 2392)
///                         / absoluteURI ; (from RFC 3261)
/// geoloc-param        = generic-param ; (from RFC 3261)
/// </bnf>

/// <summary>
/// Class for a SIP Geolocation header as defined in RFC 6442.
/// </summary>
public class SIPGeolocationHeader
{
    private SIPUserField m_Field = new SIPUserField();

    /// <summary>
    /// Gets or sets the SIPUserField object used to represent this Geolocation header.
    /// </summary>
    /// <value></value>
    public SIPUserField GeolocationField
    {
        get { return m_Field; }
        set { m_Field = value; }
    }

    /// <summary>
    /// Default constructor
    /// </summary>
    private SIPGeolocationHeader()
    { }

    /// <summary>
    /// Constructs a new SIPGeolocationHeader object from a SIPURI object.
    /// </summary>
    /// <param name="Uri">URI to use. May be a http, https, sip, sips or a cid type of URI.</param>
    public SIPGeolocationHeader(SIPURI Uri)
    {
        m_Field.URI = Uri;
    }

    /// <summary>
    /// Parses a Geolocation header value string and returns a list of SIPGeolocationHeader objects.
    /// Note: a Geolocation value string may contain multiple Geolocation headers with the headers
    /// separated by commas.
    /// </summary>
    /// <param name="strHeaderField">Geolocation header value string to parse.</param>
    /// <returns>Returns a list of SIPGeolocationHeader objects that contains one or more objects if
    /// succussful. Returns an empty list is an error occurred.</returns>
    /// <exception cref="SIPValidationException">Thrown if there was a validation error detected when
    /// parsing the user field.</exception>
    // <exception cref="Exception">Thrown if an unexpected error occurred.</exception>
    public static List<SIPGeolocationHeader> ParseGeolocationHeader(string strHeaderField)
    {
        List<SIPGeolocationHeader> Hdrs = new List<SIPGeolocationHeader>();
        string[] Headers = SIPParameters.GetKeyValuePairsFromQuoted(strHeaderField, ',');
        if (Headers == null || Headers.Length == 0)
            return Hdrs;

        foreach (string strHdr in Headers)
        {
            SIPGeolocationHeader Sgh = new SIPGeolocationHeader();
            try
            {
                Sgh.m_Field = SIPUserField.ParseSIPUserField(strHdr);
                Hdrs.Add(Sgh);
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
