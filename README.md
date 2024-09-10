# Introduction
This project is a portable, cross-platform class library written in the C# language that targets the .NET 8.0 environment. It may be used by applications that target the Windows Desktop(version 10 or later), Windows Server or Linux operating systems.

The classes in this library support the following protocols.
1. Session Initiation Protocol (SIP, RFC 3261) over UDP, TCP and TLS
2. Session Description Protocol (SDP, RFC 8866)
3. An Offer/Answer Model with the Session Description Protocol (SDP) (RFC 3264)
3. Real Time Protocol (RTP, RFC 3550) for transport of audio, video and Real Time Text
4. Real Time Text (RTT, RFC 4103)
5. Message Session Relay Protocol (MSRP, RFC 4975) using TCP or TLS
6. RTP media encryption using SDES-SRTP (RFC 4568, RFC 3711 and RFC 6188) or SDES-DTLS (RFC 5763, RFC 5764 and RFC 3711)
7. Support for IPv4 and IPv6 for SIP, RTP media and MSRP

The primary focus of this project is to provide protocol support for developing Next Generation 9-1-1 (NG9-1-1) functional elements and applications in .NET. This class library provides support for several NG9-1-1 specific requirements such as:
1. The SIP Geolocation related headers (RFC 6442)
2. The use of the SIP Call-Info header for support of caller location data, additional data about a call (RFC 7852) and NG9-1-1 specific call and incident identifiers
3. Use of multipart/mixed content types in SIP message bodies for the SDP, location data and additional call data by-value
4. Quality of Service (QOS) management using IP DSCP packet marking for SIP and media packets
5. Various other requirements defined in the National Emergency Number Association's (NENA) standard for NG9-1-1. See [NENA-STA-010.3b](https://cdn.ymaws.com/www.nena.org/resource/resmgr/standards/nena-sta-010.3b-2021_i3_stan.pdf).

As a basic protocol class library, this project does not provide implementation of SIP user agents or device specific media endpoints as these components are very application specific.

This class library, incombination with the following NG9-1-1 related class library projects may be used to build a variety of NG9-1-1 applications.
1. [Ng911Lib](https://github.com/PhrSite/Ng911Lib)
2. [EidoLib](https://github.com/PhrSite/EidoLib)
3. [Ng911CadIfLib](https://github.com/PhrSite/Ng911CadIfLib)

# Documentation
The documentation pages project for this project is called [SipLibDocumentation](https://phrsite.github.io/SipLibDocumentation). The documentation web site includes class documentation and articles that explain the usage of the classes in this library.

# Installation
This class library is available on NuGet.

To install it from the .NET CLI, type:

```
dotnet add package SipLib --version X.X.X
```
"X.X.X" is the version number of the packet to add.

To install using the NuGET Package Manager Command window, type:

```
NuGet\Install-Package SipLib --version X.X.X
```
Or, you can install it from the Visual Studio GUI.

1. Right click on a project
2. Select Manage NuGet Packages
3. Search for SipLib
4. Click on Install

# External Dependancies
The SipLib class library uses the following NuGet packages.
1. Portable.BouncyCastle
2. Microsoft.Extensions.Logging

# Project Structure

## ClassLibrary Directory
This directory contains the project files for the SipLib project and the following subdirectories that contain the source code.

| Directory | Description |
|--------|--------|
| Body | Contains classes for working with the body of SIP messages. |
| Channels | Classes for sending and receiving SIP messages over UDP, TCP or TLS connections. |
| Collections | Contains thread-safe generic collection classes that are not provided by the .NET class libraries |
| Core | Core classes for building and parsing SIP messages. |
| Dtls | Classes required to support encryption and decryption of media (audio, video and Real Time Text) using the Datagram Transport Layer Security  DTLS specified in RFC 5763 and RFC 5764. |
| Logging | Contains a static class called SipLogger that the classes in this class library can use for logging application messages. |
| Media | Classes for encoding and decoding audio. The supported codecs are G.711 Mu-Law, G.711 A-Law and G.722. |
| Msrp | Message Session Relay Protocol (MSRP, see RFC 4975) related classes. |
| Network | Contains a utility helper class for performing network protocol related functions. |
| RealTimeText | Classes for the Real Time Text (RTT, see RFC 4103) protocol. |
| Rtp | Classes for the Real Time Protocol (RTP, see RFC 3550). The main class is called RtpChannel. This class supports unencrypted RTP media as well as encryption using the SDES-SRTP (RFC 3711, RFC 4568) and SDES-DTLS (RFC 5763, RFC 5764) protocols. |
| RtpCrypto | Classes that implement the SDES-SRTP protocols used in secure RTP. |
| Sdp | Classes used for the Session Description Protocol (SDP, see RFC 8866) |
| SipTransactions | Classes for managing SIP transactions |
| Video | Classes for packing and unpacking video frames for use in RTP channels for H.264 and VP8 video. The H.264 and VP8 codecs are not included here. |

## Testing Directory
This directory contains a subdirectory called SipLibUnitTests. This directory contains the SipLibUnitTests.sln Visual Studio solution file and several subdirectories containing the source code for the SipLibUnitTests project. This project is an XUnit project for performing unit tests for the SipLib class library.

To run the unit tests:
1. Open the SipLibUnitTests.sln file using Visual Studio 2022 (or later).
2. Right click on the SipLibUnitTests project in Visual Studio's Solution Explorer window.
3. Select Run Tests

The following table is a guide to the subdirectories of the SipLibUnitTests project.

| Directory | Description |
|--------|--------|
| Body | Unit tests for the SipLib.Body namespace |
| Core | Unit tests for the classes in the SipLib.Core namespace. Also includes source code for th SIP torture tests specified in [RFC 4475](https://datatracker.ietf.org/doc/html/rfc4475) and [RFC 5118](https://datatracker.ietf.org/doc/html/rfc5118) |
| DtlsSrtp | Unit tests for the Sip.Dtls namespace classes |
| Msrp | Unit tests for the classes in the SipLib.Msrp namespace |
| MsrpMessages | Data files for the unit tests in the Msrp directory |
| RealTimeText | Source code for the unit tests for classes in the SipLib.RealTimeText namespace |
| rfc4475tests | Data files for the SIP torture tests for [RFC 4475](https://datatracker.ietf.org/doc/html/rfc4475). |
| Rtp | Unit tests for the classes in the SipLib.Rtp namespace |
| RtpCrypto | Unit tests for the classes in the SipLib.RtpCrypto namespace |
| Sdp | Unit tests for the classes in the SipLib.Sdp namespace |
| SipMessages | Data files containing SIP messages for the unit tests in the Body directory |
| SipTransactions | Unit tests for the classes in the SipLib.Transactions namespace |




