/////////////////////////////////////////////////////////////////////////////////////
//  File:   InLineUnitTests.cs                                      22 Sep 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLibUnitTests;
using SipLib.RtpCrypto;
using System.Security.Cryptography;

[Trait("Category", "unit")]
public class InlineUnitTests
{
    [Fact]
    public void InLineParsing()
    {
        // For the following test cases, the master key length is known to be 16 bytes
        string strInline = "inline:MTIzNDU2Nzg5QUJDREUwMTIzNDU2Nzg5QUJjZGVm|2^20|1:4";
        InlineParams inlineParams = InlineParams.Parse(strInline, 16);
        Assert.True(inlineParams != null, "strInline did not parse correctly");

        Assert.True(inlineParams.MKI == 1, "MKI for strInline is wrong");
        Assert.True(inlineParams.MKI_Length == 4, "MKI_Length for strInline is wrong");

        string strInlineResult = inlineParams.ToString();
        Assert.True(strInlineResult == strInline, "strInlineResult != strInline");

        string strInline2 = "inline:MTIzNDU2Nzg5QUJDREUwMTIzNDU2Nzg5QUJjZGVm|2^20";
        inlineParams = InlineParams.Parse(strInline2, 16);
        strInlineResult = inlineParams.ToString();
        Assert.True(strInlineResult == strInline2, "strInlineResult != strInline2");

        string strInline3 = "inline:MTIzNDU2Nzg5QUJDREUwMTIzNDU2Nzg5QUJjZGVm|1:4";
        inlineParams = InlineParams.Parse(strInline3, 16);
        strInlineResult = inlineParams.ToString();
        Assert.True(strInlineResult == strInline3, "strInlineResult != strInline3");
        Assert.True(inlineParams.Lifetime == 0, "Lifetime is not 0 for strInline3");

        string strInline4 = "inline:MTIzNDU2Nzg5QUJDREUwMTIzNDU2Nzg5QUJjZGVm";
        inlineParams = InlineParams.Parse(strInline4, 16);
        strInlineResult = inlineParams.ToString();
        Assert.True(strInlineResult == strInline4, "strInlineResult != strInline4");
        Assert.True(inlineParams.MKI == 0, "MKI is not 0 for strInline4");
        Assert.True(inlineParams.MKI_Length == 0, "MKI_Length is not 0 for strInline4");

        // Test a failure case string too short.
        string strInline5 = "inline:MTIzNDU2Nzg5QUJDREUwMTIzNDU2Nzg5QUJjVm";
        inlineParams = InlineParams.Parse(strInline5, 16);
        Assert.True(inlineParams == null, "inlineParams is not null");
    }

    [Fact]
    public void InlineForAes192()
    {
        RandomNumberGenerator rng = RandomNumberGenerator.Create();
        int KeyLength = 24; // 24 bytes = 192 bits
        byte[] MasterKey = new byte[KeyLength];
        rng.GetBytes(MasterKey);
        byte[] MasterSalt = new byte[14];
        rng.GetBytes(MasterSalt);

        InlineParams inline = new InlineParams();
        inline.MasterKey = MasterKey;
        inline.MasterSalt = MasterSalt;
        string str = inline.ToString();
        InlineParams result = InlineParams.Parse(str, KeyLength);
        Assert.True(result.MasterKey.Length == KeyLength, "The MasterKey length is wrong");
        Assert.True(MasterSalt.Length == 14, "The MasterSalt length is wrong");
    }

    [Fact]
    public void InlineForAes256()
    {
        RandomNumberGenerator rng = RandomNumberGenerator.Create();
        int KeyLength = 32; // 32 bytes = 256 bits
        byte[] MasterKey = new byte[KeyLength];
        rng.GetBytes(MasterKey);
        byte[] MasterSalt = new byte[14];
        rng.GetBytes(MasterSalt);

        InlineParams inline = new InlineParams();
        inline.MasterKey = MasterKey;
        inline.MasterSalt = MasterSalt;
        string str = inline.ToString();
        InlineParams result = InlineParams.Parse(str, KeyLength);
        Assert.True(result.MasterKey.Length == KeyLength, "The MasterKey length is wrong");
        Assert.True(MasterSalt.Length == 14, "The MasterSalt length is wrong");
    }

}
