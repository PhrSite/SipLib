# The RtpChannel Class
The Real Time Protocol (RTP, see RFC 3550) is used to transport audio, video and Real Time Text (RTT) media.

The SipLib.Rtp namespace provides several classes for working with RTP media. The main class is called [RtpChannel](~/api/SipLib.Rtp.RtpChannel.yml). This class provides the following functionality for sending and receiving audio, video and RTT packet data using the RTP protocol.
1. Asynchronous notification when an RTP packet is received via an event
2. A Send() method that allows the RtpChannel user to send RTP packets containing media data
3. Optional media encryption and decryption using Security Descriptor Secure RTP (SDES-SRTP) or Datagram Transport Layer Security Secure RTP (DTLS-SRTP)
4. Differentiated Service Code Point (DSCP) IP packet marking for Quality of Service (QOS) network management for NG9-1-1 applications that need to comply with the NG9-1-1 standard (NENA-STA-010.3b).
5. Support for the Real Time Control Protocol (RTCP).
6. Periodic notification of RTP statistics such as Mean Opinion Score (MOS), jitter and delay for audio media.

The RtpChannel class does not provide media codec support or any methods for sourcing or sinking media data. The SipLib.Media namespace provides some basic codec classes and defines some interfaces and base classes for implementing audio sources and sinks. The SipLib.RealTimeText namespace provides classes for implementing RTT senders and RTT receivers.

Follow these steps to create and use an RtpChannel object.
1. Call the CreateFromSdp() method with the offered SDP and offered media description and the answered SDP and the answered media description after the media description negotiation has been completed.
2. Hook the events of the RtpChannel class. At least the RtpPacketReceived event should be handled.
3. Call the StartListening() method to start the RtpChannel.
4. Call the Send() method to send a new RTP packet containing new media.

When the RTP session ends (for example, when a call ends), the user of the RtpChannel object must call the Shutdown() method to deallocate resources (such as UDP sockets) used by the RtpChannel.

# Creating an RtpChannel
Creating an RtpChannel is complicated because there are a large number of configurations and options involved. The easiest way (actually the only way) to create one is to use the CreateFromSdp() method. The declaration of this static function is as follows:
```
public static (RtpChannel?, string?) CreateFromSdp(bool Incoming, Sdp OfferedSdp, MediaDescription OfferedMd,
    Sdp AnsweredSdp, MediaDescription AnsweredMd, bool enableRtcp, string? CNAME)
```
This function can be used by both clients and servers to create an RtpChannel object. For a client, it should be called after the client receives the 200 OK SIP response from the server. For the server, it should be called after the server sends the 200 OK SIP response message to the client.

The following table describes how to set the parameter of the CreateFromSdp() function.

| Parameter | Client | Server |
|-----------|--------|--------|
| Incoming  | false   | true  |
| OfferedSdp | Sdp object that was sent with the INVITE request | Sdp object from the received INVITE request |
| OfferedMd | MediaDescription object from the OfferedSdp for the media type | Same |
| AnsweredSdp | Sdp object from the 200 OK response received from the server | Sdp object that was sent with the 200 OK response sent to the client |

The enableRtcp and the CNAME parameters are independent of whether the endpoint is a client or a server.

If enableRtcp is true then the RtpChannel will send and receive RTCP packets. This should be true (but does not need to be) for audio and RTT (text) media. RTCP is not generally used for video media.

The CNAME parameter is the Canonical End-Point Identier that will be sent as an SDES item in RTCP packets that the RtpChannel will send. See Section 6.5.1 of RFC 3550. This parameter should be of the form: user@host, where host can be a fully qualified domain name or an IP address. If the CNAME parameter is null, then the RtpChannel object will automatically create a default value such as "RtpChannel_audio@10.1.100.32", where 10.1.100.32 is the local IP address for the RtpChannel.

The CreateFromSdp() function returns an RtpChannel field and a string field. If successful, the RtpChannel field will be non-null and the string field will be null. If an error occurred, then the RtpChannel field will be null and the string field will be set to a string that describes the error condition.

Call the StartListening() method after calling CreateFromSdp().

Don't forget to call the Shutdown() method when the media session ends.

# <a name="ReceivingRtpPackets">Receiving RTP Packets</a>
In order to receive RTP media packets, the user of the RtpChannel class must hook the RtpPacketReceived event of an RtpChannel object. The delegate type of this event is:
```
public delegate void RtpPacketReceivedDelegate(RtpPacket rtpPacket);
```

The [RtpPacket](~/api/SipLib.Rtp.RtpPacket.yml) object contains the RTP packet header and the media payload. The event handler logic can then use the properties of the RtpPacket object to get information from the RTP packet header and the media payload of the packet.

The rtpPacket parameter will contain the un-encrypted (if encryption is being used by the RtpChannel object) RTP packet payload. The Payload property of the RtpPacket class can be used to get the packet payload as a byte array.

The format of the packet payload depends upon the type of media for the RtpChannel and the codec that was negotiated during call setup.

# Sending RTP Packets
The RtpChannel user can use the Send() method to send an RTP packet containing media when new media becomes available. The declaration of the Send() method is:
```
public void Send(RtpPacket rtpPacket);
```
The caller is responsible for creating an RtpPacket object containing the media packet. For each packet sent, the caller is responsible for updating the following properties of the RTP header.

The caller must perform the following steps to create an RtpPacket object.
1. Call the constructor to create a new RtpPacket
2. Set the packet parameters using the properties of the RtpPacket object
3. Copy the new media byte array into the packet using the Payload property setter

The declaration of the constructor to use for creating new RtpPackets is:
```
public RtpPacket(int PayloadLength, int HeaderLength);
```
The PayloadLength parameter is the number of bytes required for the media that will be sent. The HeaderLength parameter can be calculated as follows.
```
int PayloadLength = RtpPacket.MIN_PACKET_LENGTH + 4 * NumCSRCS;
```
NumCSRCS is the number of Contributing Sources (conference members) in the media to be sent. The value is typically 0 unless the application is a media mixer. Note: many media mixers do not provide CSRCs in the RTP packets because there are other methods of providing information about the conference members of a media stream.

The application must set the following header properties of the new RtpPacket. See [Section 5.1](https://datatracker.ietf.org/doc/html/rfc3550#page-12) of RFC 3550.

| RFC 3550 Field | RtpPacket Property | Type | Description |
|----------------|--------------------|------|-------------|
| CC             | CsrcCount          | int  | Gets or sets the number of Contributing Sources (CSRCs). This property defaults to 0 and is typically not used. |
| M              | Marker             | bool | Gets or sets the marker bit value. The meaning of the marker bit depends on the media type. |
| PT             | PayloadType        | int  | Must be a number between 0 and 127. Identifies the codec in use. For example, 0 for G.711 PCMU audio (typically), 101 for telephone-event (typically). |
| sequence number | SequenceNumber    | ushort | Gets or sets the sequence number. The sequence number must be incremented (with wrap around) for each new RTP Packet |
| timestamp      | Timestamp          | uint | Must be updated (with wrap around) for each new each new RTP packet. The value to add to the previous value depents on the media type. |
| SSRC           | SSRC               | uint | Gets or sets the synchronization source of the media stream. This can be obtained from the SSRC property of the RtpChannel that will be used to send the new media data. |

Call the Send() method after the above properties have been set.

# RtpChannel Events
The RtpChannel class provides the following events.

| Event | Description |
|--------|--------|
| RtpPacketReceived | Fired when a new RtpPacket is received. This event provides an RtpPacket object that contains new media. See [Receiving RTP Packets](#ReceivingRtpPackets) above. |
| RtpPacketSent | Fired after the RtpChannel sends a new RtpPacket.  |
| ReceiveStatisticsReady | Fired every 5 seconds for an RtpChannel that is handling audio if RTCP is enabled. |
| RtcpPacketReceived | Fired when an RtcpPacket has been received |
| RtcpPacketSent | Fired after the RtpChannel object sends a new RtcpPacket |
| DtlsHandshakeFailed | Fired if the DTLS-SRTP protocol handshake fails |

The only event that an application must hook is the RtpPacketReceived event so that it can get media from the remote endpoint.

# RtpChannel Media Encryption
The RtpChannel class supports media encryption using the Secure Real Time Protocol (SRTP). If configured for encryption, the RtpChannel class encrypts media being sent with the Send() method and it decrypts the encrypted media in RTP packets that it receives before providing the RtpPacket to the application using the RtpPacketReceived event. RTCP packets are also encrypted and decrypted.

The RtpChannel class supports two methods of negotiating the crypto suites and exchanging keying material. In the Security Descriptor Secure Real Time Protocol (SDES-SRTP) both the crypto suites and the keys are exchanged in the media descrition block of the Session Description Protocol (SDP) during call setup. In the Datagram Transport Layer Security Secure Real Time Protocol (DTLS-SRTP) method, use of DTLS-SRTP is negotiated in the media description of the SDP but the crypto suites and the keying material are negotiated during the DTLS handshake.

The CreateFromSdp() function configures the new RtpChannel based on the information in the offered and answered SDP.

## Security Descriptor Secure RTP (SDES-SRTP)
The RtpChannel class implements the following crypto suites for SDES-SRTP.

| Crypto Suite | RFC |
|--------|--------|
| AES_CM_128_HMAC_SHA1_80 | RFC 4568 |
| AES_CM_128_HMAC_SHA1_32 | RFC 4568 |
| F8_128_HMAC_SHA1_80 | RFC 4568 |
| AES_192_CM_HMAC_SHA1_80 | RFC 6188 |
| AES_192_CM_HMAC_SHA1_32 | RFC 6188 |
| AES_256_CM_HMAC_SHA1_80 | RFC 6188 |
| AES_256_CM_HMAC_SHA1_32 | RFC 6188 |

## Datagram Transport Layer Security SRTP (DTLS-SRTP)
The RtpChannel class implements the following crypto suites for DTLS-SRTP.

| Crypto Suite | RFC |
|--------|--------|
| AES_CM_128_HMAC_SHA1_80 | RFC 4568 |
| AES_CM_128_HMAC_SHA1_32 | RFC 4568 |

The DTLS protocol requires the use of X.509 certificates. The RtpChannel class creates a self-signed X.509 certificate that is stored in a static variable. Since a static variable is used, all instances of the RtpChannel class use the same X.509 certificate. A new X.509 certificate is generated each time the application that uses the RtpChannel class is run.





