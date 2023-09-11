/////////////////////////////////////////////////////////////////////////////////////
//  File: BinaryBodyParser.cs                                       28 Nov 22 PHR
//
//  Revised:    17 Aug 23 PHR
//                -- Modified ProcessMultiPartContents to check for the presence of
//                   the boundary parameter of the Content-Type header instead of
//                   searching for the first occurrance of "=", because there might
//                   be more than one header parameter.
//                -- Made ProcessMultiPartContents() public
/////////////////////////////////////////////////////////////////////////////////////

using System.Collections.Specialized;
using System.Text;
using SipLib.Core;

namespace SipLib.Body;

/// <summary>
/// This class extracts both binary and text contents blocks from a SIP message.
/// The ProcessMultiPartContents() method may be used to handle MSRP multipart/mixed message bodies.
/// </summary>
/// <remarks>The reason that this class must be used when there is the possiblity that a SIP message might
/// contain binary data is that almost all SIP related functions treat the entire message as UTF8 encoded
/// strings. If arbitrary binary data is encoded to a UTF8 string then converted back to a raw binary array
/// then encoding errors will occur.</remarks>
public class BinaryBodyParser
{
    private const string CRLF = "\r\n";
    private const string ContentDelim = CRLF + CRLF;

    /// <summary>
    /// Parses a SIP message body. This method should be used when the SIP message body could contain data
    /// that must be treated as purely binary data as opposed to character data.
    /// </summary>
    /// <param name="MsgBytes">Contains the entire SIP message -- headers and the body</param>
    /// <param name="ContentType">Value of the Content-Type header of the SIP message</param>
    /// <returns>Returns a list of SipContentsContainer objects. The list will be empty if the SIP message
    /// does not contain a body or if an error occurred.</returns>
    public static List<MessageContentsContainer> ParseSipBody(byte[] MsgBytes, string ContentType)
    {
        List<MessageContentsContainer> RetVal = new List<MessageContentsContainer>();
        if (MsgBytes == null || MsgBytes.Length == 0 || string.IsNullOrEmpty(ContentType))
            return RetVal;

        int Idx = ContentType.ToLower().IndexOf("multipart");

        try
        {
            if (Idx >= 0)
                RetVal = ProcessMultiPartContents(MsgBytes, ContentType);
            else
                RetVal = ProcessSinglePartContents(MsgBytes, ContentType);
        }
        catch (NullReferenceException)
        { 
            throw; 
        }
        catch (IndexOutOfRangeException)
        { 
            throw;
        }
        catch (Exception)
        {
            throw;
        }

        return RetVal;
    }

    /// <summary>
    /// Parses a single SIP message body (i.e., the Content-Type is not multipart/mixed). The body may be
    /// binary or text.
    /// </summary>
    /// <param name="MsgBytes"></param>
    /// <param name="ContentType"></param>
    /// <returns></returns>
    private static List<MessageContentsContainer> ProcessSinglePartContents(byte[] MsgBytes, string ContentType)
    {
        List<MessageContentsContainer> RetVal = new List<MessageContentsContainer>();

        int DelimIdx = ByteBufferInfo.GetStringPosition(MsgBytes, 0, MsgBytes.Length, 
            ContentDelim, null);

        if (DelimIdx == -1)
            return RetVal;     // Error: No body delimiter string found

        int StartIdx = DelimIdx + ContentDelim.Length;
        if (StartIdx >= MsgBytes.Length)
            return RetVal;     // Error: The body is too short to contain anything

        int Len = MsgBytes.Length - StartIdx;
        byte[] BodyBytes = new byte[Len];
        Array.ConstrainedCopy(MsgBytes, StartIdx, BodyBytes, 0, Len);
        MessageContentsContainer Scc = new MessageContentsContainer();
        Scc.ContentType = ContentType;

        if (ContentsAreBinary(ContentType, null) == false)
        {
            Scc.ContentLength = BodyBytes.Length.ToString();
            Scc.IsBinaryContents = false;
            Scc.StringContents = Encoding.UTF8.GetString(BodyBytes);
        }
        else
        {
            Scc.IsBinaryContents = true;
            Scc.ContentLength = BodyBytes.Length.ToString();
            Scc.BinaryContents = BodyBytes;
        }
        
        RetVal.Add(Scc);

        return RetVal;
    }

    /// <summary>
    /// Processes a SIP message with multiple body parts (i.e., Content-Type = multipart/mixed).
    /// Some body parts may be binary and some may be text.
    /// This method will also work with multipart/mixed MSRP message.
    /// </summary>
    /// <param name="MsgBytes">Bytes of the entire message including headers and request line) and the body.
    /// Alternatively, pass all of the bytes of only the body of the message.
    /// </param>
    /// <param name="ContentType">Value of the Content-Type header of the overall message. For example:
    /// multipart/mixed; boundary=boundary1. The boundary parameter value may be quoted or not.</param>
    /// <returns>Returns a list of SipContentsContainer objects. The return value will not be null,
    /// but it may be empty is an error occurred.</returns>
    public static List<MessageContentsContainer> ProcessMultiPartContents(byte[] MsgBytes, string ContentType)
    {
        List<MessageContentsContainer> RetVal = new List<MessageContentsContainer>();

        NameValueCollection Parameters = GetHeaderParameters(ContentType);
        if (Parameters["boundary"] == null)
            throw new Exception("There is no boundary parameter for the Content-Type header");

        // Get the boundary string and remove any quotes if present.
        string Bndry = Parameters["boundary"].Replace("\"", "").Trim();

        //string BoundaryString = CRLF + "--" + Bndry + CRLF;
        // The first boundary string may not necessarily be preceeded by a CRLF in cases where only
        // the binary body byte array is passed to this function. GetBodyDelimIndices() adds a CRLF
        // string to the beginning of the BoundaryString after the first BoundaryString pattern is
        // detected.
        string BoundaryString = "--" + Bndry + CRLF;
        string LastBoundaryString = CRLF + "--" + Bndry + "--";

        // Get the indexes of all boundary strings
        List<int> Indexes = GetBodyDelimIndices(MsgBytes, 0, BoundaryString);
        int LastBoundaryIdx = ByteBufferInfo.GetStringPosition(MsgBytes, 0, MsgBytes.Length, 
            LastBoundaryString, null);

        if (LastBoundaryIdx != -1)
            Indexes.Add(LastBoundaryIdx);
        else
        {   // Error: The last boundary was not found.
            throw new Exception("The last boundary was not found");
        }

        int StartIdx, StopIdx, BodyStartIdx, HeaderLen, BodyLen;
        for (int i=0; i < Indexes.Count - 1; i++)
        {
            StartIdx = Indexes[i] + BoundaryString.Length;
            StopIdx = Indexes[i + 1];
            BodyStartIdx = ByteBufferInfo.GetStringPosition(MsgBytes, StartIdx, StopIdx, ContentDelim, null);

            if (BodyStartIdx == -1)
                throw new Exception("The contents delimiter is not present");
            else if ((BodyStartIdx + ContentDelim.Length) >= StopIdx)
                throw new Exception("The contents length is too short");

            BodyStartIdx += ContentDelim.Length;
            HeaderLen = BodyStartIdx - StartIdx;
            BodyLen = StopIdx - BodyStartIdx;
            byte[] HeaderBytes = new byte[HeaderLen];
            byte[] BodyBytes = new byte[BodyLen];
            Array.ConstrainedCopy(MsgBytes, StartIdx, HeaderBytes, 0, HeaderLen);
            Array.ConstrainedCopy(MsgBytes, BodyStartIdx, BodyBytes, 0, BodyLen);

            string strHeaders = Encoding.UTF8.GetString(HeaderBytes);
            string[] Headers = strHeaders.Split(CRLF, StringSplitOptions.RemoveEmptyEntries);
            if (Headers == null || Headers.Length == 0)
                throw new Exception("There are no headers in the body part");

            MessageContentsContainer Cc = new MessageContentsContainer();
            string strLower;
            foreach (string Header in Headers)
            {
                strLower = Header.ToLower();
                if (strLower.IndexOf("content-type") >= 0)
                {
                    Cc.ContentType = GetHeaderValue(Header);
                    ProcessContentTypeHeaderParameters(Cc);
                }
                else if (strLower.IndexOf("content-disposition") >= 0)
                    Cc.ContentDispositon = GetHeaderValue(Header);
                else if (strLower.IndexOf("content-id") >= 0)
                    Cc.ContentID = GetHeaderValue(Header);
                else if (strLower.IndexOf("content-length") >= 0)
                    Cc.ContentLength = GetHeaderValue(Header);
                else if (strLower.IndexOf("content-transfer-encoding") >= 0)
                    Cc.ContentTransferEncoding = GetHeaderValue(Header);
            }

            if (ContentsAreBinary(Cc.ContentType, Cc.ContentTransferEncoding) == true)
            {
                Cc.IsBinaryContents = true;
                Cc.BinaryContents = BodyBytes;
            }
            else
            {
                Cc.IsBinaryContents = false;
                Cc.StringContents = Encoding.UTF8.GetString(BodyBytes);
            }

            RetVal.Add(Cc);
        } // end for

        return RetVal;
    }

    private static void ProcessContentTypeHeaderParameters(MessageContentsContainer Cc)
    {
        string[] Fields = Cc.ContentType.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        if (Fields != null && Fields.Length > 0)
        {
            Cc.ContentType = Fields[0];
            for (int i = 1; i < Fields.Length; i++)
            {   // Parameters may be of the form: name=value or simply a parameter name with no value.
                string[] Nv = Fields[i].Split(new char[] { '=' },
                    StringSplitOptions.RemoveEmptyEntries);
                if (Nv != null && Nv.Length > 0)
                {
                    string Val = Nv.Length == 2 ? Nv[1].Trim() : null;
                    Cc.ContentTypeParams.Add(Nv[0].Trim(), Val);
                }
            }
        }
    }

    private static NameValueCollection GetHeaderParameters(string HeaderValue)
    {
        NameValueCollection Parameters = new NameValueCollection();
        string[] Fields = HeaderValue.Split(";", StringSplitOptions.RemoveEmptyEntries |
            StringSplitOptions.TrimEntries);
        if (Fields != null && Fields.Length > 0)
        {
            // Note, the Content-Type header value is at index 0, but its not needed here.

            for (int i = 1; i < Fields.Length; i++)
            {   // Parameters may be of the form: name=value or simply a parameter name with no value.
                string[] Nv = Fields[i].Split("=", StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries);
                if (Nv != null && Nv.Length > 0)
                {
                    string Val = Nv.Length == 2 ? Nv[1] : null;
                    Parameters.Add(Nv[0], Val);
                }
            }
        }

        return Parameters;
    }

    /// <summary>
    /// Determines if the contents of a SIP message body are binary or text. The known binary types
    /// are defined in the KnownBinaryTypes array. This array contains only a small subset of the IANA
    /// registered MIME types defined at: https://www.iana.org/assignments/media-types/media-types.xhtml
    /// </summary>
    /// <param name="ContentType">Value of the Content-Type header</param>
    /// <param name="ContentTransferEncoding">Value of the Content-Transfer-Encoding header.
    /// May be null if not present.</param>
    /// <returns>Returns true if the Content-Type is a known binary type or false if it is not.</returns>
    public static bool ContentsAreBinary(string ContentType, string ContentTransferEncoding)
    {
        bool RetVal = false;
        if (string.IsNullOrEmpty(ContentTransferEncoding) == false)
        {
            if (ContentTransferEncoding.ToLower().IndexOf("binary") >= 0)
                RetVal = true;
        }
        else if (string.IsNullOrEmpty(ContentType) == false)
        {
            string strLower = ContentType.ToLower();
            foreach (string str in KnownBinaryTypes)
            {
                if (strLower.Contains(str) == true)
                {
                    RetVal = true;
                    break;
                }
            }
        }

        return RetVal;
    }

    private static string[] KnownBinaryTypes = 
    {
        // The application types might be encountered in the body of SIP messages
        "application/octet-stream",     // Not expected in a SIP message, but maybe in a MSRP message
        "application/isup",

        // Some common image types -- These types might be encountered in the body of a MSRP message or in a
        // CPIM message embedded in  a MSRP message
        // See: https://www.iana.org/assignments/media-types/media-types.xhtml#image
        "image/bmp",
        "image/jpeg",
        "image/jpg",
        "image/gif",
        "image/tiff",

        // Some common video types -- These types might be encountered in the body of a MSRP message or in a
        // CPIM message embedded in  a MSRP message
        // See: https://www.iana.org/assignments/media-types/media-types.xhtml#video
        "video/h264",
        "video/h265",
        "video/h266",
        "video/jpeg",
        "video/mp4",
        "video/mpeg",
        "video/mpeg-generic",
        "video/ogg",
        "video/quicktime",
        "video/raw",
        "video/vp8",
        "video/vp9",

    };

    private static List<int> GetBodyDelimIndices(byte[] MsgBody, int StartPos, string BoundaryString)
    {
        List<int> IdxList = new List<int>();
        bool Done = false;
        int CurStart = StartPos;
        int CurIdx;
        while (Done == false)
        {
            CurIdx = ByteBufferInfo.GetStringPosition(MsgBody, CurStart, MsgBody.Length,
                BoundaryString, null);
            if (CurIdx == -1)
                Done = true;
            else
            {
                CurStart = CurIdx + BoundaryString.Length;
                if (BoundaryString.StartsWith(CRLF) == false)
                    // The first boundary string might not have a preceeding CRLF string so add it in
                    // after the first one is detected.
                    BoundaryString = CRLF + BoundaryString;

                IdxList.Add(CurIdx);
            }
        }

        return IdxList;
    }

    private static string GetHeaderValue(string HdrLine)
    {
        string strValue = null;
        int Idx = HdrLine.IndexOf(":");
        if (Idx >= 0)
            strValue = HdrLine.Substring(Idx + 1).Trim();

        return strValue;
    }

}
