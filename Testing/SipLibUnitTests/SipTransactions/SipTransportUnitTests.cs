/////////////////////////////////////////////////////////////////////////////////////
//  File:   SipTransportUnitTests.cs                                22 Oct 25 PHR
/////////////////////////////////////////////////////////////////////////////////////

using SipLib.Channels;
using SipLib.Core;
using SipLib.Network;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace SipLibUnitTests.SipTransactions;
using SipLib.Transactions;

/// <summary>
/// Unit tests for functions that create SipTransport objects
/// </summary>
[Trait("Category", "unit")]
public class SipTransportUnitTests
{
    private X509Certificate2 m_Certificate;
    private IPAddress m_IPv4Address;
    private IPAddress m_IPv6Address = null;

    public SipTransportUnitTests()
    {
        m_Certificate = X509CertificateLoader.LoadPkcs12FromFile("PsapSimulator.pfx", "PsapSimulator");
        m_IPv4Address = GetDefaultIPv4Address();
        m_IPv6Address = GetDefaultIPv6Address();
    }

    private IPAddress GetDefaultIPv4Address()
    {
        List<IPAddress> addresses = IpUtils.GetIPv4Addresses();
        return addresses[0];
    }

    private IPAddress GetDefaultIPv6Address()
    {
        List<IPAddress> addresses = IpUtils.GetIPv6Addresses();
        if (addresses.Count > 0) 
            return addresses[0];
        return 
            null;
    }

    [Fact]
    public void TestIPv4UdpTransportCreation()
    {
        SipChannelSettings sipChannelSettings = new SipChannelSettings()
        {
            LocalIPv4Address = m_IPv4Address,
            LocalIPv6Address = m_IPv6Address,
            LocalSipPort = 20000,
            LocalSipsPort = 20001
        };

        string strRemoteSipUri = "sip:User@192.168.1.64:5060";
        SIPURI remoteSipUri = SIPURI.ParseSIPURI(strRemoteSipUri);

        SipTransport sipTransport = SipTransport.CreateFromRemoteSipUri(remoteSipUri, sipChannelSettings, m_Certificate);
        SIPChannel sipChannel = sipTransport.SipChannel;
        Assert.True(sipChannel.SIPChannelEndPoint.Address.AddressFamily == AddressFamily.InterNetwork, "The address family is wrong");
        Assert.True(sipChannel.SIPChannelEndPoint.Protocol == SIPProtocolsEnum.udp, "The Protocol is wrong");

        sipChannel.Close();
    }

    [Fact]
    public void TestIPv4TcpTransportCreation()
    {
        SipChannelSettings sipChannelSettings = new SipChannelSettings()
        {
            LocalIPv4Address = m_IPv4Address,
            LocalIPv6Address = m_IPv6Address,
            LocalSipPort = 20002,
            LocalSipsPort = 20003
        };

        string strRemoteSipUri = "sip:User@192.168.1.64:5060;transport=tcp";
        SIPURI remoteSipUri = SIPURI.ParseSIPURI(strRemoteSipUri);

        SipTransport sipTransport = SipTransport.CreateFromRemoteSipUri(remoteSipUri, sipChannelSettings, m_Certificate);
        SIPChannel sipChannel = sipTransport.SipChannel;
        Assert.True(sipChannel.SIPChannelEndPoint.Address.AddressFamily == AddressFamily.InterNetwork, "The address family is wrong");
        Assert.True(sipChannel.SIPChannelEndPoint.Protocol == SIPProtocolsEnum.tcp, "The Protocol is wrong");

        sipChannel.Close();
    }

    [Fact]
    public void TestIPv4TlsTransportCreation()
    {
        // The computer must have an IPv6 address in order to run this test
        if (m_IPv6Address == null)
            return;

        SipChannelSettings sipChannelSettings = new SipChannelSettings()
        {
            LocalIPv4Address = m_IPv4Address,
            LocalIPv6Address = m_IPv6Address,
            LocalSipPort = 20004,
            LocalSipsPort = 20005
        };

        string strRemoteSipUri = "sips:User@192.168.1.64:5060;transport=tcp";
        SIPURI remoteSipUri = SIPURI.ParseSIPURI(strRemoteSipUri);

        SipTransport sipTransport = SipTransport.CreateFromRemoteSipUri(remoteSipUri, sipChannelSettings, m_Certificate);
        SIPChannel sipChannel = sipTransport.SipChannel;
        Assert.True(sipChannel.SIPChannelEndPoint.Address.AddressFamily == AddressFamily.InterNetwork, "The address family is wrong");
        Assert.True(sipChannel.SIPChannelEndPoint.Protocol == SIPProtocolsEnum.tls, "The Protocol is wrong");

        sipChannel.Close();
    }

    [Fact]
    public void TestIPv6UdpTransport()
    {
        // The computer must have an IPv6 address in order to run this test
        if (m_IPv6Address == null)
            return;

        SipChannelSettings sipChannelSettings = new SipChannelSettings()
        {
            LocalIPv4Address = m_IPv4Address,
            LocalIPv6Address = m_IPv6Address,
            LocalSipPort = 20006,
            LocalSipsPort = 20007
        };

        string strRemoteSipUri = "sip:User@[2600:1298:1300::100]";
        SIPURI remoteSipUri = SIPURI.ParseSIPURI(strRemoteSipUri);

        SipTransport sipTransport = SipTransport.CreateFromRemoteSipUri(remoteSipUri, sipChannelSettings, m_Certificate);
        SIPChannel sipChannel = sipTransport.SipChannel;
        Assert.True(sipChannel.SIPChannelEndPoint.Address.AddressFamily == AddressFamily.InterNetworkV6, "The address family is wrong");
        Assert.True(sipChannel.SIPChannelEndPoint.Protocol == SIPProtocolsEnum.udp, "The Protocol is wrong");

        sipChannel.Close();
    }

    [Fact]
    public void TestIPv6TcpTransport()
    {
        // The computer must have an IPv6 address in order to run this test
        if (m_IPv6Address == null)
            return;

        SipChannelSettings sipChannelSettings = new SipChannelSettings()
        {
            LocalIPv4Address = m_IPv4Address,
            LocalIPv6Address = m_IPv6Address,
            LocalSipPort = 20006,
            LocalSipsPort = 20007
        };

        string strRemoteSipUri = "sip:User@[2600:1298:1300::100];transport=tcp";
        SIPURI remoteSipUri = SIPURI.ParseSIPURI(strRemoteSipUri);

        SipTransport sipTransport = SipTransport.CreateFromRemoteSipUri(remoteSipUri, sipChannelSettings, m_Certificate);
        SIPChannel sipChannel = sipTransport.SipChannel;
        Assert.True(sipChannel.SIPChannelEndPoint.Address.AddressFamily == AddressFamily.InterNetworkV6, "The address family is wrong");
        Assert.True(sipChannel.SIPChannelEndPoint.Protocol == SIPProtocolsEnum.tcp, "The Protocol is wrong");

        sipChannel.Close();
    }

    [Fact]
    public void TestIPv6TlsTransport()
    {
        // The computer must have an IPv6 address in order to run this test
        if (m_IPv6Address == null)
            return;

        SipChannelSettings sipChannelSettings = new SipChannelSettings()
        {
            LocalIPv4Address = m_IPv4Address,
            LocalIPv6Address = m_IPv6Address,
            LocalSipPort = 20008,
            LocalSipsPort = 20009
        };

        string strRemoteSipUri = "sips:User@[2600:1298:1300::100]:5060;transport=tcp";
        SIPURI remoteSipUri = SIPURI.ParseSIPURI(strRemoteSipUri);

        SipTransport sipTransport = SipTransport.CreateFromRemoteSipUri(remoteSipUri, sipChannelSettings, m_Certificate);
        SIPChannel sipChannel = sipTransport.SipChannel;
        Assert.True(sipChannel.SIPChannelEndPoint.Address.AddressFamily == AddressFamily.InterNetworkV6, "The address family is wrong");
        Assert.True(sipChannel.SIPChannelEndPoint.Protocol == SIPProtocolsEnum.tls, "The Protocol is wrong");

        sipChannel.Close();
    }

    [Fact]
    public void TestTransportMatchesSIPURI1()
    {
        SipChannelSettings sipChannelSettings = new SipChannelSettings()
        {
            LocalIPv4Address = m_IPv4Address,
            LocalIPv6Address = m_IPv6Address,
            LocalSipPort = 20010,
            LocalSipsPort = 20011
        };

        string strRemoteSipUri = "sip:User@192.168.1.64:5060;transport=tcp";
        SIPURI remoteSipUri = SIPURI.ParseSIPURI(strRemoteSipUri);

        SipTransport sipTransport = SipTransport.CreateFromRemoteSipUri(remoteSipUri, sipChannelSettings, m_Certificate);
        SIPChannel sipChannel = sipTransport.SipChannel;

        SIPURI candidateSIPURI = SIPURI.ParseSIPURI("sip:User@192.168.1.65:5060;transport=tcp");
        Assert.True(sipTransport.RemoteSipUriMatchesTransport(candidateSIPURI), "Match failure");
        sipTransport.SipChannel.Close();
    }

    [Fact]
    public void TestTransportMismatchesSIPURI1()
    {
        SipChannelSettings sipChannelSettings = new SipChannelSettings()
        {
            LocalIPv4Address = m_IPv4Address,
            LocalIPv6Address = m_IPv6Address,
            LocalSipPort = 20012,
            LocalSipsPort = 20013
        };

        string strRemoteSipUri = "sip:User@192.168.1.64:5060;transport=tcp";
        SIPURI remoteSipUri = SIPURI.ParseSIPURI(strRemoteSipUri);

        SipTransport sipTransport = SipTransport.CreateFromRemoteSipUri(remoteSipUri, sipChannelSettings, m_Certificate);
        SIPChannel sipChannel = sipTransport.SipChannel;

        SIPURI candidateSIPURI = SIPURI.ParseSIPURI("sip:User@192.168.1.65:5060");
        Assert.True(sipTransport.RemoteSipUriMatchesTransport(candidateSIPURI) == false, "Should not match");

        sipTransport.SipChannel.Close();
    }

    [Fact]
    public void TestTransportMismatchesSIPURI2()
    {
        SipChannelSettings sipChannelSettings = new SipChannelSettings()
        {
            LocalIPv4Address = m_IPv4Address,
            LocalIPv6Address = m_IPv6Address,
            LocalSipPort = 20014,
            LocalSipsPort = 20015
        };

        string strRemoteSipUri = "sip:User@192.168.1.64:5060;transport=tcp";
        SIPURI remoteSipUri = SIPURI.ParseSIPURI(strRemoteSipUri);

        SipTransport sipTransport = SipTransport.CreateFromRemoteSipUri(remoteSipUri, sipChannelSettings, m_Certificate);
        SIPChannel sipChannel = sipTransport.SipChannel;

        SIPURI candidateSIPURI = SIPURI.ParseSIPURI("sips:User@[2600:1298:1300::100]:5060;transport=tcp");
        Assert.True(sipTransport.RemoteSipUriMatchesTransport(candidateSIPURI) == false, "Should not match");

        sipTransport.SipChannel.Close();
    }
}
