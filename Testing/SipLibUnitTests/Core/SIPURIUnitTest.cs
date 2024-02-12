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

//  Revised:
//      16 Nov 22 PHR
//          -- Added unit tests for parsing URN and tel style URI's
//      12 Feb 24 PHR
//          -- Replaced SIPURI.ParseSIPURIRelaxed() with ParseSIPURI() because all URI's must
//             have some kind of scheme (sip, urn, https, ...)
//          -- Enabled the unit tests containing IPv6 IP addresses
//          -- Added unit tests for https, wss, etc...

using SipLib.Core;

namespace SipLibUnitTests.Core;
using System.Net;

[Trait("Category", "unit")]
public class SIPURIUnitTest
{
    public SIPURIUnitTest(Xunit.Abstractions.ITestOutputHelper output)
    {
    }

    // 16 Nov 22 PHR
    /// <summary>
    /// Tests the ability to parse a URN type of URI such as urn:service:sos for NG9-1-1.
    /// </summary>
    [Fact]
    public void ParseUrn()
    {
        SIPURI UrnUri = SIPURI.ParseSIPURI("urn:service:sos");
        Assert.NotNull(UrnUri);
        Assert.True(UrnUri.Scheme == SIPSchemesEnum.urn);
        Assert.True(UrnUri.ToString() == "urn:service:sos");
    }

    // 16 Nov 22 PHR
    /// <summary>
    /// Tests parsing of a tel: URI
    /// </summary>
    [Fact]
    public void ParseTelUri()
    {
        SIPURI TelUri = SIPURI.ParseSIPURI("tel:+18185553333");
        Assert.NotNull(TelUri);
        Assert.True(TelUri.Scheme == SIPSchemesEnum.tel);
        Assert.True(TelUri.Scheme == SIPSchemesEnum.tel);
        Assert.True(TelUri.User == "8185553333");
    }

    // 16 Nov 22 PHR
    /// <summary>
    /// Tests a sip uri with a telephone number and a +1 as the host
    /// </summary>
    [Fact]
    public void ParseSipUriPlusSign()
    {
        SIPURI sipUri = SIPURI.ParseSIPURI("sip:+18185553333");
        Assert.NotNull(sipUri);
        Assert.True(sipUri.Scheme == SIPSchemesEnum.sip);
        Assert.True(sipUri.Host == "+18185553333");
        Assert.True(sipUri.User == "8185553333");
    }

    // 16 Nov 22 PHR
    /// <summary>
    /// Tests the parsing of a tel URI containing spaces
    /// </summary>
    [Fact]
    public void ParseTelUriWithSpaces()
    {
        SIPURI telUri = SIPURI.ParseSIPURI("tel:+1 818 555 3333");
        Assert.NotNull(telUri);
        Assert.True(telUri.Scheme == SIPSchemesEnum.tel);
        Assert.True(telUri.User == "8185553333");
    }

    // 16 Nov 22 PHR
    /// <summary>
    /// Tests the parsing of a tel URI containing dashes
    /// </summary>
    [Fact]
    public void ParseTelUriWithDashes()
    {
        SIPURI telUri = SIPURI.ParseSIPURI("tel:+1 818-555-3333");
        Assert.NotNull(telUri);
        Assert.True(telUri.Scheme == SIPSchemesEnum.tel);
        Assert.True(telUri.User == "8185553333");
    }

    // 12 Feb 24
    [Fact]
    public void ParseHttpsUri()
    {
        SIPURI httpsUri = SIPURI.ParseSIPURI("https://192.168.1.102");
        Assert.NotNull(httpsUri);
        Assert.True(httpsUri.Scheme == SIPSchemesEnum.https, "httpsUri.Scheme is wrong");
        Assert.True(httpsUri.Host == "192.168.1.102", "httpsUri.Host is wrong");
    }

    [Fact]
    public void ParseHttpsUriWithPortAndPath()
    {
        SIPURI httpsUri = SIPURI.ParseSIPURI("https://192.168.1.102:5060/Logging");
        Assert.NotNull(httpsUri);
        Assert.True(httpsUri.Scheme == SIPSchemesEnum.https, "httpsUri.Scheme is wrong");
        Assert.True(httpsUri.Host == "192.168.1.102:5060/Logging", "httpsUri.Host is wrong");
    }

    [Fact]
    public void ParseWssUri()
    {
        SIPURI wssUri = SIPURI.ParseSIPURI("wss://192.168.1.102:5060");
        Assert.NotNull(wssUri);
        Assert.True(wssUri.Scheme == SIPSchemesEnum.wss, "The Scheme is wrong");
        Assert.True(wssUri.Host == "192.168.1.102:5060", "wssUri.Host is wrong");
        Assert.True(wssUri.HostPort == "5060", "wssUri.HostPort is wrong");
    }

    [Fact]
    public void ParseCidUri()
    {
        SIPURI cidUri = SIPURI.ParseSIPURI("cid:8185553333@example.com");
        Assert.NotNull(cidUri);
        Assert.True(cidUri.Scheme == SIPSchemesEnum.cid, "The scheme is wrong");
        Assert.True(cidUri.Host == "example.com", "cidUri.Host is wrong");
        Assert.True(cidUri.User == "8185553333", "cidUri.User is wrong");
    }

    [Fact]
    public void ParseHostOnlyURIUnitTest()
    {
        SIPURI sipURI = SIPURI.ParseSIPURI("sip:sip.domain.com");

        Assert.True(sipURI.User == null, "The SIP URI User was not parsed correctly.");
        Assert.True(sipURI.Host == "sip.domain.com", "The SIP URI Host was not parsed correctly.");
    }

    [Fact]
    public void ParseHostAndUserURIUnitTest()
    {
        SIPURI sipURI = SIPURI.ParseSIPURI("sip:user@sip.domain.com");

        Assert.True(sipURI.User == "user", "The SIP URI User was not parsed correctly.");
        Assert.True(sipURI.Host == "sip.domain.com", "The SIP URI Host was not parsed correctly.");
    }

    [Fact]
    public void ParseWithParamURIUnitTest()
    {
        SIPURI sipURI = SIPURI.ParseSIPURI("sip:user@sip.domain.com;param=1234");

        Assert.True(sipURI.User == "user", "The SIP URI User was not parsed correctly.");
        Assert.True(sipURI.Host == "sip.domain.com", "The SIP URI Host was not parsed correctly.");
        Assert.True(sipURI.Parameters.Get("PARAM") == "1234", "The SIP URI Parameter was not parsed correctly.");
        Assert.True(sipURI.ToString() == "sip:user@sip.domain.com;param=1234", "The SIP URI was not correctly to string'ed.");
    }

    [Fact]
    public void ParseWithParamAndPortURIUnitTest()
    {
        SIPURI sipURI = SIPURI.ParseSIPURI("sip:1234@sip.domain.com:5060;TCID-0");

        Assert.True(sipURI.User == "1234", "The SIP URI User was not parsed correctly.");
        Assert.True(sipURI.Host == "sip.domain.com:5060", "The SIP URI Host was not parsed correctly.");
        Assert.True(sipURI.Parameters.Has("TCID-0"), "The SIP URI Parameter was not parsed correctly.");
    }

    [Fact]
    public void ParseWithHeaderURIUnitTest()
    {
        SIPURI sipURI = SIPURI.ParseSIPURI("sip:user@sip.domain.com?header=1234");

        Assert.True(sipURI.User == "user", "The SIP URI User was not parsed correctly.");
        Assert.True(sipURI.Host == "sip.domain.com", "The SIP URI Host was not parsed correctly.");
        Assert.True(sipURI.Headers.Get("header") == "1234", "The SIP URI Header was not parsed correctly.");
    }

    [Fact]
    public void SpaceInHostNameURIUnitTest()
    {
        SIPURI sipURI = SIPURI.ParseSIPURI("sip:Blue Face");

        Assert.True(sipURI.User == null, "The SIP URI User was not parsed correctly.");
        Assert.True(sipURI.Host == "Blue Face", "The SIP URI Host was not parsed correctly.");
    }

    [Fact]
    public void ContactAsteriskURIUnitTest()
    {
        SIPURI sipURI = SIPURI.ParseSIPURI("*");

        Assert.True(sipURI.User == null, "The SIP URI User was not parsed correctly.");
        Assert.True(sipURI.Host == "*", "The SIP URI Host was not parsed correctly.");
    }

    [Fact]
    public void AreEqualNoParamsURIUnitTest()
    {
        SIPURI sipURI1 = SIPURI.ParseSIPURI("sip:abcd@adcb.com");
        SIPURI sipURI2 = SIPURI.ParseSIPURI("sip:abcd@adcb.com");

        Assert.True(sipURI1 == sipURI2, "The SIP URIs were not correctly found as equal.");
    }

    [Fact]
    public void AreEqualIPAddressNoParamsURIUnitTest()
    {
        SIPURI sipURI1 = SIPURI.ParseSIPURI("sip:abcd@192.168.1.101");
        SIPURI sipURI2 = SIPURI.ParseSIPURI("sip:abcd@192.168.1.101");

        Assert.True(sipURI1 == sipURI2, "The SIP URIs were not correctly found as equal.");
    }

    [Fact]
    public void AreEqualWithParamsURIUnitTest()
    {
        SIPURI sipURI1 = SIPURI.ParseSIPURI("sip:abcd@adcb.com;key1=value1;key2=value2");
        SIPURI sipURI2 = SIPURI.ParseSIPURI("sip:abcd@adcb.com;key2=value2;key1=value1");

        Assert.True(sipURI1 == sipURI2, "The SIP URIs were not correctly found as equal.");
    }

    [Fact]
    public void NotEqualWithParamsURIUnitTest()
    {
        SIPURI sipURI1 = SIPURI.ParseSIPURI("sip:abcd@adcb.com;key1=value1;key2=value2");
        SIPURI sipURI2 = SIPURI.ParseSIPURI("sip:abcd@adcb.com;key2=value2;key1=value2");

        Assert.NotEqual(sipURI1, sipURI2);
    }

    [Fact]
    public void AreEqualWithHeadersURIUnitTest()
    {
        SIPURI sipURI1 = SIPURI.ParseSIPURI("sip:abcd@adcb.com;key1=value1;key2=value2?header1=value1&header2=value2");
        SIPURI sipURI2 = SIPURI.ParseSIPURI("sip:abcd@adcb.com;key2=value2;key1=value1?header2=value2&header1=value1");

        Assert.True(sipURI1 == sipURI2, "The SIP URIs were not correctly identified as equal.");
    }

    [Fact]
    public void NotEqualWithHeadersURIUnitTest()
    {
        SIPURI sipURI1 = SIPURI.ParseSIPURI("sip:abcd@adcb.com;key1=value1;key2=value2?header1=value2&header2=value2");
        SIPURI sipURI2 = SIPURI.ParseSIPURI("sip:abcd@adcb.com;key2=value2;key1=value1?header2=value2&header1=value1");

        Assert.NotEqual(sipURI1, sipURI2);
    }

    [Fact]
    public void UriWithParameterEqualityURIUnitTest()
    {
        SIPURI sipURI1 = SIPURI.ParseSIPURI("sip:abcd@adcb.com;key1=value1");
        SIPURI sipURI2 = SIPURI.ParseSIPURI("sip:abcd@adcb.com;key1=value1");

        Assert.True(sipURI1 == sipURI2, "The SIP URIs did not have equal hash codes.");
    }

    [Fact]
    public void UriWithDifferentParamsEqualURIUnitTest()
    {
        SIPURI sipURI1 = SIPURI.ParseSIPURI("sip:abcd@adcb.com;key1=value1");
        SIPURI sipURI2 = SIPURI.ParseSIPURI("sip:abcd@adcb.com;key1=value2");

        Assert.NotEqual(sipURI1, sipURI2);
    }

    [Fact]
    public void UriWithSameParamsInDifferentOrderURIUnitTest()
    {
        SIPURI sipURI1 = SIPURI.ParseSIPURI("sip:abcd@adcb.com;key2=value2;key1=value1");
        SIPURI sipURI2 = SIPURI.ParseSIPURI("sip:abcd@adcb.com;key1=value1;key2=value2");

        Assert.Equal(sipURI1, sipURI2);
    }

    [Fact]
    public void AreEqualNullURIsUnitTest()
    {
        SIPURI sipURI1 = null;
        SIPURI sipURI2 = null;

        Assert.True(sipURI1 == sipURI2, "The SIP URIs were not correctly found as equal.");
    }

    [Fact]
    public void NotEqualOneNullURIUnitTest()
    {
        SIPURI sipURI1 = SIPURI.ParseSIPURI("sip:abcd@adcb.com");
        SIPURI sipURI2 = null;

        Assert.False(sipURI1 == sipURI2, "The SIP URIs were incorrectly found as equal.");
    }

    [Fact]
    public void AreEqualNullEqualsOverloadUnitTest()
    {
        SIPURI sipURI1 = null;

        Assert.True(sipURI1 == null, "The SIP URIs were not correctly found as equal.");
    }

    [Fact]
    public void AreEqualNullNotEqualsOverloadUnitTest()
    {
        SIPURI sipURI1 = null;

        Assert.False(sipURI1 != null, "The SIP URIs were incorrectly found as equal.");
    }

    [Fact]
    public void UnknownSchemeUnitTest()
    {
        Assert.Throws<SIPValidationException>(() => SIPURI.ParseSIPURI("mailto:1234565"));
    }

    [Fact]
    public void KnownSchemesUnitTest()
    {
        foreach (var value in System.Enum.GetValues(typeof(SIPSchemesEnum)))
        {
            Assert.True(SIPURI.ParseSIPURI(value.ToString() + ":1234565").Scheme == (SIPSchemesEnum)value);
        }

    }

    [Fact]
    public void ParamsInUserPortionURITest()
    {
        SIPURI sipURI = SIPURI.ParseSIPURI("sip:C=on;t=DLPAN@10.0.0.1:5060;lr");

        Assert.True("C=on;t=DLPAN" == sipURI.User, "SIP user portion parsed incorrectly.");
        Assert.True("10.0.0.1:5060" == sipURI.Host, "SIP host portion parsed incorrectly.");
    }

    [Fact]
    public void SwitchTagParameterUnitTest()
    {
        SIPURI sipURI = SIPURI.ParseSIPURI("sip:joebloggs@sip.mysipswitch.com;switchtag=119651");

        Assert.True("joebloggs" == sipURI.User, "SIP user portion parsed incorrectly.");
        Assert.True("sip.mysipswitch.com" == sipURI.Host, "SIP host portion parsed incorrectly.");
        Assert.True("119651" == sipURI.Parameters.Get("switchtag"), "switchtag parameter parsed incorrectly.");
    }

    [Fact]
    public void LongUserUnitTest()
    {
        SIPURI sipURI = SIPURI.ParseSIPURI("sip:EhZgKgLM9CwGqYDAECqDpL5MNrM_sKN5NurN5q_pssAk4oxhjKEMT4@10.0.0.1:5060");

        Assert.True("EhZgKgLM9CwGqYDAECqDpL5MNrM_sKN5NurN5q_pssAk4oxhjKEMT4" == sipURI.User, "SIP user portion parsed incorrectly.");
        Assert.True("10.0.0.1:5060" == sipURI.Host, "SIP host portion parsed incorrectly.");
    }

    [Fact]
    public void ParsePartialURISIPSSchemeUnitTest()
    {
        SIPURI sipURI = SIPURI.ParseSIPURI("sips:sip.domain.com:1234");

        Assert.True(sipURI.Scheme == SIPSchemesEnum.sips, "The SIP URI scheme was not parsed correctly.");
        Assert.True(sipURI.User == null, "The SIP URI User was not parsed correctly.");
        Assert.True(sipURI.Host == "sip.domain.com:1234", "The SIP URI Host was not parsed correctly.");
    }

    [Fact]
    public void ParsePartialURIWithUserUnitTest()
    {
        SIPURI sipURI = SIPURI.ParseSIPURI("sip:joe.bloggs@sip.domain.com:1234;transport=tcp");

        Assert.True(sipURI.Scheme == SIPSchemesEnum.sip, "The SIP URI scheme was not parsed correctly.");
        Assert.True(sipURI.User == "joe.bloggs", "The SIP URI User was not parsed correctly.");
        Assert.True(sipURI.Host == "sip.domain.com:1234", "The SIP URI Host was not parsed correctly.");
        Assert.True(sipURI.Protocol == SIPProtocolsEnum.tcp, "The SIP URI protocol was not parsed correctly.");
    }

    /// <summary>
    /// Got a URI like this from Zoiper.
    /// </summary>
    [Fact]
    public void ParseHoHostUnitTest()
    {
        Assert.Throws<SIPValidationException>(() => SIPURI.ParseSIPURI("sip:;transport=UDP"));
    }

    [Fact]
    public void UDPProtocolToStringTest()
    {
        SIPURI sipURI = new SIPURI(SIPSchemesEnum.sip, SIPEndPoint.ParseSIPEndPoint("udp:127.0.0.1"));
        Assert.True(sipURI.ToString() == "sip:127.0.0.1:5060", "The SIP URI was not ToString'ed correctly.");
    }

    [Fact]
    public void ParseUDPProtocolToStringTest()
    {
        SIPURI sipURI = SIPURI.ParseSIPURI("sip:127.0.0.1");
        Assert.True(sipURI.ToString() == "sip:127.0.0.1", "The SIP URI was not ToString'ed correctly.");
    }

    [Fact]
    public void ParseBigURIUnitTest()
    {
        SIPURI sipURI = SIPURI.ParseSIPURI("sip:TRUNKa1d2ce524d44cd54f39ac78bcdba85c7@65.98.14.50:5069");
        Assert.True(sipURI.ToString() == "sip:TRUNKa1d2ce524d44cd54f39ac78bcdba85c7@65.98.14.50:5069", "The SIP URI was not ToString'ed correctly.");
    }

    [Fact]
    public void ParseMalformedContactUnitTest()
    {
        Assert.Throws<SIPValidationException>(() => SIPURI.ParseSIPURI("sip:twolmsted@24.183.120.253, sip:5060"));
    }

    [Fact]
    public void NoPortIPv4CanonicalAddressToStringTest()
    {
        SIPURI sipURI = SIPURI.ParseSIPURI("sip:127.0.0.1");
        Assert.True(sipURI.ToString() == "sip:127.0.0.1", "The SIP URI was not ToString'ed correctly.");
        Assert.True(sipURI.CanonicalAddress == "sip:127.0.0.1:5060", "The SIP URI canonical address was not correct.");
    }

    /// <summary>
    /// Tests that a SIP URI with an IPv6 address is correctly parsed.
    /// </summary>
    [Fact]
    public void ParseIPv6UnitTest()
    {
        SIPURI sipURI = SIPURI.ParseSIPURI("sip:[::1]");

        Assert.True(sipURI.Scheme == SIPSchemesEnum.sip, "The SIP URI scheme was not parsed correctly.");
        Assert.True(sipURI.Host == "[::1]", "The SIP URI host was not parsed correctly.");
        Assert.True(sipURI.ToSIPEndPoint() == new SIPEndPoint(SIPProtocolsEnum.udp, IPAddress.IPv6Loopback, 5060, null, null), "The SIP URI end point details were not parsed correctly.");

        //rj2: should throw exception
        Assert.Throws<SIPValidationException>(() => SIPURI.ParseSIPURI("sip:user1@2a00:1450:4005:800::2004"));//ipv6 host without mandatory brackets
        Assert.Throws<SIPValidationException>(() => SIPURI.ParseSIPURI("sip:user1@:::ffff:127.0.0.1"));//ipv6 with mapped ipv4 localhost
        //rj2: should/does not throw exception
        sipURI = SIPURI.ParseSIPURI("sip:[::ffff:127.0.0.1]");
        Assert.True(sipURI.Host == "[::ffff:127.0.0.1]", "The SIP URI host was not parsed correctly.");
    }

    /// <summary>
    /// Tests that a SIP URI with an IPv6 address and an explicit port is correctly parsed.
    /// </summary>
    [Fact]
    public void ParseIPv6WithExplicitPortUnitTest()
    {
        SIPURI sipURI = SIPURI.ParseSIPURI("sip:[::1]:6060");

        Assert.True(sipURI.Scheme == SIPSchemesEnum.sip, "The SIP URI scheme was not parsed correctly.");
        Assert.True(sipURI.Host == "[::1]:6060", "The SIP URI host was not parsed correctly.");
        Assert.True(sipURI.ToSIPEndPoint() == new SIPEndPoint(SIPProtocolsEnum.udp, IPAddress.IPv6Loopback, 6060, null, null), "The SIP URI end point details were not parsed correctly.");
    }

    /// <summary>
    /// Tests that SIP URIs with an IPv6 address with default ports generate the same canonical addresses.
    /// </summary>
    [Fact]
    public void IPv6UriPortToNoPortCanonicalAddressUnitTest()
    {
        SIPURI sipURINoPort = SIPURI.ParseSIPURI("sip:[::1]");
        SIPURI sipURIWIthPort = SIPURI.ParseSIPURI("sip:[::1]:5060");

        Assert.Equal(sipURINoPort.CanonicalAddress, sipURIWIthPort.CanonicalAddress);
        Assert.True(sipURINoPort.ToString() == "sip:[::1]", "The SIP URI was not ToString'ed correctly.");
        Assert.True(sipURIWIthPort.CanonicalAddress == "sip:[::1]:5060", "The SIP URI canonical address was not correct.");

        //rj2: more test cases
        sipURINoPort = SIPURI.ParseSIPURI("sip:[2a00:1450:4005:800::2004]");
        sipURIWIthPort = SIPURI.ParseSIPURI("sip:[2a00:1450:4005:800::2004]:5060");

        Assert.Equal(sipURINoPort.CanonicalAddress, sipURIWIthPort.CanonicalAddress);
        Assert.True(sipURINoPort.ToString() == "sip:[2a00:1450:4005:800::2004]", "The SIP URI was not ToString'ed correctly.");
        Assert.True(sipURIWIthPort.CanonicalAddress == "sip:[2a00:1450:4005:800::2004]:5060", "The SIP URI canonical address was not correct.");

        sipURINoPort = SIPURI.ParseSIPURI("sip:user1@[2a00:1450:4005:800::2004]");
        sipURIWIthPort = SIPURI.ParseSIPURI("sip:user1@[2a00:1450:4005:800::2004]:5060");

        Assert.Equal(sipURINoPort.CanonicalAddress, sipURIWIthPort.CanonicalAddress);
        Assert.True(sipURINoPort.ToString() == "sip:user1@[2a00:1450:4005:800::2004]", "The SIP URI was not ToString'ed correctly.");
        Assert.True(sipURIWIthPort.CanonicalAddress == "sip:user1@[2a00:1450:4005:800::2004]:5060", "The SIP URI canonical address was not correct.");
    }

    /// <summary>
    /// Tests that the SIP URI constructor that takes an IP address works correctly for IPv6.
    /// </summary>
    [Fact]
    public void UriConstructorWithIPv6AddressUnitTest()
    {
        SIPURI ipv6Uri = new SIPURI(SIPSchemesEnum.sip, IPAddress.IPv6Loopback, 6060);
        Assert.Equal("sip:[::1]:6060", ipv6Uri.ToString());
    }

    /// <summary>
    /// Tests that the invalid SIP URIs with IPv6 addresses missing enclosing '[' and ']' throw an exception.
    /// </summary>
    [Fact]
    public void InvalidIPv6UriThrowUnitTest()
    {
        SIPURI ipv6Uri = new SIPURI(SIPSchemesEnum.sip, IPAddress.IPv6Loopback, 6060);

        Assert.Throws<SIPValidationException>(() => SIPURI.ParseSIPURI("sip:user1@2a00:1450:4005:800::2004"));
        Assert.Throws<SIPValidationException>(() => SIPURI.ParseSIPURI("sip:user1@:::ffff:127.0.0.1"));
    }

    /// <summary>
    /// Tests that a SIP URI with an IPv4 address mapped to an IPv6 address is parsed correctly.
    /// </summary>
    [Fact]
    public void ParseIPv4MappedAddressUnitTest()
    {
        SIPURI ipv6Uri = new SIPURI(SIPSchemesEnum.sip, IPAddress.IPv6Loopback, 6060);
        var uri = SIPURI.ParseSIPURI("sip:[::ffff:127.0.0.1]");
        Assert.Equal("[::ffff:127.0.0.1]", uri.Host);
    }

    /// <summary>
    /// Tests that a SIP URI supplied in a REFER request Refer-To header can be parsed.
    /// </summary>
    [Fact]
    public void ParseReplacesHeaderUriUnitTest()
    {
        SIPURI referToUri = SIPURI.ParseSIPURI("sip:1@127.0.0.1?Replaces=84929ZTg0Zjk1Y2UyM2Q1OWJjYWNlZmYyYTI0Njg1YjgwMzI%3Bto-tag%3D8787f9cc94bb4bb19c089af17e5a94f7%3Bfrom-tag%3Dc2b89404");

        Assert.NotNull(referToUri);
        Assert.Equal("sip:1@127.0.0.1", referToUri.ToParameterlessString());
    }

    /// <summary>
    /// Tests that a SIP URI with a private IPv4 address gets mangled correctly.
    /// </summary>
    // 9 Nov 22 PHR -- Mangle not implemented in SipLib
    //[Fact]
    //public void MangleUnitTest()
    //{
    //    SIPURI uri = SIPURI.ParseSIPURI("sip:user@192.168.0.50:5060?Replaces=xyz");
    //    SIPURI mangled = SIPURI.Mangle(uri, IPSocket.Parse("67.222.131.147:5090"));

    //    Assert.NotNull(mangled);
    //    Assert.Equal("sip:user@67.222.131.147:5090?Replaces=xyz", mangled.ToString());
    //}

    /// <summary>
    /// Tests that a SIP URI with a private IPv4 address and no port gets mangled correctly.
    /// </summary>
    // 9 Nov 22 PHR -- Mangle not implemented in SipLib
    //[Fact]
    //public void MangleNoPortUnitTest()
    //{
    //    SIPURI uri = SIPURI.ParseSIPURI("sip:user@192.168.0.50?Replaces=xyz");
    //    SIPURI mangled = SIPURI.Mangle(uri, IPSocket.Parse("67.222.131.147:5090"));

    //    Assert.NotNull(mangled);
    //    Assert.Equal("sip:user@67.222.131.147:5090?Replaces=xyz", mangled.ToString());
    //}

    /// <summary>
    /// Tests that a SIP URI with a private IPv4 address and that was recived on an IPv6
    /// end point gets mangled correctly.
    /// </summary>
    // 9 Nov 22 PHR -- Mangle not implemented in SipLib
    //[Fact]
    //public void MangleReceiveOnIPv6UnitTest()
    //{
    //    logger.LogDebug("--> " + System.Reflection.MethodBase.GetCurrentMethod().Name);
    //    logger.BeginScope(System.Reflection.MethodBase.GetCurrentMethod().Name);

    //    SIPURI uri = SIPURI.ParseSIPURI("sip:user@192.168.0.50:5060?Replaces=xyz");
    //    SIPURI mangled = SIPURI.Mangle(uri, IPSocket.Parse("[2001:730:3ec2::10]:5090"));

    //    logger.LogDebug($"Mangled URI {mangled}.");

    //    Assert.NotNull(mangled);
    //    Assert.Equal("sip:user@[2001:730:3ec2::10]:5090?Replaces=xyz", mangled.ToString());

    //    logger.LogDebug("-----------------------------------------");
    //}

    /// <summary>
    /// Tests that a SIP URI with a does not get mangled when the received on IP address
    /// is the same private IP address as the URI host.
    /// </summary>
    // 9 Nov 22 PHR -- Mangle not implemented in SipLib
    //[Fact]
    //public void NoMangleSameAddressUnitTest()
    //{
    //    logger.LogDebug("--> " + System.Reflection.MethodBase.GetCurrentMethod().Name);
    //    logger.BeginScope(System.Reflection.MethodBase.GetCurrentMethod().Name);

    //    SIPURI uri = SIPURI.ParseSIPURI("sip:user@192.168.0.50:5060?Replaces=xyz");
    //    SIPURI mangled = SIPURI.Mangle(uri, IPSocket.Parse("192.168.0.50:5060"));

    //    Assert.Null(mangled);

    //    logger.LogDebug("-----------------------------------------");
    //}

    /// <summary>
    /// Tests that a SIP URI with a public IPv4 address does not get mangled.
    /// </summary>
    // 9 Nov 22 PHR -- Mangle not implemented in SipLib
    //[Fact]
    //public void NoManglePublicIPv4UnitTest()
    //{
    //    logger.LogDebug("--> " + System.Reflection.MethodBase.GetCurrentMethod().Name);
    //    logger.BeginScope(System.Reflection.MethodBase.GetCurrentMethod().Name);

    //    SIPURI uri = SIPURI.ParseSIPURI("sip:user@67.222.131.149:5060?Replaces=xyz");
    //    SIPURI mangled = SIPURI.Mangle(uri, IPSocket.Parse("67.222.131.147:5060"));

    //    Assert.Null(mangled);

    //    logger.LogDebug("-----------------------------------------");
    //}

    /// <summary>
    /// Tests that a SIP URI with a hostname does not get mangled.
    /// </summary>
    // 9 Nov 22 PHR -- Mangle not implemented in SipLib
    //[Fact]
    //public void NoMangleHostnameUnitTest()
    //{
    //    logger.LogDebug("--> " + System.Reflection.MethodBase.GetCurrentMethod().Name);
    //    logger.BeginScope(System.Reflection.MethodBase.GetCurrentMethod().Name);

    //    SIPURI uri = SIPURI.ParseSIPURI("sip:user@sipsorcery.com:5060?Replaces=xyz");
    //    SIPURI mangled = SIPURI.Mangle(uri, IPSocket.Parse("67.222.131.147:5060"));

    //    Assert.Null(mangled);

    //    logger.LogDebug("-----------------------------------------");
    //}

    /// <summary>
    /// Tests that a SIP URI with an IPv6 host does not get mangled.
    /// </summary>
    // 9 Nov 22 PHR -- Mangle not implemented in SipLib
    //[Fact]
    //public void NoMangleIPv6UnitTest()
    //{
    //    logger.LogDebug("--> " + System.Reflection.MethodBase.GetCurrentMethod().Name);
    //    logger.BeginScope(System.Reflection.MethodBase.GetCurrentMethod().Name);

    //    SIPURI uri = SIPURI.ParseSIPURI("sip:user@[2001:730:3ec2::10]:5060?Replaces=xyz");
    //    SIPURI mangled = SIPURI.Mangle(uri, IPSocket.Parse("67.222.131.147:5060"));

    //    Assert.Null(mangled);

    //    logger.LogDebug("-----------------------------------------");
    //}

    /// <summary>
    /// Tests that a SIP URI with a default UDP port is correctly recognised.
    /// </summary>
    // 9 Nov 22 PHR -- SipLib does not support IPv6
    //[Fact]
    //public void DefaultUdpPortUnitTest()
    //{
    //    logger.LogDebug("--> " + System.Reflection.MethodBase.GetCurrentMethod().Name);
    //    logger.BeginScope(System.Reflection.MethodBase.GetCurrentMethod().Name);

    //    SIPURI uri = SIPURI.ParseSIPURI("sip:user@[2001:730:3ec2::10]:5060?Replaces=xyz");

    //    Assert.Equal(SIPProtocolsEnum.udp, uri.Protocol);
    //    Assert.True(uri.IsDefaultPort());

    //    logger.LogDebug("-----------------------------------------");
    //}

    /// <summary>
    /// Tests that a SIP URI with a the port not set is correctly recognised as using the
    /// default port.
    /// </summary>
    // 9 Nov 22 PHR -- SipLib does not support IPv6
    //[Fact]
    //public void DefaultUdpPortWhenNotSetUnitTest()
    //{
    //    logger.LogDebug("--> " + System.Reflection.MethodBase.GetCurrentMethod().Name);
    //    logger.BeginScope(System.Reflection.MethodBase.GetCurrentMethod().Name);

    //    SIPURI uri = SIPURI.ParseSIPURI("sip:user@[2001:730:3ec2::10]?Replaces=xyz");

    //    Assert.Equal(SIPProtocolsEnum.udp, uri.Protocol);
    //    Assert.True(uri.IsDefaultPort());

    //    logger.LogDebug("-----------------------------------------");
    //}

    /// <summary>
    /// Tests that a SIP URI with a non-default UDP port is correctly recognised.
    /// </summary>
    // 9 Nov 22 PHR -- SipLib does not support IPv6
    //[Fact]
    //public void NonDefaultUdpPortUnitTest()
    //{
    //    logger.LogDebug("--> " + System.Reflection.MethodBase.GetCurrentMethod().Name);
    //    logger.BeginScope(System.Reflection.MethodBase.GetCurrentMethod().Name);

    //    SIPURI uri = SIPURI.ParseSIPURI("sip:user@[2001:730:3ec2::10]:5080?Replaces=xyz");

    //    Assert.Equal(SIPProtocolsEnum.udp, uri.Protocol);
    //    Assert.False(uri.IsDefaultPort());

    //    logger.LogDebug("-----------------------------------------");
    //}

    /// <summary>
    /// Tests that a SIP URI with a default TCP port is correctly recognised.
    /// </summary>
    // 9 Nov 22 PHR -- SipLib does not support IPv6
    //[Fact]
    //public void DefaultTcpPortUnitTest()
    //{
    //    logger.LogDebug("--> " + System.Reflection.MethodBase.GetCurrentMethod().Name);
    //    logger.BeginScope(System.Reflection.MethodBase.GetCurrentMethod().Name);

    //    SIPURI uri = SIPURI.ParseSIPURI("sip:user@[2001:730:3ec2::10]:5060;transport=tcp?Replaces=xyz");

    //    Assert.Equal(SIPProtocolsEnum.tcp, uri.Protocol);
    //    Assert.True(uri.IsDefaultPort());

    //    logger.LogDebug("-----------------------------------------");
    //}

    /// <summary>
    /// Tests that a SIP URI with a default TLS port is correctly recognised.
    /// </summary>
    // 9 Nov 22 PHR -- SipLib does not support IPv6
    //[Fact]
    //public void DefaultTlsPortUnitTest()
    //{
    //    logger.LogDebug("--> " + System.Reflection.MethodBase.GetCurrentMethod().Name);
    //    logger.BeginScope(System.Reflection.MethodBase.GetCurrentMethod().Name);

    //    SIPURI uri = SIPURI.ParseSIPURI("sips:user@[2001:730:3ec2::10]:5061?Replaces=xyz");

    //    Assert.Equal(SIPProtocolsEnum.tls, uri.Protocol);
    //    Assert.True(uri.IsDefaultPort());

    //    logger.LogDebug("-----------------------------------------");
    //}

    /// <summary>
    /// Tests that a SIP URI with a default Web Socket port is correctly recognised.
    /// </summary>
    // 9 Nov 22 PHR -- SipLib does not support IPv6
    //[Fact]
    //public void DefaultWebSocketPortUnitTest()
    //{
    //    logger.LogDebug("--> " + System.Reflection.MethodBase.GetCurrentMethod().Name);
    //    logger.BeginScope(System.Reflection.MethodBase.GetCurrentMethod().Name);

    //    SIPURI uri = SIPURI.ParseSIPURI("sip:user@[2001:730:3ec2::10]:80;transport=ws?Replaces=xyz");

    //    Assert.Equal(SIPProtocolsEnum.ws, uri.Protocol);
    //    Assert.True(uri.IsDefaultPort());

    //    logger.LogDebug("-----------------------------------------");
    //}

    /// <summary>
    /// Tests that a SIP URI with a default Secure Web Socket port is correctly recognised.
    /// </summary>
    // 9 Nov 22 PHR -- SipLib does not support IPv6
    //[Fact]
    //public void DefaultSecureWebSocketPortUnitTest()
    //{
    //    logger.LogDebug("--> " + System.Reflection.MethodBase.GetCurrentMethod().Name);
    //    logger.BeginScope(System.Reflection.MethodBase.GetCurrentMethod().Name);

    //    SIPURI uri = SIPURI.ParseSIPURI("sip:user@[2001:730:3ec2::10]:443;transport=wss?Replaces=xyz");

    //    Assert.Equal(SIPProtocolsEnum.wss, uri.Protocol);
    //    Assert.True(uri.IsDefaultPort());

    //    logger.LogDebug("-----------------------------------------");
    //}
}
