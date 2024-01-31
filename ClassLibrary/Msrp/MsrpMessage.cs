/////////////////////////////////////////////////////////////////////////////////////
//  File:   MsrpMessage.cs                                          24 Jul 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

using SipLib.Core;
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
    /// <value></value>
    public MsrpMessageType MessageType { get; set; } = MsrpMessageType.Request;

    /// <summary>
    /// Transaction ID for the message;
    /// </summary>
    /// <value></value>
    public string TransactionID = NewRandomID();

    /// <summary>
    /// Request line for the message. This will be null if the MessageType is not MessageType.Request
    /// </summary>
    /// <value></value>
    public string RequestLine { get; set; } = null;

    /// <summary>
    /// Contains the request methode type (SEND or REPORT) if MessageType == MsrpMessageType.Request.
    /// Set to null otherwise.
    /// </summary>
    /// <value></value>
    public string RequestMethod { get; set; } = null;

    /// <summary>
    /// Response code for the message. Valid if the MessageType is MessageType.Response
    /// </summary>
    /// <value></value>
    public int ResponseCode { get; set; } = 0;

    /// <summary>
    /// Text or comment describing the ResponseCode. Optional.
    /// </summary>
    /// <value></value>
    public string ResponseText { get; set; } = null;

    /// <summary>
    /// Message-ID header value.
    /// </summary>
    /// <value></value>
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
    /// <value></value>
    public string SuccessReport { get; set; } = null;

    /// <summary>
    /// Failure-Report header value. A value of null indicates that Failure-Report header was not provided.
    /// </summary>
    /// <value></value>
    public string FailureReport { get; set; } = null;

    /// <summary>
    /// Byte-Range header. Initialized to a new ByteRangeHeader object.
    /// </summary>
    /// <value></value>
    public ByteRangeHeader ByteRange { get; set; } = new ByteRangeHeader();

    /// <summary>
    /// Status header. A null value indicates that a Status header is not present
    /// </summary>
    /// <value></value>
    public MsrpStatusHeader Status { get; set; } = null;

    /// <summary>
    /// To-Path header. Initialized to a new MsrpPathHeader object.
    /// </summary>
    /// <value></value>
    public MsrpPathHeader ToPath { get; set; } = new MsrpPathHeader();

    /// <summary>
    /// From-Path header. Initialized to a new MsrpPathHeadr object.
    /// </summary>
    /// <value></value>
    public MsrpPathHeader FromPath { get; set; } = new MsrpPathHeader();

    /// <summary>
    /// Use-Nickname header value. See RFC 7701. This header value contains a quoted (using double
    /// quotation marks) nickname.
    /// </summary>
    /// <value></value>
    public string UseNickname { get; set; } = null;

    /// <summary>
    /// Contains the binary byte array containing the message contents. Null indicates that there are
    /// no contents for the message.
    /// </summary>
    /// <value></value>
    public byte[] Body = null;

    /// <summary>
    /// Gets or sets the completion status for this MSRP message
    /// </summary>
    /// <value></value>
    public MsrpCompletionStatus CompletionStatus { get; set; } = MsrpCompletionStatus.Complete;

    /// <summary>
    /// Byte string that delimits the MSRP header block from the contents body. This corresponds to 
    /// CRLFCRLF (\r\n\r\n).
    /// </summary>
    private static readonly byte[] BodyDelimArray = { 0x0d, 0x0a, 0x0d, 0x0a };

    private const string EndLinePrefixString = "-------";

    private const string CRLF = "\r\n";

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
    /// <returns>Returns a new MsrpMessage object or null if a parsing error occurred.</returns>
    public static MsrpMessage ParseMsrpMessage(byte[] bytes)
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

        // Get the continuation flag from the end-line
        byte[] EndLinePatternBytes = Encoding.UTF8.GetBytes(EndLinePrefixString + msrpMessage.TransactionID);
        int EndLineIndex = ByteBufferInfo.FindLastBytePattern(bytes, bytes.Length - 1, EndLinePatternBytes);
        if (EndLineIndex == -1)
            return null;    // Error: No end-line in the message

        int ContinuationFlagIndex = EndLineIndex + EndLinePatternBytes.Length;
        if (ContinuationFlagIndex >= bytes.Length)
            return null;    // Error: No continuation flag in the message

        char ContinuationFlag = Convert.ToChar(bytes[ContinuationFlagIndex]);
        switch (ContinuationFlag)
        {
            case '$':
                msrpMessage.CompletionStatus = MsrpCompletionStatus.Complete;
                break;
            case '+':
                msrpMessage.CompletionStatus = MsrpCompletionStatus.Continuation;
                break;
            case '#':
                msrpMessage.CompletionStatus = MsrpCompletionStatus.Truncated;
                break;
            default:
                msrpMessage.CompletionStatus = MsrpCompletionStatus.Unknown;
                break;
        }

        // Get the body if there is one
        if (BodyDelimiterIndex > 0)
        {   // There is a body
            int BodyStartIndex = BodyDelimiterIndex + BodyDelimArray.Length;
            int BodyLength = EndLineIndex - BodyStartIndex;
            if (BodyLength > 0)
            {
                msrpMessage.Body = new byte[BodyLength];
                Array.ConstrainedCopy(bytes, BodyStartIndex, msrpMessage.Body, 0, BodyLength);
            }
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
            if (FirstLineFields.Length >= 4)
                msrpMessage.ResponseText = FirstLineFields[3];
        }
        else if (FirstLineFields[2] == "SEND" || FirstLineFields[2] == "REPORT" || FirstLineFields[2] ==
            "NICKNAME")
        {   // It is a known request. "NICKNAME" is defined in RFC 7701.
            msrpMessage.MessageType = MsrpMessageType.Request;
            msrpMessage.RequestLine = HeaderLines[0];
            msrpMessage.RequestMethod = FirstLineFields[2];
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
                case "Use-Nickname":
                    msrpMessage.UseNickname = HeaderValue;
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
    /// Creates a random string to use as a Session ID or a Transaction ID for MSRP.
    /// </summary>
    /// <returns>Returns the random string.</returns>
    public static string NewRandomID()
    {
        return Crypto.GetRandomString(10);
    }

    /// <summary>
    /// Converts this object into a byte array so that it can be sent over the TCP/TLS stream. All of
    /// the required properties must be set before calling this method.
    /// </summary>
    /// <returns>Returns a byte array.</returns>
    public byte[] ToByteArray()
    {
        byte[] byteArray = null;
        StringBuilder sb = new StringBuilder(2048); // Initial size

        if (MessageType == MsrpMessageType.Request)
            sb.Append($"MSRP {TransactionID} {RequestMethod}{CRLF}");
        else
        {
            sb.Append($"MSRP {TransactionID} {ResponseCode}");
            if (string.IsNullOrEmpty(ResponseText) == false)
                sb.Append($" {ResponseText}");

            sb.Append(CRLF);
        }

        sb.Append($"To-Path: {ToPath.ToString()}{CRLF}");
        sb.Append($"From-Path: {FromPath.ToString()}{CRLF}");

        if (string.IsNullOrEmpty(MessageID) == false)
            sb.Append($"Message-ID: {MessageID}{CRLF}");

        if (ByteRange != null)
            sb.Append($"Byte-Range: {ByteRange.ToString()}{CRLF}");

        if (string.IsNullOrEmpty(SuccessReport) == false)
            sb.Append($"Success-Report: {SuccessReport}{CRLF}");

        if (string.IsNullOrEmpty(FailureReport) == false)
            sb.Append($"Failure-Report: {FailureReport}{CRLF}");

        if (string.IsNullOrEmpty(UseNickname) == false)
            sb.Append($"Use-Nickname: {UseNickname}{CRLF}");

        if (Status != null)
            sb.Append($"Status: {Status.ToString()}{CRLF}");

        if (MessageType == MsrpMessageType.Request && RequestMethod == "SEND" && string.IsNullOrEmpty(
            ContentType) == false && Body != null)
            sb.Append($"Content-Type: {ContentType}{CRLF}{CRLF}");

        MemoryStream memoryStream = new MemoryStream();
        // Write the first line and the header lines to the stream
        memoryStream.Write(Encoding.UTF8.GetBytes(sb.ToString()));

        string Flag;
        switch (CompletionStatus)
        {
            case MsrpCompletionStatus.Complete:
                Flag = "$";
                break;
            case MsrpCompletionStatus.Continuation:
                Flag = "+";
                break;
            case MsrpCompletionStatus.Truncated:
                Flag = "#";
                break;
            default:
                Flag = "$";     // Error, but assume complete
                break;
        }

        // Write the body if there is one
        if (Body != null)
            memoryStream.Write(Body, 0, Body.Length);

        string EndLine = $"{EndLinePrefixString}{TransactionID}{Flag}{CRLF}";
        memoryStream.Write(Encoding.UTF8.GetBytes(EndLine));
        byteArray = memoryStream.ToArray();
        return byteArray;
    }

    /// <summary>
    /// Gets the value of the Content-Type header value without any parameters
    /// </summary>
    /// <returns>Returns the Content-Type header value after removing any header parameters if there
    /// are any. Return null if there is no Content-Type header value.</returns>
    public string GetContentType()
    {
        if (ContentType == null)
            return null;

        int index = ContentType.IndexOf(";");
        if (index != -1)
            // Strip out any parameters
            return ContentType.Substring(0, index).Trim();
        else
            return ContentType;
    }

    /// <summary>
    /// Returns true if there is a Success-Report header and its value is "yes"
    /// </summary>
    /// <returns></returns>
    public bool SuccessReportRequested()
    {
        if (string.IsNullOrEmpty(SuccessReport) == false && SuccessReport == "yes")
            return true;
        else
            return false;
    }

    /// <summary>
    /// Returns true if there is a Failure-Report header and its value is "yes"
    /// </summary>
    /// <returns></returns>
    public bool FailureReportRequested()
    {
        if (string.IsNullOrEmpty(FailureReport) == false && FailureReport == "yes")
            return true;
        else 
            return false;
    }

    /// <summary>
    /// Builds a MSRP response message to this message. This message must be a MSRP request message.
    /// </summary>
    /// <param name="ResponseCode">Response code. For example: 200</param>
    /// <param name="ResponseText">Response text. Optional. For example: OK</param>
    /// <returns>Returns a MsrpMessage containing a response that can be sent to the remote end point.</returns>
    public MsrpMessage BuildResponseMessage(int ResponseCode, string ResponseText)
    {
        MsrpMessage Msg = new MsrpMessage();

        Msg.MessageType = MsrpMessageType.Response; 
        Msg.ResponseCode = ResponseCode;
        Msg.ResponseText = ResponseText;
        Msg.TransactionID = TransactionID;
        Msg.CompletionStatus = MsrpCompletionStatus.Complete;
        Msg.ByteRange = ByteRange;

        if (ResponseCode == 200)
        {   // Special case -- For 200 OK, set the To-Path to the first (left-most MSRP URI in the From-Path
            // header. See Section 7.2 of RFC 4975.
            if (FromPath.MsrpUris.Count > 0)
                Msg.ToPath.MsrpUris.Add(FromPath.MsrpUris[0]);
        }
        else
        {   // All other cases use the full list of MSRP URIs.
            foreach (MsrpUri MsUri in FromPath.MsrpUris)
                Msg.ToPath.MsrpUris.Add(MsUri);
        }

        if (ToPath.MsrpUris.Count > 0)
            Msg.FromPath.MsrpUris.Add(ToPath.MsrpUris[0]);

        return Msg;
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
