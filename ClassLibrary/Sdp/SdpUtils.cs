/////////////////////////////////////////////////////////////////////////////////////
//  File:   SdpUtils.cs                                             12 Sep 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Sdp;
using System.Net;

/// <summary>
/// 
/// </summary>
public static class SdpUtils
{
    /// <summary>
    /// Builds an Sdp for offering G.711 audio.
    /// </summary>
    /// <param name="iPAddress">Local IP address</param>
    /// <param name="Port">Specifies the UDP port number that audio will be sent and received on</param>
    /// <param name="UaName">User agent or server name</param>
    /// <returns>Returns an Sdp object with an audio media description</returns>
    public static Sdp BuildSimpleAudioSdp(IPAddress iPAddress, int Port, string UaName)
    {
        Sdp AudioSdp = new Sdp(iPAddress, UaName);
        AudioSdp.Media.Add(MediaDescription.CreateAudioSmd(Port));

        return AudioSdp;
    }
}
