/////////////////////////////////////////////////////////////////////////////////////
//  File:   DtlsSrtpUnitTests.cs                                    11 Dec 23 PHR
/////////////////////////////////////////////////////////////////////////////////////


namespace SipLibUnitTests.DtlsSrtp;
using SipLibUnitTests.RtpCrypto;

using SipLib.Dtls;
using SipLib.Rtp;

using System.Security.Cryptography;
using Org.BouncyCastle.Crypto.Tls;
using Org.BouncyCastle.Crypto;

[Trait("Category", "unit")]
public class DtlsSrtpUnitTests
{
    [Fact]
    public void TestLoopbackMethod()
    {
        Certificate selfSigned;
        AsymmetricKeyParameter asymmetricKeyParameter;
        (selfSigned, asymmetricKeyParameter) = DtlsUtils.CreateSelfSignedTlsCert();
        Assert.True(selfSigned != null, "selfSigned is null");
        Assert.True(asymmetricKeyParameter != null, "asymmetricKeyParameter is null");

        DtlsSrtpClient dtlsClient = new DtlsSrtpClient(selfSigned, asymmetricKeyParameter);
        DtlsSrtpServer dtlsServer = new DtlsSrtpServer(selfSigned, asymmetricKeyParameter);

        DtlsSrtpTransport dtlsClientTransport = new DtlsSrtpTransport(dtlsClient);
        dtlsClientTransport.TimeoutMilliseconds = 5000;
        DtlsSrtpTransport dtlsServerTransport = new DtlsSrtpTransport(dtlsServer);
        dtlsServerTransport.TimeoutMilliseconds = 5000;

        dtlsClientTransport.OnDataReady += (buf) =>
        {   // Send the client's data to the server
            dtlsServerTransport.WriteToRecvStream(buf);
        };

        dtlsServerTransport.OnDataReady += (buf) =>
        {   // Send the server's data to the client.
            dtlsClientTransport.WriteToRecvStream(buf);
        };

        dtlsClientTransport.OnAlert += DtlsClientTransport_OnAlert;
        dtlsServerTransport.OnAlert += DtlsServerTransport_OnAlert;

        Task<bool> serverTask = Task.Run<bool>(() => dtlsServerTransport.DoHandshake(out _));
        Task<bool> clientTask = Task.Run<bool>(() => dtlsClientTransport.DoHandshake(out _));
        bool didComplete = Task.WaitAll(new Task[] { serverTask, clientTask }, 5000);

        if (didComplete == false)
            Assert.True(didComplete == true, "didComplete is false");

        Assert.True(dtlsServerTransport.IsHandshakeComplete() == true && 
            dtlsServerTransport.IsHandshakeFailed() == false, "The DTLS server handshake failed.");
        Assert.True(dtlsClientTransport.IsHandshakeComplete() == true &&
            dtlsClientTransport.IsHandshakeFailed() == false, "The DTLS client handshake failed.");

        RandomNumberGenerator Rng = RandomNumberGenerator.Create();
        int PayloadLength = 160;
        int RtpPcktLength = RtpPacket.MIN_PACKET_LENGTH + PayloadLength;
        byte[] Pckt = new byte[RtpPcktLength];
        RtpPacket rtpPacket = new RtpPacket(Pckt);
        Random Rnd = new Random();
        rtpPacket.SSRC = (uint)Rnd.Next();
        rtpPacket.SequenceNumber = 0;

        byte[] encryptedPckt;
        byte[] decryptedPckt;

        bool AreEqual = true;
        int i = 0;
        int NumPackets = 100000;    // Allow for at least 1 roll over of the SequenceNumber
        for (i = 0; i < NumPackets; i++)
        {
            encryptedPckt = dtlsClientTransport.ProtectRTP(Pckt, 0, Pckt.Length);
            decryptedPckt = dtlsServerTransport.UnprotectRTP(encryptedPckt, 0, encryptedPckt.Length);
            AreEqual = SrtpUnitTests.ArraysEqual(decryptedPckt, Pckt);

            Assert.True(AreEqual == true, $"AreEqual = {AreEqual}, i = {i}, Seq = {rtpPacket.SequenceNumber}");
            rtpPacket.SequenceNumber += 1;
        }

        Assert.True(AreEqual == true, "AreEqual is false");
    }

    private void DtlsServerTransport_OnAlert(AlertLevelsEnum arg1, AlertTypesEnum arg2, string arg3)
    {
        Assert.True(arg1 == AlertLevelsEnum.Fatal);
    }

    private void DtlsClientTransport_OnAlert(AlertLevelsEnum arg1, AlertTypesEnum arg2, string arg3)
    {
        Assert.True(arg1 == AlertLevelsEnum.Fatal);
    }

}
