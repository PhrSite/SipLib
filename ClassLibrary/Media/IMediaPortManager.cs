/////////////////////////////////////////////////////////////////////////////////////
//  File:   IMediaPortManager.cs                                    10 Jun 26 PHE
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Media;

/// <summary>
/// Interface for classes that manage the allocation and de-allocation of media ports for RTP
/// media (voice, video and RTT) and MSRP media.
/// </summary>
public interface IMediaPortManager
{
    /// <summary>
    /// Gets the next available port for audio media.
    /// </summary>
    public int NextAudioPort { get; }

    /// <summary>
    /// Gets the next available port for video media.
    /// </summary>
    public int NextVideoPort { get; }

    /// <summary>
    /// Gets the next available port for RTT media.
    /// </summary>
    public int NextRttPort { get; }

    /// <summary>
    /// Gets the next available port for MSRP media.
    /// </summary>
    public int NextMsrpPort { get; }

    /// <summary>
    /// Frees a media port for the specified media type.
    /// </summary>
    /// <param name="mediaType">Media type that the port was allocated for. Must be equal to one of
    /// the value specified in the MediaTypes class.</param>
    /// <param name="port">Port that was allocated for the specified media type.</param>
    public void FreeMediaPort(string mediaType, int port);

    /// <summary>
    /// Frees an audio port.
    /// </summary>
    /// <param name="port">Port that was allocated for audio.</param>
    public void FreeAudioPort(int port);

    /// <summary>
    /// Frees a video port.
    /// </summary>
    /// <param name="port">Port that was allocated for video.</param>
    public void FreeVideoPort(int port);

    /// <summary>
    /// Frees a RTT port.
    /// </summary>
    /// <param name="port">Port that was allocated for RTT.</param>
    public void FreeRttPort(int port);

    /// <summary>
    /// Frees a MSRP port.
    /// </summary>
    /// <param name="port">Port that was </param>
    public void FreeMsrpPort(int port);

}
