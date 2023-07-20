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

using System.Text.RegularExpressions;
using SipLib.Core;

namespace SipLibUnitTests
{
    [Trait("Category", "unit")]
    public class SIPParametersUnitTest
    {
        public SIPParametersUnitTest(Xunit.Abstractions.ITestOutputHelper output)
        {
        }

        [Fact]
        public void RouteParamExtractTest()
        {
            string routeParam = ";lr;server=hippo";
            SIPParameters serverParam = new SIPParameters(routeParam, ';');
            string serverParamValue = serverParam.Get("server");

            Assert.True(serverParamValue == "hippo", "The server parameter was not correctly extracted.");
        }

        [Fact]
        public void QuotedStringParamExtractTest()
        {
            string methodsParam = ";methods=\"INVITE, MESSAGE, INFO, SUBSCRIBE, OPTIONS, BYE, CANCEL, NOTIFY, ACK, REFER\"";
            SIPParameters serverParam = new SIPParameters(methodsParam, ';');
            string methodsParamValue = serverParam.Get("methods");

            Assert.True(methodsParamValue == "\"INVITE, MESSAGE, INFO, SUBSCRIBE, OPTIONS, BYE, CANCEL, NOTIFY, ACK, REFER\"", "The method parameter was not correctly extracted.");
        }

        [Fact]
        public void UserFieldWithNamesExtractTest()
        {
            string userField = "\"Joe Bloggs\" <sip:joe@bloggs.com>;allow=\"options, invite, cancel\"";
            string[] keyValuePairs = SIPParameters.GetKeyValuePairsFromQuoted(userField, ',');

            Assert.True(keyValuePairs.Length == 1, "An incorrect number of key value pairs was extracted");
        }

        [Fact]
        public void MultipleUserFieldWithNamesExtractTest()
        {
            string userField = "\"Joe Bloggs\" <sip:joe@bloggs.com>;allow=\"options, invite, cancel\" , \"Jane Doe\" <sip:jabe@doe.com>";
            string[] keyValuePairs = SIPParameters.GetKeyValuePairsFromQuoted(userField, ',');

            Assert.True(keyValuePairs.Length == 2, "An incorrect number of key value pairs was extracted");
        }

        [Fact]
        public void MultipleUserFieldWithNamesExtraWhitespaceExtractTest()
        {
            string userField = "  \"Joe Bloggs\"   <sip:joe@bloggs.com>;allow=\"options, invite, cancel\" \t,   \"Jane Doe\" <sip:jabe@doe.com>";
            string[] keyValuePairs = SIPParameters.GetKeyValuePairsFromQuoted(userField, ',');

            Assert.True(keyValuePairs.Length == 2, "An incorrect number of key value pairs was extracted");
        }

        [Fact]
        public void GetHashCodeDiffOrderEqualityUnittest()
        {
            string testParamStr1 = ";lr;server=hippo;ftag=12345";
            SIPParameters testParam1 = new SIPParameters(testParamStr1, ';');

            string testParamStr2 = ";lr;server=hippo;ftag=12345";
            SIPParameters testParam2 = new SIPParameters(testParamStr2, ';');

            Assert.Equal(testParam1, testParam2);
        }

        [Fact]
        public void GetHashCodeDiffOrderEqualityReorderedUnittest()
        {
            string testParamStr1 = ";lr;server=hippo;ftag=12345";
            SIPParameters testParam1 = new SIPParameters(testParamStr1, ';');

            string testParamStr2 = "ftag=12345;lr;server=hippo;";
            SIPParameters testParam2 = new SIPParameters(testParamStr2, ';');

            Assert.Equal(testParam1, testParam2);
        }

        [Fact]
        public void CheckEqualWithDiffCaseEqualityUnittest()
        {
            string testParamStr1 = ";LR;Server=hippo;FTag=12345";
            SIPParameters testParam1 = new SIPParameters(testParamStr1, ';');
            string testParamStr2 = "ftag=12345;lr;server=hippo;";
            SIPParameters testParam2 = new SIPParameters(testParamStr2, ';');

            //Assert.Equal(testParam1, testParam2);
            // 8 Nov 22 PHR -- The above is not correct, do this.
            Assert.True(testParam1 == testParam2);
        }

        [Fact]
        public void GetHashCodeDiffValueCaseEqualityUnittest()
        {
            string testParamStr1 = ";LR;Server=hippo;FTag=12345";
            SIPParameters testParam1 = new SIPParameters(testParamStr1, ';');
            string testParamStr2 = "ftag=12345;lr;server=HiPPo;";
            SIPParameters testParam2 = new SIPParameters(testParamStr2, ';');

            Assert.True(testParam1.GetHashCode() != testParam2.GetHashCode(), "The parameters had different hashcode values.");
        }

        [Fact]
        public void EmptyValueParametersUnittest()
        {
            string testParamStr1 = ";emptykey;Server=hippo;FTag=12345";
            SIPParameters testParam1 = new SIPParameters(testParamStr1, ';');

            Assert.True(testParam1.Has("emptykey"), "The empty parameter \"emptykey\" was not correctly extracted from the parameter string.");
            Assert.True(Regex.Match(testParam1.ToString(), "emptykey").Success, "The emptykey name was not in the output parameter string.");
        }
    }
}
