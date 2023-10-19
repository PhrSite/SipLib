using SipLib.Core;

namespace SipLibUnitTests.Core
{
    [Trait("Category", "unit")]
    public class SIPContactHeaderUnitTest
    {
        [Fact]
        public void ParseContactHeaderDomainForUserTest()
        {

            string testContactHeader = "<sip:sip.domain.com@sip.domain.com>";
            List<SIPContactHeader> sipContactHeaderList = SIPContactHeader.
                ParseContactHeader(testContactHeader);

            Assert.True(sipContactHeaderList[0].ContactName == null, "The Contact " +
                "header name was not parsed correctly.");
            Assert.True(sipContactHeaderList[0].ContactURI.ToString() == 
                "sip:sip.domain.com@sip.domain.com", "The Contact header URI was " +
                "not parsed correctly.");
        }

        [Fact]
        public void ParseBadAastraContactHeaderUserTest()
        {
            string testContactHeader = "<sip:10001@127.0.0.1:5060\n";

            Assert.Throws<SIPValidationException>(() => SIPContactHeader.
                ParseContactHeader(testContactHeader));
        }

        [Fact]
        public void ParseNoAngleQuotesContactHeaderUserTest()
        {
            string testContactHeader = "sip:10001@127.0.0.1:5060";
            List<SIPContactHeader> sipContactHeaderList = SIPContactHeader.
                ParseContactHeader(testContactHeader);

            Assert.True(sipContactHeaderList[0].ContactName == null, 
                "The Contact header name was not parsed correctly.");
            Assert.True(sipContactHeaderList[0].ContactURI.ToString() == testContactHeader, 
                "The Contact header URI was not parsed correctly.");
        }

        [Fact]
        public void ParseNoLineBreakContactHeaderUserTest()
        {
            string testContactHeader = "<sip:10001@127.0.0.1:5060\nAllow: OPTIONS";
            Assert.Throws<SIPValidationException>(() => SIPContactHeader.
                ParseContactHeader(testContactHeader));
        }

        [Fact]
        public void ParseContactWithParamHeaderUserTest()
        {
            string testContactHeader = "<sip:user@127.0.0.1:5060;ftag=1233>";

            List<SIPContactHeader> sipContactHeaderList = SIPContactHeader.
                ParseContactHeader(testContactHeader);

            Assert.True(sipContactHeaderList[0].ContactName == null, 
                "The Contact header name was not parsed correctly.");
            Assert.True(sipContactHeaderList[0].ContactURI.ToString() == 
                "sip:user@127.0.0.1:5060;ftag=1233", 
                "The Contact header URI was not parsed correctly, parsed valued = " + 
                sipContactHeaderList[0].ContactURI.ToString() + ".");
            Assert.True(sipContactHeaderList[0].ContactURI.Parameters.Get("ftag") == "1233", 
                "The Contact header ftag URI parameter was not parsed correctly.");
        }

        [Fact]
        public void ParseExpiresContactHeaderUserTest()
        {
            string testContactHeader = "<sip:user@127.0.0.1:5060>; expires=60";

            List<SIPContactHeader> sipContactHeaderList = SIPContactHeader.
                ParseContactHeader(testContactHeader);

            Assert.True(sipContactHeaderList[0].ContactName == null, 
                "The Contact header name was not parsed correctly.");
            Assert.True(sipContactHeaderList[0].ContactURI.ToString() == 
                "sip:user@127.0.0.1:5060", "The Contact header URI was not parsed " +
                "correctly, parsed valued = " + sipContactHeaderList[0].ContactURI.
                ToString() + ".");
            Assert.True(sipContactHeaderList[0].Expires == 60, 
                "The Contact header Expires parameter was not parsed correctly.");
        }

        [Fact]
        public void ParseZeroExpiresContactHeaderUserTest()
        {
            string testContactHeader = "<sip:user@127.0.0.1:5060>; expires=0";
            List<SIPContactHeader> sipContactHeaderList = SIPContactHeader.
                ParseContactHeader(testContactHeader);

            Assert.True(sipContactHeaderList[0].ContactName == null, 
                "The Contact header name was not parsed correctly.");
            Assert.True(sipContactHeaderList[0].ContactURI.ToString() == 
                "sip:user@127.0.0.1:5060", "The Contact header URI was not parsed " + 
                "correctly, parsed valued = " + sipContactHeaderList[0].ContactURI.
                ToString() + ".");
            Assert.True(sipContactHeaderList[0].Expires == 0, 
                "The Contact header Expires parameter was not parsed correctly.");
        }

        [Fact]
        public void MultipleContactsHeaderUserTest()
        {
            string testContactHeader = "\"Mr. Watson\" <sip:watson@worcester.bell-telephone.com>;q=0.7; expires=3600, \"Mr. Watson\" <sip:watson@bell-telephone.com> ;q=0.1";

            List<SIPContactHeader> sipContactHeaderList = SIPContactHeader.
                ParseContactHeader(testContactHeader);

            Assert.True(sipContactHeaderList[0].ContactName == "Mr. Watson", 
                "The Contact header name was not parsed correctly.");
            Assert.True(sipContactHeaderList[0].ContactURI.ToString() == 
                "sip:watson@worcester.bell-telephone.com", "The Contact header " + 
                "URI was not parsed correctly, parsed valued = " + 
                sipContactHeaderList[0].ContactURI.ToString() + ".");
            Assert.True(sipContactHeaderList[0].Expires == 3600, 
                "The Contact header Expires parameter was not parsed correctly.");
            Assert.True(sipContactHeaderList[0].Q == "0.7", 
                "The Contact header Q parameter was not parsed correctly.");
        }

        [Fact]
        public void MultipleContactsWithURIParamsHeaderUserTest()
        {
            string testContactHeader = "\"Mr. Watson\" <sip:watson@worcester.bell-telephone.com;ftag=1232>;q=0.7; expires=3600, \"Mr. Watson\" <sip:watson@bell-telephone.com?nonsense=yes> ;q=0.1";

            List<SIPContactHeader> sipContactHeaderList = SIPContactHeader.
                ParseContactHeader(testContactHeader);

            Assert.True(sipContactHeaderList[0].ContactName == "Mr. Watson", 
                "The Contact header name was not parsed correctly.");
            Assert.True(sipContactHeaderList[0].ContactURI.ToString() == 
                "sip:watson@worcester.bell-telephone.com;ftag=1232", 
                "The Contact header URI was not parsed correctly, parsed valued = " + 
                sipContactHeaderList[0].ContactURI.ToString() + ".");
            Assert.True(sipContactHeaderList[0].Expires == 3600, 
                "The Contact header Expires parameter was not parsed correctly.");
            Assert.True(sipContactHeaderList[0].Q == "0.7", 
                "The Contact header Q parameter was not parsed correctly.");
            Assert.True(sipContactHeaderList[0].ContactURI.Parameters.Get("ftag") == 
                "1232", "The Contact header URI ftag parameter was not parsed correctly.");
            Assert.True(sipContactHeaderList[1].ContactURI.Headers.Get("nonsense") == 
                "yes", "The Contact header URI nonsense header was not parsed correctly.");
        }

        [Fact]
        public void SimpleAreEqualUserTest()
        {
            SIPContactHeader contactHeader1 = new SIPContactHeader(null, SIPURI.
                ParseSIPURI("sip:user@127.0.0.1:5060"));
            SIPContactHeader contactHeader2 = new SIPContactHeader(null, SIPURI.
                ParseSIPURI("sip:user@127.0.0.1:5060"));

            Assert.True(SIPContactHeader.AreEqual(contactHeader1, contactHeader2), 
                "The Contact headers were not correctly identified as equal.");
        }


        [Fact]
        public void SimpleNotEqualUserTest()
        {
            SIPContactHeader contactHeader1 = new SIPContactHeader(null, SIPURI.
                ParseSIPURI("sip:user@127.0.0.1:5060"));
            SIPContactHeader contactHeader2 = new SIPContactHeader(null, SIPURI.
                ParseSIPURI("sip:user@127.0.0.2:5060"));

            Assert.False(SIPContactHeader.AreEqual(contactHeader1, contactHeader2), 
                "The Contact headers were not correctly identified as equal.");
        }

        [Fact]
        public void WithParametersAreEqualUserTest()
        {
            SIPContactHeader contactHeader1 = new SIPContactHeader(SIPUserField.
                ParseSIPUserField("<sip:user@127.0.0.1:5060>;param1=value1"));
            SIPContactHeader contactHeader2 = new SIPContactHeader(SIPUserField.
                ParseSIPUserField("<sip:user@127.0.0.1:5060>;param1=value1"));

            Assert.True(SIPContactHeader.AreEqual(contactHeader1, contactHeader2), 
                "The Contact headers were not correctly identified as equal.");
        }

        [Fact]
        public void WithExpiresParametersAreEqualUserTest()
        {
            SIPContactHeader contactHeader1 = new SIPContactHeader(SIPUserField.
                ParseSIPUserField("<sip:user@127.0.0.1:5060> ;expires=0; " +
                "param1=value1"));
            SIPContactHeader contactHeader2 = new SIPContactHeader(SIPUserField.
                ParseSIPUserField("<sip:user@127.0.0.1:5060>;expires=50;param1=value1"));

            Assert.True(SIPContactHeader.AreEqual(contactHeader1, contactHeader2), 
                "The Contact headers were not correctly identified as equal.");
        }

        [Fact]
        public void WithDifferentNamesAreEqualUserTest()
        {
            SIPContactHeader contactHeader1 = new SIPContactHeader(SIPUserField.
                ParseSIPUserField("\"Joe Bloggs\" <sip:user@127.0.0.1:5060> ;expires=0; param1=value1"));
            SIPContactHeader contactHeader2 = new SIPContactHeader(SIPUserField.
                ParseSIPUserField("\"Jane Doe\" <sip:user@127.0.0.1:5060>;expires=50;param1=value1"));

            Assert.True(SIPContactHeader.AreEqual(contactHeader1, contactHeader2), 
                "The Contact headers were not correctly identified as equal.");
        }
    }
}