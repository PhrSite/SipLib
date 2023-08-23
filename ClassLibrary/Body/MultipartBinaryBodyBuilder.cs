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
    public static byte[] ToByteArray(List<MessageContentsContainer> Contents, string BoundaryString)
    {
        if (Contents == null || Contents.Count == 0)
            throw new ArgumentException("Not body contents provided");

        if (string.IsNullOrEmpty(BoundaryString) == true)
            throw new ArgumentException("No BoundaryString provided");

        string BoundaryDelim = "--" + BoundaryString;
        string LastBoundaryDelim = BoundaryDelim + "--";
        MemoryStream ms = new MemoryStream();

        for (int i = 0; i < Contents.Count; i++)
        {
            if (i == 0)
                ms.Write(Encoding.UTF8.GetBytes(BoundaryDelim + CRLF));
            else
                ms.Write(Encoding.UTF8.GetBytes(CRLF + BoundaryDelim + CRLF));

            MessageContentsContainer Scc = Contents[i];
            WriteContentType(ms, Scc);

            if (string.IsNullOrEmpty(Scc.ContentDispositon) == false)
                ms.Write(Encoding.UTF8.GetBytes($"Content-Disposition: {Scc.ContentDispositon}{CRLF}"));

            if (string.IsNullOrEmpty(Scc.ContentID) == false)
                ms.Write(Encoding.UTF8.GetBytes($"Content-ID: {Scc.ContentID}{CRLF}"));

            if (string.IsNullOrEmpty(Scc.ContentTransferEncoding) == false)
                ms.Write(Encoding.UTF8.GetBytes($"Content-Transfer-Encoding: {Scc.ContentTransferEncoding}{CRLF}"));

            // Note: There is no need to write a Content-Length header for each contents block in a
            // multipart/mixed body.

            ms.Write(Encoding.UTF8.GetBytes(CRLF));

            if (Scc.IsBinaryContents == false)
                ms.Write(Encoding.UTF8.GetBytes(Scc.StringContents));
            else
                ms.Write(Scc.BinaryContents);
        }

        ms.Write(Encoding.UTF8.GetBytes(CRLF + LastBoundaryDelim + CRLF));
        return ms.ToArray();
    }

    private static void WriteContentType(MemoryStream ms, MessageContentsContainer Scc)
    {
        if (string.IsNullOrEmpty(Scc.ContentType) == true)
            return;

        if (Scc.ContentTypeParams.Count == 0)
        {
            ms.Write(Encoding.UTF8.GetBytes($"Content-Type: {Scc.ContentType}{CRLF}"));
            return;
        }

        StringBuilder sb = new StringBuilder();
        for (int i=0; i < Scc.ContentTypeParams.Count; i++)
        {
            string pname = Scc.ContentTypeParams.GetKey(i);
            string pval = Scc.ContentTypeParams.Get(i);
            if (string.IsNullOrEmpty(pval) == true)
                sb.Append($"{pname}");
            else
                sb.Append($"{pname}={pval}");

            if (i < Scc.ContentTypeParams.Count - 1)
                sb.Append("; ");
        }

        string ContentTypeLine = $"Content-Type: {Scc.ContentType}; {sb.ToString()}{CRLF}";
        ms.Write(Encoding.UTF8.GetBytes(ContentTypeLine));
    }
}
