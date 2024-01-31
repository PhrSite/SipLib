/////////////////////////////////////////////////////////////////////////////////////
//  File:   RtpChannel.cs                                           20 Oct 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Rtp;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Timers;

using SipLib.Channels;
using SipLib.RtpCrypto;
using SipLib.Sdp;

using Org.BouncyCastle.Crypto.Tls;
using Org.BouncyCastle.Crypto;
using SipLib.Dtls;

/// <summary>
/// Delegate type for the RtpPacketReceived event of the RtpChannel class.
/// </summary>
/// <param name="rtpPacket">Contains the un-encrypted RtpPacket that was received</param>
public delegate void RtpPacketReceivedDelegate(RtpPacket rtpPacket);

/// <summary>
/// Delegate type for the RtpPacketSent event of the RtpChannel class.
/// </summary>
/// <param name="rtpPacket">Contains the un-encrypted RtpPacket that was send</param>
public delegate void RtpPacketSentDelegate(RtpPacket rtpPacket);

/// <summary>
/// Delegate type for the RtcpPacketReceived event of the RtpChannel class.
/// </summary>
/// <param name="rtpCompoundPacket">Un-encrypted compound RTCP packet that was received. Note: All received RTCP packets
/// are parsed as compound packets even through they may not be compound packets.</param>
public delegate void RtcpPacketReceivedDelegate(RtcpCompoundPacket rtpCompoundPacket);

/// <summary>
/// Delegate type for the RtcpPacketSent event of the RtpChannel class.
/// </summary>
/// <param name="rtcpCompoundPacket">Un-encrypted compound RTCP packet that was sent.</param>
public delegate void RtcpPacketSentDelegate(RtcpCompoundPacket rtcpCompoundPacket);

/// <summary>
/// Delegate type for the DtlsHandshakeFailed event of the RtpChannel class
/// </summary>
/// <param name="IsServer">True if this RtpChannel is the DTLS server or false if it is the DTLS client.</param>
/// <param name="remoteEndPoint">IP endpoint of the remote peer</param>
public delegate void DtlsHandshakeFailedDelegate(bool IsServer, IPEndPoint remoteEndPoint);

/// <summary>
/// Class for sending and receiving Real Time Protocol (RTP) media such as audio, video and text (RTT).
/// </summary>
public class RtpChannel
{
    private IPEndPoint m_localRtpEndPoint = null;
    private IPEndPoint m_localRtcpEndPoint = null;
    private IPEndPoint m_remoteRtpEndpoint = null;
    private IPEndPoint m_remoteRtcpEndpoint = null;

    private string m_mediaType = null;
    private bool m_Incoming = false;

    private UdpClient m_RtpUdpClient = null;
    private UdpClient m_RtcpUdpClient = null;
    private Qos m_RtpQos = null;
    private Qos m_RtcpQos = null;

    private bool m_RtcpEnabled = false;
    private string m_CNAME = null;
    private uint m_SSRC = 0;

    private bool m_IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    private SrtpEncryptor m_srtpEncryptor = null;
    private SrtpDecryptor m_srtpDecryptor = null;
    private bool m_IsDtlsSrtp = false;
    private bool m_IsSdesSrtp = false;

    private RtpReceiveStatisticsManager m_RtpReceiveStaticsManager = null;
    private RtpSentStatisticsManager m_RtpSentStaticsManager = null;
    private Timer m_RtcpTimer = null;
    private const double RTCP_TIMER_INTERVAL_MS = 5000;
    private const int AUDIO_SAMPLE_RATE = 8000;
    private const int AUDIO_PACKETS_PER_SECOND = 50;
    private const int RTT_SAMPLE_RATE = 1000;
    private const int RTT_PACKETS_PER_SECOND = 0;   // Because its not a constant

    private static Certificate m_SelfSigned = null;
    private static AsymmetricKeyParameter m_AsymmetricKeyParameter = null;
    private static string m_CertificateFingerprint = null;

    /// <summary>
    /// Gets the fingerprint of the self-signed X.509 certificate that will be used for DTLS-SRTP.
    /// The certificate is a required SDP attribute for calls that offer or answer DTLS-SRTP media encryption.
    /// </summary>
    /// <value></value>
    public static string CertificateFingerprint
    {
        get
        {
            if (m_SelfSigned == null)
            {   // Only do this once so the same X.509 self-signed certificate will be used for all RtpChannels
                // that use DTLS-SRTP
                (m_SelfSigned, m_AsymmetricKeyParameter) = DtlsUtils.CreateSelfSignedTlsCert();
                m_CertificateFingerprint = DtlsUtils.Fingerprint(m_SelfSigned).ToString();
            }

            return m_CertificateFingerprint;
        }
    }

    /// <summary>
    /// Event that is fired when a RTP media packet has been received by this RtpChannel
    /// </summary>
    /// <value></value>
    public event RtpPacketReceivedDelegate RtpPacketReceived = null;

    /// <summary>
    /// Event that is fired when a RTP packet has been sent by this RtpChannel
    /// </summary>
    /// <value></value>
    public event RtpPacketSentDelegate RtpPacketSent = null;

    /// <summary>
    /// Event that is fired when a RTCP packet is received.
    /// </summary>
    /// <value></value>
    public event RtcpPacketReceivedDelegate RtcpPacketReceived = null;

    /// <summary>
    /// Event that is fired when this class sends an RTCP packet.
    /// </summary>
    /// <value></value>
    public event RtcpPacketSentDelegate RtcpPacketSent = null;

    /// <summary>
    /// Event that is fired fired if the DTLS-SRTP handshake failed
    /// </summary>
    /// <value></value>
    public event DtlsHandshakeFailedDelegate DtlsHandshakeFailed = null;

    private static Random m_Random = new Random();

    private RtpChannel(IPEndPoint localRtpEndpoint, string mediaType, bool enableRtcp, string CNAME)
    {
        m_localRtpEndPoint = localRtpEndpoint;
        m_mediaType = mediaType;
        m_RtcpEnabled = enableRtcp;
        m_CNAME = CNAME;
        m_SSRC = (uint) m_Random.Next();

        if (string.IsNullOrEmpty(m_CNAME) == true)
            m_CNAME = $"RtpChannel_{m_mediaType}@{m_localRtpEndPoint}";

        // Enable RTCP only if the media type is audio or RTT (text)
        if (RtcpEnabled == true)
        {
            // TODO: Figure out the sample rate and the packets/second based on the audio codec in use.

            int SampleRate, PacketsPerSeconds;
            if (m_mediaType == "audio")
            {
                SampleRate = AUDIO_SAMPLE_RATE;
                PacketsPerSeconds = AUDIO_PACKETS_PER_SECOND;
            }
            else
            {   // It must be RTT (m_mediaType == "text")
                SampleRate = RTT_SAMPLE_RATE;
                PacketsPerSeconds = RTT_PACKETS_PER_SECOND;
            }

            m_RtpReceiveStaticsManager = new RtpReceiveStatisticsManager(SampleRate, PacketsPerSeconds, m_mediaType);
            m_RtpSentStaticsManager = new RtpSentStatisticsManager(SampleRate);
            m_RtcpTimer = new Timer(RTCP_TIMER_INTERVAL_MS);
            m_RtcpTimer.AutoReset = true;
            m_RtcpTimer.Elapsed += OnRtcpTimerExpired;
        }
    }

    /// <summary>
    /// Gets or sets the RTP SSRC for this RtpChannel. By default, the SSRC is set to a random unsigned 32-bit
    /// number so there is usually no need to change it by calling the setter.
    /// </summary>
    /// <value></value>
    public uint SSRC
    {
        get { return m_SSRC; }
        set { m_SSRC = value; }
    }

    private bool RtcpEnabled
    {
        get
        {
            if (m_RtcpEnabled == true && (m_mediaType == "audio" || m_mediaType == "text"))
                return true;
            else
                return false;
        }
    }

    /// <summary>
    /// Creates an RtpChannel using the offered and answered Session Description Protocol (SDP) parameters.
    /// </summary>
    /// <param name="Incoming">Set to true if the call is incoming.</param>
    /// <param name="OfferedSdp">The SDP that was offered.</param>
    /// <param name="OfferedMd">The offered media description parameter block from the offered SDP</param>
    /// <param name="AnsweredSdp">The SDP that was answered.</param>
    /// <param name="AnsweredMd">The answered media description parameter block from the answered SDP</param>
    /// <param name="enableRtcp">If true, then RTCP packets will be sent periodically.</param>
    /// <param name="CNAME">Cononical name to use for sending SDES RTCP packets that identify the
    /// media source. If null, then a default CNAME will be automatically generated.</param>
    /// <returns>Returns a (RtpChannel, string) tuple. If the RtpChannel return value is null then an error
    /// was detected and the string return value will contain an explanation of the error. If the RtpChannel
    /// return value is not null then the string return value will be null.</returns>
    public static (RtpChannel, string) CreateFromSdp(bool Incoming, Sdp OfferedSdp, MediaDescription OfferedMd,
        Sdp AnsweredSdp, MediaDescription AnsweredMd, bool enableRtcp, string CNAME)
    {
        if (OfferedMd.MediaType != AnsweredMd.MediaType)
            return (null, "Media type mismatch");

        if (OfferedMd.MediaType != "audio" && OfferedMd.MediaType == "text" && OfferedMd.MediaType == "video")
            return (null, $"Uknown media type: {OfferedMd.MediaType}");

        Sdp LocalSdp, RemoteSdp;
        MediaDescription LocalMd, RemoteMd;
        IPEndPoint localRtpEndPoint, remoteRtpEndPoint;

        if (Incoming == true)
        {   // Its an incoming call
            LocalSdp = AnsweredSdp;
            LocalMd = AnsweredMd;
            RemoteSdp = OfferedSdp;
            RemoteMd = OfferedMd;
        }
        else
        {   // Its an outgoing call
            LocalSdp = OfferedSdp;
            LocalMd = OfferedMd;
            RemoteSdp = AnsweredSdp;
            RemoteMd = AnsweredMd;
        }

        localRtpEndPoint = Sdp.GetMediaEndPoint(LocalSdp, LocalMd);
        remoteRtpEndPoint = Sdp.GetMediaEndPoint(RemoteSdp, RemoteMd);
        RtpChannel rtpChannel = new RtpChannel(localRtpEndPoint, LocalMd.MediaType, enableRtcp, CNAME);
        rtpChannel.m_Incoming = Incoming;
        rtpChannel.m_remoteRtpEndpoint = remoteRtpEndPoint;
        rtpChannel.m_mediaType = LocalMd.MediaType;

        // Figure out the RTCP endpoints. See RFC 3605
        rtpChannel.m_localRtcpEndPoint = rtpChannel.GetRtcpEndPoint(LocalMd, localRtpEndPoint);
        rtpChannel.m_remoteRtcpEndpoint = rtpChannel.GetRtcpEndPoint(RemoteMd, remoteRtpEndPoint);

        if (AnsweredMd.Transport.IndexOf("SAVP") >= 0)
        {   // Using the Secure Audio Video Profile (SAVP) for either SDES-SRTP or DTLS-SRTP. Figure out 
            // which one.

            // The fingerprint attribute can be in the session level or in the media level. If its present
            // in either then use DTLS-SRTP
            if (AnsweredSdp.GetNamedAttribute("fingerprint") != null || AnsweredMd.GetNamedAttribute(
                "fingerprint") != null)
            {
                rtpChannel.m_IsDtlsSrtp = true;

            }
            else
            {   // If its SDES-SRTP then there must be at least one crypto attribute in the answered media
                // description

                // Set up the encryptor and the decryptor based on the answered crypto suite.
                List<CryptoAttribute> AnsweredCryptoAttributes = GetCryptoAttributes(AnsweredMd);
                List<CryptoAttribute> OfferedCryptoAttributes = GetCryptoAttributes(OfferedMd);
                if (AnsweredCryptoAttributes.Count > 0 && OfferedCryptoAttributes.Count > 0)
                {
                    string selectedCryptoSuite = AnsweredCryptoAttributes[0].CryptoSuite;
                    CryptoAttribute senderAttribute, receiverAttribute;
                    if (Incoming == true)
                    {
                        senderAttribute = AnsweredCryptoAttributes[0];
                        receiverAttribute = GetSelectedCryptoAttibute(OfferedCryptoAttributes, selectedCryptoSuite);
                        if (receiverAttribute == null)
                            return (null, "The selected crypto suite was not found in the offered crypto attributes");
                    }
                    else
                    {
                        senderAttribute = GetSelectedCryptoAttibute(OfferedCryptoAttributes, selectedCryptoSuite);
                        if (senderAttribute == null)
                            return (null, "The selected crypto suite was not found in the offered crypto attributes");
                        receiverAttribute = AnsweredCryptoAttributes[0];
                    }

                    CryptoContext sendContext = CryptoContext.CreateFromCryptoAttribute(senderAttribute);
                    CryptoContext receiveContext = CryptoContext.CreateFromCryptoAttribute(receiverAttribute);
                    rtpChannel.m_srtpEncryptor = new SrtpEncryptor(sendContext);
                    rtpChannel.m_srtpDecryptor = new SrtpDecryptor(receiveContext);
                    rtpChannel.m_IsSdesSrtp = true;
                }
                else
                {
                    if (AnsweredCryptoAttributes.Count == 0)
                        return (null, "SDES-SRTP was answered but no crypto attributes were provided in the " +
                            "answered media description");

                    if (OfferedCryptoAttributes.Count == 0)
                        return (null, "SDES-SRTP was answered but no crypto attributes were offered");
                }
            }
        }
        else
        {   // Not using any encryption
            
        }
        
        return (rtpChannel, null);
    }

    private static CryptoAttribute GetSelectedCryptoAttibute(List<CryptoAttribute> OfferedCryptoAttributes,
        string cryptoSuiteName)
    {
        CryptoAttribute cryptoAttribute = null;
        foreach (CryptoAttribute attr in OfferedCryptoAttributes)
        {
            if (attr.CryptoSuite == cryptoSuiteName)
            {
                cryptoAttribute = attr;
                break;
            }
        }


        return cryptoAttribute;
    }

    private static List<CryptoAttribute> GetCryptoAttributes(MediaDescription mediaDescription)
    {
        List<CryptoAttribute> list = new List<CryptoAttribute>();
        List<SdpAttribute> attrList = mediaDescription.GetNamedAttributes("crypto");
        foreach (SdpAttribute attr in attrList)
            list.Add(CryptoAttribute.Parse(attr.Value));

        return list;
    }

    // See RFC 3605
    private IPEndPoint GetRtcpEndPoint(MediaDescription mediaDescription, IPEndPoint remoteRtpEndPoint)
    {
        IPAddress iPAddress = remoteRtpEndPoint.Address;
        int port = remoteRtpEndPoint.Port + 1;
        SdpAttribute Sa = mediaDescription.GetNamedAttribute("rtcp");
        if (Sa != null && string.IsNullOrEmpty(Sa.Value) == false)
        {
            string[] attrFields = Sa.Value.Split(" ", StringSplitOptions.RemoveEmptyEntries);
            if (attrFields != null && (attrFields.Length == 1 || attrFields.Length == 4))
            {
                // The port number is the first field  
                int index = attrFields[0].IndexOf("/");
                if (index >= 0)
                    // Multiple ports have been specified, but we want only the first one
                    attrFields[0] = attrFields[0].Substring(0, index);

                int.TryParse(attrFields[0], out port);  // If this fails then the default port will be used

                if (attrFields.Length == 4)
                    // An IP address has been provided. If this fails then the default will be used
                    IPAddress.TryParse(attrFields[3], out iPAddress);
            }
        }

        return new IPEndPoint(iPAddress, port);
    }

    private void CreateUdpEndPoints()
    {
        m_RtpUdpClient = new UdpClient(m_localRtpEndPoint);
        if (m_IsWindows == true)
            SIPUDPChannel.DisableConnectionReset(m_RtpUdpClient);

        m_RtpQos = new Qos();
        m_RtpQos.SetUdpDscp(m_RtpUdpClient, DscpSettings.GetDscpForMediaType(m_mediaType));

        m_RtcpUdpClient = new UdpClient(m_localRtcpEndPoint);
        if (m_IsWindows == true)
            SIPUDPChannel.DisableConnectionReset(m_RtcpUdpClient);

        m_RtcpQos = new Qos();
        m_RtcpQos.SetUdpDscp(m_RtcpUdpClient, DscpSettings.GetDscpForMediaType(m_mediaType));
    }

    private bool m_IsListening = false;
    private bool m_ThreadsEnding = false;
    private Thread m_RtpListenerThread = null;
    private Thread m_RtcpListenerThread = null;

    /// <summary>
    /// Starts the listener threads for RTP and RTCP.
    /// </summary>
    public void StartListening()
    {
        if (m_IsDtlsSrtp == false)
            // Start immediately for SDES-SRTP or no encryption
            InternalStartListening();
        else
            // Don't start listening until the DTLS-SRTP handshake is completed  
            StartDtlsHandshake();

    }

    private void StartDtlsHandshake()
    {
        Thread HandshakeThread;

        if (m_Incoming == true)
            HandshakeThread = new Thread(DtlsServerHandshakeThread);
        else
            HandshakeThread = new Thread(DtlsClientHandshakeThread);

        HandshakeThread.IsBackground = true;
        HandshakeThread.Priority = ThreadPriority.Highest;
        HandshakeThread.Start();
    }

    private DtlsSrtpTransport m_DtlsTransport = null;

    private void DtlsClientHandshakeThread()
    {
        DtlsSrtpClient dtlsClient = new DtlsSrtpClient(m_SelfSigned, m_AsymmetricKeyParameter);
        DtlsSrtpTransport dtlsClientTransport = new DtlsSrtpTransport(dtlsClient);
        dtlsClientTransport.TimeoutMilliseconds = 1000;
        UdpClient udpClient = new UdpClient(m_localRtpEndPoint);
        DtlsClientUdpTransport dtlsClientUdpTransport = new DtlsClientUdpTransport(udpClient, m_remoteRtpEndpoint,
            dtlsClientTransport);
        Task<bool> clientTask = Task.Run<bool>(() => dtlsClientTransport.DoHandshake(out _));
        bool Result = clientTask.Result;

        dtlsClientUdpTransport.Close(); // Also closes the udpClient

        if (dtlsClientTransport.IsHandshakeFailed() == false)
        {
            m_DtlsTransport = dtlsClientTransport;
            InternalStartListening();
        }
        else
            DtlsHandshakeFailed?.Invoke(false, m_remoteRtpEndpoint);
    }

    private void DtlsServerHandshakeThread()
    {
        DtlsSrtpServer dtlsSrtpServer = new DtlsSrtpServer(m_SelfSigned, m_AsymmetricKeyParameter);
        DtlsSrtpTransport dtlsServerTransport = new DtlsSrtpTransport(dtlsSrtpServer);
        dtlsServerTransport.TimeoutMilliseconds = 1000;
        UdpClient udpClient = new UdpClient(m_localRtpEndPoint);
        DtlsServerUdpTransport dtlsServerUdpTransport = new DtlsServerUdpTransport(udpClient, m_remoteRtpEndpoint,
            dtlsServerTransport);
        Task<bool> serverTask = Task.Run<bool>(() => dtlsServerTransport.DoHandshake(out _));
        bool Result = serverTask.Result;

        dtlsServerUdpTransport.Close(); // Also closes the udpClient

        if (dtlsServerTransport.IsHandshakeFailed() == false)
        {
            m_DtlsTransport = dtlsServerTransport;
            InternalStartListening();
        }
        else
            DtlsHandshakeFailed?.Invoke(true, m_remoteRtpEndpoint);
    }

    private void InternalStartListening()
    {
        if (m_IsListening == true || m_ThreadsEnding == true)
            return;

        CreateUdpEndPoints();

        m_RtpListenerThread = new Thread(RtpListenerThread);
        m_RtpListenerThread.Priority = ThreadPriority.Highest;
        m_RtpListenerThread.Start();

        m_RtcpListenerThread = new Thread(RtcpListenerThread);
        m_RtcpListenerThread.Start();

        if (RtcpEnabled == true)
        {
            // Send an empty Sender report with an SDES packet to associate the RtpChannel's SSRC with the assigned
            // CNAME
            OnRtcpTimerExpired(null, null);

            m_RtcpTimer.Enabled = true;
            m_RtcpTimer.Start();
        }

        m_IsListening = true;

    }

    /// <summary>
    /// Event handler for the Elapsed event of the m_RtcpTimer object. This timer only runs if RTCP processing
    /// is enabled.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnRtcpTimerExpired(object sender, ElapsedEventArgs e)
    {
        if (m_ThreadsEnding == true)
            return;

        // Get the current received and sent statistics
        RtpReceiveStatistics currentReceiveStats = m_RtpReceiveStaticsManager.CurrentStatistics;
        RtpSentStatistics currentSentStats = m_RtpSentStaticsManager.GetCurrentStatistics();
        DateTime utcNow = DateTime.UtcNow;

        // Build an RTCP compound packet containing a Sender report block, a Receiver report block and an SDES block
        RtcpCompoundPacket rtcpCompoundPacket = new RtcpCompoundPacket();
        SenderReport senderReport = new SenderReport();
        senderReport.SSRC = m_SSRC;
        senderReport.SenderInfo.NTP = utcNow;
        senderReport.SenderInfo.RtpTimestamp = currentSentStats.Timestamp;
        senderReport.SenderInfo.SenderPacketCount = currentSentStats.PacketsSent;
        senderReport.SenderInfo.SenderOctetCount = currentSentStats.BytesSent;

        ReportBlock rb = new ReportBlock();
        rb.SSRC = currentReceiveStats.SSRC;
        if (currentReceiveStats.PacketsReceived < currentReceiveStats.PacketsExpected && currentReceiveStats.
            PacketsExpected != 0)
        {   // See Section 6.4.1 of RFC 3550
            double LostFraction = 1 - (double)currentReceiveStats.PacketsReceived / currentReceiveStats.PacketsExpected;
            rb.FractionLost = (byte)(LostFraction * 256);
            rb.CumulativePacketsLost = (uint)(currentReceiveStats.PacketsExpected - currentReceiveStats.PacketsReceived);
        }

        rb.HighestSequenceNumberReceived = currentReceiveStats.ExtendedLastSequenceNumber;
        rb.InterarrivalJitter = (uint)currentReceiveStats.SmoothedJitter.Maximum;
        senderReport.AddReportBlock(rb);

        rtcpCompoundPacket.SenderReports.Add(senderReport);

        SdesItem sdesItem = new SdesItem(SdesItemType.CNAME, $"{m_CNAME}@{m_localRtpEndPoint.Address}");
        SdesPacket sdesPacket = new SdesPacket(m_SSRC, sdesItem);
        rtcpCompoundPacket.SdesPackets.Add(sdesPacket);

        SendRtcpPacket(rtcpCompoundPacket.ToByteArray());

        RtcpPacketSent?.Invoke(rtcpCompoundPacket);
    }

    private void SendRtcpPacket(byte[] packetBytes)
    {
        byte[] encryptedPacket = null;
        if (m_IsSdesSrtp == true)
            encryptedPacket = m_srtpEncryptor.EncryptRtcpPacket(packetBytes);
        else if (m_IsDtlsSrtp == true)
            m_DtlsTransport.ProtectRTCP(packetBytes, 0, packetBytes.Length);
        else
            encryptedPacket = packetBytes;

        if (encryptedPacket == null)
            return;     // Error encrypting the RTCP compound packet

        try
        {
            m_RtcpUdpClient.Send(encryptedPacket, encryptedPacket.Length, m_remoteRtcpEndpoint);
        }
        catch (SocketException) { }
        catch (Exception) { }
    }

    /// <summary>
    /// Shuts down this RtpChannel and releases all resources. This object cannot be used after this
    /// method is called.
    /// </summary>
    public void Shutdown()
    {
        if (m_ThreadsEnding == true)
            return;
        
        m_ThreadsEnding = true;

        if (RtcpEnabled == true && m_RtcpTimer != null)
        {
            try
            {
                m_RtcpTimer.Stop();
                m_RtcpTimer.Dispose();
                m_RtcpTimer = null;
            }
            catch { }
        }

        // Closing the UdpClient objects will cause the threads to terminate
        if (m_RtpUdpClient != null)
        {
            m_RtpQos.Shutdown();
            m_RtpUdpClient.Close();
            m_RtpListenerThread.Join();
            m_RtpUdpClient = null;
        }

        if (m_RtcpUdpClient != null)
        {
            m_RtcpQos.Shutdown();
            m_RtcpUdpClient.Close();
            m_RtcpListenerThread.Join();
            m_RtcpUdpClient = null;
        }
    }

    private void RtpListenerThread()
    {
        byte[] buf = null;
        IPEndPoint Ipe = new IPEndPoint(IPAddress.Any, 0);

        while (m_ThreadsEnding == false)
        {
            try
            {
                buf = m_RtpUdpClient.Receive(ref Ipe);
                if (buf != null)
                    ProcessRtpPacket(buf, Ipe);
            }
            catch (SocketException) { }  // Occurs when the socket is closed.
            catch (ObjectDisposedException) { }
            catch (NullReferenceException) { }
            catch (Exception) { }
            finally
            {
                buf = null;   // Release the reference to the buffer.
            }
        }
    }

    private void ProcessRtpPacket(byte[] buf, IPEndPoint Ipe)
    {
        if (buf.Length < RtpPacket.MIN_PACKET_LENGTH || Ipe == null)
            return;     // Error: Packet too short or no remote endpoint provided

        byte[] decryptedPckt;
        if (m_IsSdesSrtp == true)
            decryptedPckt = m_srtpDecryptor.DecryptRtpPacket(buf);
        else if (m_IsDtlsSrtp == true)
            decryptedPckt = m_DtlsTransport.UnprotectRTP(buf, 0, buf.Length);
        else
            decryptedPckt = buf;

        RtpPacket rtpPacket = new RtpPacket(decryptedPckt);

        if (RtcpEnabled == true)
            m_RtpReceiveStaticsManager.Update(rtpPacket);

        RtpPacketReceived?.Invoke(rtpPacket);
    }

    private void RtcpListenerThread()
    {
        byte[] buf = null;
        IPEndPoint Ipe = new IPEndPoint(IPAddress.Any, 0);

        while (m_ThreadsEnding == false)
        {
            try
            {
                buf = m_RtcpUdpClient.Receive(ref Ipe);
                if (buf != null)
                    ProcessRtcpPacket(buf, Ipe);
            }
            catch (SocketException) { }  // Occurs when the socket is closed.
            catch (ObjectDisposedException) { }
            catch (NullReferenceException) { }
            catch (Exception) { }
            finally
            {
                buf = null;   // Release the reference to the buffer.
            }
        }
    }

    private void ProcessRtcpPacket(byte[] buf, IPEndPoint Ipe)
    {
        if (buf.Length < RtcpHeader.RTCP_HEADER_LENGTH)
            return;     // Error: input buffer is too short

        byte[] decryptedPckt;
        if (m_IsSdesSrtp == true)
            decryptedPckt = m_srtpDecryptor.DecryptRtcpPacket(buf);
        else if (m_IsDtlsSrtp == true)
            decryptedPckt = m_DtlsTransport.UnprotectRTCP(buf, 0, buf.Length);
        else
            decryptedPckt = buf;

        RtcpCompoundPacket rtcpCompoundPacket = RtcpCompoundPacket.Parse(decryptedPckt);
        if (rtcpCompoundPacket != null)
            RtcpPacketReceived?.Invoke(rtcpCompoundPacket);
    }

    /// <summary>
    /// Sends an RTP packet to the remote endpoint
    /// </summary>
    /// <param name="rtpPacket"></param>
    public void Send(RtpPacket rtpPacket)
    {
        if (m_IsListening == false || m_ThreadsEnding == true)
            return;

        RtpPacketSent ?.Invoke(rtpPacket);

        byte[] encryptedPckt;
        byte[] packetBytes = rtpPacket.PacketBytes;
        if (RtcpEnabled == true)
            m_RtpSentStaticsManager.Update(rtpPacket);

        if (m_IsSdesSrtp == true)
            encryptedPckt = m_srtpEncryptor.EncryptRtpPacket(packetBytes);
        else if (m_IsDtlsSrtp == true)
            encryptedPckt = m_DtlsTransport.ProtectRTP(packetBytes, 0, packetBytes.Length);
        else
            encryptedPckt = packetBytes;

        try
        {
            m_RtpUdpClient.Send(encryptedPckt, m_remoteRtpEndpoint);
        }
        catch (SocketException) { }
        catch (Exception) { }
    }
}
