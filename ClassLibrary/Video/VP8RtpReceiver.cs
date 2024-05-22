/////////////////////////////////////////////////////////////////////////////////////
//  File:   VP8RtpReceiver.cs                                       19 Oct 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Video;
using SipLib.Rtp;

/// <summary>
/// Class for processing RTP packets containing VP8 encode video frames.
/// </summary>
public class VP8RtpReceiver : VideoRtpReceiver
{
    private int _currVideoFramePosn = 0;

    private const int _maxFrameSize = 1048576;      // TBD
    private byte[] _currVideoFrame;

    /// <summary>
    /// Processes RTP packets and builds up a complete VP8 encoded video frame.
    /// </summary>
    /// <param name="rtpPacket">Input RTP packet</param>
    /// <returns>Returns a complete video frame containing VP8 encoded video data when a full frame is
    /// ready. Returns null if a full frame is not ready yet.</returns>
    public override byte[]? ProcessRtpPacket(RtpPacket rtpPacket)
    {
        byte[] payload = rtpPacket.Payload;
        if (_currVideoFramePosn + payload.Length >= _maxFrameSize)
        {   // Something has gone very wrong. Clear the buffer.
            _currVideoFramePosn = 0;
            _currVideoFrame = new byte[_maxFrameSize];

        }

        // New frames must have the VP8 Payload Descriptor Start bit set.
        // The tracking of the current video frame position is to deal with a VP8 frame being split across
        // multiple RTP packets as per https://tools.ietf.org/html/rfc7741#section-4.4.
        if (_currVideoFramePosn > 0 || (payload[0] & 0x10) > 0)
        {
            RtpVP8Header vp8Header = RtpVP8Header.GetVP8Header(payload);

            Buffer.BlockCopy(payload, vp8Header.Length, _currVideoFrame, _currVideoFramePosn, payload.Length - 
                vp8Header.Length);
            _currVideoFramePosn += payload.Length - vp8Header.Length;

            if (rtpPacket.Marker == true)
            {
                byte[] frame = _currVideoFrame.Take(_currVideoFramePosn).ToArray();
                _currVideoFramePosn = 0;
                return frame;
            }
        }
        else
        {   // Discard the packet because the VP8 header start bit is not set
        }

        return null;
    }
}
