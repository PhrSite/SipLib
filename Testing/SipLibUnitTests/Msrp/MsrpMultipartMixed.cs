/////////////////////////////////////////////////////////////////////////////////////
//  File:   MsrpMultipartMixed.cs                                   14 Mar 24 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLibUnitTests.Msrp;
using SipLib.Body;
using SipLib.Core;
using SipLib.Msrp;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;

public class MsrpMultipartMixed
{
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
    private byte[] PicBytes = null;

    private ManualResetEventSlim ServerMessageReceivedEvent = new ManualResetEventSlim(false);
    private string ServerReceivedContentType = null;
    private byte[] ServerReceivedMessageBytes = null;

    private ManualResetEventSlim ClientMessageReceivedEvent = new ManualResetEventSlim(false);
    private string ClientReceivedContentType = null;
    private byte[] ClientReceivedMessageBytes = null;

    private const int ShortMessageTimeoutMs = 1000;
    private const int LongMessageTimeoutMs = 5000;

    /// <summary>
    /// Specifies the path to the files containing the test MSRP messages. Change this if the project
    /// location or the location of the test files change.
    /// </summary>
    private const string Path = @"..\..\..\MsrpMessages\";

    [Fact]
    public void TestMsrpMultipartMixed()
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

        // Send a multipart/mixed MSRP message from the client to the server
        CpimMessage cpim = new CpimMessage();
        cpim.From = new SIPUserField("Client", new SIPURI(SIPSchemesEnum.sips, ipAddress, 7000), null);
        cpim.To.Add(new SIPUserField("Server", new SIPURI(SIPSchemesEnum.sips, ipAddress, 8000), null));
        cpim.ContentType = "text/plain";
        cpim.Subject.Add("Car crash picture");
        string cpimMessage = "Here is a picture of my car crash";
        cpim.Body = Encoding.UTF8.GetBytes(cpimMessage);

        List<MessageContentsContainer> messages = new List<MessageContentsContainer>();
        MessageContentsContainer cpimMcc = new MessageContentsContainer();
        cpimMcc.ContentType = "message/CPIM";
        cpimMcc.IsBinaryContents = true;
        cpimMcc.BinaryContents = cpim.ToByteArray();
        messages.Add(cpimMcc);

        MessageContentsContainer imageMcc = new MessageContentsContainer();
        PicBytes = File.ReadAllBytes($"{Path}CarCrashPicture.jpg");
        imageMcc.ContentType = "image/jpeg";
        imageMcc.IsBinaryContents = true;
        imageMcc.BinaryContents = PicBytes;
        messages.Add(imageMcc);

        string strBoundary = "boundary1";
        byte[] MultipartBytes = MultipartBinaryBodyBuilder.ToByteArray(messages, strBoundary);
        MsrpClient.SendMsrpMessage($"multipart/mixed;boundary={strBoundary}", MultipartBytes);
        bool Signaled = ServerMessageReceivedEvent.Wait(LongMessageTimeoutMs);
        Assert.True(Signaled == true, "Signaled is false");

        Assert.True(ServerMessageReceivedEvent.IsSet == true, "ServerMessageReceivedEvent.IsSet is false");
        Assert.True(ServerReceivedContentType.Contains("multipart/mixed") == true,
            "multipart/mixed ServerReceivedContentType is wrong");

        List<MessageContentsContainer> RecvContents = BodyParser.ProcessMultiPartContents(
            ServerReceivedMessageBytes, ServerReceivedContentType);
        Assert.True(RecvContents.Count == 2, "RecvContents.Count is wrong");
        Assert.True(RecvContents[0].ContentType == "message/CPIM", "The first ContentType is wrong");
        Assert.True(RecvContents[0].IsBinaryContents == false, "The first IsBinaryContents is wrong");
        CpimMessage RecvCpim = CpimMessage.ParseCpimBytes(Encoding.UTF8.GetBytes(RecvContents[0].
            StringContents));
        Assert.True(RecvCpim != null, "RecvCpim is null");
        Assert.True(RecvCpim.ContentType == "text/plain", "RecvCpim.ContentType is wrong");
        string RecvCpimText = Encoding.UTF8.GetString(RecvCpim.Body);
        Assert.True(RecvCpimText == cpimMessage, "The received CPIM contents are wrong");

        Assert.True(RecvContents[1].ContentType == "image/jpeg", "The second ContentType is wrong");
        byte[] RecvPicBytes = RecvContents[1].BinaryContents;
        Assert.True(RecvPicBytes.Length == PicBytes.Length, "The received image length is wrong");
        for (int i = 0; i < PicBytes.Length; i++)
            Assert.True(RecvPicBytes[i] == PicBytes[i], $"Image contents mismatch at i = {i}");

        MsrpClient.Shutdown();
        MsrpServer.Shutdown();

    }

    private void OnServerMessageReceived(string ContentType, byte[] Contents)
    {
        ServerReceivedContentType = ContentType;
        ServerReceivedMessageBytes = Contents;
        ServerMessageReceivedEvent.Set();
    }

    private void OnClientMessageReceived(string ContentType, byte[] Contents)
    {
        ClientReceivedContentType = ContentType;
        ClientReceivedMessageBytes = Contents;
        ClientMessageReceivedEvent.Set();
    }


}
