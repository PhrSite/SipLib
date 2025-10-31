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

using SipLib.Core;

namespace SipLibUnitTests.Core;

[Trait("Category", "unit")]
public class SIPReplacesParameterUnitTest
{
    public SIPReplacesParameterUnitTest(Xunit.Abstractions.ITestOutputHelper output)
    {
    }

    [Fact]
    public void ParamsInUserPortionURITest()
    {
        var replaces = SIPReplacesParameter.Parse(SIPEscape.SIPURIParameterUnescape("a48484fb-ac6e00aa%4010.0.0.12%3Bfrom-tag%3D11e7a0c7ec2ab74eo0%3Bto-tag%3D1313732478"));

        Assert.Equal("a48484fb-ac6e00aa@10.0.0.12", replaces.CallID);
        Assert.Equal("1313732478", replaces.ToTag);
        Assert.Equal("11e7a0c7ec2ab74eo0", replaces.FromTag);
    }

    [Fact]
    public void BasicReplacesTest1()
    {
        string replaces = "87134171117590;to-tag=24796;from-tag=32650";
        SIPReplacesParameter srp = SIPReplacesParameter.Parse(replaces);
        Assert.NotNull(srp);
        Assert.True(srp.CallID == "87134171117590", "CallID mismatch");
        Assert.True(srp.ToTag == "24796", "ToTag mismatch");
        Assert.True(srp.FromTag == "32650", "FromTab mismatch");
        Assert.True(srp.EarlyOnly == false, "EarlyOnly mismatch");
    }

    [Fact]
    public void ToStringTest1()
    {
        SIPReplacesParameter srp1 = new SIPReplacesParameter("87134171117590", "24796", "32650");
        srp1.EarlyOnly = true;

        string srpstring = srp1.ToString();
        SIPReplacesParameter srp2 = SIPReplacesParameter.Parse(srpstring);
        Assert.NotNull(srp2);

        Assert.True(srp2.CallID == srp1.CallID, "CallID mismatch");
        Assert.True(srp2.ToTag == srp1.ToTag, "ToTag mismatch");
        Assert.True(srp2.FromTag == srp1.FromTag, "FromTag mismatch");
        Assert.True(srp2.EarlyOnly == true, "EarlyOnly is wrong");
    }

    [Fact]
    public void ReplacesHeaderInInvite()
    {
        SIPURI requestURI = SIPURI.ParseSIPURI("urn:service:sos");
        SIPURI ToURI = SIPURI.ParseSIPURI("sip:user1@192.168.1.101:5060");
        SIPURI FromURI = SIPURI.ParseSIPURI("sip:user2@192.168.1.22:5060");

        SIPRequest invite1 = SIPRequest.CreateBasicRequest(SIPMethodsEnum.INVITE, requestURI, ToURI, "user1", FromURI,
            "user2");
        SIPReplacesParameter srp1 = new SIPReplacesParameter("87134171117590", "24796", "32650");
        invite1.Header.ReplacesParameter = srp1;
        string strInvite = invite1.ToString();
        SIPRequest invite2 = SIPRequest.ParseSIPRequest(strInvite);
        Assert.NotNull(invite2);

        Assert.True(invite2.Header.ReplacesParameter != null, "ReplacesParameter is null");
        SIPReplacesParameter srp2 = invite2.Header.ReplacesParameter;
        Assert.True(srp2.CallID == srp1.CallID, "CallID mismatch");
        Assert.True(srp2.ToTag == srp1.ToTag, "ToTag mismatch");
        Assert.True(srp2.FromTag == srp1.FromTag, "FromTag mismatch");
        Assert.True(srp2.EarlyOnly == srp1.EarlyOnly, "EarlyOnly mismatch");
    }

}
