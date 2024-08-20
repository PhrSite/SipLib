/////////////////////////////////////////////////////////////////////////////////////
//  File:   SdpUtils.cs                                             12 Sep 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Sdp;
using System.Net;
using SipLib.Core;
using SipLib.RtpCrypto;
using SipLib.RealTimeText;
using SipLib.Msrp;
using System.Security.Cryptography.X509Certificates;

/// <summary>
/// Static class that provides various functions for working with the Session Description Protocol (SDP)
/// </summary>
public static class SdpUtils
{
    /// <summary>
    /// Builds an Sdp for offering G.711 Mu-Law audio.
    /// </summary>
    /// <param name="iPAddress">IP address that the remote endpoint should send audio to. Audio data must also be
    /// sent from this address.</param>
    /// <param name="Port">Specifies the UDP port number that audio will be sent from and received on</param>
    /// <param name="UaName">User agent or server name to used for the origin name (o=). Also used to create
    /// a unique name for the audio session.</param>
    /// <returns>Returns an Sdp object with an audio media description</returns>
    public static Sdp BuildSimpleAudioSdp(IPAddress iPAddress, int Port, string UaName)
    {
        Sdp AudioSdp = new Sdp(iPAddress, UaName);
        AudioSdp.Media.Add(CreateAudioMediaDescription(Port));

        return AudioSdp;
    }

    /// <summary>
    /// Creates a basic MediaDescription object for offerring G.711 Mu-Law audio media.
    /// </summary>
    /// <param name="Port">Specifies the UDP port number that audio will be sent and received on</param>
    /// <returns>Returns a new MediaDescription object.</returns>
    public static MediaDescription CreateAudioMediaDescription(int Port)
    {
        MediaDescription AudSmd = new MediaDescription();
        AudSmd.MediaType = "audio";
        AudSmd.Port = Port;
        AudSmd.Transport = "RTP/AVP";
        AudSmd.PayloadTypes = new List<int>() { 0, 101 };
        AudSmd.Attributes.Add(new SdpAttribute("fmtp", "101 0-15"));
        AudSmd.RtpMapAttributes.Add(new RtpMapAttribute(0, "PCMU", 8000));
        AudSmd.RtpMapAttributes.Add(new RtpMapAttribute(101, "telephone-event", 8000));

        return AudSmd;
    }

    /// <summary>
    /// Creates a basic MediaDescription object for offerring G.711 Mu-Law audio media that will be
    /// encrypted using SDES-SRTP. See RFC 4568, RFC 3711 and RFC 6188.
    /// </summary>
    /// <param name="Port">Specifies the UDP port number that audio will be sent and received on</param>
    /// <returns>Returns a new MediaDescription object.</returns>
    public static MediaDescription CreateSdesSrtpAudioMediaDescription(int Port)
    {
        MediaDescription AudSmd = CreateAudioMediaDescription(Port);
        AddSdesSrtpEncryption(AudSmd);
        return AudSmd;
    }

    /// <summary>
    /// Creates a basic MediaDescription object for offering G.711 Mu-Law audio media that will be
    /// encrypted using DTLS-SRTP. See RFC 5763, RFC 5764 and RFC 3711.
    /// </summary>
    /// <param name="Port">Specifies the UDP port number that audio will be sent and received on</param>
    /// <param name="fingerPrintAttribute">Fingerprint from the X.509 certificate. For example:
    /// "SHA-256 4A:AD:B9:B1:3F:82:18:3B:54:02:12:DF:3E:5D:49:6B:19:E5:7C:AB"</param>
    /// <returns>Returns a new MediaDescription object.</returns>
    public static MediaDescription CreateDtlsSrtpAudioMediaDescription(int Port, string fingerPrintAttribute)
    {
        MediaDescription AudSmd = CreateAudioMediaDescription(Port);
        AddDtlsSrtp(AudSmd, fingerPrintAttribute);
        return AudSmd;
    }

    /// <summary>
    /// Adds DTLS-SRTP attributes to a MediaDescription object that will be sent out as part of an SDP offer.
    /// </summary>
    /// <param name="mediaDescription">Input MediaDescription to modify</param>
    /// <param name="fingerPrintAttribute">Fingerprint from the X.509 certificate. For example:
    /// "SHA-256 4A:AD:B9:B1:3F:82:18:3B:54:02:12:DF:3E:5D:49:6B:19:E5:7C:AB"</param>
    public static void AddDtlsSrtp(MediaDescription mediaDescription, string fingerPrintAttribute)
    {
        if (mediaDescription.MediaType == "message")
            return;     // Cannot use DTLS-SRTP with MSRP

        mediaDescription.Transport = "UDP/TLS/RTP/SAVP";
        mediaDescription.Attributes.Add(new SdpAttribute("fingerprint", fingerPrintAttribute.ToUpper()));
    }

    /// <summary>
    /// Adds SDES-SRTP attributes to a MediaDescription object that will be sent out as part of an SDP offer.
    /// </summary>
    /// <param name="mediaDescription">Input MediaDescription to modify</param>
    public static void AddSdesSrtpEncryption(MediaDescription mediaDescription)
    {
        if (mediaDescription.MediaType == "message")
            return;     // Cannot use SDES-SRTP with MSRP

        mediaDescription.Transport = "RTP/SAVP";

        CryptoContext Cc1 = new CryptoContext(CryptoSuites.AES_256_CM_HMAC_SHA1_80);
        CryptoAttribute Ca1 = Cc1.ToCryptoAttribute();
        Ca1.Tag = 1;
        mediaDescription.Attributes.Add(new SdpAttribute("crypto", Ca1.ToString()));

        CryptoContext Cc2 = new CryptoContext(CryptoSuites.AES_CM_128_HMAC_SHA1_80);
        CryptoAttribute Ca2 = Cc2.ToCryptoAttribute();
        Ca2.Tag = 2;
        mediaDescription.Attributes.Add(new SdpAttribute("crypto", Ca2.ToString()));
    }

    /// <summary>
    /// Builds a basic MediaDescription object for offering H.264 video using the Basic Level 1 video
    /// profile.
    /// </summary>
    /// <param name="Port">Specifies the UDP port number that video will be sent and received on</param>
    /// <returns>Returns a new MediaDescription object.</returns>
    public static MediaDescription CreateVideoMediaDescription(int Port)
    {
        MediaDescription VidSmd = new MediaDescription("video", Port, new List<int> { 96 });
        VidSmd.Transport = "RTP/AVP";
        VidSmd.RtpMapAttributes.Add(new RtpMapAttribute(96, "H264", 90000));
        VidSmd.Attributes.Add(SdpAttribute.ParseSdpAttribute("fmtp:96 " + "profile-level-id=42801f"));
        return VidSmd;
    }

    /// <summary>
    /// Builds a basic MediaDescription object for offering H.264 video using the Basic Level 1 video
    /// profile that will be encrypted using SDES-SRTP. See RFC 4568, RFC 3711 and RFC 6188.
    /// </summary>
    /// <param name="Port">Specifies the UDP port number that video will be sent and received on</param>
    /// <returns>Returns a new MediaDescription object.</returns>
    public static MediaDescription CreateSdesSrtpVideoMediaDescription(int Port)
    {
        MediaDescription VidSmd = CreateVideoMediaDescription(Port);
        AddSdesSrtpEncryption(VidSmd);
        return VidSmd;
    }

    /// <summary>
    /// Builds a basic MediaDescription object for offering H.264 video using the Basic Level 1 video
    /// profile that will be encrypted using DTLS-SRTP. See RFC 5763, RFC 5764 and RFC 3711.
    /// </summary>
    /// <param name="Port">Specifies the UDP port number that video will be sent and received on</param>
    /// <param name="fingerPrintAttribute">Fingerprint from the X.509 certificate. For example:
    /// "SHA-256 4A:AD:B9:B1:3F:82:18:3B:54:02:12:DF:3E:5D:49:6B:19:E5:7C:AB"</param>
    /// <returns>Returns a new MediaDescription object.</returns>
    public static MediaDescription CreateDtlsSrtpVideoMediaDescription(int Port, string fingerPrintAttribute)
    {
        MediaDescription VidSmd = CreateVideoMediaDescription(Port);
        AddDtlsSrtp(VidSmd, fingerPrintAttribute);
        return VidSmd;
    }

    /// <summary>
    /// Creates a basic MediaDescription object for offering Real Time Text (RTT) media.
    /// </summary>
    /// <param name="Port">Specifies the UDP port number that RTT will be sent and received on</param>
    /// <returns>Returns a new MediaDescription object.</returns>
    public static MediaDescription CreateRttMediaDescription(int Port)
    {
        MediaDescription RttSmd = new MediaDescription();
        RttSmd.MediaType = "text";
        RttSmd.Port = Port;
        RttSmd.Transport = "RTP/AVP";
        string T140Pt = RttUtils.DefaultT140PayloadType.ToString();
        string RedPt = RttUtils.DefaultRedundantPayloadType.ToString();
        RttSmd.PayloadTypes = new List<int>() { RttUtils.DefaultT140PayloadType, RttUtils.
            DefaultRedundantPayloadType };
        RttSmd.RtpMapAttributes.Add(new RtpMapAttribute(RttUtils.DefaultT140PayloadType, "t140", 1000));
        RttSmd.RtpMapAttributes.Add(new RtpMapAttribute(RttUtils.DefaultRedundantPayloadType, "red", 1000));

        // Use a default of 3 levels of redundancy
        RttSmd.Attributes.Add(new SdpAttribute("fmtp", $"{RedPt} {T140Pt}/{T140Pt}/{T140Pt}/{T140Pt}"));

        return RttSmd;
    }

    /// <summary>
    /// Creates a basic MediaDescription object for offering Message Session Relay Protocol (MSRP, see RFC 4975).
    /// </summary>
    /// <param name="ipAddress">Specifies the IPv4 or IPv6 IP address to use for the MSRP URI in the
    /// path attribute. This is address to which MSRP messages will be sent to and from which MSRP
    /// messages will be sent.</param>
    /// <param name="Port">Specifies the local port for the MSRP URI.</param>
    /// <param name="UseTls">If true then offer MSRP over TLS (MSRPS), else offer MSRP over TCP.</param>
    /// <param name="setupType">Specifies the setup type (active/passive) value to use for the setup
    /// attribute. Optional. Defaults to SetupType.active.</param>
    /// <param name="localCert">Used to build the fingerprint attribute of the client's X.509
    /// certificate. Optional. Defaults to null. This is required only if mutual authentication is
    /// being used.</param>
    /// <returns>Returns a new MediaDescription object for offering MSRP.</returns>
    public static MediaDescription CreateMsrpMediaDescription(IPAddress ipAddress, int Port,
        bool UseTls, SetupType setupType = SetupType.active, X509Certificate2? localCert = null)
    {
        MediaDescription msrpMd = new MediaDescription();
        msrpMd.MediaType = "message";
        if (UseTls == true)
            msrpMd.Transport = "TCP/TLS/MSRP";
        else
            msrpMd.Transport = "TCP/MSRP";

        msrpMd.Port = Port;
        msrpMd.Attributes.Add(new SdpAttribute("accept-types", "message/cpim text/plain"));
        msrpMd.Attributes.Add(new SdpAttribute("setup", setupType.ToString()));

        if (UseTls == true && localCert != null)
            msrpMd.Attributes.Add(SipUtils.BuildFingerprintAttr(localCert));

        string strScheme = UseTls == true ? "msrps" : "msrp";
        string strAddr = ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ?
            ipAddress.ToString() : $"[{ipAddress.ToString()}]";
        string sessionID = MsrpMessage.NewRandomID();
        msrpMd.Attributes.Add(new SdpAttribute("path", $"{strScheme}//{strAddr}:{Port}/{sessionID};tcp"));

        return msrpMd;
    }

    /// <summary>
    /// Creates a basic MediaDescription object for offerring Real Time Text (RTT) media that will be
    /// encrypted using SDES-SRTP. See RFC 4568, RFC 3711 and RFC 6188.
    /// </summary>
    /// <param name="Port">Specifies the UDP port number that RTT will be sent and received on</param>
    /// <returns>Returns a new MediaDescription object.</returns>
    public static MediaDescription CreateSdesSrtpRttMediaDescription(int Port)
    {
        MediaDescription RttSmd = CreateRttMediaDescription(Port);
        AddSdesSrtpEncryption(RttSmd);
        return RttSmd;
    }

    /// <summary>
    /// Creates a basic MediaDescription object for offerring Real Time Text (RTT) media that will be
    /// encrypted using DTLS-SRTP. See RFC 5763, RFC 5764 and RFC 3711.
    /// </summary>
    /// <param name="Port">Specifies the UDP port number that RTT will be sent and received on</param>
    /// <param name="fingerPrintAttribute">Fingerprint from the X.509 certificate. For example:
    /// "SHA-256 4A:AD:B9:B1:3F:82:18:3B:54:02:12:DF:3E:5D:49:6B:19:E5:7C:AB"</param>
    /// <returns>Returns a new MediaDescription object.</returns>
    public static MediaDescription CreateDtlsSrtpRttMediaDescription(int Port, string fingerPrintAttribute)
    {
        MediaDescription RttSmd = CreateRttMediaDescription(Port);
        AddDtlsSrtp(RttSmd, fingerPrintAttribute);
        return RttSmd;
    }
}
