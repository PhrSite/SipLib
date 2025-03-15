---
uid: SipLib.Core
summary: *content
---
Core classes for building and parsing SIP messages.

The main classes in the SipLib.Core namespace are:

1. SIPMessage
2. SIPRequest
3. SIPResponse
4. SIPHeader
5. Classes for each type of SIP header (SIPFrom, SIPTo, etc.)

The SIPMessage class is the base class for the SIPRequest (representing a SIP request message) and the SIPResponse (representing a SIP response message) classes. The SIPMessage class contains SIP headers and optionally a SIP message body. The Header field of a SIPMessage is a SIPHeader object that contains fields for the SIP headers. Each type of SIP header is represented by a unique class. For example, the SIPFromHeader class is used to manage a SIP From header. The SIPMessage class contains methods and properties for accessing the contents of the body of a SIP message if one is present.

When building applications that receive and process SIP message, it is generally not necessary to programmatically parse raw messages received via the network because message parsing is handled by classes contained in the SipLib.Channels and SipLib.Transactions namespaces.

The SipUtils class in the SipLib.Core namespace is a static class that provides various helper methods for building SIP messages and for extracting various information from SIP messages.



