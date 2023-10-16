/////////////////////////////////////////////////////////////////////////////////////
//  File:   SrtcpUnitTests.cs                                       1 Oct 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

using SipLib.Rtp;
using SipLib.RtpCrypto;

namespace SipLibUnitTests;

/// <summary>
/// Tests all of the crypto suites for SRTCP
/// </summary>
[Trait("Category", "unit")]
public class SrtcpUnitTests
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


    // Test enough packets so that the Packet Index rolls over at least once
    private const int NumRtpPackets = 100000;

    private void DoSrtpCryptoContext(string cryptoContextName)
    {
        CryptoContext EncryptorContext = new CryptoContext(cryptoContextName);

        // Make a copy of the CryptoContext
        CryptoAttribute attr = EncryptorContext.ToCryptoAttribute();
        CryptoContext DecryptorContext = CryptoContext.CreateFromCryptoAttribute(attr);

        SrtpEncryptor encryptor = new SrtpEncryptor(EncryptorContext);
        SrtpDecryptor decryptor = new SrtpDecryptor(DecryptorContext);

        byte[] encryptedPckt;
        byte[] decryptedPckt;

        SenderReport Sr = SenderReportUnitTests.BuildSenderReport();
        byte[] SrBytes;

        for (int i = 0; i < NumRtpPackets; i++)
        {
            SrBytes = Sr.ToByteArray();
            encryptedPckt = encryptor.EncryptRtcpPacket(SrBytes);
            decryptedPckt = decryptor.DecryptRtcpPacket(encryptedPckt);

            Assert.True(SrtpUnitTests.ArraysEqual(SrBytes, decryptedPckt) == true, 
                $"decryptedPckt mismatch at i = {i}");

            // Modify the contents of the SenderReport a little
            Sr.SenderInfo.RtpTimestamp += 1;
        }
    }

}

