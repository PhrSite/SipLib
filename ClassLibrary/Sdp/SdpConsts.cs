/////////////////////////////////////////////////////////////////////////////////////
//  File:   SdpConsts.cs                                            16 Nov 22 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Sdp;

/// <summary>
/// Enumeration that specifies the type of connection setup to use as described in RFC 4145. This depends
/// on the a=setup:xxx SDP attribute.
/// </summary>
public enum SetupType
{
    /// <summary>
    /// The endpoint will initiate an outgoing connection.
    /// </summary>
    active,
    /// <summary>
    /// The endpoint will accept an incoming connection.
    /// </summary>
    passive,
    /// <summary>
    /// The endpoint is willing to accept an incoming connection or to initiate 
    /// an outgoing connection.
    /// </summary>
    actpass,
    /// <summary>
    /// The endpoint does not want the connection to be established for the time 
    /// being. This option is not currently supported.
    /// </summary>
    holdcon,
    /// <summary>
    /// The endpoint did not specify a setup type or the provided attribute value is unknow or not supported
    /// </summary>
    unknown,
}

/// <summary>
/// Enumeration of values for the Media Direction SDP attribute. See Section 6.7 of RFC 8866.
/// </summary>
public enum MediaDirectionEnum
{
    /// <summary>
    /// Receive only
    /// </summary>
    recvonly,
    /// <summary>
    /// Send and receive
    /// </summary>
    sendrecv,
    /// <summary>
    /// Send only
    /// </summary>
    sendonly,
    /// <summary>
    /// Media is inactive
    /// </summary>
    inactive,
}
    
