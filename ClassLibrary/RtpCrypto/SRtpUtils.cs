/////////////////////////////////////////////////////////////////////////////////////
//  File:   SRtpUtils.cs                                            20 Sep 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

using System.Security.Cryptography;
using SipLib.Rtp;

namespace SipLib.RtpCrypto;

/// <summary>
/// Class for storing the Roll Over Counter (ROC) for SRTP encryption. The ROC counts the number of times
/// that the RTP packet sequence number has wrapped around in a RTP media session.
/// </summary>
public class RocVals
{
    /// <summary>
    /// Current ROC value.
    /// </summary>
    /// <value></value>
    public uint Roc = 0;
    /// <summary>
    /// Stores the value of ROC - 1
    /// </summary>
    /// <value></value>
    public uint RocMinus1 = uint.MaxValue;
    /// <summary>
    /// Stores the value of ROC + 1
    /// </summary>
    /// <value></value>
    public uint RocPlus1 = 1;

    /// <summary>
    /// Increments the ROC
    /// </summary>
    public void IncrementRoc()
    {
        Roc += 1;
        RocMinus1 = Roc - 1;
        RocPlus1 = Roc + 1;
    }
}

/// <summary>
/// Enumeration for the valid value of the "label" specified in Sections 4.3.1 and 4.3.2 of RFC 3711. This
/// enumeration is used by the DeriveSrtpSessionKey() function to determine what type of session item
/// to generate.
/// </summary>
public enum SrtpLabelItem : ulong
{
    /// <summary>
    /// Generate a session key for SRTP.
    /// </summary>
    SrtpSessionKey = 0,
    /// <summary>
    /// Generate an authentication key for SRTP.
    /// </summary>
    SrtpAuthKey = 1,
    /// <summary>
    /// Generate a sessiion salt for SRTP.
    /// </summary>
    SrtpSessionSalt = 2,
    /// <summary>
    /// Generate a session key for SRTCP.
    /// </summary>
    SrtcpSessionKey = 3,
    /// <summary>
    /// Generate an authentication key for SRTCP.
    /// </summary>
    SrtcpAuthKey = 4,
    /// <summary>
    /// Generate a session salt for SRTCP.
    /// </summary>
    SrtcpSessionSalt = 5
}

/// <summary>
/// Static class that provides static functions for performing various Secure RTP (SRTP) calculations.
/// </summary>
public static class SRtpUtils
{
    /// <summary>
    /// Bit size of the block for the block cipher
    /// </summary>
    /// <value></value>
    public const int n_b = 128;
    /// <summary>
    /// Bit size of the encryption key.
    /// </summary>
    /// <value></value>
    public const int n_e = 128;
    /// <summary>
    /// Byte size of the encryption key
    /// </summary>
    /// <value></value>
    public const int n_eB = n_e / 8;
    /// <summary>
    /// Bit size of the session salting key
    /// </summary>
    /// <value></value>
    public const int n_s = 112;
    /// <summary>
    /// Byte size of the session salting key
    /// </summary>
    /// <value></value>
    public const int n_sB = n_s / 8;
    /// <summary>
    /// Default bit size of the authentication key.
    /// </summary>
    /// <value></value>
    public const int n_a = 160;
    /// <summary>
    /// Default byte size of the authentication key.
    /// </summary>
    /// <value></value>
    public const int n_aB = n_a / 8;
    /// <summary>
    /// Default bit size of the authentication tag.
    /// </summary>
    /// <value></value>
    public const int n_tag = 80;
    /// <summary>
    /// Default byte size of the authentication tag.
    /// </summary>
    /// <value></value>
    public const int n_tagB = n_tag / 8;

    /// <summary>
    /// Calculates the packet index for an RTP packet. See Section 3.3.1 and Appendix A of RFC 3711. This
    /// algorithm accounds for rollover of the sequence number and the impact of packet loss.
    /// </summary>
    /// <param name="SEQ">Sequence number (SEQ) read from the RTP packet header.</param>
    /// <param name="s_l">Highest received SEQ number so far.</param>
    /// <param name="Rv">Stored ROC values.</param>
    /// <returns>Returns the packet index as a 48-bit number right justified in the lower 48 bits of a 64-bit
    /// unsigned integer.</returns>
    public static ulong PacketIndex(ushort SEQ, ushort s_l, RocVals Rv)
    {
        ulong v = 0;
        ulong PcktIdx = 0;
        if (s_l < 32768)
        {
            if (SEQ - s_l > 32768)
                v = Rv.RocMinus1;
            else
                v = Rv.Roc;
        }
        else
        {
            if (s_l - 32768 > SEQ)
                v = Rv.RocPlus1;
            else
                v = Rv.Roc;
        }

        PcktIdx = SEQ + (v << 16);
        return PcktIdx;
    }

    /// <summary>
    /// Derives a session key given the master key and the master salt for SRTP.
    /// See Section 4.3.1 Key Derivation Algorithm and Section 4.3.2 SRTCP Key Derivation of RFC 3711.
    /// </summary>
    /// <param name="PI">Packet index for the SRTP packet as calculated by the PacketIndex() function is for
    /// SRTP. Pass in 32-bit value 0 || SRTCP Index from the SRTCP packet as described in Section 4.3.2 of
    /// RFC 3711.</param>
    /// <param name="kdr">Key Derivation Rate (KDR)</param>
    /// <param name="Label">Identifies the type of key to derive.</param>
    /// <param name="MasterSalt">Master salt array.</param>
    /// <param name="MasterKey">Master Key array.</param>
    /// <param name="InputZeroArray">Array of zero values to use as the input for the key derivation.
    /// The length of this array determines the length of the array returned by this function.</param>
    /// <returns>Returns a byte array containing the derived key, salting key or the authentication key.</returns>
    public static byte[] DeriveSrtpSessionKey(ulong PI, ulong kdr, SrtpLabelItem Label, byte[] MasterSalt, 
        byte[] MasterKey, byte[] InputZeroArray)
    {
        ulong LabelVal = (ulong) Label;

        ulong r;
        if (kdr == 0)
            r = PI;
        else
            r = PI / kdr;

        ulong key_id = (LabelVal << 48) | r;
        byte[] key_id_bytes = new byte[n_sB];
        // Right justify the 8-byte key-id in the 14-byte long array.
        RtpUtils.Set8ByteWord(key_id_bytes, 6, key_id);
        byte[] ShiftedXorResult = new byte[n_eB];
        for (int i = 0; i < n_sB; i++)
            ShiftedXorResult[i] = (byte)(MasterSalt[i] ^ key_id_bytes[i]);

        byte[] Result = new byte[InputZeroArray.Length];
        AesFunctions.AesCounterModeTransform(MasterKey, ShiftedXorResult, InputZeroArray, Result);

        return Result;
    }

    /// <summary>
    /// Calculates the Initialization Vector (IV) for AES in Counter Mode (AES-CM) as specified in Section
    /// 4.1.1 AES in Counter Mode of RFC 3711.
    /// </summary>
    /// <param name="k_s">Session salting key. Must be at least n_sB bytes in length.</param>
    /// <param name="SSRC">SSRC value from the RTCP packet.</param>
    /// <param name="i">Packet index for the RTP packet. Calculated as using the PacketIndex() function as
    /// specifed in Section 3.3.1 and Appendix A of RFC 3711.</param>
    /// <returns>Returns the IV array, which is n_eB (16) bytes in length.</returns>
    public static byte[] CalcAesCmIV(byte[] k_s, uint SSRC, ulong i)
    {
        // Must be the same length as the encryption key
        byte[] IV = new byte[n_eB];
        Array.ConstrainedCopy(k_s, 0, IV, 0, n_sB); // (k_s * 2^16)

        byte[] SSRC_Shift = new byte[n_eB];
        RtpUtils.SetDWord(SSRC_Shift, 4, SSRC);    // (SSRC * 2^64)

        byte[] PcktIdx_Shift = new byte[n_eB];
        RtpUtils.Set8ByteWord(PcktIdx_Shift, 8, (i << 16));

        for (int j = 0; j < n_eB; j++)
            IV[j] = (byte) (IV[j] ^ SSRC_Shift[j] ^ PcktIdx_Shift[j]);

        return IV;
    }

    private const int F8_RTP_IV_M_PT_IDX = 1;
    private const int F8_RTP_IV_SEQ_IDX = 2;
    private const int F8_RTP_IV_TS_IDX = 4;
    private const int F8_RTP_IV_SSRC_IDX = 8;
    private const int F8_RTP_IV_ROC_IDX = 12;

    /// <summary>
    /// Calculates the Initialization Vector (IV) for the F8 AES encryption algorithm for a RTP packet given
    /// the RTP packet header and the Roll Over Counter (ROC). See Section 4.1.2.2 of RFC 3711.
    /// </summary>
    /// <param name="RtpPckt">RTP packet header.</param>
    /// <param name="ROC">Current Roll Over Counter value.</param>
    /// <returns>Returns the 16-byte long IV to use for the IV for an RTP packet.</returns>
    public static byte[] CalcF8SRTPIV(RtpPacket RtpPckt, uint ROC)
    {
        byte[] IV = new byte[SRtpUtils.n_eB];
        int MarkerBit = RtpPckt.Marker == true ? 1 : 0;
        byte MPTByte = (byte)((MarkerBit << 7) | (RtpPckt.
            PayloadType & 0x7f));
        IV[F8_RTP_IV_M_PT_IDX] = MPTByte;
        RtpUtils.SetWord(IV, F8_RTP_IV_SEQ_IDX, RtpPckt.SequenceNumber);
        RtpUtils.SetDWord(IV, F8_RTP_IV_TS_IDX, RtpPckt.Timestamp);
        RtpUtils.SetDWord(IV, F8_RTP_IV_SSRC_IDX, RtpPckt.SSRC);
        RtpUtils.SetDWord(IV, F8_RTP_IV_ROC_IDX, ROC);

        return IV;
    }

    private const int F8_RTCP_IV_E_SRTCP_INDEX_IDX = 4;
    private const int F8_RTCP_IV_V_P_RC_IDX = 8;
    private const int F8_RTCP_IV_PT_RC_IDX = 9;
    private const int F8_RTCP_IV_SSRC_IDX = 12;

    /// <summary>
    /// Calculates the Initialization Vector (IV) for the F8 AES encryption algorithm for a RTCP packet given
    /// the RTCP packet and the SRTCP packet index. See Section 4.1.2.3 of RFC 3711.
    /// </summary>
    /// <param name="RtcpHdr">RTCP packet header to compute the IV for.</param>
    /// <param name="SrtcpIndex">SRTCP packet index as read from the the
    /// SRTCP packet. Includes the "E" bit. See Figure 2 of RFC 3711.</param>
    /// <param name="SSRC">SSRC for the sender of the RTCP packet.</param>
    /// <returns>Returns the 16-byte long IV to use for the IV for an RTCP packet.</returns>
    public static byte[] CalcF8SRTCPIV(RtcpHeader RtcpHdr, uint SrtcpIndex, uint SSRC)
    {
        byte[] IV = new byte[SRtpUtils.n_eB];
        RtpUtils.SetDWord(IV, F8_RTCP_IV_E_SRTCP_INDEX_IDX, SrtcpIndex);
        byte V_P_RC = (byte)(RtcpHdr.Version << 6 | RtcpHdr.PaddingBit << 5 | RtcpHdr.Count);
        IV[F8_RTCP_IV_V_P_RC_IDX] = V_P_RC;
        uint PTLength = (uint) ((int) RtcpHdr.PacketType << 15 | RtcpHdr.Length);
        RtpUtils.Set3Bytes(IV, F8_RTCP_IV_PT_RC_IDX, PTLength);
        RtpUtils.SetDWord(IV, F8_RTCP_IV_SSRC_IDX, SSRC);

        return IV;
    }

    /// <summary>
    /// Authenticates an SRTP or an SRTCP packet. See Section 4.2 of RFC 3711. This function computes the
    /// authentication tag for the packet using the HMAC-SHA1 algorithm and compares it to the authentication
    /// tag that was sent with the SRTP or SRTCP packet.
    /// </summary>
    /// <param name="HdrBytes">Byte array containing the RTP or RTCP packet header.</param>
    /// <param name="PacketPayload">Byte array containing the payload of the RTP or RTCP packet. This is the
    /// encrypted portion of the packet.</param>
    /// <param name="ROCorSRTCPIndex">Roll Over Counter (ROC) value or the SRTCP Index.</param>
    /// <param name="k_a">Byte array containing the session authentication key.</param>
    /// <param name="AuthTag">Byte array containing the authentication tag read from the SRTP packet.</param>
    /// <returns>Returns true if the packet is successfully authenticated, i.e. the computed authentication
    /// tag matches the received authentication tag.</returns>
    public static bool AuthRtpPacket(byte[] HdrBytes, byte[] PacketPayload, uint ROCorSRTCPIndex, byte[] k_a,
        byte[] AuthTag)
    {
        bool Success = true;

        byte[] AuthPacket = BuildAuthPacket(HdrBytes, PacketPayload, ROCorSRTCPIndex);
        HMACSHA1 hmac = new HMACSHA1(k_a);
        byte[] HashVal = hmac.ComputeHash(AuthPacket);
        
        // Compare the two hash values.
        for (int i=0; i < AuthTag.Length; i++)
        {
            if (AuthTag[i] != HashVal[i])
                return false;
        }

        return Success;
    }

    /// <summary>
    /// Builds a byte array that contains the authenticated portion of a RTP or RTCP packet.
    /// </summary>
    /// <param name="HdrBytes"></param>
    /// <param name="PacketPayload"></param>
    /// <param name="ROCorSRTCPIndex"></param>
    /// <returns>A byte array that contains the authenticated portion of a RTP or RTCP packet</returns>
    private static byte[] BuildAuthPacket(byte[] HdrBytes, byte[] PacketPayload, uint ROCorSRTCPIndex)
    {
        int AuthPacketLen = HdrBytes.Length + PacketPayload.Length + 4;
        byte[] AuthPacket = new byte[AuthPacketLen];
        Array.Copy(HdrBytes, AuthPacket, HdrBytes.Length);
        Array.ConstrainedCopy(PacketPayload, 0, AuthPacket, HdrBytes.Length, PacketPayload.Length);
        int RocDestIdx = HdrBytes.Length + PacketPayload.Length;
        RtpUtils.SetDWord(AuthPacket, RocDestIdx, ROCorSRTCPIndex);

        return AuthPacket;
    }

    /// <summary>
    /// Calculates the authentication tag for an RTP or an RTCP packet. See Section 4.2 of RFC 3711.
    /// </summary>
    /// <param name="HdrBytes">Byte array containing the RTP packet header./// </param>
    /// <param name="Payload">Byte array containing the payload of the RTP or RTCP packet. This is the
    /// encrypted portion of the packet.</param>
    /// <param name="ROCorSRTCPIndex">Roll Over Counter (ROC) value.</param>
    /// <param name="k_a">Byte array containing the session authentication key.</param>
    /// <param name="TagLength">Length in bytes of the authentication tag to return. This must be either 10
    /// (for 80 bits) or 4 (for 32 bits) depending upon the encryption profile being used.</param>
    /// <returns>The authentication tag</returns>
    public static byte[] CalcRtpPacketAuthTag(byte[] HdrBytes, byte[] Payload, uint ROCorSRTCPIndex, 
        byte[] k_a, int TagLength)
    {
        byte[] AuthTag = new byte[TagLength];
        byte[] AuthPacket = BuildAuthPacket(HdrBytes, Payload, ROCorSRTCPIndex);
        HMACSHA1 hmac = new HMACSHA1(k_a);
        // ComputeHash returns 20 bytes (160 bits).
        byte[] HashVal = hmac.ComputeHash(AuthPacket);
        Array.Copy(HashVal, AuthTag, TagLength);

        return AuthTag;
    }

    /// <summary>
    /// Gets the value portion of a name/value pair from a string that is formatted as a name followed by a
    /// 1 character separator followed by a value. For example Param=Value or Param:Value
    /// </summary>
    /// <param name="Input">Input string containing the name/value.</param>
    /// <param name="Sep">Separator character such as '=' or ':'</param>
    /// <returns>Returns the value portion or null if the input string is not in the proper format.</returns>
    public static string? GetValueOfNameValuePair(string Input, char Sep)
    {
        if (string.IsNullOrEmpty(Input) == true)
            return null;

        int Idx = Input.IndexOf(Sep);
        if (Idx < 0 || Idx == Input.Length - 1)
            return null;
        else
            return Input.Substring(Idx + 1).TrimStart();
    }

    /// <summary>
    /// Derives the RTP session keys for a CryptoContext. See Section 4.3.1 of RFC 3711
    /// </summary>
    /// <param name="PI">Packet Index</param>
    /// <param name="Mks">Master keys to use to derive the session keya</param>
    /// <param name="Context">CryptoContext to write the session keys to</param>
    public static void DeriveRtpSessionKeys(ulong PI, MasterKeys Mks, CryptoContext Context)
    {
        byte[] KeyZeroInput = new byte[CryptoSuites.GetKeyLengthBytes(Context.CryptoSuite)];
        byte[] SaltZeroInput = new byte[n_sB];
        byte[] AuthZeroInput = new byte[CryptoSuites.GetAuthTagLengthBytes(Context.CryptoSuite)];

        Context.RtpSessionKeys.SessionKey = SRtpUtils.DeriveSrtpSessionKey(PI, Mks.KeyDerivationRate,
            SrtpLabelItem.SrtpSessionKey, Mks.MasterSalt, Mks.MasterKey, KeyZeroInput);
        Context.RtpSessionKeys.SessionSalt = SRtpUtils.DeriveSrtpSessionKey(PI, Mks.KeyDerivationRate,
            SrtpLabelItem.SrtpSessionSalt, Mks.MasterSalt, Mks.MasterKey, SaltZeroInput);
        Context.RtpSessionKeys.SessionAuthKey = SRtpUtils.DeriveSrtpSessionKey(PI, Mks.KeyDerivationRate,
            SrtpLabelItem.SrtpAuthKey, Mks.MasterSalt, Mks.MasterKey, AuthZeroInput);
    }

    /// <summary>
    /// Derives the RTCP session keys for a CryptoContext. See Section 4.3.2 of RFC 3711.
    /// </summary>
    /// <param name="PI">Packet Index</param>
    /// <param name="Mks">Master keys to use to derive the session keya</param>
    /// <param name="Context">CryptoContext to write the session keys to</param>
    public static void DeriveRtcpSessionKeys(ulong PI, MasterKeys Mks, CryptoContext Context)
    {
        byte[] KeyZeroInput = new byte[CryptoSuites.GetKeyLengthBytes(Context.CryptoSuite)];
        byte[] SaltZeroInput = new byte[SRtpUtils.n_sB];
        byte[] AuthZeroInput = new byte[CryptoSuites.GetAuthTagLengthBytes(Context.CryptoSuite)];

        Context.RtcpSessionKeys.SessionKey = SRtpUtils.DeriveSrtpSessionKey(PI, Mks.KeyDerivationRate,
            SrtpLabelItem.SrtcpSessionKey, Mks.MasterSalt, Mks.MasterKey, KeyZeroInput);
        Context.RtcpSessionKeys.SessionSalt = SRtpUtils.
            DeriveSrtpSessionKey(PI, Mks.KeyDerivationRate,
            SrtpLabelItem.SrtcpSessionSalt, Mks.MasterSalt, Mks.MasterKey, SaltZeroInput);
        Context.RtcpSessionKeys.SessionAuthKey = SRtpUtils.DeriveSrtpSessionKey(PI, Mks.KeyDerivationRate,
            SrtpLabelItem.SrtcpAuthKey, Mks.MasterSalt, Mks.MasterKey, AuthZeroInput);
    }

    /// <summary>
    /// Extracts a string that is delimited by two characters. The delimiter characters may be different.
    /// </summary>
    /// <param name="D1">First delimiter character.</param>
    /// <param name="D2">Second delimiter character.</param>
    /// <param name="strInput">Input string.</param>
    /// <returns>Returns the delimited string. Returns null if the delimiters are not present or the extracted
    /// string length is zero.</returns>
    public static string? ExtractDelimStr(char D1, char D2, string strInput)
    {
        string strRetVal = null;
        int IdxD1 = strInput.IndexOf(D1);
        int IdxD2;
        if (D1 == D2)
            IdxD2 = strInput.LastIndexOf(D2);
        else
            IdxD2 = strInput.IndexOf(D2);
        if (IdxD1 >= 0 && IdxD2 > 0 && IdxD2 > IdxD1 && (IdxD2 - IdxD1) > 1)
        {
            int StartIdx = IdxD1 + 1;
            strRetVal = strInput.Substring(StartIdx, IdxD2 - StartIdx);
        }

        return strRetVal;
    }
}
