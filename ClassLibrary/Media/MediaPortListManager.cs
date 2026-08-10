/////////////////////////////////////////////////////////////////////////////////////
//  File:   MediaPortListManager.cs                                 10 Jun 26 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Media;

/// <summary>
/// Class for managing allocation of UDP and TCP ports for audio, video, RTT and MSRP media. This class
/// allocates ports from a list of available ports and frees the allocated ports by returning them to
/// the list when a call ends.
/// <para>
/// User agents that use this class are responsible for freeing allocated ports by calling one of the 
/// Free*() methods.
/// </para>
/// <para>
/// All of the public methods and properties of this class are thread-safe.
/// </para>
/// </summary>
public class MediaPortListManager : IMediaPortManager
{
    private MediaPortSettings m_Settings;
    private object m_Lock = new object();

    private List<int> m_FreeAudioPorts = new List<int>();
    private List<int> m_FreeVideoPorts = new List<int>();
    private List<int> m_FreeRttPorts = new List<int>();
    private List<int> m_FreeMsrpPorts = new List<int>();

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="settings">Specifies the port ranges to allocate for each media type.</param>
    public MediaPortListManager(MediaPortSettings settings)
    {
        m_Settings = settings;
        BuildRtpFreePortList(m_FreeAudioPorts, m_Settings.AudioPorts);
        BuildRtpFreePortList(m_FreeVideoPorts, m_Settings.VideoPorts);
        BuildRtpFreePortList(m_FreeRttPorts, m_Settings.RttPorts);
        BuildMsrpFreePortList(m_FreeMsrpPorts, m_Settings.MsrpPorts);
    }

    private void BuildRtpFreePortList(List<int> portList, PortRange range)
    {
        // Allocate even ports in the range. Odd ports can be used for RTCP.
        for (int i = 0; i < range.Count; i += 2)
            portList.Add(i + range.StartPort);
    }

    private void BuildMsrpFreePortList(List<int> portList, PortRange range)
    {
        // Allocate all ports in the range for MSRP media.
        for (int i = 0; i < range.Count; i += 1)
            portList.Add(i + range.StartPort);
    }

    /// <summary>
    /// Gets the next available port for video media. 
    /// <para>Always returns an even port number. The odd port number can be used for RTCP.</para>
    /// <para>Returns 0 if there are no free audio ports available.</para>
    /// </summary>
    public int NextAudioPort
    {
        get
        {
            int port = 0;
            lock (m_Lock)
            {
                port = GetFreePort(m_FreeAudioPorts);
            }

            return port;
        }
    }

    private int GetFreePort(List<int> ports)
    {
        int port = 0;
        if (ports.Count > 0)
        {
            port = ports[0];
            ports.RemoveAt(0);
        }

        return port;
    }

    /// <summary>
    /// Gets the next available port for video media.
    /// <para>Always returns an even port number. The odd port number can be used for RTCP.</para>
    /// <para>Returns 0 if there are no free video ports available.</para>
    /// </summary>
    public int NextVideoPort
    {
        get
        {
            int port = 0;
            lock (m_Lock)
            {
                port = GetFreePort(m_FreeVideoPorts);
            }

            return port;
        }
    }

    /// <summary>
    /// Gets the next available port for RTT media.
    /// <para>Always returns an even port number. The odd port number can be used for RTCP.</para>
    /// <para>Returns 0 if there are no free RTT ports available.</para>
    /// </summary>
    public int NextRttPort
    {
        get
        {
            int port = 0;
            lock (m_Lock)
            {
                port = GetFreePort(m_FreeRttPorts);
            }

            return port;
        }
    }

    /// <summary>
    /// Gets the next available port for MSRP media.
    /// <para>Returns 0 if there are no free MSRP ports available.</para>
    /// </summary>
    public int NextMsrpPort
    {
        get
        {
            int port = 0;
            lock (m_Lock)
            {
                GetFreePort(m_FreeMsrpPorts);
            }

            return port;
        }
    }

    /// <summary>
    /// Frees an audio port.
    /// </summary>
    /// <param name="port">Port that was allocated for audio.</param>
    public void FreeAudioPort(int port)
    {
        lock (m_Lock)
        {
            FreePort(m_FreeAudioPorts, port);
        }
    }

    private void FreePort(List<int> ports, int port)
    {
        if (ports.Contains(port) == false)
            ports.Add(port);
    }

    /// <summary>
    /// Frees a video port.
    /// </summary>
    /// <param name="port">Port that was allocated for video.</param>
    public void FreeVideoPort(int port)
    {
        lock (m_Lock)
        {
            FreePort(m_FreeVideoPorts, port);
        }
    }

    /// <summary>
    /// Frees a RTT port.
    /// </summary>
    /// <param name="port">Port that was allocated for RTT.</param>
    public void FreeRttPort(int port)
    {
        lock (m_Lock)
        {
            FreePort(m_FreeRttPorts, port);
        }
    }

    /// <summary>
    /// Frees a MSRP port.
    /// </summary>
    /// <param name="port">Port that was </param>
    public void FreeMsrpPort(int port)
    {
        lock (m_Lock)
        {
            FreePort(m_FreeMsrpPorts, port);
        }
    }

    /// <summary>
    /// Frees a media port for the specified media type.
    /// </summary>
    /// <param name="mediaType">Media type that the port was allocated for. Must be equal to one of
    /// the value specified in the MediaTypes class.</param>
    /// <param name="port">Port that was allocated for the specified media type.</param>
    public void FreeMediaPort(string mediaType, int port)
    {
        switch (mediaType)
        {
            case MediaTypes.Audio:
                FreeAudioPort(port);
                break;
            case MediaTypes.Video:
                FreeVideoPort(port);
                break;
            case MediaTypes.RTT:
                FreeRttPort(port);
                break;
            case MediaTypes.MSRP:
                FreeMsrpPort(port);
                break;
        }
    }

    /// <summary>
    /// Gets the current number of free media ports. This method is for diagnostic purposes only.
    /// </summary>
    /// <param name="mediaType">Type of media to get the free port count for. For example MediaTypes.Audio.</param>
    /// <returns>Returns the number of free media ports.</returns>
    public int GetFreePortCount(string mediaType)
    {
        int FreePorts = 0;
        lock (m_Lock)
        {
            switch (mediaType)
            {
                case MediaTypes.Audio:
                    FreePorts = m_FreeAudioPorts.Count * 2;
                    break;
                case MediaTypes.Video:
                    FreePorts = m_FreeVideoPorts.Count * 2;
                    break;
                case MediaTypes.RTT:
                    FreePorts = m_FreeRttPorts.Count * 2;
                    break;
                case MediaTypes.MSRP:
                    FreePorts = m_FreeMsrpPorts.Count;
                    break;
            }
        }

        return FreePorts;
    }
}
