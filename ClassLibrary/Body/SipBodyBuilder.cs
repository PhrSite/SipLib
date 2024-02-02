/////////////////////////////////////////////////////////////////////////////////////
//  File:   SipBodyBuilder.cs                                       23 Jun 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

using SipLib.Core;
using System.Text;

namespace SipLib.Body;

/// <summary>
/// Class for building and attaching the contents body of a SIP request or a SIP response message.
/// </summary>
/// <remarks>Follow these steps to use this class:
/// <list type="number">
/// <item><description>Create an instance of this class.</description></item>
/// <item><description>Call the AddContent() method for each content block that needs to be added.</description>
/// </item>
/// <item><description>Call the AttachMessageBody() method with a SIPMessage object to attach the body to.
/// </description></item>
/// </list>
/// </remarks>
public class SipBodyBuilder
{
    private const string Boundary = "boundary1";

    /// <summary>
    /// Contains a list of different contents to add to the body.
    /// </summary>
    /// <value></value>
    private List<SipBodyContents> ContentsList = new List<SipBodyContents>();

    /// <summary>
    /// Builds the body of the SIP message from the ContentsList and attaches it to the request body and sets
    /// the Content-Type and Content-Length headers in the SIP message.
    /// </summary>
    /// <param name="Msg">SIP request message to add the contents to.</param>
    public void AttachMessageBody(SIPMessage Msg)
    {
        StringBuilder Sb = new StringBuilder(8192);
        if (ContentsList.Count == 0)
            return;     // Nothing to attach

        if (ContentsList.Count == 1)
        {
            SipBodyContents Sbc = ContentsList[0];
            Msg.Body = Sbc.Contents;
            Msg.Header.ContentType = Sbc.ContentType;
            Msg.Header.ContentLength = Msg.Body.Length;
            return;
        }

        Msg.Header.ContentType = string.Format("multipart/mixed;boundary={0}", Boundary);
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
        Msg.Header.ContentLength = strBody.Length;
        Msg.Body = strBody;
    }

    /// <summary>
    /// Adds a new content block to the list of contents.
    /// </summary>
    /// <param name="contentType">Value of the Content-Type header. For example: application/sdp. Required.</param>
    /// <param name="content">Sting containing the content to add to the message body. Required.</param>
    /// <param name="contentID">Value of the Content-ID header. Optional, may be null.</param>
    /// <param name="contentDisposition">Value of the Content-Disposition header. Optional, may be null.</param>
    public void AddContent(string contentType, string content, string contentID, string contentDisposition)
    {
        SipBodyContents Sbc = new SipBodyContents()
        {
            ContentType = contentType,
            Contents = content,
            ContentID = contentID,
            ContentDisposition = contentDisposition
        };

        ContentsList.Add(Sbc);
    }
}
