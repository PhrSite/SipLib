/////////////////////////////////////////////////////////////////////////////////////
//  File:   RtpReceiveStatisticsManager.cs                          18 Dec 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Rtp;

internal class RtpReceiveStatisticsManager
{
    private object m_LockObj = new object();    // For locking this object
    private int m_SampleRate;
    private int m_PacketsPerSecond;
    private bool m_FirstPacketReceived = false;
    private DateTime m_FirstPacketTime = DateTime.MinValue;

    /// <summary>
    /// Time in RTP timestamp units that previous RTP rtpPacket that was received. See Section 6.4.1 of RFC 3550.
    /// </summary>
    /// 
    private long m_Last_R = 0;
    /// <summary>
    /// Timestamp value from the previous RTP rtpPacket. See Section 6.4.1 of RFC 3550.
    /// </summary>
    private long m_Last_S = 0;

    /// <summary>
    /// Previous smoothed jitter value J-1. See Section 6.4.1 of RFC 3550.
    /// </summary>
    private long m_Last_J = 0;

    /// <summary>
    /// Current smothed jitter value J. See Section 6.4.1 of RFC 3550.
    /// </summary>
    private long m_Cur_J = 0;

    /// <summary>
    /// Sequence number from the previous RTP rtpPacket.
    /// </summary>
    private ushort m_Last_SEQ = 0;

    private uint m_FirstSSRC = 0;

    private uint m_ExtendedLastSequenceNumber = 0;
    private RtpReceiveStatistics m_ReceiveStatics;
    private DateTime m_LastSampleTime = DateTime.Now;

    /// <summary>
    /// Stores the one way network delay to the endpoint that is sending the RTP packets in milliseconds.
    /// </summary>
    private int m_Delay = 0;

    /// <summary>
    /// Window for detecting packets out of order or missing using the rtpPacket sequence number. This corresponds 
    /// to 2000 * 0.20 seconds/rtpPacket = 40 seconds.
    /// </summary>
    private const ushort OutOfOrderThreshold = 2000;

    private string m_MediaType = "audio";

    /// <summary>
    /// Class for building RTP statistics for a media stream.
    /// </summary>
    /// <param name="SampleRate">The sample rate in samples per second for the
    /// media stream.</param>
    /// <param name="PacketsPerSecond">The expected number of RTP packets per second. For audio, this is typically 50.
    /// If the number of packets per second is not constant or is unknown, then this parameter should be
    /// set to 0.</param>
    /// <param name="mediaType">Media type of the media stream. QOS statistics (MOS and R) are only calculated for
    /// audio media streams</param>
    public RtpReceiveStatisticsManager(int SampleRate, int PacketsPerSecond, string mediaType)
    {
        m_SampleRate = SampleRate;
        if (m_SampleRate == 0)
            m_SampleRate = 8000;    // Defensive -- prevent divide by 0

        m_PacketsPerSecond = PacketsPerSecond;
        m_MediaType = mediaType;
        m_ReceiveStatics = new RtpReceiveStatistics();
    }

    /// <summary>
    /// Initializes the last sample time.
    /// </summary>
    /// <param name="Now"></param>
    public void InitializeSampleTime(DateTime Now)
    {
        m_LastSampleTime = Now;
    }

    /// <summary>
    /// Gets the sample rate in samples per second for the media type that this object is being used for.
    /// </summary>
    public int SampleRate
    {
        get { return m_SampleRate; }
    }

    /// <summary>
    /// Calculates the number of sequence numbers numbers taking wraparound into account.
    /// </summary>
    /// <param name="Seq1">The first or earlier sequence number.</param>
    /// <param name="Seq2">The second (later or most recent) sequence number.
    /// </param>
    /// <returns>Returns the difference between the sequence numbers.</returns>
    public static ushort ElapsedSeqNumbers(ushort Seq1, ushort Seq2)
    {
        if (Seq2 >= Seq1)
            return (ushort) (Seq2 - Seq1);
        else
            // Seq2 wrapped around.
            return (ushort)(ushort.MaxValue - Seq1 + Seq2 + 1);
    }

    /// <summary>
    /// Processes a new RTP rtpPacket and updates the current statistics. This function must be called for each RTP
    /// rtpPacket received for the media stream.
    /// </summary>
    /// <param name="rtpPacket">Byte array of the RTP rtpPacket.</param>
    public void Update(RtpPacket rtpPacket)
    {
        if (rtpPacket == null)
            return;     // Error: invalid parameter.

        Monitor.Enter(m_LockObj);

        if (rtpPacket.Version != 2)
        {   // Its not an RTP rtpPacket so ignore it
            Monitor.Exit(m_LockObj);
            return;
        }

        DateTime Now = DateTime.Now;
        if (m_FirstPacketReceived == false)
        {
            m_FirstPacketTime = Now;
            m_Last_S = rtpPacket.Timestamp;
            m_Last_R = 0;
            m_FirstPacketReceived = true;
            m_Last_SEQ = rtpPacket.SequenceNumber;
            m_ReceiveStatics.PacketsReceived += 1;
            m_ExtendedLastSequenceNumber = m_Last_SEQ;
            m_FirstSSRC = rtpPacket.SSRC;
            Monitor.Exit(m_LockObj);
            return;
        }

        ushort CurSEQ = rtpPacket.SequenceNumber;
        ushort ElapsedSeq = ElapsedSeqNumbers(m_Last_SEQ, CurSEQ);

        m_Last_J = m_Cur_J;
        TimeSpan Ts = Now - m_FirstPacketTime;

        m_ReceiveStatics.PacketsReceived += 1;
        if (ElapsedSeq < OutOfOrderThreshold)
        {   // Packets are in order so update the jitter statistics. See Section 6.4.1 of RFC 3550.
            long Cur_R = Convert.ToUInt32(Ts.TotalSeconds * m_SampleRate);
            long Cur_S = rtpPacket.Timestamp;
            long D = (Cur_R - Cur_S) - (m_Last_R - m_Last_S);
            m_Cur_J = m_Last_J + (Math.Abs(D) - m_Last_J) / 4;

            m_ReceiveStatics.SmoothedJitter.Update((int)(m_Cur_J * 1000 / m_SampleRate));
            m_ReceiveStatics.InstantaneousJitter.Update((int)(D * 1000) / m_SampleRate);

            if (ElapsedSeq != 1)
                // Some packets were dropped.
                m_ReceiveStatics.DroppedPackets += ElapsedSeq - 1;

            m_Last_R = Cur_R;
            m_Last_S = Cur_S;
            m_Last_SEQ = CurSEQ;
            m_ExtendedLastSequenceNumber += ElapsedSeq;
        }
        else
        {   // Packet is out of order.
            m_ReceiveStatics.OutOfOrderPackets += 1;
        }

        Monitor.Exit(m_LockObj);
    }

    /// <summary>
    /// Sets the one-way network delay in milliseconds.
    /// </summary>
    public int Delay
    {
        set { m_Delay = value; }
    }

    /// <summary>
    /// Gets the current statistics and resets the statistics for the next sample interval. This method
    /// should be called every few seconds (5 seconds is recommended for audio) to get the RTP RX statistics.
    /// </summary>
    public RtpReceiveStatistics CurrentStatistics
    {
        get
        {
            Monitor.Enter(m_LockObj);

            RtpReceiveStatistics Current = m_ReceiveStatics.Copy();
            Current.SampleTime = DateTime.Now;
            TimeSpan Ts = Current.SampleTime - m_LastSampleTime;
            m_LastSampleTime = Current.SampleTime;
            Current.SampleTimeMilliseconds = (int) Ts.TotalMilliseconds;
            Current.PacketsExpected = (int) (Current.SampleTimeMilliseconds * m_PacketsPerSecond) / 1000;
            Current.Jitter = (int)m_Cur_J;
            Current.ExtendedLastSequenceNumber = m_ExtendedLastSequenceNumber;
            Current.SSRC = m_FirstSSRC;
            Current.DelayInMilliseconds = m_Delay;

            // Calculate the rtpPacket loss percentage
            double PlPer = (1.0 - ((double)Current.PacketsReceived /
                (double)Current.PacketsExpected)) * 100.0;

            if (m_MediaType == "audio")
            {
                int PPJitter = Current.InstantaneousJitter.Maximum - Current.InstantaneousJitter.Minimum;
                Current.Mos = new MeanOpinionScore(PlPer, PPJitter, m_Delay);
            }

            Current.SmoothedJitter.CalculateAverage();
            Current.InstantaneousJitter.CalculateAverage();

            m_ReceiveStatics.Reset();

            Monitor.Exit(m_LockObj);
            return Current;
        }
    }
}
