/////////////////////////////////////////////////////////////////////////////////////
//  File:   SilenceAudioSampleSource.cs                             31 May 25 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Media;

/// <summary>
/// Class for sending silence audio samples. This class sends a block of samples with a value of 0 every 20 milliseconds.
/// Each sample is a 16-bit linear PCM format sample with a value of 0. The AudioSamplesReady event is fired every 20 milliseconds
/// after the Start() method is called.
/// <para>An instance of this class may be used to send silence to multiple calls. To do this, create a single instance
/// of this class, then call the Start() method once. multiple AudioSource classes can then hook the AudioSamples event to
/// receive the audio samples. Do not call the Stop() method until the application shuts down.</para>
/// </summary>
public class SilenceAudioSampleSource : IAudioSampleSource
{
    private const int PACKET_TIME_MS = 20;
    private const int SAMPLE_RATE = 8000;
    private const int SAMPLE_COUNT = (SAMPLE_RATE * PACKET_TIME_MS) / 1000;
    private Timer? m_Timer = null;
    private short[] m_PacketSamples;

    /// <summary>
    /// This event is fired every 20 milliseconds to provide new audio samples to send. Unhook this event when audio samples
    /// are no longer required.
    /// </summary>
    public event AudioSamplesReadyDelegate AudioSamplesReady;

    /// <summary>
    /// Constructor. Call the Start() method after calling the constructor.
    /// </summary>
    public SilenceAudioSampleSource()
    {
        m_PacketSamples = new short[SAMPLE_COUNT];
    }

    /// <summary>
    /// Starts sending audio sample packets.
    /// </summary>
    public void Start()
    {
        if (m_Timer == null)
            m_Timer = new Timer(OnTimerElapsed, null, 0, PACKET_TIME_MS);   // Starts the timer
    }

    /// <summary>
    /// Stops sending audio sample packets. This method must be called when this object is no longer required.
    /// </summary>
    public void Stop()
    {
        if (m_Timer != null)
        {
            m_Timer.Dispose();  // Stops the timer
            m_Timer = null;
        }
    }

    private void OnTimerElapsed(object? state)
    {
        AudioSamplesReady?.Invoke(m_PacketSamples, SAMPLE_RATE);
    }
}
