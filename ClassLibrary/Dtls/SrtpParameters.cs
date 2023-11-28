//-----------------------------------------------------------------------------
// Filename: SrtpParameters.cs
//
// Description: Parameters for Secure RTP (SRTP) sessions.
//
// Derived From: 
// https://github.com/RestComm/media-core/blob/master/rtp/src/main/java/org/restcomm/media/core/rtp/crypto/SRTPParameters.java
//
// Author(s):
// Rafael Soares (raf.csoares@kyubinteractive.com)
//
// History:
// 01 Jul 2020	Rafael Soares   Created.
//
// License:
// Customisations: BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
// Original Source: AGPL-3.0 License
//-----------------------------------------------------------------------------

//  Revised: 25 Nov 23 PHR
//      -- Changed namespace to SipLib.Dtls from SIPSorcery.Net
//      -- Added documentation comments and code cleanup


using Org.BouncyCastle.Crypto.Tls;

namespace SipLib.Dtls;

/// <summary>
/// Structure that defines the DTLS derived key and salt lengths for SRTP. 
/// See http://tools.ietf.org/html/rfc5764#section-4.1.2
/// </summary>
public struct SrtpParameters
{

    /// <summary>
    /// AES-128 counter mode with 80 bits of authentication. Defined in RFC 3711.
    /// </summary>    
    public static readonly SrtpParameters SRTP_AES128_CM_HMAC_SHA1_80 = new SrtpParameters(SrtpProtectionProfile.SRTP_AES128_CM_HMAC_SHA1_80, SrtpPolicy.AESCM_ENCRYPTION, 16, SrtpPolicy.HMACSHA1_AUTHENTICATION, 20, 10, 10, 14);
    /// <summary>
    /// AES-128 counter mode with 32 bits of authentication information. Defined in RFC 3711.
    /// </summary>
    public static readonly SrtpParameters SRTP_AES128_CM_HMAC_SHA1_32 = new SrtpParameters(SrtpProtectionProfile.SRTP_AES128_CM_HMAC_SHA1_32, SrtpPolicy.AESCM_ENCRYPTION, 16, SrtpPolicy.HMACSHA1_AUTHENTICATION, 20, 4, 10, 14);
    /// <summary>
    /// No encryption with 80 bits of authentication information. Defined in RFC 3711.
    /// </summary>
    public static readonly SrtpParameters SRTP_NULL_HMAC_SHA1_80 = new SrtpParameters(SrtpProtectionProfile.SRTP_NULL_HMAC_SHA1_80, SrtpPolicy.NULL_ENCRYPTION, 0, SrtpPolicy.HMACSHA1_AUTHENTICATION, 20, 10, 10, 0);
    /// <summary>
    /// No encryption with 32 bits of authentication information. Defined in RFC 3711.
    /// </summary>
    public static readonly SrtpParameters SRTP_NULL_HMAC_SHA1_32 = new SrtpParameters(SrtpProtectionProfile.SRTP_NULL_HMAC_SHA1_32, SrtpPolicy.NULL_ENCRYPTION, 0, SrtpPolicy.HMACSHA1_AUTHENTICATION, 20, 4, 10, 0);


    private int profile;
    private int encType;
    private int encKeyLength;
    private int authType;
    private int authKeyLength;
    private int authTagLength;
    private int rtcpAuthTagLength;
    private int saltLength;

    private SrtpParameters(int newProfile, int newEncType, int newEncKeyLength, int newAuthType, int newAuthKeyLength, int newAuthTagLength, int newRtcpAuthTagLength, int newSaltLength)
    {
        this.profile = newProfile;
        this.encType = newEncType;
        this.encKeyLength = newEncKeyLength;
        this.authType = newAuthType;
        this.authKeyLength = newAuthKeyLength;
        this.authTagLength = newAuthTagLength;
        this.rtcpAuthTagLength = newRtcpAuthTagLength;
        this.saltLength = newSaltLength;
    }

    /// <summary>
    /// Returns the encryption profile.
    /// </summary>
    /// <returns>Will be either SrtpProtectionProfile.SRTP_AES128_CM_HMAC_SHA1_80,
    /// SrtpProtectionProfile.SRTP_AES128_CM_HMAC_SHA1_32, SrtpProtectionProfile.SRTP_NULL_HMAC_SHA1_80 or
    /// SrtpProtectionProfile.SRTP_NULL_HMAC_SHA1_32</returns>
    public int GetProfile()
    {
        return profile;
    }

    /// <summary>
    /// Gets the length of the cipher key in bytes.
    /// </summary>
    /// <returns></returns>
    public int GetCipherKeyLength()
    {
        return encKeyLength;
    }

    /// <summary>
    /// Gets the length of the cipher salt in bytes
    /// </summary>
    /// <returns></returns>
    public int GetCipherSaltLength()
    {
        return saltLength;
    }

    /// <summary>
    /// Gets the SRTP parameters from the SRTP profile
    /// </summary>
    /// <param name="profileValue">Specifies the SRTP protection profile. Must be one of the values in the
    /// SrtpProtectionProfile enumeration.</param>
    /// <returns>Returns a SrtpParameters structure.</returns>
    // <exception cref="Exception">Thrown if the profileValue is unknown.</exception>
    public static SrtpParameters GetSrtpParametersForProfile(int profileValue)
    {
        switch (profileValue)
        {
            case SrtpProtectionProfile.SRTP_AES128_CM_HMAC_SHA1_80:
                return SRTP_AES128_CM_HMAC_SHA1_80;
            case SrtpProtectionProfile.SRTP_AES128_CM_HMAC_SHA1_32:
                return SRTP_AES128_CM_HMAC_SHA1_32;
            case SrtpProtectionProfile.SRTP_NULL_HMAC_SHA1_80:
                return SRTP_NULL_HMAC_SHA1_80;
            case SrtpProtectionProfile.SRTP_NULL_HMAC_SHA1_32:
                return SRTP_NULL_HMAC_SHA1_32;
            default:
                throw new Exception($"SRTP Protection Profile value {profileValue} is not allowed for DTLS SRTP. See http://tools.ietf.org/html/rfc5764#section-4.1.2 for valid values.");
        }
    }

    /// <summary>
    /// Gets the SRTP policy for RTP
    /// </summary>
    /// <returns></returns>
    public SrtpPolicy GetSrtpPolicy()
    {
        SrtpPolicy sp = new SrtpPolicy(encType, encKeyLength, authType, authKeyLength, authTagLength, saltLength);
        return sp;
    }

    /// <summary>
    /// Gets the SRTP policy for RTCP
    /// </summary>
    /// <returns></returns>
    public SrtpPolicy GetSrtcpPolicy()
    {
        SrtpPolicy sp = new SrtpPolicy(encType, encKeyLength, authType, authKeyLength, rtcpAuthTagLength, saltLength);
        return sp;
    }

}