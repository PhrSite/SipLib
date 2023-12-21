/////////////////////////////////////////////////////////////////////////////////////
//  File:   RtpSentStatistics.cs                                    15 Dec 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Rtp;

/// <summary>
/// Container class for sent RTP statistics within a fixed sample interval
/// </summary>
internal class RtpSentStatistics
{
    /// <summary>
    /// Number RTP packets that have been sent
    /// </summary>
    public uint PacketsSent { get; set; } = 0;

    /// <summary>
    /// Number of bytes that have been sent
    /// </summary>
    public uint BytesSent { get; set; } = 0;

    /// <summary>
    /// Timestamp of this sample in RTP timestamp units relative to the time of the first packet that was sent.
    /// </summary>
    public uint Timestamp { get; set; } = 0;

    /// <summary>
    /// Resets the statistics
    /// </summary>
    public void Reset()
    {
        PacketsSent = 0;
        BytesSent = 0;
        Timestamp = 0;
    }
}
