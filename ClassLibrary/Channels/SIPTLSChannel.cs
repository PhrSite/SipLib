#region License
//-----------------------------------------------------------------------------
// Filename: SIPTLSChannel.cs
//
// Description: SIP transport for TLS over TCP.
// 
// History:
// 13 Mar 2009	Aaron Clauson	Created.
//
// License: 
// This software is licensed under the BSD License http://www.opensource.org/licenses/bsd-license.php
//
// Copyright (c) 2006 Aaron Clauson (aaron@sipsorcery.com), SIP Sorcery PTY LTD, Hobart, Australia (www.sipsorcery.com)
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without modification, are permitted provided that 
// the following conditions are met:
//
// Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer. 
// Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following 
// disclaimer in the documentation and/or other materials provided with the distribution. Neither the name of SIP Sorcery PTY LTD. 
// nor the names of its contributors may be used to endorse or promote products derived from this software without specific 
// prior written permission. 
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, 
// BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. 
// IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, 
// OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, 
// OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, 
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE 
// POSSIBILITY OF SUCH DAMAGE.
//-----------------------------------------------------------------------------
#endregion

/////////////////////////////////////////////////////////////////////////////////////
// Revised: 8 Nov 22 PHR Initial version
//          -- Code clean up to improve readability
//          -- Changed ReceiveCallback() from public to private and
//             EndAuthenticateAsServer() from public to private.
//          -- Added the User parameter to the constructor.
//          -- If no server certificate name is specified then default to accepting
//             any certificate when connecting as a client.
//          -- Modified Send() to not bind to the local endpoint of this SIPChannel.
//             Bind to the local address with a random port that is picked by the
//             operating system.
//          -- Modified Send() to use Write() instead of BeginWrite() because a
//             NotSupported exception is thrown when back-to-back writes occur.
//          -- Added handling of an IOExeption in the Send() function.
//          -- Added LockCollections() and UnlockCollections() to lock and unlock the
//             collections in try/finally blocks because they were being accessed
//             from different threads.
//          -- Switch to using synchronous reads because there are significant
//             performance issues with asynchronous reads when running under Linux.
//          -- Added ChannelStarted
//          -- Changed AcceptConnections to a permanent thread because of
//             occasionally slow startup times due to the asynchronous nature of
//             ThreadPool.QueueUserWorkItem().
//          -- Added support for mutual authentication and added the UseMutualAuth
//             parameter to the constructor.
//          -- Added the ForceCloseConnection() virtual function.
//          -- Modified EndAuthenticateAsServer() to force close the existing
//             connection if a client is reconnecting using the same remote
//             endpoint.
//          -- Added documentation comments
//          23 Aug 23 PHR
//          -- Modified LockCollections() and UnlockCollections() to use a single
//             lock object
//          -- Added the SIPConnectonFailed and the SIPConnectionDisconnected events
/////////////////////////////////////////////////////////////////////////////////////

using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using SipLib.Core;

namespace SipLib.Channels;

/// <summary>
/// Class for managing SIP connections using the Transport Layer Security (TLS) protocol.
/// </summary>
public class SIPTLSChannel : SIPChannel
{
    private const string ACCEPT_THREAD_NAME = "siptls-";
    private const string PRUNE_THREAD_NAME = "siptlsprune-";

    // Maximum number of connections for the TLS listener.
    private const int MAX_TLS_CONNECTIONS = 1000;
    private static int MaxSIPTCPMessageSize = SIPConstants.SIP_MAXIMUM_RECEIVE_LENGTH;

    private TcpListener m_tlsServerListener;
    private Dictionary<string, SIPConnection> m_connectedSockets = new Dictionary<string, SIPConnection>();
    // List of connecting sockets to avoid SIP re-transmits initiating multiple connect attempts.
    private List<string> m_connectingSockets = new List<string>();

    private X509Certificate2 m_serverCertificate;
    private Thread m_ListenerThread = null;
    private X509CertificateCollection m_CertCollection;
    private bool m_UseMutualAuth;

    /// <summary>
    /// Fired if the TCP connection request to a remote endpoint failed.
    /// </summary>
    public event SipConnectionFailedDelegate SIPConnectionFailed = null;

    /// <summary>
    /// Fired if the TCP connection gets disconnected
    /// </summary>
    public event SipConnectionFailedDelegate SIPConnectionDisconnected = null;

    /// <summary>
    /// Constructs a new SIPTLSChannel and initializes it.
    /// </summary>
    /// <param name="serverCertificate">Server X.509 certificate to use</param>
    /// <param name="endPoint">Local IPEndPoint to listen on </param>
    /// <param name="User">Specifies the User part of the SIPURI for the local contact URI. This
    /// parameter defaults to null.</param>
    /// <param name="UseMutualAuth">If true then use mutual TLS authentication. This parameter defaults
    /// to true.</param>
    public SIPTLSChannel(X509Certificate2 serverCertificate, IPEndPoint endPoint, string User = null,
        bool UseMutualAuth = true)
    {
        if (serverCertificate == null)
        {
            throw new ArgumentNullException("serverCertificate", "An X509 " +
                "certificate must be supplied for a SIP TLS channel.");
        }

        if (endPoint == null)
        {
            throw new ArgumentNullException("endPoint", "An IP end point must " +
                "be supplied for a SIP TLS channel.");
        }

        LocalSIPEndPoint = new SIPEndPoint(SIPProtocolsEnum.tls, endPoint);
        m_isReliable = true;
        m_IsTLS = true;
        m_serverCertificate = serverCertificate;

        X509Certificate2[] CertArray = new X509Certificate2[1];
        CertArray[0] = m_serverCertificate;
        m_CertCollection = new X509CertificateCollection(CertArray);
        m_UseMutualAuth = UseMutualAuth;

        Initialise();
        SetupContactURI(User);
    }

    private object m_CollectionLock = new object();

    private void LockCollections()
    {
        Monitor.Enter(m_CollectionLock);
    }

    private void UnlockCollections()
    {
        Monitor.Exit(m_CollectionLock);
    }

    private void Initialise()
    {
        try
        {
            m_tlsServerListener = new TcpListener(LocalSIPEndPoint.GetIPEndPoint());
            m_tlsServerListener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress,
                true);
            m_tlsServerListener.Start(MAX_TLS_CONNECTIONS);

            if (LocalSIPEndPoint.Port == 0)
                LocalSIPEndPoint = new SIPEndPoint(SIPProtocolsEnum.tls, (IPEndPoint)m_tlsServerListener.
                    Server.LocalEndPoint);

            LocalTCPSockets.Add(((IPEndPoint)m_tlsServerListener.Server.LocalEndPoint).ToString());

            m_ListenerThread = new Thread(AcceptConnections);
            m_ListenerThread.IsBackground = true;
            m_ListenerThread.Priority = ThreadPriority.AboveNormal;
            m_ListenerThread.Start();

            ThreadPool.QueueUserWorkItem(delegate { PruneConnections(PRUNE_THREAD_NAME + LocalSIPEndPoint.Port); });
        }
        catch (Exception)
        {
            throw;
        }
    }

    private void AcceptConnections()
    {
        ChannelStarted = true;
        try
        {
            Thread.CurrentThread.Name = ACCEPT_THREAD_NAME + LocalSIPEndPoint.Port;

            while (!Closed)
            {
                try
                {
                    TcpClient tcpClient = m_tlsServerListener.AcceptTcpClient();
                    tcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress,
                        true);

                    IPEndPoint remoteEndPoint = (IPEndPoint)tcpClient.Client.RemoteEndPoint;

                    SslStream sslStream;
                    if (m_UseMutualAuth == false)
                        sslStream = new SslStream(tcpClient.GetStream(), false);
                    else
                        sslStream = new SslStream(tcpClient.GetStream(), false, new 
                            RemoteCertificateValidationCallback(
                            ValidateRemoteCertificate), null);

                    SIPConnection sipTLSConnection = new SIPConnection(this, tcpClient, sslStream,
                        remoteEndPoint, SIPProtocolsEnum.tls, SIPConnectionsEnum.Listener);

                    if (m_UseMutualAuth == false)
                        sslStream.BeginAuthenticateAsServer(m_serverCertificate, 
                            EndAuthenticateAsServer, sipTLSConnection);
                    else
                        sslStream.BeginAuthenticateAsServer(m_serverCertificate,
                        true, System.Security.Authentication.SslProtocols.Tls12,
                        false, EndAuthenticateAsServer, sipTLSConnection);
                }
                catch (Exception)
                {   // An exception normally occurs when shutting down this 
                    // SIPChannel object so just ignore it. The reason for the
                    // exception is that a blocking call has been canceled.
                }
            }
        }
        catch (Exception)
        {   
            //throw excp;
        }
    }

    private void EndAuthenticateAsServer(IAsyncResult ar)
    {
        try
        {
            LockCollections();
            SIPConnection sipTLSConnection = (SIPConnection)ar.AsyncState;
            SslStream sslStream = (SslStream)sipTLSConnection.SIPStream;

            sslStream.EndAuthenticateAsServer(ar);

            // Set timeouts for the read and write to 5 seconds.
            sslStream.ReadTimeout = 5000;
            sslStream.WriteTimeout = 5000;

            string strRemEp = sipTLSConnection.RemoteEndPoint.ToString();
            m_connectedSockets.Add(strRemEp, sipTLSConnection);

            sipTLSConnection.SIPSocketDisconnected += SIPTLSSocketDisconnected;
            sipTLSConnection.SIPMessageReceived += SIPTLSMessageReceived;

            sipTLSConnection.StartSynchronousRead();
        }
        catch (Exception)
        {
            // TODO: Log the failure to authenticate
        }
        finally
        {
            UnlockCollections();
        }
    }

    private void ReceiveCallback(IAsyncResult ar)
    {
        SIPConnection sipTLSConnection = (SIPConnection)ar.AsyncState;

        if (sipTLSConnection != null && sipTLSConnection.SIPStream != null && sipTLSConnection.SIPStream.
            CanRead)
        {
            try
            {
                int bytesRead = sipTLSConnection.SIPStream.EndRead(ar);
                if (sipTLSConnection.SocketReadCompleted(bytesRead))
                {
                    sipTLSConnection.SIPStream.BeginRead(sipTLSConnection.
                        SocketBuffer, sipTLSConnection.SocketBufferEndPosition, 
                        MaxSIPTCPMessageSize - sipTLSConnection.
                        SocketBufferEndPosition, new AsyncCallback(
                        ReceiveCallback), sipTLSConnection);
                }
            }
            catch (SocketException)
            {  // Occurs if the remote end gets disconnected.
            }
            catch (Exception)
            {
                // TODO: Log this exception

                SIPTLSSocketDisconnected(sipTLSConnection.RemoteEndPoint);
            }
        }
    }

    /// <summary>
    /// Sends a string
    /// </summary>
    /// <remarks>Must already be connected to the remote endpoint in order to use this method.</remarks>
    /// <param name="destinationEndPoint">IPEndPoint to send the message to.</param>
    /// <param name="message">Message to send.</param>
    public override void Send(IPEndPoint destinationEndPoint, string message)
    {
        byte[] messageBuffer = Encoding.UTF8.GetBytes(message);
        Send(destinationEndPoint, messageBuffer);
    }

    /// <summary>
    /// Sends a byte array
    /// </summary>
    /// <remarks>Must already be connected to the remote endpoint in order to use this method.</remarks>
    /// <param name="dstEndPoint">IPEndPoint to send the message to.</param>
    /// <param name="buffer">Message to send.</param>
    public override void Send(IPEndPoint dstEndPoint, byte[] buffer)
    {
        Send(dstEndPoint, buffer, null);
    }

    /// <summary>
    /// Sends a byte array
    /// </summary>
    /// <param name="dstEndPoint">IPEndPoint to send the message to.</param>
    /// <param name="buffer">Message to send.</param>
    /// <param name="serverCertificateName">Name of the remote endpoint's X.509 certificate.</param>
    public override void Send(IPEndPoint dstEndPoint, byte[] buffer, string serverCertificateName)
    {
        Exception Excpt = null;
        if (buffer == null)
            throw new ApplicationException("An empty buffer was specified" +
                "to Send in SIPTLSChannel.");
        else if (LocalTCPSockets.Contains(dstEndPoint.ToString()))
            throw new ApplicationException("A Send call was made in " +
                "SIPTLSChannel to send to another local TCP socket.");

        try
        {
            LockCollections();
            bool sent = false;
            bool existingConnection = false;

            // Lookup a client socket that is connected to the destination.
            if (m_connectedSockets.ContainsKey(dstEndPoint.ToString()))
            {
                existingConnection = true;
                SIPConnection sipTLSClient = m_connectedSockets[dstEndPoint.ToString()];

                try
                {
                    if (sipTLSClient.SIPStream != null && sipTLSClient.
                        SIPStream.CanWrite)
                    {
                        sipTLSClient.SIPStream.Write(buffer, 0, buffer.Length);
                        sipTLSClient.SIPStream.Flush();
                        sent = true;
                        sipTLSClient.LastTransmission = DateTime.Now;
                    }
                }
                catch (SocketException Se)
                {
                    Excpt = Se;
                }
                catch (IOException Ioe)
                {   // This happens if the socket was closed by the other end. 
                    Excpt = Ioe;
                }
                catch (Exception Ex)
                {
                    Excpt = Ex;
                }

                if (Excpt != null)
                {   // Remove the connected socket so that a new one will be created on a subsequent retry.
                    sipTLSClient.SIPStream.Close();
                    m_connectedSockets.Remove(dstEndPoint.ToString());
                }
            }

            if (!sent && !existingConnection)
            {
                if (string.IsNullOrEmpty(serverCertificateName) == true)
                {   // If no server certificate name is specified then default to accepting any certificate
                    // when connecting as a client.
                    serverCertificateName = "*";
                }

                if (!m_connectingSockets.Contains(dstEndPoint.ToString()))
                {
                    TcpClient tcpClient = new TcpClient();
                    tcpClient.Client.SetSocketOption(SocketOptionLevel.
                        Socket, SocketOptionName.ReuseAddress, true);

                    // Use a random local port
                    IPEndPoint LocIpe = new IPEndPoint(LocalSIPEndPoint.Address, 0); 
                    tcpClient.Client.Bind(LocIpe);

                    m_connectingSockets.Add(dstEndPoint.ToString());
                    tcpClient.BeginConnect(dstEndPoint.Address, dstEndPoint.Port, EndConnect, new object[]
                        { tcpClient, dstEndPoint, buffer, serverCertificateName });
                }
            }
        }
        catch (Exception)
        {
            // TODO: Log this exception

        }
        finally
        {
            UnlockCollections();
        }
    }

    private void EndSend(IAsyncResult ar)
    {
        try
        {
            SIPConnection sipConnection = (SIPConnection)ar.AsyncState;
            sipConnection.SIPStream.EndWrite(ar);
        }
        catch (Exception)
        {
        }
    }

    private void EndConnect(IAsyncResult ar)
    {
        object[] stateObj = (object[])ar.AsyncState;
        TcpClient tcpClient = (TcpClient)stateObj[0];
        IPEndPoint dstEndPoint = (IPEndPoint)stateObj[1];
        byte[] buffer = (byte[])stateObj[2];
        string serverCN = (string)stateObj[3];

        Exception Excpt = null;

        try
        {
            LockCollections();
            m_connectingSockets.Remove(dstEndPoint.ToString());

            if (tcpClient.Connected == false)
                SIPConnectionFailed?.Invoke(this, dstEndPoint);
            else
            {
                tcpClient.EndConnect(ar);
                SslStream sslStream = new SslStream(tcpClient.GetStream(), false, ValidateRemoteCertificate, null);
                SIPConnection callerConnection = new SIPConnection(this, tcpClient, sslStream, dstEndPoint,
                    SIPProtocolsEnum.tls, SIPConnectionsEnum.Caller);

                if (m_UseMutualAuth == false)
                    sslStream.BeginAuthenticateAsClient(serverCN, EndAuthenticateAsClient, new object[]
                       { tcpClient, dstEndPoint, buffer, callerConnection });
                else
                    sslStream.BeginAuthenticateAsClient(serverCN, m_CertCollection,
                       System.Security.Authentication.SslProtocols.Tls12, false, EndAuthenticateAsClient,
                       new object[] { tcpClient, dstEndPoint, buffer, callerConnection });
            }
        }
        catch (Exception excp)
        {
            Excpt = excp;
            if (tcpClient != null)
            {
                try
                {
                    tcpClient.Close();
                }
                catch(Exception closeExcp)
                {
                    Excpt = closeExcp;
                }
            }
        }
        finally
        {
            UnlockCollections();
        }

        if (Excpt != null)
        {
            // TODO: Log this exception
        }
    }

    private void EndAuthenticateAsClient(IAsyncResult ar)
    {
        try
        {
            LockCollections();
            object[] stateObj = (object[])ar.AsyncState;
            TcpClient tcpClient = (TcpClient)stateObj[0];
            IPEndPoint dstEndPoint = (IPEndPoint)stateObj[1];
            byte[] buffer = (byte[])stateObj[2];
            SIPConnection callerConnection = (SIPConnection)stateObj[3];

            SslStream sslStream = (SslStream)callerConnection.SIPStream;

            sslStream.EndAuthenticateAsClient(ar);
            if (tcpClient != null && tcpClient.Connected) 
            {
                string strCc = callerConnection.RemoteEndPoint.ToString();
                if (m_connectedSockets.ContainsKey(strCc) == true)
                    m_connectedSockets.Remove(strCc);

                m_connectedSockets.Add(callerConnection.RemoteEndPoint.ToString(), callerConnection);
                callerConnection.SIPSocketDisconnected += SIPTLSSocketDisconnected;
                callerConnection.SIPMessageReceived += SIPTLSMessageReceived;

                callerConnection.StartSynchronousRead();
                //callerConnection.SIPStream.BeginRead(callerConnection.
                //    SocketBuffer, 0, MaxSIPTCPMessageSize, new AsyncCallback(
                //    ReceiveCallback), callerConnection);

                callerConnection.SIPStream.BeginWrite(buffer, 0, buffer.Length, EndSend, callerConnection);
            }
        }
        catch (Exception)
        {
            // TODO: Log this exception
        }
        finally
        {
            UnlockCollections();
        }
    }

    /// <summary>
    /// Gets the current connections dictionary. The returned object must be locked by the caller.
    /// Don't use this function because its not thread safe.
    /// </summary>
    /// <returns></returns>
    protected override Dictionary<string, SIPConnection> GetConnectionsList()
    {
        return m_connectedSockets;
    }

    /// <summary>
    /// Virtual function to get the remote certificate for a connection that is being managed by this
    /// SIPChannel object.
    /// </summary>
    /// <param name="strRemoteEp">String version of the IP endpoint of the connection.</param>
    /// <returns>Returns null if there is no remote certificate available.</returns>
    public override X509Certificate2 GetRemoteCertificate2(string strRemoteEp)
    {
        X509Certificate2 RemoteCert = null;
        lock (m_connectedSockets)
        {
            if (m_connectedSockets.ContainsKey(strRemoteEp) == true)
            {
                SIPConnection Sc = m_connectedSockets[strRemoteEp];
                if (Sc.SIPStream != null && Sc.SIPStream.GetType() == typeof(SslStream))
                {
                    SslStream Sst = (SslStream)Sc.SIPStream;
                    if (Sst.RemoteCertificate != null && Sst.RemoteCertificate.GetType() == 
                        typeof(X509Certificate2))
                        RemoteCert = (X509Certificate2)Sst.RemoteCertificate;
                }
            }
        }

        return RemoteCert;
    }

    /// <summary>
    /// Virtual function to get the remote certificate for a connection that is being managed by this
    /// SIPChannel object.
    /// </summary>
    /// <param name="strRemoteEp">String version of the IP endpoint of the connection.</param>
    /// <returns>Returns null if there is no remote certificate available.</returns>
    public override X509Certificate GetRemoteCertificate(string strRemoteEp)
    {
        X509Certificate RemoteCert = null;
        lock (m_connectedSockets)
        {
            if (m_connectedSockets.ContainsKey(strRemoteEp) == true)
            {
                SIPConnection Sc = m_connectedSockets[strRemoteEp];
                if (Sc.SIPStream != null && Sc.SIPStream.GetType() == typeof(SslStream))
                {
                    SslStream Sst = (SslStream)Sc.SIPStream;
                    RemoteCert = Sst.RemoteCertificate;
                }
            }
        }

        return RemoteCert;
    }


    /// <summary>
    /// Checks to see if this object is currently connected to a remote endpoint
    /// </summary>
    /// <param name="remoteEndPoint"></param>
    /// <returns>True if currently connected or false if not connected</returns>
    public override bool IsConnectionEstablished(IPEndPoint remoteEndPoint)
    {
        bool Result;
        lock (m_connectedSockets)
        {
            Result = m_connectedSockets.ContainsKey(remoteEndPoint.ToString());
        }

        return Result;
    }

    private void SIPTLSSocketDisconnected(IPEndPoint remoteEndPoint)
    {
        try
        {
            LockCollections();
            m_connectedSockets.Remove(remoteEndPoint.ToString());
            m_connectingSockets.Remove(remoteEndPoint.ToString());
            SIPConnectionDisconnected?.Invoke(this, remoteEndPoint);
        }
        catch (Exception)
        {
        }
        finally
        {
            UnlockCollections();
        }
    }

    private void SIPTLSMessageReceived(SIPChannel channel, SIPEndPoint 
        remoteEndPoint, byte[] buffer)
    {
        SIPMessageReceived?.Invoke(channel, remoteEndPoint, buffer);
    }

    // Accepts all certificates. Always returns true.
    private bool ValidateRemoteCertificate(
        object sender,
        X509Certificate certificate,
        X509Chain chain,
        SslPolicyErrors sslPolicyErrors)
    {
        return true;
    }

    /// <summary>
    /// Closes the listener socket and closes connections to all clients.
    /// </summary>
    public override void Close()
    {
        if (!Closed == true)
        {
            Closed = true;

            try
            {
                m_tlsServerListener.Server.Shutdown(SocketShutdown.Both);
            }
            catch (Exception)
            {
            }

            try
            {
                m_tlsServerListener.Stop();
            }
            catch (SocketException) { }
            catch (Exception) { }

            lock (m_connectedSockets)
            {
                foreach (SIPConnection tcpConnection in m_connectedSockets.
                    Values)
                {
                    try
                    {
                        tcpConnection.SIPStream.Close();
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }
    }

    private void Dispose(bool disposing)
    {
        try
        {
            this.Close();
        }
        catch (Exception)
        {
        }
    }
}
