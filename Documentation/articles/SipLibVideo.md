# The SipLib.Video Namespace

The SipLib.Video namespace provides classes for packetizing and de-packetizing H.264 and VP8 video that is sent and received as RTP packets using an RtpChannel.

The SipLib class library does not provide classes for the codecs for H.264 and VP8 video.

This namespace provides the following classes.

| Class Name | Description |
|--------|--------|
| [H264Depacketiser](~/api/SipLib.Video.H264Depacketiser.yml) | Receives H264 encoded video Network Access Layer (NALs) in RTP packets and builds complete H264 encoded access units that can then be decoded. |
| [H264Packetiser](~/api/SipLib.Video.H264Packetiser.yml) | Contains functions to packetise an H264 Network Abstraction Layer Units (NAL or NALU) into an RTP payload. See [RTP Payload Format for H.264 Video](https://tools.ietf.org/html/rfc6184) |
| [H264RtpReceiver](~/api/SipLib.Video.H264RtpReceiver.yml) | Processes RTP packets containing H264 encoded video data. |
| [H264RtpSender](~/api/SipLib.Video.H264RtpSender.yml) | Processes H264 encoded access units and packetizes H264 NAL units into RTP packets so the H264 encoded data can be sent over the network. |
| [RtpVP8Header](~/api/SipLib.Video.RtpVp8Header.yml) | VP8 RTP header as specified in [RFC7741](https://tools.ietf.org/html/rfc7741). |
| [VP8RtpReceiver](~/api/SipLib.Video.VP8RtpReceiver.yml) | Processes RTP packets containing VP8 encode video frames. |
| [VP8RtpSender](~/api/SipLib.Video.VP8RtpSender.yml) | Processes VP8 encoding video frames and for sending them in RTP packets. |
