/////////////////////////////////////////////////////////////////////////////////////
//  File:   MsrpConnectionUnitTests.cs                              21 Aug 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLibUnitTests.Msrp;

using SipLib.Core;
using SipLib.Msrp;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;

[Trait("Category", "unit")]
public class MsrpConnectionUnitTests
{
    /// <summary>
    /// Specifies the path to the files containing the test MSRP messages. Change this if the project
    /// location or the location of the test files change.
    /// </summary>
    private const string Path = @"..\..\..\MsrpMessages\";

    private int ClientPort
    {
        get { return Crypto.GetRandomInt(17000, 17999); }
    } 

    private int ServerPort
    {
        get { return Crypto.GetRandomInt(18000, 18999); }
    }

    private MsrpConnection MsrpServer;
    private MsrpConnection MsrpClient;

    private ManualResetEventSlim ServerMessageReceivedEvent = new ManualResetEventSlim(false);
    private string ServerReceivedContentType = null;
    private byte[] ServerReceivedMessageBytes = null;

    private ManualResetEventSlim ClientMessageReceivedEvent = new ManualResetEventSlim(false);
    private string ClientReceivedContentType = null;
    private byte[] ClientReceivedMessageBytes = null;

    private const int ShortMessageTimeoutMs = 1000;
    private const int LongMessageTimeoutMs = 5000;

    [Fact]
    public void MsrpClientServerMessageTests1()
    {
        IPAddress ipAddress = IPAddress.Loopback;

        X509Certificate2 ClientCert = new X509Certificate2($"{Path}MsrpClient.pfx", "MsrpClient");
        X509Certificate2 ServerCert = new X509Certificate2($"{Path}MsrpServer.pfx", "MsrpServer");

        MsrpUri ClientMsrpUri = new MsrpUri(SIPSchemesEnum.msrps, "Client", ipAddress, ClientPort);
        MsrpUri ServerMsrpUri = new MsrpUri(SIPSchemesEnum.msrps, "Server", ipAddress, ServerPort);

        MsrpServer = MsrpConnection.CreateAsServer(ServerMsrpUri, ClientMsrpUri, ServerCert);
        MsrpServer.MsrpMessageReceived += OnServerMessageReceived;
        MsrpServer.MaxMsrpMessageLength = 20000000;
        MsrpServer.Start();

        MsrpClient = MsrpConnection.CreateAsClient(ClientMsrpUri, ServerMsrpUri, ClientCert);
        MsrpClient.MsrpMessageReceived += OnClientMessageReceived;
        MsrpClient.MaxMsrpMessageLength = 20000000;
        MsrpClient.Start();

        string ClientMessage1 = "Hello from the client";
        MsrpClient.SendMsrpMessage("text/plain", Encoding.UTF8.GetBytes(ClientMessage1), MsrpMessage.
            NewRandomID());
        bool ServerMessageReceivedSignaled = ServerMessageReceivedEvent.Wait(ShortMessageTimeoutMs);
        Assert.True(ServerMessageReceivedSignaled == true, "ServerMessageReceivedSignaled is false");
        Assert.True(ServerMessageReceivedEvent.IsSet == true, "ClientMessage1 send timeout");
        Assert.True(ServerReceivedContentType == "text/plain", "ClientMessage1 ContentType is wrong");

        string strServeReceivedMessage = Encoding.UTF8.GetString(ServerReceivedMessageBytes);
        Assert.True(ClientMessage1 == Encoding.UTF8.GetString(ServerReceivedMessageBytes),
            "ClientMessage1 contents mismatch");
        ServerMessageReceivedEvent.Reset();

        string ServerMessage1 = "Hello from the server";
        MsrpServer.SendMsrpMessage("text/plain", Encoding.UTF8.GetBytes(ServerMessage1), MsrpMessage.
            NewRandomID());
        bool ClientMessageReceivedSignaled = ClientMessageReceivedEvent.Wait(ShortMessageTimeoutMs);
        Assert.True(ClientMessageReceivedSignaled == true, "ClientMessageReceivedSignaled is false");
        Assert.True(ClientMessageReceivedEvent.IsSet == true, "ServerMessage1 send timeout");
        Assert.True(ClientReceivedContentType == "text/plain", "ServerMessage1 ContentType is wrong");
        string clientReceivedMessage = Encoding.UTF8.GetString(ClientReceivedMessageBytes);
        Assert.True(ServerMessage1 == clientReceivedMessage, "ServerMessage1 Contents mismatch");
        ClientMessageReceivedEvent.Reset();

        MsrpClient.Shutdown();
        MsrpServer.Shutdown();
    }

    private void OnServerMessageReceived(string ContentType, byte[] Contents, string from)
    {
        ServerReceivedContentType = ContentType;
        ServerReceivedMessageBytes = Contents;
        ServerMessageReceivedEvent.Set();
    }

    private void OnClientMessageReceived(string ContentType, byte[] Contents, string from)
    {
        ClientReceivedContentType = ContentType;
        ClientReceivedMessageBytes = Contents;
        ClientMessageReceivedEvent.Set();
    }

    /// <summary>
    /// Tests the MsrpConnectionEstablished event of a MSRP client
    /// </summary>
    [Fact]
    public void ClientConnectionEstablished()
    {
        IPAddress ipAddress = IPAddress.Loopback;

        int serverPort = ServerPort;
        MsrpUri ClientMsrpUri = new MsrpUri(SIPSchemesEnum.msrp, "Client", ipAddress, ClientPort);
        MsrpUri ServerMsrpUri = new MsrpUri(SIPSchemesEnum.msrp, "Server", ipAddress, serverPort);

        MsrpConnection msrpServer = MsrpConnection.CreateAsServer(ServerMsrpUri, ClientMsrpUri, null);
        msrpServer.Start();

        MsrpConnection msrpClient = MsrpConnection.CreateAsClient(ClientMsrpUri, ServerMsrpUri, null);
        msrpClient.MsrpConnectionEstablished += OnClientConnectionEstablished;        
        msrpClient.Start();

        MsrpClientConnectionEstablished.Wait(ShortMessageTimeoutMs);
        Assert.True(MsrpClientConnectionEstablished.IsSet == true, "Client connection timeout");
        Assert.True(ClientConnectionIsPassive == false, "ClientConnectionIsPassive is wrong");
        Assert.True(ClientConnectionRemoteMsrpUri.uri.ToSIPEndPoint().GetIPEndPoint().Port == serverPort,
            "The server port number is wrong");
        Assert.True(ClientConnectionRemoteMsrpUri.SessionID == ServerMsrpUri.SessionID, "SessionID mismatch");

        msrpClient.Shutdown();
        msrpServer.Shutdown();
    }

    private ManualResetEventSlim MsrpClientConnectionEstablished = new ManualResetEventSlim(false);
    private bool ClientConnectionIsPassive = false;
    private MsrpUri ClientConnectionRemoteMsrpUri = null;
    
    private void OnClientConnectionEstablished(bool ConnectionIsPassive, MsrpUri RemoteMsrpUri)
    {
        ClientConnectionIsPassive = ConnectionIsPassive;
        ClientConnectionRemoteMsrpUri = RemoteMsrpUri;
        MsrpClientConnectionEstablished.Set();
    }

    /// <summary>
    /// Tests the MsrpConnectionEstablished event of a MSRP server 
    /// </summary>
    [Fact]
    public void ServerConnectionEstablished()
    {
        IPAddress ipAddress = IPAddress.Loopback;

        int clientPort = ClientPort;
        MsrpUri ClientMsrpUri = new MsrpUri(SIPSchemesEnum.msrp, "Client", ipAddress, clientPort);
        MsrpUri ServerMsrpUri = new MsrpUri(SIPSchemesEnum.msrp, "Server", ipAddress, ServerPort);

        MsrpConnection msrpServer = MsrpConnection.CreateAsServer(ServerMsrpUri, ClientMsrpUri, null);
        msrpServer.MsrpConnectionEstablished += OnServerConnectionEstablished;
        msrpServer.Start();

        MsrpConnection msrpClient = MsrpConnection.CreateAsClient(ClientMsrpUri, ServerMsrpUri, null);
        msrpClient.Start();

        ServerConnectionEstablishedEvent.Wait(ShortMessageTimeoutMs);
        Assert.True(ServerConnectionEstablishedEvent.IsSet == true, "Server connection timeout");
        Assert.True(ServerConnectionIsPassive == true, "ServerConnectionIsPassive is wrong");
        Assert.True(ServerRemoteMsrpUri.uri.ToSIPEndPoint().GetIPEndPoint().Port == clientPort,
            "The client port number is wrong");
        Assert.True(ServerRemoteMsrpUri.SessionID == ClientMsrpUri.SessionID, "SessionID mismatch");

        msrpClient.Shutdown();
        msrpServer.Shutdown();
    }

    private ManualResetEventSlim ServerConnectionEstablishedEvent = new ManualResetEventSlim(false);
    private bool ServerConnectionIsPassive = false;
    private MsrpUri ServerRemoteMsrpUri = null;

    private void OnServerConnectionEstablished(bool ConnectionIsPassive, MsrpUri RemoteMsrpUri)
    {
        ServerConnectionIsPassive = ConnectionIsPassive;
        ServerRemoteMsrpUri = RemoteMsrpUri;
        ServerConnectionEstablishedEvent.Set();
    }

    /// <summary>
    /// Tests the ReportReceived event of a MSRP client. In this test, the client sends a MSRP message to
    /// the server, the server sends a success REPORT request to the client and the client fires the
    /// ReportReceivedEvent.
    /// </summary>
    [Fact]
    public void ClientSuccessReport()
    {
        IPAddress ipAddress = IPAddress.Loopback;
        MsrpUri ClientMsrpUri = new MsrpUri(SIPSchemesEnum.msrp, "Client", ipAddress, ClientPort);
        MsrpUri ServerMsrpUri = new MsrpUri(SIPSchemesEnum.msrp, "Server", ipAddress, ServerPort);

        MsrpConnection msrpServer = MsrpConnection.CreateAsServer(ServerMsrpUri, ClientMsrpUri, null);
        // Must hook the MsrpMessageReceived event so that a success REPORT request is generated
        msrpServer.MsrpMessageReceived += (contentType, content, from) => { };
        msrpServer.Start();

        MsrpConnection msrpClient = MsrpConnection.CreateAsClient(ClientMsrpUri, ServerMsrpUri, null);
        ManualResetEventSlim ReportReceivedEvent = new ManualResetEventSlim(false);
        string ClientMessageId = null;
        int ReportStatusCode = 0;
        msrpClient.ReportReceived += (messageId, totalBytes, statusCode, statusText) => 
        {
            ClientMessageId = messageId;
            ReportStatusCode = statusCode;
            ReportReceivedEvent.Set();
        };

        msrpClient.Start();
        string MessageId = MsrpMessage.NewRandomID();
        byte[] MsgBytes = Encoding.UTF8.GetBytes("Hello");
        msrpClient.SendMsrpMessage("text/plain", MsgBytes, MessageId);

        ReportReceivedEvent.Wait(ShortMessageTimeoutMs);
        Assert.True(ReportReceivedEvent.IsSet == true, "Client REPORT timeout");
        Assert.True(ClientMessageId == MessageId, "MessageId mismatch");
        Assert.True(ReportStatusCode == 200, "The status code is wrong");
    }

    /// <summary>
    /// Tests the ReportReceived event of a MSRP client for the failure. In this test, the client sends a 
    /// MSRP message to the server, the server sends a failure REPORT request to the client and the
    /// client fires the ReportReceivedEvent.
    /// </summary>
    [Fact]
    public void ClientFailureReport()
    {
        IPAddress ipAddress = IPAddress.Loopback;
        MsrpUri ClientMsrpUri = new MsrpUri(SIPSchemesEnum.msrp, "Client", ipAddress, ClientPort);
        MsrpUri ServerMsrpUri = new MsrpUri(SIPSchemesEnum.msrp, "Server", ipAddress, ServerPort);

        MsrpConnection msrpServer = MsrpConnection.CreateAsServer(ServerMsrpUri, ClientMsrpUri, null);
        // Do not hook the MsrpMessageReceived event so that a failure REPORT request is generated
        msrpServer.Start();

        MsrpConnection msrpClient = MsrpConnection.CreateAsClient(ClientMsrpUri, ServerMsrpUri, null);
        ManualResetEventSlim ReportReceivedEvent = new ManualResetEventSlim(false);
        string ClientMessageId = null;
        int ReportStatusCode = 0;
        msrpClient.ReportReceived += (messageId, totalBytes, statusCode, statusText) =>
        {
            ClientMessageId = messageId;
            ReportStatusCode = statusCode;
            ReportReceivedEvent.Set();
        };

        msrpClient.Start();
        string MessageId = MsrpMessage.NewRandomID();
        byte[] MsgBytes = Encoding.UTF8.GetBytes("Hello");
        msrpClient.SendMsrpMessage("text/plain", MsgBytes, MessageId);

        ReportReceivedEvent.Wait(ShortMessageTimeoutMs);
        Assert.True(ReportReceivedEvent.IsSet == true, "Client REPORT timeout");
        Assert.True(ClientMessageId == MessageId, "MessageId mismatch");
        Assert.True(ReportStatusCode == 503, "The status code is wrong");
    }

    [Fact]
    public void ServerSuccessReport()
    {
        IPAddress ipAddress = IPAddress.Loopback;
        MsrpUri ClientMsrpUri = new MsrpUri(SIPSchemesEnum.msrp, "Client", ipAddress, ClientPort);
        MsrpUri ServerMsrpUri = new MsrpUri(SIPSchemesEnum.msrp, "Server", ipAddress, ServerPort);

        MsrpConnection msrpServer = MsrpConnection.CreateAsServer(ServerMsrpUri, ClientMsrpUri, null);
        ManualResetEventSlim ReportReceivedEvent = new ManualResetEventSlim(false);
        string ServerMessageId = null;
        int ReportStatusCode = 0;
        msrpServer.ReportReceived += (messageId, totalBytes, statusCode, statusText) =>
        {
            ServerMessageId = messageId;
            ReportStatusCode = statusCode;
            ReportReceivedEvent.Set();
        };
        msrpServer.Start();

        MsrpConnection msrpClient = MsrpConnection.CreateAsClient(ClientMsrpUri, ServerMsrpUri, null);
        // Hook the MsrpMessageReceived event so that a success REPORT request is sent to the server
        msrpClient.MsrpMessageReceived += (contentType, content, from) => { };
        msrpClient.Start();

        string MessageId = MsrpMessage.NewRandomID();
        byte[] MsgBytes = Encoding.UTF8.GetBytes("Hello");
        msrpServer.SendMsrpMessage("text/plain", MsgBytes, MessageId);

        ReportReceivedEvent.Wait(ShortMessageTimeoutMs);
        Assert.True(ReportReceivedEvent.IsSet == true, "Server REPORT timeout");
        Assert.True(ServerMessageId == MessageId, "MessageId mismatch");
        Assert.True(ReportStatusCode == 200, "The status code is wrong");
    }

    [Fact]
    public void ServerFailureReport()
    {
        IPAddress ipAddress = IPAddress.Loopback;
        MsrpUri ClientMsrpUri = new MsrpUri(SIPSchemesEnum.msrp, "Client", ipAddress, ClientPort);
        MsrpUri ServerMsrpUri = new MsrpUri(SIPSchemesEnum.msrp, "Server", ipAddress, ServerPort);

        MsrpConnection msrpServer = MsrpConnection.CreateAsServer(ServerMsrpUri, ClientMsrpUri, null);
        ManualResetEventSlim ReportReceivedEvent = new ManualResetEventSlim(false);
        string ServerMessageId = null;
        int ReportStatusCode = 0;
        msrpServer.ReportReceived += (messageId, totalBytes, statusCode, statusText) =>
        {
            ServerMessageId = messageId;
            ReportStatusCode = statusCode;
            ReportReceivedEvent.Set();
        };
        msrpServer.Start();

        MsrpConnection msrpClient = MsrpConnection.CreateAsClient(ClientMsrpUri, ServerMsrpUri, null);
        // Do not hook the MsrpMessageReceived event so that a failure REPORT request is sent to the server
        msrpClient.Start();

        string MessageId = MsrpMessage.NewRandomID();
        byte[] MsgBytes = Encoding.UTF8.GetBytes("Hello");
        msrpServer.SendMsrpMessage("text/plain", MsgBytes, MessageId);

        ReportReceivedEvent.Wait(ShortMessageTimeoutMs);
        Assert.True(ReportReceivedEvent.IsSet == true, "Server REPORT timeout");
        Assert.True(ServerMessageId == MessageId, "MessageId mismatch");
        Assert.True(ReportStatusCode == 503, "The status code is wrong");
    }
}
