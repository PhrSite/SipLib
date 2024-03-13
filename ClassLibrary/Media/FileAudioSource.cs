/////////////////////////////////////////////////////////////////////////////////////
//  File:   FileAudioSource.cs                                      5 Mar 24 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Media;

/// <summary>
/// Class for sending audio samples that have been read from a wave file.
/// </summary>
public class FileAudioSource : IAudioSampleSource
{
    /// <summary>
    /// This event is fired every 20 milliseonds to provide an audio source with a new RTP block's worth
    /// of audio samples to send.
    /// </summary>
    public event AudioSamplesReadyDelegate? AudioSamplesReady = null;

    private AudioSampleData m_AudioSamples;
    private Timer? m_Timer = null;
    private HighResolutionTimer? m_HighResolutionTimer = null;
    private int m_CurrentPosition = 0;
    private int m_PacketSizeBytes;
    private short[] m_PacketBytes;
    private const int PACKET_TIME_MS = 20;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="audioSampleData">Contains the audio samples to send as read from a wave file.</param>
    /// <param name="highResolutionTimer">High resolution timer to use. If null, then a low resolution timer
    /// (System.Threading.Timer) will be used.</param>
    public FileAudioSource(AudioSampleData audioSampleData, HighResolutionTimer? highResolutionTimer)
    {
        m_AudioSamples = audioSampleData;
        m_HighResolutionTimer = highResolutionTimer;

        m_PacketSizeBytes = m_AudioSamples.SampleRate / (1000 / PACKET_TIME_MS);
        m_PacketBytes = new short[m_PacketSizeBytes];
    }

    /// <summary>
    /// Starts the timer for sending audio samples.
    /// </summary>
    public void Start()
    {
        if (m_HighResolutionTimer != null)
            m_HighResolutionTimer.TimerExpired += OnHighResolutionTimer;
        else
            m_Timer = new Timer(OnTimerElapsed, null, 0, PACKET_TIME_MS);
    }

    /// <summary>
    /// Stops the timer. No samples will be sent after this method returns.
    /// </summary>
    public void Stop()
    {
        if (m_Timer != null)
        {
            m_Timer.Dispose();
            m_Timer = null;
        }
        else if (m_HighResolutionTimer != null)
            m_HighResolutionTimer.TimerExpired -= OnHighResolutionTimer;
    }

    private void OnHighResolutionTimer()
    {
        OnTimerElapsed(null);
    }

    private void OnTimerElapsed(object? state)
    {
        for (int i=0; i < m_PacketSizeBytes; i++)
        {
            m_PacketBytes[i] = m_AudioSamples.SampleData[m_CurrentPosition++];
            if (m_CurrentPosition >= m_AudioSamples.SampleData.Length)
                m_CurrentPosition = 0;      // Wrap around
        }

        AudioSamplesReady?.Invoke(m_PacketBytes, m_AudioSamples.SampleRate);
    }
}
