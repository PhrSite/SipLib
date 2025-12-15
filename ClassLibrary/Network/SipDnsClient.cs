/////////////////////////////////////////////////////////////////////////////////////
//  File:   SipDnsClient.cs                                         27 Oct 25 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Network;

using DnsClient;
using DnsClient.Protocol;
using SipLib.Core;
using System.Net;
using System.Net.Sockets;

/// <summary>
/// Class for doing DNS server lookups for SIP.
/// <para>This class uses the LookupClient class of the DnsClient class library.</para>
/// <para>It uses a single instance of the LookupClient class per application so that features such as the results 
/// cache and connection pooling are used. The LookupCLient class is thread-safe and so is this class.</para>
/// </summary>
public static class SipDnsClient
{
    private static LookupClient m_DnsClient = new LookupClient();

    /// <summary>
    /// If the input SIPURI does not contain an IP endpoint then resolve the server name into an IP endpoint and return
    /// a new SIPURI object. If the input SIPURI contains an IP endpoint, then return the input SIPURI.
    /// </summary>
    /// <param name="inSipUri">Input SIPURI to get an IP endpoint for. If this SIPURI specifies a port then that port
    /// is used in the new SIPURI. If it does not contain a port number then the default port number for the SIP sheme is
    /// used (5060 for sip or 5061 for sips).</param>
    /// <param name="includeIPv6">If true, the resolve for IPv6 addresses. Else only IPv4 addresses are searched for.
    /// The default is false.</param>
    /// <param name="preferIPv6">If true, then IPv6 is preferred over IPv4. Defaults to false. Used only if includeIPv6 is true.</param>
    /// <param name="useHostsFile">If true, then use the .NET Dns.GetHostEntry() function to search for addresses. 
    /// Dns.GetHostEntry() also searches the local Hosts file. Defaults fo false. Dns.GetHostEntry() is only called if
    /// unable to resolve any IP addresses using the DnsClient class library.</param>
    /// <returns>If the input SIPURI object does not contain an IP endpoint, then a new SIPURI is built containing the first
    /// IP address found. If the input SIPURI object contains an IP endpoint then that SIPURI is returned unmodified.</returns>
    public static async Task<SIPURI?> ResolveSipServerAsync(SIPURI inSipUri, bool includeIPv6 = false, bool preferIPv6 = false,
        bool useHostsFile = false)
    {
        SIPSchemesEnum scheme = inSipUri.Scheme;
        SIPProtocolsEnum protocol = inSipUri.Protocol;

        if (scheme != SIPSchemesEnum.sip && scheme != SIPSchemesEnum.sips)
            return null;        // Error: The input SIPURI is not for SIP

        SIPURI? newSipUri = null;

        SIPEndPoint? sipEndPoint = inSipUri.ToSIPEndPoint();
        if (sipEndPoint is not null)
        {   // The input SIPURI already contains an IPEndPoint
            return inSipUri;
        }

        List<IPAddress> addresses = new List<IPAddress>();

        int Port = 0;
        if (inSipUri.HostPort == null || int.TryParse(inSipUri.HostPort, out Port) == false)
            Port = SIPConstants.GetDefaultPort(protocol);

        string server = inSipUri.Host!;
        int lastIndex = server.LastIndexOf(':');
        // Remove the port number if one is specified.
        // Note: Have already determined that the host portion does not have an IPv6 address, so the last occurrance
        // of a ':' character separates the host name and the port number.
        if (lastIndex > 0)
            server = server.Substring(0, lastIndex);

        if (includeIPv6 == true && preferIPv6 == true)
        {
            List<IPAddress> IPv6Addresses = await GetIPv6Addresses(server);
            if (IPv6Addresses.Count > 0)
                addresses.AddRange(IPv6Addresses);
        }

        List<IPAddress> IPv4Addresses = await GetIPv4Addresses(server);
        if (IPv4Addresses.Count > 0) 
            addresses.AddRange(IPv4Addresses);

        if (includeIPv6 == true && preferIPv6 == false)
        {
            List<IPAddress> IPv6Addresses = await GetIPv6Addresses(server);
            if (IPv6Addresses.Count > 0)
                addresses.AddRange(IPv6Addresses);
        }

        // Last ditch effort. Use Dns.GetHostEntry(), which also reads entries from the Hosts file. This effort
        // ignores the preference for IPv6 for simplicity.
        if (addresses.Count == 0 && useHostsFile)
        {
            IPHostEntry iph;
            try
            {
                iph = Dns.GetHostEntry(server);
            }
            catch
            {
                return null;
            }

            foreach (IPAddress ip in iph.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    if (includeIPv6 == true)
                        addresses.Add(ip);
                }
                else
                    addresses.Add(ip);
            }
        }

        if (addresses.Count == 0)
            return null;

        SIPEndPoint newSipEndpoint = new SIPEndPoint(protocol, new IPEndPoint(addresses[0], Port));
        newSipUri = new SIPURI(scheme, newSipEndpoint);
        newSipUri.User = inSipUri.User;
        newSipUri.Parameters = inSipUri.Parameters;

        return newSipUri;
    }

    private static async Task<List<IPAddress>> GetIPv4Addresses(string server)
    {
        List<IPAddress> addresses = new List<IPAddress>();
        IDnsQueryResponse result = await m_DnsClient.QueryAsync(server, QueryType.A);
        foreach (ARecord aRecord in result.Answers.ARecords())
            addresses.Add(aRecord.Address);

        return addresses;
    }

    private static async Task<List<IPAddress>> GetIPv6Addresses(string server)
    {
        List<IPAddress> addresses = new List<IPAddress>();
        IDnsQueryResponse result = await m_DnsClient.QueryAsync(server, QueryType.AAAA);
        foreach (AaaaRecord aRecord in result.Answers.AaaaRecords())
            addresses.Add(aRecord.Address);

        return addresses;
    }
}
