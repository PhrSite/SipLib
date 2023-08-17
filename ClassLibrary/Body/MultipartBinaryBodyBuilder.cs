/////////////////////////////////////////////////////////////////////////////////////
//  File:   MultipartBinaryBodyBuilder.cs                           16 Aug 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Body;

using System.Text;

/// <summary>
/// Class for building multipart/mixed message bodies that may contain binary contents as well
/// as text contents.
/// </summary>
public class MultipartBinaryBodyBuilder
{
    private const string CRLF = "\r\n";

    /// <summary>
    /// Converts a list of contents to a byte array. This method must be used for building multipart/mixed
    /// contents if one or more of the contents blocks contains binary contents. It may also be used for
    /// building multipart/mixed contents is all of the contents blocks are text.
    /// This method may be used for building multipart/mixed bodies for both SIP message and MSRP messages.
    /// </summary>
    /// <param name="Contents">Contains a list of body contents blocks.</param>
    /// <param name="BoundaryString">Specifies the string to use for the body delimeter. Must match the
    /// value of the boundary parameter in the Content-Type header value. For example, if the Content-Type
    /// header value is "multipart/mixed;boundary=boundary1", this parameter must be boundary1.</param>
    /// <returns></returns>
    public static byte[] ToByteArray(List<SipContentsContainer> Contents, string BoundaryString)
    {
        if (Contents == null || Contents.Count == 0)
            throw new ArgumentException("Not body contents provided");

        if (string.IsNullOrEmpty(BoundaryString) == true)
            throw new ArgumentException("No BoundaryString provided");

        string BoundaryDelim = "--" + BoundaryString;
        string LastBoundaryDelim = BoundaryString + "--";

        byte[] BodyBytes = null;
        MemoryStream ms = new MemoryStream();

        for (int i = 0; i < Contents.Count; i++)
        {
            if (i == 0)
                ms.Write(Encoding.UTF8.GetBytes(BoundaryString + CRLF));
            else if (i == Contents.Count - 1)
                ms.Write(Encoding.UTF8.GetBytes(CRLF + LastBoundaryDelim));
            else
                ms.Write(Encoding.UTF8.GetBytes(CRLF + BoundaryDelim + CRLF));

            SipContentsContainer Scc = Contents[i];

            if (string.IsNullOrEmpty(Scc.ContentType) == false)
                ms.Write(Encoding.UTF8.GetBytes($"Content-Type: {Scc.ContentType}{CRLF}"));

        }

        BodyBytes = ms.ToArray();
        return BodyBytes;
    }
}
