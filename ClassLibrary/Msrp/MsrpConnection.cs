/////////////////////////////////////////////////////////////////////////////////////
//  File:   MsrpConnection.cs                                       29 Jul 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Msrp;

using System.Security.Cryptography.X509Certificates;
using System.Net.Sockets;
using System.Net;

using SipLib.Channels;
using SipLib.Core;
using System.Net.Security;
using System.Collections.Concurrent;
using Org.BouncyCastle.Crypto.Paddings;

/// <summary>
/// Class for managing a single MSRP connection to either a remote server or a remote client.
/// </summary>
public class MsrpConnection
{
    private const int DEFAULT_BUFFER_LENGTH = 4096;

    private TcpListener m_TcpListener = null;
    private TcpClient m_TcpClient;
    private MsrpUri m_RemoteMsrpUri = null;
    private MsrpUri m_LocalMsrpUri = null;
    private X509Certificate2 m_Certificate = null;

    private Stream m_NetworkStream = null;

    private MsrpStreamParser m_StreamParser = new MsrpStreamParser();
    private byte[] m_ReadBuffer = new byte[DEFAULT_BUFFER_LENGTH];

    /// <summary>
    /// Returns true if the connection is passive, i.e., this end is the server and listening for connection
    /// requests. Returns false if this end of the connection is the client.
    /// </summary>
    public bool ConnectionIsPassive = false;

    private Qos m_Qos = new Qos();
    private bool m_ShuttingDown = false;

    private ConcurrentQueue<MsrpMessage> m_RequestTransmitQueue = new ConcurrentQueue<MsrpMessage>();
    private ConcurrentQueue<MsrpMessage> m_ResponseTransmitQueue = new ConcurrentQueue<MsrpMessage>();
    private SemaphoreSlim m_TransmitTaskSemaphore = new SemaphoreSlim(0, int.MaxValue);
    private CancellationTokenSource m_TokenSource = new CancellationTokenSource();
     private MsrpMessage m_ResponseMessage = null;

    /// <summary>
    /// Constructor
    /// </summary>
    private MsrpConnection()
    {
        // Don't wait, let it run in the background
        Task transmitTask = TransmitTask(m_TokenSource.Token);
    }

    /// <summary>
    /// Event that is fired when a complete MSRP message is received. This event is not fired for empty
    /// SEND requests.
    /// </summary>
    public event MsrpMessageReceivedDelegate MsrpMessageReceived;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="LocalMsrpUri"></param>
    /// <param name="RemoteMsrpUri"></param>
    /// <param name="LocalCert"></param>
    /// <returns></returns>
    public static MsrpConnection CreateAsClient(MsrpUri LocalMsrpUri, MsrpUri RemoteMsrpUri, 
        X509Certificate2 LocalCert)
    {
        MsrpConnection Mc = new MsrpConnection();
        Mc.m_LocalMsrpUri = LocalMsrpUri;
        Mc.m_RemoteMsrpUri = RemoteMsrpUri;
        Mc.m_Certificate = LocalCert;

        Mc.m_TcpClient = new TcpClient();
        Mc.m_TcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        Mc.m_TcpClient.Client.Bind(LocalMsrpUri.uri.ToSIPEndPoint().GetIPEndPoint());
        Mc.ConnectionIsPassive = false;
        IPEndPoint RemIpe = RemoteMsrpUri.uri.ToSIPEndPoint().GetIPEndPoint();
        Mc.m_TcpClient.BeginConnect(RemIpe.Address, RemIpe.Port, Mc.ClientConnectCallback, Mc);

        return Mc;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="LocalMsrpUri"></param>
    /// <param name="RemoteMsrpUri"></param>
    /// <param name="LocalCert"></param>
    /// <returns></returns>
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
        Mc.m_TcpListener.Start();
        Mc.m_TcpListener.BeginAcceptTcpClient(Mc.AcceptCallback, Mc.m_TcpListener);

        return Mc;
    }

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
                m_NetworkStream.BeginRead(m_ReadBuffer, 0, m_ReadBuffer.Length, StreamReadCallback, 
                    m_NetworkStream);

                // TODO: Send an empty MSRP SEND request

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

    private void AuthenticateAsServerCallback(IAsyncResult Iar)
    {
        try
        {
            SslStream sslStream = (SslStream)m_NetworkStream;
            sslStream.EndAuthenticateAsServer(Iar);
            m_NetworkStream.BeginRead(m_ReadBuffer, 0, m_ReadBuffer.Length, StreamReadCallback, m_NetworkStream);

            // TODO: Send an empty MSRP SEND request

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

    private void ClientConnectCallback(IAsyncResult Iar)
    {
        MsrpConnection Mc = (MsrpConnection)Iar.AsyncState;
        Exception Excpt = null;

        try
        {
            m_TcpClient.EndConnect(Iar);
            m_Qos.SetTcpDscp(Mc.m_TcpClient, DscpSettings.MSRPDscp, Mc.m_RemoteMsrpUri.uri.ToSIPEndPoint().
                GetIPEndPoint());
            if (m_LocalMsrpUri.uri.Scheme == SIPSchemesEnum.msrps)
            {
                SslStream sslStream = new SslStream(m_TcpClient.GetStream(), false, ValidateServerCertificate, null);
                m_NetworkStream = sslStream;
                if (m_Certificate == null)
                    sslStream.BeginAuthenticateAsClient("*", AuthenticateAsClientCallback, Mc);
                else
                {
                    X509CertificateCollection Col = new X509CertificateCollection();
                    Col.Add(m_Certificate);
                    sslStream.BeginAuthenticateAsClient("*", Col, false, AuthenticateAsClientCallback, Mc);
                }
            }
            else
            {
                m_NetworkStream = m_TcpClient.GetStream();
                if (m_NetworkStream == null)
                {
                    // TODO: Send an empty message

                    // TODO: Fire an event that indicates that the connection is established

                    // Start reading from the stream asynchronously
                    m_NetworkStream.BeginRead(m_ReadBuffer, 0, m_ReadBuffer.Length, StreamReadCallback, 
                        m_NetworkStream);
                }
                else
                {   // An error occurred

                    // TODO: Fire an event that indicates a connection occurred.

                }
            }
        }
        catch (SocketException Se) { Excpt = Se; }
        catch (ObjectDisposedException Ode) { Excpt = Ode; }
        catch (Exception Ex) { Excpt = Ex; }

        if (Excpt != null)
        {
            // TODO: Handle the exception
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
            // TODO: Figure out how to handle this case because the message could be either a request or
            // a response

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
                        ProcessCompletedMessage(msrpMessage);
                    else if (msrpMessage.CompletionStatus == MsrpCompletionStatus.Continuation)
                        m_ReceivedMessagChunks.Add(msrpMessage);
                    else if (msrpMessage.CompletionStatus == MsrpCompletionStatus.Truncated)
                        // Message truncated or aborted by the sender
                        m_ReceivedMessagChunks.Clear();
                }

                // Else its an empty SEND request so just OK it (done above)
            }

        }
        else if (msrpMessage.MessageType == MsrpMessageType.Response)
        {
            m_ResponseMessage = msrpMessage;
            m_TransmitTaskSemaphore.Release();  // Signal the transmit task that a response was received
        }
    }

    private void ProcessCompletedMessage(MsrpMessage message)
    {
        if (m_ReceivedMessagChunks.Count == 0)
        {   // The received message is complete
            MsrpMessageReceived?.Invoke(message.GetContentType(), message.Body);
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

            MsrpMessageReceived?.Invoke(message.GetContentType(), buffer);
        }

        m_ReceivedMessagChunks.Clear();

        // If a success report is requested then send a REPORT request
        if (message.SuccessReportRequested() == true && MsrpMessageReceived != null)
        {   // There are listeners for the MsrpMessageReceived event so assume message delivery was
            // successful
            SendReportRequest(message, 200, "OK");
        }
        else if (message.FailureReportRequested() == true && MsrpMessageReceived == null)
        {   // There are no listeners for the MsrpMessageReceived event so assume that message delivery
            // was not successful.
            SendReportRequest(message, 503, "Service Unavailable -- No Listeners");
        }
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
        m_TransmitTaskSemaphore.Release();
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
                if (m_NetworkStream == null)
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
                        RequestSentTime = DateTime.Now;
                        NumTransmitAttempts = 1;
                    }
                }
                else
                {   // Waiting for a response to the current request.
                    if (m_ResponseMessage != null)
                    {
                        if (CurrentRequest.TransactionID == m_ResponseMessage.TransactionID)
                        {
                            if (m_ResponseMessage.ResponseCode != 200)
                            {   // An error code was returned

                                // TODO: Fire an event for this error response

                            }

                            CurrentRequest = null;
                            m_TransmitTaskSemaphore.Release();
                        }
                        else
                        {   // A response was received, but not for the last request message that was sent.
                            // Try sending the current request again
                            NumTransmitAttempts += 1;
                            if (NumTransmitAttempts <= MAX_TRANSMIT_RETRIES)
                            {
                                await SendBytes(RequestBytes);
                                RequestSentTime = DateTime.Now;
                            }
                            else
                            {   // Too many attempts

                                // TODO: Fire an event

                                // Give up on the current request
                                CurrentRequest = null;
                                m_TransmitTaskSemaphore.Release();
                            }
                        }

                        m_ResponseMessage = null;
                    }
                    else
                    {   // Test for a timeout condition
                        TimeSpan Ts = RequestSentTime - DateTime.Now;
                        if (Ts.TotalMilliseconds > TRANSMIT_TIMEOUT_MS)
                        {   // A timeout occurred
                            NumTransmitAttempts += 1;
                            if (NumTransmitAttempts <= MAX_TRANSMIT_RETRIES)
                            {   // Try again
                                await SendBytes(RequestBytes);
                                RequestSentTime = DateTime.Now;
                            }
                            else
                            {   // Too many attempts

                                // TODO: Fire an event

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

    private void AuthenticateAsClientCallback(IAsyncResult ar)
    {
        MsrpConnection Mc = (MsrpConnection)ar.AsyncState;
        SslStream sslStream = (SslStream)Mc.m_NetworkStream;
        try
        {
            sslStream.EndAuthenticateAsClient(ar);

            SendMsrpMessage(null, null);    // Send an empty SEND request.

            // TODO: Fire an event to indicate that the connection was successful

            sslStream.BeginRead(m_ReadBuffer, 0, m_ReadBuffer.Length, StreamReadCallback, sslStream);

        }
        catch (Exception)
        {
            // TODO: Fire an event to indicate that the connection failed
        }
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

    private const int CHUNK_SIZE = 2048;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="ContentType"></param>
    /// <param name="Contents"></param>
    public void SendMsrpMessage(string ContentType, byte[] Contents)
    {
        string MessageID = MsrpMessage.NewTransactionID();  // Use a new random ID
        MsrpMessage msg;
        if (ContentType == null || Contents == null)
        {   // Send an empty SEND request
            msg = BuildSendRequest(MessageID);
            msg.ByteRange = new ByteRangeHeader() { Start = 1, End = 0, Total = 0 };
            EnqueueMsrpRequestMessage(msg);
            return;
        }

        int NumChunks = Contents.Length / CHUNK_SIZE;
        if (Contents.Length % CHUNK_SIZE != 0)
            NumChunks += 1;

        for (int i = 0; i < NumChunks; i++)
        {
            msg = BuildSendRequest(MessageID);

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
