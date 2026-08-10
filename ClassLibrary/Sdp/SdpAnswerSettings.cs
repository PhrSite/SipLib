/////////////////////////////////////////////////////////////////////////////////////
//  File:   SdpAnswerSettings.cs                                    27 Feb 24 PHR
//
//  Revised:    11 Jun 26 PHR
//              -- Changed to use IMediaPortManager instead of MediaPortManager.
/////////////////////////////////////////////////////////////////////////////////////

using SipLib.Media;
using System.Net;

namespace SipLib.Sdp;

/// <summary>
/// Class for passing the media settings to use when building an SDP object in response to an
/// offered SDP
/// </summary>
public class SdpAnswerSettings
{
    /// <summary>
    /// If true, then answer with an audio media MediaDescription if audio media is offered. Else, reject
    /// the audio media.
    /// </summary>
    /// <value></value>
    public bool EnableAudio { get; set; } = true;

    /// <summary>
    /// If true, then answer with an video media MediaDescription if video media is offered. Else, reject
    /// the video media.
    /// </summary>
    /// <value></value>
    public bool EnableVideo { get; set; } = true;

    /// <summary>
    /// If true, then answer with an RTT media MediaDescription if RTT media is offered. Else, reject
    /// the RTT media.
    /// </summary>
    /// <value></value>
    public bool EnableRtt { get; set; } = true;

    /// <summary>
    /// If true, then answer with an MSRP media MediaDescription if MSRP media is offered. Else, reject
    /// the MSRP media.
    /// </summary>
    /// <value></value>
    public bool EnableMsrp { get; set; } = true;

    /// <summary>
    /// Contains a list of supported audio codecs. For example: "PCMU", "PCMA", "G722"
    /// </summary>
    /// <value></value>
    public List<string> SupportedAudioCodecs { get; set; }

    /// <summary>
    /// Contains a list of supported video codecs. For example: "H264", "VP8"
    /// </summary>
    /// <value></value>
    public List<string> SupportedVideoCodecs { get; set; }

    /// <summary>
    /// User name to use for the session owner and MSRP URI in the media descriptions
    /// </summary>
    /// <value></value>
    public string UserName { get; set; }

    /// <summary>
    /// Fingerprint of the self-signed X.509 certificate that that will be used for DTLS-SRTP keying
    /// material negotiation.
    /// <para>
    /// The RtpChannel class automatically creates a static X.509 certificate object that it will use for the
    /// DTLS handshake if a media channel uses DTLS-SRTP encryption. It is best to pass the static RtpChannel.CertificateFingerprint property
    /// for this property.
    /// </para>
    /// </summary>
    /// <value></value>
    public string Fingerprint { get; set; }

    /// <summary>
    /// MediaPortManager to use for allocation of media ports.
    /// </summary>
    /// <value></value>
    public IMediaPortManager PortManager { get; set; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="AudioCodecs">List of supported audio codecs</param>
    /// <param name="VideoCodecs">List of supported video codecs</param>
    /// <param name="userName">User name to use for the session owner and MSRP URI in the media descriptions</param>
    /// <param name="fingerprint">Fingerprint of the self-signed X.509 certificate that will be used for DTLS-SDES keying
    /// material negotiation. The RtpChannel class automatically creates a static X.509 certificate object that it will use for the
    /// DTLS handshake if a media channel uses DTLS-SRTP encryption. It is best to pass the static RtpChannel.CertificateFingerprint property
    /// for this parameter.
    /// </param>
    /// <param name="portManager">MediaPortManager to use use for allocation of media ports.</param>
    public SdpAnswerSettings(List<string> AudioCodecs, List<string> VideoCodecs, string userName, string fingerprint, 
        IMediaPortManager portManager)
    {
        SupportedAudioCodecs = AudioCodecs;
        SupportedVideoCodecs = VideoCodecs;
        UserName = userName;
        Fingerprint = fingerprint;
        PortManager = portManager;
    }
}
