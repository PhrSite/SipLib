/////////////////////////////////////////////////////////////////////////////////////
//  File:   BodyParsingUnitTests.cs                                 29 Nov 22 PHR
/////////////////////////////////////////////////////////////////////////////////////

using SipLib.Sdp;
using SipLib.Core;
using SipLib.Body;
using Ng911Lib.Utilities;
using AdditionalData;
using Pidf;

namespace SipLibUnitTests
{
    [Trait("Category", "unit")]
    public class BodyParsingUnitTests
    {
        /// <summary>
        /// Specifies the path to the files containing the test SIP messages.
        /// Change this if the project location or the location of the test files change.
        /// </summary>
        private const string Path = @"..\..\..\SipMessages\";

        public BodyParsingUnitTests(Xunit.Abstractions.ITestOutputHelper output)
        {
        }

        /// <summary>
        /// Helper function to read a test case SIP message file into a byte array.
        /// </summary>
        /// <param name="strFileName">File name to read</param>
        /// <returns>Returns a byte array containing a SIP message if successful</returns>
        private byte[] GetRawData(string strFileName)
        {
            string strFilePath = $"{Path}{strFileName}";
            Assert.True(File.Exists(strFilePath), $"The {strFileName} input file was missing.");
            byte[] Bytes = File.ReadAllBytes(strFilePath);
            return Bytes;
        }

        [Fact]
        public void InviteAllAdditionalDataByValue1()
        {
            byte[] Bytes = GetRawData("InviteAllAdditionalDataByValue1.sip");
            Assert.NotNull(Bytes);
            SIPMessage Msg = SIPMessage.ParseSIPMessage(Bytes, null, null);
            Assert.NotNull(Msg);
            SIPRequest Req = SIPRequest.ParseSIPRequest(Msg);
            Assert.NotNull(Req);

            List<MessageContentsContainer> Contents = BinaryBodyParser.ParseSipBody(Bytes, Req.Header.
                ContentType);
            Assert.NotNull(Contents);

            Assert.True(Contents.Count == 9, "The number of contents blocks is wrong");

            Assert.True(Contents[0].ContentType == "application/sdp", "The first content type is wrong");
            Sdp sdp = Sdp.ParseSDP(Contents[0].ToString());
            Assert.NotNull(sdp);

            Assert.True(Contents[1].ContentType == SchemaConsts.PidfContentType, 
                "The second content type is wrong");
            Presence Pres = (Presence) XmlHelper.DeserializeFromString(Contents[1].StringContents,
                typeof(Presence));
            Assert.True(Pres != null, "Error deserializing the PIDF-LO Presence document");
            
            Assert.True(Contents[2].ContentType == SchemaConsts.ProviderInfoContentType,
                "The third content type is wrong");
            ProviderInfoType Pi = (ProviderInfoType)XmlHelper.DeserializeFromString(Contents[2].StringContents,
                typeof(ProviderInfoType));
            Assert.True(Pi != null, "Error deserializing the ProviderInfo XML document");

            Assert.True(Contents[3].ContentType == SchemaConsts.ServiceInfoContentType,
                "The fourth content type is wrong");
            ServiceInfoType Si = (ServiceInfoType)XmlHelper.DeserializeFromString(Contents[3].StringContents,
                typeof(ServiceInfoType));
            Assert.True(Si != null, "Error deserializing the ServiceInfo XML document");

            Assert.True(Contents[4].ContentType == SchemaConsts.DeviceInfoContentType,
                "The fifth content type is wrong");
            DeviceInfoType Di = (DeviceInfoType)XmlHelper.DeserializeFromString(Contents[4].StringContents,
                typeof(DeviceInfoType));
            Assert.True(Di != null, "Error deserializing the DeviceInfo XML document");

            Assert.True(Contents[5].ContentType == SchemaConsts.SubscriberInfoContentType,
                "The sixth content type is wrong");
            SubscriberInfoType SubInfo = (SubscriberInfoType)XmlHelper.DeserializeFromString(
                Contents[5].StringContents, typeof(SubscriberInfoType));
            Assert.True(SubInfo != null, "Error deserializing the SubscriberInfo XML document");

            Assert.True(Contents[6].ContentType == SchemaConsts.CommentContentType,
                "The seventh content type is wrong");
            CommentType Ct = (CommentType)XmlHelper.DeserializeFromString(Contents[6].StringContents,
                typeof(CommentType));
            Assert.True(Ct != null, "Error deserializing the Comment XML document");

            // Skip for now
            Assert.True(Contents[7].ContentType == "application/EmergencyCallData.NENA-CallerInfo+xml",
                "The eigth content type is wrong");

            // Skip for now
            Assert.True(Contents[8].ContentType == "application/EmergencyCallData.NENA-LocationInfo+xml",
                "The ninth content type is wrong");
        }
    }
}
