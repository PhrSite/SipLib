/////////////////////////////////////////////////////////////////////////////////////
//  File:   CpimMessage.cs                                          20 Jul 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Msrp;

using SipLib.Core;
using System.Text;

/// <summary>
/// Class for Common Profile for Instant Messaging (CPIM) messages defined by RFC 3862. CPIM messages
/// are not used in standalone manner in NG9-1-1 applications. They are always encapsulated in MSRP messages.
/// </summary>
public class CpimMessage
{
    private const string CRLF = "\r\n";
    private const string BodyDelim = CRLF + CRLF;

    /// <summary>
    /// Gets or sets the From header. Required.
    /// </summary>
    public SIPUserField From { get; set; }

    /// <summary>
    /// Gets or sets a list of To header fields. Initialized to an empty list. The list
    /// must contain at least one To header
    /// </summary>
    public List<SIPUserField> To { get; set; } = new List<SIPUserField>();

    /// <summary>
    /// Gets or sets the list of cc header fields. Initialized to an empty list. cc headers are optional.
    /// </summary>
    public List<SIPUserField> cc { get; set; } = new List<SIPUserField>();

    /// <summary>
    /// Gets or sets the Content-Type header value. Required. Must be set to a valid MIME type such as
    /// text/plain.
    /// </summary>
    public string ContentType { get; set; } = null;

    /// <summary>
    /// Gets or set the Content-ID header value. Optional.
    /// </summary>
    public string ContentID { get; set; } = null;

    /// <summary>
    /// Gets or sets the a list of Subject header values. Optional. Initialized to an empty list.
    /// </summary>
    public List<string> Subject { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the value of the DateTime header field. Optional. A value of DateTime.MinValue
    /// indicates that the DateTime header is not present.
    /// </summary>
    public DateTime DateTime { get; set; } = DateTime.MinValue;

    /// <summary>
    /// Gets or sets the value of the Require header field. Optional.
    /// </summary>
    public string Require { get; set; } = null;

    /// <summary>
    /// Gets or sets the list of Name Space (NS) headers. Optional. Initialized to an empty string.
    /// NS headers are not used in this implementation and are treated as simple strings.
    /// </summary>
    public List<string> NS = new List<string>();

    /// <summary>
    /// Stores unknown or non-standard headers lines (header: headervalue). Initialized to an empty list.
    /// </summary>
    public List<string> NonStandardHeaders = new List<string>();

    /// <summary>
    /// Gets or sets the contents (body) of the message.
    /// </summary>
    public byte[] Body { get; set; } = null;

    /// <summary>
    /// Default constructor
    /// </summary>
    public CpimMessage()
    {
    }

    /// <summary>
    /// Parses a CPIM message contained in a byte array.
    /// </summary>
    /// <param name="cpimBytes">Input message</param>
    /// <returns>Returns a new CpimMessage object if successful or null is a parsing error occurred</returns>
    public static CpimMessage ParseCpimBytes(byte[] cpimBytes)
    {
        CpimMessage cpimMessage = null;

        string strMsg = Encoding.UTF8.GetString(cpimBytes);
        int LastIdx = strMsg.LastIndexOf(BodyDelim);
        string HeaderString = null;
        byte[] BodyBytes = null;


        int Idx = strMsg.ToLower().IndexOf("content-type");
        if (Idx < 0)
            // This is an error because there must be a Content-Type header. The body, if there is one
            // is not usable. Try to parse just the headers.
            HeaderString = strMsg;
        else
        {
            // Must do a binary search for the body.
            int FirstBodyDelimIdx = ByteBufferInfo.GetStringPosition(cpimBytes, 0, cpimBytes.Length
                - 1, BodyDelim, null);
            if (FirstBodyDelimIdx < 0)
                // Error: Try to parse just the headers
                HeaderString = strMsg;
            else
            {
                int LastBodyDelimIdx = ByteBufferInfo.GetStringPosition(cpimBytes, FirstBodyDelimIdx +
                        BodyDelim.Length, cpimBytes.Length - 1, BodyDelim, null);
                if (LastBodyDelimIdx < 0)
                    HeaderString = strMsg;
                else
                {
                    byte[] HeaderBytes = new byte[LastBodyDelimIdx];
                    Array.Copy(cpimBytes, HeaderBytes, HeaderBytes.Length);
                    HeaderString = Encoding.UTF8.GetString(HeaderBytes);
                    int BodyStartIdx = LastBodyDelimIdx + BodyDelim.Length;
                    if (BodyStartIdx < cpimBytes.Length - 1)
                    {
                        int BodyLength = cpimBytes.Length - BodyStartIdx;
                        BodyBytes = new byte[BodyLength];
                        Array.ConstrainedCopy(cpimBytes, BodyStartIdx, BodyBytes, 0, BodyLength);
                    }
                }
            }
        }

        cpimMessage = new CpimMessage();
        cpimMessage.Body = BodyBytes;

        try
        {
            ParseHeaders(HeaderString, cpimMessage);
        }
        catch
        {
            cpimMessage = null;
        }

        return cpimMessage;
    }

    private static void ParseHeaders(string HeaderString, CpimMessage cpimMessage)
    {
        string[] Lines = HeaderString.Split(new string[] { CRLF }, StringSplitOptions.RemoveEmptyEntries);
        if (Lines == null || Lines.Length == 0)
            return;     // No header lines

        SIPUserField Suf = null;
        string HeaderName, HeaderValue;
        int Idx;
        foreach (string Line in Lines)
        {
            Idx = Line.IndexOf(":");
            if (Idx < 0)
                continue;   // Error: Not a valid header line so skip it

            HeaderName = Line.Remove(Idx);
            HeaderValue = Line.Substring(Idx + 1).Trim();
            if (string.IsNullOrEmpty(HeaderName) || string.IsNullOrEmpty(HeaderValue))
                continue;   // Error: Not a valid header line

            switch (HeaderName)
            {
                case "To":
                    Suf = SIPUserField.TryParseSIPUserField(HeaderValue);
                    if (Suf != null)
                        cpimMessage.To.Add(Suf);
                    break;
                case "From":
                    Suf = SIPUserField.TryParseSIPUserField(HeaderValue);
                    if (Suf! != null)
                        cpimMessage.From = Suf;
                    break;
                case "DateTime":
                    DateTime Dt;
                    if (DateTime.TryParse(HeaderValue, out Dt) == true)
                        cpimMessage.DateTime = Dt;
                    break;
                case "Require":
                    cpimMessage.Require = HeaderValue;
                    break;
                case "NS":
                    cpimMessage.NS.Add(HeaderValue);
                    break;
                case "cc":
                    Suf = SIPUserField.TryParseSIPUserField(HeaderValue);
                    if (Suf != null)
                        cpimMessage.cc.Add(Suf);
                    break;
                case "Subject":
                    cpimMessage.Subject.Add(HeaderValue);
                    break;
                case "Content-Type":
                    cpimMessage.ContentType = HeaderValue;
                    break;
                case "Content-ID":
                    cpimMessage.ContentID = HeaderValue;
                    break;
                default:
                    cpimMessage.NonStandardHeaders.Add(Line);
                    break;
            } // end switch
        } // end foreach
    }

    /// <summary>
    /// Converts this object into a byte array so that it may be encapsulated in a MSRP message.
    /// </summary>
    /// <returns>Returns a UTF8 encoded byte array.</returns>
    public byte[] ToByteArray()
    {
        byte[] byteArray = null;

        StringBuilder Sb = new StringBuilder(1024);

        foreach (SIPUserField ToUsf in To)
            Sb.AppendFormat("To: {0}{1}", ToUsf.ToCpimFormatString(), CRLF);

        if (From != null)
            Sb.AppendFormat("From: {0}{1}", From.ToCpimFormatString(), CRLF);

        foreach (SIPUserField ccSuf in cc)
            Sb.AppendFormat("From: {0}{1}", ccSuf.ToCpimFormatString(), CRLF);

        foreach (string strSub in Subject)
            Sb.AppendFormat("Subject: {0}{1}", strSub, CRLF);

        if (DateTime != DateTime.MinValue)
            Sb.AppendFormat("DateTime: {0}{1}", DateTime.ToString("yyyy-MM-ddTHH:mm:sszzz"), CRLF);

        foreach (string strNS in NS)
            Sb.AppendFormat("NS: {0}{1}", strNS, CRLF);

        if (string.IsNullOrEmpty(Require) == false)
            Sb.AppendFormat("Require: {0}{1}", Require, CRLF);

        foreach (string strNonStandard in NonStandardHeaders)
            Sb.AppendFormat("{0}{1}", strNonStandard, CRLF);

        if (string.IsNullOrEmpty(ContentType) == false && Body != null && Body.Length > 0)
        {
            Sb.AppendFormat("\r\nContent-Type: {0}{1}", ContentType, CRLF);
            if (string.IsNullOrEmpty(ContentID) == false)
                Sb.AppendFormat("Content-ID: {0}{1}", ContentID, BodyDelim);
            else
                Sb.Append(CRLF);
        }

        MemoryStream memoryStream = new MemoryStream();
        memoryStream.Write(Encoding.UTF8.GetBytes(Sb.ToString()));
        if (Body != null && Body.Length > 0)
            memoryStream.Write(Body, 0, Body.Length);

        byteArray = memoryStream.ToArray();
        return byteArray;
    }
}
