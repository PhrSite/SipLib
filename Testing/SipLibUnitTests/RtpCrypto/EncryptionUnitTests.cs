/////////////////////////////////////////////////////////////////////////////////////
//  File:   EncryptionUnitTests.cs                                  22 Sep 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

using SipLib.RtpCrypto;
using System.Security.Cryptography;

namespace SipLibUnitTests;

[Trait("Category", "unit")]
public class EncryptionUnitTests
{
    [Fact]
    public void Aes256Encryption()
    {
        byte[] key = new byte[32]; // 256-bit key
        byte[] iv = new byte[16];  // 128-bit IV

        int DataLength = 160;
        byte[] Input = new byte[DataLength];
        for (int i = 0; i < DataLength; i++)
            Input[i] = (byte)i;

        byte[] Output = new byte[DataLength];
        byte[] Decrypted = new byte[DataLength];

        RandomNumberGenerator rng = RandomNumberGenerator.Create();
        rng.GetBytes(key);
        rng.GetBytes(iv);
        rng.GetBytes(Input);

        AesFunctions.AesCounterModeTransform(key, iv, Input, Output);
        AesFunctions.AesCounterModeTransform(key, iv, Output, Decrypted);

        // Compare the results
        for (int i = 0; i < DataLength; i++)
        {
            Assert.True(Decrypted[i] == Input[i], $"Mismatch at index = {i}");
        }
    }

    // See Appendix B.1 of RFC 3711.
    [Fact]
    public void TestAesF8Mode()
    {
        byte[] F8key = new byte[]
        {   0x23, 0x48, 0x29, 0x00, 0x84, 0x67, 0xbe, 0x18,
            0x6c, 0x3d, 0xe1, 0x4a, 0xae, 0x72, 0xd6, 0x2c
        };

        byte[] F8salt = new byte[]
        {   0x32, 0xf2, 0x87, 0x0d };

        byte[] F8IV = new byte[]
        {   0x00, 0x6e, 0x5c, 0xba, 0x50, 0x68, 0x1d, 0xe5,
            0x5c, 0x62, 0x15, 0x99, 0xd4, 0x62, 0x56, 0x4a
        };

        byte[] RtpPayload = new byte[]
        {   0x70, 0x73, 0x65, 0x75, 0x64, 0x6f, 0x72, 0x61,
            0x6e, 0x64, 0x6f, 0x6d, 0x6e, 0x65, 0x73, 0x73,
            0x20, 0x69, 0x73, 0x20, 0x74, 0x68, 0x65, 0x20,
            0x6e, 0x65, 0x78, 0x74, 0x20, 0x62, 0x65, 0x73,
            0x74, 0x20, 0x74, 0x68, 0x69, 0x6e, 0x67
        };

        byte[] F8EncryptedAnswer = new byte[]
        {
            0x01, 0x9c, 0xe7, 0xa2, 0x6e, 0x78, 0x54, 0x01,
            0x4a, 0x63, 0x66, 0xaa, 0x95, 0xd4, 0xee, 0xfd,
            0x1a, 0xd4, 0x17, 0x2a, 0x14, 0xf9, 0xfa, 0xf4,
            0x55, 0xb7, 0xf1, 0xd4, 0xb6, 0x2b, 0xd0, 0x8f,
            0x56, 0x2c, 0x0e, 0xef, 0x7c, 0x48, 0x02
        };

        byte[] Output = new byte[RtpPayload.Length];
        AesFunctions.AesF8ModeTransform(F8key, F8salt, F8IV, RtpPayload, Output);
        Assert.True(ArraysEqual(Output, F8EncryptedAnswer) == true, "Output != F8EncrpytedAnswer");

        byte[] Decrypted = new byte[RtpPayload.Length];
        AesFunctions.AesF8ModeTransform(F8key, F8salt, F8IV, Output, Decrypted);
        Assert.True(ArraysEqual(RtpPayload, Decrypted) == true, "Decrypted != RtpPayload");
    }

    private static bool ArraysEqual(byte[] Ary1, byte[] Ary2)
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
