/////////////////////////////////////////////////////////////////////////////////////
//  File:   MediaPortManager.cs                                     25 Feb 24 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Media;

/// <summary>
/// Class for managing allocation of UDP and TCP ports for audio, video, RTT and MSRP media.
/// </summary>
public class MediaPortManager
{
    private MediaPortSettings m_Settings;
    private object m_Lock = new object();

    private int m_NextAudioPort;
    private int m_NextVideoPort;
    private int m_NextRttPort;
    private int m_NextMsrpPort;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="settings">Media port range allocations for each type of media.</param>
    public MediaPortManager(MediaPortSettings settings)
    {
        m_Settings = settings;
        m_NextAudioPort = m_Settings.AudioPorts.StartPort;
        m_NextVideoPort = m_Settings.VideoPorts.StartPort;
        m_NextRttPort = m_Settings.RttPorts.StartPort;
        m_NextMsrpPort = m_Settings.MsrpPorts.StartPort;
    }

    /// <summary>
    /// Gets the next port to use for audio media. The return value is for audio media. The return value + 1
    /// may be used for RTCP for the audio stream.
    /// </summary>
    /// <value></value>
    public int NextAudioPort
    {
        get
        {
            return GetNextPort(ref m_NextAudioPort, m_Settings.AudioPorts, 2);
        }
    }

    /// <summary>
    /// Gets the next port to use for video media. The return value is for video media. The return value + 1
    /// may be used for RTCP for the video stream.
    /// </summary>
    /// <value></value>
    public int NextVideoPort
    {
        get
        {
            return GetNextPort(ref m_NextVideoPort, m_Settings.VideoPorts, 2);
        }
    }

    /// <summary>
    /// Gets the next port to use for RTT media. The return value is for RTT media. The return value + 1
    /// may be used for RTCP for the RTT stream.
    /// </summary>
    /// <value></value>
    public int NextRttPort
    {
        get
        {
            return GetNextPort(ref m_NextRttPort, m_Settings.RttPorts, 2);
        }
    }

    /// <summary>
    /// Gets the next port to use for MSRP media. RTCP is not used for MSRP so only the return value may
    /// be used for the MSRP media.
    /// </summary>
    /// <value></value>
    public int NextMsrpPort
    {
        get
        {
            return GetNextPort(ref m_NextMsrpPort, m_Settings.MsrpPorts, 1);
        }
    }

    private int GetNextPort(ref int CurrentPort, PortRange range, int increment)
    {
        int Port;
        lock (m_Lock)
        {
            Port = CurrentPort;
            CurrentPort += increment;
            if (CurrentPort >= range.StartPort + range.Count)
                CurrentPort = range.StartPort;
        }

        return Port;
    }

}

