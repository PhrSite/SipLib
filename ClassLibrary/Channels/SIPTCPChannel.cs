#region License
//-----------------------------------------------------------------------------
// Filename: SIPTCPChannel.cs
//
// Description: SIP transport for TCP.
// 
// History:
// 19 Apr 2008	Aaron Clauson	Created.
//
// License: 
// This software is licensed under the BSD License http://www.opensource.org/licenses/bsd-license.php
//
// Copyright (c) 2006-2009 Aaron Clauson (aaron@sipsorcery.com), SIP Sorcery PTY LTD, Hobart, Australia (www.sipsorcery.com)
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
//          -- Changed m_connectedSockets from a Dictionary<IPEndpoint,
//             SIPConnection> to a Dictionary<string, SIPConnection>
//          -- Changed FAILED_CONNECTION_DONTUSE_INTERVAL from 300 seconds to 5 seconds.
//          -- Removed throw ApplicationException() in Send().
//          -- Modified Close() to work for both Linux and Windows.
//          -- Changed the void Send(IPEndPoint dstEndPoint, byte[] buffer, string
//             serverCertificateName) method so that it calls the Send(IPEndPoint
//             dstEndPoint, byte[] buffer) method instead of throwing an
//             ApplicationException.
//          -- Changed ReceiveCallback() from public to private
//          -- Added the User parameter to the constructor.
//          -- Modified Send() to not bind to the local endpoint of this SIPChannel.
//             Bind to the local address with a random port that is picked by the
//             operating system.
//          -- Changed ReceiveCallback() to test for SIPStream.CanRead before calling
//             before calling SIPStream.EndRead() to avoid an ObjectDisposedException
//             (which was being caught OK) when closing or if a disconnect occurred.
//          -- Added handling of an IOExeption in the Send() function.
//          -- Added LockCollections() and UnlockCollections() to lock and unlock the
//             dictionary collections in try/finally blocks because they were being
//             accessed from different threads.
//          -- Switch to using synchronous reads because there are significant
//             performance issues with asynchronous reads when running under Linux.
//          -- Added ChannelStarted
//          -- Changed AcceptConnections to a permanent thread because of occasionally
//             slow startup times due to the asynchronous nature of ThreadPool.
//             QueueUserWorkItem().
//          -- Added the ForceCloseConnection() virtual function.
//          -- Modified AcceptConnections() to force close the existing connection if
//             the client is connecting using the same remote endpoint.
//          -- Added documentation comments
/////////////////////////////////////////////////////////////////////////////////////

using System.Net;
using System.Net.Sockets;
using System.Text;
using SipLib.Core;

namespace SipLib.Channels;

/// <summary>
/// Class for managing a SIP connection using the TCP protocol.
/// </summary>
public class SIPTCPChannel : SIPChannel
{
    private const string ACCEPT_THREAD_NAME = "siptcp-";
    private const string PRUNE_THREAD_NAME = "siptcpprune-";

    // Maximum number of connections for the TCP listener.
    private const int MAX_TCP_CONNECTIONS = 1000;

    // The number of failed connection attempts permitted before classifying a remote socket as failed.
    private const int CONNECTION_ATTEMPTS_ALLOWED = 3;

    // If a socket cannot be connected to don't try and reconnect to it for this interval. Units = seconds.
    // Was 300.
    private const int FAILED_CONNECTION_DONTUSE_INTERVAL = 5;

    private static int MaxSIPTCPMessageSize = SIPConstants.SIP_MAXIMUM_RECEIVE_LENGTH;
    
    private TcpListener m_tcpServerListener;
    private Dictionary<string, SIPConnection> m_connectedSockets = new Dictionary<string, SIPConnection>();

    // List of sockets that are in the process of being connected to. Need to avoid SIP re-transmits
    // initiating multiple connect attempts.
    private List<string> m_connectingSockets = new List<string>();
    
    // Tracks the number of connection attempts made to a remote socket, three strikes and it's out.
    private Dictionary<string, int> m_connectionFailureStrikes = new Dictionary<string, int>();

    // Tracks sockets that have had a connection failure on them to avoid endless re-connect attmepts.
    private Dictionary<string, DateTime> m_connectionFailures = new Dictionary<string, DateTime>();

    private Thread m_ListenerThread = null;

    /// <summary>
    /// Constructs a new SIPTCPChannel and initializes the connection.
    /// </summary>
    /// <param name="endPoint">Local IPEndPoint to listen on.</param>
    /// <param name="User">Specifies the User part of the SIPURI for the local contact URI. This parameter
    /// defaults to null.</param>
    public SIPTCPChannel(IPEndPoint endPoint, string User = null)
    {
        LocalSIPEndPoint = new SIPEndPoint(SIPProtocolsEnum.tcp, endPoint);
        m_isReliable = true;
        Initialise();
        SetupContactURI(User);
    }

    private void LockCollections()
    {
        Monitor.Enter(m_connectedSockets);
        Monitor.Enter(m_connectingSockets);
        Monitor.Enter(m_connectionFailureStrikes);
        Monitor.Enter(m_connectionFailures);
    }

    private void UnlockCollections()
    {
        Monitor.Exit(m_connectedSockets);
        Monitor.Exit(m_connectingSockets);
        Monitor.Exit(m_connectionFailureStrikes);
        Monitor.Exit(m_connectionFailures);
    }

    private void Initialise()
    {
        try
        {
            m_tcpServerListener = new TcpListener(LocalSIPEndPoint.GetIPEndPoint());
            m_tcpServerListener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.
                ReuseAddress, true);

            m_tcpServerListener.Start(MAX_TCP_CONNECTIONS);

            if (LocalSIPEndPoint.Port == 0)
                LocalSIPEndPoint = new SIPEndPoint(SIPProtocolsEnum.tcp, (IPEndPoint)m_tcpServerListener.
                    Server.LocalEndPoint);

            LocalTCPSockets.Add(((IPEndPoint)m_tcpServerListener.Server.LocalEndPoint).ToString());

            //ThreadPool.QueueUserWorkItem(delegate { AcceptConnections(
            //    ACCEPT_THREAD_NAME + LocalSIPEndPoint.Port); });
            m_ListenerThread = new Thread(AcceptConnections);
            m_ListenerThread.IsBackground = true;
            m_ListenerThread.Priority = ThreadPriority.AboveNormal;
            m_ListenerThread.Start();

            ThreadPool.QueueUserWorkItem(delegate { PruneConnections(
                PRUNE_THREAD_NAME + LocalSIPEndPoint.Port); });
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
                    TcpClient tcpClient = m_tcpServerListener.AcceptTcpClient();
                    LockCollections();
                    if (!Closed)
                    {
                        tcpClient.Client.SetSocketOption(SocketOptionLevel.
                            Socket, SocketOptionName.ReuseAddress, true);
                        tcpClient.LingerState = new LingerOption(false, 0);
                        IPEndPoint remoteEndPoint = (IPEndPoint)tcpClient.Client.RemoteEndPoint;

                        SIPConnection sipTCPConnection = new SIPConnection(this, tcpClient, tcpClient.
                            GetStream(), remoteEndPoint, SIPProtocolsEnum.tcp, SIPConnectionsEnum.Listener);

                        string strRemEp = remoteEndPoint.ToString();
                        m_connectedSockets.Add(strRemEp, sipTCPConnection);

                        sipTCPConnection.SIPSocketDisconnected += SIPTCPSocketDisconnected;
                        sipTCPConnection.SIPMessageReceived += SIPTCPMessageReceived;

                        sipTCPConnection.StartSynchronousRead();
                        //sipTCPConnection.SIPStream.BeginRead(sipTCPConnection.
                        //    SocketBuffer, 0, MaxSIPTCPMessageSize, new 
                        //    AsyncCallback(ReceiveCallback), sipTCPConnection);
                    }
                }
                catch (Exception)
                {   // This exception gets thrown if the remote end disconnects
                    // during the socket accept.
                }
                finally
                {
                    UnlockCollections();
                }
            }

        }
        catch (Exception)
        {
        }
    }

    private void ReceiveCallback(IAsyncResult ar)
    {
        SIPConnection sipTCPConnection = (SIPConnection)ar.AsyncState;

        int bytesRead = 0;
        try
        {
            if (sipTCPConnection.SIPStream.CanRead == true)
            {
                bytesRead = sipTCPConnection.SIPStream.EndRead(ar);

                if (sipTCPConnection.SocketReadCompleted(bytesRead))
                {
                    sipTCPConnection.SIPStream.BeginRead(sipTCPConnection.SocketBuffer, sipTCPConnection.
                        SocketBufferEndPosition, MaxSIPTCPMessageSize - sipTCPConnection.
                        SocketBufferEndPosition, new AsyncCallback(ReceiveCallback), sipTCPConnection);
                }
            }
        }
        catch (SocketException)  // Occurs if the remote end gets disconnected.
        {
        }
        catch (ObjectDisposedException)
        {
        }
        catch (Exception)
        {
        }
    }

    /// <summary>
    /// Checks to see if this object is currently connected to a remote endpoint
    /// </summary>
    /// <param name="remoteEndPoint"></param>
    /// <returns>True if currently connected or false if not connected</returns>
    public override bool IsConnectionEstablished(IPEndPoint remoteEndPoint)
    {
        lock (m_connectedSockets)
        {
            return m_connectedSockets.ContainsKey(remoteEndPoint.ToString());
        }
    }

    /// <summary>
    /// Gets a dictionary containing the current connections. The returned
    /// object must be locked by the caller.
    /// </summary>
    /// <returns></returns>
    protected override Dictionary<string, SIPConnection> GetConnectionsList()
    {
        return m_connectedSockets;
    }

    private void SIPTCPSocketDisconnected(IPEndPoint remoteEndPoint)
    {
        try
        {
            lock (m_connectedSockets)
            {
                if(m_connectedSockets.ContainsKey(remoteEndPoint.ToString()))
                {
                    m_connectedSockets.Remove(remoteEndPoint.ToString());
                }
            }
        }
        catch (Exception)
        {
        }
    }

    private void SIPTCPMessageReceived(SIPChannel channel, SIPEndPoint remoteEndPoint, byte[] buffer)
    {
        try
        {
            LockCollections();
            if (m_connectionFailures.ContainsKey(remoteEndPoint.GetIPEndPoint().ToString()))
                m_connectionFailures.Remove(remoteEndPoint.GetIPEndPoint().ToString());

            if (m_connectionFailureStrikes.ContainsKey(remoteEndPoint.GetIPEndPoint().ToString()))
                m_connectionFailureStrikes.Remove(remoteEndPoint.GetIPEndPoint().ToString());
        }
        finally
        {
            UnlockCollections();
        }

        SIPMessageReceived?.Invoke(channel, remoteEndPoint, buffer);
    }

    /// <summary>
    /// Sends a string
    /// </summary>
    /// <param name="destinationEndPoint">IPEndPoint to send the message to.
    /// </param>
    /// <param name="message">Message to send.</param>
    public override void Send(IPEndPoint destinationEndPoint, string message)
    {
        byte[] messageBuffer = Encoding.UTF8.GetBytes(message);
        Send(destinationEndPoint, messageBuffer);
    }

    /// <summary>
    /// Sends a byte array
    /// </summary>
    /// <param name="dstEndPoint">IPEndPoint to send the message to.
    /// </param>
    /// <param name="buffer">Message to send.</param>
    public override void Send(IPEndPoint dstEndPoint, byte[] buffer)
    {
        string strDestEndPoint = dstEndPoint.ToString();
        Exception Excpt = null;
        if (buffer == null)
            throw new ApplicationException("An empty buffer was specified to Send in SIPTCPChannel.");
        
        if (LocalTCPSockets.Contains(strDestEndPoint) == true)
            throw new ApplicationException("A Send call was made in " +
                "SIPTCPChannel to send to another local TCP socket.");

        try
        {
            LockCollections();
            bool sent = false;

            // Lookup a client socket that is connected to the destination.
            if (m_connectedSockets.ContainsKey(strDestEndPoint) == true)
            {
                SIPConnection sipTCPClient = m_connectedSockets[strDestEndPoint];

                try
                {
                    sipTCPClient.SIPStream.BeginWrite(buffer, 0, buffer.Length, new AsyncCallback(EndSend),
                        sipTCPClient);
                    sipTCPClient.SIPStream.Flush();
                    sent = true;
                    sipTCPClient.LastTransmission = DateTime.Now;
                }
                catch (SocketException Se)
                {
                    Excpt = Se;
                }
                catch (IOException Ioe)
                {   // This happens if the socket was closed by the other
                    // end.
                    Excpt = Ioe;
                }
                catch (Exception Ex)
                {
                    Excpt = Ex;
                }

                if (Excpt != null)
                {   // Remove the connected socket so that a new one will be created on a subsequent
                    // retry.
                    sipTCPClient.SIPStream.Close();
                    m_connectedSockets.Remove(strDestEndPoint);
                }
            }

            if (!sent)
            {
                if (m_connectionFailures.ContainsKey(strDestEndPoint) && m_connectionFailures[
                    strDestEndPoint] < DateTime.Now.AddSeconds(FAILED_CONNECTION_DONTUSE_INTERVAL * -1))
                    m_connectionFailures.Remove(strDestEndPoint);
    
                if (m_connectionFailures.ContainsKey(strDestEndPoint))
                {
                }
                else if (!m_connectingSockets.Contains(strDestEndPoint))
                {
                    TcpClient tcpClient = new TcpClient();
                    tcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, 
                        SocketOptionName.ReuseAddress, true);

                    // If Bind() is called with the local end point, it will not be possible to
                    // communicate with the remote server using the local endpoint for a period equal
                    // to the TIME_WAIT interval (about 4 minutes) if the application or server is
                    // stopped and restarted again before the TIME_WAIT interval expires.
                    //tcpClient.Client.Bind(LocalSIPEndPoint.GetIPEndPoint());

                    // Use a random local port
                    IPEndPoint LocIpe = new IPEndPoint(LocalSIPEndPoint.Address, 0); 
                    tcpClient.Client.Bind(LocIpe);

                    m_connectingSockets.Add(strDestEndPoint);

                    // Applies to send operations only
                    tcpClient.NoDelay = true;
                    tcpClient.Client.NoDelay = true;

                    tcpClient.BeginConnect(dstEndPoint.Address, dstEndPoint.Port, EndConnect, 
                        new object[] { tcpClient, dstEndPoint, buffer });
                }
            }
        }
        catch (ApplicationException)
        {
            throw;
        }
        catch (Exception)
        {
            throw;
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
            SIPConnection sipTCPConnection = (SIPConnection)ar.AsyncState;
            sipTCPConnection.SIPStream.EndWrite(ar);
        }
        catch (Exception) { }
    }

    /// <summary>
    /// Sends a byte array.
    /// </summary>
    /// <param name="dstEndPoint">IPEndPoint to send the message to.</param>
    /// <param name="buffer">Message to send.</param>
    /// <param name="serverCertificateName">Not used. May be null.</param>
    public override void Send(IPEndPoint dstEndPoint, byte[] buffer, string serverCertificateName)
    {
        // Just ignore the cert. name for TCP.
        Send(dstEndPoint, buffer);
    }

    private void EndConnect(IAsyncResult ar)
    {
        bool connected = false;
        IPEndPoint dstEndPoint = null;

        try
        {
            LockCollections();
            object[] stateObj = (object[])ar.AsyncState;
            TcpClient tcpClient = (TcpClient)stateObj[0];
            dstEndPoint = (IPEndPoint)stateObj[1];
            byte[] buffer = (byte[])stateObj[2];

            m_connectingSockets.Remove(dstEndPoint.ToString());

            tcpClient.EndConnect(ar);

            if (tcpClient != null && tcpClient.Connected)
            {
                connected = true;

                m_connectionFailureStrikes.Remove(dstEndPoint.ToString());
                m_connectionFailures.Remove(dstEndPoint.ToString());

                SIPConnection callerConnection = new SIPConnection(this, tcpClient, tcpClient.GetStream(),
                    dstEndPoint, SIPProtocolsEnum.tcp, SIPConnectionsEnum.Caller);
                m_connectedSockets.Add(dstEndPoint.ToString(), callerConnection);

                callerConnection.SIPSocketDisconnected += SIPTCPSocketDisconnected;
                callerConnection.SIPMessageReceived += SIPTCPMessageReceived;

                callerConnection.StartSynchronousRead();
                //callerConnection.SIPStream.BeginRead(callerConnection.
                //    SocketBuffer, 0, MaxSIPTCPMessageSize, new 
                //    AsyncCallback(ReceiveCallback), callerConnection);

                callerConnection.SIPStream.BeginWrite(buffer, 0, buffer.Length, EndSend, callerConnection);
            }
        }
        catch (SocketException)
        {
        }
        catch (Exception)
        {
        }
        finally
        {
            if (!connected && dstEndPoint != null)
            {
                if (m_connectionFailureStrikes.ContainsKey(dstEndPoint.ToString()))
                    m_connectionFailureStrikes[dstEndPoint.ToString()] = m_connectionFailureStrikes[
                        dstEndPoint.ToString()] + 1;
                else
                    m_connectionFailureStrikes.Add(dstEndPoint.ToString(), 1);

                if (m_connectionFailureStrikes[dstEndPoint.ToString()] >= CONNECTION_ATTEMPTS_ALLOWED)
                {
                    if (!m_connectionFailures.ContainsKey(dstEndPoint.ToString()))
                    {
                        m_connectionFailures.Add(dstEndPoint.ToString(), DateTime.Now);
                    }

                    m_connectionFailureStrikes.Remove(dstEndPoint.ToString());
                }
            }

            UnlockCollections();
        }
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
                m_tcpServerListener.Server.Shutdown(SocketShutdown.Both);
            }
            catch (Exception) { }

            try 
            {
                m_tcpServerListener.Stop();
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
                        tcpConnection.Close();
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
