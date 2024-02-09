/////////////////////////////////////////////////////////////////////////////////////
//  File:   MediaPortSettings.cs                                    6 Feb 24 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Media;

/// <summary>
/// Stores the media port ranges for each media type.
/// </summary>
public class MediaPortSettings
{
    /// <summary>
    /// Port range for audio
    /// </summary>
    public PortRange AudioPorts { get; set; }
    /// <summary>
    /// Port range for video
    /// </summary>
    public PortRange VideoPorts { get; set; }
    /// <summary>
    /// Port range for Real Time Text (RTT)
    /// </summary>
    public PortRange RttPorts { get; set; }
    /// <summary>
    /// Port range for Message Session Relay Protocol (MSRP)
    /// </summary>
    public PortRange MsrpPorts { get; set; }

    /// <summary>
    /// Constructor. Sets up come defaults.
    /// </summary>
    public MediaPortSettings()
    {
        AudioPorts = new PortRange() { StartPort = 6000, Count = 1000 };
        VideoPorts = new PortRange() { StartPort = 7000, Count = 1000 };
        RttPorts = new PortRange() { StartPort = 8000, Count = 1000 };
        MsrpPorts = new PortRange() { StartPort = 9000, Count = 1000 };
    }
}

/// <summary>
/// Stores the port range for a single media type
/// </summary>
public class PortRange
{
    /// <summary>
    /// Starting port number
    /// </summary>
    public int StartPort { get; set; }

    /// <summary>
    /// Number of ports to allocate
    /// </summary>
    public int Count { get; set; }
}