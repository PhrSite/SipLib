/////////////////////////////////////////////////////////////////////////////////////
//  File:   SrtpEncryptor.cs                                        26 Sep 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

using SipLib.Rtp;
using System.Security.Cryptography;

namespace SipLib.RtpCrypto;

/// <summary>
/// Class for encypting RTP and RTCP packets to be sent. See RFC 3711.
/// </summary>
public class SrtpEncryptor : SrtpTransformBase
{
    private bool m_FirstRtpPacketProcessed = false;
    private ushort m_SEQ = 0;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="context">CryptoContext greated by the sender of the RTP and SRTP. If null, then
    /// RTP and RTCP packets are not encrypted.</param>
    public SrtpEncryptor(CryptoContext context) : base(context)
    {
    }

    /// <summary>
    /// Encrypts an RTP packet if encryption is being used.
    /// </summary>
    /// <param name="Pckt">Input RTP packet to encrypt.</param>
    /// <returns>Returns an encrypted packet if encryption is being used. Returns the original packet
    /// if encryption is not being used. Returns null if an error is detected and the Error property value
    /// will indicate the type of error..</returns>
    public byte[]? EncryptRtpPacket(byte[] Pckt)
    {
        byte[] EncryptedPckt = null;
        if (Pckt == null || Pckt.Length < RtpPacket.MIN_PACKET_LENGTH)
        {
            Error = SRtpErrorsEnum.InputPacketTooShort;
            return null;
        }

        if (m_Context == null)
            // Not using encryption, just return the input packet.
            return Pckt;

        MasterKeys Mks = m_Context.MasterKeys[0];

        RtpPacket Rp = new RtpPacket(Pckt);
        if (m_FirstRtpPacketProcessed == false)
        {
            m_SEQ = 0;
            m_FirstRtpPacketProcessed = true;
        }

        Rp.SequenceNumber = m_SEQ;
        // Calculate the packet index. See Section 3.3.1 of RFC 3711.
        ulong lROC = m_Context.ROC.Roc;
        ulong PI = (lROC << 16) + m_SEQ;

        bool DeriveSessionKeys = false;
        if (Mks.NumKdrPackets > 0)
        {   // See Section 4.3.1 of RFC 3711.
            if (PI % Mks.NumKdrPackets == 0)
                DeriveSessionKeys = true;
        }

        if (m_Context.RtpSessionKeys.SessionKey == null || DeriveSessionKeys == true)
            // Session keys not derived yet so derive them now.
            SRtpUtils.DeriveRtpSessionKeys(PI, Mks, m_Context);

        // Encrypt the RTP payload.
        byte[] PayloadBytes = Rp.GetPayloadBytes();
        byte[] EncryptedBytes = new byte[PayloadBytes.Length];
        ApplySrtpTransform(Rp, PI, PayloadBytes, EncryptedBytes);

        // Build the authentication tag.
        byte[] AuthTagBytes = SRtpUtils.CalcRtpPacketAuthTag(Rp.GetHeaderBytes(),
            EncryptedBytes, m_Context.ROC.Roc, m_Context.RtpSessionKeys.SessionAuthKey, m_Context.AuthTagLength);

        // Build the encrypted, authenticated RTP packet
        EncryptedPckt = new byte[Rp.HeaderLength + EncryptedBytes.Length + m_Context.AuthTagLength];
        Array.Copy(Pckt, EncryptedPckt, Rp.HeaderLength);
        Array.ConstrainedCopy(EncryptedBytes, 0, EncryptedPckt, Rp.HeaderLength, EncryptedBytes.Length);
        Array.ConstrainedCopy(AuthTagBytes, 0, EncryptedPckt, Rp.HeaderLength + EncryptedBytes.Length,
            AuthTagBytes.Length);

        if (m_SEQ != ushort.MaxValue)
            m_SEQ += 1;
        else
        {   // A SEQ number wrap around occurred. Increment the ROC
            m_SEQ = 0;
            m_Context.ROC.IncrementRoc();
        }

        return EncryptedPckt;
    }

    /// <summary>
    /// Encrypts a RTCP packet to send
    /// </summary>
    /// <param name="Pckt">Input RTCP packet</param>
    /// <returns>Returns the encrypted RTCP packet if encryption is being used. Returns the original
    /// input packet if encryption is not being used. Returns null if an error occurs and the Error property
    /// value will indicate the type of error..</returns>
    public byte[]? EncryptRtcpPacket(byte[] Pckt)
    {
        if (m_Context == null)
            // Not using encryption, just return the input packet.
            return Pckt;

        byte[] EncryptedPckt = null;
        // Index of the start byte of the encrypted part of the input packet. See Figure 2 of RFC 3711.
        int EncStartIdx = RtcpHeader.HeaderLength + 4;
        int EncLen = Pckt.Length - EncStartIdx;
        if (EncLen <= 0)
        {
            Error = SRtpErrorsEnum.InputPacketTooShort;
            return null;
        }

        uint SSRC = RtpUtils.GetDWord(Pckt, RtcpHeader.HeaderLength);

        MasterKeys Mks = m_Context.MasterKeys[m_Context.CurrentRtcpMasterKeyIndex];

        ulong PI = m_Context.SendRtcpIndex;
        bool DeriveSessionKeys = false;
        if (Mks.NumKdrPackets > 0)
        {   // See Section 4.3.1 of RFC 3711.
            if (PI % Mks.NumKdrPackets == 0)
                DeriveSessionKeys = true;
        }

        if (m_Context.RtcpSessionKeys.SessionKey == null || DeriveSessionKeys == true)
            // The session keys have not been derived yet because this is
            // the first packet or its time to derive new session keys based
            // on the packet index.
            SRtpUtils.DeriveRtcpSessionKeys(PI, Mks, m_Context);

        byte[] EncPayloadBytes = new byte[EncLen];
        RtcpHeader Header = new RtcpHeader(Pckt, 0);
        ApplySrtcpTransform(Header, SSRC, PI, Pckt, EncStartIdx, EncLen, EncPayloadBytes);

        int AuthLen = Pckt.Length + 4;
        byte[] AuthBytes = new byte[AuthLen];
        // Copy the RTCP header and SSRC
        Array.Copy(Pckt, AuthBytes, EncStartIdx);
        // Copy in the encrypted payload bytes
        Array.ConstrainedCopy(EncPayloadBytes, 0, AuthBytes, EncStartIdx, EncLen);
        // Set the SRTP Index and encrypted indicator bit E
        RtpUtils.SetDWord(AuthBytes, Pckt.Length, m_Context.SendRtcpIndex | 0x80000000);
        HMACSHA1 hmac = new HMACSHA1(m_Context.RtcpSessionKeys.SessionAuthKey);
        byte[] HashVal = hmac.ComputeHash(AuthBytes);

        int RetPcktLen = Pckt.Length + 4 + m_Context.MkiLength + m_Context.AuthTagLength;
        EncryptedPckt = new byte[RetPcktLen];

        Array.Copy(AuthBytes, EncryptedPckt, AuthBytes.Length);
        Array.ConstrainedCopy(HashVal, 0, EncryptedPckt, AuthBytes.Length, m_Context.AuthTagLength);

        if (m_Context.SendRtcpIndex < CryptoContext.MaxSendRtcpIndex)
            m_Context.SendRtcpIndex += 1;
        else
            m_Context.SendRtcpIndex = 0;      // Wrap around

        return EncryptedPckt;
    }
}
