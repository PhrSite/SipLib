/////////////////////////////////////////////////////////////////////////////////////
//  File:   Program.cs                                              18 Apr 25 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SimpleTestCallGenerator;
using System.Net;
using SipLib.Network;
using SipLib.Channels;
using SipLib.Transactions;
using SipLib.Core;
using SipLib.TestCalls;

internal class Program
{
    private const int LocalPort = 5090;

    static async Task Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: SimpleTestCallGenerator IPEndPoint");
            return;
        }

        IPEndPoint? TargetIpEndPoint;
        if (IPEndPoint.TryParse(args[0], out TargetIpEndPoint) == false || TargetIpEndPoint == null)
        {
            Console.WriteLine("The specified IPEndPoint is not valid.");
            return;
        }

        List<IPAddress> ipAddresses = IpUtils.GetIPv4Addresses();
        if (ipAddresses.Count == 0)
        {
            Console.WriteLine("Error: No IPv4 addresses available");
            return;
        }

        // Select the first available IPv4 address.
        IPAddress LocalAddress = ipAddresses[0];
        Console.WriteLine($"Local IPv4 address = {LocalAddress}");

        SIPTCPChannel channel = new SIPTCPChannel(new IPEndPoint(LocalAddress, LocalPort), "SimpleTestCallGenerator", null);
        SipTransport transport = new SipTransport(channel);
        transport.Start();

        SIPURI toSipUri = new SIPURI(SIPSchemesEnum.sip, TargetIpEndPoint.Address, TargetIpEndPoint.Port);
        toSipUri.User = "UnitUnderTest";
        Console.WriteLine($"Calling {toSipUri.ToString()} now...");
        SimpleOutgoingAudioTestCall testCall = new SimpleOutgoingAudioTestCall(toSipUri, transport, 10000);

        for (int i=1; i <= 1; i++)
        {
            Console.WriteLine($"\nStarting call {i}...");
            OutgoingTestCallResults results = await testCall.DoTestCall();
            Console.WriteLine($"Success = {results.Success}");
            if (results.Success == true)
            {
                Console.WriteLine($"Packets Sent = {results.PacketsSent}, Packets Received = {results.PacketsReceived}");
                Console.WriteLine($"Call Duration = {(results.CallStopTime - results.CallStartTime).TotalMilliseconds} milliseconds");
            }
            else
                Console.WriteLine($"Failure Reason: {results.FailureReason}");

            await Task.Delay(1000);
        }
       
        Console.WriteLine("Press any key to exit");
        Console.ReadKey();
        transport.Shutdown();
    }
}
