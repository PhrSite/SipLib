/////////////////////////////////////////////////////////////////////////////////////
//  File:   VP8RtpSender.cs                                         19 Oct 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Video;

using SipLib.Rtp;

/// <summary>
/// Class for processing VP8 encoding video frames and sending them in RTP packets.
/// </summary>
public class VP8RtpSender
{
    private ushort m_SequenceNumber = 0;
    private uint m_Timestamp = 0;
    private uint m_SSRC = 0;
    private int m_PayloadType = 0;
    private uint m_TimestampIncrement = 0;
    private RtpSendDelegate RtpSender = null;

    private const uint VP8_CLOCK_RATE = 90000;
    private static Random m_Rnd = new Random();
    private const int MAX_RTP_PAYLOAD = 1200;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="payloadType">RTP payload number to use</param>
    /// <param name="frameRate">Video frame rate in frames per second</param>
    /// <param name="sender">Delegate to use to send RTP packets.</param>
    public VP8RtpSender(int payloadType, uint frameRate, RtpSendDelegate sender)
    {
        m_PayloadType = payloadType;
        m_TimestampIncrement = VP8_CLOCK_RATE / frameRate;
        RtpSender = sender;
        m_SSRC = Convert.ToUInt32(m_Rnd.Next());
    }

    /// <summary>
    /// Processes a VP8 encoded video frame and sends it as multiple RTP packets if the frame is longer
    /// than MAX_RTP_PAYLOAD bytes long.
    /// </summary>
    /// <param name="buffer">Input byte array containg a VP8 encoded video frame.</param>
    public void ProcessVP8Frame(byte[] buffer)
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

        m_Timestamp += m_TimestampIncrement;
    }

    private void SendRtpPacket(byte[] payload, bool markerBit)
    {
        RtpPacket rtpPacket = new RtpPacket(payload.Length);
        rtpPacket.SSRC = m_SSRC;
        rtpPacket.SequenceNumber = m_SequenceNumber;
        rtpPacket.Timestamp = m_Timestamp;
        rtpPacket.Marker = markerBit;
        rtpPacket.PayloadType = m_PayloadType;
        rtpPacket.Payload = payload;
        RtpSender?.Invoke(rtpPacket);

        m_SequenceNumber += 1;
    }
}
