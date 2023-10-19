/////////////////////////////////////////////////////////////////////////////////////
//  File:   SrtpUnitTests.cs                                        28 Sep 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLibUnitTests.RtpCrypto;

using SipLib.Rtp;
using SipLib.RtpCrypto;
using System;
using System.Security.Cryptography;

/// <summary>
/// Tests all SRTP crypto suites.
/// </summary>
[Trait("Category", "unit")]
public class SrtpUnitTests
{
    [Fact]
    public void AES_CM_128_HMAC_SHA1_80()
    {
        DoSrtpCryptoContext(CryptoSuites.AES_CM_128_HMAC_SHA1_80);
    }

    [Fact]
    public void AES_CM_128_HMAC_SHA1_32()
    {
        DoSrtpCryptoContext(CryptoSuites.AES_CM_128_HMAC_SHA1_32);
    }

    [Fact]
    public void F8_128_HMAC_SHA1_80()
    {
        DoSrtpCryptoContext(CryptoSuites.F8_128_HMAC_SHA1_80);
    }

    [Fact]
    public void AES_192_CM_HMAC_SHA1_80()
    {
        DoSrtpCryptoContext(CryptoSuites.AES_192_CM_HMAC_SHA1_80);
    }

    [Fact]
    public void AES_192_CM_HMAC_SHA1_32()
    {
        DoSrtpCryptoContext(CryptoSuites.AES_192_CM_HMAC_SHA1_32);
    }

    [Fact]
    public void AES_256_CM_HMAC_SHA1_80()
    {
        DoSrtpCryptoContext(CryptoSuites.AES_256_CM_HMAC_SHA1_80);
    }

    [Fact]
    public void AES_256_CM_HMAC_SHA1_32()
    {
        DoSrtpCryptoContext(CryptoSuites.AES_256_CM_HMAC_SHA1_32);
    }

    // Test enough packets so that the SEQ/Packet Index rolls over at least once
    private const int NumRtpPackets = 100000;

    private static Random Rnd = new Random();

    private void DoSrtpCryptoContext(string cryptoContextName)
    {
        RandomNumberGenerator Rng = RandomNumberGenerator.Create();
        int PayloadLength = 160;
        int RtpPcktLength = RtpPacket.MIN_PACKET_LENGTH + PayloadLength;
        byte[] Pckt = new byte[RtpPcktLength];
        RtpPacket rtpPacket = new RtpPacket(Pckt);
        rtpPacket.SSRC = (uint) Rnd.Next();

        CryptoContext EncryptorContext = new CryptoContext(cryptoContextName);
        // Make a copy of the CryptoContext
        CryptoAttribute attr = EncryptorContext.ToCryptoAttribute();
        CryptoContext DecryptorContext = CryptoContext.CreateFromCryptoAttribute(attr);

        SrtpEncryptor encryptor = new SrtpEncryptor(EncryptorContext);
        SrtpDecryptor decryptor = new SrtpDecryptor(DecryptorContext);

        byte[] encryptedPckt;
        byte[] decryptedPckt;

        for (int i = 0; i < NumRtpPackets; i++)
        {
            Rng.GetBytes(Pckt, RtpPacket.MIN_PACKET_LENGTH, PayloadLength);

            encryptedPckt = encryptor.EncryptRtpPacket(Pckt);
            decryptedPckt = decryptor.DecryptRtpPacket(encryptedPckt);

            Assert.True(ArraysEqual(decryptedPckt, Pckt) == true, $"Decryption failed. i = {i}, " +
                $"Context = {cryptoContextName}, Error = {decryptor.Error}");

            rtpPacket.SequenceNumber += 1;
        }
    }

    public static bool ArraysEqual(byte[] Ary1, byte[] Ary2)
    {
        bool Eq = true;
        if (Ary1.Length != Ary2.Length)
            return false;

        for (int i = 0; (i < Ary1.Length && Eq == true); i++)
        {
            if (Ary1[i] != Ary2[i])
                Eq = false;
        }

        return Eq;
    }

}
