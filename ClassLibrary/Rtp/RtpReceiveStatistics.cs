/////////////////////////////////////////////////////////////////////////////////////
//  File:   RtpReceiveStatistics.cs                                 15 Dec 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Rtp;

/// <summary>
/// Class for storing statistics related to received RTP packets.
/// </summary>
public class RtpReceiveStatistics
{
    /// <summary>
    /// Local time of the sample
    /// </summary>
    public DateTime SampleTime { get; set; } = DateTime.Now;

    /// <summary>
    /// Number of milliseconds since the previous sample was taken.
    /// </summary>
    public int SampleTimeMilliseconds { get; set; } = 0;

    /// <summary>
    /// Number of RTP packets that were received during this sample interval
    /// </summary>
    public int PacketsReceived { get; set; } = 0;

    /// <summary>
    /// The number of RTP packets that were expected during this interval
    /// </summary>
    public int PacketsExpected { get; set; } = 0;

    /// <summary>
    /// The number of dropped packets detected during this interval
    /// </summary>
    public int DroppedPackets {  get; set; } = 0;

    /// <summary>
    /// The number of packets that were received out of order
    /// </summary>
    public int OutOfOrderPackets { get; set; } = 0;

    /// <summary>
    /// Contains the smoothed jitter in timestamp units sampled at the time this sample was taken. 
    /// See Section 6.4.1 of RFC 3550.
    /// </summary>
    public int Jitter {  get; set; } = 0;

    /// <summary>
    /// Contains the minimum, average and maximum jitter values in milliseconds using the smoothed jitter
    /// calculation specified in Section 6.4.1 of RFC 3550.
    /// </summary>
    public JitterStatistics SmoothedJitter { get; set; } = new JitterStatistics();

    /// <summary>
    /// Contains the minimum, average and maximum jitter values in milliseconds of the instantaneous jitter
    /// calculation.
    /// </summary>
    public JitterStatistics InstantaneousJitter { get; set; } = new JitterStatistics();

    /// <summary>
    /// Contains the extend sequence (SEQ) number. See Section 6.4.1 of RFC 3550. This field is used for a RTCP
    /// Sender Report.
    /// </summary>
    public uint ExtendedLastSequenceNumber { get; set; } = 0;

    /// <summary>
    /// SSRC of the sender from the received RTP packet.
    /// </summary>
    public uint SSRC { get; set; } = 0;

    /// <summary>
    /// Contains the calculate Mean Opinion Score (MOS) for this interval
    /// </summary>
    public MeanOpinionScore Mos { get; set; } = new MeanOpinionScore();

    /// <summary>
    /// Estimated one-way delay in milliseconds that was used for the MOS
    /// calculations. This is calculated as the absolute value between the local NTP timestamp and the NTP
    /// timestamp contained in the Sender report that the remote endpoint sent.
    /// </summary>
    public int DelayInMilliseconds { get; set; } = 0;

    /// <summary>
    /// Creates a deep copy of this object
    /// </summary>
    /// <returns></returns>
    public RtpReceiveStatistics Copy()
    {
        RtpReceiveStatistics Rrs = new RtpReceiveStatistics();
        Rrs.SampleTime = SampleTime;
        Rrs.SampleTimeMilliseconds = SampleTimeMilliseconds;
        Rrs.PacketsReceived = PacketsReceived;
        Rrs.PacketsExpected = PacketsExpected;
        Rrs.DroppedPackets = DroppedPackets;
        Rrs.OutOfOrderPackets = OutOfOrderPackets;
        Rrs.Jitter = Jitter;
        Rrs.SmoothedJitter = SmoothedJitter.Copy();
        Rrs.InstantaneousJitter = InstantaneousJitter.Copy();
        Rrs.ExtendedLastSequenceNumber = ExtendedLastSequenceNumber;
        Rrs.SSRC = SSRC;
        Rrs.Mos = new MeanOpinionScore(Mos.MOS, Mos.R);
        Rrs.DelayInMilliseconds = DelayInMilliseconds;
        return Rrs;
    }

    /// <summary>
    /// Resets all statistics to the default value.
    /// </summary>
    public void Reset()
    {
        SampleTime = DateTime.Now;
        SampleTimeMilliseconds = 0;
        PacketsReceived = 0;
        PacketsExpected = 0;
        DroppedPackets = 0;
        OutOfOrderPackets = 0;
        Jitter = 0;
        ExtendedLastSequenceNumber = 0;
        SSRC = 0;
        Mos = new MeanOpinionScore();
        SmoothedJitter.Reset();
        InstantaneousJitter.Reset();
    }
}
