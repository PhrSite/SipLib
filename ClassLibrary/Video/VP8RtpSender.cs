/////////////////////////////////////////////////////////////////////////////////////
//  File:   VP8RtpSender.cs                                         19 Oct 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Video;

/// <summary>
/// Class for processing VP8 encoding video frames and sending them in RTP packets.
/// </summary>
public class VP8RtpSender : VideoRtpSender
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="payloadType">RTP payload number to use</param>
    /// <param name="frameRate">Video frame rate in frames per second</param>
    /// <param name="sender">Delegate to use to send RTP packets.</param>
    public VP8RtpSender(int payloadType, uint frameRate, RtpSendDelegate sender) : base(payloadType, frameRate, sender)
    {
    }

    /// <summary>
    /// Processes a VP8 encoded video frame and sends it as multiple RTP packets if the frame is longer
    /// than MAX_RTP_PAYLOAD bytes long.
    /// </summary>
    /// <param name="buffer">Input byte array containg a VP8 encoded video frame.</param>
    public override void SendEncodedFrame(byte[] buffer)
    {
        for (int index = 0; index * MAX_RTP_PAYLOAD < buffer.Length; index++)
        {
            int offset = index * MAX_RTP_PAYLOAD;
            int payloadLength = (offset + MAX_RTP_PAYLOAD < buffer.Length) ? MAX_RTP_PAYLOAD : 
                buffer.Length - offset;

            byte[] vp8HeaderBytes = (index == 0) ? new byte[] { 0x10 } : new byte[] { 0x00 };
            byte[] payload = new byte[payloadLength + vp8HeaderBytes.Length];
            Buffer.BlockCopy(vp8HeaderBytes, 0, payload, 0, vp8HeaderBytes.Length);
            Buffer.BlockCopy(buffer, offset, payload, vp8HeaderBytes.Length, payloadLength);
            // Set marker bit for the last packet in the frame.
            bool markerBit = ((offset + payloadLength) >= buffer.Length) ? true : false;

            SendRtpPacket(payload, markerBit);
        }

        Timestamp += TimestampIncrement;
    }
}
