/////////////////////////////////////////////////////////////////////////////////////
//  File:   SrtpDecryptor.cs                                        26 Sep 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

using SipLib.Rtp;
using System.Security.Cryptography;

namespace SipLib.RtpCrypto;

/// <summary>
/// Class for decrypting SRTP and SRTCP packets received from a remote endpoint.
/// </summary>
public class SrtpDecryptor
{
    private CryptoContext m_Context = null;

    private bool m_FirstPacketReceived = false;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="context">CryptoContext provided by the remote endpoint if SRTP is being used.
    /// If null, then RTP and RTCP packets are not encrypted.</param>
    public SrtpDecryptor(CryptoContext context)
    {
        m_Context = context;
    }

    /// <summary>
    /// Decrypts an RTP packet if encryption is used.
    /// </summary>
    /// <param name="Pckt">Input RTP packet.</param>
    /// <returns>Returns the decrypted version of the encrypted input packet if encryption is being
    /// used. Returns the original input packet if encryption is not being used. Returns null if
    /// an error is detected.</returns>
    public byte[] DecryptRtpPacket(byte[] Pckt)
    {
        if (Pckt == null || Pckt.Length < RtpPacket.MIN_PACKET_LENGTH)
            return null;

        byte[] DecryptedPckt = null;
        if (m_Context != null)
            return Pckt;

        RtpPacket Rp = new RtpPacket(Pckt);
        int PcktMkiIdx = 0; // Index of the Master Key Identifier (MKI) at the end of the packet
        if (m_Context.MkiLength > 0)
        {
            PcktMkiIdx = Pckt.Length - m_Context.AuthTagLength - m_Context.MkiLength;
            if (PcktMkiIdx > Rp.HeaderLength)
                // Error: The packet is too short, just ignore it.
                return Pckt;
        }

        int AuthTagIdx = Pckt.Length - m_Context.AuthTagLength;
        if (AuthTagIdx < Rp.HeaderLength + m_Context.MkiLength)
            // Error: The packet is not long enough, just ignore it.
            return Pckt;

        ushort SEQ = Rp.SequenceNumber;
        if (m_FirstPacketReceived == false)
        {
            m_FirstPacketReceived = true;
            m_Context.HighestSeq = SEQ;
        }

        // Determine which master key to use.
        if (m_Context.MkiIndicator == true)
        {   // The MKI is at the end of the RTP packet.
            int MasterKeyIndex = GetMasterKeyIndex(Pckt, PcktMkiIdx);
            if (MasterKeyIndex == -1)
                return null;    // Errors already logged.

            if (MasterKeyIndex != m_Context.CurrentRtpMasterKeyIndex)
            {   // This packet uses a different master key than the previous packet.
                m_Context.CurrentRtpMasterKeyIndex = MasterKeyIndex;
                // Reset the session keys to null so that they are regenerated using the new master key.
                m_Context.RtpSessionKeys = new SessionKeys();
            }
            // Else, the index of the master key has not changed.
        }
        // Else use the current master key.

        ulong PI = SRtpUtils.PacketIndex(SEQ, m_Context.HighestSeq, m_Context.ROC);

        // Update the ROC in the m_RxCryptoContext if the SEQ from the RTP header wrapped around.
        if (SEQ < m_Context.HighestSeq)
            // The sequence number (SEQ) of the RTP packet stream wrapped around. Update the ROC.
            m_Context.ROC.IncrementRoc();

        // Update the s_l value
        m_Context.HighestSeq = SEQ;

        MasterKeys Mks = m_Context.MasterKeys[m_Context.CurrentRtpMasterKeyIndex];

        bool DeriveSessionKeys = false;
        if (Mks.NumKdrPackets > 0)
        {   // See Section 4.3.1 of RFC 3711.
            if (PI % Mks.NumKdrPackets == 0)
                DeriveSessionKeys = true;
        }

        if (m_Context.RtpSessionKeys.SessionKey == null || DeriveSessionKeys == true)
            // The session keys have not been derived yet because this is the first packet or its time
            // to derive new session keys based on the packet index.
            SRtpUtils.DeriveRtpSessionKeys(PI, Mks, m_Context);

        int ActualPayloadLength = Pckt.Length - Rp.HeaderLength - m_Context.MkiLength - m_Context.AuthTagLength;
        byte[] PayloadBytes = new byte[ActualPayloadLength];
        Array.ConstrainedCopy(Pckt, Rp.HeaderLength, PayloadBytes, 0, ActualPayloadLength);

        // Authenticate the packet using the current auth. session key
        bool Success = AuthenticateRtpPacket(Pckt, PayloadBytes, Rp, AuthTagIdx);
        if (Success == false)
            // Error: The packet could not be authenticated.
            return null;

        byte[] DecryptedPayload = new byte[ActualPayloadLength];
        // Decrypt the payload portion of the packet.
        if (m_Context.CryptoSuite != CryptoSuites.F8_128_HMAC_SHA1_80)
        {
            byte[] AesCmIV = SRtpUtils.CalcAesCmIV(m_Context.RtpSessionKeys.SessionSalt, Rp.SSRC, PI);
            AesFunctions.AesCounterModeTransform(m_Context.RtpSessionKeys.SessionKey, AesCmIV, PayloadBytes,
                DecryptedPayload);
        }
        else
        {   // Its AES_F8
            byte[] AesF8IV = SRtpUtils.CalcF8SRTPIV(Rp, m_Context.ROC.Roc);
            AesFunctions.AesF8ModeTransform(m_Context.RtpSessionKeys.SessionKey, m_Context.RtpSessionKeys.
                SessionSalt, AesF8IV, PayloadBytes, DecryptedPayload);
        }

        // Build the decrypted RTP packet from the RTP packet header and
        // the decrypted payload.
        DecryptedPckt = new byte[Rp.HeaderLength + ActualPayloadLength];
        Array.Copy(Pckt, DecryptedPckt, Rp.HeaderLength);
        Array.ConstrainedCopy(DecryptedPayload, 0, DecryptedPckt, Rp.HeaderLength, ActualPayloadLength);

        return DecryptedPckt;
    }

    /// <summary>
    /// Decrypts an RTCP packet if encryption is used.
    /// </summary>
    /// <param name="Pckt">Input RTCP packet.</param>
    /// <returns>Returns the decrypted version of the encrypted input packet if encryption is being
    /// used. Returns the original input packet if encryption is not being used. Returns null if
    /// an error is detected.</returns>
    public byte[] DecryptRtcpPacket(byte[] Pckt)
    {
        if (Pckt == null || Pckt.Length < RtcpHeader.RTCP_HEADER_LENGTH)
            return null;

        byte[] DecryptedPckt = null;
        if (m_Context != null)
            return Pckt;

        int RtcpIndexIdx = Pckt.Length - m_Context.AuthTagLength - m_Context.MkiLength - 4;
        if (RtcpIndexIdx < 0)
            // Error: The RTCP packet is too short
            return null;

        uint RtcpIndex = RtpUtils.GetDWord(Pckt, RtcpIndexIdx);
        bool IsEncrypted = (RtcpIndex & 0x80000000) == 0x80000000 ? true : false;
        RtcpIndex = RtcpIndex & 0x7fffffff;   // Clear the E bit

        // Determine which master key to use.
        if (m_Context.MkiIndicator == true)
        {
            int MkiIdx = Pckt.Length - m_Context.AuthTagLength - m_Context.MkiLength;
            int MasterKeyIndex = GetMasterKeyIndex(Pckt, MkiIdx);
            if (MasterKeyIndex == -1)
                return null;

            if (MasterKeyIndex != m_Context.CurrentRtcpMasterKeyIndex)
            {   // This packet uses a different master key than the previous packet.
                m_Context.CurrentRtcpMasterKeyIndex = MasterKeyIndex;
                // Reset the session keys to null so that they are regenerated 
                // using the new master key.
                m_Context.RtcpSessionKeys = new SessionKeys();
            }
            // Else, the index of the master key has not changed.
        }

        ulong PI = RtcpIndex;
        MasterKeys Mks = m_Context.MasterKeys[m_Context.CurrentRtcpMasterKeyIndex];

        bool DeriveSessionKeys = false;
        if (Mks.NumKdrPackets > 0)
        {   // See Section 4.3.1 of RFC 3711.
            if (PI % Mks.NumKdrPackets == 0)
                DeriveSessionKeys = true;
        }

        if (m_Context.RtcpSessionKeys.SessionKey == null || DeriveSessionKeys == true)
            // The session keys have not been derived yet because this is the first packet or its time to
            // derive new session keys based on the packet index.
            SRtpUtils.DeriveRtcpSessionKeys(PI, Mks, m_Context);

        // Authenticate the RTCP packet.
        int AuthPayloadLen = Pckt.Length - m_Context.MkiLength - m_Context.AuthTagLength;
        byte[] AuthPayloadBytes = new byte[AuthPayloadLen];
        Array.Copy(Pckt, AuthPayloadBytes, AuthPayloadLen);
        int AuthTagIdx = Pckt.Length - m_Context.AuthTagLength;
        byte[] AuthTagBytes = new byte[m_Context.AuthTagLength];
        Array.ConstrainedCopy(Pckt, AuthTagIdx, AuthTagBytes, 0, m_Context.AuthTagLength);

        bool Success = AuthenticateRtcpPacket(AuthPayloadBytes, AuthTagBytes);
        if (Success == false)
            return null;

        RtcpHeader Header = new RtcpHeader(Pckt, 0);
        uint SSRC = RtpUtils.GetDWord(Pckt, 4);
        // Remove the SRTCP index also
        int RetPcktLen = Pckt.Length - m_Context.AuthTagLength - m_Context.MkiLength - 4;
        DecryptedPckt = new byte[RetPcktLen];

        if (IsEncrypted == true)
        {   // Get the encrypted portion of the packet. See Figure 2 or RFC 3711 Skip the RTCP header
            // (4 bytes) and the SSRC (4 bytes)
            int StartIdx = 8;
            int EndIdx = Pckt.Length - m_Context.AuthTagLength - m_Context.MkiLength - 4;
            int Len = EndIdx - StartIdx;
            byte[] EncryptedBytes = new byte[Len];
            Array.ConstrainedCopy(Pckt, StartIdx, EncryptedBytes, 0, Len);
            byte[] DecryptedBytes = new byte[Len];

            if (m_Context.CryptoSuite != CryptoSuites.F8_128_HMAC_SHA1_80)
            {
                byte[] AesCmIV = SRtpUtils.CalcAesCmIV(m_Context.RtcpSessionKeys.SessionSalt, SSRC, PI);
                AesFunctions.AesCounterModeTransform(m_Context.RtcpSessionKeys.SessionKey, AesCmIV,
                    EncryptedBytes, DecryptedBytes);
            }
            else
            {   // Its AES_F8
                byte[] AesF8IV = SRtpUtils.CalcF8SRTCPIV(Header, (uint)PI, SSRC);
                AesFunctions.AesF8ModeTransform(m_Context.RtcpSessionKeys.SessionKey, 
                    m_Context.RtcpSessionKeys.SessionSalt, AesF8IV, EncryptedBytes, DecryptedBytes);
            }

            // Build the return packet
            Array.Copy(Pckt, DecryptedPckt, 8);   // Copy the RTCP header and the SSRC
            Array.ConstrainedCopy(DecryptedBytes, 0, DecryptedPckt, StartIdx, DecryptedBytes.Length);
        }
        else
            Array.Copy(Pckt, DecryptedPckt, RetPcktLen);

        return DecryptedPckt;
    }

    // See Section 4.2 of RFC 3711
    private bool AuthenticateRtcpPacket(byte[] AuthPcktBytes, byte[] AuthTagBytes)
    {
        HMACSHA1 hmac = new HMACSHA1(m_Context.RtcpSessionKeys.SessionAuthKey);
        byte[] HashVal = hmac.ComputeHash(AuthPcktBytes);
        for (int i = 0; i < AuthTagBytes.Length; i++)
        {
            if (AuthTagBytes[i] != HashVal[i])
                return false;
        }

        return true;
    }

    // See Section 4.2 of RFC 3711
    private bool AuthenticateRtpPacket(byte[] Pckt, byte[] PayloadBytes, RtpPacket Rp, int AuthTagIdx)
    {
        bool Success = true;
        byte[] AuthTagBytes = new byte[m_Context.AuthTagLength];
        Array.ConstrainedCopy(Pckt, AuthTagIdx, AuthTagBytes, 0, m_Context.AuthTagLength);
        Success = SRtpUtils.AuthRtpPacket(Rp.GetHeaderBytes(), PayloadBytes, m_Context.ROC.Roc,
            m_Context.RtpSessionKeys.SessionAuthKey, AuthTagBytes);

        return Success;
    }

    /// <summary>
    /// Gets the index of the master key to use by reading the MKI from the packet and then searching the
    /// list of master keys for one that has a MKI value equal to the value read from the packet.
    /// Only call this function if the MKI indicator value is set to 1 in the crytographic context.
    /// </summary>
    /// <param name="Pckt">Input RTP or RTCP packet to get the MKI from.</param>
    /// <param name="MkiPcktIdx">Index of the MKI in the packet.</param>
    /// <returns>Return the index of the master key in the list of master keys in the crypto context.
    /// Returns -1 if an error occurred.
    /// </returns>
    private int GetMasterKeyIndex(byte[] Pckt, int MkiPcktIdx)
    {
        int MkiIdx = -1;
        uint PcktMki = GetMki(Pckt, MkiPcktIdx);
        if (PcktMki == 0)
            // Error: Invalid MKI value.
            return -1;
        else
        {
            MkiIdx = MkiToMkiIndex(PcktMki);
            if (MkiIdx == -1)
                // Error: MKI not found.
                return -1;
        }

        return MkiIdx;
    }

    private int MkiToMkiIndex(uint MKI)
    {
        int Idx = -1;   // Indicates not found
        for (int i = 0; i < m_Context.MasterKeys.Count; i++)
        {
            if (m_Context.MasterKeys[i].MKI == MKI)
            {
                Idx = i;
                break;
            }
        }

        return Idx;
    }


    private uint GetMki(byte[] Pckt, int MkiIdx)
    {
        uint Mki = 0;
        switch (m_Context.MkiLength)
        {
            case 1:
                Mki = Pckt[MkiIdx];
                break;
            case 2:
                Mki = RtpUtils.GetWord(Pckt, MkiIdx);
                break;
            case 3:
                Mki = RtpUtils.Get3Bytes(Pckt, MkiIdx);
                break;
            case 4:
                Mki = RtpUtils.GetDWord(Pckt, MkiIdx);
                break;
        }

        return Mki;
    }


}
