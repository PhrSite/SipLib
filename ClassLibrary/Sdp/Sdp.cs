/////////////////////////////////////////////////////////////////////////////////////
//	File:	Sdp.cs													16 Nov 22 PHR
/////////////////////////////////////////////////////////////////////////////////////

// RFC 4566 describes the Session Description Protocol (SDP)
// See RFC 3266 for a description of how to handle IPv6 addresses in the SDP
// See RFC 5118 for the SIP Torture tests using IPv6
    
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Metadata;

namespace SipLib.Sdp;

/// <summary>
/// Class for processing the Session Description Protocol message contents
/// of a SIP message. See RFC 4566. 
/// </summary>
public class Sdp
{
    /// <summary>
    /// Contains the version number (v) of the SDP protocol. This is
    /// expected to be always 0. See Section 5.1 of RFC 4566.
    /// </summary>
    public int Version = 0;
    /// <summary>
    /// Contains the origin information for the "o" parameter. See Section 5.2 of 
    /// RFC 4566.
    /// </summary>
    public Origin Origin = null;
    /// <summary>
    /// Contains the Session Name for the "s" type. parameter Section 5.3 of RFC 4566.
    /// </summary>
    public string SessionName = "";

    /// <summary>
    /// Contains the session information "i" parameter. See Section 5.4 of RFC 4566.
    /// This parameter is optional.
    /// </summary>
    public string SessionInformation = "";

    /// <summary>
    /// Contains a URI to more information about the session. This is the "u" parameter.
    /// See Section 5.5 of RFC 4566. This parameter is optional.
    /// </summary>
    public string Uri = "";

    /// <summary>
    /// Contains the e-mail parameter (e) for the session. See Section 5.6 of RFC 4566.
    /// This parameter is optional.
    /// </summary>
    public string Email = "";

    /// <summary>
    /// Contains the phone number (p) parameter for the session. See Section 5.6 of RFC 4566.
    /// This parameter is optional.
    /// </summary>
    public string PhoneNumber = "";

    /// <summary>
    /// Bandwidth parameter (b) for the entire session. See Section 5.8 of RFC 4566.
    /// This parameter is optional. Treating it as a simple string.
    /// </summary>
    public string Bandwidth = "";

    /// <summary>
    /// Timing (t) parameter. See Section 5.9 of RFC 4566.
    /// </summary>
    public string Timing = "0 0";

    /// <summary>
    /// Contains a list of all of the different types of media for a call.
    /// </summary>
    public List<MediaDescription> Media = new List<MediaDescription>();

    /// <summary>
    /// Contains the connecton data for the call.
    /// </summary>
    public ConnectionData ConnectionData = null;

    /// <summary>
    /// Contains the attributes for this media session.
    /// </summary>
    public List<SdpAttribute> Attributes = new List<SdpAttribute>();

    /// <summary>
    /// Constructs a new Sdp object. Use this constructor when building a new SDP block to send.
    /// </summary>
    public Sdp()
    {
        Origin = new Origin();
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
        if (LocalIp.AddressFamily == AddressFamily.InterNetwork)
            ConnectionData = new ConnectionData("IP4", LocalIp);
        else
                ConnectionData = new ConnectionData("IP6", LocalIp);

            SessionName = UaName + "_" + Rnd.Next().ToString();
    }

    /// <summary>
    /// Parses the SDP contained in a list of strings.
    /// </summary>
    /// <param name="SdpContents">Each item in the list contains one
    /// line in the SDP</param>
    /// <returns>Returns an Sdp object</returns>
    // <exception cref="ArgumentException">Thrown if an invalid argument is detected</exception>
    // <exception cref="Exception">Thrown if an unexpected error occurs</exception>
    public static Sdp ParseSDP(List<string> SdpContents)
    {
        if (SdpContents == null || SdpContents.Count == 0)
            throw new ArgumentException("SdpContents is null or empty", nameof(SdpContents));

        Sdp sdp;
        try
        {
            sdp = ParseSDP(SdpContents.ToArray());
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
    /// Parses the SDP contained in an array of strings.
    /// </summary>
    /// <param name="Lines">Each string in the array contains one line in the SDP</param>
    /// <returns>Returns an Sdp object</returns>
    // <exception cref="ArgumentException">Thrown if an invalid argument is detected</exception>
    // <exception cref="Exception">Thrown if an unexpected error occurs</exception>
    public static Sdp ParseSDP(string[] Lines)
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
                        sdp.Timing = strValue;
                        break;
                    case "s":
                        sdp.SessionName = strValue;
                        break;
                    case "i":
                        sdp.SessionInformation = strValue;
                        break;
                    case "u":
                        sdp.Uri = strValue;
                        break;
                    case "e":
                        sdp.Email = strValue;
                        break;
                    case "p":
                        sdp.PhoneNumber = strValue;
                        break;
                    case "b":
                        if (sdp.Media.Count == 0)
                            sdp.Bandwidth = strValue;
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
                        sdp.Media.Add(MediaDescription.ParseMediaDescription(strValue));
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
                        if (sdp.Media.Count > 0)
                        {   // In a m= block so attributes belong to that media
                            sdp.Media[sdp.Media.Count - 1].Attributes.Add(SdpAttribute.
                                ParseSdpAttribute(strValue));
                        }
                        else
                        {   // No media blocks yet so attributes belong to the 
                            // session
                            sdp.Attributes.Add(SdpAttribute.ParseSdpAttribute(strValue));
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
    // <exception cref="ArgumentException">Thrown if an invalid argument is detected</exception>
    // <exception cref="Exception">Thrown if an unexpected error occurs</exception>
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
        string str = ToString();
        string[] Lines = str.Split(new char[] { '\r', '\n' }, 
            StringSplitOptions.RemoveEmptyEntries);
        return Sdp.ParseSDP(Lines);
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
    /// <param name="strType">Type of media such as audio, video, text, etc..
    /// </param>
    /// <returns>Returns the SepMediaDescription object if the specified type
    /// of media is present or null if it is not.</returns>
    public MediaDescription GetMediaType(string strType)
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
    /// Gets the index of a specified media type in the Media list.
    /// </summary>
    /// <param name="strType">Type of media to look for. Must be "audio",
    /// "video", "text" or "message"</param>
    /// <returns>Returns the index of the media type in the Media list.
    /// Returns -1 if the media type is not present.</returns>
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
            if (Smd.MediaType == "video" || Smd.MediaType == "text" ||
                Smd.MediaType == "message")
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
    public bool GetAudioConnectionData(ref string strIpAddr, ref int Port)
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

    /// <summary>
    /// Processes the contents of SDP line.
    /// </summary>
    /// <param name="str">Input line to parse and process</param>
    private void ProcessContentsLine(string str)
    {
        string strType;
        string strValue;

        if (str.Length >= 3)
        {
            strType = str.Substring(0, 1);
                strValue = GetValueOfNameValuePair(str, '=');

            switch (strType)
            {
                case "v":
                    Version = Convert.ToInt32(strValue);
                    break;
                case "o":
                    Origin = Origin.ParseOrigin(strValue);
                    break;
                case "s":
                    SessionName = strValue;
                    break;
                case "m":
                    Media.Add(MediaDescription.ParseMediaDescription(strValue));
                    break;
                case "c":
                    if (Media.Count == 0)
                        ConnectionData = ConnectionData.ParseConnectionData(strValue);
                    else
                    {
                        Media[Media.Count - 1].ConnectionData =  
                            ConnectionData.ParseConnectionData(strValue);
                    }
                    break;
                case "a":
                    if (Media.Count > 0)
                    {	// In a m= block so attributes belong to that media
                        Media[Media.Count - 1].Attributes.Add(SdpAttribute.ParseSdpAttribute(
                            strValue));
                    }
                    else
                    {   // No media blocks yet so attributes belong to the 
                            // session
                        Attributes.Add(SdpAttribute.ParseSdpAttribute(strValue));
                    }

                    break;
            } // end switch strType
        }
    }

    private string GetValueOfNameValuePair(string Input, char Sep)
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
    /// Returns the SdpAttribute object for a named attribute for the entire
    /// SDP at the session level.
    /// </summary>
    /// <param name="strAttr">Name of the attribute to search for.</param>
    /// <returns>Returns a SdpAttribute for the named attribute if it is found 
    /// or null if the named attribute is not present.</returns>
    public SdpAttribute GetNamedAttribute(string strAttr)
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
    public static IPAddress GetMediaIPAddr(Sdp Sdp, MediaDescription Smd)
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
    /// <param name="Smd">The SDP Media Description block for the media to get the IP address for.</param>
    /// <returns>Returns an IPAddress. Returns null if there was no address specified in the session or
    /// media levels.</returns>
    public static IPEndPoint GetMediaEndPoint(Sdp Sdp, MediaDescription 
        Smd)
    {
        IPAddress MediaIpAddr = Sdp.GetMediaIPAddr(Sdp, Smd);
        if (MediaIpAddr == null)
            return null;
        else 
            return new IPEndPoint(MediaIpAddr, Smd.Port);
    }
}
