//-----------------------------------------------------------------------------
// Filename: DtlsSrtpTransport.cs
//
// Description: This class represents the DTLS SRTP transport connection to use 
// as Client or Server.
//
// Author(s):
// Rafael Soares (raf.csoares@kyubinteractive.com)
//
// History:
// 01 Jul 2020	Rafael Soares   Created.
// 02 Jul 2020  Aaron Clauson   Switched underlying transport from socket to
//                              piped memory stream.
//
// License:
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//-----------------------------------------------------------------------------

//  Revised: 18 Nov 23 PHR
//      -- Changed namespace to SipLib.Dtls from SIPSorcery.Net
//      -- Added documentation comments and code cleanup
//      -- Changed the public declaration of the constant values to private

using System.Collections.Concurrent;
using Org.BouncyCastle.Crypto.Tls;
using Org.BouncyCastle.Security;

namespace SipLib.Dtls;

/// <summary>
/// Class for managing the transport logic for a DTLS SRTP client or a server.
/// </summary>
public class DtlsSrtpTransport : DatagramTransport, IDisposable
{
    private const int DEFAULT_RETRANSMISSION_WAIT_MILLIS = 100;
    private const int DEFAULT_MTU = 1500;
    private const int MIN_IP_OVERHEAD = 20;
    private const int MAX_IP_OVERHEAD = MIN_IP_OVERHEAD + 64;
    private const int UDP_OVERHEAD = 8;
    private const int DEFAULT_TIMEOUT_MILLISECONDS = 20000;
    private const int DTLS_RETRANSMISSION_CODE = -1;
    private const int DTLS_RECEIVE_ERROR_CODE = -2;

    private static readonly Random random = new Random();

    private IPacketTransformer? srtpEncoder = null;
    private IPacketTransformer? srtpDecoder = null;
    private IPacketTransformer? srtcpEncoder = null;
    private IPacketTransformer? srtcpDecoder = null;
    IDtlsSrtpPeer? connection = null;

    /// <summary>The collection of chunks to be written.</summary>
    private BlockingCollection<byte[]> _chunks = new BlockingCollection<byte[]>(new ConcurrentQueue<byte[]>());

    /// <summary>
    /// Gets or sets the DTLS transport object
    /// </summary>
    /// <value></value>
    public DtlsTransport? Transport { get; private set; }

    /// <summary>
    /// Sets the period in milliseconds that the handshake attempt will timeout after.
    /// </summary>
    /// <value></value>
    public int TimeoutMilliseconds = DEFAULT_TIMEOUT_MILLISECONDS;

    /// <summary>
    /// Sets the period in milliseconds that receive will wait before try retransmission
    /// </summary>
    /// <value></value>
    public int RetransmissionMilliseconds = DEFAULT_RETRANSMISSION_WAIT_MILLIS;

    /// <summary>
    /// Event that is fired when there is data that needs to be sent via UDP
    /// </summary>
    /// <value></value>
    public Action<byte[]>? OnDataReady;

    /// <summary>
    /// Event that is fired if a DTlS protocol Alert occurs
    /// </summary>
    /// <value></value>
    public event Action<AlertLevelsEnum, AlertTypesEnum, string>? OnAlert;

    private System.DateTime _startTime = System.DateTime.MinValue;
    private bool _isClosed = false;

    // Network properties
    private int _waitMillis = DEFAULT_RETRANSMISSION_WAIT_MILLIS;
    private int _mtu;
    private int _receiveLimit;
    private int _sendLimit;

    private volatile bool _handshakeComplete;
    private volatile bool _handshakeFailed;
    private volatile bool _handshaking;


    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="connection">A DtlsSrtpClient object or a DtlsSrtpServer object that this transport
    /// object will manage</param>
    /// <param name="mtu">Maximum transfer unit for the network. Defaults to 1500.</param>
    public DtlsSrtpTransport(IDtlsSrtpPeer connection, int mtu = DEFAULT_MTU)
    {
        // Network properties
        this._mtu = mtu;
        this._receiveLimit = System.Math.Max(0, mtu - MIN_IP_OVERHEAD - UDP_OVERHEAD);
        this._sendLimit = System.Math.Max(0, mtu - MAX_IP_OVERHEAD - UDP_OVERHEAD);
        this.connection = connection;

        connection.OnAlert += (level, type, description) => OnAlert?.Invoke(level, type, description);
    }

    /// <summary>
    /// Gets the SRTP decoder
    /// </summary>
    /// <value></value>
    public IPacketTransformer SrtpDecoder
    {
        get
        {
            return srtpDecoder!;
        }
    }

    /// <summary>
    /// Gets the SRTP encoder
    /// </summary>
    /// <value></value>
    public IPacketTransformer SrtpEncoder
    {
        get
        {
            return srtpEncoder!;
        }
    }

    /// <summary>
    /// Gets the SRTCP decoder
    /// </summary>
    /// <value></value>
    public IPacketTransformer SrtcpDecoder
    {
        get
        {
            return srtcpDecoder!;
        }
    }

    /// <summary>
    /// Gets the SRTCP decoder
    /// </summary>
    /// <value></value>
    public IPacketTransformer SrtcpEncoder
    {
        get
        {
            return srtcpEncoder!;
        }
    }


    /// <summary>
    /// Returns true if the DTLS handshake is complete or false if it is not
    /// </summary>
    /// <returns></returns>
    public bool IsHandshakeComplete()
    {
        return _handshakeComplete;
    }

    /// <summary>
    /// Returns true if the DTLS hanshake failed or false if it did not
    /// </summary>
    /// <returns></returns>
    public bool IsHandshakeFailed()
    {
        return _handshakeFailed;
    }

    /// <summary>
    /// Returns true if the DTLS handshake is in progress
    /// </summary>
    /// <returns></returns>
    public bool IsHandshaking()
    {
        return _handshaking;
    }

    /// <summary>
    /// Starts the DTLS handshake as a client or as a server. This method blocks until the handshake is
    /// complete.
    /// </summary>
    /// <param name="handshakeError">Set to a string that describes the handshake error. Set to null
    /// if no errors occurred. Will be non-null if this method returns false.</param>
    /// <returns>Returns true if successful or false if a handshake error occurred.</returns>
    public bool DoHandshake(out string? handshakeError)
    {
        if (connection!.IsClient())
        {
            return DoHandshakeAsClient(out handshakeError);
        }
        else
        {
            return DoHandshakeAsServer(out handshakeError);
        }
    }

    /// <summary>
    /// Returns true if this transport object is for a DTLS client or false if its for a DTLS server.
    /// </summary>
    /// <value></value>
    public bool IsClient
    {
        get { return connection!.IsClient(); }
    }

    private bool DoHandshakeAsClient(out string? handshakeError)
    {
        handshakeError = null;

        //logger.LogDebug("DTLS commencing handshake as client.");

        if (!_handshaking && !_handshakeComplete)
        {
            this._waitMillis = RetransmissionMilliseconds;
            this._startTime = System.DateTime.Now;
            this._handshaking = true;
            SecureRandom secureRandom = new SecureRandom();
            DtlsClientProtocol clientProtocol = new DtlsClientProtocol(secureRandom);
            try
            {
                DtlsSrtpClient client = (DtlsSrtpClient)connection!;
                // Perform the handshake in a non-blocking fashion
                Transport = clientProtocol.Connect(client, this);

                // Prepare the shared key to be used in RTP streaming
                //client.PrepareSrtpSharedSecret();
                // Generate encoders for DTLS traffic
                if (client.GetSrtpPolicy() != null)
                {
                    srtpDecoder = GenerateRtpDecoder();
                    srtpEncoder = GenerateRtpEncoder();
                    srtcpDecoder = GenerateRtcpDecoder();
                    srtcpEncoder = GenerateRtcpEncoder();
                }
                // Declare handshake as complete
                _handshakeComplete = true;
                _handshakeFailed = false;
                _handshaking = false;
                // Warn listeners handshake completed
                //UnityEngine.Debug.Log("DTLS Handshake Completed");

                return true;
            }
            catch (System.Exception excp)
            {
                if (excp.InnerException is TimeoutException)
                {
                    //logger.LogWarning(excp, $"DTLS handshake as client timed out waiting for handshake to complete.");
                    handshakeError = "timeout";
                }
                else
                {
                    handshakeError = "unknown";
                    if (excp is Org.BouncyCastle.Crypto.Tls.TlsFatalAlert)
                    {
                        handshakeError = (excp as Org.BouncyCastle.Crypto.Tls.TlsFatalAlert)!.Message;
                    }

                    //logger.LogWarning(excp, $"DTLS handshake as client failed. {excp.Message}");
                }

                // Declare handshake as failed
                _handshakeComplete = false;
                _handshakeFailed = true;
                _handshaking = false;
                // Warn listeners handshake completed
                //UnityEngine.Debug.Log("DTLS Handshake failed\n" + e);
            }
        }
        return false;
    }

    private bool DoHandshakeAsServer(out string? handshakeError)
    {
        handshakeError = null;

        //logger.LogDebug("DTLS commencing handshake as server.");

        if (!_handshaking && !_handshakeComplete)
        {
            this._waitMillis = RetransmissionMilliseconds;
            this._startTime = System.DateTime.Now;
            this._handshaking = true;
            SecureRandom secureRandom = new SecureRandom();
            DtlsServerProtocol serverProtocol = new DtlsServerProtocol(secureRandom);
            try
            {
                DtlsSrtpServer server = (DtlsSrtpServer)connection!;

                // Perform the handshake in a non-blocking fashion
                Transport = serverProtocol.Accept(server, this);
                // Prepare the shared key to be used in RTP streaming
                //server.PrepareSrtpSharedSecret();
                // Generate encoders for DTLS traffic
                if (server.GetSrtpPolicy() != null)
                {
                    srtpDecoder = GenerateRtpDecoder();
                    srtpEncoder = GenerateRtpEncoder();
                    srtcpDecoder = GenerateRtcpDecoder();
                    srtcpEncoder = GenerateRtcpEncoder();
                }
                // Declare handshake as complete
                _handshakeComplete = true;
                _handshakeFailed = false;
                _handshaking = false;
                // Warn listeners handshake completed
                //UnityEngine.Debug.Log("DTLS Handshake Completed");
                return true;
            }
            catch (System.Exception excp)
            {
                if (excp.InnerException is TimeoutException)
                {
                    //logger.LogWarning(excp, $"DTLS handshake as server timed out waiting for handshake to complete.");
                    handshakeError = "timeout";
                }
                else
                {
                    handshakeError = "unknown";
                    if (excp is Org.BouncyCastle.Crypto.Tls.TlsFatalAlert)
                    {
                        handshakeError = (excp as Org.BouncyCastle.Crypto.Tls.TlsFatalAlert)!.Message;
                    }

                    //logger.LogWarning(excp, $"DTLS handshake as server failed. {excp.Message}");
                }

                // Declare handshake as failed
                _handshakeComplete = false;
                _handshakeFailed = true;
                _handshaking = false;
                // Warn listeners handshake completed
                //UnityEngine.Debug.Log("DTLS Handshake failed\n"+ e);
            }
        }
        return false;
    }

    /// <summary>
    /// Gets the X.509 certificate of the remote peer
    /// </summary>
    /// <returns></returns>
    public Certificate GetRemoteCertificate()
    {
        return connection!.GetRemoteCertificate();
    }

    /// <summary>
    /// Gets the server's master key
    /// </summary>
    /// <returns></returns>
    protected byte[] GetMasterServerKey()
    {
        return connection!.GetSrtpMasterServerKey();
    }

    /// <summary>
    /// Gets the server's master salt
    /// </summary>
    /// <returns></returns>

    protected byte[] GetMasterServerSalt()
    {
        return connection!.GetSrtpMasterServerSalt();
    }

    /// <summary>
    /// Gets the client's master key
    /// </summary>
    /// <returns></returns>
    protected byte[] GetMasterClientKey()
    {
        return connection!.GetSrtpMasterClientKey();
    }


    /// <summary>
    /// Gets the client's master salt
    /// </summary>
    /// <returns></returns>
    protected byte[] GetMasterClientSalt()
    {
        return connection!.GetSrtpMasterClientSalt();
    }

    /// <summary>
    /// Gets the SRTCP encryption and authenticaion policy information for the DTLS-SRTP session
    /// </summary>
    /// <returns></returns>
    protected SrtpPolicy GetSrtpPolicy()
    {
        return connection!.GetSrtpPolicy();
    }

    /// <summary>
    /// Gets the SRTCP encryption and authenticaion policy information for the DTLS-SRTP session
    /// </summary>
    /// <returns></returns>
    protected SrtpPolicy GetSrtcpPolicy()
    {
        return connection!.GetSrtcpPolicy();
    }

    /// <summary>
    /// Generates an IPacketTransformer interface to use to encode RTP packets
    /// </summary>
    /// <returns></returns>
    protected IPacketTransformer GenerateRtpEncoder()
    {
        return GenerateTransformer(connection!.IsClient(), true);
    }


    /// <summary>
    /// Generates an IPacketTransformer interface to use to decode RTP packets
    /// </summary>
    /// <returns></returns>
    protected IPacketTransformer GenerateRtpDecoder()
    {
        //Generate the reverse result of "GenerateRtpEncoder"
        return GenerateTransformer(!connection!.IsClient(), true);
    }

    /// <summary>
    /// Generates an IPacketTransformer interface to use to encode RTCP packets
    /// </summary>
    /// <returns></returns>
    protected IPacketTransformer GenerateRtcpEncoder()
    {
        var isClient = connection is DtlsSrtpClient;
        return GenerateTransformer(connection!.IsClient(), false);
    }

    /// <summary>
    /// Generates an IPacketTransformer interface to use to decode RTCP packets
    /// </summary>
    /// <returns></returns>
    protected IPacketTransformer GenerateRtcpDecoder()
    {
        //Generate the reverse result of "GenerateRctpEncoder"
        return GenerateTransformer(!connection!.IsClient(), false);
    }

    /// <summary>
    /// Generates an IPacketTransformer for a DTLS client or server for RTP or RTCP packets
    /// </summary>
    /// <param name="isClient">Set to true to generate the transformer for the client or false to
    /// generate the transformer for the server.</param>
    /// <param name="isRtp">Set to true to generate the transformer for RTP packets or to false to
    /// generate the transformer for RTCP packets</param>
    /// <returns></returns>
    protected IPacketTransformer GenerateTransformer(bool isClient, bool isRtp)
    {
        SrtpTransformEngine? engine = null;
        if (!isClient)
        {
            engine = new SrtpTransformEngine(GetMasterServerKey(), GetMasterServerSalt(), GetSrtpPolicy(), GetSrtcpPolicy());
        }
        else
        {
            engine = new SrtpTransformEngine(GetMasterClientKey(), GetMasterClientSalt(), GetSrtpPolicy(), GetSrtcpPolicy());
        }

        if (isRtp)
        {
            return engine.GetRTPTransformer();
        }
        else
        {
            return engine.GetRTCPTransformer();
        }
    }

    /// <summary>
    /// Unprotects (decrypts) an RTP packet received from the remote endpoint.
    /// Only call this method if IsHandshake() complete returns true and IsHandshakeFailed() returns false
    /// </summary>
    /// <param name="packet">Complete RTP packet that was receivedincluding the RTP header</param>
    /// <param name="offset">Offset into the input of the RTP packet</param>
    /// <param name="length">Number of bytes in the RTP packet</param>
    /// <returns></returns>
    public byte[] UnprotectRTP(byte[] packet, int offset, int length)
    {
        lock (this.srtpDecoder!)
        {
            return this.srtpDecoder.ReverseTransform(packet, offset, length);
        }
    }

    /// <summary>
    /// Protects (encrypts) a complete RTP packet.
    /// </summary>
    /// <param name="packet">Complete RTP packet to encrypt</param>
    /// <param name="offset">Offset to the RTP packet</param>
    /// <param name="length">Number of bytes in the RTP packet</param>
    /// <returns>Returns the encrypted RTP packet</returns>
    public byte[] ProtectRTP(byte[] packet, int offset, int length)
{
        lock (this.srtpEncoder!)
        {
            return this.srtpEncoder.Transform(packet, offset, length);
        }
    }

    /// <summary>
    /// Unprotects (decrypts) a complete RTCP packet
    /// </summary>
    /// <param name="packet">Complete RTCP packet to decrypt</param>
    /// <param name="offset">Offset to the RTCP packet</param>
    /// <param name="length">Number of bytes in the RTCP packet</param>
    /// <returns>Returns the decrypted RTCP packet</returns>
    public byte[] UnprotectRTCP(byte[] packet, int offset, int length)
    {
        lock (this.srtcpDecoder!)
        {
            return this.srtcpDecoder.ReverseTransform(packet, offset, length);
        }
    }

    /// <summary>
    /// Protects (encrypts) a compete RTCP packet
    /// </summary>
    /// <param name="packet">The complete RTCP packet to protect</param>
    /// <param name="offset">Offset to the RTCP packet</param>
    /// <param name="length">Number of bytes in the RTCP packet</param>
    /// <returns>Returns the encrypted RTCP packet</returns>
    public byte[] ProtectRTCP(byte[] packet, int offset, int length)
    {
        lock (this.srtcpEncoder!)
        {
            return this.srtcpEncoder.Transform(packet, offset, length);
        }
    }

    /// <summary>
    /// Returns the number of milliseconds remaining until a timeout occurs.
    /// </summary>
    private int GetMillisecondsRemaining()
    {
        return TimeoutMilliseconds - (int)(System.DateTime.Now - this._startTime).TotalMilliseconds;
    }

    /// <summary>
    /// Returns the maximum number of bytes that can be received
    /// </summary>
    /// <returns></returns>
    public int GetReceiveLimit()
    {
        return this._receiveLimit;
    }

    /// <summary>
    /// Returns the maximum number of bytes that can be sent
    /// </summary>
    /// <returns></returns>
    public int GetSendLimit()
    {
        return this._sendLimit;
    }


    /// <summary>
    /// Call this method to send a UDP packet that has been received from the network to the receive
    /// stream of the DTLS handshake logic. Only call this method if the DTLS handshake has not been
    /// completed.
    /// </summary>
    /// <param name="buf">The UDP packet that was received from the network.</param>
    public void WriteToRecvStream(byte[] buf)
    {
        if (!_isClosed)
        {
            _chunks.Add(buf);
        }
    }

    private int Read(byte[] buffer, int offset, int count, int timeout)
    {
        try
        {
            if(_isClosed)
            {
                throw new System.Net.Sockets.SocketException((int)System.Net.Sockets.SocketError.NotConnected);
                //return DTLS_RECEIVE_ERROR_CODE;
            }
            else if (_chunks.TryTake(out var item, timeout))
            {
                Buffer.BlockCopy(item, 0, buffer, 0, item.Length);
                return item.Length;
            }
        }
        catch (ObjectDisposedException) { }
        catch (ArgumentNullException) { }

        return DTLS_RETRANSMISSION_CODE;
    }

    /// <summary>
    /// Implementation of the ReciveMethod of the BouncyCastle DatagramTransport interface. Users
    /// of the DtlsSrtpTransport class must not call this method.
    /// </summary>
    /// <param name="buf"></param>
    /// <param name="off"></param>
    /// <param name="len"></param>
    /// <param name="waitMillis"></param>
    /// <returns></returns>
    // <exception cref="TimeoutException"></exception>
    // <exception cref="System.Net.Sockets.SocketException"></exception>
    public int Receive(byte[] buf, int off, int len, int waitMillis)
    {
        if (!_handshakeComplete)
        {
            // The timeout for the handshake applies from when it started rather than
            // for each individual receive..
            int millisecondsRemaining = GetMillisecondsRemaining();

            //Handle DTLS 1.3 Retransmission time (100 to 6000 ms)
            //https://tools.ietf.org/id/draft-ietf-tls-dtls13-31.html#rfc.section.5.7
            //As HandshakeReliable class contains too long hardcoded initial waitMillis (1000 ms) we must control this internally
            //PS: Random extra delta time guarantee that work in local networks.
            waitMillis = _waitMillis + random.Next(5, 25);

            if (millisecondsRemaining <= 0)
            {
                //logger.LogWarning($"DTLS transport timed out after {TimeoutMilliseconds}ms waiting for handshake from remote {(connection.IsClient() ? "server" : "client")}.");
                throw new TimeoutException();
            }
            else if (!_isClosed)
            {
                waitMillis = Math.Min(waitMillis, millisecondsRemaining);
                var receiveLen = Read(buf, off, len, waitMillis);

                //Handle DTLS 1.3 Retransmission time (100 to 6000 ms)
                //https://tools.ietf.org/id/draft-ietf-tls-dtls13-31.html#rfc.section.5.7
                if (receiveLen == DTLS_RETRANSMISSION_CODE)
                {
                    _waitMillis = BackOff(_waitMillis);
                }
                else
                {
                    _waitMillis = RetransmissionMilliseconds;
                }

                return receiveLen;
            }
            else
            {
                throw new System.Net.Sockets.SocketException((int)System.Net.Sockets.SocketError.NotConnected);
                //return DTLS_RECEIVE_ERROR_CODE;
            }
        }
        else if (!_isClosed)
        {
            return Read(buf, off, len, waitMillis);
        }
        else
        {
            //throw new System.Net.Sockets.SocketException((int)System.Net.Sockets.SocketError.NotConnected);
            return DTLS_RECEIVE_ERROR_CODE;
        }
    }

    /// <summary>
    /// Implementation of the Send() method of the BouncyCastle DatagramTransport interface. Users of the
    /// DtlsSrtpTransport class must not call this method.
    /// </summary>
    /// <param name="buf"></param>
    /// <param name="off"></param>
    /// <param name="len"></param>
    public void Send(byte[] buf, int off, int len)
    {
        if (len != buf.Length)
        {
            // Only create a new buffer and copy bytes if the length is different
            var tempBuf = new byte[len];
            Buffer.BlockCopy(buf, off, tempBuf, 0, len);
            buf = tempBuf;
        }

        OnDataReady?.Invoke(buf);
    }

    /// <summary>
    /// Closes this transport object.
    /// </summary>
    public virtual void Close()
    {
        _isClosed = true;
        this._startTime = System.DateTime.MinValue;
        this._chunks?.Dispose();
    }

    /// <summary>
    /// Close the transport if the instance is out of scope.
    /// </summary>
    protected void Dispose(bool disposing)
    {
        Close();
    }

    /// <summary>
    /// Close the transport if the instance is out of scope.
    /// </summary>
    public void Dispose()
    {
        Close();
    }

    /// <summary>
    /// Handle retransmission time based in DTLS 1.3 
    /// </summary>
    /// <param name="currentWaitMillis"></param>
    /// <returns></returns>
    protected virtual int BackOff(int currentWaitMillis)
    {
        return System.Math.Min(currentWaitMillis * 2, 6000);
    }
}
