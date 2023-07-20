#region License
//-----------------------------------------------------------------------------
// Filename: SIPConstants.cs
//
// Description: SIP constants.
// 
// History:
// 17 Sep 2005	Aaron Clauson	Created.
//
// License: 
// This software is licensed under the BSD License http://www.opensource.org/licenses/bsd-license.php
//
// Copyright (c) 2006 Aaron Clauson (aaron@sipsorcery.com), SIP Sorcery PTY LTD, Hobart, Australia (www.sipsorcery.com)
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without modification, are permitted provided that 
// the following conditions are met:
//
// Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer. 
// Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following 
// disclaimer in the documentation and/or other materials provided with the distribution. Neither the name of SIP Sorcery PTY LTD. 
// nor the names of its contributors may be used to endorse or promote products derived from this software without specific 
// prior written permission. 
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, 
// BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. 
// IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, 
// OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, 
// OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, 
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE 
// POSSIBILITY OF SUCH DAMAGE.
//-----------------------------------------------------------------------------
#endregion

/////////////////////////////////////////////////////////////////////////////////////
//	Revised:	7 Nov 22 PHR -- Initial version.
//              12 Nov 22 PHR -- Revised GetProtocol so that it can handle unknown
//                protocol types.
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Core;

/// <summary>
/// Defines various constants used for SIP
/// </summary>
public class SIPConstants
{
    /// <summary>
    /// SIP message line terminator
    /// </summary>
    public const string CRLF = "\r\n";

    /// <summary>
    /// SIP protocol identifier
    /// </summary>
    public const string SIP_VERSION_STRING = "SIP";

    /// <summary>
    /// SIP major version
    /// </summary>
    public const int SIP_MAJOR_VERSION = 2;

    /// <summary>
    /// SIP minor version
    /// </summary>
    public const int SIP_MINOR_VERSION = 0;

    /// <summary>
    /// SIP full version string
    /// </summary>
    public const string SIP_FULLVERSION_STRING = "SIP/2.0";

    /// <summary>
    /// Any SIP messages over this size will generate an error.
    /// </summary>
    public const int SIP_MAXIMUM_RECEIVE_LENGTH = 65535;

    /// <summary>
    /// Magic cookie for the branch parameter in a Via header
    /// </summary>
    public const string SIP_BRANCH_MAGICCOOKIE = "z9hG4bK";

    /// <summary>
    /// Default URI for the From header
    /// </summary>
    public const string SIP_DEFAULT_FROMURI = "sip:thisis@anonymous.invalid";

    /// <summary>
    /// Remove all registrations
    /// </summary>
    public const string SIP_REGISTER_REMOVEALL = "*";

    /// <summary>
    /// SIP loose routing parameter
    /// </summary>
    public const string SIP_LOOSEROUTER_PARAMETER = "lr";

    /// <summary>
    /// SIP header delimiter character
    /// </summary>
    public const char HEADER_DELIMITER_CHAR = ':';

    /// <summary>
    /// Default value for the Max-Forwards SIP header
    /// </summary>
    public const int DEFAULT_MAX_FORWARDS = 70;

    /// <summary>
    /// Default SIP port number for UDP and TCP
    /// </summary>
    public const int DEFAULT_SIP_PORT = 5060;

    /// <summary>
    /// Default SIP port number for TLS
    /// </summary>
    public const int DEFAULT_SIP_TLS_PORT = 5061;

    /// <summary>
    /// Default SIP port number for the Web Sockets transport
    /// </summary>
    public const ushort DEFAULT_SIP_WEBSOCKET_PORT = 80;

    /// <summary>
    /// Default SIP port number for the secure Web Sockets transport
    /// </summary>
    public const ushort DEFAULT_SIPS_WEBSOCKET_PORT = 443;

    /// <summary>
    /// Gets the default SIP port for the protocol. 
    /// </summary>
    /// <param name="protocol">The transport layer protocol to get the port for.</param>
    /// <returns>The default port to use.</returns>
    public static int GetDefaultPort(SIPProtocolsEnum protocol)
    {
        switch (protocol)
        {
            case SIPProtocolsEnum.udp:
                return SIPConstants.DEFAULT_SIP_PORT;
            case SIPProtocolsEnum.tcp:
                return SIPConstants.DEFAULT_SIP_PORT;
            case SIPProtocolsEnum.tls:
                return SIPConstants.DEFAULT_SIP_TLS_PORT;
            case SIPProtocolsEnum.ws:
                return SIPConstants.DEFAULT_SIP_WEBSOCKET_PORT;
            case SIPProtocolsEnum.wss:
                return SIPConstants.DEFAULT_SIPS_WEBSOCKET_PORT;
            default:
                throw new ApplicationException($"Protocol {protocol} was not recognised in GetDefaultPort.");
        }
    }
}

/// <summary>
/// Message types for SIP
/// </summary>
public enum SIPMessageTypesEnum
{
    /// <summary>
    /// Unknown SIP message type
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// SIP Request message
    /// </summary>
    Request = 1,

    /// <summary>
    /// SIP Response message
    /// </summary>
    Response = 2,
}

/// <summary>
/// Enumeration of the different URI schemes that the SIPURI and other classes must handle.
/// </summary>
public enum SIPSchemesEnum
{
    /// <summary>
    /// Basic SIP URI
    /// </summary>
    sip = 1,

    /// <summary>
    /// SIP over TLS
    /// </summary>
    sips = 2,

    /// <summary>
    /// Tel URI
    /// </summary>
    tel = 3,    // 7 Nov 22 PHR

    /// <summary>
    /// URN type URI. For example: urn:service:sos for NG9-1-1 calls
    /// </summary>
    urn = 4,    // 7 Nov 22 PHR

    /// <summary>
    /// HTTP URI
    /// </summary>
    http = 5,   // 7 Nov 22 PHR

    /// <summary>
    /// HTTP over TLS URI
    /// </summary>
    https = 6,  // 7 Nov 22 PHR

    /// <summary>
    /// Content ID URI
    /// </summary>
    cid = 7,    // 7 Nov 22 PHR
}

/// <summary>
/// Helper functions for dealing with SIP schemes
/// </summary>
public class SIPSchemesType
{
    /// <summary>
    /// Maps a string version of the SIP scheme to the enum equivalent. Only call this function if 
    /// IsAllowedScheme() returns true.
    /// </summary>
    /// <param name="schemeType">Input string</param>
    /// <returns>Returns the enum equivalent</returns>
    public static SIPSchemesEnum GetSchemeType(string schemeType)
    {
        return (SIPSchemesEnum)Enum.Parse(typeof(SIPSchemesEnum), schemeType, true);
    }

    /// <summary>
    /// Determines if a URI scheme is supported
    /// </summary>
    /// <param name="schemeType">String containing the URI scheme</param>
    /// <returns>Return true if the scheme is supported or false if it is not.</returns>
    public static bool IsAllowedScheme(string schemeType)
    {
        try
        {
            Enum.Parse(typeof(SIPSchemesEnum), schemeType, true);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Enumeration that defines the transport protocols used for SIP
/// </summary>
public enum SIPProtocolsEnum
{
    /// <summary>
    /// User Datagram Protocol
    /// </summary>
    udp = 1,

    /// <summary>
    /// Transport Control Protocol
    /// </summary>
    tcp = 2,

    /// <summary>
    /// Transport Layer Security
    /// </summary>
    tls = 3,

    /// <summary>
    /// Web Socket.
    /// </summary>
    ws = 4,

    /// <summary>
    /// Web Socket over TLS.
    /// </summary>
    wss = 5,
}

/// <summary>
/// Helper functions for dealing with tranport protocols
/// </summary>
public class SIPProtocolsType
{
    /// <summary>
    /// Returns a protocol enum value given a string containing the transport protocol type. Only call
    /// this function if IsAllowedProtocol() returns true.
    /// </summary>
    /// <param name="protocolType">Input string value</param>
    /// <returns>Returns the enum equivalent</returns>
    public static SIPProtocolsEnum GetProtocolType(string protocolType)
    {
        if (protocolType == null)
            // Not expected. Return something reasonable
            return SIPProtocolsEnum.tcp;
        else
        {   // 12 Nov 22 PHR -- Don't want an exception for unknown enum values.
            SIPProtocolsEnum RetVal = SIPProtocolsEnum.udp;
            switch (protocolType.ToLower())
            {
                case "udp":
                    RetVal = SIPProtocolsEnum.udp;
                    break;
                case "tcp":
                    RetVal = SIPProtocolsEnum.tcp;
                    break;
                case "tls":
                    RetVal = SIPProtocolsEnum.tls;
                    break;
                case "ws":
                    RetVal = SIPProtocolsEnum.ws;
                    break;
                case "wss":
                    RetVal= SIPProtocolsEnum.wss;
                    break;
                default:
                    RetVal = SIPProtocolsEnum.udp;
                    break;
            }

            return RetVal;
        }
    }

    /// <summary>
    /// Determines if a transport protocol is allowed.
    /// </summary>
    /// <param name="protocol">Input string value</param>
    /// <returns>Return true if the transport protocol is allowed, else return false</returns>
    public static bool IsAllowedProtocol(string protocol)
    {
        if (protocol == null)
            return false;

        try
        {
            Enum.Parse(typeof(SIPProtocolsEnum), protocol, true);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

#pragma warning disable CS1591

/// <summary>
/// Definitions for SIP Header fields.
/// </summary>
public class SIPHeaders
{
    // SIP Header Keys.
    public const string SIP_HEADER_ACCEPT = "Accept";
    public const string SIP_HEADER_ACCEPTENCODING = "Accept-Encoding";
    public const string SIP_HEADER_ACCEPTLANGUAGE = "Accept-Language";
    public const string SIP_HEADER_ALERTINFO = "Alert-Info";
    public const string SIP_HEADER_ALLOW = "Allow";

    /// <summary>
    /// RC3265 (SIP Events).
    /// </summary>
    public const string SIP_HEADER_ALLOW_EVENTS = "Allow-Events";
    public const string SIP_HEADER_AUTHENTICATIONINFO = "Authentication-Info";
    public const string SIP_HEADER_AUTHORIZATION = "Authorization";
    public const string SIP_HEADER_CALLID = "Call-ID";
    public const string SIP_HEADER_CALLINFO = "Call-Info";
    public const string SIP_HEADER_CONTACT = "Contact";
    public const string SIP_HEADER_CONTENT_DISPOSITION = "Content-Disposition";
    public const string SIP_HEADER_CONTENT_ENCODING = "Content-Encoding";
    public const string SIP_HEADER_CONTENT_LANGUAGE = "Content-Language";
    public const string SIP_HEADER_CONTENTLENGTH = "Content-Length";
    public const string SIP_HEADER_CONTENTTYPE = "Content-Type";
    public const string SIP_HEADER_CSEQ = "CSeq";
    public const string SIP_HEADER_DATE = "Date";
    public const string SIP_HEADER_ERROR_INFO = "Error-Info";

    /// <summary>
    /// RC3265 (SIP Events).
    /// </summary>
    public const string SIP_HEADER_EVENT = "Event";

    /// <summary>
    /// RFC3903
    /// </summary>
    public const string SIP_HEADER_ETAG = "SIP-ETag";
    public const string SIP_HEADER_EXPIRES = "Expires";
    public const string SIP_HEADER_FROM = "From";

    public const string SIP_HEADER_PAI = "P-Asserted-Identity";  // 7 Nov 22 PHR
    public const string SIP_HEADER_PPI = "P-Preferred-Identity"; // 7 Nov 22 PHR

    // 7 Nov 22 PHR -- From RFC 6442
    public const string SIP_HEADER_GEOLOCATION = "Geolocation";
    public const string SIP_HEADER_GEOLOCATION_ROUTING = "Geolocation-Routing";
    public const string SIP_HEADER_GEOLOCATION_ERROR = "Geolocation-Error";

    public const string SIP_HEADER_IN_REPLY_TO = "In-Reply-To";
    public const string SIP_HEADER_MAXFORWARDS = "Max-Forwards";
    public const string SIP_HEADER_MINEXPIRES = "Min-Expires";
    public const string SIP_HEADER_MIME_VERSION = "MIME-Version";
    public const string SIP_HEADER_ORGANIZATION = "Organization";
    public const string SIP_HEADER_PRIORITY = "Priority";
    public const string SIP_HEADER_PROXYAUTHENTICATION = "Proxy-Authenticate";
    public const string SIP_HEADER_PROXYAUTHORIZATION = "Proxy-Authorization";
    public const string SIP_HEADER_PROXY_REQUIRE = "Proxy-Require";
    public const string SIP_HEADER_REASON = "Reason";
    public const string SIP_HEADER_RECORDROUTE = "Record-Route";

    /// <summary>
    /// RFC 3515 "The Session Initiation Protocol (SIP) Refer Method".
    /// </summary>
    public const string SIP_HEADER_REFERREDBY = "Referred-By";

    /// <summary>
    /// RFC 4488 Used to stop the implicit SIP event subscription on a REFER request.
    /// </summary>
    public const string SIP_HEADER_REFERSUB = "Refer-Sub";

    /// <summary>
    /// RFC 3515 "The Session Initiation Protocol (SIP) Refer Method".
    /// </summary>
    public const string SIP_HEADER_REFERTO = "Refer-To";
    public const string SIP_HEADER_REPLY_TO = "Reply-To";
    public const string SIP_HEADER_REQUIRE = "Require";

    // 7 Nov 22 PHR
    public const string SIP_HEADER_RESOURCE_PRIORITY = "Resource-Priority";

    public const string SIP_HEADER_RETRY_AFTER = "Retry-After";
    public const string SIP_HEADER_ROUTE = "Route";
    public const string SIP_HEADER_SERVER = "Server";
    public const string SIP_HEADER_SUBJECT = "Subject";

    /// <summary>
    /// RC3265 (SIP Events).
    /// </summary>
    public const string SIP_HEADER_SUBSCRIPTION_STATE = "Subscription-State";
    public const string SIP_HEADER_SUPPORTED = "Supported";
    public const string SIP_HEADER_TIMESTAMP = "Timestamp";
    public const string SIP_HEADER_TO = "To";
    public const string SIP_HEADER_UNSUPPORTED = "Unsupported";
    public const string SIP_HEADER_USERAGENT = "User-Agent";
    public const string SIP_HEADER_VIA = "Via";
    public const string SIP_HEADER_WARNING = "Warning";
    public const string SIP_HEADER_WWWAUTHENTICATE = "WWW-Authenticate";

    // SIP Compact Header Keys.

    /// <summary>
    /// RC3265 (SIP Events).
    /// </summary>
    public const string SIP_COMPACTHEADER_ALLOWEVENTS = "u";
    public const string SIP_COMPACTHEADER_CALLID = "i";
    public const string SIP_COMPACTHEADER_CONTACT = "m";
    public const string SIP_COMPACTHEADER_CONTENTLENGTH = "l";
    public const string SIP_COMPACTHEADER_CONTENTTYPE = "c";
    
    /// <summary>
    /// RC3265 (SIP Events).
    /// </summary>
    public const string SIP_COMPACTHEADER_EVENT = "o";
    public const string SIP_COMPACTHEADER_FROM = "f";

    /// <summary>
    /// RFC 3515 "The Session Initiation Protocol (SIP) Refer Method".
    /// </summary>
    public const string SIP_COMPACTHEADER_REFERTO = "r";
    public const string SIP_COMPACTHEADER_SUBJECT = "s";
    public const string SIP_COMPACTHEADER_SUPPORTED = "k";
    public const string SIP_COMPACTHEADER_TO = "t";
    public const string SIP_COMPACTHEADER_VIA = "v";
}

/// <summary>
/// Definitions for various SIP header parameter names
/// </summary>
public class SIPHeaderAncillary
{
    // Header parameters used in the core SIP protocol.
    public const string SIP_HEADERANC_TAG = "tag";
    public const string SIP_HEADERANC_BRANCH = "branch";
    public const string SIP_HEADERANC_RECEIVED = "received";
    public const string SIP_HEADERANC_TRANSPORT = "transport";
    public const string SIP_HEADERANC_MADDR = "maddr";

    // Via header parameter, documented in RFC 3581 "An Extension to the Session Initiation Protocol (SIP)
    // for Symmetric Response Routing".
    public const string SIP_HEADERANC_RPORT = "rport";

    // SIP header parameter from RFC 3515 "The Session Initiation Protocol (SIP) Refer Method".
    public const string SIP_REFER_REPLACES = "Replaces";
}

/// <summary>
/// Authorization Headers
/// </summary>
public class AuthHeaders
{
    public const string AUTH_DIGEST_KEY = "Digest";
    public const string AUTH_REALM_KEY = "realm";
    public const string AUTH_NONCE_KEY = "nonce";
    public const string AUTH_USERNAME_KEY = "username";
    public const string AUTH_RESPONSE_KEY = "response";
    public const string AUTH_URI_KEY = "uri";
    public const string AUTH_ALGORITHM_KEY = "algorithm";
    public const string AUTH_CNONCE_KEY = "cnonce";
    public const string AUTH_NONCECOUNT_KEY = "nc";
    public const string AUTH_QOP_KEY = "qop";
    public const string AUTH_OPAQUE_KEY = "opaque";
}

/// <summary>
/// 
/// </summary>
public enum SIPMethodsEnum
{
    NONE = 0,
    UNKNOWN = 1,
        
    // Core.
    REGISTER = 2,
    INVITE = 3,
    BYE = 4,
    ACK = 5,
    CANCEL = 6,
    OPTIONS = 7,

    INFO = 8,           // RFC2976.
    NOTIFY = 9,         // RFC3265.
    SUBSCRIBE = 10,     // RFC3265.
    PUBLISH = 11,       // RFC3903.
    PING = 13,
    REFER = 14,         // RFC3515
    MESSAGE = 15,       // RFC3428.
    PRACK = 16,         // RFC3262.
    UPDATE = 17,        // RFC3311.
}

/// <summary>
/// Class for mapping SIP method string to SIPMethodsEnum values
/// </summary>
public class SIPMethods
{
    /// <summary>
    /// Converts a SIP method string to a SIPMethodsEnum value
    /// </summary>
    /// <param name="method"></param>
    /// <returns></returns>
    public static SIPMethodsEnum GetMethod(string method)
    {
        SIPMethodsEnum sipMethod = SIPMethodsEnum.UNKNOWN;
        try
        {
            sipMethod = (SIPMethodsEnum)Enum.Parse(typeof(SIPMethodsEnum), method, true);
        }
        catch {}

        return sipMethod;
    }
}
        
/// <summary>
/// Enumeration of SIP response status codes
/// </summary>
public enum SIPResponseStatusCodesEnum
{
    None = 0,
   
    // Informational
    Trying = 100,
    Ringing = 180,
    CallIsBeingForwarded = 181,
    Queued = 182,
    SessionProgress = 183,
        
    // Success
    Ok = 200,
    Accepted = 202, // RC3265 (SIP Events).
    NoNotification = 204,

    // Redirection
    MultipleChoices = 300,
    MovedPermanently = 301,
    MovedTemporarily = 302,
    UseProxy = 303,
    AlternativeService = 304,

    // Client-Error
    BadRequest = 400,
    Unauthorised = 401,
    PaymentRequired = 402,
    Forbidden = 403,
    NotFound = 404,
    MethodNotAllowed = 405,
    NotAcceptable = 406,
    ProxyAuthenticationRequired = 407,
    RequestTimeout = 408,
    Gone = 410,
    ConditionalRequestFailed = 412,
    RequestEntityTooLarge = 413,
    RequestURITooLong = 414,
    UnsupportedMediaType = 415,
    UnsupportedURIScheme = 416,
    UnknownResourcePriority = 417,
    BadExtension = 420,
    ExtensionRequired = 421,
    SessionIntervalTooSmall = 422,
    IntervalTooBrief = 423,
    UseIdentityHeader = 428,
    ProvideReferrerIdentity = 429,
    FlowFailed = 430,
    AnonymityDisallowed = 433,
    BadIdentityInfo = 436,
    UnsupportedCertificate = 437,
    InvalidIdentityHeader = 438,
    FirstHopLacksOutboundSupport = 439,
    MaxBreadthExceeded = 440,
    ConsentNeeded = 470,
    TemporarilyUnavailable = 480,
    CallLegTransactionDoesNotExist = 481,
    LoopDetected = 482,
    TooManyHops = 483,
    AddressIncomplete = 484,
    Ambiguous = 485,
    BusyHere = 486,
    RequestTerminated = 487,
    NotAcceptableHere = 488,
    BadEvent = 489,         // RC3265 (SIP Events).
    RequestPending = 491,
    Undecipherable = 493,
    SecurityAgreementRequired = 580,

    // Server Failure.
    InternalServerError = 500,
    NotImplemented = 501,
    BadGateway = 502,
    ServiceUnavailable = 503,
    ServerTimeout = 504,
    SIPVersionNotSupported = 505,
    MessageTooLarge = 513,
    PreconditionFailure = 580,

    // Global Failures.
    BusyEverywhere = 600,
    Decline = 603,
    DoesNotExistAnywhere = 604,
    NotAcceptableAnywhere = 606,
}

/// <summary>
/// SIP response status codes functions
/// </summary>
public class SIPResponseStatusCodes
{
    /// <summary>
    /// Converts an integer into a SIPResponseStatusCodesEnum
    /// </summary>
    /// <param name="statusCode"></param>
    /// <returns></returns>
    public static SIPResponseStatusCodesEnum GetStatusTypeForCode(int statusCode)
    {
        return (SIPResponseStatusCodesEnum)Enum.Parse(typeof(SIPResponseStatusCodesEnum), statusCode.
            ToString(), true);
    }
}

// For SIP URI user portion the reserved characters below need to be escaped.
// reserved    =  ";" / "/" / "?" / ":" / "@" / "&" / "=" / "+"  / "$" / ","
// user-unreserved  =  "&" / "=" / "+" / "$" / "," / ";" / "?" / "/"
// Leaving to be escaped = ":" / "@" 
// 
// For SIP URI parameters different characters are unreserved (just to make life 
// difficult).
// reserved    =  ";" / "/" / "?" / ":" / "@" / "&" / "=" / "+"  / "$" / ","
// param-unreserved = "[" / "]" / "/" / ":" / "&" / "+" / "$"
// Leaving to be escaped =  ";" / "?" / "@" / "=" / ","

/// <summary>
/// Static class that contains various methods for escaping and unescaping 
/// reserved characters in various parts of SIPURIs, SIP headers, SIP parameters,
/// etc.
/// </summary>
public static class SIPEscape
{
    /// <summary>
    /// Escapes reserved characters in a SIP user name field
    /// </summary>
    /// <param name="unescapedString"></param>
    /// <returns></returns>
    public static string SIPURIUserEscape(string unescapedString)
    {
        string result = unescapedString;
        if (string.IsNullOrEmpty(result) == false)
        {
            result = result.Replace(":", "%3A");
            result = result.Replace("@", "%40");
            result = result.Replace(" ", "%20");
        }
        return result;
    }

    /// <summary>
    /// Unescapes reserved characters in a SIP user name field
    /// </summary>
    /// <param name="escapedString"></param>
    /// <returns></returns>
    public static string SIPURIUserUnescape(string escapedString)
    {
        string result;
        if (string.IsNullOrEmpty(escapedString) == false)
        {
            result = escapedString.Replace("%3A", ":");
            result = result.Replace("%3a", ":");
            result = result.Replace("%20", " ");
            return result;
        }
        else
            return null;
    }

    /// <summary>
    /// Escapes reserved characters in SIP URI parameter fields
    /// </summary>
    /// <param name="unescapedString"></param>
    /// <returns></returns>
    public static string SIPURIParameterEscape(string unescapedString)
    {
        string result = unescapedString;
        if (string.IsNullOrEmpty(result) == false)
        {
            result = result.Replace(";", "%3B");
            result = result.Replace("?", "%3F");
            result = result.Replace("@", "%40");
            result = result.Replace("=", "%3D");
            result = result.Replace(",", "%2C");
            result = result.Replace(" ", "%20");
        }
        return result;
    }

    /// <summary>
    /// Unescapes reserved characters in SIP URI parameters
    /// </summary>
    /// <param name="escapedString"></param>
    /// <returns></returns>
    public static string SIPURIParameterUnescape(string escapedString)
    {
        string result = escapedString;
        if (string.IsNullOrEmpty(result) == false)
        {
            result = result.Replace("%3B", ";");
            result = result.Replace("%3b", ";");
            result = result.Replace("%3F", "?");
            result = result.Replace("%3f", "?");
            result = result.Replace("%40", "@");
            result = result.Replace("%3D", "=");
            result = result.Replace("%3d", "=");
            result = result.Replace("%2C", ",");
            result = result.Replace("%2c", ",");
            result = result.Replace("%20", " ");
        }

        return result;
    }
}
#pragma warning restore CS1591

