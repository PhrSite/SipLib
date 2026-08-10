/////////////////////////////////////////////////////////////////////////////////////
//  File:   SdpOfferSettings.cs                                     14 Oct 25 PHR
//
//  Revised:    11 Jun 26 PHR
//              -- Changed to use IMediaPortManager instead of MediaPortManager.
/////////////////////////////////////////////////////////////////////////////////////

using Org.BouncyCastle.Bcpg.OpenPgp;
using SipLib.Media;
using SipLib.Rtp;
using System.Text.Json.Serialization;

namespace SipLib.Sdp;

/// <summary>
/// Class for passing the media settings to use when building an SDP to offer in a new call (INVITE) request.
/// </summary>
public class SdpOfferSettings
{
    /// <summary>
    /// If true then offer audio media. The default setting is true.
    /// </summary>
    public bool OfferAudio { get; set; } = true;

    /// <summary>
    /// If true, then offer video media. The default setting is false.
    /// </summary>
    public bool OfferVideo { get; set; }

    /// <summary>
    /// If true then offer Real Time Text (RTT) media The default setting is false.
    /// </summary>
    public bool OfferRtt { get; set; }

    /// <summary>
    /// If true, then offer Message Session Rely Protocol (MSRP) media. The default setting is false.
    /// </summary>
    public bool OfferMsrp { get; set; }

    /// <summary>
    /// Contains a list of audio codecs to offer. For example: "PCMU", "PCMA", "G722", etc. The default setting is "PCMU".
    /// </summary>
    public List<string> OfferAudioCodecs { get; set; } = new List<string>() { "PCMU" };

    /// <summary>
    /// Contains a list of video codecs to offer. For example: "H264", "VP8". The default setting is "H264".
    /// </summary>
    public List<string> OfferVideoCodecs { get; set; } = new List<string>() { "H264" };

    /// <summary>
    /// User name to use for the session owner and MSRP URI in the media descriptions
    /// </summary>
    /// <value></value>
    public string UserName { get; set; } = "DefaultUser";

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
    [JsonIgnore]
    public string Fingerprint { get; set; } = string.Empty;

    /// <summary>
    /// MediaPortManager to use for allocation of media ports.
    /// </summary>
    /// <value></value>
    public IMediaPortManager PortManager { get; set; } = new MediaPortManager(new MediaPortSettings());

    /// <summary>
    /// Specifies the type of media encryption to use for RTP type media (Audio, Video and RTT).
    /// </summary>
    public RtpEncryptionEnum RtpEncryptionType { get; set; } = RtpEncryptionEnum.None;

    /// <summary>
    /// If true, the use TLS for MSRP media (MSRPS). The default setting is false.
    /// </summary>
    public bool UseTlsForMsrp { get; set; } = false;

    /// <summary>
    /// Connection setup method for MSRP media. The default is SetupType.active for outgoing calls.
    /// </summary>
    public SetupType MsrpSetupType { get; set; } = SetupType.active;

    /// <summary>
    /// Default constructor
    /// </summary>
    public SdpOfferSettings()
    {
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="AudioCodecs">List of audio codecs to offer.</param>
    /// <param name="VideoCodecs">List of video codecs to offer.</param>
    /// <param name="userName">User name to use for the session owner and MSRP URI in the media descriptions</param>
    /// <param name="fingerprint">Fingerprint of the self-signed X.509 certificate that will be used for DTLS-SDES keying
    /// material negotiation. The RtpChannel class automatically creates a static X.509 certificate object that it will use for the
    /// DTLS handshake if a media channel uses DTLS-SRTP encryption. It is best to pass the static RtpChannel.CertificateFingerprint property
    /// for this parameter.
    /// </param>
    /// <param name="portManager">MediaPortManager to use for allocation of media ports.</param>
    public SdpOfferSettings(List<string> AudioCodecs, List<string> VideoCodecs, string userName, string fingerprint,
        IMediaPortManager portManager)
    {
        OfferAudioCodecs = AudioCodecs;
        OfferVideoCodecs = VideoCodecs;
        UserName = userName;
        Fingerprint = fingerprint;
        PortManager = portManager;
    }

}
