# The SipLib.TestCalls Namespace
The SipLib.TestCalls namespace provides various classes for supporting NG9-1-1 test calls.

The requirements for handling NG9-1-1 test calls originate from the following documents.
> Section 9 of the National Emergency Number Association (NENA) Standard for NG9-1-1. See [NENA-STA-010.3b](https://cdn.ymaws.com/www.nena.org/resource/resmgr/standards/nena-sta-010.3b-2021_i3_stan.pdf).
>
> An Extension to the Session Description Protocol (SDP) and Real-time Transport Protocol (RTP) for Media Loopback, [RFC 6849](https://www.rfc-editor.org/rfc/rfc6849.html), IETF, February 2013.

The purpose of NG9-1-1 test calls is to verify both the SIP signaling and the RTP media pathways. A test call generator sends a test call SIP request to a test call target. The test call target answers the call and echoes back RTP media sent to it by the test call generator. The test call target then terminates the call after it receives three RTP packets. Test calls are automatic and operate in the background. There is no human interaction for test calls.

Test calls typically use audio media, but it is possible to use any type of media that uses the RTP protocol. This includes video and RTT. There is no mention of MSRP test calls in NENA-STA-010.3b and RFC 6849 so the classes in SipLib.TestCalls namespace do not currently support test calls containing MSRP media.

# Handling Incoming Test Calls
An application can use the [IncomingTestCallManager](~/api/SipLib.TestCalls.IncomingTestCallManager.yml) class to process NG9-1-1 test calls in the background. This class is a complete User Agent Client (UAC) that is capable of handling multiple test calls simultaneously. An application can follow the following procedure for adding support for handling incoming test calls.
1. Construct an instance of the IncomingTestCallManager and save a reference to that instance.
2. Call the Start() method of the IncomintTestCallManager class.
3. Route SIP INVITE requests for test calls to the IncomingTestCallManager instance by calling the ProcessTestCallInviteRequest() method.
4. Route SIP BYE requests for test calls by calling the ProcessTestCallByeRequest() method of the IncomingTestCallManager instance.
5. Call the Shutdown() method of the IncomingTestCallManager class when the application is shutting down.

The IncomingTestCallManager class will either answer an incoming test call or reject it immediately so there is no need for the application's UAC to route any other SIP requests to the IncomingTestCallManager class.

## Creating an IncomingTestCallManager Object
The declaration of the constructor of the IncomingTestCallManager class is:
```
public IncomingTestCallManager(SdpAnswerSettings answerSettings, IncomingTestCallSettings
    testCallSettings, string userName);
```
The [SdpAnswerSettings](~/api/SipLib.Sdp.SdpAnswerSettings.yml) parameter contains various parameters that determine how the IncomingTestCallManager will answer incoming test calls. The application can use the same instance of the SdpAnswerSettings that it uses or it may create a new instance of this class with different settings than those used by the application.

The [IncomingTestCallSettings](~/api/SipLib.TestCalls.IncomingTestCallSettings.yml) parameter determines how incoming test calls will be handled. This class has the following properties.

| Property | Type | Description |
|--------|--------|-------------|
| Enable | bool | Enables incoming test calls if true. If false then the IncomingTestCallManager will reject all incoming test calls. The default setting is true. |
| MaxTestCalls | int | Specifies the maximum number of concurrent test calls that the IncomingTestCallManager must handle. The IncomingTestCallManager will reject test calls after the number of concurrent calls reaches this number. The minimum setting is 1. There is no upper limit. The default setting is 1. |
| DurationUnits | [TestCallDurationUnitsEnum](~/api/SipLib.TestCalls.TestCallDurationUnitsEnum.yml) | This property determines how the test call will be ended. If equal to DurationUnitsPackets then the test call will be terminated after the number of RTP packets specified by the DurationPackets property is received. If equal to DurationUnitsMinutes then the test call will be terminated when the test call duration reaches the number of minutes specified by the DurationMinutes. The default is DurationPackets. |
| DurationPackets | int | Specifies the number of RTP packets to receive before terminating the test call. The default is 3. |
| DurationMinutes | int | Specifies the number of minutes to wait before terminating the call. The default is 1. |

## Routing Test Call INVITE Requests
The application must route incoming SIP INVITE requests to its instance of the IncomingTestCallManager class. The application can use the static IsNg911TestCall() method of the IncomingTestCallManager class to determine if an incoming INVITE is for a test call.
```
public static bool IsNg911TestCall(SIPRequest sipRequest);
```
This method returns true if the SIPRequest object is an INVITE request for a test call.

The following code sample shows how the application can route incoming test calls.
```
private void ProcessInviteRequest(SIPRequest sipRequest, SIPEndPoint remoteEndPoint,
    SipTransport sipTransport)
{
    if (IncomingTestCallManager.IsNg911TestCall(sipRequest) == true)
    {
        m_TestCallManager.ProcessTestCallInviteRequest(sipRequest, remoteEndPoint,
            sipTransport);
        return;
    }

    // TODO: Handle a normal INVITE request.
}
```

## Routing Incoming BYE Requests
Normally, the test call target will end a test call. However it is possible for the test call generator to end a test call by sending a BYE request. The application must therefore be able to route an incoming BYE request for a test call to  its instance of the IncomingTestCallManager class.

The following code sample shows how to do this.
```
private void ProcessByeRequest(SIPRequest sipRequest, SIPEndPoint remoteEndPoint,
    SipTransport sipTransport)
{
    if (m_TestCallManager.IsActiveTestCall(sipRequest) == true)
    {
        m_TestCallManager.ProcessTestCallByeRequest(sipRequest, remoteEndPoint,
            sipTransport);
        return;
    }

    // TODO: Handle a BYE request for an existing call.
}
```

# Generating Test Calls
The SipLib.TestCalls namspace contains a class called [SimpleOutgoingAudioTestCall](~/api/SipLib.TestCalls.SimpleOutgoingAudioTestCall.yml) that can send a simple audio test call to a server application that can handle NG9-1-1 test calls. There is also a sample application in the Samples directory of the SipLib repository that shows how to use this class.
## <a name="UsingSimpleOutgointAudioTestCall">Using The SimpleOutgoingAudioTestCall Class</a>
This class sends an NG9-1-1 test call to an application that supports NG9-1-1 test calls. The test call only sends audio data. The audio data contains silence. The audio format is G.711 MuLaw.

This class waits for the test call to be terminated by the application and reports the results.

Follow this procedure to use this class to send a test call.
1. Construct an instance of this class.
2. Call the DoTestCall() method and await the results.

The declaration of the constructor of this class is:
```
public SimpleOutgoingAudioTestCall(SIPURI toSipUri, SipTransport transport,
    int localRtpAudioPort, int maxTestCallDurationSeconds = int.MaxValue)
```



## Sample Test Call Generator Application
The [Samples/SimpleTestCallGenerator](https://github.com/PhrSite/SipLib/tree/master/Samples/RTT) directory in the SipLib GitHub repository contains a Visual Studio command line project called SimpleTestCallGenerator.

This sample program binds to the first available loca IP address using port 5090 for SIP and port 10000 for audio. It uses TCP for the SIP transport.

This sample program uses the [SimpleOutgoingAudioTestCall](~/api/SipLib.TestCalls.SimpleOutgoingAudioTestCall.yml) class to send a single test call to another application that will answer the test call.

To run this application from a command prompt window;
1. Open a command prompt window
2. Change directories to the SimpleTestCallGenerator directory
3. Type `dotnet run -- IPEndPoint`, where IPEndPoint is the IPv4 endpoint of the application that will answer the test call.

For example:
```
dotnet run -- 192.168.1.84:5060
```










