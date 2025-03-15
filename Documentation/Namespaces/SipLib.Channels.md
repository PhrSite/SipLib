---
uid: SipLib.Channels
summary: *content
---
Classes for sending and receiving SIP messages over UDP, TCP or TLS connections.

The main classes in this namespace are:
1. SIPChannel
2. SIPUDPChannel
3. SIPTCPChannel
4. SIPTLSChannel
5. SIPConnection

The SIPChannel class is the base class for the SIPUDPChannel, SIPTCPChannel and SIPTLSChannel classes. The SIPUDPChannel manages SIP packet transport using UDP. The SIPTCPChannel class manages SIP packet transport using TCP and the SIPTLSChannel manages SIP packet transport using TLS.

TCP and TLS are connection oriented stream protocols so the SIPTCPChannel and SIPTLSChannel manage multiple transport layer connections by maintaining a list of SIPConnection objects. Each SIPConnection object represents a socket connection between a local endpoint and a remote endpoint.
