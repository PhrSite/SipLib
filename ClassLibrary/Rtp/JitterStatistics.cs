/////////////////////////////////////////////////////////////////////////////////////
//  File:   JitterStatistics.cs                                     18 Dec 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Rtp;

/// <summary>
/// Container class for storing RTP packet jitter statistics for a sampled interval
/// </summary>
public class JitterStatistics
{
    /// <summary>
    /// Stores the maximum inter-packet jitter in milliseconds
    /// </summary>
    public int Maximum {  get; set; } = int.MinValue;

    /// <summary>
    /// Stores the minimum inter-packet jitter in milliseconds
    /// </summary>
    public int Minimum { get; set; } = int.MaxValue;

    /// <summary>
    /// Average jitter in milliseconds
    /// </summary>
    public int Average { get; set; } = 0;

    private long m_Sum = 0;
    private int m_SampleCount = 0;

    /// <summary>
    /// Updates the minimum and maximum jitter statistics. Call this method each time a new jitter value
    /// is calculated.
    /// </summary>
    /// <param name="NewJitterValue"></param>
    public void Update(int NewJitterValue)
    {
        m_SampleCount += 1;
        m_Sum += NewJitterValue;
        if (NewJitterValue < Minimum) Minimum = NewJitterValue;
        if (NewJitterValue > Maximum) Maximum = NewJitterValue;
    }

    /// <summary>
    /// Calculates the average jitter value for the sample interval. This method must be called at the end of the
    /// sample interval.
    /// </summary>
    public void CalculateAverage()
    {
        if (m_SampleCount == 0)
            Average = 0;
        else
            Average = (int) (Average / m_SampleCount);
    }

    /// <summary>
    /// Returns a deep copy of this object.
    /// </summary>
    /// <returns></returns>
    public JitterStatistics Copy()
    {
        JitterStatistics Js = new JitterStatistics();
        Js.Maximum = Maximum;
        Js.Minimum = Minimum;
        Js.Average = Average;
        Js.m_SampleCount = m_SampleCount;

        return Js;
    }

    /// <summary>
    /// Resets the statistics to the default values.
    /// </summary>
    public void Reset()
    {
        Maximum = int.MinValue; 
        Minimum = int.MaxValue;
        Average = 0;
        m_Sum = 0;
        m_SampleCount = 0;
    }
}
