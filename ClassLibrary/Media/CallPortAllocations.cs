/////////////////////////////////////////////////////////////////////////////////////
//  File:   CallPortAllocations.cs                                  11 Jun 26 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Media;

using SipLib.Sdp;

/// <summary>
/// This class assists user agents in managing media ports that have been allocated for a call object.
/// <para>
/// This method should be used if a user agent is using the MediaPortListManager class to allocate
/// media ports for a call. It is not necessary to use this class if the user agent uses the MediaPortManager
/// class to allocate media ports.
/// </para>
/// <para>
/// To use this class, construct an instance of this class and save it with the call object. Then call
/// the AddAllocatedPortsFromSdp() method when the user agent constructs an Sdp object for that call. If
/// the user agent adds media to the call then it must call the AddAllocatedPortsFromMediaDescription() method for
/// each media being added to the call. When the call ends, the user agent must call the FreeAllocatedPorts() method.
/// </para>
/// </summary>
public class CallPortAllocations
{
    private IMediaPortManager m_MediaPortManager;

    /// <summary>
    /// The key is the media type (for example MediaType.Audio) the value is the list of ports that
    /// have been allocated for that media type.
    /// </summary>
    Dictionary<string, List<int>> m_MediaPorts = new Dictionary<string, List<int>>()
    {
        { MediaTypes.Audio, new List<int>() },
        { MediaTypes.Video, new List<int>() },
        { MediaTypes.RTT, new List<int>() },
        { MediaTypes.MSRP, new List<int>() }
    };

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="mediaPortManager">IMediaportManager object to use for freeing allocated ports
    /// for the call.</param>
    public CallPortAllocations(IMediaPortManager mediaPortManager)
    {
        m_MediaPortManager = mediaPortManager;
    }

    /// <summary>
    /// Call this method when an Sdp object is created for a call either to offer media for an outgoing
    /// call or to answer media offered for a incoming call.
    /// </summary>
    /// <param name="sdp">Sdp containing the offered or answered media for a call.</param>
    public void AddAllocatedPortsFromSdp(Sdp sdp)
    {
        foreach (MediaDescription mediaDescription in sdp.Media)
        {
            if (m_MediaPorts.ContainsKey(mediaDescription.MediaType) == true)
                m_MediaPorts[mediaDescription.MediaType].Add(mediaDescription.Port);
        }
    }

    /// <summary>
    /// Call this method when the user agent is adding media to a call.
    /// </summary>
    /// <param name="mediaDescription">MediaDescription for the media type being added to the call.</param>
    public void AddAllocatedPortsFromMediaDescription(MediaDescription mediaDescription) 
    {
        if (m_MediaPorts.ContainsKey(mediaDescription.MediaType) == true)
            m_MediaPorts[mediaDescription.MediaType].Add(mediaDescription.Port);
    }

    /// <summary>
    /// Frees all ports that have been allocated for a call. This method must be called when the call ends.
    /// </summary>
    public void FreeAllocatedPorts()
    {
        foreach (string mediaType in m_MediaPorts.Keys)
        {
            List<int> ports = m_MediaPorts[mediaType];
            foreach (int port in ports)
                m_MediaPortManager.FreeMediaPort(mediaType, port);
        }
    }
}
