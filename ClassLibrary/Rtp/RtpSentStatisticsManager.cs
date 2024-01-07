/////////////////////////////////////////////////////////////////////////////////////
//  File:   RtpSentStatisticsManager.cs                             15 Dec 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Rtp;

/// <summary>
/// Class for managing statistics about RTP data that has been sent.
/// </summary>
internal class RtpSentStatisticsManager
{
    private object m_LockObj = new object();

    private bool m_FirstPacketSent = false;
    private DateTime m_FirstPacketTime = DateTime.Now;
    private int m_SampleRate;
    private RtpSentStatistics m_SentStatistics;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="SampleRate">Samples per second for the media stream</param>
    public RtpSentStatisticsManager(int SampleRate)
    {
        m_SampleRate = SampleRate;
        m_SentStatistics = new RtpSentStatistics();
    }

    /// <summary>
    /// Updates the current statics for a RTP rtpPacket that was sent. This method must be called each time that
    /// an RTP rtpPacket is sent.
    /// </summary>
    /// <param name="rtpPacket">RTP packet that was just sent</param>
    public void Update(RtpPacket rtpPacket)
    {
        if (rtpPacket == null)
            return;

        lock (m_LockObj)
        {
            if (m_FirstPacketSent == false)
            {
                m_FirstPacketSent = true;
                m_FirstPacketTime = DateTime.Now;
            }

            m_SentStatistics.PacketsSent += 1;
            m_SentStatistics.BytesSent += (uint) rtpPacket.PacketBytes.Length;
            m_SentStatistics.Timestamp = rtpPacket.Timestamp;
        }
    }

    /// <summary>
    /// Gets a copy of the current statistics and resets them for the next sample period
    /// </summary>
    /// <returns>Returns a deep copy of the current statistics</returns>
    public RtpSentStatistics GetCurrentStatistics()
    {
        RtpSentStatistics result = new RtpSentStatistics();

        lock (m_LockObj)
        {
            result.PacketsSent = m_SentStatistics.PacketsSent;
            result.BytesSent = m_SentStatistics.BytesSent;
            result.Timestamp = m_SentStatistics.Timestamp;
            m_SentStatistics.Reset();
        }

        return result;
    }
}
