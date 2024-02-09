/////////////////////////////////////////////////////////////////////////////////////
//  File:   MsrpPathHeader.cs                                       24 Jul 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

using System.Text;

namespace SipLib.Msrp;

/// <summary>
/// Class for a MSRP To-Path or a From-Path header value. See RFC 4975. Both the To-Path and the
/// From-Path MSRP message headers can contain multiple MSRP URIs.
/// </summary>
public class MsrpPathHeader
{
    /// <summary>
    /// Contains a list of MsrpUris. Initialized to an empty list.
    /// </summary>
    /// <value></value>
    public List<MsrpUri> MsrpUris = new List<MsrpUri>();

    /// <summary>
    /// Constructor
    /// </summary>
    public MsrpPathHeader()
    {
    }

    /// <summary>
    /// Parses a To-Path or a From-Path MSRP header value into a new MsrpPathHeader object.
    /// </summary>
    /// <param name="HeaderValue">Input header string value</param>
    /// <returns>Returns a new MsrpPathHeader if successful or null if an error is detected</returns>
    public static MsrpPathHeader? ParseMsrpPathHeader(string HeaderValue)
    {
        MsrpPathHeader pathHeader = new MsrpPathHeader();
        if (string.IsNullOrEmpty(HeaderValue))
            return null;

        string[] paths = HeaderValue.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (paths == null || paths.Length == 0)
            return null;

        foreach (string path in paths)
        {
            MsrpUri uri = MsrpUri.ParseMsrpUri(path);
            if (uri != null)
                pathHeader.MsrpUris.Add(uri);
            else
                return null;
        }

        return pathHeader;
    }

    /// <summary>
    /// Converts this MsrpPathHeader object into a header string value
    /// </summary>
    /// <returns>Returns the string value of the header</returns>
    public override string ToString()
    {
        StringBuilder Sb = new StringBuilder();
        for (int i = 0; i < MsrpUris.Count; i++)
        {
            Sb.Append(MsrpUris[i].ToString());
            if (i < MsrpUris.Count - 1)
                Sb.Append(" ");
        }

        return Sb.ToString();
    }
}
