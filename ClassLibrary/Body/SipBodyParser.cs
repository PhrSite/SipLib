/////////////////////////////////////////////////////////////////////////////////////
//  File:   SipBodyParser.cs                                        23 Nov 22 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Body;

using SipLib.Sdp;

/// <summary>
/// Class for processing the contents of a SIP message body. The body may contain nothing, a single 
/// type of contents such or application/sdp or a multi-part body contain two or more contents 
/// blocks of different MIME types.
/// </summary>
public class SipBodyParser
{
    /// <summary>
    /// Contains a list of ContentsContainer objects. Each ContentsContainer 
    /// object contains the lines of a single contents block.
    /// </summary>
    public List<SipContentsContainer> ContentsContainers = new List<SipContentsContainer>();
    private string m_MsgBody = null;
    private string m_ContentsType = null;

    /// <summary>
    /// Creates a new SipBodyParser object by parsing a string containing the body of a SIP message.
    /// </summary>
    /// <param name="MsgBody">A string containing the contents of the body of the SIP message.</param>
    /// <param name="ContentsType">The value of the Content-Type header of the SIP message
    /// that contains the body.</param>
    public static SipBodyParser ParseSipBody(string MsgBody, string ContentsType)
    {
        SipBodyParser Sbp = new SipBodyParser();
        if (string.IsNullOrEmpty(MsgBody) == true || string.IsNullOrEmpty(ContentsType) == true)
        {
            Sbp.m_MsgBody = null;
            Sbp.m_ContentsType = "Unknown";
            return Sbp;
        }

        Sbp.m_MsgBody = MsgBody;
        Sbp.m_ContentsType = ContentsType;

        int Idx = ContentsType.ToUpper().IndexOf("MULTIPART");
        if (Idx >= 0)
            ProcessMultiPartContents(Sbp);
        else
            ProcessSinglePartContents(Sbp);

        return Sbp;
    }

    /// <summary>
    /// Constructor
    /// </summary>
    public SipBodyParser()
    {
    }

    /// <summary>
    /// Gets the boundary string that separates each contents block in a multipart/mixed SIP message 
    /// body. Will be null if the Content-Type header value is not multipart/mixed.
    /// </summary>
    public string BoundaryString { get; private set; } = null;

    /// <summary>
    /// Processes the case where the SIP message body contains multiple contents blocks, i.e. 
    /// Content-Type = "multipart/mixed".
    /// </summary>
    private static void ProcessMultiPartContents(SipBodyParser Sbp)
    {
        // Get the contents boundary delimiter.
        int Idx = Sbp.m_ContentsType.IndexOf("=");
        if (Idx < 0)
            return;     // Error: There is no contents boundary string specified.

        // Get the boundary string and remove any quotes if present.
        string Boundary = Sbp.m_ContentsType.Substring(Idx + 1).Replace("\"", "").Trim();
        Sbp.BoundaryString = Boundary;

        string[] Blocks = Sbp.m_MsgBody.Split(new string[] { Boundary }, StringSplitOptions.None);
        if (Blocks == null || Blocks.Length == 0)
            return;

        // Process contents block that is delimited by the contents boundary string.
        foreach (string str in Blocks)
        {
            if (str.IndexOf("Content-Type") >= 0)
                ProcessContentsBlock(str, Sbp);
        }
    }

    private static string GetHeaderValue(string HdrLine)
    {
        string strValue = null;
        int Idx = HdrLine.IndexOf(":");
        if (Idx >= 0)
            strValue = HdrLine.Substring(Idx + 1).Trim();

        return strValue;
    }

    /// <summary>
    /// Builds a SipContentsContainer object for a single contents block and adds it to the list 
    /// contained in the ContentsContainers list.
    /// </summary>
    /// <param name="strBlock">Input block</param>
    /// <param name="Sbp">For parsing the body block.</param>
    private static void ProcessContentsBlock(string strBlock, SipBodyParser Sbp)
    {
        string[] strLines = strBlock.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (strLines == null || strLines.Length == 0)
            return;

        SipContentsContainer Cc = new SipContentsContainer();
        string strUpper, strTrim;
        foreach (string str in strLines)
        {
            strUpper = str.ToUpper();
            if (strUpper.IndexOf("CONTENT-TYPE") >= 0)
            {
                Cc.ContentsType = GetHeaderValue(str);
                // Process any parameters in the Content-Type line
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
            else if (strUpper.IndexOf("CONTENT-DISPOSITION") >= 0)
                Cc.ContentsDispositon = GetHeaderValue(str);
            else if (strUpper.IndexOf("CONTENT-ID") >= 0)
                Cc.ContentID = GetHeaderValue(str);
            else if (strUpper.IndexOf("CONTENT-LENGTH") >= 0)
                Cc.ContentsLength = GetHeaderValue(str);
            else if (strUpper.IndexOf("CONTENT-TRANSFER-ENCODING") >= 0)
                Cc.ContentTransferEncoding = GetHeaderValue(str);
            else
            {
                strTrim = str.Trim();
                // Skip empty lines or lines that contain the last "--"
                if (string.IsNullOrEmpty(strTrim) == false && strTrim != "--")
                    Cc.ContentsLines.Add(strTrim);
            }
        }

        Sbp.ContentsContainers.Add(Cc);
    }

    /// <summary>
    /// Processes the case where the SIP message body contains a single contents block (i.e. 
    /// Content-Type != "multipart/mixed".
    /// </summary>
    private static void ProcessSinglePartContents(SipBodyParser Sbp)
    {
        string[] strLines = Sbp.m_MsgBody.Split(new char[] { '\n' });
        if (strLines == null || strLines.Length == 0)
            return;

        char[] Whitespace = { '\r', '\n', ' ' };
        SipContentsContainer Cc = new SipContentsContainer();
        Cc.ContentsType = Sbp.m_ContentsType;
        string strTrim;

        foreach (string str in strLines)
        {
            strTrim = str.Trim();
            // Skip empty lines or lines that contain the last "--"
            if (string.IsNullOrEmpty(strTrim) == false && strTrim != "--")
                Cc.ContentsLines.Add(strTrim);
        }

        Sbp.ContentsContainers.Add(Cc);
    }

    /// <summary>
    /// Returns a ContentsContainer containing the contents lines for a specified
    /// Content-Type.
    /// </summary>
    /// <param name="ContentsType">MIME contents type to search for. For example:
    /// application/sdp.</param>
    /// <returns>Returns the first ContentsContainer found or null if the message
    /// body does not contain the specified contents type.</returns>
    public SipContentsContainer GetContentsOfType(string ContentsType)
    {
        SipContentsContainer RetCc = null;
        foreach (SipContentsContainer Cc in ContentsContainers)
        {
            if (Cc.ContentsType == ContentsType)
            {
                RetCc = Cc;
                break;
            }
        }
        return RetCc;
    }

    /// <summary>
    /// Returns a ContentsContainer containing the contents lines for a specified Content-Type using a 
    /// case-insensitive comparison.
    /// </summary>
    /// <param name="ContentsType">MIME contents type to search for. For example:
    /// application/sdp.</param>
    /// <returns>Returns the first ContentsContainer found or null if the message body does not 
    /// contain the specified contents type.</returns>
    public SipContentsContainer GetContentsOfTypeNc(string ContentsType)
    {
        SipContentsContainer RetCc = null;
        foreach (SipContentsContainer Cc in ContentsContainers)
        {
            if (Cc.ContentsType.ToLower() == ContentsType.ToLower())
            {
                RetCc = Cc;
                break;
            }
        }

        return RetCc;
    }

    /// <summary>
    /// Tests to see of the SIP message body has a specified Content-Type. Uses case-insensitive comparison.
    /// </summary>
    /// <param name="ContentsType">Content-Type header value.</param>
    /// <returns>Returns true if the content type exists or false if it does not.</returns>
    public bool HasContentType(string ContentsType)
    {
        bool Result = false;
        foreach (SipContentsContainer Cc in ContentsContainers)
        {
            if (Cc.ContentsType.ToLower() == ContentsType.ToLower())
            {
                Result = true; ;
                break;
            }
        }

        return Result;
    }

    /// <summary>
    /// Builds a SipSdp object from the contents of a SIP message.
    /// </summary>
    /// <returns>Return a Sdp object if there is a contents type of application/sdp or null if there 
    /// is not.</returns>
    public Sdp GetSdpContents()
    {
        SipContentsContainer SdpCc = GetContentsOfType("application/sdp");
        if (SdpCc == null)
            return null;    // No SDP in the message body.
        else 
            return Sdp.ParseSDP(SdpCc.ContentsLines);
    }

    /// <summary>
    /// Gets the contents of the body of the SIP message as a string given the specified type of contents.
    /// </summary>
    /// <param name="ContentType">Body type to search for. For example:
    ///     "application/pidf+xml or application/conference-info+xml.</param>
    /// <returns>Returns a string containing the specified contents or
    /// null if the contents could not be found.</returns>
    public string GetBodyContents(string ContentType)
    {
        if (m_MsgBody == null || string.IsNullOrEmpty(ContentType) == true)
            return null;

        SipContentsContainer Cc = GetContentsOfType(ContentType);
        if (Cc == null)
            return null;
        else
            return Cc.ToString();
    }
}
