/////////////////////////////////////////////////////////////////////////////////////
//  File:   SrtpKeyDerivationUnitTests.cs                           22 Sep 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLibUnitTests.RtpCrypto;
using SipLib.RtpCrypto;

/// <summary>
/// Tests the SRTP key derivation algorithms using AES-128 (RFC 3711), AES-256 and AES-192 (RFC 6811)
/// </summary>
[Trait("Category", "unit")]
public class SrtpKeyDerivationUnitTests
{
    // See Appendix B.3 of RFC 3711
    [Fact]
    public void Aes128SessionKeyDerivation()
    {
        byte[] MasterKey = new byte[16]
        {   0xE1, 0xF9, 0x7A, 0x0D, 0x3E, 0x01, 0x8B, 0xE0,
            0xD6, 0x4F, 0xA3, 0x2C, 0x06, 0xDE, 0x41, 0x39
        };

        byte[] MasterSalt = new byte[14]
        {   0x0E, 0xC6, 0x75, 0xAD, 0x49, 0x8A, 0xFE, 0xEB,
            0xB6, 0x96, 0x0B, 0x3A, 0xAB, 0xE6
        };

        byte[] SessionKeyAnswer = new byte[]
        {
            0xC6, 0x1E, 0x7A, 0x93, 0x74, 0x4F, 0x39, 0xEE,
            0x10, 0x73, 0x4A, 0xFE, 0x3F, 0xF7, 0xA0, 0x87
        };

        byte[] SessionSaltAnswer = new byte[]
        {
            0x30, 0xCB, 0xBC, 0x08, 0x86, 0x3D, 0x8C, 0x85,
            0xD4, 0x9D, 0xB3, 0x4A, 0x9A, 0xE1
        };

        // Only need 10 bytes (80 bits) for the authentication session key.
        byte[] SessionAuthAnswer = new byte[]
        {
            0xCE, 0xBE, 0x32, 0x1F, 0x6F, 0xF7, 0x71, 0x6B,
            0x6F, 0xD4
        };

        DoSessionKeyGeneration(MasterKey, MasterSalt, SessionKeyAnswer, SessionSaltAnswer, SessionAuthAnswer);
    }

    // See Section 7.2 of RFC 6188
    [Fact]
    public void Aes256SessionKeyGeneration()
    {
        byte[] MasterKey = new byte[32]
        {
            0xf0, 0xf0, 0x49, 0x14, 0xb5, 0x13, 0xf2, 0x76, 0x3a, 0x1b, 0x1f, 0xa1, 0x30, 0xf1, 0x0e, 0x29,
            0x98, 0xf6, 0xf6, 0xe4, 0x3e, 0x43, 0x09, 0xd1, 0xe6, 0x22, 0xa0, 0xe3, 0x32, 0xb9, 0xf1, 0xb6
        };

        byte[] MasterSalt = new byte[14]
        {
            0x3b, 0x04, 0x80, 0x3d, 0xe5, 0x1e, 0xe7, 0xc9, 0x64, 0x23, 0xab, 0x5b, 0x78, 0xd2
        };

        byte[] SessionKeyAnswer = new byte[]
        {
            0x5b, 0xa1, 0x06, 0x4e, 0x30, 0xec, 0x51, 0x61, 0x3c, 0xad, 0x92, 0x6c, 0x5a, 0x28, 0xef, 0x73,
            0x1e, 0xc7, 0xfb, 0x39, 0x7f, 0x70, 0xa9, 0x60, 0x65, 0x3c, 0xaf, 0x06, 0x55, 0x4c, 0xd8, 0xc4
        };

        byte[] SessionSaltAnswer = new byte[]
        {
            0xfa, 0x31, 0x79, 0x16, 0x85, 0xca, 0x44, 0x4a, 0x9e, 0x07, 0xc6, 0xc6, 0x4e, 0x93
        };

        byte[] SessionAuthAnswer = new byte[]
        {
            0xfd, 0x9c, 0x32, 0xd3, 0x9e, 0xd5, 0xfb, 0xb5, 0xa9, 0xdc, 0x96, 0xb3, 0x08, 0x18, 0x45, 0x4d,
            0x13, 0x13, 0xdc, 0x05
        };

        DoSessionKeyGeneration(MasterKey, MasterSalt, SessionKeyAnswer, SessionSaltAnswer, SessionAuthAnswer);
    }

    // See Section 7.4 of RFC 6188
    [Fact]
    public void Aes192SessionKeyGeneration()
    {
        byte[] MasterKey = new byte[]
        {
            0x73, 0xed, 0xc6, 0x6c, 0x4f, 0xa1, 0x57, 0x76, 0xfb, 0x57, 0xf9, 0x50, 0x5c, 0x17, 0x13, 0x65,
            0x50, 0xff, 0xda, 0x71, 0xf3, 0xe8, 0xe5, 0xf1
        };

        byte[] MasterSalt = new byte[]
        {
            0xc8, 0x52, 0x2f, 0x3a, 0xcd, 0x4c, 0xe8, 0x6d, 0x5a, 0xdd, 0x78, 0xed, 0xbb, 0x11
        };

        byte[] SessionKeyAnswer = new byte[]
        {
            0x31, 0x87, 0x47, 0x36, 0xa8, 0xf1, 0x14, 0x38, 0x70, 0xc2, 0x6e, 0x48, 0x57, 0xd8, 0xa5, 0xb2,
            0xc4, 0xa3, 0x54, 0x40, 0x7f, 0xaa, 0xda, 0xbb
        };

        byte[] SessionSaltAnswer = new byte[]
        {
            0x23, 0x72, 0xb8, 0x2d, 0x63, 0x9b, 0x6d, 0x85, 0x03, 0xa4, 0x7a, 0xdc, 0x0a, 0x6c
        };

        byte[] SessionAuthAnswer = new byte[]
        {
            0x35, 0x5b, 0x10, 0x97, 0x3c, 0xd9, 0x5b, 0x9e, 0xac, 0xf4, 0x06, 0x1c, 0x7e, 0x1a, 0x71, 0x51,
            0xe7, 0xcf, 0xbf, 0xcb
        };

        DoSessionKeyGeneration(MasterKey, MasterSalt, SessionKeyAnswer, SessionSaltAnswer, SessionAuthAnswer);
    }

    private void DoSessionKeyGeneration(byte[] MasterKey, byte[] MasterSalt, byte[] SessionKeyAnswer,
        byte[] SessionSaltAnswer, byte[] SessionAuthAnswer)
    {
        byte[] KeyZeroInput = new byte[MasterKey.Length];
        byte[] SaltZeroInput = new byte[14];
        byte[] AuthZeroInput = new byte[SessionAuthAnswer.Length];

        byte[] SessionKey = SRtpUtils.DeriveSrtpSessionKey(0, 0, SrtpLabelItem.SrtpSessionKey, MasterSalt,
            MasterKey, KeyZeroInput);
        Assert.True(ArraysEqual(SessionKey, SessionKeyAnswer) == true, "SessionKey != SessionKeyAnswer");

        byte[] SessionSalt = SRtpUtils.DeriveSrtpSessionKey(0, 0, SrtpLabelItem.SrtpSessionSalt, MasterSalt,
            MasterKey, SaltZeroInput);
        Assert.True(ArraysEqual(SessionSalt, SessionSaltAnswer) == true, "SessionSalt != SessionSaltAnswer");

        byte[] SessionAuthKey = SRtpUtils.DeriveSrtpSessionKey(0, 0, SrtpLabelItem.SrtpAuthKey, MasterSalt,
            MasterKey, AuthZeroInput);
        Assert.True(ArraysEqual(SessionAuthKey, SessionAuthAnswer), "SessionAuthKey != SessionAuthAnswer");
    }

    private bool ArraysEqual(byte[] Ary1, byte[] Ary2)
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
