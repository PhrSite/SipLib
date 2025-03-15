# Message Session Relay Protocol
The Message Session Relay Protocol (MSRP, see [RFC 4975](https://datatracker.ietf.org/doc/html/rfc4975)) can be used in applications to receive and send text messages from callers and call takers. It may also be used to send messages containing binary content such as images or to send private messages between members of a conference.

The SipLib.Msrp namespace contains classes for working with calls that have MSRP media. The main class is called [MsrpConnection](~/api/SipLib.Msrp.MsrpConnection.yml). This class can be used to send and receive text messages as text/plain or message/cpim. This class also supports sending and receiving binary content such as images or video files.

# Creating an MsrpConnection
Follow these steps to use the MsrpConnection class to send text and other types of media during a SIP call.
1. Create a new MsrpConnection object after the SIP call has been established by calling the CreateFromSdp() method.
2. Hook the events of the new MsrpConnection object.
3. Set the maximum MSRP message length for receiving MSRP messages with the MaxMsrpMessageLength property. See below. This is not necessary for text only applications.
4. Call the Start() method of the new MsrpConnection object.

When the SIP session ends you must call the Shutdown() method of the MsrpConnection() object.

The CreateFromSdp() method declaration is:
```
public static (MsrpConnection?, string?) CreateFromSdp(MediaDescription OfferedMd,
	MediaDescription AnsweredMd, bool IsIncoming, X509Certificate2? LocalCert)
```
The OfferedMd parameter is the MediaDescription for MSRP (media type = "message") that was offered in the INVITE request and the AnsweredMd parameter is the MediaDescription from the 200 OK response.

Set IsIncoming to false if the INVITE request was sent and to true if the INVITE request was received.

The LocalCert parameter can be a self-signed X.509 certificate and is required if using MSRP over TLS.

The CreateFromSdp() method returns a new MsrpConnection and a string. If successful, the MsrpConnection object will be non-null and the string value will be null. If an error occurred, then the MsrpConnection object will be null and the string will contain text describing the error that occurred.

# Maximum MSRP Message Length
There is no upper limit on the length of an MSRP message that the MsrpConnection class can send.

When sending large messages, the MsrpConnection class breaks the message into 2048 byte chuncks and sends each trunk using an MSRP transaction as specified in [Section 5.1 of RFC 4975](https://datatracker.ietf.org/doc/html/rfc4975#page-9).

For receiving MSRP messages, the MaxMsrpMessageLength property determines the maximum length of a complete MSRP message that the MsrpConnection can receive. The default setting is 10,000 bytes which is more than adequate for text-only applications. If the application needs to be able to receive images or video files then this property must be set to a value large enough to accommodate the largest file that is expected.

The MaxMsrpMessageLength property must be set before calling the Start() methos.

# MsrpConnection Events
The MsrpConnection class provides the following events.

| Event Name | Delegate Type | Description |
|------------|---------------|-------------|
| MsrpConnectionEstablished  | [MsrpConnectionStatusDelegate](~/api/SipLib.Msrp.MsrpConnectionStatusDelegate.yml) | This event is fired when a connection is established with the remote endpoint either as a client or as a server. Handling of this event is optional. |
| MsrpConnectionFailed | [MsrpConnectionStatusDelegate](~/api/SipLib.Msrp.MsrpConnectionStatusDelegate.yml) | This event is fired if the MsrpConnection object was unable to connect to the remote endpoint as a client. Handling of this event is optional. |
| MsrpMessageDeliveryFailed | [MsrpMessageDeliveryFailedDelegate](~/api/SipLib.Msrp.MsrpMessageDeliveryFailedDelegate.yml) | This event is fired if the remote endpoint rejected a MSRP message sent by the MsrpConnection object or if there was another problem delivering the message. Handling of this event is optional. |
| MsrpMessageReceived | [MsrpMessageReceivedDelegate](~/api/SipLib.Msrp.MsrpMessageReceivedDelegate.yml) | Event that is fired when a complete MSRP message is received. This event is not fired for empty SEND requests. |
| MsrpTextMessageReceived | [MsrpTextMessageReceivedDelegate](~/api/SipLib.Msrp.MsrpTextMessageReceivedDelegate.yml) | Event that fired when an MSRP message is received with a Content-Type of text/plain or message/cpim and the content type of the CPIM message is text/plain. This event is not fired for empty SEND requests.|
| ReportReceived | [ReportReceivedDelegate](~/api/SipLib.Msrp.ReportReceivedDelegate.yml) | Event that is fired when a MSRP REPORT request is received. Handling of this event is optional. |

The minimum requirement for a functionally complete application is to handle either the MsrpMessageReceived or the MsrpTextMessageReceived events. See <a href="#ReceivingMsrpMessages">Receiving MSRP Messages</a> below.

# Sending MSRP Messages
Applications use the SendMsrpMessage() method of the MsrpConnection class to send MSRP messages after the MsrpConnection object has been started. This method is declared as:
```
public void SendMsrpMessage(string ContentType, byte[] Contents, string? messageID = null);
```
The ContentType parameter specifies the the MIME type of the message. For example: plain/text or message/cpim. The Contents parameter contains the raw binary contents of the MSRP message to send. If sending a text message, then the string containing the message must be UTF8 encoded.

The messageID parameter is optional and used for confirmation of message delivery or notification of a delivery failure. This should be a 9 or 10 character alphanumeric string that identifies the message. If non-null, then the MsrpConnection object will fire the ReportReceived event to notifiy the application of the delivery status.

The SendMsrpMessage() method queues the MSRP message and returns immediately.

The following code sample shows how to send a simple text message.
```
string Message = "Hello World";
msrpConnection.SendMsrpMessage("text/plain", Encoding.UTF8.GetBytes(Message));
```
Common Profile for Instant Messaging (CPIM) messages are commonly used in NG9-1-1 applications, especially in applications that need to be conference-aware. See [RFC 3862](https://datatracker.ietf.org/doc/html/rfc3862). The following code sample shows how to send a CPIM message using the MsrpConnection class and the [CpimMessage](~/api/SipLib.Msrp.CpimMessage.yml) class.
```
public void SendCpim(MsrpConnection connection, string message, SIPUserField from,
    SIPUserField to)
{
    CpimMessage cpimMessage = new CpimMessage();
    cpimMessage.From = from;
    cpimMessage.To.Add(to);
    cpimMessage.ContentType = "text/plain";
    cpimMessage.Body = Encoding.UTF8.GetBytes(message);
    connection.SendMsrpMessage("message/cpim", cpimMessage.ToByteArray());
}
```
The from parameter identifies the source of the message and the to parameter identifies the destination for the message. These are [SIPUserField](~/api/SipLib.Core.SIPUserField.yml) objects and are commonly taken from the INVITE message of a SIP call.

The MsrpConnection class may also be used to send binary contents as shown below.
```
public void SendBinaryJpeg(MsrpConnection connection, string jpegFilePath)
{
    byte[] Contents = File.ReadAllBytes(jpegFilePath);
    connection.SendMsrpMessage("image/jpeg", Contents);
}
```
The following code sample shows how to sent a multipart/mixed MSRP message.
```
public void SendMultipartMixed(MsrpConnection connection, string message, string jpegFilePath)
{
    byte[] jpegBytes = File.ReadAllBytes(jpegFilePath);
    List<MessageContentsContainer> parts = new List<MessageContentsContainer>();

    MessageContentsContainer textContent = new MessageContentsContainer();
    textContent.ContentType = "text/plain";
    textContent.StringContents = message;
    parts.Add(textContent);

    MessageContentsContainer jpegContent = new MessageContentsContainer();
    jpegContent.ContentType = "image/jpeg";
    jpegContent.BinaryContents = jpegBytes;
    jpegContent.IsBinaryContents = true;
    parts.Add(jpegContent);

    string strBoundary = "boundary1";
    byte[] MultipartBytes = MultipartBinaryBodyBuilder.ToByteArray(parts, strBoundary);
    connection.SendMsrpMessage($"multipart/mixed;boundary={strBoundary}", MultipartBytes);
}
```

# <a name="ReceivingMsrpMessages">Receiving MSRP Messages</a>
The MsrpConnection class has two events that it fires when it receives a complete MSRP message. The application can hook either or both of these events.

The MsrpConnection class fires the MsrpTextMessageReceived event when it receives a message containing simple text, either in the body of the MSRP message or in the body of an embedded CPIM message.. The delegate type for this event is:
```
public delegate void MsrpTextMessageReceivedDelegate(string message, string from);
```
The from parameter is the user part of the remote MSRP URI if the content type is text/plain or the user part from the CPIM From header if the content type is message/cpim.

This is the easiest event to use if the application only needs to handle simple text messages.

The MsrpConnection fires the MsrpMessageReceived event when an MSRP message containing any media type (text, images, video or other). The delegate type for this event is:
```
public delegate void MsrpMessageReceivedDelegate(string ContentType, byte[] Contents,
    string from);
```
The ContentType parameter is from the Content-Type MSRP message header. For example: "text/plain", "message/cpim", "image/jpeg", "multipart/mixed;boundary=boundary1". The Contents parameter contains the binary contents of the message and the from header is the user part of the URI in the MSRP From-Path message header.

The MsrpMessageReceived event is fired for all received message. If the application is also handling the MsrpTextMessageReceived event, then it should not handle text messages in its handler for the MsrpMessageReceived event.

The following code sample shows what the event handler for the MsrpMessageReceived event should look like.
```
private void OnMsrpMessageReceived(string ContentType, byte[] Contents, string from)
{
    string strContentType = ContentType.ToLower();
    if (strContentType == "text/plain")
    {
        string strText = Encoding.UTF8.GetString(Contents);
        // TODO: Do something to display the message in strText.
    }
    else if (strContentType == "message/cpim")
    {
        HandleCpim(Contents, from);
    }
    else if (strContentType.IndexOf("multipart/mixed") >= 0)
        HandleMultipartMixed(ContentType, Contents, from);
}

private void HandleMultipartMixed(string ContentType, byte[] Contents, string from)
{
    List<MessageContentsContainer> ContentsList = BodyParser.ProcessMultiPartContents(
        Contents, ContentType);
    foreach (MessageContentsContainer content in ContentsList)
    {
        if (content.ContentType == "text/plain" && content.StringContents != null)
        {
            string strText = content.StringContents;
            // TODO: Do something with strText
        }
        else if (content.ContentType == "message/cpim")
        {
            if (content.IsBinaryContents == true && content.BinaryContents != null)
                HandleCpim(content.BinaryContents, from);
            else if (content.StringContents != null)
                HandleCpim(Encoding.UTF8.GetBytes(content.StringContents), from);
        }
        else
        {
            if (content.ContentType == "image/jpeg" && contneType.BinaryContents != null)
            {
            	byte[] jpegImage = content.BinaryContents;
                // TODO: Do something with jpegImage
            }
            // TODO: handle other media types
        }
    }
}

private void HandleCpim(byte[] Contents, string from)
{
    CpimMessage? cpimMessage = CpimMessage.ParseCpimBytes(Contents);
    if (cpimMessage == null)
    {
        // TODO: Handle the parsing error
        return;
    }

    string? strContentType = cpimMessage.ContentType?.ToLower();
    if (strContentType == "text/plain" && cpimMessage.Body != null)
    {
        string strMessage = Encoding.UTF8.GetString(cpimMessage.Body);
        // TODO: Do something with strMessage.
    }

    // Else handle other media types in a CPIM message
}
```

# MSRP Samples
The [Samples/MSRP](https://github.com/PhrSite/SipLib/tree/master/Samples/MSRP) directory of the SipLib GitHub repository contains two sample applications that demonstrate how to work with MSRP media for SIP calls.

