# Introduction
The Session Description Protocol (SDP) is used in SIP applications for negotiating media sessions for multimedia calls. [RFC 8866](https://datatracker.ietf.org/doc/html/rfc8866) describes the SDP.

The SipLib.Sdp namespace provides classes for working with the Session Description Protocol.

The [Sdp](~/api/SipLib.Sdp.Sdp.yml) class represents a complete SDP media session. The Sdp class provides methods and properties for setting up parameters that describe the entire session. Each media stream or media type (audio, video, text, etc.) is represented by media description block within the SDP. The [MediaDescription](~/api/SipLib.Sdp.MediaDescription.yml) class provides methods and properties for creating a media description and setting up prameters that describe the media stream.

SIP clients and servers negotiate media sessions according to the offer/answer model described in [RFC 3264](https://www.ietf.org/rfc/rfc3264.txt). When a client wants to start a media session, it contructs an SDP text block that describes the session capabilities and the media capabilities that it can support. It then attaches the SDP text block to the body of a SIP INVITE request message and sends the INVITE request to a SIP server. The SIP server receives the SIP INVITE request and extracts the SDP text block and parses it. The server then builds its own SDP text block that describes the media session and media streams that it is willing to accept. It then sends this SDP text block to the client in the body of a SIP 200 OK response. The client and the server then setup their media streams and start sending media back and forth.

This article shows how to use the classes in the SipLib.Sdp namespace to build and parse SDP data in order to support multimedia applications containing audio, text and video media.

# Building an SDP Offer
A client builds an SDP parameter block by performing the following steps.
1. Create an Sdp object by using a constructor of the Sdp class.
2. Set the session level parameters by using the properties of the Sdp class.
3. Construct a MediaDescription object for each media type that it wishes to support for the session using one of the constructors of the MediaDescription class.
4. Set the parameters of the media using the properties of the MediaDescription class.
5. Add each MediaDescription object to the list of media of the Sdp object by calling the Sdp.Media.Add() method.
6. Convert the Sdp object to a string by calling the Sdp.ToString() method of the Sdp class.
7. Attach the SDP text block an INVITE SIPRequest object and send it to the server. The client can use the [SipBodyBuilder](~/api/SipLib.Body.SipBodyBuilder.yml) class to do this. See [Building SIP Message Bodies](~/articles/WorkingWithSipMessages.md#building-sip-message-bodies).

The following code sample shows how to create a simple Sdp object that offers G.711 Mu-Law audio.
```
private Sdp BuildAudioSdp(IPAddress iPAddress, int Port, string UaName)
{
    Sdp AudioSdp = new Sdp(iPAddress, UaName);

    MediaDescription AudSmd = new MediaDescription();
    AudSmd.MediaType = "audio";
    AudSmd.Port = Port;
    AudSmd.Transport = "RTP/AVP";
    AudSmd.PayloadTypes = new List<int>() { 0, 101 };
    AudSmd.Attributes.Add(new SdpAttribute("fmtp", "101 0-15"));
    AudSmd.RtpMapAttributes.Add(new RtpMapAttribute(0, "PCMU", 8000));
    AudSmd.RtpMapAttributes.Add(new RtpMapAttribute(101, "telephone-event", 8000));

    // Add the media description to the Sdp's Media list
    AudioSdp.Media.Add(AudSmd);

    return AudioSdp;
}
```
The iPAddress parameter is the IP address (IPv4 or IPv6) that the client wants to receive audio samples at and will be sending audio samples from. The Port parameter is the destination and source UDP port number. The UaName parameter is a user provided name that will be used to identify the SDP session.

If you call the above function with `Sdp sdp = BuildAudioSdp(IPAddress.Parse("192.168.1.100"), 7000, "MySession");` and then call `sdp.ToString()` the resulting SDP string will be:
```
v=0
o=MySession 7324305794702971685 1 IN IP4 192.168.1.100
s=MySession_2130495306
c=IN IP4 192.168.1.100
t=0 0
m=audio 7000 RTP/AVP 0 101
a=rtpmap:0 PCMU/8000
a=rtpmap:101 telephone-event/8000
a=fmtp:101 0-15
```

## Helper Functions for Build SDP Offers
The [SdpUtils](~/api/SipLib.Sdp.SdpUtils.yml) class contains helper functions that can be used to simplify the process of building an SDP offer. For example, the code in the code sample shown in the previous section could be replaced with the following code.
```
Sdp sdp = SdpUtils.BuildSimpleAudioSdp(IPAddress.Parse("192.168.1.100"), 7000,
	"MySession");
```
The SdpUtils class provides the following static functions for creating audio, Real Time Text (RTT) and video MediaDescription objects to assist in building an SDP offer.

| Function Name | Description |
|--------|--------|
| CreateAudioMediaDescription() | Creates a basic MediaDescription object for offerring G.711 Mu-Law audio media. The MediaDescription also includes an offer of telephone-event.  |
| CreateRttMediaDescription() | Creates a basic MediaDescription object for offering Real Time Text (RTT, see [RFC 4103](https://datatracker.ietf.org/doc/html/rfc4103)) media. |
| CreateVideoMediaDescription() | Builds a basic MediaDescription object for offering H.264 video using the Basic Level 1 video profile. |
| CreateMsrpMediaDescription() | Creates a basic MediaDescription object for offering Message Session Relay Protocol (MSRP, see RFC 4975). |

The following code sample shows how to use the above functions to add another media type to the SDP offer.
```
Sdp sdp = SdpUtils.BuildSimpleAudioSdp(IPAddress.Parse("192.168.1.100"), 7000,
	"MySession");
MediaDescription rttMd = SdpUtils.CreateRttMediaDescription(8000);
sdp.Media.Add(rttMd);
```

### Modifying MediaDescription Objects Created With the Helper Functions
#### Adding Media Types
It is easy to modify a MediaDescription object that was created with one of the SdpUtils helper functions. For instance, the following code sample shows how to A-Law audio to a MediaDescription CreateAudioMediaDescription() function.
```
MediaDescription audioMd = SdpUtils.CreateAudioMediaDescription(7000);
audioMd.PayloadTypes.Add(8);
audioMd.RtpMapAttributes.Add(new RtpMapAttribute(8, "PCMA", 8000));
string strMd = audioMd.ToString();
```
The above code produces the following media description text block.
```
m=audio 7000 RTP/AVP 0 101 8
a=rtpmap:0 PCMU/8000
a=rtpmap:101 telephone-event/8000
a=rtpmap:8 PCMA/8000
a=fmtp:101 0-15
```

#### <a name="MediaLevelConnectionInfo">Media Level Connection Information</a>
There are situations where it is necessary to have the connection information for a media type be different from the connection information of the entire sessions. The following code shows how to do this.
```
Sdp sdp = SdpUtils.BuildSimpleAudioSdp(IPAddress.Parse("192.168.1.100"), 7000,
    "MySession");
sdp.Media[0].ConnectionData = new ConnectionData(IPAddress.Parse("192.168.200"));
string strSdp = sdp.ToString();
```
The above code produces the following SDP text block.
```
v=0
o=MySession 6272624327141496333 1 IN IP4 192.168.1.100
s=MySession_289783837
c=IN IP4 192.168.1.100
t=0 0
m=audio 7000 RTP/AVP 0 101
c=IN IP4 192.168.0.200
a=rtpmap:0 PCMU/8000
a=rtpmap:101 telephone-event/8000
a=fmtp:101 0-15
```

### Adding Media Encryption to the Offer
The SipLib class library supports media encryption for all media types that use the Real Time Protocol (RTP). This library supports two ways to negotiate encryption keys. With the Security Descriptor/Secure Real Time Protocol (SDES-SRTP), encryptions keys are negotiated using attributes in the negotiated media descriptions. With Datagram Transport Layer Security/Secure Real Time Protocol (DTLS-SRTP) cipher suites and encryption keys are negotiated using the DTLS algorithm.

SDES-SRTP and DTLS-SRTP operate over UDP/RTP and may not be used for Message Session Relay Protocol (MSRP, see RFC 4975)  media because MSRP operates over TCP. Use the Transport Layer Security (TLS) transport mechanism for MSRP if encryption is required as described in [Encryption for MSRP](#MsrpEncryption), below.

SDES-SRTP and DTLS-SRTP use the SRTP algorithm described in RFC 3711 and RFC 6811.

Only one of these encryption methods should be offered for a MediaDescription.

#### SDES/SRTP Encryption
The AddSdesSrtpEncryption() static method of the SdpUtils class can be used to configure a MediaDescription for audio, RTT or video for SDES-SRTP. The declaration of this method is as follows.
```
public static void AddSdesSrtpEncryption(MediaDescription mediaDescription);
```

This method adds an offer of the following crypto-suites.
1. AES_256_CM_HMAC_SHA1_80 (RFC 6188)
2. AES_CM_128_HMAC_SHA1_80 (RFC 3711, RFC 4568)

#### DTLS/SRTP Encryption
The AddDtlsSrtpEncryption() static method of the SdpUtils class can be used to configure a MediaDescription for audio, RTT or video for DTLS-SRTP. The declaration of this method is as follows.
```
public static void AddDtlsSrtp(MediaDescription mediaDescription, 
    string fingerPrintAttribute);
```
The fingerPrintAttribute parameter is the fingerprint of the X.509 certificate that the client will use for the DTLS handshake. If you are using the [RtpChannel](~/api/SipLib.Rtp.RtpChannel.yml) class for all RTP media, then it automatically creates a self-signed X.509 certificate. You can use the RtpChannel.CertificateFingerprint static property to get the correct value for the fingerPringAttribute parameter.

#### <a name="MsrpEncryption">Encryption for MSRP</a>
The SDES-SRTP and DTLS-SRTP encryption methods cannot be used for MSRP because MSRP operates over TCP and not UDP/RTP. The CreateMsrpMediaDescription() method of the SdpUtils class can be used to media descriptions for un-encrypted or encrypted MSRP media offers. The declaration of this method is as follows.
```
public static MediaDescription CreateMsrpMediaDescription(IPAddress ipAddress, int Port,
    bool UseTls, SetupType setupType = SetupType.active,
    X509Certificate2? localCert = null);
```
To use TLS, set the UseTls parameter to true as shown below.
```
MediaDescription msrpMd = SdpUtils.CreateMsrpMediaDescription(IPAddress.
    Parse("192.168.1.100"), 5000, true, SetupType.active);
string strMd = msrpMd.ToString();
```
The above code sample will produce the following media description text block.
```
m=message 5000 TCP/TLS/MSRP *
a=accept-types:message/cpim text/plain
a=setup:active
a=path:msrps//192.168.1.100:5000/2683pldqnv;tcp
```

# Building an SDP Answer
When a SIP server receives an INVITE request containing an offered SDP text block, it must parse offered SDP text block and build an SDP to send as an answer to the offered SDP. The contents of the SDP answer can depend on many factors such as:
1. What media types is the application willing to accept (audio, video. MSRP, RTT)?
2. What IP address is the SIP server going to use for receiving media?
3. What audio codecs is the application willing to use?
4. What video codecs is the application willing to use?
5. What port numbers are available for the application to use for each media type?

Given the answers to the above questions and the offered SDP the application can then build an SDP answer, but the logic to accomplish is somewhat complicated.

The the Sdp.BuildAnswerSdp() static method provide a fairly simple way to build an SDP answer. The declaration of the Sdp.BuildAnswerSdp() method is:
```
public static Sdp BuildAnswerSdp(Sdp OfferedSdp, IPAddress address,
    SdpAnswerSettings AnswerSettings)
```
The OfferedSdp parameter is the SDP that was received in an INVITE request. The address parameter is the IP address (IPv4 or IPv6) that the application wishes to use for receiving and sending media. This function sets the IP address in a c= parameter at the session level when it builds the answer SDP. This behavior can be overridden as described in [Media Level Connection Information](#MediaLevelConnectionInfo) above.

The [SdpAnswerSettings](~/api/SipLib.Sdp.SdpAnswerSettings.yml) class contains various properties that specify how to answer an offered SDP. The application can construct an instance of this class and modify the properties according to how it wants to handle media. It then uses the instance of the SdpAnswerSetting class each time it needs to answer a call. The declaration of the constructor of this class is:
```
public SdpAnswerSettings(List<string> AudioCodecs, List<string> VideoCodecs,
    string userName, string fingerprint, MediaPortManager portManager)
```
The following code sample shows how to construct an SdpAnswerSettings object with some commonly used settings.
```
SdpAnswerSettings Sas = new SdpAnswerSettings(new List<string> { "PCMU", "PCMA" },
    new List<string> { "H264" }, "MySession", RtpChannel.CertificateFingerprint,
    new MediaPortManager(new MediaPortSettings()));
```

The BuildAnswerSdp() method uses a [MediaPortManager](~/api/SipLib.Media.MediaPortManager.yml) class object to assign unique UDP ports for each media type for each call. The reason for this is to easily allow a server to handle media for multiple calls simultaneously.

The MediaPortManager class increments the port number by 2 for audio, video and RTT media to allow the application to use the RTCP on odd port numbers. The MediaPortManager class increments the port number by 1 for MSRP because RTCP is not used for MSRP media.

The default constructor of the [MediaPortSettings](~/api/SipLib.Media.MediaPortSettings.yml) class assigns the port settings for the different types of media as follows.

| Media Type | Starting Port | Port Count |
|--------|--------|---------|
|  audio   |  6000  | 1000 |
| video | 7000 | 1000 |
| text (RTT) | 8000 | 1000 |
| message (MSRP) | 9000 | 1000 |

The application can alter the port ranges for each media type by using the properties of the MediaPortSettings class.

# RFC 8866 Support Level

## Session Level Parameters

| SDP Parameter | Required? | Sdp Property Name | Type | Description |
|--------|--------|-----|----------------|------|-------------|
| v | Yes  | Version | int | Version of the SDP protocol. Defaults to 0. This cannot be changed |
| o | Yes | Origin | [Origin](~/api/SipLib.Sdp.Origin.yml) | Originator and session identifier. The constructor of the Sdp class initializes this property. |
| s | Yes | SessionName | string | Session Name. The constructor of the Sdp class initializes this property to a unique value. |
| i | No | SessionInformation | string | Session Information. Optional, not generally used. |
| u | No | Uri | string | URI of the session description. Optional. |
| e | No | Email | string | E-mail address. Optional.|
| p | No | PhoneNumber | string | Phone Number. Optional. |
| c | No | ConnectionData | [ConnectionData](~/api/SipLib.Sdp.ConnectionData.yml) | Connection information. Required if not provided in all of the media description blocks, otherwise required. |
| b | No | Bandwidth | string | Bandwidth information for the session. Optional. |
| k | No | | | Obsolete -- not supported |
| t | Yes | Timing | string | Timing information. The Sdp class sets this field to a default value to "0 0". For the applications that the SipLib class library is intended for there is generally no reason to change the value of this field. |
| r | No |  |  | Repeat times. Not supported by the Sdp class |
| z | No |  |  | Time zone adjustment information. Not supported by the Sdp class |
| a | No | Attributes | List of [SdpAttribute](~/api/SipLib.Sdp.SdpAttribute.yml) objects | Session level attributes. The Sdp class initializes this field to an empty generic list of type SdpAttribute |
| m | Yes | Media | List of [MediaDescription](~/api/SipLib.Sdp.MediaDescription.yml) objects | Media description blocks. The Sdp class initializes this field to an empty generic list of type MediaDescription. |


## Media Level Parameters

| Parameter | Required? | MediaDescription Field Name | Type | Description |
|--------|--------|-----|----------------|------|-------------|
| i | No |  |  | Media Title. Not supported by the MediaDescription class. |
| c | No | ConnectionData | [ConnectionData](~/api/SipLib.Sdp.ConnectionData.yml) | Connection information. Not required if provided at the session level.  |
| b | No | Bandwidth | string | Bandwidth information for the media type. |
| a | No | Attributes | List of [SdpAttribute](~/api/SipLib.Sdp.SdpAttribute.yml) objects | Media level attributes. The MediaDescription class initializes this field to an empty generic list of type SdpAttribute |
| k | No | | | Obsolete -- not supported |
| a | No | Attributes | List of [SdpAttribute](~/api/SipLib.Sdp.SdpAttribute.yml) objects | Media level attributes. The MediaDescription class initializes this field to an empty generic list of type SdpAttribute |


