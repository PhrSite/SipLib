/////////////////////////////////////////////////////////////////////////////////////
//  File:   CryptoSuites.cs                                         26 Sep 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.RtpCrypto;

/// <summary>
/// Class that provides information about the crypto suites for SRTP that this class library supports.
/// </summary>
public class CryptoSuites
{
    /// <summary>
    /// See Section 6.2.1 of RFC 4568
    /// </summary>
    public const string AES_CM_128_HMAC_SHA1_80 = "AES_CM_128_HMAC_SHA1_80";
    /// <summary>
    /// See Section 6.2.2 of RFC 4568
    /// </summary>
    public const string AES_CM_128_HMAC_SHA1_32 = "AES_CM_128_HMAC_SHA1_32";
    /// <summary>
    /// See Section 6.2.3 of RFC 4568
    /// </summary>
    public const string F8_128_HMAC_SHA1_80 = "F8_128_HMAC_SHA1_80";

    /// <summary>
    /// See Table 1 of RFC 6188
    /// </summary>
    public const string AES_192_CM_HMAC_SHA1_80 = "AES_192_CM_HMAC_SHA1_80";
    /// <summary>
    /// See Table 2 of RFC 6188
    /// </summary>
    public const string AES_192_CM_HMAC_SHA1_32 = "AES_192_CM_HMAC_SHA1_32";
    /// <summary>
    /// See Table 3 of RFC 6188
    /// </summary>
    public const string AES_256_CM_HMAC_SHA1_80 = "AES_256_CM_HMAC_SHA1_80";
    /// <summary>
    /// See Table 4 of RFC 6188
    /// </summary>
    public const string AES_256_CM_HMAC_SHA1_32 = "AES_256_CM_HMAC_SHA1_32";

    /// <summary>
    /// Gets the list of supported SRTP cryptographic algorithms 
    /// </summary>
    public static List<string> SupportedAlgorithms = new List<string>()
    {
        AES_CM_128_HMAC_SHA1_80,
        AES_CM_128_HMAC_SHA1_32,
        F8_128_HMAC_SHA1_80,
        AES_192_CM_HMAC_SHA1_80,
        AES_192_CM_HMAC_SHA1_32,
        AES_256_CM_HMAC_SHA1_80,
        AES_256_CM_HMAC_SHA1_32,
    };

    /// <summary>
    /// Returns true if the crypto cryptoSuite is supported or false if it not
    /// </summary>
    /// <param name="cryptoSuite">Name of the crypto cryptoSuite</param>
    /// <returns>True if the cryptoSuite is supported or false if it is not.</returns>
    public static bool CryptoSuiteIsSupported(string cryptoSuite)
    {
        return SupportedAlgorithms.Contains(cryptoSuite);
    }

    /// <summary>
    /// Gets the key length in bytes for a cryto suite
    /// </summary>
    /// <param name="cryptoSuite">The SRTP crypto suite that the inline parameters relate to</param>
    /// <returns>Returns the expected key length</returns>
    public static int GetKeyLengthBytes(string cryptoSuite)
    {
        int KeyLength = 16;
        switch (cryptoSuite)
        {
            case AES_CM_128_HMAC_SHA1_80:
            case AES_CM_128_HMAC_SHA1_32:
            case F8_128_HMAC_SHA1_80:
                KeyLength = 16;
                break;
            case AES_192_CM_HMAC_SHA1_80:
            case AES_192_CM_HMAC_SHA1_32:
                KeyLength = 24;
                break;
            case AES_256_CM_HMAC_SHA1_80:
            case AES_256_CM_HMAC_SHA1_32:
                KeyLength = 32;
                break;
        }

        return KeyLength;
    }

    /// <summary>
    /// Gets the length in bytes of the authentication tag that is appended to each RTP and RTCP packet.
    /// </summary>
    /// <param name="cryptoSuite">Crypto suite.</param>
    /// <returns>Returns the length of the SRTP authentication in bytes. The return value either be
    /// 10 or 4.</returns>
    public static int GetAuthTagLengthBytes(string cryptoSuite)
    {
        int AuthTagLength = 10;
        switch (cryptoSuite)
        {
            case AES_CM_128_HMAC_SHA1_80:
            case F8_128_HMAC_SHA1_80:
            case AES_192_CM_HMAC_SHA1_80:
            case AES_256_CM_HMAC_SHA1_80:
                AuthTagLength = 10;
                break;
            case AES_CM_128_HMAC_SHA1_32:
            case AES_192_CM_HMAC_SHA1_32:
            case AES_256_CM_HMAC_SHA1_32:
                AuthTagLength = 4;
                break;
        }

        return AuthTagLength;
    }

}
