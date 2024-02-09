/////////////////////////////////////////////////////////////////////////////////////
//  File: SipBodyContents.cs                                        23 Nov 22 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Body;

/// <summary>
/// Container class for adding contents to the body of a SIP request.
/// </summary>
internal class SipBodyContents
{
    /// <summary>
    /// Content-Type header value for the contents. This field is required.
    /// </summary>
    /// <value></value>
    public string? ContentType = null;
    /// <summary>
    /// Contains the contents to attach to the SIP request. This field is required.
    /// </summary>
    /// <value></value>
    public string? Contents = null;
    /// <summary>
    /// Content-ID header value for the contents. This field is optional.
    /// </summary>
    /// <value></value>
    public string? ContentID = null;

    /// <summary>
    /// Content-Disposition header value for the contents. This field is optional.
    /// </summary>
    /// <value></value>
    public string? ContentDisposition = null;

    /// <summary>
    /// Constructs a new object from the Content-Type and the Body. The Content-Disposition and 
    /// the Content-ID header value are set to null.
    /// </summary>
    /// <param name="contentType">Content-Type header value.</param>
    /// <param name="contents">Body</param>
    public SipBodyContents(string contentType, string contents)
    {
        ContentType = contentType;
        Contents = contents;
    }

    /// <summary>
    /// Constructor
    /// </summary>
    public SipBodyContents()
    {
    }

}
