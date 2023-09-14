/////////////////////////////////////////////////////////////////////////////////////
//	File:	MediaDescription.cs                                     16 Nov 22 PHR
/////////////////////////////////////////////////////////////////////////////////////

using System.Text;

namespace SipLib.Sdp;

/// <summary>
/// Class for processing the Media Description "m=" type for the SDP contents. See Section 5.14 of RFC 4566.
/// </summary>
public class MediaDescription
{
    /// <summary>
    /// Specifies the media type. Example: "audio", "video", "text" or "message".
    /// </summary>
    public string MediaType = "";
    /// <summary>
    /// Specifies the TCP/UDP port number;
    /// </summary>
    public int Port = 0;
    /// <summary>
    /// Specifies the transport mechanism: "TCP", "UDP", "RTP/AVP", etc.
    /// </summary>
    public string Transport = "";
    /// <summary>
    /// Contains a list of media format numbers.
    /// </summary>
    public List<string> MediaFormatNumbers = new List<string>();
    /// <summary>
    /// Contains the attributes for this type of media.
    /// </summary>
    public List<SdpAttribute> Attributes = new List<SdpAttribute>();
    /// <summary>
    /// Contains the connection data (c=) for this media. If null, then use the ConnectionData of the SDP
    /// session.
    /// </summary>
    public ConnectionData ConnectionData = null;

    /// <summary>
    /// Specifies the bandwidth limit for the media in kilo-bits per second. This is comes from or generates
    /// a b=AS:Bandwidth line in the media description. An empty or null value indicates that there is no b=
    /// line in the media description.
    /// </summary>
    public string Bandwidth = "";

    /// <summary>
    /// Constructs an empty MediaDescription object. Use this constructor to create a new Media Description
    /// for a new SDP message block to send.
    /// </summary>
    public MediaDescription()
    {
    }

    /// <summary>
    /// Parses a string containing the parameter fields of a SDP m= line
    /// </summary>
    /// <param name="strMd">Contains the parameter fields of a m= line. The m= field must not be included.
    /// </param>
    /// <returns>Returns a new MediaDescription object.</returns>
    // <exception cref="ArgumentException">Thrown if the media description parameters
    // are not valid.</exception>
    public static MediaDescription ParseMediaDescription(string strMd)
    {
        MediaDescription Md = new MediaDescription();
            char[] Delim = { ' ' };
            string[] Fields = strMd.Split(Delim);

        if (Fields.Length < 4)
            throw new ArgumentException("The number of fields in the media description " +
                "line is less than 4.", nameof(strMd));

        Md.MediaType = Fields[0];

        // Get the port number.
        int Idx = Fields[1].IndexOf('/');
        String strPort;
        if (Idx >= 0)
            // The number of ports is specified so remove it.
            strPort = Fields[1].Remove(Idx);
        else
            strPort = Fields[1];

        bool Success = int.TryParse(strPort, out Md.Port);
        if (Success == false)
            throw new ArgumentException("The port number in the media description " +
            "line is not valid", nameof(strMd));
            
        Md.Transport = Fields[2];

        int i;
        for (i = 3; i < Fields.Length; i++)
            Md.MediaFormatNumbers.Add(Fields[i]);

        return Md;
    }

    /// <summary>
    /// Creates a deep copy of this object.
    /// </summary>
    /// <returns>Returns a new object with a copy of each member variable in this object.</returns>
    public MediaDescription CreateCopy()
    {
        MediaDescription MdCopy = MediaDescription.ParseMediaDescription(this.ToString().
            Replace("m=", "").Replace("\r\n", ""));
        return MdCopy;
    }

    /// <summary>
    /// Constructs a new MediaDescription object from parameters provided by the caller.
    /// </summary>
    /// <param name="strMediaType">Media type. Ex: "audio" or "video"</param>
    /// <param name="iPort">Port number that the session will occur on.</param>
    /// <param name="strFormatNumbers">Media format for the session.</param>
    public MediaDescription(string strMediaType, int iPort, string strFormatNumbers)
    {
        MediaType = strMediaType;
        Port = iPort;
        Transport = "TCP";

        if (string.IsNullOrEmpty(strFormatNumbers) == true)
        {	// Error, but default to something
            MediaFormatNumbers.Add("0");
            return;
        }

        string[] Nums = strFormatNumbers.Split(" ", StringSplitOptions.RemoveEmptyEntries);
        if (Nums == null || Nums.Length == 0)
            {   // Error, but default to something
                MediaFormatNumbers.Add("0");
                return;
            }

        foreach (string Num in Nums)
            MediaFormatNumbers.Add(Num);
    }

    /// <summary>
    /// Gets the attribute value for the specified attribute name.
    /// </summary>
    /// <param name="strAttributeName">Name of the attribute to search for.
    /// </param>
    /// <returns>A string containing the attribute value. Returns an empty
    /// string if the attribute name was not found.</returns>
    public string GetAttributeValue(string strAttributeName)
    {
        string strRetVal = "";
        if (Attributes.Count > 0)
        {
            foreach (SdpAttribute Sa in Attributes)
            {
                if (Sa.Attribute == strAttributeName)
                {
                    strRetVal = Sa.Value;
                    break;
                }
            }
        }

        return strRetVal;
    }

    /// <summary>
    /// Returns the SdpAttribute object for a named attribute.
    /// </summary>
    /// <param name="strAttr">Name of the attribute to search for.</param>
    /// <returns>Returns a SdpAttribute for the named attribute if it is found or null if the named
    /// attribute is not present.</returns>
    public SdpAttribute GetNamedAttribute(String strAttr)
    {
        SdpAttribute RetVal = null;
        foreach (SdpAttribute Sa in Attributes)
        {
            if (Sa.Attribute == strAttr)
            {
                RetVal = Sa;
                break;
            }
        } // end foreach

        return RetVal;
    }

    /// <summary>
    /// Gets a list of SdpAttributes for an attribute name.
    /// </summary>
    /// <param name="strAttr">Attribute name.</param>
    /// <returns>Returns a list of SdpAttribute object. The return value will never be null but will be
    /// empty if there are no attributes with a name that matches the strAttr parameter.</returns>
    public List<SdpAttribute> GetNamedAttributes(string strAttr)
    {
        List<SdpAttribute> Attrs = new List<SdpAttribute>();
        foreach (SdpAttribute Sa in Attributes)
        {
            if (Sa.Attribute == strAttr)
                Attrs.Add(Sa);
        }

        return Attrs;
    }

    /// <summary>
    /// Removes all instances of a named SDP attribute.
    /// </summary>
    /// <param name="Attr">Name of the SDP attribute to remove.</param>
    public void RemoveNamedAttributes(string Attr)
    {
        List<SdpAttribute> AttrsToRemove = new List<SdpAttribute>();
        foreach (SdpAttribute Sa in Attributes)
        {
            if (Sa.Attribute == Attr)
                AttrsToRemove.Add(Sa);
        }

        foreach (SdpAttribute RemAttr in AttrsToRemove)
            Attributes.Remove(RemAttr);
    }

    /// <summary>
    /// Determines if its necessary to use DTLS-SRTP to negotiate encryption keys and algorithms.
    /// </summary>
    /// <param name="Sdp">Media session that this MediaDescription belong to.</param>
    /// <param name="SetType">SetupType to use in the answer to the offered SetupType.</param>
    /// <returns>Returns true if DTLS-SRTP is required.</returns>
    public bool UsingDtlsSrtp(Sdp Sdp, out SetupType SetType)
    {
        bool IsDtlsSrtp = false;
        SetType = SetupType.active;
        if (Transport != null && Transport.ToUpper().IndexOf("UDP/TLS") >= 0 ||
            (Transport.ToUpper().IndexOf("RTP/SAVP") >= 0 && GetNamedAttribute("fingerprint") != null))
        {   // Encryption using DTLS-SRTP has been offerred.
            IsDtlsSrtp = true;
            SdpAttribute SetupAttr = GetNamedAttribute("setup");
            if (SetupAttr == null)
                SetupAttr = GetNamedAttribute("setup");

            if (SetupAttr != null)
            {
                if (SetupAttr.Value == "actpass" || SetupAttr.Value == "passive")
                    // Become the active element
                    SetType = SetupType.active;
                else
                    SetType = SetupType.passive;
            }
        }

        return IsDtlsSrtp;
    }

    /// <summary>
    /// Adds a a=setup:xxx attribute.
    /// </summary>
    /// <param name="SetType">Specifies the role (active, passive, etc.)</param>
    public void AddSetupAttribute(SetupType SetType)
    {
        SdpAttribute Sa = GetNamedAttribute("setup");
        if (Sa != null)
            Sa.Value = SetType.ToString();
        else
        {
            Sa = new SdpAttribute("setup", SetType.ToString());
            Attributes.Add(Sa);
        }
    }

    /// <summary>
    /// Determines if the media description has an attribute that specifies the media state. The media
    /// state attributes are inactive, sendrecv, recvonly and sendonly.
    /// </summary>
    /// <returns>Returns true if there is a media state attribute, else returns false.</returns>
    public bool HasMediaStateAttribute()
    {
        bool RetVal = false;
        foreach (SdpAttribute Sa in Attributes)
        {
            if (Sa.Attribute != null && (Sa.Attribute == "inactive" ||
                Sa.Attribute == "sendrecv" || Sa.Attribute == "recvonly" ||
                Sa.Attribute == "sendonly"))
            {
                RetVal = true;
                break;
            }
        }

        return RetVal;
    }

    /// <summary>
    /// Retrieves the payload type for DTMF telephone events.
    /// </summary>
    /// <param name="PayloadType">Output is set to the value for the first rtpmap attribute for a
    /// telephone-event if the return value is true. This output parameter will be set to 0 if the return
    /// value of this function is false. </param>
    /// <returns>True if the rtpmap attribute for a telephone-event is found. A return value of false
    /// indicates that the telephone-event rtpmap attrubute could not be found.</returns>
    public bool GetTelephoneEventPayloadType(out int PayloadType)
    {
        bool Success = false;
        PayloadType = 0;
        foreach (SdpAttribute Sa in Attributes)
        {
            if (Sa.Attribute != null && Sa.Attribute == "rtpmap" && Sa.Value != null)
            {
                foreach (KeyValuePair<string, string> Kvp in Sa.Params)
                {
                    if (Kvp.Key.IndexOf("telephone-event") >= 0)
                    {
                        Success = int.TryParse(Sa.Value, out PayloadType);
                        break;
                    }
                } // end foreach

                if (Success == true)
                    break;
            }
        } // end foreach

        return Success;
    }

    /// <summary>
    /// Finds the rtpmap attribute for the specified codec name.
    /// </summary>
    /// <param name="strCodecName">Name of the codec to search for, such as PCMU or H264.</param>
    /// <returns>Returns the SdpAttribute object if found or null if the specified codec was not found.
    /// </returns>
    public SdpAttribute GetRtpmapForCodecType(string strCodecName)
    {
        SdpAttribute RetVal = null;

        foreach (SdpAttribute Sa in Attributes)
        {
            if (Sa.Attribute == "rtpmap")
            {
                if (Sa.Params.ContainsKey(strCodecName.ToLower()) == true ||
                        Sa.Params.ContainsKey(strCodecName.ToUpper()) == true)
                {
                    RetVal = Sa;
                    break;
                }
            }
        } // end for each

        return RetVal;
    }

    /// <summary>
    /// Finds the fmtp attribute for the specified media format number.
    /// </summary>
    /// <param name="strFormatNumber">String representation of the media format number to look for.</param>
    /// <returns>Returns the SdpAttribute object or null if its not found.</returns>
    public SdpAttribute GetFmtpForFormatNumber(string strFormatNumber)
    {
        SdpAttribute RetVal = null;
        foreach (SdpAttribute Sa in Attributes)
        {
            if (Sa.Attribute == "fmtp")
            {
                if (Sa.Value == strFormatNumber)
                {
                    RetVal = Sa;
                    break;
                }
            }
        } // end foreach

        return RetVal;
    }

    /// <summary>
    /// Builds an MediaDescription that contains only the offerred H264 codec attributes.
    /// </summary>
    /// <param name="Port">RTP port number to use.</param>
    /// <returns>Returns a new MediaDescription object or null if the video media block does not contain
    /// an offer of H264 media.</returns>
    public MediaDescription BuildH264AnswerSmd(int Port)
    {
        MediaDescription AnsSmd = null;

        string H264FormatNumber = null;
        SdpAttribute RtpMap = null;
        foreach (string str in MediaFormatNumbers)
        {
            foreach(SdpAttribute Attr in Attributes)
            {
                if (Attr.Attribute == "rtpmap" && 
                    (Attr.ToString().IndexOf("H264") >= 0 || 
                    Attr.ToString().IndexOf("h264") >= 0))
                    {
                    RtpMap = Attr;
                    H264FormatNumber = str;
                }
            }
        }

        if (RtpMap == null)
            return null;    // H264 was not offered

        AnsSmd = new MediaDescription("video", Port, H264FormatNumber);
        AnsSmd.Transport = "RTP/AVP";
        AnsSmd.Attributes.Add(RtpMap);
        SdpAttribute FmtpAttr = GetFmtpForFormatNumber(H264FormatNumber);
        if (FmtpAttr != null)
            AnsSmd.Attributes.Add(FmtpAttr);

        return AnsSmd;
    }

    /// <summary>
    /// Converts the MediaDescription object to a string.
    /// </summary>
    /// <returns>The string format is: "m=MediaType Port Transport MediaFormat(s)"</returns>
    public override string ToString()
    {
        StringBuilder Sb = new StringBuilder(1024);
        Sb.AppendFormat("m={0} {1} {2}", MediaType, Port.ToString(),
            Transport);
        foreach (string strFormat in MediaFormatNumbers)
            Sb.AppendFormat(" {0}", strFormat);

        Sb.Append("\r\n");

        if (ConnectionData != null)
            Sb.Append(ConnectionData.ToString());

        if (string.IsNullOrEmpty(Bandwidth) == false)
            Sb.AppendFormat("b={0}\r\n", Bandwidth);

        foreach (SdpAttribute Sa in Attributes)
        {
            if (Sa != null)
                Sb.Append(Sa.ToString());
        } // end for each

        return Sb.ToString();
    }

    /// <summary>
    /// Creates a basic MediaDescription object for offerring audio media.
    /// </summary>
    /// <param name="Port">Specifies the UDP port number that audio will be sent and received on</param>
    /// <returns>Returns a new MediaDescription object.</returns>
    public static MediaDescription CreateAudioSmd(int Port)
    {
        MediaDescription AudSmd = new MediaDescription();
        AudSmd.MediaType = "audio";
        AudSmd.Port = Port;
        AudSmd.Transport = "RTP/AVP";
        AudSmd.MediaFormatNumbers = new List<string>() { "0", "101" };
        AudSmd.Attributes.Add(new SdpAttribute("fmtp", "101 0-15"));
        AudSmd.Attributes.Add(new SdpAttribute("rtpmap", "0 PCMU/8000"));
        AudSmd.Attributes.Add(new SdpAttribute("rtpmap", "101 telephone-event/8000"));

        return AudSmd;
    }

    /// <summary>
    /// Builds a basic MediaDescription object for offering H.264 video using the Basic Level 1 video
    /// profile.
    /// </summary>
    /// <param name="Port"></param>
    /// <returns></returns>
    public static MediaDescription CreateVideoSmd(int Port)
    {
        MediaDescription VidSmd = new MediaDescription("video", Port, "96");
        VidSmd.Transport = "RTP/AVP";
        VidSmd.Attributes.Add(SdpAttribute.ParseSdpAttribute("rtpmap:96 H264/90000"));
        VidSmd.Attributes.Add(SdpAttribute.ParseSdpAttribute("fmtp:96 " + "profile-level-id=42801f"));

        return VidSmd;
    }
}
