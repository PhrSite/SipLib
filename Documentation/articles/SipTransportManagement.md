# SIP Message Transport and Transaction Management
The [SipTransport](~/api/SipLib.Transactions.SipTransport.yml) class can be used to manage User Agent Server (UAS) and User Agent Client (UAC) SIP transactions.

Each instance of a SipTransport class can manage SIP transactions over a single SIP channel. It is possible to handle SIP transactions over multiple SIP channels by creating multiple SipTransport objects, one for each individual SIP channel.

Perform the following steps to use the SipTransport class.
1. Create an instance of a SIPChannel derived class.
2. Create an instance of the SipTransport class by passing the reference to the SIPChannel derived object in the constructor of the SipTransport class.
3. Hook the events of the SipTransport object.
4. Call the Start() method of the SipTransport object.

The user of a SipTransport object should store a reference to the object.

When the user of a SipTransport object has no further need for it, it should perform the following steps.
1. Unhook the events (optional if the SipTransport object is shut down when the application is shutting down)
2. Call the Shutdown() method of the SipTransport object.

The Shutdown() method of the SipTransport class calls the Close() method of the SIPChannel that it is using so it is not necessary to call SIPChannel.Close().

The following code snippet shows how to create and use the SipTransport class.
```
class CallManager
{
    private SIPTCPChannel m_TcpChannel;
    private SipTransport m_SipTransport;

    public CallManager
    {
        // Create a SIPChannel derived object that binds to a local address and port
        // for processing SIP messages
        IPAddress localAddress = IPAddress.Parse("192.168.1.101");
        IPEndPoint localEndpoint = new IPEndPoint(localAddress, 5060);
        m_TcpChannel = new SIPTCPChannel(localEndpoint, "SomeUserName");

        m_SipTransport = new SipTransport(m_TcpChannel);
        m_SipTransport.SipRequestReceived += OnSipRequestReceived;
        m_SipTransport.SipResponseReceived += OnSipResponseReceived;

        m_SipTransport.Start();
    }

    void OnSipRequestReceived(SIPRequest sipRequest, SIPEndPoint remoteEndPoint,
        SipTransport sipTransport)
    {
        // Do something to handle the SIP request message
    }

    void OnSipResponseReceived(SIPResponse sipResponse, SIPEndPoint remoteEndPoint,
        SipTransport sipTransport)
    {
        // So something to handle the SIP response message
    }

    // This method shoud be called when the application is shutting down.
    public void Shutdown()
    {
        m_SipTransport.SipRequestReceived += OnSipRequestReceived;
        m_SipTransport.SipResponseReceived += OnSipResponseReceived;

        // Releases all network related resources
        m_SipTransport.Shutdown();
    }
}
```
Applications that need to handle SIP messages from multiple network transport protocols (UDP, TCP, TLS) or multiple local endpoints can do so by creating a SIPChannel derived object for each protocol or local endpoint and then create a SipTransport object for each SIPChannel. The event handler functions may be shared among multiple SipTransport objects.

## Handling SipTransport Events
The the SipTransport class provides the following events.

| Event Name | Description |
|--------|--------|
| SipRequestReceived | Fired when a SIP request message is received. This event is not fired if a SIP request processed by the transaction manager. |
| SipResponseReceived | Fired when a SIP response message is received. This event is not fired if the SIP response message is handled as part of a SIP transaction. |
| LogSipRequest| Fired for every SIP request message that is sent or received by the SipTransport object. For received requests, this event is fired after the request message is sent to the transaction object or the SipTransport user. The purpose of this event is to allow an application to log all SIP request messages. The event handler for this event must do nothing to process the SIPRequest object passed to it by this event. |
| LogSipResponse | Fired for every SIP response message that is sent or received by the SipTransport object. For received response messages, this event is fired after the message is sent to the transaction manager or the SipTransport user. The purpose of this event is to allow an application to log all SIP response messages. The event handler for this event must do nothing to process the SIPResponse object that is passed to it by this event. |
| LogInvalidMessage | Fired if the SipTransport object receives an invalid SIP message. |

The event handler functions are called asynchronously from a thread context that is managed by the SipTransport class so the event handler functions are responsible for managing synchronization of objects that are shared by different thread contexts.

## Handling SIP Requests
The SipRequestReceived event is fired by the SipTransport class when a new SIP request message is received. The following code snippet provides an example of how to handle this event.

~~~
class CallManager
{
    void OnSipRequestReceived(SIPRequest sipRequest, SIPEndPoint remoteEndPoint,
        SipTransport sipTransport)
    {
        switch (sipRequest.Method)
        {
            case SIPMethodsEnum.INVITE:
                ProcessInviteRequest(sipRequest, remoteEndPoint, sipTransport);
                break;
            case SIPMethodsEnum.BYE:
                ProcessByeRequest(sipRequest, remoteEndPoint, sipTransport);
                break;
            case SIPMethodsEnum.ACK:
                // Normally, no action is necessary unless the ACK message is for an
                // 200 OK response to an offer-less INVITE request
                break;
            case SIPMethodsEnum.CANCEL:
                ProcessCancelRequest(sipRequest, remoteEndPoint, sipTransport);
                break;
            case SIPMethodsEnum.OPTIONS:
                ProcessSipOptionsRequest(sipRequest, remoteEndPoint, sipTransport);
                break;
            case SIPMethodsEnum.INFO:
                MethdNotAllowedTransaction(sipRequest, remoteEndPoint, sipTransport);
                break;
            case SIPMethodsEnum.NOTIFY:
                MethdNotAllowedTransaction(sipRequest, remoteEndPoint, sipTransport);
                break;
            case SIPMethodsEnum.SUBSCRIBE:
                MethdNotAllowedTransaction(sipRequest, remoteEndPoint, sipTransport);
                break;
            case SIPMethodsEnum.PUBLISH:
                SendMethdNotAllowed(sipRequest, remoteEndPoint, sipTransport);
                break;
            case SIPMethodsEnum.REFER:
                SendMethdNotAllowed(sipRequest, remoteEndPoint, sipTransport);
                break;
            case SIPMethodsEnum.MESSAGE:
                MethdNotAllowedTransaction(sipRequest, remoteEndPoint, sipTransport);
                break;
            case SIPMethodsEnum.UPDATE:
                // Not used for incoming calls.
                SendMethdNotAllowed(sipRequest, remoteEndPoint, sipTransport);
                break;
            case SIPMethodsEnum.REGISTER:
                SendMethdNotAllowed(sipRequest, remoteEndPoint, sipTransport);
                break;
        }
    }

    // This method shows how the user agent server can send a simple response to the
    // client without using a server non-INVITE transaction
    private void SendMethodNotAllowed(SIPRequest sipRequest,
        SIPEndPoint remoteEndPoint,
    	SipTransport sipTransport)
    {
        SIPResponse Response = SipUtils.BuildResponse(sipRequest,
            SIPResponseStatusCodesEnum.MethodNotAllowed,
            "Not Allowed", sipTransport.SipChannel, UserName);
        sipTransport.SendSipResponse(Response, remoteEndPoint);
    }

    // This method shows how the user agent server can send a simple response to the
    // client using a server non-INVITE transaction
    private void MethodNotAllowedTransaction(SIPRequest sipRequest,
        SIPEndPoint remoteEndPoint,
    	SipTransport sipTransport)
    {
        SIPResponse Response = SipUtils.BuildResponse(sipRequest,
            SIPResponseStatusCodesEnum.MethodNotAllowed,
            "Not Allowed", sipTransport.SipChannel, UserName);
    	sipTransport.ServerNonInviteTransaction(sipRequest, remoteEndPoint, null,
            Response);
    }
}

~~~

## Logging SIP Messages
If there is a need for an application to log all of the SIP messages that it sends and receives, the SipTransport class provides two events that it fires when it sends a SIP message or receives a SIP message. The application can use the information provided by these events for debug logging or NG9-1-1 SIP message logging.

The SipTransport class fires the LogSipRequest event when it sends or receives a SIP request message. The delegate type for the LogSipRequest is as follows.
```
public delegate void LogSipRequestDelegate(SIPRequest sipRequest,
    IPEndPoint remoteEndPoint, bool Sent, SipTransport sipTransport);
```
If the Sent parameter is true, then the remoteEndPoint parameter is the IP endpoint that the SipTransport object sent the request to. If Sent is false, then the remoteEndPoint parameter is the IP endpoint that the SipTransport object received the request from

The SipTransport class fires the LogSipResponse event when it sends or receives a SIP response message. The delegate type for the LogSipResponse event is as follows.
```
public delegate void LogSipResponseDelegate(SIPResponse sipResponse,
    IPEndPoint remoteEndPoint, bool Sent, SipTransport sipTransport);
```
If the Sent parameter is true, then the remoteEndPoint is the IP endpoint that the SipTransport object sent the response message to. If the Sent parameter is false, then the remoteEndPoint parameter is the IP endpoint that the SipTransport object received the response message from.

The sipTransport parameter is the SipTransport object that fired the event. An application can use the SipChannel property of the SipTransport object to get information about the SIP channel that was used to send or receive the event. Applications must not use any methods of SipTransport class in the event handler for the LogSipRequest and LogSipResponse events.

Applications may only use the following properties and methods of the SipChannel property of the SipTransport object.
1. SIPChannelContactURI -- Gets the [SIPURI](~/api/SipLib.Core.SIPURI.yml) that can be used for contacting the SIPChannel
2. SIPChannelEndPoint -- Gets the local [SIPEndPoint](~/api/SipLib.Core.SIPEndPoint.yml) for the SIP channel
3. GetProtocol() -- Gets the transport protocol used for this channel as a [SIPProtocolsEnum](~/api/SipLib.Core.SIPProtocolsEnum.yml) value.
4. GetRemoteCertificate() and GetRemoteCertificate2() to get the X.509 certificate of the remote endpoint if the SIPChannel is using TLS.

Applications should only use these events for logging purposes and must not infer anything about the signaling state of a transaction or a call.

## SipTransport Class Transaction Handling
The SipTransport class provides a SIP transaction manager function that manages the following four types of SIP transactions as defined in RFC 3261.
1. <a href="#ClientNonInvite">Client non-INVITE transaction</a>
2. <a href="#ClientInvite">Client INVITE transaction</a>
3. <a href="#ServerNonInvite">Server non-INVITE transaction</a>
4. <a href="#ServerInvite">Server INVITE transaction</a>

The SipTransport class provides the following methods for starting SIP transactions.

| Method Name | Description |
|--------|--------|
| [StartClientNonInviteTransaction](~/api/SipLib.Channels.SipTransport.yml#SipLib_Channels_SipTransport_StartClientNonInviteTransaction_SipLib_Core_SIPRequest_System_Net_IPEndPoint_SipLib_Channels_SipTransactionCompleteDelegate_System_Int32_) | Creates and starts a client-side non-INVITE transaction.  Returns a [ClientNonInviteTransaction](~/api/SipLib.Transactions.ClientNonInviteTransaction.yml) object that implements the logic specified in [Section 17.1.2 of RFC 3261](https://datatracker.ietf.org/doc/html/rfc3261#section-17.1.2). |
| [StartClientInvite](~/api/SipLib.Channels.SipTransport.yml#SipLib_Channels_SipTransport_StartClientInvite_SipLib_Core_SIPRequest_System_Net_IPEndPoint_SipLib_Channels_SipTransactionCompleteDelegate_SipLib_Transactions_TransactionResponseReceivedDelegate_) | Creates and starts a client-side transaction for sending an INVITE request. This method returns a [ClientInviteTransaction](~/api/SipLib.Transactions.ClientInviteTransaction.yml) object that implements the logic specified in [Section 17.1.1 of RFC 3261](https://datatracker.ietf.org/doc/html/rfc3261#section-17.1.1). This method sends an INVITE request and returns a ClientInviteTransaction object immediately. It does not wait for a response. The caller can provide a delegate function that gets called when the INVITE transaction is completed.|
| [StartClientInviteAsync](~/api/SipLib.Channels.SipTransport.yml#SipLib_Channels_SipTransport_StartClientInviteAsync_SipLib_Core_SIPRequest_System_Net_IPEndPoint_SipLib_Transactions_TransactionResponseReceivedDelegate_) | Creates and starts a client-side transaction for sending an INVITE request. This method returns an awaitable Task that returns an [ClientInviteTransaction](~/api/SipLib.Transactions.ClientInviteTransaction.yml) object. This object implements the logic specified in [Section 17.1.1 of RFC 3261](https://datatracker.ietf.org/doc/html/rfc3261#section-17.1.1).  |
| [StartServerNonInviteTransaction](~/api/SipLib.Channels.SipTransport.yml#SipLib_Channels_SipTransport_StartServerNonInviteTransaction_SipLib_Core_SIPRequest_System_Net_IPEndPoint_SipLib_Channels_SipTransactionCompleteDelegate_SipLib_Core_SIPResponse_) | Creates and starts a server-side non-INVITE transaction. This method returns a [ServerNonInviteTransaction](~/api/SipLib.Transactions.ServerNonInviteTransaction.yml) object that implements the logic specified in [Section 17.2.2 of RFC 3261](https://datatracker.ietf.org/doc/html/rfc3261#section-17.2.2).  |
| [StartServerInviteTransaction](~/api/SipLib.Channels.SipTransport.yml#SipLib_Channels_SipTransport_StartServerInviteTransaction_SipLib_Core_SIPRequest_System_Net_IPEndPoint_SipLib_Channels_SipTransactionCompleteDelegate_SipLib_Core_SIPResponse_) | Creates and starts a server-side INVITE transaction. This method returns a [ServerInviteTransaction](~/api/SipLib.Transactions.ServerInviteTransaction.yml) object that implements the logic specified in [Section 17.2.1 of RFC 3261](https://datatracker.ietf.org/doc/html/rfc3261#section-17.2.1). This method takes a SIPResponse that can be either an provisional response or a final response. It also takes a delegate function that is called when the transaction is completed. The delegate function may be null if notification of the transaction completion is not required.  |

Transaction users have three options for receiving notification that a transaction is completed. These options are available for client and server transactions.
1. Provide a callback delegate that is called by the SipTransport object (the transaction manager)
2. Asynchronously await completion of the transaction
3. Don't wait for transaction completion (fire and forget)

Each method of the SipTransport class that starts a SIP transaction takes a callback delegate parameter. The delegate type is called [SipTransactionCompleteDelegate](~/api/SipLib.Channels.SipTransactionCompleteDelegate.yml). The callback name for each method is completeDelegate.
```
public delegate void SipTransactionCompleteDelegate(SIPRequest sipRequest,
    SIPResponse? sipResponse, IPEndPoint remoteEndPoint, SipTransport sipTransport,
    SipTransactionBase Transaction);
```
The sipRequest parameter is the SIPRequest message that was sent to start the transaction and the sipResponse parameter is the SIPResponse object that was received if the transaction completed normally.

If the completeDelegate parameter is non-null in a SipTransport method that starts a transaction, then the SipTransport transaction manager will call the method when a final SIP response (status code >= 200) is received or a transaction timeout occurs.

The user's callback funcition can check the [TerminationReason](~/api/SipLib.Transactions.TransactionTerminationReasonEnum.yml) property of the Transaction parameter to determine the reason that the transaction terminated. If the value of the TerminationReason property is OkReceived or FinalResponseReceived, then the sipResponse parameter will contain the response that was received. Alternatively, the user's callback function can check to see if the sipResponse parameter of the callback is null or not. If it is not null then the transaction completed normally.

If the transaction user does not wish to receive a callback notification when a transaction completes or is terminated then it can asynchronously await completion of the transaction. Each method of the SipTransport class that starts a transaction returns an object derived from the [SipTransactionBase](~/api/SipLib.Transactions.SipTransactionBase.yml) class. The SipTransactionBase class contains a method called WaitForCompletionAsync() that the transaction user can asynchronously wait on. If this method is used then the transacton user must set the completeDelegate parameter to null in the call to the SipTransport method used to start the transaction.

To fire and forget a transaction, the transaction user must set the completeDelegate parameter to null in the call to the SipTransport method used to start the transaction and must not await for completion of the transaction.

### <a name="ClientNonInvite">Client Non-INVITE Transactions</a>
A client non-INVITE transaction is a SIP transaction for any SIP request except an INVITE request. In a client non-INVITE transaction, the client builds a SIP request and starts a SIP transaction by calling the StartClientNonInviteTransaction() method of a SipTransport object. The SipTransport object sends the request message to a server and notifies the client when the transaction is completed due to receipt of a final response or if the transaction times out.

The declaration of the StartClientNonInviteTransaction is:
```
public ClientNonInviteTransaction StartClientNonInviteTransaction(SIPRequest request,
    IPEndPoint remoteEndPoint, SipTransactionCompleteDelegate completeDelegate,
    int FinalResponseTimeoutMs)
```
The request parameter is the SIP request that will be sent to the IP endpoint specified by the remoteEndPoint parameter. The completeDelegate is the callback method that will be called when the client transaction has been completed or terminated due to a failure. If the completeDelegate parameter is null, then the caller can asynchronously await completion of the transaction by calling the WaitForCompletionAsync() method of the ClientNonInviteTransaction object that this method returns.

The FinalResponseTimeoutMs parameter specifies the length of time in milliseconds that the transaction manager will wait for a final response to be received or for a timeout to occur. This timer interval corresponds to Timer F described in RFC 3261. The default value is 64 times the default value of the SIP T1 timer (which is 500 ms), or 32 seconds. In many cases, this timeout value may be too long. If the application implementor understands the characteristics of the network and the expected performance of the SIP servers within the network, then this parameter may be set to a much lower value (for example, 500 ms).

The following code sample shows how to call the StartClientNonInviteTransaction() method using a callback.
```
private void SampleClientNonInvite(SipTransport sipTransport, SIPURI toUri, SIPURI fromUri)
{
    SIPRequest OptionsReq = SIPRequest.CreateBasicRequest(SIPMethodsEnum.OPTIONS,
        toUri, toUri, null, fromUri, null);
    sipTransport.StartClientNonInviteTransaction(OptionsReq, toUri.ToSIPEndPoint()!.
        GetIPEndPoint(), OptionsCompleteCallback, 500);
}

private void OptionsCompleteCallback(SIPRequest sipRequest, SIPResponse? sipResponse,
    IPEndPoint remoteEndPoint, SipTransport sipTransport, SipTransactionBase Transaction)
{
    if (sipResponse != null)
    {   // The transaction completed normally.
        if (sipResponse.Status == SIPResponseStatusCodesEnum.Ok)
        {   // Handle an 200 OK response
        }
        else
        {   // Handle a different response
        }
    }
    else
    {   // Check the reason that the transaction failed
        switch (Transaction.TerminationReason)
        {
            case TransactionTerminationReasonEnum.NoFinalResponseReceived:
                break;
            case TransactionTerminationReasonEnum.NoResponseReceived:
                break;
            case TransactionTerminationReasonEnum.ConnectionFailure:
                break;
        }
    }
}

```
The following code sample shows how to use the asynchronous await notification method.
```
private async Task SampleClientNonInviteAsync(SipTransport sipTransport,
    SIPURI toUri, SIPURI fromUri)
{
    SIPRequest OptionsReq = SIPRequest.CreateBasicRequest(SIPMethodsEnum.OPTIONS,
        toUri, toUri, null, fromUri, null);
    ClientNonInviteTransaction Cnit = sipTransport.StartClientNonInviteTransaction(
        OptionsReq, toUri.ToSIPEndPoint()!.GetIPEndPoint(), null, 500);
    // WaitForCompletionAsync() returns the same transaction object
    await Cnit.WaitForCompletionAsync();

    if (Cnit.LastReceivedResponse != null)
    {   // Do something with the response that was received
    }
    else
    {
        switch (Cnit.TerminationReason)
        {
            case TransactionTerminationReasonEnum.NoFinalResponseReceived:
                break;
            case TransactionTerminationReasonEnum.NoResponseReceived:
                break;
            case TransactionTerminationReasonEnum.ConnectionFailure:
                break;
        }
    }
}
```
### <a name="ClientInvite">Client INVITE Transactions</a>
A client sends an INVITE request to set up a call with a remote endpoint. The SipTransport class provides two methods that a client can use to start a client INVITE request transaction.
```
public ClientInviteTransaction StartClientInvite(SIPRequest request,
    IPEndPoint remoteEndPoint,
    SipTransactionCompleteDelegate? completeDelegate,
    TransactionResponseReceivedDelegate? responseReceivedDelegate)
```

```
public async Task<ClientInviteTransaction> StartClientInviteAsync(SIPRequest request,
    IPEndPoint remoteEndPoint,
    TransactionResponseReceivedDelegate? responseReceivedDelegate)
```
The second method allows the caller to asynchronously await completion of the INVITE transaction.

The request parameter must be an INVITE SIPRequest. The remoteEndPoint parameter specifies the IP endpoint to send the request to.

The responseReceivedDelegate parameter is a callback method that the transaction manager will call when a provisional response is received. This method is not called for a 100 Trying response. The purpose of this callback is to notify the transaction user of a 180 Ringing or a 183 Session Progress response. This callback parameter may be null if notification of provisional responses is not required. See the [TransactionResponseReceivedDelegate](~/api/SipLib.Transactions.TransactionResponseReceivedDelegate.yml).

There is no way for the transaction user to specify a timeout for the INVITE transaction. The reason for this is that the called party may take anywhere from several seconds to several minutes to answer the call. If the call is not rejected by the caller's user agent with a 3XX, 4XX, 5XX or a 6XX final response, then the INVITE transaction will continue until the called pary answers the call with a 200 OK or the calling user agent cancels the INVITE request by sending a CANCEL request.

The following code sample shows how to use the StartClientInviteAsync() method to send an INVITE request.
```
private async Task SampleClientInviteAsync(SIPRequest request,
    IPEndPoint remoteEndPoint, SipTransport sipTransport)
{
    ClientInviteTransaction Cit = await sipTransport.StartClientInviteAsync(request,
        remoteEndPoint, OnResponseReceived);
    if (Cit.LastReceivedResponse != null)
    {
        if (Cit.LastReceivedResponse.Status == SIPResponseStatusCodesEnum.Ok)
        {   // The call was answered, notify the user and set up the media channels
        }
        else
        {   // The call was rejected so notify the user of the reason
        }
    }
    else
    {   // The INVITE request transaction failed so figure out why and do something
        switch (Cit.TerminationReason)
        {
            case TransactionTerminationReasonEnum.NoResponseReceived:
                break;
            case TransactionTerminationReasonEnum.ConnectionFailure:
                break;
        }
    }
}

// Called when an interim response with a status code between 101 and 199 is received
// by the transport manager.
private void OnResponseReceived(SIPResponse Response, IPEndPoint RemoteEndPoint,
    SipTransactionBase Transaction)
{
    // The Transaction.Request property will contain the INVITE request if needed
    if (Response.Status == SIPResponseStatusCodesEnum.Ringing)
    {   // Notify the user that the call is ringing
    }
    else if (Response.Status == SIPResponseStatusCodesEnum.SessionProgress)
    {   // Notify the user that the call is ringing and set up the media channels to
        // receive ring sound.
    }
    else
    {   // Handle other interim responses if necessary
    }
}
```

#### Canceling an INVITE Request
The only way for a client to terminate an INVITE request transaction is to build and send a SIP CANCEL message using a separate client non-INIVTE transaction. When the server receives the CANCEL request, it will respond to the CANCEL request with a 200 OK response, then it will terminate its the client INVITE transaction by responding with a 487 Request Terminated final response. This will cause the transport manager to terminate the client INVITE request.

The easiest way for an application to cancel a client INVITE transaction is to hold onto a reference to the ClientInviteTransaction object (but only until the transacton has been completed or terminated) and to then call the CancelInvite() method of that object if the user wishes to cancel the call. The following code sample shows how to do this.
```
private void StartClientInvite(SIPRequest request, IPEndPoint remoteEndPoint,
    SipTransport transport)
{
    ClientInviteTransaction Cit = transport.StartClientInvite(request, remoteEndPoint,
        OnInviteComplete, null);

    // Give the server some time to send an interim response.
    Thread.Sleep(500);
    Cit.CancelInvite();
}

private void OnInviteComplete(SIPRequest sipRequest, SIPResponse? sipResponse,
    IPEndPoint remoteEndPoint, SipTransport sipTransport,
    SipTransactionBase Transaction)
{
    if (sipResponse != null)
    {
        if (sipResponse.Status == SIPResponseStatusCodesEnum.Ok)
        {   // Unexpected because the server answered the call before it received the
            // CANCEL request. The call must be terminated by sending a BYE request.
        }
        else if (sipResponse.Status == SIPResponseStatusCodesEnum.RequestTerminated)
        {   // This is the expected result
        }
        else
        {   // Some other response was received -- unexpected
        }
    }
    else
    {
        switch (Transaction.TerminationReason)
        {
            case TransactionTerminationReasonEnum.NoResponseReceived:
                break;
            case TransactionTerminationReasonEnum.CancelRequestFailed:
                break;
        }
    }
}
```

### <a name="ServerNonInvite">Server Non-INVITE Transactions</a>
A server non-INVTE transaction is used by a SIP server to handle a SIP request that is not an INVITE request. The [ServerNonInviteTransaction](~/api/SipLib.Transactions.ServerNonInviteTransaction.yml) class handles this type of transaction. When an application receives a non-INVITE SIP request it can call the StartServerNonInviteTransaction() method of the SipTransport class to start (and usually complete) the transaction.

The declaration of the StartServerNonInviteTransaction() method of the SipTransport class is as follows.
```
public ServerNonInviteTransaction StartServerNonInviteTransaction(SIPRequest request,
	IPEndPoint remoteEndPoint, SipTransactionCompleteDelegate? completeDelegate,
    SIPResponse ResponseToSend);
```
The request parameter is the SIPRequest object that was received by the server user agent. The remoteEndPoint is the IP endpoint that sent the request. The ResponseToSend is the SIPResponse to send back to the client. Server user agents will typically respond to non-INVITE requests by sending a final SIP response that will cause the transaction to be completed immediately.

The completeDelegate parameter is a callback function that the SipTransport class will call when the transaction is completed. This parameter is optional and may be set to null if the server user agent does not need to be notified when the transaction is completed.

The following code sample shows how to handle and complete a server non-INVITE transacton without being notified when the transaction is complete or waiting for completion.
```
private void ProcessRequest(SIPRequest request, IPEndPoint remoteEndPoint,
    SipTransport sipTransport)
{
    if (request.Method == SIPMethodsEnum.OPTIONS)
    {
        SIPResponse OkResponse = SipUtils.BuildResponse(request,
        SIPResponseStatusCodesEnum.Ok, "OK", sipTransport.SipChannel, null);
        // A 200 OK response will cause the transaction to be completed. Don't need to be
        // notified of success or failure in this case.
        sipTransport.StartServerNonInviteTransaction(request, remoteEndPoint, null,
            OkResponse);
    }
    else
    {   // Process other types of requests.
    }
}
```

### <a name="ServerInvite">Server INVITE Transactions</a>
A server INVITE request is used by a SIP user agent server to handle an INVITE request. The [ServerInviteTransaction](~/api/SipLib.Transactions.ServerInviteTransaction.yml) class handles this type of transaction. When the user agent server receives an INVITE request it can call the StartServerInviteTransaction method of the SipTransport class to start the transaction. The declaration of this method is as follows.
```
public ServerInviteTransaction StartServerInviteTransaction(SIPRequest request,
	IPEndPoint remoteEndPoint, SipTransactionCompleteDelegate? completeDelegate,
    SIPResponse ResponseToSend);
```
The request parameter is the INVITE request from the client. The remoteEndPoint is the IP endpoint of the client that sent the request. The ResponseToSend is the response to immediately send to the client. This is typically an intermediate response such as 100 Trying, but may be a final response that will end the transaction.

The completeDelegate parameter is a callback function that the SipTransport class will call when the transaction is completed. This parameter is optional and may be set to null if the server user agent does not need to be notified when the transaction is completed.

A server user agent may also asynchronously await the completion of the transaction by calling the WaitForCompletionAsync() method of the ServerInviteTransaction object.

Server INVITE transactions are typically long lasting transactions because it may take several seconds or several minutes for a user to answer the call. For this reason, a server user agent will typically want to keep a reference to the ServerInviteTransaction object and use this object to send interim and final responses to the client. The server user agent can use the SendResponse() method of the ServerInviteTransaction for this purpose.

The following code sample shows how to handle a server INVITE transaction using the callback notification method.
```
private async Task ProcessRequest(SIPRequest request, IPEndPoint remoteIPEndPoint,
    SipTransport sipTransport)
{
    if (request.Method == SIPMethodsEnum.INVITE)
    {
        SIPResponse trying = SipUtils.BuildResponse(request, 
            SIPResponseStatusCodesEnum.Trying, "Trying", sipTransport.SipChannel, null);
        m_Sit = sipTransport.StartServerInviteTransaction(request, remoteIPEndPoint,
            OnServerInviteComplete, trying);

		// Simulate a delay
        await Task.Delay(100);
        SIPResponse ringing = SipUtils.BuildResponse(request,
            SIPResponseStatusCodesEnum.Ringing, "Ringing", sipTransport.SipChannel, null);
        m_Sit.SendResponse(ringing);

		// Simulate a delay
        await Task.Delay(5000);
        // TODO: Build a 200 OK response with a SDP message body to answer the call
        // and send it by calling m_Sit.SendResponse(). This will cause the transaction
        // manager to end the INVITE transaction and OnServerInviteComplete will be called.
    }
    else
        ProcessCancel(request, remoteIPEndPoint, sipTransport);
}

private void OnServerInviteComplete(SIPRequest request, SIPResponse? sipResponse,
    IPEndPoint remoteEndPoint, SipTransport sipTransport, SipTransactionBase transaction)
{
	// TODO: shutdown any media channels that may have been setup if a 200 OK response
    // was sent.

    m_Sit = null;
}
```

#### Handling INVITE Cancel Requests
The transaction manager does not process CANCEL requests for a server INVITE transaction. CANCEL requests are passed to the transaction user. The following code sample is a continuation of the previous sample and shows how to a server user agent should process a CANCEL request.
```
private void ProcessCancel(SIPRequest request, IPEndPoint remoteIPEndPoint,
    SipTransport sipTransport)
{
    if (request.Method != SIPMethodsEnum.CANCEL)
        return;

    if (m_Sit == null)
        return;

    string cancelTransactionId = SipTransactionBase.GetServerTransactionID(request);
    if (cancelTransactionId == m_Sit.TransactionID)
    {
        SIPResponse ok = SipUtils.BuildResponse(request, SIPResponseStatusCodesEnum.Ok,
            "OK", sipTransport.SipChannel, null);
        sipTransport.StartServerNonInviteTransaction(request, remoteIPEndPoint, null, ok);

        SIPResponse reqTerminated = SipUtils.BuildResponse(m_Sit.Request!,
            SIPResponseStatusCodesEnum.RequestTerminated, "Request Terminated",
            sipTransport.SipChannel, null);
        // Sending this final response will cause the transaction manager to complete the
        // server INVITE transaction. OnServerInviteComplete() will be called
        m_Sit.SendResponse(reqTerminated);
    }
    else
    {   // Error: unknown transaction
        SIPResponse error = SipUtils.BuildResponse(request, SIPResponseStatusCodesEnum.
            CallLegTransactionDoesNotExist, "Unknown Transaction ID",
            sipTransport.SipChannel, null);
        // Starts and completes the CANCEL transaction
        sipTransport.StartServerNonInviteTransaction(request, remoteIPEndPoint, null,
        	error);
    }
}
```






