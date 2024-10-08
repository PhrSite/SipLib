﻿#region License
//-----------------------------------------------------------------------------
// Filename: SIPEndPoint.cs
//
// Description: Represents what needs to be known about a SIP end point for 
// network communications.
//
// Author(s):
// Aaron Clauson
//
// History:
// 14 Oct 2019	Aaron Clauson	Added missing header.
// 07 Nov 2019  Aaron Clauson   Added ConnectionID property.
//
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//-----------------------------------------------------------------------------
#endregion

/////////////////////////////////////////////////////////////////////////////////////
//  Revised:    15 Nov 22 PHR -- Added from sipsorcery. This version supports IPv6.
//                -- Fixed code formatting
//                -- Added documentation comments
//              12 Feb 24 PHR
//                -- Replaced SIPURI.ParseSIPURIRelaxed() with ParseSIPURI()
/////////////////////////////////////////////////////////////////////////////////////

using SipLib.Msrp;
using System.Net;
using System.Net.Sockets;

namespace SipLib.Core;

/// <summary>
/// This class is a more specific version of the SIPURI class BUT is only concerned with the network and
/// transport properties. It contains all the information needed to determine the remote end point to
/// deliver a SIP request or response to.
/// 
/// This class must remain immutable otherwise the SIP stack can develop problems. SIP end points can get
/// passed amongst different servers for logging and forwarding SIP messages and a modification of the end 
/// point by one server can result in a problem for a different server. Instead a new SIP end point should 
/// be created wherever a modification is required.
/// </summary>
public class SIPEndPoint
{
    private const string CHANNELID_ATTRIBUTE_NAME = "cid";
    private const string CONNECTIONID_ATTRIBUTE_NAME = "xid";

    private static SIPEndPoint Empty { get; } = new SIPEndPoint();

    /// <summary>
    /// The transport/application layer protocol the SIP end point is using.
    /// </summary>
    /// <value></value>
    public SIPProtocolsEnum Protocol { get; private set; } = SIPProtocolsEnum.udp;

    /// <summary>
    /// The network address for the SIP end point. IPv4 and IPv6 are supported.
    /// </summary>
    /// <value></value>
    public IPAddress? Address { get; private set; }

    /// <summary>
    /// The network port for the SIP end point.
    /// </summary>
    /// <value></value>
    public int Port { get; private set; }

    /// <summary>
    /// For connection oriented transport protocols such as TCP, TLS and WebSockets this
    /// ID can record the unique connection a SIP message was received on. This makes it 
    /// possible to ensure responses or subsequent request can re-use the same connection.
    /// </summary>
    /// <value></value>
    public string? ConnectionID { get; set; }

    /// <summary>
    /// If set represents the SIP channel ID that this SIP end point was created from.
    /// </summary>
    /// <value></value>
    public string? ChannelID { get; set; }

    private SIPEndPoint() { }

    /// <summary>
    /// Instantiates a new SIP end point from a network end point. Unspecified properties will be set to
    /// their defaults.
    /// </summary>
    /// <param name="endPoint">Input IPEndPoint to create the SIPEndPoint from</param>
    public SIPEndPoint(IPEndPoint endPoint) :
        this(SIPProtocolsEnum.udp, endPoint.Address, endPoint.Port, null, null)
    { }

    /// <summary>
    /// Instantiates a new SIP end point from a protocol, an IP address and a port number. Unspecified
    /// properties will be set to their defaults.
    /// </summary>
    /// <param name="protocol">Protocol for the SIPEndPoint</param>
    /// <param name="address">IPAddress of the SIPEndPoint</param>
    /// <param name="port">Port number of the SIPEndPoint</param>
    public SIPEndPoint(SIPProtocolsEnum protocol, IPAddress address, int port) :
        this(protocol, address, port, null, null)
    { }

    /// <summary>
    /// Creates a SIP endpoint from the protocol and an IPEndPoint. Unspecified properties will be
    /// set to their default values.
    /// </summary>
    /// <param name="protocol">Protocol for the SIPEndPoint</param>
    /// <param name="endPoint">IPEndPoint for the SIPEndPoint</param>
    public SIPEndPoint(SIPProtocolsEnum protocol, IPEndPoint endPoint) :
        this(protocol, endPoint, null, null)
    { }

    /// <summary>
    /// Constructs a new SIPEndPoint given an IPEndPoint and all parameters
    /// </summary>
    /// <param name="protocol">Protocol for the SIPEndPoint</param>
    /// <param name="endPoint">IPEndPoint for the SIPEndPoint</param>
    /// <param name="channelID">Optional. The unique ID of the channel that created the end point.
    /// </param>
    /// <param name="connectionID">Optional. For connection oriented protocols the unique ID of the
    /// connection. For connectionless protocols should be set to null.</param>
    public SIPEndPoint(SIPProtocolsEnum protocol, IPEndPoint endPoint, string? channelID,
        string? connectionID) : this(protocol, endPoint.Address, endPoint.Port, channelID, connectionID)
    { }

    /// <summary>
    /// Instantiates a new SIP end point.
    /// </summary>
    /// <param name="protocol">The SIP transport/application protocol used for the transmission.</param>
    /// <param name="address">The network address.</param>
    /// <param name="port">The network port.</param>
    /// <param name="channelID">Optional. The unique ID of the channel that created the end point.
    /// </param>
    /// <param name="connectionID">Optional. For connection oriented protocols the unique ID of the
    /// connection. For connectionless protocols should be set to null.</param>
    public SIPEndPoint(SIPProtocolsEnum protocol, IPAddress address, int port, string? channelID, string? 
        connectionID)
    {
        Protocol = protocol;
        Address = address?.IsIPv4MappedToIPv6 == true ? address.MapToIPv4() : address;
        Port = (port == 0) ? SIPConstants.GetDefaultPort(Protocol) : port;
        ChannelID = channelID;
        ConnectionID = connectionID;
    }

    /// <summary>
    /// Constructs a SIPEndPoint from a SIPURI. Unspecified parameters will be set to their default
    /// values.
    /// </summary>
    /// <param name="sipURI">Input SIPURI to build the SIPEndPoint from</param>
    // <exception cref="ApplicationException"></exception>
    public SIPEndPoint(SIPURI sipURI)
    {
        Protocol = sipURI.Protocol;

        if (IPSocket.TryParseIPEndPoint(sipURI.Host, out var endPoint) == false)
        {
            throw new ApplicationException($"Could not parse SIPURI host {sipURI.Host} as an IP end point.");
        }

        Address = endPoint.Address?.IsIPv4MappedToIPv6 == true ? endPoint.Address.MapToIPv4() : 
            endPoint.Address;
        Port = (endPoint.Port == 0) ? SIPConstants.GetDefaultPort(Protocol) : endPoint.Port;
    }

    /// <summary>
    /// Parses a SIP end point from either a serialised SIP end point string, format of:
    /// (udp|tcp|tls|ws|wss):(IPEndpoint)[;connid=abcd] or from a string that represents a SIP URI.
    /// </summary>
    /// <param name="sipEndPointStr">The string to parse to extract the SIP end point.</param>
    /// <returns>If successful a SIPEndPoint object or null otherwise.</returns>
    public static SIPEndPoint? ParseSIPEndPoint(string sipEndPointStr)
    {
        if (sipEndPointStr.IsNullOrBlank())
            return null;

        if (sipEndPointStr.ToLower().StartsWith("udp:") ||
            sipEndPointStr.ToLower().StartsWith("tcp:") ||
            sipEndPointStr.ToLower().StartsWith("tls:") ||
            sipEndPointStr.ToLower().StartsWith("ws:") ||
            sipEndPointStr.ToLower().StartsWith("wss:"))
            return ParseSerialisedSIPEndPoint(sipEndPointStr);
        else
        {
            SIPURI? sipUri = null;
            try
            {
                sipUri = SIPURI.ParseSIPURI(sipEndPointStr);
            }
            catch (SIPValidationException)
            {
                return null;
            }
            
            SIPEndPoint? sipEndPoint = sipUri?.ToSIPEndPoint();
            return sipEndPoint;
        }
    }

    /// <summary>
    /// Tries to parse a SIPEndPoint from a string.
    /// </summary>
    /// <param name="sipEndPointStr">Input string</param>
    /// <returns>Returns a new SIPEndPoint object if successful. Return null if an error occurred.
    /// </returns>
    public static SIPEndPoint? TryParse(string sipEndPointStr)
    {
        try
        {
            return ParseSIPEndPoint(sipEndPointStr);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Reverses The SIPEndPoint.ToString() method. 
    /// </summary>
    /// <param name="serialisedSIPEndPoint">The serialised SIP end point MUST be in the form 
    /// protocol:socket[;connid=abcd]. Valid examples are udp:10.0.0.1:5060 and ws:10.0.0.1:5060;
    /// connid=abcd. An invalid example is 10.0.0.1:5060.</param>
    private static SIPEndPoint ParseSerialisedSIPEndPoint(string serialisedSIPEndPoint)
    {
        string channelID = null;
        string connectionID = null;
        string endPointStr = null;
        string protcolStr = serialisedSIPEndPoint.Substring(0, serialisedSIPEndPoint.IndexOf(':'));

        if (serialisedSIPEndPoint.Contains(";"))
        {
            endPointStr = serialisedSIPEndPoint.Slice(':', ';');
            string paramsStr = serialisedSIPEndPoint.Substring(serialisedSIPEndPoint.IndexOf(';') + 1)?.
                Trim();
            SIPParameters endPointParams = new SIPParameters(paramsStr, ';');

            if (endPointParams.Has(CHANNELID_ATTRIBUTE_NAME))
                channelID = endPointParams.Get(CHANNELID_ATTRIBUTE_NAME);

            if (endPointParams.Has(CONNECTIONID_ATTRIBUTE_NAME))
                connectionID = endPointParams.Get(CONNECTIONID_ATTRIBUTE_NAME);
        }
        else
            endPointStr = serialisedSIPEndPoint.Substring(serialisedSIPEndPoint.IndexOf(':') + 1);

        if (!IPSocket.TryParseIPEndPoint(endPointStr, out var endPoint))
            throw new ApplicationException($"Could not parse SIPEndPoint host {endPointStr} as an IP end point.");

        return new SIPEndPoint(SIPProtocolsType.GetProtocolType(protcolStr), endPoint, channelID, 
            connectionID);
    }

    /// <summary>
    /// Converts this SIPEndPoint object to a string
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        if (Address == null)
            return Protocol + ":empty";
        else
        {
            IPEndPoint ep = new IPEndPoint(Address, Port);
            return $"{Protocol}:{ep}";
        }
    }

    /// <summary>
    /// Determines if two SIPEndPoints are equal.
    /// </summary>
    /// <param name="endPoint1">First SIPEndPoint object</param>
    /// <param name="endPoint2">Second SIPEndPoint object</param>
    /// <returns>Returns true if they are equal or false if they are not.</returns>
    public static bool AreEqual(SIPEndPoint endPoint1, SIPEndPoint endPoint2)
    {
        return endPoint1 == endPoint2;
    }

    /// <summary>
    /// Determines if another SIPEndPoint object is equal to this one.
    /// </summary>
    /// <param name="obj">Another SIPEndPoint object</param>
    /// <returns>Returns true if they are equal of false if they are not.</returns>
    public override bool Equals(object? obj)
    {
        return AreEqual(this, (SIPEndPoint)obj!);
    }

    /// <summary>
    /// Equals operator for the SIPEndPoint class
    /// </summary>
    /// <param name="endPoint1"></param>
    /// <param name="endPoint2"></param>
    /// <returns></returns>
    public static bool operator ==(SIPEndPoint endPoint1, SIPEndPoint endPoint2)
    {
        if ((object)endPoint1 == null && (object)endPoint2 == null)
            return true;
        else if ((object)endPoint1 == null || (object)endPoint2 == null)
            return false;
        else if (endPoint1.ToString() != endPoint2.ToString())
            return false;
        else if (endPoint1.ChannelID != null && endPoint1.ChannelID != endPoint2.ChannelID)
            return false;
        else if (endPoint1.ConnectionID != null && endPoint1.ConnectionID != endPoint2.ConnectionID)
            return false;

        return true;
    }

    /// <summary>
    /// Not equal operator for the SIPEndPoint class
    /// </summary>
    /// <param name="endPoint1"></param>
    /// <param name="endPoint2"></param>
    /// <returns></returns>
    public static bool operator !=(SIPEndPoint endPoint1, SIPEndPoint endPoint2)
    {
        return !(endPoint1 == endPoint2);
    }

    /// <summary>
    /// Computes the hash code for this object
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode()
    {
        return Protocol.GetHashCode() + Address.GetHashCode() + Port.GetHashCode()
            + (ChannelID != null ? ChannelID.GetHashCode() : 0)
            + (ConnectionID != null ? ConnectionID.GetHashCode() : 0);
    }

    /// <summary>
    /// Creates a deep copy of this SIPEndPoint object
    /// </summary>
    /// <returns></returns>
    public SIPEndPoint CopyOf()
    {
        return new SIPEndPoint(Protocol, new IPAddress(Address.GetAddressBytes()), Port, ChannelID,
            ConnectionID);
    }

    /// <summary>
    /// Get the IP end point from the SIP end point
    /// </summary>
    /// <param name="mapIpv4ToIpv6">Set to true if a resultant IPv4 end point should be mapped to IPv6.
    /// This is required in some cases when using dual mode sockets. For example Mono requires that a
    /// destination IP end point for a dual mode socket is set as IPv6.</param>
    /// <returns>An IP end point.</returns>
    public IPEndPoint GetIPEndPoint(bool mapIpv4ToIpv6 = false)
    {
        if (mapIpv4ToIpv6 && Address.AddressFamily == AddressFamily.InterNetwork)
            return new IPEndPoint(Address.MapToIPv6(), Port);
        else
            return new IPEndPoint(Address!, Port);
    }

    /// <summary>
    /// Determines whether the socket destination for two different SIP end points are equal.
    /// </summary>
    /// <param name="endPoint1">First end point to compare.</param>
    /// <param name="endPoint2">Second end point to compare.</param>
    /// <returns>True if the end points both resolve to the same protocol and IP end point.</returns>
    public static bool AreSocketsEqual(SIPEndPoint endPoint1, SIPEndPoint endPoint2)
    {
        if (endPoint1 == Empty || endPoint2 == Empty)
            return false;
        else
        {
            IPAddress ep1Address = (endPoint1.Address.IsIPv4MappedToIPv6) ? endPoint1.Address.MapToIPv4() :
                endPoint1.Address;
            IPAddress ep2Address = (endPoint2.Address.IsIPv4MappedToIPv6) ? endPoint2.Address.MapToIPv4() :
                endPoint2.Address;

            return endPoint1.Protocol == endPoint2.Protocol && endPoint1.Port == endPoint2.Port &&
                ep1Address.Equals(ep2Address) ;
        }
    }

    /// <summary>
    /// Determines if the socket destination of a SIPEndPoint object is equal to the socket destination
    /// of this SIPEndPoint object
    /// </summary>
    /// <param name="endPoint"></param>
    /// <returns>True if the end points both resolve to the same protocol and IP end point.</returns>
    public bool IsSocketEqual(SIPEndPoint endPoint)
    {
        return AreSocketsEqual(this, endPoint);
    }
}
