/////////////////////////////////////////////////////////////////////////////////////
//  File:   PcmuEncoder.cs                                          3 Jan 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Media;
using NAudio.Codecs;

/// <summary>
/// Class for encoding linear 16-bit PCM samples into PCMU (G.711 Mu-Law) samples
/// </summary>
public class PcmuEncoder : IAudioEncoder
{
    /// <summary>
    /// Encodes linear 16-bit PCM samples into PCMU (G.711 Mu-Law) samples
    /// </summary>
    /// <param name="InputSamples">Input linear 16-bit Mu-Law samples</param>
    /// <param name="EncodedOutput">Destination of the encoded samples</param>
    /// <param name="OutputOffset">Starting offset index in the EncodedOutput array to start writing encoded
    /// samples at.</param>
    public void Encode(short[] InputSamples, byte[] EncodedOutput, int OutputOffset)
    {
        for (int i = 0; i < InputSamples.Length; i++)
            EncodedOutput[i + OutputOffset] = MuLawEncoder.LinearToMuLawSample(InputSamples[i]);
    }
}
