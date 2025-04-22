# Version History

## v0.0.4 -- TBD
| Issue No. | Change Type | Description |
|--------|--------|-------|
| NA     | Change | SipLib.Media.G722Codec.cs -- Changed from public scope to internal scope |
| NA     | Fix    | The ServerInviteTransaction class was not calling the TransactionComplete delegate when the transaction user sent a final response (>= 200) for the INVITE to the client endpoint. |
| NA     | Addition | Added the SipLib.TestCalls namespace. This namespace contains classes for handling incoming and outgoing NG9-1-1 test calls. |


## v0.0.3 - 14 Mar 2025
| Issue No. | Change Type | Description |
|--------|--------|-------|
| NA     | Fix    | Changed the SRTP authentication key length to 20 bytes to conform with Section 5.2 of RFC 3711 and fixed the problems with RTP packet authentication with SDES-SRTP. |
| NA     | Addition | Finished coding for the AudioDestination and AudioSource classes in the SipLib.Media namespace. |
| NA     | Fix      | The RttClient test program in Samples/RTT/RttClient was not sending an in-dialog BYE request. |
| NA     | Fix      | The MsrpClient test program in Samples/MSRP/MsrpClient was not sending an in-dialog BYE request. |
| NA     | Fix      | The SipLib.Media.AudioSource class was calling the wrong RtpPacket constructor resulting in extra bytes being attached to each RTP packet. |
| NA     | Addition | Added GetCallInfoHeaderForPurpose(SIPHeader Sh, string strPurpose, SIPSchemesEnum ExcludeScheme) to the SipUtils class. |
| NA     | Addition | Added the MsrpMessageSent event to the MsrpConnection class. |
| NA     | Change   | Modified SipLib.Sdp.MediaDescription.CreateCopy() to create a deep copy of the entire MediaDescription object instead of just the m= line. |
| NA     | Addition | Added the Threading directory. This directory contains some base classes such as QueuedActionWorkerTask. |
| NA     | Change   | Modified the SipTransport class to send a 400 Bad Request response if a SIP request message can be parsed but is not valid. |
| NA     | Change   | Modified the SIPResponse.IsValid() method to check for the presence of a branch parameter in the Via header. |
| NA     | Addition | Added the Sdp.GetMediaByTypeAndLabel() method. |
| NA     | Fix      | Changed SipLib.Body.ContentTypes.ConferenceEvent from application/conference+xml to application/conference-info+xml to comply with Section 9.2 of RFC 4575. |
| NA     | Change   | Modified the RtpChannel class to ignore packets that do not have a Version field of 2. |
| NA     | Fix      | Modified the constructor of the Sdp class to set the UaName to a default value if it is not specified. |
| NA     | Fix      | Modified the constructor of the SdpOrigin class to set the UserName to a default value if it is not specified. |
| NA     | Addition | Added the SIPFrag class to the SipLib.Core namespace |
| NA     | Change   | Modified SIPMessage.GetContentsOfType() to catch any exceptions that occur when parsing the message body. |
| NA     | Change   | Changed the target framework to .NET 9. |
| NA     | Change   | Modified DtlsUtils.CreateSelfSignedCert() to use X509CertificateLoader.LoadCertificate() instead of using the X509Certificate2 constructor to load the X.509 certificate because this constructor is obsolete in .NET 9. |
| NA     | Change   | Modified DtlsUtils.ConvertBouncyCert() to use X509CertificateLoader.LoadPkcs12() instead of using the of X509Certificate constructor to load the X.509 certificate because this construtor is obsolete in .NET 9. |

## v0.0.2 - 16 Sep 2024
| Issue No. | Change Type | Description |
|--------|--------|-------|
| NA       |  Change  | Added a "user" parameter to SipUtils.CreateMsrpMediaDescription(). Changed the setupType to be required. |
| NA     | Change | Removed the OfferedSdp and AnsweredSdp parameters from MsrpConnection.CreateFromSdp() because they are not used. |
| NA      | Change | SipLib.Msrp.MsrpConnection -- Added a new private method calld SendEmptySendRequest(). |
| NA      | Change | SipLib.Msrp.MessageReceivedDelegate -- added the "from" parameter. |

## v0.0.1 - 9 Sep 2024
| Issue No. | Change Type | Description |
|--------|--------|-------|
| NA       |  New      | Initial version |




