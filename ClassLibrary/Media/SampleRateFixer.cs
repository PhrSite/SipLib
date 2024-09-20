/////////////////////////////////////////////////////////////////////////////////////
//  File:   SampleRateFixer.cs                                      18 Sep 24 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Media;

/// <summary>
/// This class contains function for doubling or halving the sample rate for audio packets.
/// </summary>
internal static class SampleRateFixer
{

    /// <summary>
    /// Doubles the sample rate by simple interpolation.
    /// </summary>
    /// <param name="source">Input samples</param>
    /// <returns>Output samples at double the sample rate.</returns>
    internal static short[] DoubleSampleRate(short[] source)
    {
        short[] dest = new short[source.Length * 2];
        // Copy the original sample points into the destination
        int i;
        for (i = 0; i < source.Length; i++)
            dest[i * 2] = source[i];

        i = dest.Length - 1;
        dest[i] = dest[i - 1];  // Copy the last point from the input into last point in the output

        // Now interpolate the points
        for (i = 2; i < dest.Length; i = i + 2)
        {
            int delta = (dest[i] - dest[i - 2]) / 2;
            dest[i - 1] = (short)(dest[i - 2] + delta);
        }

        return dest;
    }

    /// <summary>
    /// Reduces the sample rate by half by copying every other sample point into the destination.
    /// </summary>
    /// <param name="source">Input samples</param>
    /// <returns>Output samples at half the sample rate of the input.</returns>
    internal static short[] HalveSampleRate(short[] source)
    {
        int DestLength = source.Length / 2;
        short[] dest = new short[DestLength];
        int destIndex = 0;
        for (int i = 0; i < source.Length; i = i + 2)
            dest[destIndex++] = source[i];

        return dest;
    }
}
