/////////////////////////////////////////////////////////////////////////////////////
//  File:   ByteRangeUnitTests.cs                                   23 Jul 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLibUnitTests.Msrp;
using SipLib.Msrp;

[Trait("Category", "unit")]
public class ByteRangeUnitTests
{
    [Fact]
    public void ValidIntegerFormat()
    {
        string strHdrVal = "1-25/25";
        ByteRangeHeader Brh = ByteRangeHeader.ParseByteRangeHeader(strHdrVal);
        Assert.NotNull(Brh);

        Assert.True(Brh.Start == 1, "The Start value is wrong");
        Assert.True(Brh.End == 25, "The End value is wrong");
        Assert.True(Brh.Total == 25, "The Total value is wrong");
    }

    [Fact]
    public void ValidEndWildCard()
    {
        string strHdrVal = "1-*/25";
        ByteRangeHeader Brh = ByteRangeHeader.ParseByteRangeHeader(strHdrVal);
        Assert.NotNull(Brh);
        Assert.True(Brh.Start == 1, "The Start value is wrong");
        Assert.True(Brh.End == -1, "The End value is wrong");
        Assert.True(Brh.Total == 25, "The Total value is wrong");
    }

    [Fact]
    public void ValidEndAndTotalWildCards()
    {
        string strHdrVal = "1-*/*";
        ByteRangeHeader Brh = ByteRangeHeader.ParseByteRangeHeader(strHdrVal);
        Assert.NotNull(Brh);
        Assert.True(Brh.Start == 1, "The Start value is wrong");
        Assert.True(Brh.End == -1, "The End value is wrong");
        Assert.True(Brh.Total == -1, "The Total value is wrong");
    }

    [Fact]
    public void InvalidStartValue()
    {
        string strHdrVal = "*-*/*";
        ByteRangeHeader Brh = ByteRangeHeader.ParseByteRangeHeader(strHdrVal);
        Assert.Null(Brh);
    }

    [Fact]
    public void InvalidFormat1()
    {
        string strHdrVal = "*/*";
        ByteRangeHeader Brh = ByteRangeHeader.ParseByteRangeHeader(strHdrVal);
        Assert.Null(Brh);
    }

    [Fact]
    public void InvalidFormat2()
    {
        string strHdrVal = "1-*";
        ByteRangeHeader Brh = ByteRangeHeader.ParseByteRangeHeader(strHdrVal);
        Assert.Null(Brh);
    }

    [Fact]
    public void ToStingAllIntegers()
    {
        ByteRangeHeader Brh = new ByteRangeHeader() { Start = 1, End = 6, Total = 6 };
        Assert.True(Brh.ToString() == "1-6/6", "ToString() failed");
    }

    [Fact]
    public void ToStringEndWildCard()
    {
        ByteRangeHeader Brh = new ByteRangeHeader() { Start = 1, End = -1, Total = 6 };
        Assert.True(Brh.ToString() == "1-*/6", "ToString() failed");
    }

    [Fact]
    public void ToStringEndAndTotalWildCards()
    {
        ByteRangeHeader Brh = new ByteRangeHeader() { Start = 1, End = -1, Total = -1 };
        Assert.True(Brh.ToString() == "1-*/*", "ToString() failed");
    }
}
