/////////////////////////////////////////////////////////////////////////////////////
//  File:   PcmuEncoder.cs                                          3 Jan 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Media;

/// <summary>
/// Class for encoding linear 16-bit PCM samples into PCMU (G.711 Mu-Law) samples
/// </summary>
public class PcmuEncoder : IAudioEncoder
{
    /// <summary>
    /// Encodes linear 16-bit PCM samples into PCMU (G.711 Mu-Law) samples
    /// </summary>
    /// <param name="InputSamples">Input linear 16-bit PCM samples</param>
    public byte[] Encode(short[] InputSamples)
    {
        byte[] EncodedOutput = new byte[InputSamples.Length];
        for (int i = 0; i < InputSamples.Length; i++)
            EncodedOutput[i] = MuLawEncoder.LinearToMuLawSample(InputSamples[i]);

        return EncodedOutput;
    }

    /// <summary>
    /// Gets the RTP clock rate in samples/second
    /// </summary>
    /// <value></value>
    public int ClockRate
    {
        get { return 8000; }
    }

    /// <summary>
    /// Gets the sample rate in samples/second
    /// </summary>
    /// <value></value>
    public int SampleRate
    {
        get { return 8000; }
    }

    /// <summary>
    /// Amount to increment the RTP packet Time Stamp field by for each new packet.
    /// </summary>
    public uint TimeStampIncrement
    {
        get { return 160; }
    }

    /// <summary>
    /// Closes the encoder so that it can release any memory or resources it has been using.
    /// </summary>
    public void CloseEncoder()
    {
    }
}
