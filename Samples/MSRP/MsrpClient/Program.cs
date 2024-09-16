/////////////////////////////////////////////////////////////////////////////////////
//  File:   Program.cs                                              2 Sep 24 PHR
/////////////////////////////////////////////////////////////////////////////////////

using SipLib.Channels;
using SipLib.Core;
using SipLib.Network;
using SipLib.Transactions;

using System.Net;

namespace MsrpClient;

/// <summary>
/// Main for the MsrpClient test program
/// </summary>
internal class Program
{
    private const int localPort = 5060;
    private const int remotePort = 5062;

    static async Task Main(string[] args)
    {
        SIPTCPChannel Channel;
        SipTransport sipTransport;
        string UserName = "MsrpClient";
        IPAddress localAddress;

        //List<IPAddress> addresses = IpUtils.GetIPv4Addresses();
        List<IPAddress> addresses = IpUtils.GetIPv6Addresses();

        if (addresses == null || addresses.Count == 0)
        {
            Console.WriteLine("Error: No IPv6 addresses available");
            return;
        }

        localAddress = addresses[0];    // Pick the first available IP address to listen on
        IPEndPoint localIPEndPoint = new IPEndPoint(localAddress, localPort);
        Console.WriteLine($"Local  IPEndPoint = {localIPEndPoint}");
        Channel = new SIPTCPChannel(localIPEndPoint, UserName);
        sipTransport = new SipTransport(Channel);
        sipTransport.Start();
        IPEndPoint remoteIPEndPoint = new IPEndPoint(localAddress, remotePort);

        Console.Title = "MsrpClient";
        Console.WriteLine("Connecting...");
        Console.WriteLine("Type 'quit' (without quotes) to exit the program");

        MsrpUac msrpUac = new MsrpUac(sipTransport, UserName);
        msrpUac.TextMessageReceived += OnTextMessageReceived;
        msrpUac.InterimResponseReceived += OnInterimResponseReceived;
        msrpUac.ConnectionTimeout += OnConnectionTimeout;
        msrpUac.CallRejected += OnCallRejected;
        msrpUac.OkReceived += OnOkReceived;
        msrpUac.ByeReceived += OnByeReceived;
        msrpUac.Error += OnError;

        msrpUac.Start();
        msrpUac.Call(remoteIPEndPoint);

        string? strLine;
        while (true)
        {
            strLine = Console.ReadLine();
            if (string.IsNullOrEmpty(strLine))
                continue;

            if (strLine == "quit")
                break;

            msrpUac.Send(strLine);
        }

        await msrpUac.Stop();
        sipTransport.Shutdown();
    }

    private static void OnOkReceived()
    {
        Console.WriteLine("200 OK received");
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


    private static void OnCallRejected(SIPResponseStatusCodesEnum status)
    {
        Console.WriteLine($"Call rejected. Reason = {status}");
    }


    private static void OnConnectionTimeout()
    {
        Console.WriteLine("No response received. Type quit to exit the program");
    }

    private static void OnInterimResponseReceived(SIPResponseStatusCodesEnum status)
    {
        Console.WriteLine($"Received: {status}");
    }

    private static void OnTextMessageReceived(string message, string from)
    {
        Console.WriteLine($"From {from}: {message.Replace("\r\n", "")}");
    }
}
