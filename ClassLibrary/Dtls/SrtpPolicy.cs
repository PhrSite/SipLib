//-----------------------------------------------------------------------------
// Filename: SrtpPolicy.cs
//
// Description: SRTP Policy encapsulation.
//
// Derived From: https://github.com/jitsi/jitsi-srtp/blob/master/src/main/java/org/jitsi/srtp/SrtpPolicy.java
//
// Author(s):
// Rafael Soares (raf.csoares@kyubinteractive.com)
//
// History:
// 01 Jul 2020	Rafael Soares   Created.
//
// License:
// Customisations: BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
// Original Source: Apache License
//-------------------------------------

//  Revised: 26 Nov 23 PHR
//      -- Changed namespace to SipLib.Dtls from SIPSorcery.Net
//      -- Added documentation comments and code cleanup

namespace SipLib.Dtls;

/// <summary>
/// SrtpPolicy holds the SRTP encryption / authentication policy of a SRTP session.
///
/// Author: Bing SU (nova.su@gmail.com)
/// </summary>
public class SrtpPolicy
{
    /// <summary>
    /// No encryption. See RFC 3711.
    /// </summary>
    /// <value></value>
    public const int NULL_ENCRYPTION = 0;
    /// <summary>
    /// AES counter mode encryption. See RFC 3711.
    /// </summary>
    /// <value></value>
    public const int AESCM_ENCRYPTION = 1;
    /// <summary>
    /// Not used in DTLS-SRTP
    /// </summary>
    /// <value></value>
    public const int TWOFISH_ENCRYPTION = 3;
    /// <summary>
    /// AES F8 encryption. See RFC 3711.
    /// </summary>
    /// <value></value>
    public const int AESF8_ENCRYPTION = 2;
    /// <summary>
    /// Not used in DTLS-SRTP
    /// </summary>
    /// <value></value>
    public const int TWOFISHF8_ENCRYPTION = 4;
    /// <summary>
    /// Not used in DTLS-SRTP
    /// </summary>
    /// <value></value>
    public const int NULL_AUTHENTICATION = 0;
    /// <summary>
    /// HMAC SHA1 authentication. See RFC 3711.
    /// </summary>
    /// <value></value>
    public const int HMACSHA1_AUTHENTICATION = 1;
    /// <summary>
    /// Not used in DTLS-SRTP
    /// </summary>
    /// <value></value>
    public const int SKEIN_AUTHENTICATION = 2;

    private int encType;
    private int encKeyLength;
    private int authType;
    private int authKeyLength;
    private int authTagLength;
    private int saltKeyLength;

    /// <summary>
    /// Gets or sets the authentication key length
    /// </summary>
    /// <value></value>
    public int AuthKeyLength { get => authKeyLength; set => authKeyLength = value; }
    /// <summary>
    /// Gets or sets the authentication tag length
    /// </summary>
    /// <value></value>
    public int AuthTagLength { get => authTagLength; set => authTagLength = value; }
    /// <summary>
    /// Gets or sets the authentication type
    /// </summary>
    /// <value></value>
    public int AuthType { get => authType; set => authType = value; }
    /// <summary>
    /// Gets or sets the encryption key length
    /// </summary>
    /// <value></value>
    public int EncKeyLength { get => encKeyLength; set => encKeyLength = value; }
    /// <summary>
    /// Gets or sets the encryption type
    /// </summary>
    /// <value></value>
    public int EncType { get => encType; set => encType = value; }
    /// <summary>
    /// Gets or sets the salt length
    /// </summary>
    /// <value></value>
    public int SaltKeyLength { get => saltKeyLength; set => saltKeyLength = value; }

    /// <summary>
    /// Construct a SRTPPolicy object based on given parameters. This class acts as a storage class, so all the
    /// parameters are passed in through this constructor.
    /// </summary>
    /// <param name="encType">SRTP encryption type</param>
    /// <param name="encKeyLength">SRTP encryption key length</param>
    /// <param name="authType">SRTP authentication type</param>
    /// <param name="authKeyLength">SRTP authentication key length</param>
    /// <param name="authTagLength">SRTP authentication tag length</param>
    /// <param name="saltKeyLength">SRTP salt key length</param>
    public SrtpPolicy(int encType,
                      int encKeyLength,
                      int authType,
                      int authKeyLength,
                      int authTagLength,
                      int saltKeyLength)
    {
        this.encType = encType;
        this.encKeyLength = encKeyLength;
        this.authType = authType;
        this.authKeyLength = authKeyLength;
        this.authTagLength = authTagLength;
        this.saltKeyLength = saltKeyLength;
    }
}
