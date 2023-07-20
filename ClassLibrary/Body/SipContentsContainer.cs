//////////////////////////////////////////////////////////////////////////////////////
//  File:   SipContentsContainer.cs                                 23 Nov 22 PHR
//////////////////////////////////////////////////////////////////////////////////////

using System.Collections.Specialized;
using System.Text;

namespace SipLib.Body;

/// <summary>
/// Class for holding the Contents-Type and the contents lines for a single contents block of a 
/// SIP message.
/// </summary>
public class SipContentsContainer
{
    /// <summary>
    /// Contains the value of the Content-Type header that indicates the type of the contents received 
    /// with a SIP message.
    /// </summary>
    public string ContentsType = "";
    /// <summary>
    /// Contains the Content-Disposition header value. Will be null if there is no Content-Disposition 
    /// header.
    /// </summary>
    public string ContentsDispositon = null;
    /// <summary>
    /// Contains the Content-ID header value. Will be null if there is none.
    /// </summary>
    public string ContentID = null;

    /// <summary>
    /// Contains the Content-Transfer-Encoding header value. Will be null if there is none.
    /// </summary>
    public string ContentTransferEncoding = null;

    /// <summary>
    /// Contains the string value of the Contents-Length header. Will be null if there is none.
    /// </summary>
    public string ContentsLength = null;

    /// <summary>
    /// Contains the contents lines.
    /// </summary>
    public List<string> ContentsLines = new List<string>();

    /// <summary>
    /// Contains a collection of parameters from the Content-Type header.
    /// </summary>
    public NameValueCollection Params = new NameValueCollection();

    /// <summary>
    /// If true, then the contents contains raw binary data that must not
    /// be converted to a string in order to a string.
    /// </summary>
    public bool IsBinaryContents = false;
    /// <summary>
    /// Contains the raw binary data. Will be non-null if IsBinaryContents is true.
    /// </summary>
    public byte[] BinaryContents = null;

    /// <summary>
    /// Converts the lines of the contents into a string.
    /// </summary>
    /// <returns>Returns a single string containing the lines of the contents block. Each line is 
    /// separated by a \r\n (CRLF). Returns an empty string if there are no contents.</returns>
    public override string ToString()
    {
        StringBuilder Sb = new StringBuilder(2048);
        foreach (string str in ContentsLines)
        {
            Sb.Append(str + "\r\n");
        }

        return Sb.ToString();
    }
}
