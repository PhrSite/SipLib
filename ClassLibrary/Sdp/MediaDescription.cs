/////////////////////////////////////////////////////////////////////////////////////
//	File:	MediaDescription.cs                                     16 Nov 22 PHR
//  Revised: 28 Dec 23 PHR
//             -- Added the RtpMapAttributes field
//             -- Removed SdpAttribute GetRtpmapForCodecType(string strCodecName).
//             -- Added RtpMapAttribute GetRtpMapForCodecType(string strCodecName)
/////////////////////////////////////////////////////////////////////////////////////

using SipLib.RtpCrypto;
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
    /// <value></value>
    public string MediaType = "";
    /// <summary>
    /// Specifies the TCP/UDP port number;
    /// </summary>
    /// <value></value>
    public int Port = 0;
    /// <summary>
    /// Specifies the transport mechanism: "TCP", "UDP", "RTP/AVP", etc.
    /// </summary>
    /// <value></value>
    public string Transport = "";
    /// <summary>
    /// Contains a list of payload types for the m= line.
    /// </summary>
    /// <value></value>
    public List<int> PayloadTypes = new List<int>();
    /// <summary>
    /// Contains the attributes for this type of media except the rtpmap attributes, which are stored
    /// in the RtpMapAttributes field.
    /// </summary>
    /// <value></value>
    public List<SdpAttribute> Attributes = new List<SdpAttribute>();
    /// <summary>
    /// Contains the connection data (c=) for this media. If null, then use the ConnectionData of the SDP
    /// session.
    /// </summary>
    /// <value></value>
    public ConnectionData? ConnectionData = null;

    /// <summary>
    /// Specifies the bandwidth limit for the media in kilo-bits per second. This is comes from or generates
    /// a b=AS:Bandwidth line in the media description. An empty or null value indicates that there is no b=
    /// line in the media description.
    /// </summary>
    /// <value></value>
    public string Bandwidth = "";

    /// <summary>
    /// Contains a list of RtpMap objects. Each object corresponds to a a=rtpmap .... line in the media description.
    /// </summary>
    /// <value></value>
    public List<RtpMapAttribute> RtpMapAttributes = new List<RtpMapAttribute>();

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
    public static MediaDescription ParseMediaDescriptionLine(string strMd)
    {
        MediaDescription Md = new MediaDescription();
        string[] Fields = strMd.Split(' ');

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
            throw new ArgumentException("The port number in the media description line is not valid", 
                nameof(strMd));
            
        Md.Transport = Fields[2];

        int i;
        int Pt;
        for (i = 3; i < Fields.Length; i++)
        {
            if (int.TryParse(Fields[i], out Pt) == true)
                Md.PayloadTypes.Add(Pt);
        }

        return Md;
    }
    
    /// <summary>
    /// Parses a string containing a full media description block into a MediaDescription object.
    /// </summary>
    /// <param name="strMd">Input string containing the lines of an SDP media description block. The
    /// first line must contain an m= line</param>
    /// <returns>Returns a new MediaDescription object of successful or null if an error is detected.</returns>
    public static MediaDescription? ParseMediaDescriptionString(string strMd)
    {
        MediaDescription Md = null;
        if (string.IsNullOrEmpty(strMd) == true)
            return null;

        string[] Lines = strMd.Split("\r\n");
        if (Lines == null || Lines.Length < 1)
            return null;

        int index = Lines[0].IndexOf("m=");
        if (index < 0)
            return null;    // Error: no m= line

        Md = ParseMediaDescriptionLine(Lines[index].Substring(2));
        if (Md == null)
            return null;

        string strType;
        string strValue;

        try
        {
            for (int i = 0; i < Lines.Length; i++)
            {
                strType = Lines[i].Substring(0, 1);
                strValue = GetValueOfNameValuePair(Lines[i], '=');
                if (string.IsNullOrEmpty (strType) == true || string.IsNullOrEmpty(strValue) == true)
                    return null;

                switch (strType)
                {
                    case "a":
                        SdpAttribute sdpAttr = SdpAttribute.ParseSdpAttribute(strValue);
                        if (sdpAttr != null)
                        {
                            if (sdpAttr.Attribute == "rtpmap")
                            {
                                RtpMapAttribute rtpMap = RtpMapAttribute.ParseRtpMap(strValue.Replace(
                                    "rtpmap:", "").Trim());
                                if (rtpMap != null)
                                    Md.RtpMapAttributes.Add(rtpMap);
                            }
                            else
                                Md.Attributes.Add(sdpAttr);
                        }
                        break;
                    case "b":
                        Md.Bandwidth = strValue;
                        break;
                    case "c":
                        Md.ConnectionData = ConnectionData.ParseConnectionData(strValue);
                        break;
                }
            }
        }
        catch (Exception)
        {
            return null;
        }
        return Md;
    }

    private static string? GetValueOfNameValuePair(string Input, char Sep)
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
    /// Creates a deep copy of this object. This only copies the m= line.
    /// </summary>
    /// <returns>Returns a new object with a copy of each member variable in this object.</returns>
    public MediaDescription CreateCopy()
    {
        MediaDescription MdCopy = MediaDescription.ParseMediaDescriptionLine(this.ToString().
            Replace("m=", "").Replace("\r\n", ""));
        return MdCopy;
    }

    /// <summary>
    /// Constructs a new MediaDescription object from parameters provided by the caller.
    /// </summary>
    /// <param name="strMediaType">Media type. Ex: "audio" or "video"</param>
    /// <param name="iPort">Port number that the session will occur on.</param>
    /// <param name="payloadTypes">Media payload types.</param>
    public MediaDescription(string strMediaType, int iPort, List<int> payloadTypes)
    {
        MediaType = strMediaType;
        Port = iPort;
        Transport = "RTP/AVP";

        if (payloadTypes.Count == 0)
        {	// Error, but default to something
            PayloadTypes.Add(0);
            return;
        }

        PayloadTypes = payloadTypes;
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
    /// Gets a list of the CryptoAttributes in this MediaDescription.
    /// </summary>
    /// <returns>Returns a list of the crypto attributes for SDES-SRTP in this MediaDescription. The return
    /// value will not be null but it may be empty.</returns>
    public List<CryptoAttribute> GetCryptoAttributes()
    {
        List<CryptoAttribute> cryptoAttributes = new List<CryptoAttribute>();
        List<SdpAttribute> sdpAttributes = GetNamedAttributes("crypto");
        foreach (SdpAttribute Sa in sdpAttributes)
        {
            CryptoAttribute cryptoAttribute = CryptoAttribute.Parse(Sa.Value);
            cryptoAttributes.Add(cryptoAttribute);
        }

        return cryptoAttributes;
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
    /// <param name="SetType">SetupType to use in the answer to the offered SetupType.</param>
    /// <returns>Returns true if DTLS-SRTP is required.</returns>
    public bool UsingDtlsSrtp(out SetupType SetType)
    {
        bool IsDtlsSrtp = false;
        SetType = SetupType.active;
        if (Transport != null && Transport.ToUpper().IndexOf("UDP/TLS") >= 0 ||
            (Transport.ToUpper().IndexOf("RTP/SAVP") >= 0 && GetNamedAttribute("fingerprint") != null))
        {   // Encryption using DTLS-SRTP has been offerred.
            IsDtlsSrtp = true;
            SdpAttribute SetupAttr = GetNamedAttribute("setup");

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
    /// Returns true if this MediaDescription object is for SDES-SRTP media encryption.
    /// </summary>
    /// <returns></returns>
    public bool UsingSdesSrtp()
    {
        if (GetNamedAttribute("crypto") != null)
            return true;
        else
            return false;
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
    /// Gets the value of the setup attribute.
    /// </summary>
    /// <returns></returns>
    public SetupType GetSetupTypeAttributeValue()
    {
        SetupType setupType = SetupType.unknown;
        string strSetup = GetAttributeValue("setup");
        if (string.IsNullOrEmpty(strSetup) == true)
            return SetupType.unknown;

        switch (strSetup)
        {
            case "active":
                setupType = SetupType.active;
                break;
            case "passive":
                setupType = SetupType.passive;
                break;
            case "actpass":
                setupType = SetupType.actpass;
                break;
            case "holdcon":
                setupType = SetupType.unknown;  // Not supported  
                break;
            default:
                setupType = SetupType.unknown;
                break;
        }

        return setupType;
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
    /// Finds the RtpMapAttribute object for the specified codec name (encoding name)
    /// </summary>
    /// <param name="strCodecName">Codec or encoding name</param>
    /// <returns>Returns the RtpMapAttribute object if found or null if not found</returns>
    public RtpMapAttribute? GetRtpMapForCodecType(string strCodecName)
    {
        RtpMapAttribute rtpMap = null;
        foreach (RtpMapAttribute rtpMapAttribute in RtpMapAttributes)
        {
            if (rtpMapAttribute.EncodingName.ToLower() == strCodecName.ToLower() ||
                rtpMapAttribute.EncodingName.ToUpper() == strCodecName.ToUpper())
            {
                rtpMap = rtpMapAttribute;
                break;
            }
        }

        return rtpMap;
    }

    /// <summary>
    /// Gets the RtpMapAttribute object for the specified payload type
    /// </summary>
    /// <param name="payloadType">Specifies the payload type to look for</param>
    /// <returns>Returns the RtpMapAttribute object if successful or null if it is not present</returns>
    public RtpMapAttribute? GetRtpMapForPayloadType(int payloadType)
    {
        RtpMapAttribute rtpMap = null;
        foreach (RtpMapAttribute rtpAttr in  RtpMapAttributes)
        {
            if (rtpAttr.PayloadType == payloadType)
            {
                rtpMap = rtpAttr;
                break;
            }
        }

        return rtpMap;
    }

    /// <summary>
    /// Finds the fmtp attribute for the specified media format number.
    /// </summary>
    /// <param name="strFormatNumber">String representation of the media format number to look for.</param>
    /// <returns>Returns the SdpAttribute object or null if its not found.</returns>
    public SdpAttribute? GetFmtpForFormatNumber(string strFormatNumber)
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
    public MediaDescription? BuildH264AnswerSmd(int Port)
    {
        MediaDescription AnsSmd = null;

        RtpMapAttribute rtpMapAttribute = GetRtpMapForCodecType("H264");
        if (rtpMapAttribute == null)
            return null;

        if (rtpMapAttribute == null)
            return null;    // H264 was not offered

        AnsSmd = new MediaDescription("video", Port, new List<int> { rtpMapAttribute.PayloadType });
        AnsSmd.Transport = "RTP/AVP";
        RtpMapAttribute AnsRtpAttr = new RtpMapAttribute()
        {
            PayloadType = rtpMapAttribute.PayloadType,
            EncodingName = rtpMapAttribute.EncodingName,
            ClockRate = rtpMapAttribute.ClockRate
        };
        AnsSmd.RtpMapAttributes.Add(AnsRtpAttr);

        SdpAttribute FmtpAttr = GetFmtpForFormatNumber(rtpMapAttribute.PayloadType.ToString());
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
        Sb.AppendFormat("m={0} {1} {2}", MediaType, Port.ToString(), Transport);
        if (PayloadTypes.Count > 0)
        {
            foreach (int Pt in PayloadTypes)
                Sb.AppendFormat(" {0}", Pt);
        }
        else
            Sb.Append(" *");    // Special case -- for MSRP the Payload type is always *

        Sb.Append("\r\n");

        if (ConnectionData != null)
            Sb.Append(ConnectionData.ToString());

        if (string.IsNullOrEmpty(Bandwidth) == false)
            Sb.AppendFormat("b={0}\r\n", Bandwidth);

        foreach (RtpMapAttribute rtpMap in RtpMapAttributes)
            Sb.Append(rtpMap.ToString());

        foreach (SdpAttribute Sa in Attributes)
        {
            if (Sa != null)
                Sb.Append(Sa.ToString());
        } // end for each

        return Sb.ToString();
    }
}
