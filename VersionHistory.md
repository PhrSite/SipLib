# Version History

## v0.0.1 - 9 Sep 2024
| Issue No. | Change Type | Description |
|--------|--------|-------|
| NA       |  New      | Initial version |

## v0.0.2 - 16 Sep 2024
| Issue No. | Change Type | Description |
|--------|--------|-------|
| NA       |  Change  | Added a "user" parameter to SipUtils.CreateMsrpMediaDescription(). Changed the setupType to be required. |
| NA     | Change | Removed the OfferedSdp and AnsweredSdp parameters from MsrpConnection.CreateFromSdp() because they are not used. |
| NA      | Change | SipLib.Msrp.MsrpConnection -- Added a new private method calld SendEmptySendRequest(). |
| NA      | Change | SipLib.Msrp.MessageReceivedDelegate -- added the "from" parameter. |

## v0.0.3 - TBD
| Issue No. | Change Type | Description |
|--------|--------|-------|
| NA     | Fix    | Changed the SRTP authenticaion key length to 20 bytes to conform with Section 5.2 of RFC 3711 and fix the problems with RTP packet authentication with SDES-SRTP. |
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


