//////////////////////////////////////////////////////////////////////////////////////
//  File: SdpConnectionDataUnitTests.cs                             16 Nov 22 PHR  
//////////////////////////////////////////////////////////////////////////////////////

using System.Net.Sockets;

using SipLib.Sdp;

namespace SipLibUnitTests
{
    [Trait("Category", "unit")]
    public class SdpConnectionDataUnitTests
    {
        public SdpConnectionDataUnitTests(Xunit.Abstractions.ITestOutputHelper output)
        {
        }

        [Fact]
        public void TestBasicIPv4Parsing()
        {
            ConnectionData Cd = ConnectionData.ParseConnectionData("IN IP4 10.2.36.42");
            Assert.NotNull(Cd);
            Assert.True(Cd.Address.AddressFamily == AddressFamily.InterNetwork);
            Assert.True(Cd.Address.ToString() == "10.2.36.42");
            Assert.True(Cd.NetworkType == "IN", "The network type is incorrect");
            Assert.True(Cd.AddressType == "IP4", "The address type is incorrect");
        }

        [Fact]
        public void TestBasicIPv6Parsing()
        {
            ConnectionData Cd = ConnectionData.ParseConnectionData("IN IP6 ff15::101");
            Assert.NotNull(Cd);
            Assert.True(Cd.Address.AddressFamily == AddressFamily.InterNetworkV6);
            string strIPv6 = Cd.Address.ToString();
            Assert.True(Cd.Address.ToString() == "ff15::101");
            Assert.True(Cd.NetworkType == "IN", "The network type is incorrect");
            Assert.True(Cd.AddressType == "IP6", "The address type is incorrect");
        }

        [Fact]
        public void TestIPv4ParsingWithTTLAndAddressCount()
        {
            ConnectionData Cd = ConnectionData.ParseConnectionData("IN IP4 10.2.36.42/128/3");
            Assert.NotNull(Cd);
            Assert.True(Cd.Address.AddressFamily == AddressFamily.InterNetwork);
            Assert.True(Cd.Address.ToString() == "10.2.36.42");
            Assert.True(Cd.NetworkType == "IN", "The network type is incorrect");
            Assert.True(Cd.AddressType == "IP4", "The address type is incorrect");
            Assert.True(Cd.TTL == 128, "The IPv4 TTL is incorrect");
            Assert.True(Cd.AddressCount == 3, "The IPv4 address count is incorrect");
        }

        [Fact]
        public void TestIPv6ParsingWithAddressCount()
        {
            ConnectionData Cd = ConnectionData.ParseConnectionData("IN IP6 ff15::101/3");
            Assert.NotNull(Cd);
            Assert.True(Cd.Address.AddressFamily == AddressFamily.InterNetworkV6);
            string strIPv6 = Cd.Address.ToString();
            Assert.True(Cd.Address.ToString() == "ff15::101");
            Assert.True(Cd.NetworkType == "IN", "The network type is incorrect");
            Assert.True(Cd.AddressType == "IP6", "The address type is incorrect");
            Assert.True(Cd.AddressCount == 3, "The IPv6 address count is incorrect");
        }

        [Fact]
        public void TestCreateCopy()
        {
            ConnectionData Cd1 = ConnectionData.ParseConnectionData("IN IP4 10.2.36.42/128/3");
            ConnectionData Cd2 = Cd1.CreateCopy();
            Assert.True(Cd1.NetworkType == Cd2.NetworkType, "The network types do not match");
            Assert.True(Cd1.AddressType == Cd2.AddressType, "The address types do not match");
            Assert.True(Cd1.Address.ToString() == Cd2.Address.ToString(), 
                "The IP addresses do not match");
            Assert.True(Cd1.TTL == Cd2.TTL, "The TTL values do not match");
            Assert.True(Cd1.AddressCount == Cd2.AddressCount, "The address counts do not match");
        }

        [Fact]
        public void TestInvalidIPv4Address()
        {
            Assert.Throws<ArgumentException>(() => ConnectionData.ParseConnectionData("IN IP4 abc"));
        }

        [Fact]
        public void TestInvalidIPv6Address()
        {
            Assert.Throws<ArgumentException>(() => ConnectionData.ParseConnectionData("IN IP6 abc"));
        }

    }
}
