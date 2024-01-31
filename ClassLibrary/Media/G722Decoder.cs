/////////////////////////////////////////////////////////////////////////////////////
//  File:   G722Decoder.cs                                          24 Jan 24 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Media;

/// <summary>
/// Class for decoding G.722 encoded data into linear 16-bit PCM samples.
/// </summary>
public class G722Decoder : IAudioDecoder
{
    private G722CodecState m_CodecState;
    private int SAMPLES_PER_PACKET = 320;
    private G722Codec m_Codec;

    /// <summary>
    /// Constructor
    /// </summary>
    public G722Decoder()
    {
        m_CodecState = new G722CodecState(64000, G722Flags.None);
        m_Codec = new G722Codec();
    }

    /// <summary>
    /// Closes the decoder. Not necessary for G.722 
    /// </summary>
    public void CloseDecoder()
    {
    }

    /// <summary>
    /// Decodes the input byte array containing G.722 encoded data and returns an array of audio samples.
    /// </summary>
    /// <param name="EncodedData">Input data to decode</param>
    /// <returns>Returns an array of linear 16-bit PCM audio data.</returns>
    public short[] Decode(byte[] EncodedData)
    {
        short[] Samples = new short[SAMPLES_PER_PACKET];
        try
        {
            m_Codec.Decode(m_CodecState, Samples, EncodedData, EncodedData.Length);
        }
        catch { }

        return Samples;
    }

}
