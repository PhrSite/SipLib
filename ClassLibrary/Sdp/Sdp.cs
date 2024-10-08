/////////////////////////////////////////////////////////////////////////////////////
//	File:	Sdp.cs													16 Nov 22 PHR
//
//  Revised: 6 Aug 24 PHR
//              -- Removed public static Sdp ParseSDP(List<string> lines)
//              -- Changed public static Sdp ParseSDP(string[] lines) to private
/////////////////////////////////////////////////////////////////////////////////////

// RFC 8866 describes the Session Description Protocol (SDP). RFC 8866 obsoletes RFC 4566.
// See RFC 3266 for a description of how to handle IPv6 addresses in the SDP
// See RFC 5118 for the SIP Torture tests using IPv6
    
using System.Text;
using System.Net;
using SipLib.RtpCrypto;
using SipLib.Core;
using SipLib.Msrp;

namespace SipLib.Sdp;

/// <summary>
/// Class for processing the Session Description Protocol message contents of a SIP message. See RFC 8866. 
/// </summary>
public class Sdp
{
    /// <summary>
    /// Contains the version number (v) of the SDP protocol. This is expected to be always 0. See Section 5.1
    /// of RFC 8866.
    /// </summary>
    /// <value></value>
    public int Version { get; private set; } = 0;
    /// <summary>
    /// Contains the origin information for the "o" parameter. See Section 5.2 of RFC 8866.
    /// </summary>
    /// <value></value>
    public Origin? Origin { get; set; } = null;
    /// <summary>
    /// Contains the Session Name for the "s" type. parameter Section 5.3 of RFC 8866.
    /// </summary>
    /// <value></value>
    public string SessionName { get; set; } = "";

    /// <summary>
    /// Contains the session information "i" parameter. See Section 5.4 of RFC 8866. This parameter is optional.
    /// </summary>
    /// <value></value>
    public string SessionInformation { get; set; } = "";

    /// <summary>
    /// Contains a URI to more information about the session. This is the "u" parameter. See Section 5.5 of
    /// RFC 8866. This parameter is optional.
    /// </summary>
    /// <value></value>
    public string Uri { get; set; } = "";

    /// <summary>
    /// Contains the e-mail parameter (e) for the session. See Section 5.6 of RFC 8866. This parameter is
    /// optional.
    /// </summary>
    /// <value></value>
    public string Email { get; set; } = "";

    /// <summary>
    /// Contains the phone number (p) parameter for the session. See Section 5.6 of RFC 8866. This parameter
    /// is optional.
    /// </summary>
    /// <value></value>
    public string PhoneNumber { get; set; } = "";

    /// <summary>
    /// Bandwidth parameter (b) for the entire session. See Section 5.8 of RFC 8866.
    /// This parameter is optional. Treating it as a simple string.
    /// </summary>
    /// <value></value>
    public string Bandwidth { get; set; } = "";

    /// <summary>
    /// Timing (t) parameter. See Section 5.9 of RFC 8866.
    /// </summary>
    /// <value></value>
    public string Timing { get; set; } = "0 0";

    /// <summary>
    /// Contains a list of all of the different types of media for a call.
    /// </summary>
    /// <value></value>
    public List<MediaDescription> Media { get; set; } = new List<MediaDescription>();

    /// <summary>
    /// Contains the connecton data for the call.
    /// </summary>
    /// <value></value>
    public ConnectionData? ConnectionData { get; set; } = null;

    /// <summary>
    /// Contains the attributes for this media session.
    /// </summary>
    /// <value></value>
    public List<SdpAttribute> Attributes { get; set; } = new List<SdpAttribute>();

    /// <summary>
    /// Constructs a new Sdp object. Use this constructor when building a new SDP block to send.
    /// </summary>
    private Sdp()
    {
    }

    private static Random Rnd = new Random();

    /// <summary>
    /// Constructs a new Sdp and initializes the Origin (o=), ConnectionData (c=), session name (s=) and
    /// an empty media list.
    /// </summary>
    /// <param name="LocalIp">Local IP address</param>
    /// <param name="UaName">User agent or server name</param>
    public Sdp(IPAddress LocalIp, string UaName)
    {
        Origin = new Origin(UaName, LocalIp);
        ConnectionData = new ConnectionData(LocalIp);
        SessionName = UaName + "_" + Rnd.Next().ToString();
    }

    /// <summary>
    /// Parses the SDP contained in an array of strings.
    /// </summary>
    /// <param name="Lines">Each string in the array contains one line in the SDP</param>
    /// <returns>Returns an Sdp object</returns>
    // <exception cref="ArgumentException">Thrown if an invalid argument is detected</exception>
    // <exception cref="Exception">Thrown if an unexpected error occurs</exception>
    private static Sdp ParseSDP(string[] Lines)
    {
        if (Lines == null || Lines.Length == 0)
            throw new ArgumentException("Lines is null empty", nameof(Lines));

        Sdp sdp = new Sdp();
        try
        {
            foreach (string str in Lines)
            {
                ProcessLine(str, sdp);
            }

            sdp.ApplySessionAttributesToEachMediaType();
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (Exception)
        {
            throw new Exception("An unexpected SDP parsing error occurred");
        }

        return sdp;
    }

    /// <summary>
    /// Processes a single SDP line.
    /// </summary>
    /// <param name="str">Input line to parse and process</param>
    /// <param name="sdp">Sdp object to add the line to</param>
    private static void ProcessLine(string str, Sdp sdp)
    {
        string strType;
        string strValue;

        try
        {
            if (str.Length >= 3)
            {
                strType = str.Substring(0, 1);
                strValue = sdp.GetValueOfNameValuePair(str, '=');

                switch (strType)
                {
                    case "v":
                        sdp.Version = Convert.ToInt32(strValue);
                        break;
                    case "o":
                        sdp.Origin = Origin.ParseOrigin(strValue);
                        break;
                    case "t":
                        sdp.Timing = strValue!;
                        break;
                    case "s":
                        sdp.SessionName = strValue!;
                        break;
                    case "i":
                        sdp.SessionInformation = strValue!;
                        break;
                    case "u":
                        sdp.Uri = strValue!;
                        break;
                    case "e":
                        sdp.Email = strValue!;
                        break;
                    case "p":
                        sdp.PhoneNumber = strValue!;
                        break;
                    case "b":
                        if (sdp.Media.Count == 0)
                            sdp.Bandwidth = strValue!;
                        else
                        {
                            if (strValue.StartsWith("CT:") == true)
                                // CT indicates the session's total bandwidth
                                sdp.Bandwidth = strValue;
                            else
                                // In a media block, then add the b= to that media block
                                sdp.Media[sdp.Media.Count - 1].Bandwidth = strValue;
                        }
                        break;
                    case "m":
                        sdp.Media.Add(MediaDescription.ParseMediaDescriptionLine(strValue));
                        break;
                    case "c":
                        if (sdp.Media.Count == 0)
                            sdp.ConnectionData = ConnectionData.ParseConnectionData(strValue);
                        else
                            // If in a media block then add the connection data (c=) to that media block.
                            sdp.Media[sdp.Media.Count - 1].ConnectionData =
                                ConnectionData.ParseConnectionData(strValue);
                        break;
                    case "a":
                        SdpAttribute sdpAttr = SdpAttribute.ParseSdpAttribute(strValue);
                        if (sdpAttr != null)
                        {
                            if (sdp.Media.Count > 0)
                            {   // In a m= block so attributes belong to that media block
                                if (sdpAttr != null)
                                {
                                    if (sdpAttr.Attribute == "rtpmap")
                                    {   // rtpmap attributes only apply to the media block, not the session.
                                        // Pass only the rtpmap attribute value to ParseRtpMap.
                                        RtpMapAttribute rtpMap = RtpMapAttribute.ParseRtpMap(strValue.Replace(
                                            "rtpmap:", "").Trim());
                                        if (rtpMap != null)
                                            sdp.Media[sdp.Media.Count - 1].RtpMapAttributes.Add(rtpMap);
                                    }
                                    else
                                        sdp.Media[sdp.Media.Count - 1].Attributes.Add(sdpAttr);
                                }
                            }
                            else
                                // No media blocks yet so attributes belong to the session
                                sdp.Attributes.Add(sdpAttr);
                        }
                        break;
                } // end switch strType
            }
        }
        catch (ArgumentException)
        {
            throw; 
        }
        catch (Exception)
        {
            throw new Exception("Unexpected error in Sdp.ParseLine()");
        }
    }

    /// <summary>
    /// Parses the SDP contained in a string
    /// </summary>
    /// <param name="strSdp">Contains a SDP SIP body part.</param>
    /// <returns>Returns a new Sdp object.</returns>
    /// <exception cref="ArgumentException">Thrown if an invalid argument is detected</exception>
    /// <exception cref="Exception">Thrown if an unexpected error occurs</exception>
    public static Sdp ParseSDP(string strSdp)
    {
        if (string.IsNullOrEmpty(strSdp) == true)
            throw new ArgumentException("The input string is null or empty", nameof(strSdp));

        string[] Lines = strSdp.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Sdp sdp = null;
        try
        {
            sdp = ParseSDP(Lines);
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (Exception)
        {
            throw new Exception("Unexpected error in ParseSDP(string strSdp");
        }

        return sdp;
    }	

    /// <summary>
    /// Creates a deep copy (i.e. by-value) of this object.
    /// </summary>
    /// <returns>Returns a new Sdp object.</returns>
    public Sdp CreateCopy()
    {
        return Sdp.ParseSDP(ToString());
    }

    /// <summary>
    /// If the session level contains a media state attribute and the media description does not contain
    /// a media state attribute, then add that attribute to the media description.
    /// </summary>
    private void ApplySessionAttributesToEachMediaType()
    {
        foreach (SdpAttribute Sa in Attributes)
        {
            if (Sa.Attribute != null && (Sa.Attribute == "inactive" ||
                Sa.Attribute == "sendrecv" || Sa.Attribute == "recvonly" ||
                Sa.Attribute == "sendonly"))
            {	// Only interested in media state attributes
                foreach (MediaDescription Sm in Media)
                {
                    if (Sm.HasMediaStateAttribute() == false)
                    {	// The attribute Sa is in the session level but not
                        // in the media level so add it to the media level of
                        // each media type.
                        Sm.Attributes.Add(Sa);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Gets the MediaDescription object for the specified type of media.
    /// </summary>
    /// <param name="strType">Type of media such as audio, video, text, etc..</param>
    /// <returns>Returns the SepMediaDescription object if the specified type of media is present or null if
    /// it is not.</returns>
    public MediaDescription? GetMediaType(string strType)
    {
        MediaDescription RetVal = null;
        foreach (MediaDescription Med in Media)
        {
            if (Med.MediaType == strType)
            {
                RetVal = Med;
                break;
            }
        }

        return RetVal;
    }

    /// <summary>
    /// Gets a list of MediaDescription objects in the SDP that have the same type of media.
    /// </summary>
    /// <param name="strType">Media type. Must be one of "audio", "video", "text" or "message"</param>
    /// <returns>Returns a list of MediaDescription objects of the same media type. The return value
    /// will not be null.</returns>
    public List<MediaDescription> GetMediaTypeList(string strType)
    {
        List<MediaDescription> list = new List<MediaDescription>();
        foreach (MediaDescription Med in Media)
        {
            if (Med.MediaType == strType)
                list.Add(Med);
        }

        return list;
    }

    /// <summary>
    /// Gets the index of a specified media type in the Media list.
    /// </summary>
    /// <param name="strType">Type of media to look for. Must be "audio", "video", "text" or "message"</param>
    /// <returns>Returns the index of the media type in the Media list. Returns -1 if the media type is not
    /// present.</returns>
    public int GetMediaTypeIndex(string strType)
    {
        int Idx = -1;

        for (int i=0; i < Media.Count; i++)
        {
            if (Media[i].MediaType == strType)
            {
                Idx = i;
                break;
            }
        }

        return Idx;
    }

    /// <summary>
    /// Checks to see if the offered SDP has media other that "audio" that is a known media type.
    /// </summary>
    /// <returns>Returns true if the SDP contains media other than "audio" or false does not.</returns>
    public bool HasMultiMedia()
    {
        bool RetVal = false;
        foreach (MediaDescription Smd in Media)
        {
            if (Smd.MediaType == "video" || Smd.MediaType == "text" || Smd.MediaType == "message")
            {
                RetVal = true;
                break;
            }
        } // end foreach

        return RetVal;
    }

    /// <summary>
    /// Retrieves the IP address and the port number for the audio media in this SDP.
    /// </summary>
    /// <param name="strIpAddr">IP address to send the audio to. This output will contain the IP address
    /// if this method returns true or it will be set to null if this method returns false.</param>
    /// <param name="Port">Port number to send the audio to. This output will contain the port number to
    /// if this method returns true or it will be set to 0 if this function returns false.</param>
    /// <returns>Returns true if the audio media type is present and valid. Else returns null.</returns>
    public bool GetAudioConnectionData(ref string? strIpAddr, ref int Port)
    {
        strIpAddr = null;
        Port = 0;

        MediaDescription Md = GetMediaType("audio");
        if (Md == null)
            return false;

        ConnectionData ConnData = Md.ConnectionData;
        if (ConnData == null || ConnData.Address == null)
            ConnData = ConnectionData;

        if (ConnData == null || ConnData.Address == null)
            return false;

        if (ConnData.Address == null)
            return false;

        strIpAddr = ConnData.Address.ToString();
        Port = Md.Port;

        return true;
    }

    private string? GetValueOfNameValuePair(string Input, char Sep)
    {
        if (string.IsNullOrEmpty(Input) == true)
            return null;

        int Idx = Input.IndexOf(Sep);
        if (Idx < 0 || Idx == Input.Length - 1)
            return null;
        else
            return Input.Substring(Idx + 1).TrimStart();
    }

    /// <summary>
    /// Returns the SdpAttribute object for a named attribute for the entire SDP at the session level.
    /// </summary>
    /// <param name="strAttr">Name of the attribute to search for.</param>
    /// <returns>Returns a SdpAttribute for the named attribute if it is found or null if the named attribute
    /// is not present.</returns>
    public SdpAttribute? GetNamedAttribute(string strAttr)
    {
        SdpAttribute RetVal = null;
        foreach (SdpAttribute Sa in Attributes)
        {
            if (Sa.Attribute == strAttr)
            {
                RetVal = Sa;
                break;
            }
        }

        return RetVal;
    }

    /// <summary>
    /// Removes all instances of a named SDP attribute.
    /// </summary>
    /// <param name="strAttr">Name of the SDP attribute to remove.</param>
    public void RemoveNamedAttribute(string strAttr)
    {
        List<SdpAttribute> AttrsToRemove = new List<SdpAttribute>();
        foreach (SdpAttribute Sa in Attributes)
        {
            if (Sa.Attribute == strAttr)
                AttrsToRemove.Add(Sa);
        }

        foreach (SdpAttribute RemAttr in AttrsToRemove)
            Attributes.Remove(RemAttr);
    }

    /// <summary>
    /// Converts the Sdp object to a string.
    /// </summary>
    /// <returns>The string is formatted so that it can be appended to a SIP
    /// message as the SDP contents.</returns>
    public override string ToString()
    {
        StringBuilder Sb = new StringBuilder();

        Sb.AppendFormat("v={0}\r\n", Version);
        Sb.Append(Origin.ToString());

        if (string.IsNullOrEmpty(SessionName) == false)
            Sb.AppendFormat("s={0}\r\n", SessionName);

        if (string.IsNullOrEmpty(SessionInformation) == false)
            Sb.AppendFormat("i={0}\r\n", SessionInformation);

        if (string.IsNullOrEmpty(Uri) == false)
            Sb.AppendFormat("u={0}\r\n", Uri);

            if (string.IsNullOrEmpty(Email) == false)
                Sb.AppendFormat("e={0}\r\n", Email);

        if (string.IsNullOrEmpty(Bandwidth) == false)
            Sb.AppendFormat("b={0}\r\n", Bandwidth);

        if (string.IsNullOrEmpty(PhoneNumber) == false)
            Sb.AppendFormat("p={0}\r\n", PhoneNumber);

        if (ConnectionData != null)
            Sb.Append(ConnectionData.ToString());

        foreach (SdpAttribute Sa in Attributes)
        {
            if (Sa != null)
                Sb.Append(Sa.ToString());
        } // end for each

        if (string.IsNullOrEmpty(Timing) == true)
            Sb.Append("t=0 0\r\n");
        else
            Sb.AppendFormat("t={0}\r\n", Timing);

        foreach (MediaDescription Med in Media)
        {
            Sb.Append(Med.ToString());
        }

        return Sb.ToString();
    }

    /// <summary>
    /// Gets the destination IP address for media given the offered SDP and the offered SDP Media Description
    /// for that media. The destination IP address may be specified at the session level or the media level.
    /// </summary>
    /// <param name="Sdp">The entire SDP block that was offered.</param>
    /// <param name="Smd">The SDP Media Description block for the media to get the IP address for.</param>
    /// <returns>Returns an IPAddress. Returns null if there was no address specified in the session or
    /// media levels.</returns>
    public static IPAddress? GetMediaIPAddr(Sdp Sdp, MediaDescription Smd)
    {
        IPAddress RetVal = null;
        if (Smd.ConnectionData != null)
            RetVal = Smd.ConnectionData.Address;
        else if (Sdp.ConnectionData != null)
            RetVal = Sdp.ConnectionData.Address;

        return RetVal;
    }

    /// <summary>
    /// Gets the IPEndPoint for media given the offered SDP and the offered SDP Media Description for that
    /// media. The destination IP address may be specified at the session level or the media level.
    /// </summary>
    /// <param name="Sdp">The entire SDP block that was offered.</param>
    /// <param name="Md">The SDP Media Description block for the media to get the IP address for.</param>
    /// <returns>Returns an IPAddress. Returns null if there was no address specified in the session or
    /// media levels.</returns>
    public static IPEndPoint? GetMediaEndPoint(Sdp Sdp, MediaDescription Md)
    {
        IPAddress MediaIpAddr = Sdp.GetMediaIPAddr(Sdp, Md);
        if (MediaIpAddr == null)
            return null;
        else 
            return new IPEndPoint(MediaIpAddr, Md.Port);
    }

    /// <summary>
    /// Gets the media direction attribute for a media type. The media direction may be specified
    /// at the session level or at the media level. The media level has priority.
    /// </summary>
    /// <param name="sdp">Session data</param>
    /// <param name="md">Media description. Must be one of the MediaDescription objects in the
    /// SDP.</param>
    /// <returns>Returns the media direction if specified at the media level or the session level.
    /// Returns MediaDirectionEnum.sendrecv if the direction is not specified at either level.</returns>
    public static MediaDirectionEnum GetMediaDirection(Sdp sdp, MediaDescription md)
    {
        string? dir = GetMediaDirectionAttribute(md.Attributes);
        if (string.IsNullOrEmpty(dir) == false)
            return (MediaDirectionEnum) Enum.Parse(typeof(MediaDirectionEnum), dir);

        dir = GetMediaDirectionAttribute(sdp.Attributes);
        if (string.IsNullOrEmpty(dir) == false)
            return (MediaDirectionEnum)Enum.Parse(typeof(MediaDirectionEnum), dir);
        else
            return MediaDirectionEnum.sendrecv;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="AttrList"></param>
    /// <returns></returns>
    internal static string? GetMediaDirectionAttribute(List<SdpAttribute> AttrList)
    {
        string? dir = null;
        foreach (SdpAttribute attr in AttrList)
        {
            if (attr.Attribute == "recvonly" || attr.Attribute == "sendrecv" || attr.Attribute == "sendonly" ||
                attr.Attribute == "inactive")
            {
                dir = attr.Attribute;
                break;
            }
        }

        return dir;
    }

    /// <summary>
    /// Gets or sets the media direction attribute for the session level.
    /// </summary>
    public MediaDirectionEnum MediaDirection
    {
        set
        {
            Sdp.ClearMediaDirection(Attributes);
            Attributes.Add(new SdpAttribute(value.ToString(), null));
        }
        get
        {
            string? dir = Sdp.GetMediaDirectionAttribute(Attributes);
            if (string.IsNullOrEmpty(dir) == false)
                return (MediaDirectionEnum)Enum.Parse(typeof(MediaDirectionEnum), dir);
            else
                return MediaDirectionEnum.sendrecv;
        }
    }

    internal static void ClearMediaDirection(List<SdpAttribute> AttrList)
    {
        List<SdpAttribute> removeList = new List<SdpAttribute>();
        foreach (SdpAttribute attr in AttrList)
        {
            if (attr.Attribute == "recvonly" || attr.Attribute == "sendrecv" || attr.Attribute == "sendonly" ||
                attr.Attribute == "inactive")
                removeList.Add(attr);
        }

        foreach (SdpAttribute attr in removeList)
        {
            AttrList.Remove(attr);
        }
    }

    /// <summary>
    /// Builds an Sdp object to send as the answered Sdp in responsed to the offered Sdp
    /// </summary>
    /// <param name="OfferedSdp">SDP that was offered</param>
    /// <param name="address">IP address to be used for transport of all media. This address is used in the
    /// "c=" line of the SDP.</param>
    /// <param name="AnswerSettings">Settings that determine how to build the answered SDP</param>
    /// <returns>Returns the SDP to send to the client that offered the SDP</returns>
    public static Sdp BuildAnswerSdp(Sdp OfferedSdp, IPAddress address, SdpAnswerSettings AnswerSettings)
    {
        Sdp AnswerSdp = new Sdp(address, AnswerSettings.UserName);
        foreach (MediaDescription Md in OfferedSdp.Media)
        {
            switch (Md.MediaType)
            {
                case "audio":
                    AnswerSdp.Media.Add(GetAudioAnswerMediaDescription(Md, AnswerSettings));
                    break;
                case "video":
                    AnswerSdp.Media.Add(GetVideoAnswerMediaDescription(Md, AnswerSettings));
                    break;
                case "text":    // Real Time Text
                    AnswerSdp.Media.Add(GetRttAnswerMediaDescription(Md, AnswerSettings));
                    break;
                case "message": // MSRP
                    AnswerSdp.Media.Add(GetMsrpAnswerMediaDescription(Md, address, AnswerSettings));
                    break;
                default:        // Unknown media type, reject it
                    MediaDescription UnknownMd = new MediaDescription(Md.MediaType, 0, Md.PayloadTypes);
                    AnswerSdp.Media.Add(UnknownMd);
                    break;
            }
        }

        return AnswerSdp;
    }

    private static MediaDescription GetAudioAnswerMediaDescription(MediaDescription OfferedMd, SdpAnswerSettings
        Settings)
    {
        MediaDescription? AnsMd = null;

        if (Settings.EnableAudio == false)
        {
            AnsMd = new MediaDescription("audio", 0, OfferedMd.PayloadTypes);
            return AnsMd;
        }

        RtpMapAttribute? SupportedRma = FindSupportedAudioCodec(OfferedMd, Settings.SupportedAudioCodecs);
        if (SupportedRma == null)
        {   // Could not find a rtpmap attribute for the supported audio codecs. Try using a well known
            // codec number.

            AnsMd = new MediaDescription("audio", 0, OfferedMd.PayloadTypes);
            return AnsMd;    // Error: No supported codecs offerred
        }

        RtpMapAttribute AnsRma = new RtpMapAttribute(SupportedRma.PayloadType, SupportedRma.EncodingName!,
            SupportedRma.ClockRate);

        List<int> PayloadTypes = new List<int>();
        PayloadTypes.Add(AnsRma.PayloadType);
        AnsMd = new MediaDescription(OfferedMd.MediaType, Settings.PortManager.NextAudioPort, PayloadTypes);
        AnsMd.Transport = OfferedMd.Transport;
        AnsMd.RtpMapAttributes.Add(AnsRma);

        if (SupportedRma.EncodingName == "AMR-WB")
        {
            SdpAttribute Fmtp = OfferedMd.GetFmtpForFormatNumber(AnsRma.PayloadType.ToString());
            if (Fmtp != null)
                AnsMd.Attributes.Add(Fmtp);
        }

        // TODO: Get any fmtp attribute(s) for the media codec selected

        // Test to see if telephone-event is offerred. If it is then answer with it
        RtpMapAttribute? OfferedTelephoneEvent = OfferedMd.GetRtpMapForCodecType("telephone-event");
        if (OfferedTelephoneEvent != null)
        {
            AnsMd.PayloadTypes.Add(OfferedTelephoneEvent.PayloadType);
            AnsMd.RtpMapAttributes.Add(OfferedTelephoneEvent);
            SdpAttribute? TelFmtpAttr = OfferedMd.GetFmtpForFormatNumber(OfferedTelephoneEvent.
                PayloadType.ToString());
            if (TelFmtpAttr != null)
                AnsMd.Attributes.Add(TelFmtpAttr);
        }

        SdpAttribute? LabelAttr = OfferedMd.GetNamedAttribute("label");
        if (LabelAttr != null)
            AnsMd.Attributes.Add(LabelAttr);

        HandleOfferedEncryption(OfferedMd, AnsMd, Settings);

        return AnsMd;
    }

    private static MediaDescription GetVideoAnswerMediaDescription(MediaDescription OfferedMd, SdpAnswerSettings Settings)
    {
        MediaDescription? AnsMd = null;
        if (Settings.EnableVideo == false)
        {
            AnsMd = new MediaDescription("video", 0, OfferedMd.PayloadTypes);
            return AnsMd;
        }

        RtpMapAttribute? SupportedRma = FindSupportedCodec(OfferedMd, Settings.SupportedVideoCodecs);
        if (SupportedRma == null)
        {
            AnsMd = new MediaDescription("video", 0, OfferedMd.PayloadTypes);
            return AnsMd;    // Error: No supported codecs offerred
        }

        RtpMapAttribute AnsRma = new RtpMapAttribute(SupportedRma.PayloadType, SupportedRma.EncodingName!,
            SupportedRma.ClockRate);
        List<int> PayloadTypes = new List<int>();
        PayloadTypes.Add(AnsRma.PayloadType);
        AnsMd = new MediaDescription(OfferedMd.MediaType, Settings.PortManager.NextVideoPort, PayloadTypes);
        AnsMd.RtpMapAttributes.Add(AnsRma);
        AnsMd.Transport = OfferedMd.Transport;

        SdpAttribute? VideoFmtpAttr = OfferedMd.GetFmtpForFormatNumber(SupportedRma.PayloadType.ToString());
        if (VideoFmtpAttr != null)
            AnsMd.Attributes.Add(VideoFmtpAttr);

        SdpAttribute? LabelAttr = OfferedMd.GetNamedAttribute("label");
        if (LabelAttr != null)
            AnsMd.Attributes.Add(LabelAttr);

        HandleOfferedEncryption(OfferedMd, AnsMd, Settings);

        return AnsMd;
    }

    private static MediaDescription GetRttAnswerMediaDescription(MediaDescription OfferedMd, SdpAnswerSettings 
        Settings)
    {
        MediaDescription? AnsMd = null;
        if (Settings.EnableRtt == false)
        {
            AnsMd = new MediaDescription("text", 0, OfferedMd.PayloadTypes);
            return AnsMd;
        }

        AnsMd = new MediaDescription("text", Settings.PortManager.NextRttPort, OfferedMd.PayloadTypes);
        AnsMd.Transport = OfferedMd.Transport;
        foreach (int PayloadType in OfferedMd.PayloadTypes)
        {
            RtpMapAttribute? Rma = OfferedMd.GetRtpMapForPayloadType(PayloadType);
            if (Rma != null)
                AnsMd.RtpMapAttributes.Add(Rma);
        }

        RtpMapAttribute? redRtpMapAttribute = OfferedMd.GetRtpMapForCodecType("red");
        if (redRtpMapAttribute != null)
        {
            SdpAttribute? fmtpAttr = OfferedMd.GetFmtpForFormatNumber(redRtpMapAttribute.PayloadType.
                ToString());
            if (fmtpAttr != null)
                AnsMd.Attributes.Add(fmtpAttr);
        }

        SdpAttribute? LabelAttr = OfferedMd.GetNamedAttribute("label");
        if (LabelAttr != null)
            AnsMd.Attributes.Add(LabelAttr);

        SdpAttribute RttMixerAttr = OfferedMd.GetNamedAttribute("rtt-mixer");
        if (RttMixerAttr != null)
            AnsMd.Attributes.Add(new SdpAttribute("rtt-mixer", null!));

        HandleOfferedEncryption(OfferedMd, AnsMd, Settings);

        return AnsMd;
    }

    private static MediaDescription GetMsrpAnswerMediaDescription(MediaDescription OfferedMd, IPAddress Address,
        SdpAnswerSettings Settings)
    {
        MediaDescription? AnsMd = null;
        if (Settings.EnableMsrp == false)
        {
            AnsMd = new MediaDescription("message", 0, new List<int>());
            return AnsMd;
        }

        AnsMd = new MediaDescription("message", Settings.PortManager.NextMsrpPort, OfferedMd.PayloadTypes);
        AnsMd.Transport = OfferedMd.Transport;
        AnsMd.Attributes.Add(new SdpAttribute("accept-types", "message/CPIM text/plain"));

        SdpAttribute? LabelAttr = OfferedMd.GetNamedAttribute("label");
        if (LabelAttr != null)
            AnsMd.Attributes.Add(LabelAttr);

        // Handle the setup attribute if there is one
        SdpAttribute? SetupAttr = OfferedMd.GetNamedAttribute("setup");
        SetupType AnsSetup;
        if (SetupAttr != null)
        {
            if (SetupAttr.Value == "actpass" || SetupAttr.Value == "passive")
                // Become the active element
                AnsSetup = SetupType.active;
            else
                AnsSetup = SetupType.passive;

        }
        else
            AnsSetup = SetupType.passive;

        AnsMd.AddSetupAttribute(AnsSetup);

        SIPSchemesEnum scheme;
        if (AnsMd.Transport.IndexOf("TLS") >= 0)
            scheme = SIPSchemesEnum.msrps;
        else
            scheme = SIPSchemesEnum.msrp;

        // See RFC 7701 Multi-Party Chat using MSRP
        SdpAttribute chatAttr = OfferedMd.GetNamedAttribute("chatroom");
        if (chatAttr != null)
        {
            if (string.IsNullOrEmpty(chatAttr.Value) == false && chatAttr.Value.Contains("private-messages") == true)
                AnsMd.Attributes.Add(new SdpAttribute("chatroom", "private-messages"));
        }

        MsrpUri msrpUri = new MsrpUri(scheme, Settings.UserName, Address, AnsMd.Port);
        AnsMd.Attributes.Add(new SdpAttribute("path", msrpUri.ToString()));

        return AnsMd;
    }

    private static RtpMapAttribute? FindSupportedAudioCodec(MediaDescription OfferedMd, List<string> SupportedCodecNames)
    {
        RtpMapAttribute? Result = null;
        foreach (string CodecName in SupportedCodecNames)
        {
            foreach (RtpMapAttribute rma in OfferedMd.RtpMapAttributes)
            {
                if (rma.EncodingName == CodecName)
                {
                    Result = rma;
                    break;
                }
            }

            if (Result != null)
                break;
        }

        if (Result == null)
        {   // Could not find a supported codec name, try a well-known payload type
            foreach (int pt in OfferedMd.PayloadTypes)
            {
                if (pt == 0)
                {   // G.711 PCMU
                    Result = new RtpMapAttribute(0, "PCMU", 8000);
                }
                else if (pt == 8)
                {   // G.711 PCMA
                    Result = new RtpMapAttribute(8, "PCMA", 8000);
                }
                else if (pt == 9)
                {   // G.722
                    Result = new RtpMapAttribute(9, "G722", 8000);
                }
            }
        }

        return Result;
    }

    private static RtpMapAttribute? FindSupportedCodec(MediaDescription OfferedMd, List<string> SupportedCodecNames)
    {
        RtpMapAttribute? Result = null;
        foreach (string CodecName in SupportedCodecNames)
        {
            foreach (RtpMapAttribute rma in OfferedMd.RtpMapAttributes)
            {
                if (rma.EncodingName == CodecName)
                {
                    Result = rma;
                    break;
                }
            }

            if (Result != null)
                break;
        }

        return Result;
    }

    private static void HandleOfferedEncryption(MediaDescription OfferedMediaDescription, MediaDescription 
        AnswerMediaDescription, SdpAnswerSettings Settings)
    {
        if (OfferedMediaDescription.UsingDtlsSrtp(out SetupType Setup) == true)
        {
            SdpUtils.AddDtlsSrtp(AnswerMediaDescription, Settings.Fingerprint);
            SetupType AnsSetup;
            if (Setup == SetupType.passive)
                AnsSetup = SetupType.active;
            else if (Setup == SetupType.active)
                AnsSetup = SetupType.passive;
            else
                AnsSetup = SetupType.active;
            AnswerMediaDescription.AddSetupAttribute(AnsSetup);
        }
        else if (OfferedMediaDescription.UsingSdesSrtp() == true)
        {   // Negotiate the crypto suite to answer with
            CryptoAttribute offeredCryptoAttribute = GetAnswerCryptoAttribute(OfferedMediaDescription);
            if (offeredCryptoAttribute != null)
            {
                CryptoContext Cc1 = new CryptoContext(offeredCryptoAttribute.CryptoSuite);
                CryptoAttribute Ca1 = Cc1.ToCryptoAttribute();
                Ca1.Tag = offeredCryptoAttribute.Tag;
                AnswerMediaDescription.Attributes.Add(new SdpAttribute("crypto", Ca1.ToString()));
            }
        }
    }

    private static CryptoAttribute? GetAnswerCryptoAttribute(MediaDescription OfferedMediaDescription)
    {
        CryptoAttribute? result = null;
        List<CryptoAttribute> offerredCryptoAttributes = OfferedMediaDescription.GetCryptoAttributes();
        if (offerredCryptoAttributes.Count == 0)
            return null;

        foreach (string cryptoSuite in CryptoSuites.SupportedAlgorithms)
        {
            foreach (CryptoAttribute cryptoAttribute in offerredCryptoAttributes)
            {
                if (cryptoAttribute.CryptoSuite == cryptoSuite)
                {
                    result = cryptoAttribute;
                    break;
                }
            }

            if (result != null)
                break;
        }

        return result;
    }

    /// <summary>
    /// Returns a display name for the associated media type name.
    /// </summary>
    /// <param name="mediaTypeName">Media type name. Should be one of "audio", "video", "message" or
    /// "text".</param>
    /// <returns>Return a name for displaying.</returns>
    public static string MediaTypeToDisplayString(string mediaTypeName)
    {
        if (mediaTypeName == "audio")
            return "Audio";
        else if (mediaTypeName == "video")
            return "Video";
        else if (mediaTypeName == "message")
            return "MSRP";
        else if (mediaTypeName == "text")
            return "RTT";
        else
            return "Unknown";
    }
}
