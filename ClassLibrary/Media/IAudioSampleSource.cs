/////////////////////////////////////////////////////////////////////////////////////
//  File:   IAudioSampleSource.cs                                   4 Mar 24 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Media;

/// <summary>
/// Delegate definition for the SendAudioSamples event of the IAudioSamplesSource interface.
/// </summary>
/// <param name="NewSamples">Block of new 16-bit linear PCM audio samples.</param>
/// <param name="SampleRate">Sample rate in samples/second of the data in the NewSamples array.</param>
public delegate void AudioSamplesReadyDelegate(short[] NewSamples, int SampleRate);

/// <summary>
/// Interface that must be implemented for each type of an audio sample source.
/// </summary>
public interface IAudioSampleSource
{
    /// <summary>
    /// This event is fired when a new block of 20 milliseconds worth of audio samples is available.
    /// </summary>
    public event AudioSamplesReadyDelegate AudioSamplesReady;

    /// <summary>
    /// Tells the audio sample source to start sending audio samples by firing the SendAudioSamples event.
    /// </summary>
    public void Start();

    /// <summary>
    /// Tells the audio sample source to stop sending audio samples.
    /// </summary>
    public void Stop();
}
