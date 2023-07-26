/////////////////////////////////////////////////////////////////////////////////////
//  File:   MsrpStatusHeader.cs                                     24 Jul 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Msrp;

/// <summary>
/// Class for the Status header of a MSRP message. See RFC 4975
/// </summary>
public class MsrpStatusHeader
{
    /// <summary>
    /// Contains the namespace field of the Status header. This should always be "000".
    /// </summary>
    public string Namespace = "000";
    /// <summary>
    /// Contains the status-code field of the Status header. A value of 0 indicates an invalid status.
    /// </summary>
    public int StatusCode = 0;
    /// <summary>
    /// Contains the comment field of the Status header. The comment field is optional. A value of null
    /// indicates that the comment field is not present.
    /// </summary>
    public string Comment = null;

    /// <summary>
    /// Constructor
    /// </summary>
    public MsrpStatusHeader()
    {
    }

    /// <summary>
    /// Parses a Status header value into a new MsrpStatusHeader object
    /// </summary>
    /// <param name="strValue">Input Status header value</param>
    /// <returns>Returns a new MsrpStatusHeader object if successful or null if an error is detected
    /// </returns>
    public static MsrpStatusHeader ParseStatusHeader(string strValue)
    {
        MsrpStatusHeader status = new MsrpStatusHeader();
        string[] Fields = strValue.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (Fields.Length < 2)
            return null;     // Error: the header value is not properly formatted.

        status.Namespace = Fields[0];
        if (int.TryParse(Fields[1], out status.StatusCode) == false)
            return null;    // Error: the StatusCode must be an integer

        // Allow for multi-work Comment fields
        if (Fields.Length >= 3)
        {
            status.Comment = string.Empty;
            for (int i = 2; i < Fields.Length; i++)
            {
                status.Comment += Fields[i];
                if (i < Fields.Length - 1)
                    status.Comment += " ";
            }
        }

        return status;
    }

    /// <summary>
    /// Converts this object into a Status header value string
    /// </summary>
    /// <returns>Returns the string value of a Status header</returns>
    public override string ToString()
    {
        string strStatus = null;
        if (Comment != null)
            strStatus = string.Format("{0} {1} {2}", Namespace, StatusCode.ToString(), Comment);
        else
            strStatus = string.Format("{0} {1}", Namespace, StatusCode);

        return strStatus;
    }
}
