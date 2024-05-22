/////////////////////////////////////////////////////////////////////////////////////
//  File:   VideoRtpReceiver.cs                                     19 Apr 24 PHR
/////////////////////////////////////////////////////////////////////////////////////

using SipLib.Rtp;

namespace SipLib.Video;

/// <summary>
/// Base class for classes that receive encoded RTP packets
/// </summary>
public class VideoRtpReceiver
{
    /// <summary>
    /// Constructor
    /// </summary>
    public VideoRtpReceiver()
    {
    }

    /// <summary>
    /// Processes a single RTP packet containing encoded video data.
    /// </summary>
    /// <param name="rtpPacket">Input RTP packet that has been received from the network.</param>
    /// <returns>Returns a byte array containing the encoded video for a complete video frame. Returns null
    /// if a full encoded frame is not ready yet.</returns>
    public virtual byte[]? ProcessRtpPacket(RtpPacket rtpPacket)
    {
        return null;
    }
}
