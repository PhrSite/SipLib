/////////////////////////////////////////////////////////////////////////////////////
//  File:   IAudioDecoder.cs                                        24 Jan 24 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Media;

/// <summary>
/// Interface definition for an audio decoder
/// </summary>
public interface IAudioDecoder
{
    /// <summary>
    /// Decodes the input byte array into array of 16-bit PCMU audio samples.
    /// </summary>
    /// <param name="EncodedData">Input array of encoded audio from the payload of an RTP packet</param>
    /// <returns>Returns an array of linear 16-bit PCMU samples. Returns null if an error occurred.</returns>
    short[] Decode(byte[] EncodedData);

    /// <summary>
    /// Closes the decoder so that it can release any memory or resources it has been using.
    /// </summary>
    void CloseDecoder();

}
