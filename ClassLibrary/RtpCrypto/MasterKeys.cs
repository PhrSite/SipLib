/////////////////////////////////////////////////////////////////////////////////////
//  File:   MasterKeys.cs                                           25 Sep 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.RtpCrypto;
using System.Security.Cryptography;

/// <summary>
/// Class for storing a single SRTP master key, master salt and parameters related to these.
/// </summary>
public class MasterKeys
{
    /// <summary>
    /// Master Key Identifier (MKI) for the master key.
    /// </summary>
    /// <value></value>
    public uint MKI = 0;

    /// <summary>
    /// Stores the Key Derivation Rate (KDR). This is a power of 2 that is in the range of 1 to 24. This
    /// value specifies the number of packets after which the session keys are recalculated. A value of 0
    /// indicates that the KDR is unspecified. This means that the session keys are only calculated from
    /// the master keys once.
    /// </summary>
    /// <value></value>
    public ulong KeyDerivationRate = 0;

    /// <summary>
    /// Specifies the number of packets for the KDR. This value if calculated as 2^KDR.
    /// </summary>
    /// <value></value>
    public ulong NumKdrPackets = 0;

    /// <summary>
    /// Contains the bytes of the master key. The key length depends on the AES encryption algorithm being
    /// used.
    /// </summary>
    /// <value></value>
    public byte[]? MasterKey;

    /// <summary>
    /// Contains the bytes of the master salt. The length will always be 14 bytes for the AES-CM and AES-f8
    /// encryption algorithms.
    /// </summary>
    /// <value></value>
    public byte[]? MasterSalt;

    /// <summary>
    /// Number of RTP packets that have been sent using this master key.
    /// </summary>
    /// <value></value>
    public long RtpMasterKeyCount = 0;

    /// <summary>
    /// The number of RTCP packets that have been sent using this master key.
    /// </summary>
    /// <value></value>
    public long RtcpMasterKeyCount = 0;

    private static RandomNumberGenerator m_Rng = RandomNumberGenerator.Create();

    /// <summary>
    /// Gets the crypto suite name
    /// </summary>
    /// <value></value>
    public string CryptoSuite { get; private set; }

    /// <summary>
    /// Creates a new RtpMaster key object and initializes the MasterKey and the MasterSalt to random values.
    /// </summary>
    /// <param name="cryptoSuite">Crypto suite to be used.</param>
    public MasterKeys(string cryptoSuite)
    {
        CryptoSuite = cryptoSuite;
        MasterKey = new byte[CryptoSuites.GetKeyLengthBytes(cryptoSuite)];
        MasterSalt = new byte[SRtpUtils.n_sB];  // Always 14 bytes for all crypto suites
        m_Rng.GetBytes(MasterKey);
        m_Rng.GetBytes(MasterSalt);
        MKI = 1;
    }
}
