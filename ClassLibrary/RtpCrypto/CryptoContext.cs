/////////////////////////////////////////////////////////////////////////////////////
//  File:   CryptoContext.cs                                        26 Sep 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.RtpCrypto;

/// <summary>
/// Class for storing the settings and status of the cryptographic context for SRTP and SRTCP.
/// </summary>
public class CryptoContext
{
    /// <summary>
    /// Highest received RTP sequence number. This value is the s_l variable in Section 3.3.1 of RFC 3711.
    /// This is used by the receiver only.
    /// </summary>
    /// <value></value>
    public ushort HighestSeq = 0;

    /// <summary>
    /// Specifies the length in bytes of the packet authentication tag that is appended to each RTP or RTCP
    /// packet. Will be equal to 10 or 4 depending upon the crypto suite being used.
    /// </summary>
    /// <value></value>
    public int AuthTagLength = 10;

    /// <summary>
    /// If true, then each SRTP or SRTCP packet will be appended by a Master Key Indentifier (MKI) value that
    /// identifies the master encryption key used for that packet. If false, then no MKI will be attached to
    /// the packets.
    /// </summary>
    /// <value></value>
    public bool MkiIndicator = false;

    /// <summary>
    /// Specifies the length in bytes of the MKI attached to each SRTP or SRTCP packet. Used only if
    /// MkiIndicator is true.
    /// </summary>
    /// <value></value>
    public int MkiLength = 0;

    /// <summary>
    /// Contains the master keys and master salts used for this crypto context. Will contain at least one entry.
    /// </summary>
    /// <value></value>
    public List<MasterKeys> MasterKeys = new List<MasterKeys>();
    /// <summary>
    /// Index of the current master key in the MasterKeys list for RTP.
    /// </summary>
    /// <value></value>
    public int CurrentRtpMasterKeyIndex = 0;
    /// <summary>
    /// Index of the current maser key in the MasterKeys list for RTCP.
    /// </summary>
    /// <value></value>
    public int CurrentRtcpMasterKeyIndex = 0;

    /// <summary>
    /// Current Roll Over Counter (ROC) values. The ROC represents the number of times that the SEQ number
    /// of the SRTP packets has been reset to 0. The ROC is not used for RTCP.
    /// </summary>
    /// <value></value>
    public RocVals ROC = new RocVals();

    /// <summary>
    /// Stores the current session keys (key, salt and auth. key) for SRTP.
    /// </summary>
    /// <value></value>
    public SessionKeys RtpSessionKeys = new SessionKeys();

    /// <summary>
    /// Stores the current session keys (key, salt and auth. key) for SRTCP.
    /// </summary>
    /// <value></value>
    public SessionKeys RtcpSessionKeys = new SessionKeys();

    /// <summary>
    /// SRTP Index used for sending RTCP packets
    /// </summary>
    /// <value></value>
    public uint SendRtcpIndex = 0;

    /// <summary>
    /// Maximum RTCP Index value. 2^31 - 1 because only 31 bits of the RTCP index are used, the MS bit is
    /// used to indicate encryption.
    /// </summary>
    /// <value></value>
    public const uint MaxSendRtcpIndex = 2147483647;

    /// <summary>
    /// Gets the crypto suite name used by this crypto context.
    /// </summary>
    /// <value></value>
    public string CryptoSuite { get; private set; }

    /// <summary>
    /// Creates a new CryptoContext object with a single MasterKey. Use this constructor when building a SRTP
    /// crypto context for sending SRTP and SRTCP packets.
    /// </summary>
    /// <param name="cryptoSuite">Specifies the name of the crypto suite to use.</param>
    public CryptoContext(string cryptoSuite)
    {
        CryptoSuite = cryptoSuite;
        AuthTagLength = CryptoSuites.GetAuthTagLengthBytes(cryptoSuite);
        MasterKeys Mks = new MasterKeys(cryptoSuite);
        MasterKeys.Add(Mks);
    }

    /// <summary>
    /// Converts this crypto context object into a CryptoAttribute object.
    /// </summary>
    /// <returns>Returns a new CryptoAttribute object</returns>
    public CryptoAttribute ToCryptoAttribute()
    {
        CryptoAttribute attr = new CryptoAttribute();
        attr.CryptoSuite = CryptoSuite;
        InlineParams inlineParams = new InlineParams();
        inlineParams.MasterKey = MasterKeys[0].MasterKey;
        inlineParams.MasterSalt = MasterKeys[0].MasterSalt;
        inlineParams.Lifetime = Convert.ToUInt64(Math.Pow(2, 48));
        attr.InlineParameters.Add(inlineParams);
        return attr;
    }

    /// <summary>
    /// Creates a new CryptoContext object from a CryptoAttribute object.
    /// </summary>
    /// <param name="attr">Input CryptoAttribute</param>
    /// <returns>Returns a new CryptoContext object</returns>
    public static CryptoContext CreateFromCryptoAttribute(CryptoAttribute attr)
    {
        CryptoContext context = new CryptoContext(attr.CryptoSuite);
        context.AuthTagLength = CryptoSuites.GetAuthTagLengthBytes(attr.CryptoSuite);
        context.MasterKeys.Clear();
        MasterKeys Mks;
        foreach (InlineParams Ilp in attr.InlineParameters)
        {
            if (Ilp.MKI == 0)
                context.MkiIndicator = false;
            else
            {
                context.MkiIndicator = true;
                context.MkiLength = Ilp.MKI_Length;
            }

            Mks = new MasterKeys(attr.CryptoSuite);
            Mks.MKI = (uint) Ilp.MKI;
            Mks.KeyDerivationRate = (ulong) attr.KDR;
            Mks.NumKdrPackets = (ulong)Math.Pow(2, attr.KDR);
            Mks.MasterKey = Ilp.MasterKey;
            Mks.MasterSalt = Ilp.MasterSalt;
            context.MasterKeys.Add(Mks);
        }

        return context;
    }
}
