//////////////////////////////////////////////////////////////////////////////////////
//	File:	ConnectionData.cs                                       16 Nov 22 PHR
//////////////////////////////////////////////////////////////////////////////////////

using System.Net;
using System.Net.Sockets;

namespace SipLib.Sdp;

/// <summary>
/// Class for processing the Connection Data "c=" type of the SDP contents.
/// </summary>
public class ConnectionData
{
    /// <summary>
    /// Contains the network type. "IN" = Internet.
    /// </summary>
    /// <value></value>
    public string NetworkType = "IN";
    /// <summary>
    /// Contains the Address Type information. Should be "IP4" or "IP6".
    /// </summary>
    /// <value></value>
    public string AddressType = "IP4";

    /// <summary>
    /// Contains the IP address portion of a c= SDP line. May be an IPv4 or an IPv6 address
    /// </summary>
    /// <value></value>
    public IPAddress Address = null;

    /// <summary>
    /// Contains the number of IP addresses if the Address is a multicast address.
    /// A value of 0 indicates that the address count field is not present.
    /// </summary>
    /// <value></value>
    public int AddressCount = 0;
    /// <summary>
    /// Contains the Time To Live field of the IP address. A value of -1 indicates that the TTL field is
    /// not present.
    /// </summary>
    /// <value></value>
    public int TTL = -1;

    /// <summary>
    /// Constructs a new, empty ConnectionData object. Use this construct when creating new connection
    /// data for SDP contents of a new SIP message.
    /// </summary>
    public ConnectionData()
    {
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="address"></param>
    public ConnectionData(IPAddress address)
    {
        Address = address;
        AddressType = address.AddressFamily == AddressFamily.InterNetwork ? "IP4" : "IP6";
    }

    /// <summary>
    /// Parses a string containing the parameter fields of the SDP c= line.
    /// </summary>
    /// <param name="strConnectionData">Contains the parameter fields of the c= line. The "c=" field must
    /// not be present. </param>
    /// <returns>Returns a new ConnectionData object</returns>
    // <exception cref="ArgumentException">Thrown if the c= line is not valid.</exception>
    public static ConnectionData ParseConnectionData(string strConnectionData)
    {
        ConnectionData Cd = new ConnectionData();
        char[] Delim = { ' ' };
        string[] Fields = strConnectionData.Split(Delim);
        if (Fields.Length != 3)
            throw new ArgumentException("Incorrect number of fields in the " +
                "connection data line", "strConnectionData");

        Cd.NetworkType = Fields[0];
        Cd.AddressType = Fields[1];
        string strAddrField = Fields[2];
        string strAddress;

        int Idx;
        Idx = strAddrField.IndexOf("/");
        if (Idx >= 0)
            // Strip out the TTL and/or address count fields from the IP address
            strAddress = strAddrField.Remove(Idx);
        else
            strAddress = strAddrField;

        if (IPAddress.TryParse(strAddress, out Cd.Address) == false)
            throw new ArgumentException("The IP address in the SDP connection " +
                "data line is not valid", "strConnectionData");

        // Get the TTL and address count fields if present
        string[] strAry = strAddrField.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (strAry.Length >= 2)
        {
            if (Cd.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                int.TryParse(strAry[1], out Cd.TTL);
                if (strAry.Length == 3)
                    int.TryParse(strAry[2], out Cd.AddressCount);
            }
            else
                // Its an IPv6 address and there is no TTL, so get the address count
                int.TryParse(strAry[1], out Cd.AddressCount);
        }

        return Cd;
    }

    /// <summary>
    /// Creates a deep copy of this object.
    /// </summary>
    /// <returns>A new object with a copy of the member variables.</returns>
    public ConnectionData CreateCopy()
    {
        ConnectionData RetVal = new ConnectionData();
        string str = this.ToString();
        RetVal = ConnectionData.ParseConnectionData(str.Replace("c=", "").
            Replace("\r\n", ""));
        return RetVal;
    }

    /// <summary>
    /// Converts the ConnectionData object to a string.
    /// </summary>
    /// <returns>The string format is: 
    /// "c=NetworkType AddressType ConnectionAddress\r\n"</returns>
    public override string ToString()
    {
        string strRetValue = string.Format("c={0} {1} {2}\r\n",
            NetworkType, AddressType, Address.ToString());

        if (Address.AddressFamily == AddressFamily.InterNetwork)
        {
            if (TTL != -1)
            {
                strRetValue += "/" + TTL.ToString();
                // For IPv4, if there is a TTL then there may be an address count
                if (AddressCount != 0)
                    strRetValue += "/" + AddressCount.ToString();
            }
        }
        else if (Address.AddressFamily == AddressFamily.InterNetworkV6)
        {    // There is no TTL field for IPv6, but there may be an address count
            if (AddressCount != 0)
                strRetValue += "/" + AddressCount.ToString();
        }

        return strRetValue;
    }
}
