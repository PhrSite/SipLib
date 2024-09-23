/////////////////////////////////////////////////////////////////////////////////////
//  File:   Program.cs                                              22 Sep 24 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace TestAes256;

using SipLib.Channels;
using SipLib.Core;
using SipLib.Network;
using SipLib.Transactions;
using System.Net;
using SipLib.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Logging;
using SipLib.Logging;


internal class Program
{
    private const int localPort = 5060;
    private const int remotePort = 5060;
    private const string strRemoteIpAddress = "192.168.1.65";
    //private const string strToUserName = "conf-123";

    internal const string LoggingDirectory = @"\var\log\TestAes256";
    private const string LoggingFileName = "TestAes256.log";
    private static LoggingLevelSwitch m_LevelSwitch = new LoggingLevelSwitch();

    static async Task Main(string[] args)
    {
        if (Directory.Exists(LoggingDirectory) == false)
            Directory.CreateDirectory(LoggingDirectory);

        string LoggingPath = Path.Combine(LoggingDirectory, LoggingFileName);
        Logger log = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(m_LevelSwitch)
            .WriteTo.File(LoggingPath, fileSizeLimitBytes: 1000000, retainedFileCountLimit: 5,
            outputTemplate: "{Timestamp:yyyy-MM-ddTHH:mm:ss.ffffffzzz} [{Level}] {Message}{NewLine}{Exception}")
            .CreateLogger();

        SerilogLoggerFactory factory = new SerilogLoggerFactory(log);
        SipLogger.Log = factory.CreateLogger("TestAes256");
        SipLogger.LogInformation("Starting TestAes256 now");


        SIPTCPChannel Channel;
        SipTransport sipTransport;
        string UserName = "AudioUac";
        IPAddress localAddress;

        List<IPAddress> addresses = IpUtils.GetIPv4Addresses();

        if (addresses == null || addresses.Count == 0)
        {
            Console.WriteLine("Error: No IPv4 addresses available");
            return;
        }

        localAddress = addresses[0];    // Pick the first available IP address to listen on
        IPEndPoint localIPEndPoint = new IPEndPoint(localAddress, localPort);
        Console.WriteLine($"Local  IPEndPoint = {localIPEndPoint}");
        Channel = new SIPTCPChannel(localIPEndPoint, UserName);
        sipTransport = new SipTransport(Channel);
        sipTransport.Start();
        IPEndPoint remoteIPEndPoint = new IPEndPoint(IPAddress.Parse(strRemoteIpAddress), remotePort);

        Console.Title = "AudioClient";

        AudioUac audioUac = new AudioUac(sipTransport, "AudioClient");
        audioUac.InterimResponseReceived += OnInterimResponseReceived;
        audioUac.ConnectionTimeout += OnConnectionTimeout;
        audioUac.CallRejected += OnCallRejected;
        audioUac.OkReceived += OnOkReceived;
        audioUac.ByeReceived += OnByeReceived;
        audioUac.Error += OnError;

        audioUac.Start();

        Console.WriteLine("Connecting...");
        Console.WriteLine("Type quit to exit the program");

        audioUac.Call(remoteIPEndPoint);

        string? strLine;
        bool Done = false;
        while (Done == false)
        {
            strLine = Console.ReadLine();
            if (strLine == "quit")
                Done = true;
        }

        await audioUac.Stop();
        sipTransport.Shutdown();
    }

    private static void OnInterimResponseReceived(SIPResponseStatusCodesEnum status)
    {
        Console.WriteLine($"Received: {status}");
    }

    private static void OnConnectionTimeout()
    {
        Console.WriteLine("No response received. Type quit to exit the program");
    }

    private static void OnCallRejected(SIPResponseStatusCodesEnum status)
    {
        Console.WriteLine($"Call rejected. Reason = {status}");
    }

    private static void OnOkReceived()
    {
        Console.WriteLine("200 OK received");
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
