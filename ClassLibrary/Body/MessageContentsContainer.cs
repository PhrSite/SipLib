//////////////////////////////////////////////////////////////////////////////////////
//  File:   MessageContentsContainer.cs                             23 Nov 22 PHR
//
//  Revised:    20 Aug 23 PHR
//                -- Removed List<string> ContentsLines and replaced it with
//                   string StringContents.
//                -- Changed the name from SipContentsContainer to MessageContentsContainer
//////////////////////////////////////////////////////////////////////////////////////

using System.Collections.Specialized;

namespace SipLib.Body;

/// <summary>
/// Class for holding the Content-Type and the contents for a single contents block of a SIP message
/// or an MSRP message.
/// </summary>
public class MessageContentsContainer
{
    /// <summary>
    /// Contains the value of the Content-Type header that indicates the type of the body contents block. 
    /// </summary>
    /// <value></value>
    public string ContentType = "";
    /// <summary>
    /// Contains the Content-Disposition header value. Will be null if there is no Content-Disposition 
    /// header.
    /// </summary>
    /// <value></value>
    public string? ContentDispositon = null;
    /// <summary>
    /// Contains the Content-ID header value. Will be null if there is none.
    /// </summary>
    /// <value></value>
    public string? ContentID = null;

    /// <summary>
    /// Contains the Content-Transfer-Encoding header value. Will be null if there is none.
    /// </summary>
    /// <value></value>
    public string? ContentTransferEncoding = null;

    /// <summary>
    /// Contains the string value of the Content-Length header. Will be null if there is none.
    /// Optional for contents blocks in a message where the Contents-Type is multipart/mixed.
    /// </summary>
    /// <value></value>
    public string? ContentLength = null;

    /// <summary>
    /// Contains the message body contents as a string. Not null if IsBinaryContents is false.
    /// </summary>
    /// <value></value>
    public string? StringContents = null;

    /// <summary>
    /// Contains a collection of parameters from the Content-Type header.
    /// </summary>
    /// <value></value>
    public NameValueCollection ContentTypeParams = new NameValueCollection();

    /// <summary>
    /// If true, then the contents contains raw binary data that must not be converted to a string.
    /// </summary>
    /// <value></value>
    public bool IsBinaryContents = false;
    /// <summary>
    /// Contains the raw binary data. Will be non-null if IsBinaryContents is true.
    /// </summary>
    /// <value></value>
    public byte[]? BinaryContents = null;
}
