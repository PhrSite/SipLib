/////////////////////////////////////////////////////////////////////////////////////
//  File: BinaryBodyParser.cs                                       28 Nov 22 PHR
/////////////////////////////////////////////////////////////////////////////////////

using System.Text;
using SipLib.Core;

namespace SipLib.Body;

/// <summary>
/// This class extracts both binary and text contents blocks from a SIP message.
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
    public static List<SipContentsContainer> ParseSipBody(byte[] MsgBytes, string ContentType)
    {
        List<SipContentsContainer> RetVal = new List<SipContentsContainer>();
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
    private static List<SipContentsContainer> ProcessSinglePartContents(byte[] MsgBytes, 
        string ContentType)
    {
        List<SipContentsContainer> RetVal = new List<SipContentsContainer>();

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
        SipContentsContainer Scc = new SipContentsContainer();
        Scc.ContentsType = ContentType;

        if (ContentsAreBinary(ContentType, null) == false)
        {
            Scc.ContentsLength = BodyBytes.Length.ToString();
            Scc.IsBinaryContents = false;
            string str = Encoding.UTF8.GetString(BodyBytes);
            string[] strings = str.Split(CRLF, StringSplitOptions.RemoveEmptyEntries);

            char[] Whitespace = { '\r', '\n', ' ' };
            string strTrim;

            foreach (string s in strings)
            {
                strTrim = s.Trim(Whitespace);
                if (string.IsNullOrEmpty(strTrim) == false && strTrim.StartsWith("--") == false)
                    Scc.ContentsLines.Add(strTrim);
            }
        }
        else
        {
            Scc.IsBinaryContents = true;
            Scc.ContentsLength = BodyBytes.Length.ToString();
            Scc.BinaryContents = BodyBytes;
        }
        
        RetVal.Add(Scc);

        return RetVal;
    }

    /// <summary>
    /// Processes a SIP message with multiple body parts (i.e., Content-Type = multipart/mixed).
    /// Some body parts may be binary and some may be text.
    /// </summary>
    /// <param name="MsgBytes"></param>
    /// <param name="ContentType"></param>
    /// <returns></returns>
    public static List<SipContentsContainer> ProcessMultiPartContents(byte[] MsgBytes, 
        string ContentType)
    {
        List<SipContentsContainer> RetVal = new List<SipContentsContainer>();

        // Get the boundary string
        int Idx = ContentType.IndexOf("=");
        if (Idx < 0)
            // Error: There is no contents boundary string specified.
            throw new Exception("There is no boundary parameter for the Content-Type header");

        // Get the boundary string and remove any quotes if present.
        string Bndry = ContentType.Substring(Idx + 1).Replace("\"", "").Trim();
        string BoundaryString = CRLF + "--" + Bndry + CRLF;
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
            BodyStartIdx = ByteBufferInfo.GetStringPosition(MsgBytes, StartIdx, StopIdx, ContentDelim,
                null);

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

            SipContentsContainer Cc = new SipContentsContainer();
            string strLower;
            foreach (string Header in Headers)
            {
                strLower = Header.ToLower();
                if (strLower.IndexOf("content-type") >= 0)
                {
                    Cc.ContentsType = GetHeaderValue(Header);
                    ProcessContentTypeHeaderParameters(Cc);
                }
                else if (strLower.IndexOf("content-disposition") >= 0)
                    Cc.ContentsDispositon = GetHeaderValue(Header);
                else if (strLower.IndexOf("content-id") >= 0)
                    Cc.ContentID = GetHeaderValue(Header);
                else if (strLower.IndexOf("content-length") >= 0)
                    Cc.ContentsLength = GetHeaderValue(Header);
                else if (strLower.IndexOf("content-transfer-encoding") >= 0)
                    Cc.ContentTransferEncoding = GetHeaderValue(Header);
            }

            if (ContentsAreBinary(Cc.ContentsType, Cc.ContentTransferEncoding) == true)
            {
                Cc.IsBinaryContents = true;
                Cc.BinaryContents = BodyBytes;
            }
            else
            {
                Cc.IsBinaryContents = false;
                string[] Lines = Encoding.UTF8.GetString(BodyBytes).Split(CRLF,
                    StringSplitOptions.RemoveEmptyEntries);
                Cc.ContentsLines = new List<string>(Lines);
            }

            RetVal.Add(Cc);
        } // end for

        return RetVal;
    }

    private static void ProcessContentTypeHeaderParameters(SipContentsContainer Cc)
    {
        string[] Fields = Cc.ContentsType.Split(new char[] { ';' },
            StringSplitOptions.RemoveEmptyEntries);
        if (Fields != null && Fields.Length > 0)
        {
            Cc.ContentsType = Fields[0];
            for (int i = 1; i < Fields.Length; i++)
            {   // Parameters may be of the form: name=value or simply a
                // parameter name with no value.
                string[] Nv = Fields[i].Split(new char[] { '=' },
                    StringSplitOptions.RemoveEmptyEntries);
                if (Nv != null && Nv.Length > 0)
                {
                    string Val = Nv.Length == 2 ? Nv[1].Trim() : null;
                    Cc.Params.Add(Nv[0].Trim(), Val);
                }
            }
        }
    }

    /// <summary>
    /// Determines if the contents of a SIP message body are binary or text.
    /// </summary>
    /// <param name="ContentType">Value of the Content-Type header</param>
    /// <param name="ContentTransferEncoding">Value of the Content-Transfer-Encoding header.
    /// May be null if not present.</param>
    /// <returns></returns>
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
        "application/octet-stream",
        "application/isup",
        "application/jpeg",
        "application/jpg"

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
                IdxList.Add(CurIdx);
                CurStart = CurIdx + BoundaryString.Length;
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
