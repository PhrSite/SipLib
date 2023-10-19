#region License
//-----------------------------------------------------------------------------
// Author(s):
// Aaron Clauson
// 
// History:
// 
//
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//-----------------------------------------------------------------------------
#endregion

/////////////////////////////////////////////////////////////////////////////////////
//	Revised:	10 Nov 22 PHR -- Initial version.
/////////////////////////////////////////////////////////////////////////////////////

using System.Net;
using SipLib.Core;

namespace SipLibUnitTests.Core
{
    [Trait("Category", "unit")]
    public class SIPEndPointTest
    {
        public SIPEndPointTest(Xunit.Abstractions.ITestOutputHelper output)
        {
        }

        [Fact]
        public void AllFieldsParseTest()
        {
            string sipEndPointStr = "sips:10.0.0.100:5060;lr;transport=tcp;";
            SIPEndPoint sipEndPoint = SIPEndPoint.ParseSIPEndPoint(sipEndPointStr);

            Assert.True(sipEndPoint.Protocol == SIPProtocolsEnum.tls, "The SIPEndPoint protocol was incorrectly parsed.");
            Assert.True(sipEndPoint.Address.ToString() == "10.0.0.100", "The SIPEndPoint IP address was incorrectly parsed.");
            Assert.True(sipEndPoint.Port == 5060, "The SIPEndPoint port was incorrectly parsed.");

            Assert.True(true, "True was false.");
        }

        [Fact]
        public void HostOnlyParseTest()
        {
            string sipEndPointStr = "10.0.0.100";
            SIPEndPoint sipEndPoint = SIPEndPoint.ParseSIPEndPoint(sipEndPointStr);

            Assert.True(sipEndPoint.Protocol == SIPProtocolsEnum.udp, "The SIPEndPoint protocol was incorrectly parsed.");
            Assert.True(sipEndPoint.Address.ToString() == "10.0.0.100", "The SIPEndPoint IP address was incorrectly parsed.");
            Assert.True(sipEndPoint.Port == 5060, "The SIPEndPoint port was incorrectly parsed.");
        }

        [Fact]
        public void HostAndSchemeParseTest()
        {
            string sipEndPointStr = "sip:10.0.0.100";
            SIPEndPoint sipEndPoint = SIPEndPoint.ParseSIPEndPoint(sipEndPointStr);

            Assert.True(sipEndPoint.Protocol == SIPProtocolsEnum.udp, "The SIPEndPoint protocol was incorrectly parsed.");
            Assert.True(sipEndPoint.Address.ToString() == "10.0.0.100", "The SIPEndPoint IP address was incorrectly parsed.");
            Assert.True(sipEndPoint.Port == 5060, "The SIPEndPoint port was incorrectly parsed.");

            Assert.True(true, "True was false.");
        }

        [Fact]
        public void HostAndPortParseTest()
        {
            string sipEndPointStr = "10.0.0.100:5065";
            SIPEndPoint sipEndPoint = SIPEndPoint.ParseSIPEndPoint(sipEndPointStr);

            Assert.True(sipEndPoint.Protocol == SIPProtocolsEnum.udp, "The SIPEndPoint protocol was incorrectly parsed.");
            Assert.True(sipEndPoint.Address.ToString() == "10.0.0.100", "The SIPEndPoint IP address was incorrectly parsed.");
            Assert.True(sipEndPoint.Port == 5065, "The SIPEndPoint port was incorrectly parsed.");
        }

        [Fact]
        public void HostAndTransportParseTest()
        {
            string sipEndPointStr = "10.0.0.100;transport=tcp";
            SIPEndPoint sipEndPoint = SIPEndPoint.ParseSIPEndPoint(sipEndPointStr);

            Assert.True(sipEndPoint.Protocol == SIPProtocolsEnum.tcp, "The SIPEndPoint protocol was incorrectly parsed.");
            Assert.True(sipEndPoint.Address.ToString() == "10.0.0.100", "The SIPEndPoint IP address was incorrectly parsed.");
            Assert.True(sipEndPoint.Port == 5060, "The SIPEndPoint port was incorrectly parsed.");

            Assert.True(true, "True was false.");
        }

        [Fact]
        public void SchemeHostPortParseTest()
        {
            string sipEndPointStr = "sips:10.0.0.100:5063";
            SIPEndPoint sipEndPoint = SIPEndPoint.ParseSIPEndPoint(sipEndPointStr);

            Assert.True(sipEndPoint.Protocol == SIPProtocolsEnum.tls, "The SIPEndPoint protocol was incorrectly parsed.");
            Assert.True(sipEndPoint.Address.ToString() == "10.0.0.100", "The SIPEndPoint IP address was incorrectly parsed.");
            Assert.True(sipEndPoint.Port == 5063, "The SIPEndPoint port was incorrectly parsed.");
        }

        [Fact]
        public void SchemeHostTransportParseTest()
        {
            string sipEndPointStr = "sip:10.0.0.100:5063;lr;tag=123;transport=tcp;tag2=abcd";
            SIPEndPoint sipEndPoint = SIPEndPoint.ParseSIPEndPoint(sipEndPointStr);

            Assert.True(sipEndPoint.Protocol == SIPProtocolsEnum.tcp, "The SIPEndPoint protocol was incorrectly parsed.");
            Assert.True(sipEndPoint.Address.ToString() == "10.0.0.100", "The SIPEndPoint IP address was incorrectly parsed.");
            Assert.True(sipEndPoint.Port == 5063, "The SIPEndPoint port was incorrectly parsed.");
        }

        [Fact]
        public void EqualityTestNoPostHostTest()
        {
            SIPEndPoint sipEP1 = SIPEndPoint.ParseSIPEndPoint("10.0.0.100");
            SIPEndPoint sipEP2 = SIPEndPoint.ParseSIPEndPoint("10.0.0.100:5060");

            Assert.True(sipEP1 == sipEP2, "The SIP end points should have been detected as equal.");
        }

        [Fact]
        public void EqualityTestTLSHostTest()
        {
            SIPEndPoint sipEP1 = SIPEndPoint.ParseSIPEndPoint("sips:10.0.0.100");
            SIPEndPoint sipEP2 = SIPEndPoint.ParseSIPEndPoint("10.0.0.100:5061;transport=tls");

            Assert.True(sipEP1 == sipEP2, "The SIP end points should have been detected as equal.");
        }

        [Fact]
        public void EqualityTestRouteTest()
        {
            SIPEndPoint sipEP1 = SIPEndPoint.ParseSIPEndPoint("sip:10.0.0.100;lr");
            SIPEndPoint sipEP2 = new SIPEndPoint(SIPProtocolsEnum.udp, new IPEndPoint(IPAddress.Parse
                ("10.0.0.100"), 5060));
            Assert.True(sipEP1 == sipEP2, "The SIP end points should have been detected as equal.");
        }

        /// <summary>
        /// Tests that a SIP end point with an IPv6 loopback address gets represented as a string correctly.
        /// </summary>
        [Fact]
        public void IPv6LoopbackToStringTest()
        {
            SIPEndPoint sipEndPoint = new SIPEndPoint(SIPProtocolsEnum.udp, new IPEndPoint(IPAddress.
                IPv6Loopback, 0));

            Assert.True(sipEndPoint.ToString() == "udp:[::1]:5060", "The SIP end point string " + 
                "representation was not correct.");
        }

        /// <summary>
        /// Tests that a SIP end point with an IPv6 loopback address gets parsed correctly.
        /// </summary>
        [Fact]
        public void IPv6LoopbackAndSchemeParseTest()
        {
            string sipEndPointStr = "udp:[::1]";
            SIPEndPoint sipEndPoint = SIPEndPoint.ParseSIPEndPoint(sipEndPointStr);

            Assert.True(sipEndPoint.Protocol == SIPProtocolsEnum.udp, "The SIPEndPoint protocol was " +
                "incorrectly parsed.");
            Assert.True(sipEndPoint.Address.ToString() == "::1", "The SIPEndPoint IP address was " + 
                "incorrectly parsed.");
            Assert.True(sipEndPoint.Port == 5060, "The SIPEndPoint port was incorrectly parsed.");
        }

        /// <summary>
        /// Tests that a SIP end point with an IPv6 loopback address and port gets parsed correctly.
        /// </summary>
        [Fact]
        public void IPv6LoopbackAndPortParseTest()
        {
            string sipEndPointStr = "tcp:[::1]:6060";
            SIPEndPoint sipEndPoint = SIPEndPoint.ParseSIPEndPoint(sipEndPointStr);

            Assert.True(sipEndPoint.Protocol == SIPProtocolsEnum.tcp, "The SIPEndPoint protocol was " + 
                "incorrectly parsed.");
            Assert.True(sipEndPoint.Address.ToString() == "::1", "The SIPEndPoint IP address was " + 
                "incorrectly parsed.");
            Assert.True(sipEndPoint.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6, 
                "The SIPEndPoint IP address family was incorrectly parsed.");
            Assert.True(sipEndPoint.Port == 6060, "The SIPEndPoint port was incorrectly parsed.");
        }

        /// <summary>
        /// Tests that a SIP end point with an IPv6 loopback address and scheme gets parsed correctly.
        /// </summary>
        [Fact]
        public void IPv6LoopbackWithScehemeParseTest()
        {
            string sipEndPointStr = "sip:[::1]:6060";
            SIPEndPoint sipEndPoint = SIPEndPoint.ParseSIPEndPoint(sipEndPointStr);

            Assert.True(sipEndPoint.Protocol == SIPProtocolsEnum.udp, "The SIPEndPoint protocol was " + 
                "incorrectly parsed.");
            Assert.True(sipEndPoint.Address.ToString() == "::1", "The SIPEndPoint IP address was " + 
                "incorrectly parsed.");
            Assert.True(sipEndPoint.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6, 
                "The SIPEndPoint IP address family was incorrectly parsed.");
            Assert.True(sipEndPoint.Port == 6060, "The SIPEndPoint port was incorrectly parsed.");
        }

        /// <summary>
        /// Tests that a SIP end point for a web socket with an IPv6 loopback address and port gets 
        /// parsed correctly.
        /// </summary>
        [Fact]
        public void WebSocketLoopbackAndPortParseTest()
        {
            string sipEndPointStr = "ws:[::1]:6060";
            SIPEndPoint sipEndPoint = SIPEndPoint.ParseSIPEndPoint(sipEndPointStr);

            Assert.True(sipEndPoint.Protocol == SIPProtocolsEnum.ws, "The SIPEndPoint protocol was " +
                "incorrectly parsed.");
            Assert.True(sipEndPoint.Address.ToString() == "::1", "The SIPEndPoint IP address was " +
                "incorrectly parsed.");
            Assert.True(sipEndPoint.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6, 
                "The SIPEndPoint IP address family was incorrectly parsed.");
            Assert.True(sipEndPoint.Port == 6060, "The SIPEndPoint port was incorrectly parsed.");
        }

        /// <summary>
        /// Tests that a SIP end point for a secure web socket gets parsed correctly.
        /// </summary>
        [Fact]
        public void SecureWebSocketLoopbackAndPortParseTest()
        {
            string sipEndPointStr = "wss:[fe80::54a9:d238:b2ee:ceb]:7060";
            SIPEndPoint sipEndPoint = SIPEndPoint.ParseSIPEndPoint(sipEndPointStr);

            Assert.True(sipEndPoint.Protocol == SIPProtocolsEnum.wss, "The SIPEndPoint protocol was " + 
                "incorrectly parsed.");
            Assert.True(sipEndPoint.Address.ToString() == "fe80::54a9:d238:b2ee:ceb", "The SIPEndPoint " + 
                "IP address was incorrectly parsed.");
            Assert.True(sipEndPoint.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6, 
                "The SIPEndPoint IP address family was incorrectly parsed.");
            Assert.True(sipEndPoint.Port == 7060, "The SIPEndPoint port was incorrectly parsed.");
        }

        /// <summary>
        /// Tests that a SIP end point an IPV6 address and a connection id gets parsed correctly.
        /// </summary>
        [Fact]
        public void IPv6WithConnectionIDParseTest()
        {
            string sipEndPointStr = "udp:[::1];xid=1234567";
            SIPEndPoint sipEndPoint = SIPEndPoint.ParseSIPEndPoint(sipEndPointStr);

            Assert.True(sipEndPoint.Protocol == SIPProtocolsEnum.udp, "The SIPEndPoint protocol was " + 
                "incorrectly parsed.");
            Assert.True(sipEndPoint.Address.ToString() == "::1", "The SIPEndPoint IP address was " + 
                "incorrectly parsed.");
            Assert.True(sipEndPoint.Port == 5060, "The SIPEndPoint port was incorrectly parsed.");
            Assert.True(sipEndPoint.ConnectionID == "1234567", "The SIPEndPoint connection ID was " + 
                "incorrectly parsed.");
        }

        /// <summary>
        /// Tests that a SIP end point an IPV6 address, a connection id and a channel id gets parsed correctly.
        /// </summary>
        [Fact]
        public void IPv6WithConnectionAndChannelIDParseTest()
        {
            string sipEndPointStr = "udp:[::1];cid=123;xid=1234567";
            SIPEndPoint sipEndPoint = SIPEndPoint.ParseSIPEndPoint(sipEndPointStr);

            Assert.True(sipEndPoint.Protocol == SIPProtocolsEnum.udp, "The SIPEndPoint protocol was " + 
                "incorrectly parsed.");
            Assert.True(sipEndPoint.Address.ToString() == "::1", "The SIPEndPoint IP address was " + 
                "incorrectly parsed.");
            Assert.True(sipEndPoint.Port == 5060, "The SIPEndPoint port was incorrectly parsed.");
            Assert.True(sipEndPoint.ChannelID == "123", "The SIPEndPoint channel ID was incorrectly parsed.");
            Assert.True(sipEndPoint.ConnectionID == "1234567", "The SIPEndPoint connection ID was " + 
                "incorrectly parsed.");
        }
    }
}
