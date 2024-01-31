//////////////////////////////////////////////////////////////////////////////////////
//  File:  SdpOrigin.cs												20 Nov 22 PHR
//////////////////////////////////////////////////////////////////////////////////////

using System.Net;
using System.Net.Sockets;

namespace SipLib.Sdp;

/// <summary>
/// Class for processing the Origin "o=" SDP line. See Section 5.2 of RFC 4566.
/// </summary>
public class Origin
{
    /// <summary>
    /// User name field.
    /// </summary>
    /// <value></value>
    public string UserName = "";
    /// <summary>
    /// Session ID field.
    /// </summary>
    /// <value></value>
    public string SessionId = Origin.NewSessionId();
    /// <summary>
    /// Version field.
    /// </summary>
    /// <value></value>
    public long Version = 1;
    /// <summary>
    /// Network Type field. Example: "IN" for Internet.
    /// </summary>
    /// <value></value>
    public string NetworkType = "IN";
    /// <summary>
    /// Address type field. Should be "IP4" or "IP6".
    /// </summary>
    /// <value></value>
    public string AddressType = "IP4";
    /// <summary>
    /// IP address. May be an IPv4, an IPv6 address or a fully qualified domain name.
    /// </summary>
    /// <value></value>
    public string Address = null;

    private static Random m_Random = new Random();
    private static string NewSessionId()
    {
        return m_Random.NextInt64(1000000, long.MaxValue).ToString();
    }

    /// <summary>
    /// Constructs a new Origin object. Use this constructor for creating a new origin line for a new SDP
    /// message.
    /// </summary>
    public Origin()
    {
    }

    /// <summary>
    /// Constructs a new Origin object from a string. Use this constructor for parsing the origin string
    /// received in the SDP contents of an SIP message.
    /// </summary>
    /// <param name="strOrigin">The format of this string is:
    ///		"UserName SessionId Version NetworkType AddressType Address".</param>
    /// <returns>Returns a new Origin object.</returns>
    // <exception cref="ArgumentException">If the input string does not contain six fields separated
    // by single spaces or the input string is null or empty, the version field is not valid or the
    // address field is not valid.</exception>
    public static Origin ParseOrigin(string strOrigin)
    {
        if (string.IsNullOrEmpty(strOrigin))
            throw new ArgumentException("The origin line is null or empty", nameof(strOrigin));

        Origin Or = new Origin();
        string[] Fields = strOrigin.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (Fields.Length != 6)
            throw new ArgumentException("Origin input string does not contain six " +
                "fields separated by single spaces", nameof(strOrigin));

        Or.UserName = Fields[0];
        Or.SessionId = Fields[1];
        if (long.TryParse(Fields[2], out Or.Version) == false)
            throw new ArgumentException("The origin Version field is not valid", 
                nameof(strOrigin));

        Or.NetworkType = Fields[3];
        Or.AddressType = Fields[4];
        Or.Address = Fields[5];

        return Or;
    }

    /// <summary>
    /// Constructs a new Origin object from parameters provided by the caller.
    /// </summary>
    /// <param name="strUserName">Display version of the user name.</param>
    /// <param name="Addr">IP address or domain name of the user's position
    /// </param>
    public Origin(string strUserName, IPAddress Addr)
    {
        UserName = strUserName;
        SessionId = Origin.NewSessionId();
        Version = 1;
        NetworkType = "IN";
        if (Addr.AddressFamily == AddressFamily.InterNetwork)
            AddressType = "IP4";
        else
            AddressType = "IP6";

        Address = Addr.ToString();
    }

    /// <summary>
    /// Gets or sets the version number in the SDP Origin as an integer.
    /// </summary>
    /// <value></value>
    public long VersionNumber
    {
        get { return Version; }
        set { Version = value; }
    }

    /// <summary>
    /// Returns the fully formatted Origin string for a SDP message block. The format is : o=Origin where
    /// Origin is the contains each parameter separated by a space.
    /// </summary>
    /// <returns>Returns a full origin line. The format is:
    ///		o=UserName, SessionId, Version, NetworkType, AdddressType Address\r\n
    /// </returns>
    public override string ToString()
    {
        string strRetValue = string.Format("o={0} {1} {2} {3} {4} {5}\r\n",
            UserName, SessionId, Version.ToString(), NetworkType, AddressType,
            Address.ToString());

        return strRetValue;
    }
}
