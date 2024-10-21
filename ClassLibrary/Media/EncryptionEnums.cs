/////////////////////////////////////////////////////////////////////////////////////
//  File:   EncryptionEnums.cs                                      14 Oct 24 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Media;

/// <summary>
/// Enumeration for the RTP media encryption methods
/// </summary>
public enum RtpEncryptionEnum : int
{
    /// <summary>
    /// No encryption
    /// </summary>
    None,
    /// <summary>
    /// SDES-SRTP encryption as specified in RFC 4568 and RFC 3711
    /// </summary>
    SdesSrtp,
    /// <summary>
    /// DTLS-SRTP encryption as specified in RFC 5763, RFC 5764 and RFC 3711
    /// </summary>
    DtlsSrtp
}

/// <summary>
/// Enumeration of the encryption methods for MSRP media
/// </summary>
public enum MsrpEncryptionEnum : int
{
    /// <summary>
    /// No encryption -- use TCP
    /// </summary>
    None,
    /// <summary>
    /// Use MSRP over TLS (MSRPS)
    /// </summary>
    Msrps
}
