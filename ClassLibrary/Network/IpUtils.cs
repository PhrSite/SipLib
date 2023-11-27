/////////////////////////////////////////////////////////////////////////////////////
//  File:   IpUtils.cs                                              14 Jul 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace SipLib.Network;

/// <summary>
/// Static class that contains various utility functions relating to IP addresses. 
/// </summary>
public static class IpUtils
{
    // Used by hosts attempting to acquire a DHCP address. See RFC 3330.
    private const string LINK_LOCAL_BLOCK_PREFIX = "169.254";

    private const string IPV4_LOCAL_LOOPBACK = "127.0.0.1";
    private const string IPV6_LOCAL_LOOPBACK = "::1";
    private const string IPV6_LOCAL_LINK_PREFIX = "fe80";

    /// <summary>
    /// Gets a list of all available IPv4 IP addresses on the local machine. The list will not contain
    /// the local loopback IPv4 address.
    /// </summary>
    /// <returns>Returns a list of addresses. The list may be empty but it will never be null.</returns>
    public static List<IPAddress> GetIPv4Addresses()
    {
        List<IPAddress> localAddresses = new List<IPAddress>();

        NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
        foreach (NetworkInterface adapter in adapters)
        {
            if (adapter.OperationalStatus != OperationalStatus.Up)
                continue;

            IPInterfaceProperties adapterProperties = adapter.GetIPProperties();

            UnicastIPAddressInformationCollection localIPs = adapterProperties.UnicastAddresses;
            foreach (UnicastIPAddressInformation localIP in localIPs)
            {
                string strIpv4Addr = localIP.Address.ToString();
                if (localIP.Address.AddressFamily != AddressFamily.InterNetwork)
                    continue;

                if (strIpv4Addr.StartsWith(LINK_LOCAL_BLOCK_PREFIX) == false && strIpv4Addr != 
                    IPV4_LOCAL_LOOPBACK)
                    localAddresses.Add(localIP.Address);
            }
        }

        return localAddresses;
    }

    /// <summary>
    /// Gets a list of all available IPv6 IP addresses on the local machine. This function does not
    /// include IPv6 local link addresses.
    /// </summary>
    /// <returns>Returns a list of addresses. The list may be empty but it will never be null.</returns>
    public static List<IPAddress> GetIPv6Addresses()
    {
        List<IPAddress> localAddresses = new List<IPAddress>();

        NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
        foreach (NetworkInterface adapter in adapters)
        {
            if (adapter.OperationalStatus != OperationalStatus.Up)
                continue;

            IPInterfaceProperties adapterProperties = adapter.GetIPProperties();

            UnicastIPAddressInformationCollection localIPs = adapterProperties.UnicastAddresses;
            foreach (UnicastIPAddressInformation localIP in localIPs)
            {
                string strIpv6Addr = localIP.Address.ToString();
                if (localIP.Address.AddressFamily != AddressFamily.InterNetworkV6)
                    continue;

                if (strIpv6Addr == IPV6_LOCAL_LOOPBACK || strIpv6Addr.StartsWith(IPV6_LOCAL_LINK_PREFIX) 
                    == true)
                    continue;

                localAddresses.Add(localIP.Address);
            }
        }

        return localAddresses;
    }

    /// <summary>
    /// Gets a list of local link IPv6 addresses on the local machine. The list will not include the
    /// local loopback address.
    /// </summary>
    /// <returns>Returns a list of addresses. The list may be empty but it will never be null.</returns>
    public static List<IPAddress> GetIPv6LocalLinkAddresses()
    {
        List<IPAddress> localAddresses = new List<IPAddress>();
        NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
        foreach (NetworkInterface adapter in adapters)
        {
            if (adapter.OperationalStatus != OperationalStatus.Up)
                continue;

            IPInterfaceProperties adapterProperties = adapter.GetIPProperties();

            UnicastIPAddressInformationCollection localIPs = adapterProperties.UnicastAddresses;
            foreach (UnicastIPAddressInformation localIP in localIPs)
            {
                string strIpv6Addr = localIP.Address.ToString();
                if (localIP.Address.AddressFamily != AddressFamily.InterNetworkV6)
                    continue;

                if (strIpv6Addr == IPV6_LOCAL_LOOPBACK)
                    continue;

                if (strIpv6Addr.StartsWith(IPV6_LOCAL_LINK_PREFIX) == true)
                    localAddresses.Add(localIP.Address);
            }
        }

        return localAddresses;
    }
}
