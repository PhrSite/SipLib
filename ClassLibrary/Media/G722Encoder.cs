/////////////////////////////////////////////////////////////////////////////////////
//  File:   G722Encoder.cs                                          23 Jan 24 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Media;

/// <summary>
/// Class for encoding audio samples using a G.722 encoder.
/// </summary>
public class G722Encoder : IAudioEncoder
{
    private G722Codec? m_Encoder = null;
    private G722CodecState? m_CodecState = null;

    /// <summary>
    /// Constructor
    /// </summary>
    public G722Encoder()
    {
        m_Encoder = new G722Codec();
        m_CodecState = new G722CodecState(64000, G722Flags.None);
    }

    /// <summary>
    /// Gets the clock rate
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
        get { return 16000; }
    }

    /// <summary>
    /// Amount to increment the RTP packet Time Stamp field by for each new packet.
    /// </summary>
    public uint TimeStampIncrement
    {
        get { return 160; }
    }

    /// <summary>
    /// Closes the encoder -- nothing to do for this codec.
    /// </summary>
    public void CloseEncoder()
    {
    }

    /// <summary>
    /// Encodes the input samples into G.722 encoded audio
    /// </summary>
    /// <param name="InputSamples">Raw input samples</param>
    /// <returns>Returns the encoded byte array.</returns>
    public byte[] Encode(short[] InputSamples)
    {
        if (m_Encoder == null)
            return null!;

        byte[] encodedBytes = new byte[InputSamples.Length / 2];
        int EncodedByteCount;

        try
        {
            EncodedByteCount = m_Encoder.Encode(m_CodecState, encodedBytes, InputSamples, InputSamples.Length);
        }
        catch
        {
        }

        return encodedBytes;
    }
}
