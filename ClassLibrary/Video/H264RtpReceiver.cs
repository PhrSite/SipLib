/////////////////////////////////////////////////////////////////////////////////////
//  File:   H264RtpReceiver.cs                                      18 Oct 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

using SipLib.Rtp;

namespace SipLib.Video;

/// <summary>
/// Class that processes RTP packets containing H264 encoded video data.
/// </summary>
public class H264RtpReceiver
{
    H264Depacketiser m_Depacketiser = null;

    /// <summary>
    /// Constructor
    /// </summary>
    public H264RtpReceiver()
    {
        m_Depacketiser = new H264Depacketiser();
    }

    /// <summary>
    /// Processes a new RTP packet containing an H264 NAL.
    /// </summary>
    /// <param name="rtpPacket">RTP packet to process</param>
    /// <returns>Returns a byte array containing a complete H264 access unit frame when a full frame
    /// has been received. Returns null if a full frame is not ready yet.</returns>
    public byte[] ProcessRtpPacket(RtpPacket rtpPacket)
    {
        int markerBit = rtpPacket.Marker == true ? 1 : 0;
        MemoryStream frameStream = m_Depacketiser.ProcessRTPPayload(rtpPacket.Payload, rtpPacket.SequenceNumber, 
            rtpPacket.Timestamp, markerBit, out bool isKeyFrame);

        if (frameStream != null)
            return frameStream.ToArray();
        else
            return null;
    }

}
