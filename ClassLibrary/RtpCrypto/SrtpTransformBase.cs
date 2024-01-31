/////////////////////////////////////////////////////////////////////////////////////
//  File:   SrtpTransformBase.cs                                    27 Sep 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

using SipLib.Rtp;

namespace SipLib.RtpCrypto;

/// <summary>
/// Base class for the SrtpEncryptor and SrtpDecryptor classes
/// </summary>
public class SrtpTransformBase
{
    /// <summary>
    /// Cryptographic context to use for encryption and decryption of RTP and RTCP packets.
    /// </summary>
    /// <value></value>
    protected CryptoContext m_Context;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="context">CryptoContext to use</param>
    protected SrtpTransformBase(CryptoContext context)
    {
        m_Context = context;
    }

    /// <summary>
    /// Applies the AES or F8 transform specified in the CryptoContext to the input byte array to produce
    /// the output byte array for a RTP packet
    /// </summary>
    /// <param name="Rp">RtpPacket containing the SSRC.</param>
    /// <param name="PI">The calculated Packet Index</param>
    /// <param name="InputBytes">Input array</param>
    /// <param name="OutputBytes">Output array. Must be the same length as the input array</param>
    protected void ApplySrtpTransform(RtpPacket Rp, ulong PI, byte[] InputBytes, byte[] OutputBytes)
    {
        if (m_Context.CryptoSuite != CryptoSuites.F8_128_HMAC_SHA1_80)
        {
            byte[] AesCmIV = SRtpUtils.CalcAesCmIV(m_Context.RtpSessionKeys.SessionSalt, Rp.SSRC, PI);
            AesFunctions.AesCounterModeTransform(m_Context.RtpSessionKeys.SessionKey, AesCmIV, InputBytes,
                OutputBytes);
        }
        else
        {   // Its AES_F8
            byte[] AesF8IV = SRtpUtils.CalcF8SRTPIV(Rp, m_Context.ROC.Roc);
            AesFunctions.AesF8ModeTransform(m_Context.RtpSessionKeys.SessionKey, m_Context.RtpSessionKeys.
                SessionSalt, AesF8IV, InputBytes, OutputBytes);
        }
    }

    /// <summary>
    /// Applies the AES or F8 transform specified in the CryptoContext to the input byte array to produce
    /// the output byte array for a RTCP packet
    /// </summary>
    /// <param name="Header">RTCP packet header</param>
    /// <param name="SSRC">SSRC of the RTCP packet</param>
    /// <param name="PI">The calculated Packet Index</param>
    /// <param name="InputBytes">Input array</param>
    /// <param name="OutputBytes">Output array. Must be the same length as the input array</param>
    protected void ApplySrtcpTransform(RtcpHeader Header, uint SSRC, ulong PI, byte[] InputBytes,
        byte[] OutputBytes)
    {
        if (m_Context.CryptoSuite != CryptoSuites.F8_128_HMAC_SHA1_80)
        {
            byte[] AesCmIV = SRtpUtils.CalcAesCmIV(m_Context.RtcpSessionKeys.SessionSalt, SSRC, PI);
            AesFunctions.AesCounterModeTransform(m_Context.RtcpSessionKeys.SessionKey, AesCmIV,
                InputBytes, OutputBytes);
        }
        else
        {   // Its AES_F8
            byte[] AesF8IV = SRtpUtils.CalcF8SRTCPIV(Header, (uint)PI, SSRC);
            AesFunctions.AesF8ModeTransform(m_Context.RtcpSessionKeys.SessionKey,
                m_Context.RtcpSessionKeys.SessionSalt, AesF8IV, InputBytes, OutputBytes);
        }
    }

    /// <summary>
    /// Applies the AES or F8 transform specified in the CryptoContext to the input byte array to produce
    /// the output byte array for a RTCP packet
    /// </summary>
    /// <param name="Header">RTCP header for the RTCP packet</param>
    /// <param name="SSRC">SSRC of the RTCP packet</param>
    /// <param name="PI">The calculated Packet Index</param>
    /// <param name="InputBytes">Input array</param>
    /// <param name="StartIdx">Starting index in the input array</param>
    /// <param name="Length">Number of bytes to apply the transform to</param>
    /// <param name="OutputBytes">Output array. Must be the same length as the input array</param>
    protected void ApplySrtcpTransform(RtcpHeader Header, uint SSRC, ulong PI, byte[] InputBytes, 
        int StartIdx, int Length, byte[] OutputBytes)
    {
        if (m_Context.CryptoSuite != CryptoSuites.F8_128_HMAC_SHA1_80)
        {
            byte[] AesCmIV = SRtpUtils.CalcAesCmIV(m_Context.RtcpSessionKeys.SessionSalt, SSRC, PI);
            AesFunctions.AesCounterModeTransform(m_Context.RtcpSessionKeys.SessionKey, AesCmIV, InputBytes,
                StartIdx, Length, OutputBytes);
        }
        else
        {
            //RtcpHeader Header = new RtcpHeader(Pckt, 0);
            byte[] AesF8IV = SRtpUtils.CalcF8SRTCPIV(Header, (uint)PI, SSRC);
            AesFunctions.AesF8ModeTransform(m_Context.RtcpSessionKeys.SessionKey, m_Context.RtcpSessionKeys.
                SessionSalt, AesF8IV, InputBytes, StartIdx, Length, OutputBytes);
        }
    }

    /// <summary>
    /// Gets or sets the reason for an encryption or a decryption error. This property will be set to
    /// a value indicating the cause of the error if an encryption or decryption method returns null.
    /// </summary>
    /// <value></value>
    public SRtpErrorsEnum Error { get; protected set; } = SRtpErrorsEnum.NoError;
}

/// <summary>
/// Enumeration of SRTP error conditions
/// </summary>
public enum SRtpErrorsEnum
{
    /// <summary>
    /// No errors detected.
    /// </summary>
    NoError,
    /// <summary>
    /// The input packet was too short.
    /// </summary>
    InputPacketTooShort,
    /// <summary>
    /// The CryptoContext specified that an MKI will be provided with each packet but the input packet
    /// did not contain an MKI.
    /// </summary>
    NoMKI,
    /// <summary>
    /// To input packet was too short to contain an authentication tag
    /// </summary>
    NoAuthenticationTag,
    /// <summary>
    /// The input packet for decryption contained an MKI, but the master key for that MKI was not found
    /// in the CryptoContext
    /// </summary>
    MasterKeyNotFound,
    /// <summary>
    /// Packet authentication failed when decrypting an input packet.
    /// </summary>
    AuthenticationFailed,
}
