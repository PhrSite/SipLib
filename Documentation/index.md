# The SipLib Class Library
The SipLib class library is a portable, cross-platform class library written in the C# language that targets the .NET 8.0 environment. It may be used by applications that target the Windows Desktop (version 10 or later), Windows Server or Linux operating systems.

As a basic protocol class library, this project does not provide implementation of SIP user agents or device specific media endpoints as these components are very application specific.

The primary focus of this project is to provide protocol support (message building, message parsing and message transport) for developing Next Generation 9-1-1 (NG9-1-1) functional elements and applications in .NET. This class library provides support for several NG9-1-1 specific requirements such as:
1. The SIP Geolocation related headers (RFC 6442)
2. The use of the SIP Call-Info header for support of caller location data, additional data about a call (RFC 7852) and NG9-1-1 specific call and incident identifiers
3. Use of multipart/mixed content types in SIP message bodies for the SDP, location data and additional call data by-value
4. Quality of Service (QOS) management using IP DSCP packet marking for SIP and media packets
5. Various other requirements defined in the National Emergency Number Association's (NENA) Standard for NG9-1-1. See [NENA-STA-010.3b](https://cdn.ymaws.com/www.nena.org/resource/resmgr/standards/nena-sta-010.3b-2021_i3_stan.pdf).

The classes in this library support the following protocols.
1. Session Initiation Protocol (SIP, RFC 3261) over UDP, TCP and TLS
2. Session Description Protocol (SDP, RFC 8866)
3. An Offer/Answer Model with the Session Description Protocol (SDP) (RFC 3264)
3. Real Time Protocol (RTP, RFC 3550) for transport of audio, video and Real Time Text
4. Real Time Text (RTT, RFC 4103)
5. Message Session Relay Protocol (MSRP, RFC 4975) using TCP or TLS
6. RTP media encryption using SDES-SRTP (RFC 4568, RFC 3711 and RFC 6188) or SDES-DTLS (RFC 5763, RFC 5764 and RFC 3711)
7. Support for IPv4 and IPv6 for SIP, RTP media and MSRP

