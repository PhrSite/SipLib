﻿/////////////////////////////////////////////////////////////////////////////////////
//  File:   MsrpConnection.cs                                       29 Jul 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Msrp;

using System.Security.Cryptography.X509Certificates;
using System.Net.Sockets;
using System.Net;

using SipLib.Channels;
using SipLib.Core;
using SipLib.Sdp;
using System.Net.Security;
using System.Collections.Concurrent;
using System.Text;

/// <summary>
/// Class for managing a single MSRP connection to either a remote server or from a remote client.
/// </summary>
public class MsrpConnection
{
    private const int DEFAULT_READ_BUFFER_LENGTH = 4096;
    private const int CHUNK_SIZE = 2048;

    private TcpListener? m_TcpListener = null;
    private TcpClient? m_TcpClient;
    private MsrpUri? m_RemoteMsrpUri = null;
    private MsrpUri? m_LocalMsrpUri = null;
    private X509Certificate2? m_Certificate = null;

    private Stream? m_NetworkStream = null;

    /// <summary>
    /// Absolute maximum size of a MSRP message for receiving messages.
    /// </summary>
    private const int DEFAULT_MAX_MSRP_MESSAGE_LENGTH = 10000;
    private const int MINIMUM_MAX_MSRP_MESSAGE_LENGTH = 3000;
    private int m_MaxMsrpTransactionLength = DEFAULT_MAX_MSRP_MESSAGE_LENGTH;

    private MsrpStreamParser m_StreamParser = new MsrpStreamParser(DEFAULT_MAX_MSRP_MESSAGE_LENGTH);
    private byte[] m_ReadBuffer = new byte[DEFAULT_READ_BUFFER_LENGTH];

    /// <summary>
    /// Returns true if the connection is passive, i.e., this end is the server and listening for connection
    /// requests. Returns false if this end of the connection is the client.
    /// </summary>
    /// <value></value>
    public bool ConnectionIsPassive { get; private set; } = false;

    private Qos m_Qos = new Qos();
    private bool m_ShuttingDown = false;

    private ConcurrentQueue<MsrpMessage> m_RequestTransmitQueue = new ConcurrentQueue<MsrpMessage>();
    private ConcurrentQueue<MsrpMessage> m_ResponseTransmitQueue = new ConcurrentQueue<MsrpMessage>();
    private SemaphoreSlim m_TransmitTaskSemaphore = new SemaphoreSlim(0, int.MaxValue);
    private CancellationTokenSource m_TokenSource = new CancellationTokenSource();
    private MsrpMessage? m_ResponseMessage = null;
    private bool m_Started = false;

    /// <summary>
    /// Constructor
    /// </summary>
    private MsrpConnection()
    {
        // Don't wait, let it run in the background
        Task transmitTask = TransmitTask(m_TokenSource.Token);
    }

    /// <summary>
    /// Gets or sets the maximum size of a complete MSRP message that the MsrpConnection can receive.
    /// <para>
    /// The default size is 10,000 bytes. The minimum limit on the message length is 3,000 bytes. There is no
    /// upper limit. For text applications it is not necessary to change the default. If the application will
    /// be expected to receive large images or video files then this property must be set to an appropriate value.
    /// </para>
    /// <para>
    /// This property must be set before calling the Start() method.
    /// </para>
    /// </summary>
    public int MaxMsrpMessageLength
    {
        get { return m_MaxMsrpTransactionLength; }
        set
        {
            if (m_Started == false)
            {
                if (value < MINIMUM_MAX_MSRP_MESSAGE_LENGTH)
                    throw new ArgumentException("The maximum MSRP message message length may not be less than " +
                        $"{MINIMUM_MAX_MSRP_MESSAGE_LENGTH} bytes");

                m_MaxMsrpTransactionLength = value;
                m_StreamParser = new MsrpStreamParser(m_MaxMsrpTransactionLength);
            }
        }
    }

    /// <summary>
    /// Event that is fired when a complete MSRP message is received. This event is not fired for empty
    /// SEND requests. Use this event if you wish to parse and process the complete MSRP message given
    /// the Content-Type that is a custom type -- for example a binary type such as image/jpeg.
    /// <para>
    /// If the application only needs to handle text messages then use the MsrpTextMessageReceived event.
    /// </para>
    /// </summary>
    /// <value></value>
    public event MsrpMessageReceivedDelegate? MsrpMessageReceived = null;

    /// <summary>
    /// Event that fired when an MSRP message is received with a Content-Type of text/plain or message/cpim and the content
    /// type of the CPIM message is text/plain. 
    /// </summary>
    public event MsrpTextMessageReceivedDelegate? MsrpTextMessageReceived = null;

    /// <summary>
    /// This event is fired when a connection is established with the remote endpoint either as a client
    /// or as a server.
    /// </summary>
    /// <value></value>
    public event MsrpConnectionStatusDelegate? MsrpConnectionEstablished = null;

    /// <summary>
    /// This event is fired if the MsrpConnection object was unable to connect to the remote endpoint as
    /// a client.
    /// </summary>
    /// <value></value>
    public event MsrpConnectionStatusDelegate? MsrpConnectionFailed = null;

    /// <summary>
    /// This event is fired if the remote endpoint rejected a MSRP message sent by the MsrpConnection object
    /// or if there was another problem delivering the message.
    /// </summary>
    /// <value></value>
    public event MsrpMessageDeliveryFailedDelegate? MsrpMessageDeliveryFailed = null;

    /// <summary>
    /// Event that is fired when a MSRP REPORT request is received.
    /// </summary>
    /// <value></value>
    public event ReportReceivedDelegate? ReportReceived = null;

    /// <summary>
    /// Creates a client MsrpConnection object. Call this method to create a client that connects to a
    /// remote endpoint that listens as a server. After calling this method, hook the events and then
    /// call the Start() method when ready to connect.
    /// </summary>
    /// <param name="LocalMsrpUri">MsrpUri of the local endpoint. The host portion of the URI must
    /// be a valid IPEndPoint object.</param>
    /// <param name="RemoteMsrpUri">MsrpUri of the remote endpoint to connect to. The host portion of the
    /// URI must be a valid IPEndPoint.</param>
    /// <param name="LocalCert">X.509 certificate to use for MSRP over TLS (MSRPS) for optional mutual
    /// authentication as a client. Optional. May be null is not using MSRP over TLS.</param>
    /// <returns>Returns a new MsrpConnection object.</returns>
    public static MsrpConnection CreateAsClient(MsrpUri LocalMsrpUri, MsrpUri RemoteMsrpUri, 
        X509Certificate2 LocalCert)
    {
        MsrpConnection Mc = new MsrpConnection();
        Mc.m_LocalMsrpUri = LocalMsrpUri;
        Mc.m_RemoteMsrpUri = RemoteMsrpUri;
        Mc.m_Certificate = LocalCert;

        Mc.m_TcpClient = new TcpClient(LocalMsrpUri.uri.ToSIPEndPoint().GetIPEndPoint());
        Mc.ConnectionIsPassive = false;
        return Mc;
    }

    /// <summary>
    /// Start the connection request to the remote endpoint server. Only use this method after calling
    /// the CreateAsClient() method.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if this method is called for a MSRP server</exception>
    private void StartClientConnection()
    {
        if (m_Started == true)
            return;

        if (m_TcpClient == null || ConnectionIsPassive == true)
            throw new InvalidOperationException("This method can only be called for client connections");

        m_Started = true;
        IPEndPoint RemIpe = m_RemoteMsrpUri.uri.ToSIPEndPoint().GetIPEndPoint();
        m_TcpClient.BeginConnect(RemIpe.Address, RemIpe.Port, ClientConnectCallback, this);
    }

    /// <summary>
    /// Starts the server listening for connection requests. Call this method after calling the CreateAsServer()
    /// method.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if this method is called for a MSRP client.</exception>
    private void StartListening()
    {
        if (m_Started == true)
            return;

        if (m_TcpListener == null || ConnectionIsPassive == false)
            throw new InvalidOperationException("This method can only be used for MSRP servers");

        m_Started = true;
        m_TcpListener.Start();
        m_TcpListener.BeginAcceptTcpClient(AcceptCallback, m_TcpListener);
    }

    /// <summary>
    /// Starts the connection process as a MSRP client or as a MSRP server depending upon the configuration.
    /// </summary>
    public void Start()
    {
        if (m_Started == true)
            return;

        if (ConnectionIsPassive == true)
            StartListening();
        else
            StartClientConnection();
    }

    /// <summary>
    /// Creates a new MsrpConnection object that listens for MSRP connection requests as a server.
    /// After calling this method, hook the events and then call the Start() method.
    /// </summary>
    /// <param name="LocalMsrpUri">Specifies the MsrpUri that the server listens on. The host portion of
    /// the URI must be a valid IPEndPoint.</param>
    /// <param name="RemoteMsrpUri">Specifies the MsrpUri that the remote client will be connecting from.
    /// The host portion of the URI must be a valid IPEndPoint.</param>
    /// <param name="LocalCert">X.509 certificate to use. Required if using MSRP over TLS (MSRPS). May
    /// be null if using MSRP over TCP.</param>
    /// <returns>Returns a new MsrpConnection object.</returns>
    public static MsrpConnection CreateAsServer(MsrpUri LocalMsrpUri, MsrpUri RemoteMsrpUri,
        X509Certificate2 LocalCert)
    {
        if (LocalMsrpUri.uri.Scheme == SIPSchemesEnum.msrps && LocalCert == null)
            throw new ArgumentException("Must provide a LocalCert parameter if using MSRPS");

        MsrpConnection Mc = new MsrpConnection();
        Mc.m_LocalMsrpUri = LocalMsrpUri;
        Mc.m_RemoteMsrpUri= RemoteMsrpUri;
        Mc.m_Certificate = LocalCert;
        Mc.m_TcpListener = new TcpListener(LocalMsrpUri.uri.ToSIPEndPoint().GetIPEndPoint());
        Mc.ConnectionIsPassive = true;
        return Mc;
    }

    /// <summary>
    /// Creates a MsrpConnection from the offered and answered SDP.
    /// </summary>
    /// <param name="OfferedMd">The offered MediaDescription for MSRP ("message") media</param>
    /// <param name="AnsweredMd">The answered MediaDescription for MSRP ("message") media</param>
    /// <param name="IsIncoming">If true then its an incoming call. Else its an outgoing call</param>
    /// <param name="LocalCert">X.509 certificate to use for MSRP over TLS (MSRPS)</param>
    /// <returns>Returns a tuple containing a MsrpConnection object and a string. If the returned MsrpConnection
    /// object is null, then the will contain an error message explaining why the MsrpConnection could no be
    /// created.</returns>
    public static (MsrpConnection?, string?) CreateFromSdp(MediaDescription OfferedMd,
        MediaDescription AnsweredMd, bool IsIncoming, X509Certificate2? LocalCert)
    {
        MsrpConnection? connection = null;

        MsrpUri LocalMsrpUri;
        MsrpUri RemoteMsrpUri;

        MsrpUri? OfferedMsrpUri = GetPathMsrpUri(OfferedMd);
        if (OfferedMsrpUri == null)
            return (null, "The offered MsrpUri is not valid");

        MsrpUri? AnsweredMsrpUri = GetPathMsrpUri(AnsweredMd);
        if (AnsweredMsrpUri == null)
            return (null, "The answered MsrpUri is not valid");

        SetupType OfferedSetupType = OfferedMd.GetSetupTypeAttributeValue();
        SetupType AnsweredSetupType = AnsweredMd.GetSetupTypeAttributeValue();

        if (IsIncoming == true)
        {
            LocalMsrpUri = AnsweredMsrpUri;
            RemoteMsrpUri = OfferedMsrpUri;
        }
        else
        {
            LocalMsrpUri = OfferedMsrpUri;
            RemoteMsrpUri = AnsweredMsrpUri;
        }

        bool IsServer = true;
        if (OfferedSetupType == SetupType.unknown || AnsweredSetupType == SetupType.unknown)
        {
            if (IsIncoming == true)
                IsServer = true;
            else
                IsServer = false;
        }
        else
        {
            if (IsIncoming == true)
            {
                if (AnsweredSetupType == SetupType.active)
                    IsServer = false;
                else
                    IsServer = true;
            }
            else
            {
                if (AnsweredSetupType == SetupType.passive)
                    IsServer = false;
                else
                    IsServer = true;
            }
        }

        if (IsServer == true)
            connection = CreateAsServer(LocalMsrpUri, RemoteMsrpUri, LocalCert);
        else
            connection = CreateAsClient(LocalMsrpUri, RemoteMsrpUri, LocalCert);

        return (connection, null);
    }

    private static MsrpUri? GetPathMsrpUri(MediaDescription Md)
    {
        MsrpUri? msrpUri = null;
        string strPathAttr = Md.GetAttributeValue("path");
        if (string.IsNullOrEmpty(strPathAttr) == true)
            return null;

        msrpUri = MsrpUri.ParseMsrpUri(strPathAttr);
        return msrpUri;
    }

    /// <summary>
    /// Called by the TcpListener object when a client attempts to connect.
    /// </summary>
    /// <param name="Iar">The AsuncState property is set to the TcpListener object, but its not used.</param>
    private void AcceptCallback(IAsyncResult Iar)
    {
        if (m_ShuttingDown == true)
            return;

        try
        {
            TcpClient tcpClient = m_TcpListener.EndAcceptTcpClient(Iar);

            // Verify that the remote endpoint is who it should be, i.e. it matches the information in the
            // m_RemoteMsrpUri object.
            IPEndPoint ClientRemIpe = (IPEndPoint)tcpClient.Client.RemoteEndPoint;
            IPEndPoint RemIpe = m_RemoteMsrpUri.uri.ToSIPEndPoint().GetIPEndPoint();

            if (ClientRemIpe.Equals(RemIpe) == false)
            {
                CancelTcpClient(tcpClient);
                return;
            }

            StopTcpClient();    // Only allow one client connection at a time.
            m_TcpClient = tcpClient;
            m_Qos.SetTcpDscp(m_TcpClient, DscpSettings.MSRPDscp, m_RemoteMsrpUri.uri.ToSIPEndPoint().
                GetIPEndPoint());
            if (m_LocalMsrpUri.uri.Scheme == SIPSchemesEnum.msrps)
            {
                SslStream sslStream = new SslStream(m_TcpClient.GetStream(), false);
                m_NetworkStream = sslStream;
                sslStream.BeginAuthenticateAsServer(m_Certificate, AuthenticateAsServerCallback, this);
            }
            else
            {
                m_NetworkStream = m_TcpClient.GetStream();
                MsrpConnectionEstablished?.Invoke(true, m_RemoteMsrpUri);
                m_NetworkStream.BeginRead(m_ReadBuffer, 0, m_ReadBuffer.Length, StreamReadCallback, 
                    m_NetworkStream);

                SendEmptySendRequest();
            }

            // Listen for another connection request
            m_TcpListener.BeginAcceptTcpClient(AcceptCallback, m_TcpListener);
        }
        catch (SocketException) { }
        catch (IOException) { }
        catch (ObjectDisposedException) { }
        catch (NullReferenceException) { }
    }

    private void CancelTcpClient(TcpClient client)
    {
        try
        {
            client.Close();
        }
        catch { }
    }

    /// <summary>
    /// Called by the SslStream object when authenticating as a server when using MSRP over TLS.
    /// </summary>
    /// <param name="Iar">The AsyncState property is set to this MsrpConnection object but its not used.</param>
    private void AuthenticateAsServerCallback(IAsyncResult Iar)
    {
        try
        {
            SslStream sslStream = (SslStream)m_NetworkStream;
            sslStream.EndAuthenticateAsServer(Iar);
            MsrpConnectionEstablished?.Invoke(ConnectionIsPassive, m_RemoteMsrpUri);
            m_NetworkStream.BeginRead(m_ReadBuffer, 0, m_ReadBuffer.Length, StreamReadCallback, m_NetworkStream);
            
            SendEmptySendRequest();
        }
        catch (Exception) { }
    }

    private void StopTcpClient()
    {
        if (m_TcpClient == null)
            return;

        try
        {
            if (m_NetworkStream != null)
                m_NetworkStream.Close();

            if (m_TcpClient.Connected == true)
            {
                m_TcpClient.Client.Shutdown(SocketShutdown.Both);
                m_TcpClient.Close();
            }
        }
        catch (SocketException) { }
        catch (ObjectDisposedException) { }
        catch (InvalidOperationException) { }
        catch (Exception) { }
        finally
        {
            m_TcpClient = null;
            m_NetworkStream = null;
        }

    }

    /// <summary>
    /// Called by the TcpClient object when connecting as a client when the connection to the server is
    /// established.
    /// </summary>
    /// <param name="Iar">Ths AsyncState property is set to this MsrpConnection object, but its not used.</param>
    private void ClientConnectCallback(IAsyncResult Iar)
    {
        //MsrpConnection Mc = (MsrpConnection)Iar.AsyncState;
        Exception Excpt = null;

        try
        {
            m_TcpClient.EndConnect(Iar);
            m_Qos.SetTcpDscp(m_TcpClient, DscpSettings.MSRPDscp, m_RemoteMsrpUri.uri.ToSIPEndPoint().
                GetIPEndPoint());
            if (m_LocalMsrpUri.uri.Scheme == SIPSchemesEnum.msrps)
            {
                SslStream sslStream = new SslStream(m_TcpClient.GetStream(), false, ValidateServerCertificate!, null);
                m_NetworkStream = sslStream;
                if (m_Certificate == null)
                    sslStream.BeginAuthenticateAsClient("*", AuthenticateAsClientCallback, this);
                else
                {
                    X509CertificateCollection Col = new X509CertificateCollection();
                    Col.Add(m_Certificate);
                    sslStream.BeginAuthenticateAsClient("*", Col, false, AuthenticateAsClientCallback, this);
                }
            }
            else
            {
                m_NetworkStream = m_TcpClient.GetStream();
                if (m_NetworkStream != null)
                {
                    MsrpConnectionEstablished?.Invoke(ConnectionIsPassive, m_RemoteMsrpUri);
                    SendEmptySendRequest();

                    // Start reading from the stream asynchronously
                    m_NetworkStream.BeginRead(m_ReadBuffer, 0, m_ReadBuffer.Length, StreamReadCallback, 
                        m_NetworkStream);
                }
                else
                {   // An error occurred
                    MsrpConnectionFailed?.Invoke(ConnectionIsPassive, m_RemoteMsrpUri);
                }
            }
        }
        catch (SocketException Se) { Excpt = Se; }
        catch (ObjectDisposedException Ode) { Excpt = Ode; }
        catch (Exception Ex) { Excpt = Ex; }

        if (Excpt != null)
        {
            MsrpConnectionFailed?.Invoke(ConnectionIsPassive, m_RemoteMsrpUri);
        }
    }

    /// <summary>
    /// Called to complete the authentication as a client.
    /// </summary>
    /// <param name="ar">The AsyncState property is set to this MsrpConnection object.</param>
    private void AuthenticateAsClientCallback(IAsyncResult ar)
    {
        MsrpConnection Mc = (MsrpConnection)ar.AsyncState;
        SslStream sslStream = (SslStream)Mc.m_NetworkStream;
        try
        {
            sslStream.EndAuthenticateAsClient(ar);
            SendEmptySendRequest(); 
            MsrpConnectionEstablished?.Invoke(ConnectionIsPassive, m_RemoteMsrpUri);
            sslStream.BeginRead(m_ReadBuffer, 0, m_ReadBuffer.Length, StreamReadCallback, sslStream);
        }
        catch (Exception)
        {
            MsrpConnectionFailed?.Invoke(ConnectionIsPassive, m_RemoteMsrpUri);
        }
    }

    /// <summary>
    /// Callback function that is called when there is data in the stream to read.
    /// </summary>
    /// <param name="Iar"></param>
    private void StreamReadCallback(IAsyncResult Iar)
    {
        int BytesRead = 0;
        bool CompleteMesage = false;
        Stream Ns = (Stream)Iar.AsyncState;
        if (Ns == null || Ns.CanRead == false)
            return;

        try
        {
            BytesRead = Ns.EndRead(Iar);
            if (BytesRead > 0)
            {
                for (int i = 0; i < BytesRead; i++)
                {
                    CompleteMesage = m_StreamParser.ProcessByte(m_ReadBuffer[i]);
                    if (CompleteMesage == true)
                    {
                        byte[] bytes = m_StreamParser.GetMessageBytes();
                        ProcessCompleteMsrpMessage(bytes);
                    }
                }

                Array.Clear(m_ReadBuffer, 0, m_ReadBuffer.Length);
                // Keep reading from the network stream
                Ns.BeginRead(m_ReadBuffer, 0, m_ReadBuffer.Length, StreamReadCallback, Ns);
            }
        }
        catch (ObjectDisposedException) { }
        catch (InvalidOperationException) { }
        catch (IOException ) { }
    }

    private List<MsrpMessage> m_ReceivedMessagChunks = new List<MsrpMessage>();

    /// <summary>
    /// Called when a complete MSRP message transaction message has been received.
    /// </summary>
    /// <param name="bytes"></param>
    private void ProcessCompleteMsrpMessage(byte[] bytes)
    {
        MsrpMessage msrpMessage = MsrpMessage.ParseMsrpMessage(bytes);
        if (msrpMessage == null)
        {   // Error: Unable to parse the message
            EnqueueMsrpResponseMessage(msrpMessage.BuildResponseMessage(400, "Bad Request"));
            return;
        }

        if (msrpMessage.MessageType == MsrpMessageType.Request)
        {
            if (msrpMessage.RequestMethod == "SEND" || msrpMessage.RequestMethod == "REPORT" ||
                msrpMessage.RequestMethod == "NICKNAME")
            {   // Send a 200 OK response to the transaction
                EnqueueMsrpResponseMessage(msrpMessage.BuildResponseMessage(200, "OK"));
            }
            else
            {   // Unknown request method
                EnqueueMsrpResponseMessage(msrpMessage.BuildResponseMessage(501, "Unknown Method"));
                return;
            }

            if (msrpMessage.RequestMethod == "SEND")
            {
                if (msrpMessage.Body != null)
                {
                    if (msrpMessage.CompletionStatus == MsrpCompletionStatus.Complete)
                        ProcessCompletedSendRequest(msrpMessage);
                    else if (msrpMessage.CompletionStatus == MsrpCompletionStatus.Continuation)
                        m_ReceivedMessagChunks.Add(msrpMessage);
                    else if (msrpMessage.CompletionStatus == MsrpCompletionStatus.Truncated)
                        // Message truncated or aborted by the sender
                        m_ReceivedMessagChunks.Clear();
                }

                // Else its an empty SEND request so just OK it (done above)
            }
            else if (msrpMessage.RequestMethod == "REPORT")
            {
                ReportReceived?.Invoke(msrpMessage.MessageID, msrpMessage.ByteRange.Total,
                    msrpMessage.Status.StatusCode, msrpMessage.Status.Comment);
            }

        }
        else if (msrpMessage.MessageType == MsrpMessageType.Response)
        {
            m_ResponseMessage = msrpMessage;
            m_TransmitTaskSemaphore.Release();  // Signal the transmit task that a response was received
        }
    }

    /// <summary>
    /// This method is not called for empty SEND requests.
    /// </summary>
    /// <param name="message"></param>
    private void ProcessCompletedSendRequest(MsrpMessage message)
    {
        if (m_ReceivedMessagChunks.Count == 0)
        {   // The received message is complete
            FireMsrpMessageReceived(message.ContentType, message.Body);
            FireMsrpTextMessageReceived(message.ContentType, message.Body);
        }
        else
        {   // The message was chunked so build up the entire contents
            m_ReceivedMessagChunks.Add(message);
            int TotalBytesReceived = 0;
            foreach (MsrpMessage msg in m_ReceivedMessagChunks)
                TotalBytesReceived += msg.Body.Length;

            byte[] buffer = new byte[TotalBytesReceived];
            int CurIdx = 0;

            foreach (MsrpMessage msg in m_ReceivedMessagChunks)
            {
                Array.ConstrainedCopy(msg.Body, 0, buffer, CurIdx, msg.Body.Length);
                CurIdx += msg.Body.Length;
            }

            FireMsrpMessageReceived(message.ContentType, buffer);
            FireMsrpTextMessageReceived(message.ContentType, buffer);
        }

        m_ReceivedMessagChunks.Clear();

        // If a success report is requested then send a REPORT request
        if (message.SuccessReportRequested() == true && HaveEventListeners() == true)
        {   // There are listeners for the events so assume message delivery was successful
            SendReportRequest(message, 200, "OK");
        }
        else if (message.FailureReportRequested() == true && HaveEventListeners() == false)
        {   // There are no listeners for the events so assume that message delivery was not successful.
            SendReportRequest(message, 503, "Service Unavailable -- No Listeners");
        }
    }

    private bool HaveEventListeners()
    {
        if (MsrpMessageReceived == null && MsrpTextMessageReceived == null)
            return false;
        else
            return true;
    }

    private void FireMsrpMessageReceived(string ContentType, byte[] Contents)
    {
        string from = m_RemoteMsrpUri.uri.User != null ? m_RemoteMsrpUri.uri.User : m_RemoteMsrpUri.uri.Host;
        MsrpMessageReceived?.Invoke(ContentType, Contents, from);
    }

    private void FireMsrpTextMessageReceived(string ContentType, byte[] Contents)
    {
        string message = string.Empty;
        string from = string.Empty;
        string strContents = Encoding.UTF8.GetString(Contents);

        if (ContentType.ToLower() == "text/plain")
        {
            message = strContents;
            from = m_RemoteMsrpUri.uri.User != null ? m_RemoteMsrpUri.uri.User : m_RemoteMsrpUri.uri.Host;
        }
        else if (ContentType.ToLower() == "message/cpim")
        {
            CpimMessage? cpim = CpimMessage.ParseCpimBytes(Contents);
            if (cpim != null && cpim?.ContentType == "text/plain")
            {
                if (cpim.Body != null)
                    message = Encoding.UTF8.GetString((byte[])cpim.Body);
                else
                    message = "Empty CPIM message received";

                from = cpim.From.Name != null ? cpim.From.Name : cpim.From.ToString();
            }
            else
            {   // Error: Invalid CpimMessage or its not a CPIM message with text in the body
                return;
            }
        }
        else
            return;

        MsrpTextMessageReceived?.Invoke(message, from);
    }

    private void SendReportRequest(MsrpMessage message, int StatusCode, string StatusText)
    {
        MsrpMessage RptMessage = new MsrpMessage();
        RptMessage.MessageType = MsrpMessageType.Request;
        RptMessage.RequestMethod = "REPORT";
        RptMessage.ToPath = message.FromPath;
        RptMessage.FromPath = message.ToPath;
        RptMessage.MessageID = message.MessageID;
        RptMessage.ByteRange = new ByteRangeHeader();
        RptMessage.ByteRange.Start = 1;
        RptMessage.ByteRange.End = message.ByteRange.Total;
        RptMessage.ByteRange.Total = message.ByteRange.Total;
        RptMessage.Status = new MsrpStatusHeader();
        RptMessage.Status.StatusCode = StatusCode;
        RptMessage.Status.Comment = StatusText;
        EnqueueMsrpRequestMessage(RptMessage);
    }

    private void EnqueueMsrpResponseMessage(MsrpMessage message)
    {
        m_ResponseTransmitQueue.Enqueue(message);
        m_TransmitTaskSemaphore.Release();      // Signal the transmit task.
    }

    private void EnqueueMsrpRequestMessage(MsrpMessage message)
    {
        m_RequestTransmitQueue.Enqueue(message);
        m_TransmitTaskSemaphore.Release();      // Signal the transmit task.
    }

    private const int MAX_TRANSMIT_RETRIES = 3;
    private const int TRANSMIT_TIMEOUT_MS = 500;    // Transmit timeout in milliseconds

    /// <summary>
    /// Background task that handles all transmit operations
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task TransmitTask(CancellationToken cancellationToken)
    {
        MsrpMessage CurrentRequest = null;
        DateTime RequestSentTime = DateTime.Now;
        int NumTransmitAttempts = 0;

        MsrpMessage ResponseMessage;
        byte[] RequestBytes = null;

        try
        {
            while (cancellationToken.IsCancellationRequested == false)
            {
                await m_TransmitTaskSemaphore.WaitAsync(100, cancellationToken);
                if (m_NetworkStream == null || m_NetworkStream.CanWrite == false)
                    continue;

                // Send all MSRP response messages that are queued
                while (m_ResponseTransmitQueue.TryDequeue(out ResponseMessage) == true)
                {
                    await SendBytes(ResponseMessage.ToByteArray());
                }

                if (CurrentRequest == null)
                {   // Not currently waiting for a response to a request
                    if (m_RequestTransmitQueue.TryDequeue(out CurrentRequest) == true)
                    {
                        m_ResponseMessage = null;
                        RequestBytes = CurrentRequest.ToByteArray();
                        await SendBytes(RequestBytes);
                        if (CurrentRequest.RequestMethod == "REPORT")
                        {   // Don't expect a response to a REPORT request.
                            CurrentRequest = null;
                        }
                        else
                        {
                            RequestSentTime = DateTime.Now;
                            NumTransmitAttempts = 1;
                        }
                    }
                }
                else
                {   // Waiting for a response to the current request.
                    if (m_ResponseMessage != null)
                    {   // A response was received
                        if (CurrentRequest.TransactionID == m_ResponseMessage.TransactionID)
                        {
                            if (m_ResponseMessage.ResponseCode != 200)
                                // An error code was returned
                                MsrpMessageDeliveryFailed?.Invoke(CurrentRequest, m_RemoteMsrpUri, m_ResponseMessage.
                                    ResponseCode, m_ResponseMessage.ResponseText);

                            CurrentRequest = null;
                            m_TransmitTaskSemaphore.Release();
                        }
                        else
                        {   // A response was received, but not for the last request message that was sent.
                            // Try sending the current request again
                            NumTransmitAttempts += 1;
                            if (NumTransmitAttempts <= MAX_TRANSMIT_RETRIES)
                            {
                                await SendBytes(RequestBytes!);
                                RequestSentTime = DateTime.Now;
                            }
                            else
                            {   // Too many attempts
                                MsrpMessageDeliveryFailed?.Invoke(CurrentRequest, m_RemoteMsrpUri!, 481,
                                    "Timeout");

                                // Give up on the current request
                                CurrentRequest = null;
                                m_TransmitTaskSemaphore.Release();
                            }
                        }

                        m_ResponseMessage = null;
                    }
                    else
                    {   // Test for a timeout condition
                        TimeSpan Ts = DateTime.Now - RequestSentTime;
                        if (Ts.TotalMilliseconds > TRANSMIT_TIMEOUT_MS)
                        {   // A timeout occurred
                            NumTransmitAttempts += 1;
                            if (NumTransmitAttempts <= MAX_TRANSMIT_RETRIES)
                            {   // Try again
                                await SendBytes(RequestBytes!);
                                RequestSentTime = DateTime.Now;
                            }
                            else
                            {   // Too many attempts
                                MsrpMessageDeliveryFailed?.Invoke(CurrentRequest, m_RemoteMsrpUri!, 481,
                                    "Timeout");

                                // Give up on the current request
                                CurrentRequest = null;
                                m_TransmitTaskSemaphore.Release();
                            }
                        }
                    }
                }
            }
        }
        catch (TaskCanceledException) { }
        catch (Exception) { }
    }

    private async Task SendBytes(byte[] bytes)
    {
        if (m_ShuttingDown == true || m_NetworkStream == null)
            return;

        try
        {
            await m_NetworkStream.WriteAsync(bytes, 0, bytes.Length);
        }
        catch (IOException) { }
        catch (ObjectDisposedException) { }
        catch (NullReferenceException) { }
        catch (Exception) { }
    }

    private MsrpMessage BuildSendRequest(string MessageID)
    {
        MsrpMessage msg = new MsrpMessage();
        msg.MessageID = MessageID;
        msg.MessageType = MsrpMessageType.Request;
        msg.RequestMethod = "SEND";
        msg.ToPath.MsrpUris.Add(m_RemoteMsrpUri);
        msg.FromPath.MsrpUris.Add(m_LocalMsrpUri);
        return msg;
    }

    private void SendEmptySendRequest()
    {
        string MessageID = MsrpMessage.NewRandomID();  // Use a new random ID;
        MsrpMessage msg = BuildSendRequest(MessageID);
        msg.ByteRange = new ByteRangeHeader() { Start = 1, End = 0, Total = 0 };
        EnqueueMsrpRequestMessage(msg);
    }

    /// <summary>
    /// Sends an MSRP message to the remote endpoint. The method queues the message for transmission and
    /// returns immediately. It does not block. If the length of the message contents is longer than
    /// 2048 bytes, then the message is split up into chunks and each message chunk is queued.
    /// To send an empty SEND request, set the ContentType and the Contents parameters to null.
    /// </summary>
    /// <param name="ContentType">Specifies the Content-Type header value for the message. For example:
    /// text/plain or message/CPIM.</param>
    /// <param name="Contents">Binary message contents encoded using UTF8 if the message is text or
    /// the binary contents if sending a non-text message such as a picture or a video file.</param>
    /// <param name="messageID">If specified, this is the Message-ID header that will be include
    /// in the SEND request. This should be a 9 or 10 character alphanumeric string that identifies the
    /// message. This parameter is optional. If present then the SEND request will include a
    /// Success-Report header with a value of "yes" so that the remote endpoint will generate a
    /// success report.</param>
    public void SendMsrpMessage(string ContentType, byte[] Contents, string? messageID = null)
    {
        string MessageID;
        bool RequestSuccessReport = false;
        bool RequestFailureReport = false;

        if (string.IsNullOrEmpty(messageID) == true)
            MessageID = MsrpMessage.NewRandomID();  // Use a new random ID
        else
        {
            MessageID = messageID;
            RequestSuccessReport = true;
            RequestFailureReport = true;
        }

        MsrpMessage msg;

        int NumChunks = Contents.Length / CHUNK_SIZE;
        if (Contents.Length % CHUNK_SIZE != 0)
            NumChunks += 1;

        int CurrentStartIdx = 0;
        int BytesRemaining = Contents.Length;
        int CurrentChunkSize = BytesRemaining >= CHUNK_SIZE ? CHUNK_SIZE : BytesRemaining;
        int CurrentEndIdx = CurrentStartIdx + CurrentChunkSize - 1;

        for (int i = 0; i < NumChunks; i++)
        {
            msg = BuildSendRequest(MessageID);
            msg.ContentType = ContentType;

            if (RequestSuccessReport == true)
                msg.SuccessReport = "yes";
            if (RequestFailureReport == true)
                msg.FailureReport = "yes";

            msg.ByteRange = new ByteRangeHeader() { Start = CurrentStartIdx + 1, End = CurrentEndIdx + 1,
                Total = Contents.Length };
            msg.Body = new byte[CurrentChunkSize];
            Array.ConstrainedCopy(Contents, CurrentStartIdx, msg.Body, 0, CurrentChunkSize);

            if (i == NumChunks - 1)
                msg.CompletionStatus = MsrpCompletionStatus.Complete;
            else
                msg.CompletionStatus = MsrpCompletionStatus.Continuation;

            EnqueueMsrpRequestMessage(msg);

            CurrentStartIdx = CurrentStartIdx + CurrentChunkSize;
            BytesRemaining -= CurrentChunkSize;
            CurrentChunkSize = BytesRemaining >= CHUNK_SIZE ? CHUNK_SIZE : BytesRemaining;
            CurrentEndIdx = CurrentEndIdx + CurrentChunkSize;
        }
    }

    /// <summary>
    /// Call this method to close all network connections. This method must be called when a call ends or
    /// if the MSRP session must be ended in case of a re-INVITE request to change the media destination
    /// or characteristics.
    /// </summary>
    public void Shutdown()
    {
        m_ShuttingDown = true;

        m_TokenSource.Cancel();     // Tell the TransmitTask to stop

        if (m_TcpClient != null)
        {
            m_Qos.Shutdown();
            StopTcpClient();
        }

        if (m_TcpListener != null)
        {
            try
            {
                m_TcpListener.Stop();
                m_TcpListener = null;
            }
            catch (SocketException) { }
            catch (Exception) { }
        }
    }

    /// <summary>
    /// Callback function called by the SslStream class that to validate the remote server's X.509 certificate.
    /// This function currently accepts any certificate that is provided by the remote server.
    /// </summary>
    /// <param name="sender">Not used.</param>
    /// <param name="certificate">Certificate that was provided by the remote server.</param>
    /// <param name="chain">The chain of certificate authorities associated with the remote certificate.</param>
    /// <param name="sslPolicyErrors">One or more errors associated with the remote certificate.</param>
    /// <returns>Return true to indicate that the specified certificate is acceptable for authentication or
    /// false if it is not acceptable.
    /// </returns>
    private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain,
        SslPolicyErrors sslPolicyErrors)
    {
        if (sslPolicyErrors == SslPolicyErrors.None)
            return true;
        else
            // Ignore errors. Any certificate is OK
            return true;
    }
}
