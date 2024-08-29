# The RTT Sample Projects
This directory contains two project subdirectories. The RttServer project is a Windows command prompt test program that implements a SIP User Agent Server (UAS) that accepts calls with Real Time Text (RTT) media. The RttClient project is a Windows command prompt test program that implements a SIP User Agent Client (UAC) that generates a call with RTT media. These two sample programs demonstrate how to handle RTT media using the SipLib class library.

Both of these programs use TCP over IPv6 for SIP and RTT media so the computer that you run them on must have at least one IPv6 address that is not a loopback or local link address.

Microsoft .NET 8 must be installed on the computer.

Start by running the RttServer program first by following these steps.
1. Open a command prompt window and change directories to the RttServer directory.
2. Thpe: dotnet run

Next, run the RttClient program by following thest steps.
1. Open another command prompt window and change directories to the RttClient directory.
2. Type: dotnet run

When the RttClient runs, it immediately sends a SIP INVITE request to the RttServer application and the RttServer answers the call immediately. You can then start sending RTT text messages between the RttServer and the RttClient applications.

You can stop both applications by pressing the ESC key.

