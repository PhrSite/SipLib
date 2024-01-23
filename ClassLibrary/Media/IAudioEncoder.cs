/////////////////////////////////////////////////////////////////////////////////////
//  File:   IAudioEncoder.cs                                        3 Jan 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Media;

/// <summary>
/// Interface definition for an audio encoder
/// </summary>
public interface IAudioEncoder
{
    /// <summary>
    /// Input sample array of 16-bit PCMU audio samples
    /// </summary>
    /// <param name="InputSamples">Input linear 16-bit Mu-Law samples</param>
    /// <returns>Returns an array of encoded sample bytes</returns>
    byte[] Encode(short[] InputSamples);

    /// <summary>
    /// Gets the clock rate (samples/second)
    /// </summary>
    int ClockRate { get; }

    /// <summary>
    /// Closes the encoder so that it can release any memory or resources it has been using.
    /// </summary>
    void CloseEncoder();
}
