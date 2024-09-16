/////////////////////////////////////////////////////////////////////////////////////
//  File:   Program.cs                                              4 Sep 24 PHR
/////////////////////////////////////////////////////////////////////////////////////

using SipLib.Channels;
using SipLib.Network;
using SipLib.Transactions;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace MsrpServer;

/// <summary>
/// Main for the MsrpServer test program
/// </summary>
internal class Program
{
    private const int localPort = 5062;

    static async Task Main(string[] args)
    {
        X509Certificate2 myCertificate = new X509Certificate2("MsrpServerCertificate.pfx", "MsrpServer");

        SIPTCPChannel Channel;
        SipTransport sipTransport;
        string UserName = "MsrpServer";
        IPAddress localAddress;

        Console.Title = UserName;

        //List<IPAddress> addresses = IpUtils.GetIPv4Addresses();
        List<IPAddress> addresses = IpUtils.GetIPv6Addresses();
        if (addresses == null || addresses.Count == 0)
        {
            Console.WriteLine("Error: No IPv6 addresses available");
            return;
        }

        localAddress = addresses[0];    // Pick the first available IPv4 address to listen on
        IPEndPoint localIPEndPoint = new IPEndPoint(localAddress, localPort);
        Channel = new SIPTCPChannel(localIPEndPoint, UserName);
        sipTransport = new SipTransport(Channel);
        sipTransport.Start();

        MsrpUas msrpUas = new MsrpUas(sipTransport, UserName, myCertificate);
        msrpUas.Error += OnError;
        msrpUas.ByeReceived += OnByeReceived;
        msrpUas.InviteReceived += OnInviteReceived;
        msrpUas.TextMessageReceived += OnTextMessageReceived;
        msrpUas.Start();

        Console.WriteLine($"Listening on {Channel.SIPChannelContactURI} ...");
        Console.WriteLine("Type quit to exit the program");

        string? strLine;
        while (true)
        {
            strLine = Console.ReadLine();
            if (string.IsNullOrEmpty(strLine))
                continue;

            if (strLine == "quit")
                break;

            msrpUas.Send(strLine);
        }

        await msrpUas.Stop();
        sipTransport.Shutdown();
    }

    private static void OnTextMessageReceived(string message, string from)
    {
        Console.WriteLine($"From {from}: {message.Replace("\r\n", "")}");
    }

    private static void OnInviteReceived(string from)
    {
        Console.WriteLine($"INVITE received from: {from}");
        Console.WriteLine("\nType a message and press Enter to send it. Type quit to exit the program\n");
    }

    private static void OnByeReceived()
    {
        Console.WriteLine("BYE received. Type quit to exit the program");
    }

    private static void OnError(string errorMsg)
    {
        Console.WriteLine(errorMsg);
    }
}
