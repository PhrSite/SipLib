/////////////////////////////////////////////////////////////////////////////////////
//  File:   PcmuDecoder.cs                                          24 Jan 24 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Media;

/// <summary>
/// Class for decoding PCMU (Mu-Law) encoded data into linear 16-bit PCM samples.
/// </summary>
public class PcmuDecoder : IAudioDecoder
{
    /// <summary>
    /// Closes the decoder. Not necessary for PCMU (Mu-Law) 
    /// </summary>
    public void CloseDecoder()
    {
    }

    /// <summary>
    /// Decodes the input byte array containing PCMU (Mu-Law) data and returns an array of audio samples.
    /// </summary>
    /// <param name="EncodedData">Input data to decode</param>
    /// <returns>Returns an array of linear 16-bit PCM audio data.</returns>
    public short[] Decode(byte[] EncodedData)
    {
        short[] Samples = new short[EncodedData.Length];
        for (int i = 0; i < EncodedData.Length; i++)
            Samples[i] = MuLawDecoder.MuLawToLinearSample(EncodedData[i]);

        return Samples;
    }
}
