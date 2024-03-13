/////////////////////////////////////////////////////////////////////////////////////
//  File:   AudioSampleDate.cs                                      5 Mar 24 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Media;

/// <summary>
/// Data clas for passing audio sample data to an audio sample source 
/// </summary>
public class AudioSampleData
{
    /// <summary>
    /// Audio samples. Each element is a 16-bit linear PCM sample.
    /// </summary>
    public short[] SampleData { get; private set; }

    /// <summary>
    /// Sample rate in samples/second of the data in the SampleData array.
    /// </summary>
    public int SampleRate { get; private set; }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="sampleData">Audio samples. Each sample is a 16-bit linear PCM sample.</param>
    /// <param name="sampleRate">Sample rate in samples/second of the data in the SampleData array.</param>
    public AudioSampleData(short[] sampleData, int sampleRate)
    {
        SampleData = sampleData;
        SampleRate = sampleRate;
    }
}
