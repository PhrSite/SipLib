/////////////////////////////////////////////////////////////////////////////////////
//  File:   MsrpUri.cs                                              21 Jul 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Msrp;

using Org.BouncyCastle.Bcpg.OpenPgp;
using SipLib.Core;

//<bnf>
// MSRP-URI = msrp-scheme "://" authority
//      ["/" session-id] ";" transport *( ";" URI-parameter)
//      ; authority as defined in RFC3986
//  msrp-scheme = "msrp" / "msrps"
//  session-id = 1* (unreserved / "+" / "=" / "/" )
//      ; unreserved as defined in RFC3986
//  transport = "tcp" / 1 * ALPHANUM
//  URI-parameter = token["=" token]
//</bnf>

/// <summary>
/// Class for managing a MSRP URI. See Section 9 of RFC 4975.
/// </summary>
public class MsrpUri
{
    /// <summary>
    /// Gets or sets the authority URI portion of the MsrpUri
    /// </summary>
    public SIPURI uri { get; set; }

    /// <summary>
    /// Session ID portion of the MSRP URI. Required.
    /// </summary>
    public string SessionID { get; set; }

    /// <summary>
    /// Specifies the MSRP transport protocol. Required. Must be one of: tcp, tls, ws or wss.
    /// </summary>
    public string Transport { get; set; }

    /// <summary>
    /// Constructor
    /// </summary>
    public MsrpUri()
    {
    }

    /// <summary>
    /// Parses a string into a MsrpUri object
    /// </summary>
    /// <param name="uriString">Input string</param>
    /// <returns>Returns a new MsrpUri object if successful or null if the input string does not
    /// represent a valid MSRP URI</returns>
    public static MsrpUri ParseMsrpUri(string uriString)
    {
        MsrpUri msrpUri = new MsrpUri();

        int Idx1 = uriString.LastIndexOf('/');
        if (Idx1 < 0)
            return null;

        try
        {
            string strUri = uriString.Substring(0, Idx1);
            msrpUri.uri = SIPURI.ParseSIPURI(strUri);

            int Idx2 = uriString.LastIndexOf(";");
            if (Idx2 < 0)
                return null;

            msrpUri.SessionID = uriString.Substring(Idx1 + 1, Idx2 - Idx1 - 1);
            msrpUri.Transport = uriString.Substring(Idx2 + 1);
        }
        catch
        {
            return null;
        }

        return msrpUri;
    }

    /// <summary>
    /// Converts this MSRP URI to a string.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        string str = string.Format("{0}/{1};{2}", uri.ToString(), SessionID, Transport);
        return str;
    }
}
