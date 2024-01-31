/////////////////////////////////////////////////////////////////////////////////////
//  File: PcmaDecoder.cs                                            24 Jan 24 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Media;

/// <summary>
/// Class for decoding PCMA (A-Law) encoded data into linear 16-bit PCM samples.
/// </summary>
public class PcmaDecoder : IAudioDecoder
{
    /// <summary>
    /// Closes the decoder. Not necessary for PCMA (A-Law) 
    /// </summary>
    public void CloseDecoder()
    { 
    }

    /// <summary>
    /// Decodes the input byte array containing PCMA (A-Law) data and returns an array of audio samples.
    /// </summary>
    /// <param name="EncodedData">Input data to decode</param>
    /// <returns>Returns an array of linear 16-bit PCM audio data.</returns>
    public short[] Decode(byte[] EncodedData)
    {
        short[] Samples = new short[EncodedData.Length];
        for (int i = 0; i < EncodedData.Length; i++)
            Samples[i] = ALawDecoder.ALawToLinearSample(EncodedData[i]);

        return Samples;
    }
}
