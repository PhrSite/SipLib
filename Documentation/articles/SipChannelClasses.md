# Introduction
The [SIPChannel](~/api/SipLib.Channels.SIPChannel.yml) class is the base class for all classes that manage low-level SIP connections and basic SIP message transport for sending and receiving SIP messages.

This class library provides the following classes derived from the SIPChannel class. The SIPChannel class is abstract and is therefore never used directly.

| Class | Transport Protocol | Description |
|--------|-------------------|-------------|
| [SIPUDPChannel](~/api/SipLib.Channels.SIPUDPChannel.yml) | UDP | Provides functions for sending and receiving SIP messages using the User Datagram Protocol (UDP). |
| [SIPTCPChannel](~/api/SipLib.Channels.SIPTCPChannel.yml) | TCP | Provides functions for sending and receiving SIP messages using the Transport Control Protocol (TCP). Manages persistent TCP connections to multiple remote endpoints. |
| [SIPTLSChannel](~/api/SipLib.Channels.SIPTLSChannel.yml) | TLS | Provides functions for sending and receiving SIP messages using the Transport Layer Security (TLS) protocol. Manages persistent TLS connections to multiple remote endpoints. Supports mutual client certificate authentication. Supports TLS v1.3 if the network stack of the operating system does.|

Each of these classes provides the following functionality.
1. The ability to send SIP messages to a remote endpoint
2. Notifies the class user when a complete SIP message is received via an event
3. Differentiated Services (DiffServ) packet marking for network based Quality of Service control
4. Operation using IPv4 or IPv6
4. Client and server operation (SIPTCPChannel and SIPTLSChannel only)

The SIPChannel derived classes are fairly low-level in that they do not manage SIP transactions and SIP dialogs. Applications normally create a SIPChannel derived class and pass that object to a [SipTransport](~/api/SipLib.Channels.SipTransport.yml) object. The SipTransport class provides methods for sending and receiving SIP messages as well as SIP message transaction management. See the article entitled [SIP Message Transport and Transaction Management](~/articles/SipTransportManagement.md)

## Local End Point Requirements
Each instance of a SIPChannel derived class binds to a specific local IP endpoint. The IP address of the local IP endpoint may be an IPv4 or an IPv6 address. The IPAddress component of the local IP endpoint must be a specific address, it may not be a general network address such as IPAddress.Any or IPAddress.IPv6Any (0.0.0.0 or 0:0:0:0:0:0:0:0)

The local endpoint is passed as an IPEndPoint object in the constructor of each SIPChannel derived class.

For each combination of IP network type (IPv4, IPv6) and protocol type (UDP, TCP, TLS) each instance of a SIPChannel derived class must have a unique IP address and port combination. The port number must be non-zero and must be a port that is not currently in use by the operating system, another application or service.

## Connection Management
The SIPTCPChannel and SIPTLSChannel classes maintain a list of persistent connections. Connections are represented by the [SIPConnection](~/api/SipLib.Channels.SIPConnection.yml) class. Each SIPConnection object is used by the channel class for sending and receiving SIP message to a unique IP endpoint.

Each instance of the SIPTCPChannel and SIPTLSChannel classes maintains a dictionary of up to 1000 connections. Each connection is uniquely identified by the string version of the remote IP endpoint (for excample: "192.168.1.200:5060").

Connections may be created by a remote client or by the SIP channel class.

When the application calls the Send() method of a SIP channel class, the SIP channel class checks its dictionary of connections to see if an available SIPConnection object is available to send the SIP message. If an open connection is available, then the SIP channel class uses that SIPConnection to send the message. If a connection is not available yet, then the SIP channel class establishes a TCP or TLS connection to the remote endpoint, then sends the SIP message.

Connections are maintained by each SIP channel class until one of the followng events occur.
1. The connection is closed by the remote endpoint. The channel class fires an event called SIPConnectionDisconnected if this occurs.
2. The connection is closed by the SIP channel class
3. The SIP channel class detects an abnormal disconnect event. The channel class fires an event called SIPConnectionDisconnected if this occurs.
4. The SIP channel class closes the connection due to lack of SIP message activity.

Each SIP channel class closes all active connections when the Close() method is called.

The SIP message inactivity interval is set in the software to 180 minutes. If no SIP messages are sent or received over a persistent SIP connection for longer than this interval, then the SIP channel class will automatically close the connection to that remote endpoint. The purpose of this inactivity timer is to automatically clean stale or inactive connections that may occur under unusual connection failure conditions. Applications can avoid automatic connection closures by periodically sending SIP OPTIONS messages to all remote endpoints.

The number of TCP or TLS connections does not limit the number of SIP calls that an application can handled because each connection can handle multiple SIP calls simultaneously.

## Connection Security Features
The SIPChannel derived classes allow applications to implement security based on user defined access control lists and in the case of TLS channels, applications may implement mutual authentication using X.509 certificates.
### Connection Based Access Control (UDP, TCP, TLS)
SIPUDPChannel, SIPTCPChannel and SIPTLSChannel support user defined access control via a delegate method called AcceptConnection. The following shows the delegate type for the AcceptConnection callback function.
```
public delegate bool AcceptConnectionDelegate(SIPProtocolsEnum protocol,
	IPEndPoint remoteEndPoint);
```

If the implemented AcceptConnectionDelegate returns false then the SIPChannel derived class will reject the connection request, else it will accept the connection request.

SIPUDPChannel operates over UDP, which is a connectionless transport, so SIPUDPChannel calls the AcceptConnection delegate for every UDP packet that it receives.

Perform the following steps to use connection control for a SIP channel class.
1. Implement a AcceptConnectionDelegate in your call controller class or in another appropriate place in the application.
2. Create an instance of a SIPChannel derived class.
3. Pass the AcceptionConnection in the constructor of the SIPChannel derived class.

The AcceptionConnection callback parameter of the SIPChannel derived classes defaults to null so the default operation is to accept all connections.

An application can use the SIP protocol type and the IPEndPoint of the remote endpoint point to decide whether or not to accept the connection request. Applications are also free to implement a different AcceptConnectionDelegate method for each SIP channel object in the event that there are multiple SIP channels being used.

### Certificate Based Access Control (TLS)
The SIPTLSChannel class allows the class user (the application) to perform custom mutual authentication based on client and server X.509 certificates.

Certificate based mutual authentication is controlled by the following three constructor parameters of the SIPTLSChannel class.
1. The UseMutualAuth parameter of the class's constructor. This parameter defaults to true.
2. The acceptClientCertificate callback parameter.
3. The acceptServerCertificate callback parameter.

If the UseMutualAuth constructor parameter is true, then the SIPTLSChannel class will only allow connection requests from clients that provide an X.509 certificate during the TLS connection handshake process. The SIPTLSChannel class will also provide its X.509 certificate during the TLS handshake process when it is connecting as a client.

If the UseMutualAuth constructor parameter is false, then the SIPTLSChannel class will allow clients to connect even if they don't provide an X.509 certificate. Also, the SIPTLSChannel class will not provided its X.509 certificate when connecting as a client.

The delegate type for the acceptClientCertificate and the acceptServerCertificate callback parameters is a AcceptCertificateDelegate as shown below.
```
public delegate bool AcceptCertificateDelegate(X509Certificate? certificate,
    X509Chain? chain,
    SslPolicyErrors? sslPolicyErrors);
```
If the acceptClientCertificate callback parameter is null then the SIPTLSChannel class will accept all client connection requests. If the acceptClientCertificate callback parameter is not null and returns true then the SIPTLSChannel class will accept the client's connection request. If this callback function returns false then the SIPTLSChannel will refuse the client's connection request.

If the acceptServerCertificate callback parameter is null then the SIPTLSChannel class will allow connections to a server regardless of the server's certificate. If the acceptServerCertificate callback parameter is not null and returns true then the SIPTLSChannel class will allow the connection to the server. If this function returns false then the SIPTLSChannel class will block the connection to the server.

## Maximum SIP Message Sizes
The maximum SIP message that can be sent via UDP using the SIPUDPChannel class is 65547 (65535 minus 8 bytes for the UDP header and 20 bytes for the IP header). An attempt to send a message longer this limit will result in an ArgumentException.

TCP and TLS are stream protocols. Ths maximum SIP message that may be sent using the SIPTCPChannel and SIPTLSChannel classes is 200,000 bytes in length. This is set within the software.

## Quality of Service (QOS) and DSCP
Each SIPChannel derived class sets the Differentiated Services Code Point (DSCP) for IPv4 and IPv6 to a value of 0x03 for SIP signaling. Each IP packet that is sent is marked with this DSCP value. This applies to both Windows and Linux platforms.

Section 2.7 of the document entitled "National Emergency Number Association's (NENA) Standard for NG9-1-1" specifies the default DSCP value of 0x03. See [NENA-STA-010.3b](https://cdn.ymaws.com/www.nena.org/resource/resmgr/standards/nena-sta-010.3b-2021_i3_stan.pdf).

For SIPTCPChannel and SIPTLSChannel, the DSCP is set for each client or server connection. For SIPUDPChannel, the DSCP is sent for the UdpClient object that is used for sending SIP packets.
