using SipLib.Channels;
using SipLib.Core;
using SipLib.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace SipLibUnitTests.Channels;

/// <summary>
/// Unit tests for the SIPTLSChannel class
/// </summary>
[Trait("Category", "unit")]
public class SIPTLSChannelUnitTests
{
    // Common Name -- CN=PsapSimulator
    private X509Certificate2 m_PsapSimulatorCert = X509CertificateLoader.LoadPkcs12FromFile("PsapSimulator.pfx", "PsapSimulator");
    // Common Name -- CN=SrsSimulator
    private X509Certificate2 m_SrsSimulatorCert = X509CertificateLoader.LoadPkcs12FromFile("SrsSimulator.pfx", "SrsSimulator");
    // Common Name -- CN=OspSimulator
    private X509Certificate2 m_OspSimulatorCert = X509CertificateLoader.LoadPkcs12FromFile("OspSimulator.Pfx", "OspSimulator");
    // Common Name -- CN=PsapSimulator
    private X509Certificate2 m_PsapSimulatorNoPrivateKey = X509CertificateLoader.LoadCertificateFromFile("PsapSimulator.cer");

    private IPAddress m_MyIpAddress = IpUtils.GetDefaultIPv4Address();

    // Be careful to pick port numbers that no other unit tests that use the default IPv4 address use
    private const int SERVER_SIP_PORT = 16000;
    private const int CLIENT_1_SIP_PORT = 16002;
    private const int CLIENT_2_SIP_PORT = 16004;

    private AutoResetEvent m_Event = new AutoResetEvent(false);
    private string m_ServerCertificateSubject = string.Empty;
    private const int WAIT_ONE_MS = 1000;

    public SIPTLSChannelUnitTests(Xunit.Abstractions.ITestOutputHelper output)
    {
    }

    // Tests SIPTLSChannel.SwapCertificate()
    [Fact]
    public void TestSwapCertificate()
    {
        IPEndPoint serverIpe = new IPEndPoint(m_MyIpAddress, SERVER_SIP_PORT);
        IPEndPoint client1Ipe = new IPEndPoint(m_MyIpAddress, CLIENT_1_SIP_PORT);
        IPEndPoint client2Ipe = new IPEndPoint(m_MyIpAddress, CLIENT_2_SIP_PORT);

        SIPTLSChannel serverChannel = new SIPTLSChannel(m_PsapSimulatorCert, serverIpe, "Server", true);
        serverChannel.SIPMessageReceived = ServerSIPMessageReceivedCallback;

        SIPTLSChannel client1Channel = new SIPTLSChannel(m_OspSimulatorCert, client1Ipe, "Client1", true,
            null, null, Client1AcceptServerCertificate);
        SIPTLSChannel client2Channel = new SIPTLSChannel(m_OspSimulatorCert, client2Ipe, "Client1", true,
            null, null, Client2AcceptServerCertificate);

        SIPURI serverSipUri = serverChannel.SIPChannelContactURI;
        SIPURI client1SipUri = client1Channel.SIPChannelContactURI;
        SIPURI client2SipUri = client2Channel.SIPChannelContactURI;

        SIPRequest request1 = SIPRequest.CreateRequest(SIPMethodsEnum.OPTIONS, serverSipUri, serverSipUri,
            "Server", client1SipUri, "Client1", client1SipUri);
        // Sending the first message to the serverChannel causes client1Channel to connect to the 
        // serverChannel first.
        client1Channel.Send(serverIpe, request1.ToByteArray());

        bool signaled = m_Event.WaitOne(WAIT_ONE_MS);
        Assert.True(signaled == true, "Send() from client1 failed");

        serverChannel.SwapCertificate(m_SrsSimulatorCert);

        SIPRequest request2 = SIPRequest.CreateRequest(SIPMethodsEnum.OPTIONS, serverSipUri, serverSipUri,
            "Server", client2SipUri, "Client2", client2SipUri);
        // Sending the first message to the serverChannel causes client2Channel to connect to the 
        // serverChannel first.
        client2Channel.Send(serverIpe, request2.ToByteArray());
        signaled = m_Event.WaitOne(WAIT_ONE_MS);
        Assert.True(signaled == true, "Send() from client2 failed");

        Assert.True(string.IsNullOrEmpty(m_ServerCertificateSubject) == false, "SwapCertificate() failed");
        Assert.True(m_ServerCertificateSubject.Contains("CN=SrsSimulator") == true, "Server certificate mismatch");

        // Now send a second message from client1 and verify that it did not have to reconnect. This can
        // be determined by the fact that Client1AcceptServerCertificate() is not called.
        m_ServerCertificateSubject = string.Empty;
        client1Channel.Send(serverIpe, request1.ToByteArray());

        signaled = m_Event.WaitOne(WAIT_ONE_MS);
        Assert.True(signaled == true, "Second Send() from client1 failed");
        Assert.True(string.IsNullOrEmpty(m_ServerCertificateSubject) == true, "Failure, client1 needed to reconnect");

        // Test passing null for the new certificate
        Assert.Throws<ArgumentNullException>("newCertificate", () => serverChannel.SwapCertificate(null));

        // Now try passing a certificate with no private key.
        Assert.Throws<ArgumentException>("newCertificate", () => serverChannel.SwapCertificate(m_PsapSimulatorNoPrivateKey));

        client1Channel.Close();
        client2Channel.Close();
        serverChannel.Close();
    }

    // Handles a SIP message received by the serverChannel
    private void ServerSIPMessageReceivedCallback(SIPChannel sipChannel, SIPEndPoint remoteEndPoint, byte[] buffer)
    {
        // Don't need to worry about the message contents -- just so long as a SIP message was received
        m_Event.Set();
    }

    // Called by client1Channel as it attempts to connect to the serverChannel with TLS and mutual authentication enabled
    private bool Client1AcceptServerCertificate(X509Certificate certificate, X509Chain chain,
        SslPolicyErrors? sslPolicyErrors)
    {
        if (certificate != null)
            m_ServerCertificateSubject = certificate.Subject;

        return true;    // Return true to allow the connection to the server
    }

    // Called by client1Channel as it attempts to connect to the serverChannel with TLS and mutual authentication enabled
    private bool Client2AcceptServerCertificate(X509Certificate certificate, X509Chain chain,
        SslPolicyErrors? sslPolicyErrors)
    {
        if (certificate != null)
            m_ServerCertificateSubject = certificate.Subject;

        return true;    // Return true to allow the connection to the server
    }
}
