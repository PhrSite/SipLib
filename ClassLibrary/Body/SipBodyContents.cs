/////////////////////////////////////////////////////////////////////////////////////
//  File: SipBodyContents.cs                                        23 Nov 22 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Body;

/// <summary>
/// Container class for adding contents to the body of a SIP request.
/// </summary>
public class SipBodyContents
{
    /// <summary>
    /// Content-Type header value for the contents. This field is required.
    /// </summary>
    public string ContentType = null;
    /// <summary>
    /// Content-Disposition header value for the contents. This field is optional.
    /// </summary>
    public string ContentDisposition = null;
    /// <summary>
    /// Content-ID header value for the contents. This field is optional.
    /// </summary>
    public string ContentID = null;
    /// <summary>
    /// Contains the contents to attach to the SIP request. This field is required.
    /// </summary>
    public string Contents = null;

    /// <summary>
    /// Constructs a new object from the Content-Type and the Body. The Content-Disposition and 
    /// the Content-ID header value are set to null.
    /// </summary>
    /// <param name="Ct">Content-Type header value.</param>
    /// <param name="Cont">Body</param>
    public SipBodyContents(string Ct, string Cont)
    {
        ContentType = Ct;
        Contents = Cont;
    }
}
