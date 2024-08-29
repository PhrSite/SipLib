using SipLib.Channels;
using SipLib.Core;
using SipLib.Network;
using SipLib.Transactions;
using System.Net;

namespace RttClient;

internal class Program
{
    private const int localPort = 5060;
    private const int remotePort = 5062;

    static async Task Main(string[] args)
    {
        SIPTCPChannel Channel;
        SipTransport sipTransport;
        string UserName = "RttClient";
        IPAddress localAddress;

        //List<IPAddress> addresses = IpUtils.GetIPv4Addresses();
        List<IPAddress> addresses = IpUtils.GetIPv6Addresses();

        if (addresses == null || addresses.Count == 0)
        {
            Console.WriteLine("Error: No IPv4 addresses available");
            return;
        }

        localAddress = addresses[0];    // Pick the first available IPv4 address to listen on
        IPEndPoint localIPEndPoint = new IPEndPoint(localAddress, localPort);
        Console.WriteLine($"Local  IPEndPoint = {localIPEndPoint}");
        Channel = new SIPTCPChannel(localIPEndPoint, UserName);
        sipTransport = new SipTransport(Channel);
        sipTransport.Start();
        IPEndPoint remoteIPEndPoint = new IPEndPoint(localAddress, remotePort);

        ConsoleKeyInfo cki;
        Console.Title = "RttClient";

        RttUac rttUac = new RttUac(sipTransport, "RttClient");
        rttUac.InterimResponseReceived += OnInterimResponseReceived;
        rttUac.ConnectionTimeout += OnConnectionTimeout;
        rttUac.CallRejected += OnCallRejected;
        rttUac.OkReceived += OnOkReceived;
        rttUac.CharactersReceived += OnCharactersReceived;
        rttUac.ByeReceived += OnByeReceived;
        rttUac.Error += OnError;

        rttUac.Start();

        Console.WriteLine("Connecting...");
        Console.WriteLine("Press ESC to exit the program");
        rttUac.Call(remoteIPEndPoint);

        while (true)
        {
            cki = Console.ReadKey(true);
            if (cki.Key == ConsoleKey.Escape)
                break;
            else
            {

                rttUac.SendRtt(cki.KeyChar.ToString());
                if (cki.Key != ConsoleKey.Enter)
                    Console.Write(cki.KeyChar);
                else
                    Console.WriteLine();
            }
        }

        await rttUac.Stop();
        sipTransport.Shutdown();
    }

    private static void OnInterimResponseReceived(SIPResponseStatusCodesEnum status)
    {
        Console.WriteLine($"Received: {status}");
    }

    private static void OnConnectionTimeout()
    {
        Console.WriteLine("No response received. Press ESC to exit the program");
    }

    private static void OnCallRejected(SIPResponseStatusCodesEnum status)
    {
        Console.WriteLine($"Call rejected. Reason = {status}");
    }

    private static void OnOkReceived()
    {
        Console.WriteLine("200 OK received");
        Console.WriteLine("\nStart typing characters. Press ENTER to indicate to the called party " +
            "that you are done typing and \nwaiting for a response\n");
    }

    private static void OnCharactersReceived(string RxChars, string Source)
    {
        Console.Write(RxChars);
        if (RxChars[0] == '\r')
            Console.WriteLine();

        if (RxChars.Length == 1 && RxChars[0] == '\b')
        {
            (int OrigLeft, int OrigTop) = Console.GetCursorPosition();
            Console.Write(' ');
            int Left, Top;
            (Left, Top) = Console.GetCursorPosition();
            if (Left > 0)
                Console.SetCursorPosition(Left - 1, Top);
            else
            {
                Left = OrigLeft - 1;
                Top = Top - 1;
                Console.SetCursorPosition(Left, Top);
            }
        }
    }

    private static void OnByeReceived()
    {
        Console.WriteLine("BYE received. Press ESC to exit the program");
    }

    private static void OnError(string errorMsg)
    {
        Console.WriteLine(errorMsg);
    }
}
