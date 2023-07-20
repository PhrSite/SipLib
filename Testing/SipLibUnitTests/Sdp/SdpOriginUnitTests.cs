//////////////////////////////////////////////////////////////////////////////////////
//  File:   SdpOriginUnitTests.cs                                   20 Nov 22 PHR
//////////////////////////////////////////////////////////////////////////////////////

using SipLib.Sdp;
using System.Net;

namespace SipLibUnitTests
{
    [Trait("Category", "unit")]
    public class SdpOriginUnitTests
    {
        public SdpOriginUnitTests(Xunit.Abstractions.ITestOutputHelper output)
        {
        }

        [Fact]
        public void TestBasicParsing()
        {
            string strOrigin = "jdoe 2890844526 2890842807 IN IP4 10.47.16.5";
            Origin Or = Origin.ParseOrigin(strOrigin);
            Assert.NotNull(Or);
            Assert.True(Or.UserName == "jdoe", "The UserName is incorrect");
            Assert.True(Or.NetworkType == "IN", "The NetworkType is incorrect");
            Assert.True(Or.AddressType == "IP4", "The AddressType is incorrect");
            Assert.True(Or.Address == "10.47.16.5", "The Address is incorrect");
        }

        [Fact]
        public void TestInvalidOrigin()
        {
            string strOrigin = "jdoe 2890844526 abcd IN IP4 10.47.16.5";
            Assert.Throws<ArgumentException>(() => Origin.ParseOrigin(strOrigin));
        }

        [Fact]
        public void TestToString()
        {
            string strOrigin = "jdoe 2890844526 2890842807 IN IP4 10.47.16.5";
            Origin Or = Origin.ParseOrigin(strOrigin);
            string str = Or.ToString();
            Assert.True(str == "o=" + strOrigin + "\r\n", "ToString() is incorrect");
        }

        [Fact]
        public void TestConstructor()
        {
            Origin Or = new Origin("jdoe", IPAddress.Parse("10.47.16.5"));
            Assert.True(Or.UserName == "jdoe", "The UserName is incorrect");
            Assert.True(string.IsNullOrEmpty(Or.SessionId) == false, "The SessionId is " +
                "null or empty");
            Assert.True(Or.Version == 1, "The Version is incorrect");
            Assert.True(Or.NetworkType == "IN", "The NetworkType is incorrect");
            Assert.True(Or.AddressType == "IP4", "The AddressType is incorrect");
            Assert.True(Or.Address == "10.47.16.5", "The Address is incorrect");
        }
    }
}
