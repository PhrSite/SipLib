/////////////////////////////////////////////////////////////////////////////////////
//  File:   RtpChannel.cs                                           20 Oct 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Rtp;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Org.BouncyCastle.Crypto.Paddings;
using SipLib.Channels;
using SipLib.RtpCrypto;

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
/// Class for sending and receiving Real Time Protocol (RTP) media such as audio, video and text (RTT).
/// </summary>
public class RtpChannel
{
    private IPEndPoint m_localRtpEndPoint = null;
    private IPEndPoint m_localRtcpEndPoint = null;
    private IPEndPoint m_remoteRtpEndpoint = null;
    private IPEndPoint m_remoteRtcpEndpoint = null;

    private string m_mediaType = null;

    private UdpClient m_RtpUdpClient = null;
    private UdpClient m_RtcpUdpClient = null;
    private Qos m_RtpQos = null;
    private Qos m_RtcpQos = null;

    private bool m_RtcpEnabled = false;
    private string m_CNAME = null;

    private bool m_IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    private SrtpEncryptor m_srtpEncryptor = null;
    private SrtpDecryptor m_srtpDecryptor = null;

    /// <summary>
    /// Event that is fired when a RTP media packet has been received by this RtpChannel
    /// </summary>
    public event RtpPacketReceivedDelegate RtpPacketReceived = null;

    /// <summary>
    /// Event that is fired when a RTP packet has been sent by this RtpChannel
    /// </summary>
    public event RtpPacketSentDelegate RtpPacketSent = null;

    private RtpChannel(IPEndPoint localRtpEndpoint, string mediaType, bool enableRtcp, string CNAME)
    {
        m_localRtpEndPoint = localRtpEndpoint;
        m_mediaType = mediaType;
        m_RtcpEnabled = enableRtcp;
        m_CNAME = CNAME;
        if (string.IsNullOrEmpty(m_CNAME) == true)
            m_CNAME = $"RtpChannel_{m_mediaType}@{m_localRtpEndPoint}";

        m_localRtcpEndPoint = new IPEndPoint(m_localRtpEndPoint.Address, m_localRtpEndPoint.Port + 1);

        CreateUdpEndPoints();
    }

    /// <summary>
    /// Creates a new RtpChannel object for an incoming call or an outgoing call.
    /// </summary>
    /// <param name="localRtpEndpoint">Local IPEndPoint to listen on for RTP packets</param>
    /// <param name="mediaType">Media type. Must be one of: audio, text (for RTT), message (for MSRP) or
    /// video.</param>
    /// <param name="enableRtcp">If true, then RTCP packets will be sent periodically.</param>
    /// <param name="CNAME">Cononical name to use for sending SDES RTCP packets that identify the
    /// media source. If null, then a default CNAME will be automatically generated.</param>
    /// <param name="remoteRtpEndPoint">Remote endpoint to send media to and to receive media from</param>
    /// <param name="receiveContext">CryptoContext to use for decrypting RTP and RTCP packets
    /// that are received from the remote endpoint. If null, then received packets will not be encrypted.</param>
    /// <param name="sendContext">CryptoContext to use for encrypting RTP and RTCP packets that are sent to
    /// the remote endpoints. If null, then RTP and RTCP packets will not be encrypted.</param>
    /// <returns></returns>
    public static RtpChannel Create(IPEndPoint localRtpEndpoint, string mediaType, bool enableRtcp,
        string CNAME, IPEndPoint remoteRtpEndPoint, CryptoContext receiveContext = null, 
        CryptoContext sendContext = null)
    {
        RtpChannel rtpChannel = new RtpChannel(localRtpEndpoint, mediaType, enableRtcp, CNAME);
        rtpChannel.m_remoteRtpEndpoint = remoteRtpEndPoint;
        rtpChannel.m_remoteRtcpEndpoint = new IPEndPoint(remoteRtpEndPoint.Address, remoteRtpEndPoint.Port + 1);
        rtpChannel.m_srtpEncryptor = new SrtpEncryptor(sendContext);
        rtpChannel.m_srtpDecryptor = new SrtpDecryptor(receiveContext);

        return rtpChannel;
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
        if (m_IsListening == true || m_ThreadsEnding == true)
            return;

        m_IsListening = true;

        m_RtpListenerThread = new Thread(RtpListenerThread);
        m_RtpListenerThread.Priority = ThreadPriority.Highest;
        m_RtcpListenerThread.Start();

        m_RtcpListenerThread = new Thread(RtcpListenerThread);
        m_RtcpListenerThread.Start();
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

        byte[] decryptedPckt = m_srtpDecryptor.DecryptRtpPacket(buf);
        RtpPacket rtpPacket = new RtpPacket(decryptedPckt);

        if (m_RtcpEnabled == true)
        {
            // TODO: handle RTCP receive statistics
        }

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

        byte[] decryptedPckt = m_srtpDecryptor.DecryptRtcpPacket(buf);

        // TODO: Handle the RTCP packet
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

        if (m_RtcpEnabled == true)
        {
            // TODO: RTCP calculations
        }

        byte[] encryptedPckt = m_srtpEncryptor.EncryptRtpPacket(rtpPacket.PacketBytes);

        try
        {
            m_RtpUdpClient.Send(encryptedPckt, m_remoteRtpEndpoint);
        }
        catch (SocketException) { }
        catch (Exception) { }
    }
}
