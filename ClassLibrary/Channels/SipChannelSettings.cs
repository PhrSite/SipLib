/////////////////////////////////////////////////////////////////////////////////////
//  File:   SipChannelSettings.cs                                   21 Oct 25 PHR
/////////////////////////////////////////////////////////////////////////////////////

using System.Net;

namespace SipLib.Channels;

/// <summary>
/// Class containing configuration settings for building SIPChannel derived classes.
/// </summary>
public class SipChannelSettings
{
    /// <summary>
    /// Specifies the user portion of the SIPURI for the Contact header for the SIPChannel. Optional.
    /// </summary>
    public string? LocalUser { get; set; }

    /// <summary>
    /// Specifies the local IPv4 address that the SIPChannel will listen on. Optional if LocalIPv6Address is provided.
    /// </summary>
    public IPAddress? LocalIPv4Address { get; set; }

    /// <summary>
    /// Specifies the local IPv6 address that the SIPChannel will listen on. Optional if LocalIPv4Address is provided.
    /// </summary>
    public IPAddress? LocalIPv6Address { get; set; }

    /// <summary>
    /// Local port number that the SIPChannel will listen on. The default value is 5060.
    /// </summary>
    public int LocalSipPort { get; set; } = 5060;

    /// <summary>
    /// Local port number that the SIPChannel will listen on for SIP over TLS (SIPS). The default value is 5061.
    /// </summary>
    public int LocalSipsPort { get; set; } = 5061;

    /// <summary>
    /// If true, then then generated the SIPTLSChannel will provide a client X.509 certificate when connecting as a
    /// client and will require that clients making a connection request to provide a client X.509 certificate when acting
    /// as a server.
    /// </summary>
    public bool UseMutualAuthentication { get; set; } = false;

    /// <summary>
    /// Delegate function that can be provided by the user of a SIPConnection derived class to determine
    /// whether or not to accept a SIP connection request. The function should return true to accept a connection
    /// request or false to refuse it.
    /// </summary>
    public AcceptConnectionDelegate? AcceptConnection { get; set; } = null;

    /// <summary>
    /// Delegate function that is called to allow the user of the SIPTLSChannel class to decide whether or
    /// not to accept the connection based on the X.509 client certificate provided by the client. The function
    /// should return true to allow the connection or false to reject the connection. Used only for the SIPTLSChannel class.
    /// </summary>    
    public AcceptCertificateDelegate? AcceptClientCertificate { get; set; } = null;

    /// <summary>
    /// Delegate function that is called to allow the user of the SIPTLSChannel class to decide whether or not
    /// to accept the connection based on the X.509 server certificate provided by the remote server when
    /// connecting as a client. The function should return true to allow the connection or false to cancel the
    /// connection. Used only for the SIPTLSChannel class.
    /// </summary>
    public AcceptCertificateDelegate? AcceptServerCertificate { get; set; } = null;

}
