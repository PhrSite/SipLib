# The SimpleTestCallGenerator Application

This directory contains a Visual Studio command line project called SimpleTestCallGenerator. This sample application sends a NG9-1-1 test call to an application that can handle NG9-1-1 test calls.

This sample program binds to the first available local IPv4 address using port 5090 for SIP and port 10000 for audio. It uses TCP for the SIP transport.

To run this application from a command prompt window;
1. Open a command prompt window
2. Change directories to the SimpleTestCallGenerator directory
3. Type `dotnet run -- IPEndPoint`, where IPEndPoint is the IPv4 endpoint of the application that will answer the test call.

For example:
```
dotnet run -- 192.168.1.84:5060
```

