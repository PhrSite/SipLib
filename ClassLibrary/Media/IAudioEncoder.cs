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
    /// <param name="EncodedOutput">Destination of the encoded samples.</param>
    /// <param name="OutputOffset">Starting offset index in the EncodedOutput array to start writing encoded
    /// samples at.</param>
    void Encode(short[] InputSamples, byte[] EncodedOutput, int OutputOffset);
}
