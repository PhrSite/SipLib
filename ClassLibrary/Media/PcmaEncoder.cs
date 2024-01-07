/////////////////////////////////////////////////////////////////////////////////////
//  File: PcmaEncoder.cs                                            3 Jan 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

using NAudio.Codecs;

namespace SipLib.Media;

/// <summary>
/// Class for encoding linear 16-bit PCM samples into PCMA (G.711 A-Law) samples
/// </summary>
public class PcmaEncoder : IAudioEncoder
{
    /// <summary>
    /// Encodes linear 16-bit PCM samples into PCMA (G.711 A-Law) samples
    /// </summary>
    /// <param name="InputSamples">Input linear 16-bit Mu-Law samples</param>
    /// <param name="EncodedOutput">Destination of the encoded samples</param>
    /// <param name="OutputOffset">Starting offset index in the EncodedOutput array to start writing encoded
    /// samples at.</param>
    public void Encode(short[] InputSamples, byte[] EncodedOutput, int OutputOffset)
    {
        for (int i = 0; i < InputSamples.Length; i++)
            EncodedOutput[i + OutputOffset] = ALawEncoder.LinearToALawSample(InputSamples[i]);
    }
}
