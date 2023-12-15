/////////////////////////////////////////////////////////////////////////////////////
//  File:   SdpUtils.cs                                             12 Sep 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Sdp;
using System.Net;
using SipLib.RtpCrypto;
using SipLib.RealTimeText;

/// <summary>
/// Static class that provides various functions for working with the Session Description Protocol (SDP)
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
        AudioSdp.Media.Add(CreateAudioMediaDescription(Port));

        return AudioSdp;
    }

    /// <summary>
    /// Creates a basic MediaDescription object for offerring audio media.
    /// </summary>
    /// <param name="Port">Specifies the UDP port number that audio will be sent and received on</param>
    /// <returns>Returns a new MediaDescription object.</returns>
    public static MediaDescription CreateAudioMediaDescription(int Port)
    {
        MediaDescription AudSmd = new MediaDescription();
        AudSmd.MediaType = "audio";
        AudSmd.Port = Port;
        AudSmd.Transport = "RTP/AVP";
        AudSmd.MediaFormatNumbers = new List<string>() { "0", "101" };
        AudSmd.Attributes.Add(new SdpAttribute("fmtp", "101 0-15"));
        AudSmd.Attributes.Add(new SdpAttribute("rtpmap", "0 PCMU/8000"));
        AudSmd.Attributes.Add(new SdpAttribute("rtpmap", "101 telephone-event/8000"));

        return AudSmd;
    }

    /// <summary>
    /// Creates a basic MediaDescription object for offerring audio media that will be encrypted using
    /// SDES-SRTP. See RFC 4568, RFC 3711 and RFC 6188.
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
    /// Creates a basic MediaDescription object for offering audio media that will be encrypted using
    /// DTLS-SRTP. See RFC 5763, RFC 5764 and RFC 3711.
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
        mediaDescription.Transport = "UDP/TLS/RTP/SAVP";
        mediaDescription.Attributes.Add(new SdpAttribute("fingerprint", fingerPrintAttribute.ToUpper()));
    }

    /// <summary>
    /// Adds SDES-SRTP attributes to a MediaDescription object that will be sent out as part of an SDP offer.
    /// </summary>
    /// <param name="mediaDescription">Input MediaDescription to modify</param>
    public static void AddSdesSrtpEncryption(MediaDescription mediaDescription)
    {
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
        MediaDescription VidSmd = new MediaDescription("video", Port, "96");
        VidSmd.Transport = "RTP/AVP";
        VidSmd.Attributes.Add(SdpAttribute.ParseSdpAttribute("rtpmap:96 H264/90000"));
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
    /// Creates a basic MediaDescription object for offerring Real Time Text (RTT) media.
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
        RttSmd.MediaFormatNumbers = new List<string>() { T140Pt, RedPt };
        RttSmd.Attributes.Add(new SdpAttribute("rtpmap", $"{T140Pt} t140/1000"));
        RttSmd.Attributes.Add(new SdpAttribute("rtpmap", $"{RedPt} red/1000"));
        // Use a default of 3 levels of redundancy
        RttSmd.Attributes.Add(new SdpAttribute("fmtp", $"{RedPt} {T140Pt} {T140Pt} {T140Pt} {T140Pt}"));

        return RttSmd;
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
