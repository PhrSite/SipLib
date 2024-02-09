/////////////////////////////////////////////////////////////////////////////////////
//  File:   SessionKeys.cs                                          25 Sep 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.RtpCrypto;

/// <summary>
/// Class for storing the current session key, salt and authentication keys for SRTP
/// </summary>
public class SessionKeys
{
    /// <summary>
    /// Contains the bytes of the session key. The length depends on the crypto suite in use
    /// </summary>
    /// <value></value>
    public byte[]? SessionKey = null;
    /// <summary>
    /// Contains the bytes of the session salt. The length will always be 14 bytes for the AES-CM and the
    /// AES-F8 algorithms.
    /// </summary>
    /// <value></value>
    public byte[]? SessionSalt = null;
    /// <summary>
    /// Contains the bytes of the session authentication key. The length will always be 20 bytes (160 bits)
    /// for the HMAC-SHA1 authentication algorithm.
    /// </summary>
    /// <value></value>
    public byte[]? SessionAuthKey = null;
}
