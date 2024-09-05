using SipLib.Channels;
using SipLib.Core;
using SipLib.Network;
using SipLib.Transactions;
using System.Net;

namespace RttServer;

internal class Program
{
    private const int localPort = 5062;

    static async Task Main(string[] args)
    {
        SIPTCPChannel Channel;
        SipTransport sipTransport;
        string UserName = "RttServer";
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

        RttUas rttUas = new RttUas(sipTransport, "RttServer");
        rttUas.InviteReceived += OnInviteReceived;
        rttUas.CharactersReceived += OnCharactersReceived;
        rttUas.ByeReceived += OnByeReceived;
        rttUas.Error += OnError;

        rttUas.Start();

        Console.WriteLine($"Listening on {Channel.SIPChannelContactURI} ...");
        Console.WriteLine("Press ESC to exit the program");

        ConsoleKeyInfo cki;

        while (true)
        {
            cki = Console.ReadKey(true);
            if (cki.Key == ConsoleKey.Escape)
                break;
            else
            {
                rttUas.SendRtt(cki.KeyChar.ToString());
                
                if (cki.Key != ConsoleKey.Enter)
                    Console.Write(cki.KeyChar);
                else
                    Console.WriteLine();
            }
        }

        await rttUas.Stop();
        sipTransport.Shutdown();
    }

    private static void OnInviteReceived(string from)
    {
        Console.WriteLine(from);
        Console.WriteLine("\nStart typing characters. Press ENTER to indicate to the calling party " +
            "that you are done typing \nand waiting for a response\n");
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
        Console.WriteLine("BYE received");
    }

    private static void OnError(string ErrorMsg)
    {
        Console.WriteLine(ErrorMsg);
    }
}
