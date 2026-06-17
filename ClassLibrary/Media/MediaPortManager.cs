/////////////////////////////////////////////////////////////////////////////////////
//  File:   MediaPortManager.cs                                     25 Feb 24 PHR
//
//  Revised:    11 Jun 26 PHR
//              -- Revised to implement the IMediaPortManager interface. This is not
//                 a breaking change.
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Media;

/// <summary>
/// Class for managing allocation of UDP and TCP ports for audio, video, RTT and MSRP media.
/// <para>
/// This class increments port numbers within a specified range of ports. When the last port in a
/// range is allocated, this class wraps around to the starting port number.
/// </para>
/// <para>
/// There is no need for the user agent using this class to free ports that have been used for a call.
/// </para>
/// </summary>
public class MediaPortManager : IMediaPortManager
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

    /// <summary>
    /// Frees a media port for the specified media type.
    /// </summary>
    /// <param name="mediaType">Media type that the port was allocated for. Must be equal to one of
    /// the value specified in the MediaTypes class.</param>
    /// <param name="port">Port that was allocated for the specified media type.</param>
    public void FreeMediaPort(string mediaType, int port)
    {
    }

    /// <summary>
    /// Frees an audio port.
    /// </summary>
    /// <param name="port">Port that was allocated for audio.</param>
    public void FreeAudioPort(int port)
    {
    }

    /// <summary>
    /// Frees a video port.
    /// </summary>
    /// <param name="port">Port that was allocated for video.</param>
    public void FreeVideoPort(int port)
    {
    }

    /// <summary>
    /// Frees a RTT port.
    /// </summary>
    /// <param name="port">Port that was allocated for RTT.</param>
    public void FreeRttPort(int port)
    {
    }

    /// <summary>
    /// Frees a MSRP port.
    /// </summary>
    /// <param name="port">Port that was </param>
    public void FreeMsrpPort(int port)
    {
    }


}

