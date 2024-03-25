#region License
//-----------------------------------------------------------------------------
// Filename: SIPURI.cs
//
// Description: SIP URI.
//
// Author(s):
// Aaron Clauson (aaron@sipsorcery.com)
// 
// History:
// 09 Apr 2006	Aaron Clauson	Created, Dublin, Ireland.
//
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//-----------------------------------------------------------------------------
#endregion

/////////////////////////////////////////////////////////////////////////////////////
//  Revised:    15 Nov 22 PHR -- Added from sipsorcery. This version supports IPv6.
//                -- Fixed code formatting
//                -- Added code in ParseSIPURI to format the user part of the URI
//                   for tel: URIs.
//                -- Added documentation comments
//              21 Jul 23 PHR
//                -- Fixed the handling of the // characters for the http,
//                   https, msrp, msrps, ws, wss schemes.
//              12 Feb 24 PHR
//                -- Removed ParseSIPURIRelaxed()
//              25 Mar 24 PHR
//                -- Added a special test for urn scheme SIPURIs to handle the possibility
//                   of multiple ":" characters in the host portion so as not to
//                   confuse the host for an IPv6 address.
/////////////////////////////////////////////////////////////////////////////////////

using System.Net;
using System.Text.RegularExpressions;

namespace SipLib.Core;

/// <summary>
/// Implements the SIP URI concept from the SIP RFC3261. This class also handles URIs for
/// http, https, ws, wss, msrp, msrps and im schemes.
/// </summary>
public class SIPURI
{
    private const char SCHEME_ADDR_SEPARATOR = ':';
    private const char USER_HOST_SEPARATOR = '@';
    private const char PARAM_TAG_DELIMITER = ';';
    private const char HEADER_START_DELIMITER = '?';
    private const char HEADER_TAG_DELIMITER = '&';
    private const char TAG_NAME_VALUE_SEPERATOR = '=';

    private static char[] m_invalidSIPHostChars = new char[] { ',', '"' };

    private static SIPProtocolsEnum m_defaultSIPTransport = SIPProtocolsEnum.udp;
    private static SIPSchemesEnum m_defaultSIPScheme = SIPSchemesEnum.sip;
    private static string m_sipRegisterRemoveAll = SIPConstants.SIP_REGISTER_REMOVEALL;
    private static string m_uriParamTransportKey = SIPHeaderAncillary.SIP_HEADERANC_TRANSPORT;

    /// <summary>
    /// SIP scheme (sip, sips, etc.)
    /// </summary>
    /// <value></value>
    public SIPSchemesEnum Scheme = m_defaultSIPScheme;

    /// <summary>
    /// User portion of the URI
    /// </summary>
    /// <value></value>
    public string? User;

    /// <summary>
    /// Host portion of the URI.
    /// </summary>
    /// <value></value>
    public string? Host;

    /// <summary>
    /// URI parameters
    /// </summary>
    /// <value></value>
    public SIPParameters Parameters = new SIPParameters();

    /// <summary>
    /// Embedded URI headers
    /// </summary>
    /// <value></value>
    public SIPParameters Headers = new SIPParameters();

    /// <summary>
    /// The protocol for a SIP URI is dictated by the scheme of the URI and then by the transport 
    /// parameter and finally by the use of a default protocol. If the URI is a sips one then the 
    /// protocol must be TLS. After that if there is a transport parameter specified for the URI it 
    /// dictates the protocol for the URI. Finally if there is no transport parameter for a sip
    /// URI then the default UDP transport is used.
    /// </summary>
    /// <value></value>
    public SIPProtocolsEnum Protocol
    {
        get
        {
            if (Scheme == SIPSchemesEnum.sips)
                return SIPProtocolsEnum.tls;
            else if (Scheme == SIPSchemesEnum.msrp)
                return SIPProtocolsEnum.tcp;
            else if (Scheme == SIPSchemesEnum.msrps)
                return SIPProtocolsEnum.tls;
            else
            {
                if (Parameters.Has(m_uriParamTransportKey))
                {
                    if (SIPProtocolsType.IsAllowedProtocol(Parameters.Get(m_uriParamTransportKey)!))
                        return SIPProtocolsType.GetProtocolType(Parameters.Get(m_uriParamTransportKey)!);
                }

                return m_defaultSIPTransport;
            }
        }
        set
        {
            if (value == SIPProtocolsEnum.udp)
            {
                Scheme = SIPSchemesEnum.sip;
                if (Parameters.Has(m_uriParamTransportKey))
                    Parameters.Remove(m_uriParamTransportKey);
            }
            else
                Parameters.Set(m_uriParamTransportKey, value.ToString());
        }
    }

    /// <summary>
    /// Gets a string that can be used to compare SIP URI addresses.
    /// </summary>
    /// <value></value>
    public string CanonicalAddress
    {
        get
        {
            string canonicalAddress = Scheme + ":";
            canonicalAddress += !String.IsNullOrEmpty(User) ? User + "@" : null;

            // First expression is for IPv6 addresses with a port.
            // Second expression is for IPv4 addresses and hostnames with a port.
            if (Host!.Contains("]:") ||
                (Host.IndexOf(':') != -1 && Host.IndexOf(':') == Host.LastIndexOf(':')))
                canonicalAddress += Host;
            else
                canonicalAddress += Host + ":" + SIPConstants.GetDefaultPort(Protocol);

            return canonicalAddress;
        }
    }

    /// <summary>
    /// Gets the host address portion of the URI. If the address is an IPv6 address, then it will
    /// include the [ and ] delimiters.
    /// </summary>
    /// <value></value>
    public string HostAddress
    {
        get
        {
            //rj2: colon might be IPv6 delimiter, not port delimiter, check first against IPv6 with Port
            //notation, and then the occurrence of multiple colon
            if (Host!.IndexOf("]:") > 0)
                return Host.Substring(0, Host.IndexOf("]:") + 1);
            //if there are multiple colon, it's IPv6 without port, else IPv4 with port
            else if (Host.IndexOf(':') > 0 && Host.IndexOf(':') != Host.LastIndexOf(':'))
                return Host;
            else if (Host.IndexOf(':') > 0)
                return Host.Substring(0, Host.IndexOf(":"));

            return Host;
        }
    }

    /// <summary>
    /// Gets the maddr parameter or the host address if the maddr parameter is not present
    /// </summary>
    /// <value></value>
    public string MAddrOrHostAddress
    {
        get
        {
            return this.MAddr ?? this.HostAddress;
        }
    }

    /// <summary>
    /// Gets the maddr parameter or null if it is not present
    /// </summary>
    /// <value></value>
    public string? MAddr
    {
        get
        {
            if (this.Parameters.Has(SIPHeaderAncillary.SIP_HEADERANC_MADDR))
                return this.Parameters.Get(SIPHeaderAncillary.SIP_HEADERANC_MADDR);
            else
                return null;
        }
    }

    /// <summary>
    /// Gets the port portion of the host address or null if it is not present
    /// </summary>
    /// <value></value>
    public string? HostPort
    {
        get
        {
            //rj2: colon might be IPv6 delimiter, not port delimiter, check first against IPv6 with
            //Port notation, and then the occurrence of multiple colon
            if (Host!.IndexOf("]:") > 0)
                return Host.Substring(Host.IndexOf("]:") + 2);
            //if there are multiple colon, it's IPv6 without port, else IPv4 with port
            else if (Host.IndexOf(':') > 0 && Host.IndexOf(':') != Host.LastIndexOf(':'))
                return null;
            else if (Host.IndexOf(':') > 0)
                return Host.Substring(Host.IndexOf(":") + 1);
            else
                return null;
        }
    }

    /// <summary>
    /// Gets the un-escapted user part of the URI or null if there is no user part.
    /// </summary>
    /// <value></value>
    public string? UnescapedUser
    {
        get
        {
            //return (User.IsNullOrBlank()) ? User : SIPEscape.SIPURIUserUnescape(User);
            if (string.IsNullOrEmpty(User))
                return User;
            else
                return SIPEscape.SIPURIUserUnescape(User);

        }
    }

    private SIPURI()
    { }

    /// <summary>
    /// Constructs a new SIPURI from the user, host and parameters and headers strings.
    /// Unspecified parameters are set to their default values
    /// </summary>
    /// <param name="user">User part of the URI. Optional</param>
    /// <param name="host">Host portion of the URI. Required</param>
    /// <param name="paramsAndHeaders">URI parameters and headers. Optional</param>
    public SIPURI(string user, string host, string paramsAndHeaders)
    {
        User = user;
        Host = host;
        ParseParamsAndHeaders(paramsAndHeaders);
    }

    /// <summary>
    /// Constructs a new SIPURI from the user, host, parameters and headers and scheme.
    /// Unspecified parameters are set to their default values
    /// </summary>
    /// <param name="user">User part of the URI. Optional</param>
    /// <param name="host">Host portion of the URI. Required</param>
    /// <param name="paramsAndHeaders">URI parameters and headers. Optional</param>
    /// <param name="scheme">URI scheme.</param>
    public SIPURI(string user, string host, string paramsAndHeaders, SIPSchemesEnum scheme)
    {
        User = user;
        Host = host;
        ParseParamsAndHeaders(paramsAndHeaders);
        Scheme = scheme;
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="user">User part of the URI. Optional</param>
    /// <param name="host">Host portion of the URI. Required</param>
    /// <param name="paramsAndHeaders">URI parameters and headers. Optional</param>
    /// <param name="scheme">URI scheme.</param>
    /// <param name="protocol">Transport protocol</param>
    public SIPURI(string user, string host, string paramsAndHeaders, SIPSchemesEnum scheme, 
        SIPProtocolsEnum protocol)
    {
        User = user;
        Host = host;
        ParseParamsAndHeaders(paramsAndHeaders);
        Scheme = scheme;

        // if (protocol != SIPProtocolsEnum.udp && scheme != SIPSchemesEnum.sips)
        // 24 Jul 23 PHR
        if (protocol != SIPProtocolsEnum.udp && scheme != SIPSchemesEnum.sips && scheme != 
            SIPSchemesEnum.msrp && scheme != SIPSchemesEnum.msrps)
            Parameters.Set(m_uriParamTransportKey, protocol.ToString());
    }

    /// <summary>
    /// Constructs a new SIPURI from the scheme and a SIPEndPoint
    /// Unspecified parameters are set to their default values
    /// </summary>
    /// <param name="scheme">Scheme</param>
    /// <param name="sipEndPoint">SIPEndPoint to build the SIPURI from</param>
    public SIPURI(SIPSchemesEnum scheme, SIPEndPoint sipEndPoint)
    {
        Scheme = scheme;
        Host = sipEndPoint.GetIPEndPoint().ToString();

        //if (sipEndPoint.Protocol != SIPProtocolsEnum.udp && scheme != SIPSchemesEnum.sips)
        // 24 Jul 23 PHR
        if (sipEndPoint.Protocol != SIPProtocolsEnum.udp && scheme != SIPSchemesEnum.sips && scheme !=
            SIPSchemesEnum.msrp && scheme != SIPSchemesEnum.msrps)
            Parameters.Set(m_uriParamTransportKey, sipEndPoint.Protocol.ToString());
    }

    /// <summary>
    /// Constructs a new SIPURI from a scheme, an IPAddress and a port number
    /// Unspecified parameters are set to their default values
    /// </summary>
    /// <param name="scheme">Scheme</param>
    /// <param name="address">IPAddress -- may be IPv4 or IPv6</param>
    /// <param name="port">Port number</param>
    public SIPURI(SIPSchemesEnum scheme, IPAddress address, int port)
    {
        Scheme = scheme;
        if (address != null)
            Host = address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 ? 
                $"[{address}]:{port}" : $"{address}:{port}";
    }

    /// <summary>
    /// Parses a string into a SIPURI object.
    /// </summary>
    /// <param name="uri">Input string</param>
    /// <returns>Returns a new SIPURI object</returns>
    /// <exception cref="SIPValidationException">Thrown if the input string is not a valid SIPURI</exception>
    public static SIPURI ParseSIPURI(string uri)
    {
        try
        {
            SIPURI sipURI = new SIPURI();

            if (String.IsNullOrEmpty(uri))
            {
                throw new SIPValidationException(SIPValidationFieldsEnum.URI, 
                    "A SIP URI cannot be parsed from an empty string.");
            }
            else
            {
                if (uri == m_sipRegisterRemoveAll)
                    sipURI.Host = m_sipRegisterRemoveAll;
                else
                {
                    int colonPosn = uri.IndexOf(SCHEME_ADDR_SEPARATOR);

                    if (colonPosn == -1)
                        throw new SIPValidationException(SIPValidationFieldsEnum.URI, 
                            "SIP URI did not contain compulsory colon");
                    else
                    {
                        try
                        {
                            sipURI.Scheme = SIPSchemesType.GetSchemeType(uri.Substring(0, colonPosn));
                        }
                        catch
                        {
                            throw new SIPValidationException(SIPValidationFieldsEnum.URI, 
                                SIPResponseStatusCodesEnum.UnsupportedURIScheme, "SIP scheme " + 
                                uri.Substring(0, colonPosn) + " was not understood");
                        }

                        string uriHostPortion = uri.Substring(colonPosn + 1);
                        int ampPosn = uriHostPortion.IndexOf(USER_HOST_SEPARATOR);
                        int paramHeaderPosn = -1;
                        if (ampPosn != -1)
                            paramHeaderPosn = uriHostPortion.IndexOfAny(new char[] { PARAM_TAG_DELIMITER, 
                                HEADER_START_DELIMITER }, ampPosn);
                        else
                            // Host only SIP URI.
                            paramHeaderPosn = uriHostPortion.IndexOfAny(new char[] { PARAM_TAG_DELIMITER, 
                                HEADER_START_DELIMITER });

                        if (ampPosn != -1 && paramHeaderPosn != -1)
                        {
                            sipURI.User = uriHostPortion.Substring(0, ampPosn);
                            sipURI.Host = uriHostPortion.Substring(ampPosn + 1, paramHeaderPosn -
                                ampPosn - 1);
                            string paramsAndHeaders = uriHostPortion.Substring(paramHeaderPosn);

                            sipURI.ParseParamsAndHeaders(paramsAndHeaders);
                        }
                        else if (ampPosn == -1 && paramHeaderPosn == 0)
                            throw new SIPValidationException(SIPValidationFieldsEnum.URI,
                                "No Host portion in SIP URI");
                        else if (ampPosn == -1 && paramHeaderPosn != -1)
                        {
                            sipURI.Host = uriHostPortion.Substring(0, paramHeaderPosn);
                            string paramsAndHeaders = uriHostPortion.Substring(paramHeaderPosn);
                            sipURI.ParseParamsAndHeaders(paramsAndHeaders);
                        }
                        else if (ampPosn != -1)
                        {
                            sipURI.User = uriHostPortion.Substring(0, ampPosn);
                            // 21 Jul 23 PHR -- In case the scheme is http, msrp, ws, etc.
                            sipURI.User = sipURI.User.Replace("//", "");
                            sipURI.Host = uriHostPortion.Substring(ampPosn + 1, uriHostPortion.Length - 
                                ampPosn - 1);
                        }
                        else
                        {
                            sipURI.Host = uriHostPortion;
                            // 25 Mar 25 PHR
                            // If the scheme is urn, then everything to the right of urn: is considered the host
                            if (sipURI.Scheme == SIPSchemesEnum.urn)
                                return sipURI;

                            // 21 Jul 23 PHR -- In case the scheme is http, msrp, ws, etc.
                            sipURI.Host = sipURI.Host.Replace("//", "");
                        }

                        if (sipURI.Host.IndexOfAny(m_invalidSIPHostChars) != -1)
                            throw new SIPValidationException(SIPValidationFieldsEnum.URI, 
                                "The SIP URI host portion contained an invalid character.");
                        else if (sipURI.Host.IndexOf(':') != sipURI.Host.LastIndexOf(':'))
                        {
                            // If the host contains multiple ':' characters then it must be an IPv6
                            // address which requires a start '[' and an end ']'.
                            if (sipURI.Host.ToCharArray()[0] != '[')
                                throw new SIPValidationException(SIPValidationFieldsEnum.URI, 
                                    "The SIP URI host portion contained an IPv6 address that was " + 
                                    "missing the start '['.");
                            else if (!sipURI.Host.EndsWith("]") &&
                                (sipURI.Host.ToCharArray().Length < sipURI.Host.LastIndexOf(':') + 1 ||
                                sipURI.Host.ToCharArray()[sipURI.Host.LastIndexOf(':') - 1] != ']'))
                                throw new SIPValidationException(SIPValidationFieldsEnum.URI, 
                                    "The SIP URI host portion contained an IPv6 address that was missing the end ']'.");

                            //rj2: apply robustness principle mentioned in RFC 5118 4.10
                            while (sipURI.Host.Contains(":::"))
                            {
                                sipURI.Host = sipURI.Host.Replace(":::", "::");
                            }
                        }
                    }
                }

                // 15 Nov 22 PHR
                if (sipURI.User == null && (sipURI.Scheme == SIPSchemesEnum.sip || sipURI.Scheme == 
                    SIPSchemesEnum.sips || sipURI.Scheme == SIPSchemesEnum.tel) && sipURI.Host !=
                    null && sipURI.Host.IndexOf("+1") >= 0)
                {   // The URI is of the format sip:+1xxxxxxxxxx or 
                    // tel:+1xxxxxxxxxx. This handles the North American 
                    // Numbering plan (NANP) only.
                    sipURI.User = sipURI.Host.Replace("+1", "").Trim();
                }

                if (sipURI.Scheme == SIPSchemesEnum.tel && sipURI.User != null)
                    sipURI.User = sipURI.User.Replace(" ", "").Replace("-", "");

                return sipURI;
            }
        }
        catch (SIPValidationException)
        {
            throw;
        }
        catch (Exception)
        {
            throw new SIPValidationException(SIPValidationFieldsEnum.URI, "Unknown error parsing SIP URI.");
        }
    }

    /// <summary>
    /// Tests to see if a string can be parsed into a SIPURI object.
    /// </summary>
    /// <param name="uriStr">Input string</param>
    /// <param name="uri">Output SIPURI object. Set to a valid SIPURI object if successful</param>
    /// <returns>Returns true if successful or false if an error occurred.</returns>
    public static bool TryParse(string uriStr, out SIPURI? uri)
    {
        try
        {
            //uri = ParseSIPURIRelaxed(uriStr);
            uri = ParseSIPURI(uriStr);
            return (uri! != null!);
        }
        catch
        {
            uri = null;
            return false;
        }
    }

    /// <summary>
    /// Converts this SIPURI object into a string
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        try
        {
            string uriStr = Scheme.ToString() + SCHEME_ADDR_SEPARATOR;
            // 21 Jul 23 PHR
            if (Scheme == SIPSchemesEnum.http || Scheme == SIPSchemesEnum.https || Scheme ==
                SIPSchemesEnum.msrp || Scheme == SIPSchemesEnum.msrps || Scheme == SIPSchemesEnum.ws ||
                Scheme == SIPSchemesEnum.wss)
                uriStr = uriStr + "//";

            uriStr = (User != null) ? uriStr + User + USER_HOST_SEPARATOR + Host : uriStr + Host;

            if (Parameters.Count > 0)
            {
                uriStr += Parameters.ToString();
            }

            // If the URI's protocol is not implied already set the transport parameter.
            //if (Scheme != SIPSchemesEnum.sips && Protocol != SIPProtocolsEnum.udp && !Parameters.Has(
            //    m_uriParamTransportKey))
            // 22 Jul 23 PHR -- Don't want a transport parameter added if the this URI is for msrp or
            // msrps
            if (Scheme != SIPSchemesEnum.sips && Protocol != SIPProtocolsEnum.udp && !Parameters!.Has(
                m_uriParamTransportKey) && Scheme != SIPSchemesEnum.msrp && Scheme != 
                SIPSchemesEnum.msrps && Scheme != SIPSchemesEnum.msrp)
                uriStr += PARAM_TAG_DELIMITER + m_uriParamTransportKey + TAG_NAME_VALUE_SEPERATOR + 
                    Protocol.ToString();

            if (Headers.Count > 0)
            {
                string headerStr = Headers.ToString();
                uriStr += HEADER_START_DELIMITER + headerStr.Substring(1);
            }

            return uriStr;
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// Returns a string representation of the URI with any parameter and headers omitted except for 
    /// the transport parameter. The string returned by this function is used amongst other things to 
    /// match Route headers set by this SIP agent.
    /// </summary>
    /// <returns>A string representation of the URI with headers and parameters omitted except for 
    /// the transport parameter if it is required.</returns>
    public string ToParameterlessString()
    {
        try
        {
            string uriStr = Scheme.ToString() + SCHEME_ADDR_SEPARATOR;

            uriStr = (User != null) ? uriStr + User + USER_HOST_SEPARATOR + Host : uriStr + Host;

            // If the URI's protocol is not implied already set the transport parameter.
            if (Scheme != SIPSchemesEnum.sips && Protocol != SIPProtocolsEnum.udp && !Parameters.Has(
                m_uriParamTransportKey))
                uriStr += PARAM_TAG_DELIMITER + m_uriParamTransportKey + TAG_NAME_VALUE_SEPERATOR + 
                    Protocol.ToString();
            
            return uriStr;
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// Returns an address of record for the SIP URI which is a string in the format user@host.
    /// </summary>
    /// <returns>A string representing the address of record for the URI.</returns>
    public string ToAOR()
    {
        return User + USER_HOST_SEPARATOR + Host;
    }

    /// <summary>
    /// Converts this SIPURI object into a SIPEndPoint object.
    /// </summary>
    /// <returns>Returns a SIPEndPoint object if the SIPURI represents an endpoint or
    /// null if it does not (for example: urn:service:sos)</returns>
    public SIPEndPoint? ToSIPEndPoint()
    {
        if (IPSocket.TryParseIPEndPoint(Host!, out var ipEndPoint))
        {
            if (ipEndPoint.Port != 0)
            {
                return new SIPEndPoint(Protocol, ipEndPoint);
            }
            else
            {
                ipEndPoint.Port = SIPConstants.GetDefaultPort(Protocol);
                return new SIPEndPoint(Protocol, ipEndPoint);
            }
        }
        else
        {
            return null;
        }
    }

    private void ParseParamsAndHeaders(string paramsAndHeaders)
    {
        if (paramsAndHeaders != null && paramsAndHeaders.Trim().Length > 0)
        {
            int headerDelimPosn = paramsAndHeaders.IndexOf(HEADER_START_DELIMITER);

            if (headerDelimPosn == -1)
            {
                Parameters = new SIPParameters(paramsAndHeaders, PARAM_TAG_DELIMITER);
            }
            else
            {
                Parameters = new SIPParameters(paramsAndHeaders.Substring(0, headerDelimPosn), 
                    PARAM_TAG_DELIMITER);
                Headers = new SIPParameters(paramsAndHeaders.Substring(headerDelimPosn + 1), 
                    HEADER_TAG_DELIMITER);
            }
        }
    }

    /// <summary>
    /// Returns true if two SIPURI objects represent the same resource
    /// </summary>
    /// <param name="uri1">First SIPURI object</param>
    /// <param name="uri2">Second SIPURI object</param>
    /// <returns>Returns true if the two SIPURI objects are equal</returns>
    public static bool AreEqual(SIPURI uri1, SIPURI uri2)
    {
        return uri1 == uri2;
    }

    /// <summary>
    /// Returns true if the input SIPURI represents the same resource as this one
    /// </summary>
    /// <param name="obj">SIPURI object to test</param>
    /// <returns>Returns true if the input SIPURI is equal to this one</returns>
    public override bool Equals(object? obj)
    {
        return AreEqual(this, (SIPURI)obj!);
    }

    /// <summary>
    /// Returns true if two SIPURI objects represent the same resource
    /// </summary>
    /// <param name="uri1">Right-hand SIPURI object</param>
    /// <param name="uri2">Left-hand SIPURI object</param>
    /// <returns>Returns true if they are equal or false if they are not</returns>
    public static bool operator ==(SIPURI uri1, SIPURI uri2)
    {
        if (uri1 is null && uri2 is null)
            return true;
        else if (uri1 is null || uri2 is null)
            return false;
        else if (uri1.Host == null || uri2.Host == null)
            return false;
        else if (uri1.CanonicalAddress != uri2.CanonicalAddress)
            return false;
        else if (uri1.Parameters != uri2.Parameters)
            return false;
        else if (uri1.Headers != uri2.Headers)
            return false;

        return true;
    }

    /// <summary>
    /// Override of the != operator
    /// </summary>
    /// <param name="x">Left-hand SIPURI object</param>
    /// <param name="y">Right-hand SIPURI object</param>
    /// <returns>Returns true if the SIPURI objects are not equal or false if they are equal</returns>
    public static bool operator !=(SIPURI x, SIPURI y)
    {
        return !(x == y);
    }

    /// <summary>
    /// Override of GetHashCode()
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode()
    {
        int RetVal = CanonicalAddress.GetHashCode();
        RetVal += Parameters.GetHashCode();
        RetVal += Headers.GetHashCode();
        return RetVal;
    }

    /// <summary>
    /// Creates a deep copy if this SIPURI object
    /// </summary>
    /// <returns></returns>
    public SIPURI CopyOf()
    {
        SIPURI copy = new SIPURI();
        copy.Scheme = Scheme;
        copy.Host = Host;
        copy.User = User;

        if (Parameters?.Count > 0)
        {
            copy.Parameters = Parameters.CopyOf();
        }

        if (Headers?.Count > 0)
        {
            copy.Headers = Headers.CopyOf();
        }

        return copy;
    }

    /// <summary>
    /// Checks whether the specified SIP URI Host field contains a private IPv4 address
    /// and if so and the received on IP address is different then "mangles" the host to
    /// contain the received on IP end point. The purpose of the mangling is to assist
    /// in dealing with IPv4 NAT's.
    /// If the SIP URI host is IPv6 or a host name no mangling will be done.
    /// </summary>
    /// <param name="uri">The SIP URI to mangle.</param>
    /// <param name="receivedOn">The IP end point that the SIP message was received from.</param>
    /// <returns>A new SIP URI if mangling took place. Null if no mangling occurred.</returns>
    public static SIPURI? Mangle(SIPURI uri, IPEndPoint receivedOn)
    {
        if (receivedOn != null && IPAddress.TryParse(uri.HostAddress, out var ipv4Host))
        {

            if (ipv4Host.IsPrivate() && !IPAddress.Equals(ipv4Host, receivedOn.Address))
            {
                var mangledURI = uri.CopyOf();
                mangledURI.Host = mangledURI.Host!.Replace(mangledURI.Host, receivedOn.ToString());
                return mangledURI;
            }
        }

        return null;
    }

    /// <summary>
    /// Indicates whether the SIP URI is using the default port for its protocol.
    /// Default ports are 5060 for UDP and TCP, 5061 for TLS, 80 for WS and 443 for WSS.
    /// </summary>
    /// <returns>True if the default port is being used, false if not.</returns>
    public bool IsDefaultPort()
    {
        if (HostPort == null)
            // If the URI does not contain an explicit port it means the default is implcit.
            return true;
        else if (int.TryParse(HostPort, out var port))
        {
            switch (Protocol)
            {
                case SIPProtocolsEnum.udp:
                case SIPProtocolsEnum.tcp:
                    return port == SIPConstants.DEFAULT_SIP_PORT;
                case SIPProtocolsEnum.tls:
                    return port == SIPConstants.DEFAULT_SIP_TLS_PORT;
                case SIPProtocolsEnum.ws:
                    return port == SIPConstants.DEFAULT_SIP_WEBSOCKET_PORT;
                case SIPProtocolsEnum.wss:
                    return port == SIPConstants.DEFAULT_SIPS_WEBSOCKET_PORT;
                default:
                    return false;
            }
        }
        else
        {   // Couldn't understand the port. Assume it's not a default.
            return false;
        }
    }
}
