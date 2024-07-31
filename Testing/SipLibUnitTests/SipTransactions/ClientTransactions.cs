/////////////////////////////////////////////////////////////////////////////////////
//  File:   ClientTransactions.cs                                   14 Sep 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLibUnitTests.SipTransactions;
using System.Net;
using SipLib.Channels;
using SipLib.Core;
using SipLib.Network;
using SipLib.Sdp;
using SipLib.Transactions;

/// <summary>
/// This class performs unit tests for several SIP transactions from the perspective of the SIP client.
/// It uses the ServerTransactionSimulator class to simulate the server-side of the transaction.
/// ServerTransactionSimulator uses the ServerNonInviteTransaction and ServerInviteTransaction classes
/// so the server-side transactions are also exercised.
/// </summary>
[Trait("Category", "unit")]
public class ClientTransactions
{
    // Note: Tests are run in parallel so each test must use unique port numbers, so add a sequential value
    // to the port numbers starting with the following values for each unit test.
    private const int ClientPort = 5060;
    private const int ServerPort = 6060;

    [Fact]
    public async Task ClientNonInviteTransaction_Success()
    {
        IPAddress ipAddress = GetIpAddress(true);
        IPEndPoint ClientIpe = new IPEndPoint(ipAddress, ClientPort);
        IPEndPoint ServerIpe = new IPEndPoint(ipAddress, ServerPort);
        SIPURI ServerUri = new SIPURI(SIPSchemesEnum.sip, ServerIpe.Address, ServerPort);
        ServerUri.User = "Server";

        ServerTransactionSimulator Server = new ServerTransactionSimulator(ServerIpe, true, true);
        SIPTCPChannel ClientChannel = new SIPTCPChannel(ClientIpe, "Client");
        SipTransport Transport = new SipTransport(ClientChannel);
        Transport.Start();

        SIPURI reqUri = SIPURI.ParseSIPURI("urn:service:sos");
        SIPRequest OptionsReq = SIPRequest.CreateBasicRequest(SIPMethodsEnum.OPTIONS, reqUri, ServerUri,
            null, ClientChannel.SIPChannelContactURI, null);

        ClientNonInviteTransaction Cnit = Transport.StartClientNonInviteTransaction(OptionsReq, ServerIpe,
            null, 500);

        await Cnit.WaitForCompletionAsync();
        Assert.True(Cnit.TerminationReason == TransactionTerminationReasonEnum.FinalResponseReceived,
            "Incorrect OPTIONS transaction termination reason");

        await Task.Delay(200);
        // Make sure that the transactions are properly terminated
        Assert.True(Transport.TransactionCount == 0, "The client transport transaction count is not 0");
        Assert.True(Server.TransactionCount == 0, "The server transport transaction count is no 0");

        Transport.Shutdown();
        Server.Shutdown();
    }

    [Fact]
    public async Task ClientNonInviteTransaction_Failure()
    {
        IPAddress ipAddress = GetIpAddress(true);
        IPEndPoint ClientIpe = new IPEndPoint(ipAddress, ClientPort + 1);
        IPEndPoint ServerIpe = new IPEndPoint(ipAddress, ServerPort + 1);

        SIPURI ServerUri = new SIPURI(SIPSchemesEnum.sip, ServerIpe.Address, ServerPort);
        ServerUri.User = "Server";

        // Causes the server simulator to not answer the OPTIONS request
        ServerTransactionSimulator Server = new ServerTransactionSimulator(ServerIpe, true, false);
        SIPTCPChannel ClientChannel = new SIPTCPChannel(ClientIpe, "Client");
        SipTransport Transport = new SipTransport(ClientChannel);
        Transport.Start();

        SIPURI reqUri = SIPURI.ParseSIPURI("urn:service:sos");
        SIPRequest OptionsReq = SIPRequest.CreateBasicRequest(SIPMethodsEnum.OPTIONS, reqUri, ServerUri,
            null, ClientChannel.SIPChannelContactURI, null);
        ClientNonInviteTransaction Cnit = Transport.StartClientNonInviteTransaction(OptionsReq, ServerIpe,
            null, 500);

        await Cnit.WaitForCompletionAsync();
        Assert.True(Cnit.TerminationReason == TransactionTerminationReasonEnum.NoResponseReceived,
            "Incorrect OPTIONS transaction termination reason");

        await Task.Delay(200);
        // Make sure that the transactions are properly terminated
        Assert.True(Transport.TransactionCount == 0, "The client transport transaction count is not 0");
        Assert.True(Server.TransactionCount == 0, "The server transport transaction count is no 0");

        Transport.Shutdown();
        Server.Shutdown();
    }


    [Fact]
    public async Task ClientInviteTransaction_Success()
    {
        IPAddress ipAddress = GetIpAddress(true);
        IPEndPoint ClientIpe = new IPEndPoint(ipAddress, ClientPort + 2);
        IPEndPoint ServerIpe = new IPEndPoint(ipAddress, ServerPort + 2);

        SIPURI ServerUri = new SIPURI(SIPSchemesEnum.sip, ServerIpe.Address, ServerPort);
        ServerUri.User = "Server";

        // The server will send a Trying, Ringing and then an OK response to the INVITE request sent
        // to it by the client.
        ServerTransactionSimulator Server = new ServerTransactionSimulator(ServerIpe, true, true);
        SIPTCPChannel ClientChannel = new SIPTCPChannel(ClientIpe, "Client");
        SipTransport Transport = new SipTransport(ClientChannel);
        Transport.Start();

        SIPURI reqUri = SIPURI.ParseSIPURI("urn:service:sos");
        SIPRequest Invite = SIPRequest.CreateBasicRequest(SIPMethodsEnum.INVITE, reqUri, ServerUri,
            null, ClientChannel.SIPChannelContactURI, null);
        Sdp AudioSdp = SdpUtils.BuildSimpleAudioSdp(ipAddress, 6000, "Client");
        Invite.Header.ContentType = "application/sdp";
        Invite.Body = AudioSdp.ToString();
        Invite.Header.ContentLength = Invite.Body.Length;
        ClientInviteTransaction Cit = Transport.StartClientInvite(Invite, ServerIpe, null, null);

        await Cit.WaitForCompletionAsync();
        Assert.True(Cit.TerminationReason == TransactionTerminationReasonEnum.OkReceived,
            "Client INVITE transaction TerminationReason is wrong");
        Assert.True(Cit.LastReceivedResponse.Status == SIPResponseStatusCodesEnum.Ok,
            "Client INVITE transaction LastReceivedResponse.Status is wrong");

        SIPRequest AckReq = SipUtils.BuildAckRequest(Cit.LastReceivedResponse, Transport.SipChannel);
        Transport.SendSipRequest(AckReq, Cit.RemoteEndPoint);
        await Task.Delay(100);
        SIPRequest ByeReq = SipUtils.BuildByeRequest(Invite, Transport.SipChannel, Cit.RemoteEndPoint,
            false, Invite.Header.CSeq, Cit.LastReceivedResponse);
        ClientNonInviteTransaction Cnit = Transport.StartClientNonInviteTransaction(ByeReq, Cit.RemoteEndPoint,
            null, 1000);

        await Task.Delay(200);
        // Make sure that the transactions are properly terminated
        Assert.True(Transport.TransactionCount == 0, "The client transport transaction count is not 0");
        Assert.True(Server.TransactionCount == 0, "The server transport transaction count is no 0");

        Transport.Shutdown();
        Server.Shutdown();
    }

    [Fact]
    public async Task ClientInviteCancelTransaction_Success()
    {
        IPAddress ipAddress = GetIpAddress(true);
        IPEndPoint ClientIpe = new IPEndPoint(ipAddress, ClientPort + 3);
        IPEndPoint ServerIpe = new IPEndPoint(ipAddress, ServerPort + 3);

        SIPURI ServerUri = new SIPURI(SIPSchemesEnum.sip, ServerIpe.Address, ServerPort);
        ServerUri.User = "Server";

        // The server will not send a 200 OK response to the INVITE request
        ServerTransactionSimulator Server = new ServerTransactionSimulator(ServerIpe, false, true);
        SIPTCPChannel ClientChannel = new SIPTCPChannel(ClientIpe, "Client");
        SipTransport Transport = new SipTransport(ClientChannel);
        Transport.Start();

        SIPURI reqUri = SIPURI.ParseSIPURI("urn:service:sos");
        SIPRequest Invite = SIPRequest.CreateBasicRequest(SIPMethodsEnum.INVITE, reqUri, ServerUri,
            null, ClientChannel.SIPChannelContactURI, null);
        Sdp AudioSdp = SdpUtils.BuildSimpleAudioSdp(ipAddress, 6000, "Client");
        Invite.Header.ContentType = "application/sdp";
        Invite.Body = AudioSdp.ToString();
        Invite.Header.ContentLength = Invite.Body.Length;

        ClientInviteTransaction Cit = Transport.StartClientInvite(Invite, ServerIpe, null, null);

        await Task.Delay(100);
        // Now send a CANCEL request to the server
        SIPRequest CancelReq = SipUtils.BuildCancelRequest(Invite, Transport.SipChannel, Cit.RemoteEndPoint,
            Invite.Header.CSeq);
        ClientNonInviteTransaction Cnit = Transport.StartClientNonInviteTransaction(CancelReq,
            Cit.RemoteEndPoint, null, 1000);

        await Cnit.WaitForCompletionAsync();
        Assert.True(Cnit.TerminationReason == TransactionTerminationReasonEnum.FinalResponseReceived,
            "Incorrect response to CANCEL request transaction");

        await Cit.WaitForCompletionAsync();
        Assert.True(Cit.TerminationReason == TransactionTerminationReasonEnum.FinalResponseReceived,
            "Incorrect TerminationReason for the INVITE transaction");
        Assert.True(Cit.LastReceivedResponse.Status == SIPResponseStatusCodesEnum.RequestTerminated,
            "Incorrect final response received to the INVITE transacton");

        await Task.Delay(200);
        // Make sure that the transactions are properly terminated
        Assert.True(Transport.TransactionCount == 0, "The client transport transaction count is not 0");
        Assert.True(Server.TransactionCount == 0, "The server transport transaction count is no 0");

        Transport.Shutdown();
        Server.Shutdown();
    }

    [Fact]
    public async Task ClientInviteServerConnectionFails()
    {
        IPAddress ipAddress = GetIpAddress(true);
        IPEndPoint ClientIpe = new IPEndPoint(ipAddress, ClientPort + 4);
        IPEndPoint ServerIpe = new IPEndPoint(ipAddress, ServerPort + 4);

        SIPURI ServerUri = new SIPURI(SIPSchemesEnum.sip, ServerIpe.Address, ServerPort);
        ServerUri.User = "Server";

        // Don't answer the call
        ServerTransactionSimulator Server = new ServerTransactionSimulator(ServerIpe, false, true);

        SIPTCPChannel ClientChannel = new SIPTCPChannel(ClientIpe, "Client");
        SipTransport Transport = new SipTransport(ClientChannel);
        Transport.Start();

        SIPURI reqUri = SIPURI.ParseSIPURI("urn:service:sos");
        SIPRequest Invite = SIPRequest.CreateBasicRequest(SIPMethodsEnum.INVITE, reqUri, ServerUri,
            null, ClientChannel.SIPChannelContactURI, null);
        Sdp AudioSdp = SdpUtils.BuildSimpleAudioSdp(ipAddress, 6000, "Client");
        Invite.Header.ContentType = "application/sdp";
        Invite.Body = AudioSdp.ToString();
        Invite.Header.ContentLength = Invite.Body.Length;
        ClientInviteTransaction Cit = Transport.StartClientInvite(Invite, ServerIpe, null, null);

        // Simulate a network failure or a disconnect by the server
        await Task.Delay(200);
        Server.Shutdown();

        await Cit.WaitForCompletionAsync();
        Assert.True(Cit.TerminationReason == TransactionTerminationReasonEnum.ConnectionFailure,
            "Termination reason is not ConnectionFailure");

        await Task.Delay(200);
        // Make sure that the transactions are properly terminated
        Assert.True(Transport.TransactionCount == 0, "The client transport transaction count is not 0");

        Transport.Shutdown();
    }

    [Fact]
    public async Task ClientInviteClientConnectionFails()
    {
        IPAddress ipAddress = GetIpAddress(true);
        IPEndPoint ClientIpe = new IPEndPoint(ipAddress, ClientPort + 5);
        IPEndPoint ServerIpe = new IPEndPoint(ipAddress, ServerPort + 5);

        SIPURI ServerUri = new SIPURI(SIPSchemesEnum.sip, ServerIpe.Address, ServerPort);
        ServerUri.User = "Server";

        // Don't answer the call
        ServerTransactionSimulator Server = new ServerTransactionSimulator(ServerIpe, false, true);

        SIPTCPChannel ClientChannel = new SIPTCPChannel(ClientIpe, "Client");
        SipTransport Transport = new SipTransport(ClientChannel);
        Transport.Start();

        SIPURI reqUri = SIPURI.ParseSIPURI("urn:service:sos");
        SIPRequest Invite = SIPRequest.CreateBasicRequest(SIPMethodsEnum.INVITE, reqUri, ServerUri,
            null, ClientChannel.SIPChannelContactURI, null);
        Sdp AudioSdp = SdpUtils.BuildSimpleAudioSdp(ipAddress, 6000, "Client");
        Invite.Header.ContentType = "application/sdp";
        Invite.Body = AudioSdp.ToString();
        Invite.Header.ContentLength = Invite.Body.Length;
        ClientInviteTransaction Cit = Transport.StartClientInvite(Invite, ServerIpe, null, null);

        // Simulate a network failure or a disconnect on the client side
        await Task.Delay(500);
        ClientChannel.Close();

        await Cit.WaitForCompletionAsync();
        Assert.True(Cit.TerminationReason == TransactionTerminationReasonEnum.ConnectionFailure,
            "Termination reason is not ConnectionFailure");

        await Task.Delay(200);
        // Make sure that the transactions are properly terminated
        Assert.True(Transport.TransactionCount == 0, "The client transport transaction count is not 0");

        Transport.Shutdown();
    }

    private IPAddress GetIpAddress(bool GetIPv4)
    {
        List<IPAddress> ipAddresses;
        if (GetIPv4 == true)
            ipAddresses = IpUtils.GetIPv4Addresses();
        else
            ipAddresses = IpUtils.GetIPv6Addresses();

        if (ipAddresses.Count == 0)
        {
            Assert.True(ipAddresses.Count > 0, "No IP addresses available");
            return null;
        }
        else
            return ipAddresses[0];
    }
}
