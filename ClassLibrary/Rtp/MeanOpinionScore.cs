/////////////////////////////////////////////////////////////////////////////////////
//  File:   MeanOpinionScore.cs                                     15 Dec 23 PHR
/////////////////////////////////////////////////////////////////////////////////////


namespace SipLib.Rtp;

/// <summary>
/// Class for calculating and storing the Mean Opionion Score (mos) values. The mos is a numerical
/// estimation of the audio quality.
/// </summary>
public class MeanOpinionScore
{
    /// <summary>
    /// mos estimate. The mos is in the range of 1.0 to 4.5. A value of 4.5 indicates the highest quality audio
    /// and a value of 1.0 is the lowest quality audio. A value of 0.0 indicates that the mos has not been
    /// calculated.
    /// </summary>
    public double MOS { get; set; } = 0.0;
    
    /// <summary>
    /// The R value is the rating value used to calculate the mos. It is calculated from the packet loss,
    /// the jitter and the delay.
    /// </summary>
    public double R {  get; set; } = 0.0;

    /// <summary>
    /// Default constructor
    /// </summary>
    public MeanOpinionScore()
    {
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="mos"></param>
    /// <param name="r"></param>
    public MeanOpinionScore(double mos, double r)
    {
        MOS = mos;
        R = r;
    }

    private const double R0 = 93.2;

    /// <summary>
    /// This constructor calculates the MOS and R values given the packet loss percentage, the jitter and
    /// the network delay in milliseconds using the algorithm described in "EMOS - Estimated Mean Opinion"
    /// Score. See https://arimas.com/2017/09/12/emos-estimated-mean-opinion-score/.
    /// </summary>
    /// <param name="PacketLossPercent"></param>
    /// <param name="Jitter"></param>
    /// <param name="DelayInMs"></param>
    public MeanOpinionScore(double PacketLossPercent, int Jitter, int DelayInMs)
    {
        // EL = Effective Latency
        double El = DelayInMs + Jitter * 2 + 10;

        if (El < 160)
            R = R0 - El / 40;
        else
            R = R0 - (El - 120) / 10;

        R = R - PacketLossPercent * 2.5;
        MOS = RFactorToMos(R);
    }

    private double RFactorToMos(double R)
    {
        double mos;
        if (R < 0.0)
            mos = 1.0;
        else if (R >= 0.0 && R <= 100.0)
            mos = 1.0 + 0.035 * R + R * (R - 60) * (100.0 - R) * 7.0e-6;
        else
            mos = 4.5;

        return mos;
    }

}
