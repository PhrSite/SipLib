/////////////////////////////////////////////////////////////////////////////////////
//  File:   MsrpMessage.cs                                          24 Jul 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

using Org.BouncyCastle.Asn1.Cmp;
using SipLib.Core;
using System.Reflection.PortableExecutable;
using System.Text;

namespace SipLib.Msrp;

/// <summary>
/// Class for MSRP messages. See RFC 4975.
/// </summary>
public class MsrpMessage
{
    /// <summary>
    /// Specifies the type of MSRP message
    /// </summary>
    public MsrpMessageType MessageType { get; set; } = MsrpMessageType.Request;

    /// <summary>
    /// Transaction ID for the message;
    /// </summary>
    public string TransactionID = NewTransactionID();

    /// <summary>
    /// Request line for the message. This will be null if the MessageType is not MessageType.Request
    /// </summary>
    public string RequestLine { get; set; } = null;

    /// <summary>
    /// Response code for the message. Valid if the MessageType is MessageType.Response
    /// </summary>
    public int ResponseCode { get; set; } = 0;

    /// <summary>
    /// Text or comment describing the ResponseCode. Optional.
    /// </summary>
    public string ResponseText { get; set; } = null;

    /// <summary>
    /// Message-ID header value.
    /// </summary>
    public string MessageID { get; set; } = null;
    /// <summary>
    /// Content-Type header value. A value of null indicates that there was no Content-Type header in the
    /// message.
    /// </summary>
    public string ContentType { get; set; } = null;

    /// <summary>
    /// Success-Report header value. A value of null indicates that a Success-Report header was not
    /// provided.
    /// </summary>
    public string SuccessReport { get; set; } = null;

    /// <summary>
    /// Failure-Report header value. A value of null indicates that Failure-Report header was not provided.
    /// </summary>
    public string FailureReport { get; set; } = null;

    /// <summary>
    /// Byte-Range header. Initialized to a new ByteRangeHeader object.
    /// </summary>
    public ByteRangeHeader ByteRange { get; set; } = new ByteRangeHeader();

    /// <summary>
    /// Status header. A null value indicates that a Status header is not present
    /// </summary>
    public MsrpStatusHeader Status { get; set; } = null;

    /// <summary>
    /// To-Path header. Initialized to a new MsrpPathHeader object.
    /// </summary>
    public MsrpPathHeader ToPath { get; set; } = new MsrpPathHeader();

    /// <summary>
    /// From-Path header. Initialized to a new MsrpPathHeadr object.
    /// </summary>
    public MsrpPathHeader FromPath { get; set; } = new MsrpPathHeader();

    /// <summary>
    /// Contains the binary byte array containing the message contents. Null indicates that there are
    /// no contents for the message.
    /// </summary>
    public byte[] Contents = null;

    /// <summary>
    /// Byte string that delimits the MSRP header block from the contents body. This corresponds to 
    /// CRLFCRLF (\r\n\r\n).
    /// </summary>
    private static readonly byte[] BodyDelimArray = { 0x0d, 0x0a, 0x0d, 0x0a };

    /// <summary>
    /// Constructor
    /// </summary>
    public MsrpMessage()
    {
    }

    /// <summary>
    /// Parses a MSRP message that was received over a TCP/TLS network connection.
    /// </summary>
    /// <param name="bytes">Bytes of the complete MSRP message or a MSRP message chunk. The byte
    /// array includes the end-line.</param>
    /// <param name="completionStatus">Indicates the completion status of the MSRP message</param>
    /// <returns>Returns a new MsrpMessage object or null if a parsing error occurred.</returns>
    public static MsrpMessage ParseMsrpMessage(byte[] bytes, MsrpCompletionStatus completionStatus)
    {
        MsrpMessage msrpMessage = new MsrpMessage();

        int BodyDelimiterIndex = ByteBufferInfo.FindFirstBytePattern(bytes, 0, BodyDelimArray);
        byte[] HeaderBytes;
        if (BodyDelimiterIndex == -1)
            HeaderBytes = bytes;    // There is no body in this message
        else
        {
            HeaderBytes = new byte[BodyDelimiterIndex];
            Array.ConstrainedCopy(bytes, 0, HeaderBytes, 0, HeaderBytes.Length);
        }

        bool Success = ParseHeaders(HeaderBytes, msrpMessage);
        if (Success == false)
            return null;

        // Get the body if there is one
        if (BodyDelimiterIndex > 0)
        {   // There is a body
            msrpMessage.Contents = ByteBufferInfo.ExtractDelimitedByteArray(bytes, BodyDelimiterIndex,
                BodyDelimArray, Encoding.UTF8.GetBytes("-------"));
        }

        return msrpMessage;
    }

    private static bool ParseHeaders(byte[] HeaderBytes, MsrpMessage msrpMessage)
    {
        bool Success = true;
        string strHeaders = Encoding.UTF8.GetString(HeaderBytes);
        string[] HeaderLines = strHeaders.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        if (HeaderLines == null || HeaderLines.Length < 4)
            return false;

        // The first line is the request or the response line. For a request, the format is:
        // MSRP TransactionID Request, where Request is either SEND or REPORT. For a response,
        // the format is: MSRP TransactionID ResponseCode [Response Text]
        string[] FirstLineFields = HeaderLines[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (FirstLineFields == null || FirstLineFields.Length < 3)
            return false;   // Error: Invalid request line

        if (FirstLineFields[0] != "MSRP")
            return false;   // Error: Invalid request line

        int code = 0;
        if (int.TryParse(FirstLineFields[2], out code) == true)
        {   // Its a MSRP response message
            msrpMessage.MessageType = MsrpMessageType.Response;
            msrpMessage.ResponseCode = code;
            if (FirstLineFields.Length > 4)
                msrpMessage.ResponseText = FirstLineFields[3];
        }
        else if (FirstLineFields[2] == "SEND" || FirstLineFields[2] == "REPORT")
        {   // It is a known request
            msrpMessage.MessageType = MsrpMessageType.Request;
            msrpMessage.RequestLine = HeaderLines[0];
        }
        else
            return false;   // Error: Unknown request type

        msrpMessage.TransactionID = FirstLineFields[1];

        // Parse each of the header lines
        string HeaderName = null;
        string HeaderValue = null;
        for (int i = 1; i < HeaderLines.Length; i++)
        {
            GetHeaderAndValue(HeaderLines[i], out HeaderName, out HeaderValue);
            if (HeaderName == null || HeaderValue == null)
                continue;   // Skip this line

            switch (HeaderName)
            {
                case "To-Path":
                    msrpMessage.ToPath = MsrpPathHeader.ParseMsrpPathHeader(HeaderValue);
                    break;
                case "From-Path":
                    msrpMessage.FromPath = MsrpPathHeader.ParseMsrpPathHeader(HeaderValue);
                    break;
                case "Message-ID":
                    msrpMessage.MessageID = HeaderValue;
                    break;
                case "Success-Report":
                    msrpMessage.SuccessReport = HeaderValue;
                    break;
                case "Failure-Report":
                    msrpMessage.FailureReport = HeaderValue;
                    break;
                case "Byte-Range":
                    msrpMessage.ByteRange = ByteRangeHeader.ParseByteRangeHeader(HeaderValue);
                    break;
                case "Status":
                    msrpMessage.Status = MsrpStatusHeader.ParseStatusHeader(HeaderValue);
                    break;
                case "Content-Type":
                    msrpMessage.ContentType = HeaderValue;
                    break;
                case "ext-header":

                    break;
            } // end switch

        }

        return Success;
    }

    /// <summary>
    /// Splits a header string into a header name and a header value.
    /// </summary>
    /// <param name="HeaderLine">Input header string.</param>
    /// <param name="HeaderName">Output header name. Will be set to null if the header line is not
    /// properly formatted.</param>
    /// <param name="HeaderValue">Output header value. Will be set to null if the header line is not
    /// properly formatted. </param>
    private static void GetHeaderAndValue(string HeaderLine, out string HeaderName, out string
        HeaderValue)
    {
        HeaderName = null;
        HeaderValue = null;

        int Idx = HeaderLine.IndexOf(":");
        if (Idx < 0)
            return;

        if (Idx == HeaderLine.Length - 1)
            return;     // Error the ':' is in the last character position.

        HeaderName = HeaderLine.Substring(0, Idx).Trim();
        HeaderValue = HeaderLine.Substring(Idx + 1).Trim();
    }

    /// <summary>
    /// Creates a random string to use as a Session ID for MSRP.
    /// </summary>
    /// <returns>Returns the random string.</returns>
    public static string NewTransactionID()
    {
        return Crypto.GetRandomString(10);
    }
}

/// <summary>
/// Enumeration for MSRP message types.
/// </summary>
public enum MsrpMessageType
{
    /// <summary>
    /// MSRP request message.
    /// </summary>
    Request,
    /// <summary>
    /// MSRP response message.
    /// </summary>
    Response,
    /// <summary>
    /// Invalid message.
    /// </summary>
    InvalidMessage
}