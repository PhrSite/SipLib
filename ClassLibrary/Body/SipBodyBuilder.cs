/////////////////////////////////////////////////////////////////////////////////////
//  File:   SipBodyBuilder.cs                                       23 Jun 22 PHR
/////////////////////////////////////////////////////////////////////////////////////

using SipLib.Core;
using System.Text;

namespace SipLib.Body;

/// <summary>
/// Class for building and attaching the contents body of a SIP request message.
/// </summary>
/// <remarks>Follow these steps to use this class:
/// <list type="number">
/// <item><description>Create an instance of this class.</description></item>
/// <item><description>Add SipBodyContents objects to the ContentsList
/// </description></item>
/// <item><description>Call the AttachRequestBody() method with a SIPRequest 
/// object to attach the body to.</description></item>
/// </list>
/// </remarks>
public class SipBodyBuilder
{
    private const string Boundary = "boundary1";

    /// <summary>
    /// Contains a list of different contents to add to the body.
    /// </summary>
    /// <value></value>
    public List<SipBodyContents> ContentsList = new List<SipBodyContents>();

    /// <summary>
    /// Builds the body of the request from the ContentsList and attaches it to the request body and sets
    /// the Content-Type and Content-Length headers in the request message.
    /// </summary>
    /// <param name="Req">SIP request message to add the contents to.</param>
    public void AttachRequestBody(SIPRequest Req)
    {
        StringBuilder Sb = new StringBuilder(8192);
        if (ContentsList.Count == 0)
            return;     // Nothing to attach

        if (ContentsList.Count == 1)
        {
            SipBodyContents Sbc = ContentsList[0];
            Req.Body = Sbc.Contents;
            Req.Header.ContentType = Sbc.ContentType;
            Req.Header.ContentLength = Req.Body.Length;
            return;
        }

        Req.Header.ContentType = string.Format("multipart/mixed;boundary={0}", Boundary);
        foreach (SipBodyContents Sbc in ContentsList)
        {
            Sb.AppendFormat("--{0}\r\n", Boundary);
            Sb.AppendFormat("Content-Type: {0}\r\n", Sbc.ContentType);
            if (string.IsNullOrEmpty(Sbc.ContentDisposition) == false)
                Sb.AppendFormat("Content-Disposition: {0}\r\n", Sbc.ContentDisposition);
            if (string.IsNullOrEmpty(Sbc.ContentID) == false)
                Sb.AppendFormat("Content-ID: {0}\r\n", Sbc.ContentID);

            Sb.Append("\r\n");
            Sb.Append(Sbc.Contents);
            Sb.Append("\r\n");
        }

        Sb.AppendFormat("--{0}--\r\n", Boundary);
        string strBody = Sb.ToString();
        Req.Header.ContentLength = strBody.Length;
        Req.Body = strBody;
    }
}
