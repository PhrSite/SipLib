# Real Time Text
The Real Time Text protocol (RTT, see RFC 4103) is used in NG9-1-1 to allow deaf or hard of hearing callers to communicate with 911 call takers using text messages. Text characters are typically sent one character at a time and displayed immediately, one character at a time.

The SipLib.RealTimeText namespace provides two classes for working with RTT media. The [RttSender](~/api/SipLib.RealTimeText.RttSender.yml) class is for sending RTT media and the [RttReceiver](~/api/SipLib.RealTimeText.RttReceiver.yml) class is for receiving RTT media.

The [Samples/RTT](https://github.com/PhrSite/SipLib/tree/master/Samples/RTT) directory in the SipLib GitHub repository contains two sample programs that demonstrate how to use the RttSender and the RttReceiver classes.

# <a name="UsingRttSender">Using The RttSender Class</a>
The RttSender class allows an application to sent text characters to the remote party of a call using the RTT protocol.

The procedure for using this class is:
1. Construct an instance of the RttSender class and store it with the call object
2. Call the Start() method of RttSender object.

When the call ends, the application must call the Stop() method of the RttSender object.

The declaration of the RttSender's constructor is:
```
public RttSender(RttParameters Rp, RttRtpSendDelegate sender);
```

The [RttParameters](~/api/SipLib.RealTimeText.RttParameters.yml) parameter contains the negotiated RTT protocol parameters such as the RTP payload types, redundancy level and the maximum number of characters per second that may be sent, etc... The RttParameters object may be constructed  from the negotiated MediaDescription object with the static FromMediaDescription() method of the RttParameters class.
```
public static RttParameters? FromMediaDescription(MediaDescription mediaDescription;
```

The MediaDescription parameter is the negotiated or answered media description. If the application is the client, then this is the MediaDescription object for RTT that it received from the server. If the application is the server, then this is the MediaDescription object for RTT that it sent to the client.

The declaration of RttRtpSendDelegate delegate type is:
```
public delegate void RttRtpSendDelegate(RtpPacket rtpPckt);
```

This is typically the Send() method of the RtpChannel class. For instance, if the RtpChannel that was created for the RTT media for a call is named "rtpChannel", then the value of this parameter should be "rtpChannel.Send".

The application sends characters with the SendMessage() method of the RttSender class.
```
public void SendMessage(string message);
```

An application is generally expected to send characters one at a time as the user types them. However, there are cases in which it is desirable to send stored text messages. The length of the message may be one or more characters. The maximum length of a message depends on the negotiated maximum character rate (CPS) and whether or not redundancy is being used.

If CPS is 0 (no rate restriction) and redundancy is being used then the maximum length is 1024 characters. If CPS is 0 and redundancy is not being used then the maximum length is the maximum size of a RTP UDP packet. If CPS is greater than 0 then characters are sent one at a time so there is no length restriction. The SendMessage() method will truncate the message to the maximum value allowed if the message is too long.

# <a name="UsingRttReceiver">Using the RttReceiver Class</a>
To use this class, create an instance of it and hook the RttCharacters received event.

The declaration of the constructor to user is:
```
public RttReceiver(RttParameters rttParams, RtpChannel rtpChannel, string source);
```
The [RttParameters](~/api/SipLib.RealTimeText.RttParameters.yml) parameter contains the negotiated RTT protocol parameters such as the RTP payload types, redundancy level and the maximum number of characters per second that may be sent, etc... The RttParameters object may be constructed  from the negotiated MediaDescription object with the static FromMediaDescription() method of the RttParameters class.
```
public static RttParameters? FromMediaDescription(MediaDescription mediaDescription;
```

The MediaDescription parameter is the negotiated or answered media description. If the application is the client, then this is the MediaDescription object for RTT that it received from the server. If the application is the server, then this is the MediaDescription object for RTT that it sent to the client.

The parameter called rtpChannel must be the RtpChannel that was created to handle RTT media for the call.

The source parameter should be a string that identifies the remote party. This can be something like "Caller" or "John Doe".

The declaration of the delegate for the RttCharactersReceived event is as follows:
```
public delegate void RttCharactersReceivedDelegate(string RxChars, string Source);
```

The RxChars paramter contains the characters that were received. The Source paramter identifies the sender of the characters.

