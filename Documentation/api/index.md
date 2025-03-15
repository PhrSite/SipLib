# SipLib Namespaces
The SipLib namespace contains the following child namespaces.

| Namespace              | Description |
|------------------------|-------------|
| Body                   | Contains classes for working with the body of SIP messages. |
| Channels               | Classes for sending and receiving SIP messages over UDP, TCP or TLS connections. |
| Collections            | Contains thread-safe generic collection classes that are not provided by the .NET class libraries |
| Core                   | Core classes for building and parsing SIP messages. |
| Dtls                   | Classes required to support encryption and decryption of media (audio, video and Real Time Text) using the Datagram Transport Layer Security  DTLS specified in RFC 5763 and RFC 5764. The classes in this namespace are used by the RtpChannel class for encrypting and decrypting RTP media data. Users of the SipLib class library do not normally need to use the classes in this namespace. |
| Logging                | Contains a static class called SipLogger that the classes in this class library can use for logging application messages. |
| Media                  | Classes for encoding and decoding audio. The supported codecs are G.711 Mu-Law, G.711 A-Law and G.722. |
| Msrp                   | Message Session Relay Protocol (MSRP, see RFC 4975) related classes. |
| Network                | Contains a utility helper class for performing network protocol related functions. |
| RealTimeText           | Classes for the Real Time Text (RTT, see RFC 4103) protocol.  |
| Rtp                    | Classes for the Real Time Protocol (RTP, see RFC 3550). The main class is called RtpChannel. This class supports unencrypted RTP media as well as encryption using the SDES-SRTP (RFC 3711, RFC 4568) and SDES-DTLS (RFC 5763, RFC 5764) protocols. |
| RtpCrypto              | Classes that implement the SDES-SRTP protocols used in secure RTP. The classes in this namespace are used by the RtpChannel class for encrypting and decrypting RTP media data. Users of the SipLib class library do not normally need to use the classes in this namespace. |
| Sdp                    | Classes used for the Session Description Protocol (SDP, see RFC 8866). |
| Transactions           | Classes for managing SIP transactions as specified in Section 17 of RFC 3261. |
| Video                  | Classes for packing and unpacking video frames for use in RTP channels for H.264 and VP8 video. The H.264 and VP8 codecs are not included here. |



