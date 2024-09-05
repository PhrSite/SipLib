# The MSRP Sample Projects
This directory contains two project subdirectories. The MsrpServer project is a Windows command prompt test program that implements a SIP User Agent Server (UAS) that accepts calls with Message Session Relay Protocol (MSRP) media. The MsrpClient project is a Windows command prompt test program that implements a SIP User Agent Client (UAC) that generates a call with MSRP media. These two sample programs demonstrate how to handle MSRP media using the SipLib class library.

Both of these programs use TCP over IPv6 for SIP and MSRP media so the computer that you run them on must have at least one IPv6 address that is not a loopback or local link address.

Microsoft .NET 8 must be installed on the computer.

Start by running the MsrpServer program first by following these steps.
1. Open a command prompt window and change directories to the MsrpServer directory.
2. Type: dotnet run

Next, run the MsrpClient program by following thest steps.
1. Open another command prompt window and change directories to the MsrpClient directory.
2. Type: dotnet run

When the MsrpClient runs, it immediately sends a SIP INVITE request to the MsrpServer application and the MsrpServer answers the call immediately. You can then start sending MSRP text messages between the MsrpServer and the MsrpClient applications.

Type 'quit' (without quotes) to terminate either program.
