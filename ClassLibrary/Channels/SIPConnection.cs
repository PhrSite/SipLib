﻿#region License
//-----------------------------------------------------------------------------
// Filename: SIPConnection.cs
//
// Description: Represents an established socket connection on a connection oriented SIP 
// TCL or TLS.
//
// History:
// 31 Mar 2009	Aaron Clauson	Created.
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
//	Revised:	7 Nov 22 PHR -- Initial version.
//              -- Add support for synchronous reading in a separate thread in order
//                 to reduce latency
//              -- Added support for QOS packet marking
//	            -- Added documentation comments
//              20 Jul 23 PHR
//              -- Changed the call from Qos.AddQos() to Qos.SetTcpDscp()
/////////////////////////////////////////////////////////////////////////////////////

using System.Net;
using System.Net.Sockets;
using System.Text;

using SipLib.Core;
namespace SipLib.Channels;

/// <summary>
/// Enumeration for the type of the SIP connection type.
/// </summary>
public enum SIPConnectionsEnum
{
    /// <summary>
    /// Indicates the connection was initiated by the remote client to a local server socket.
    /// </summary>
    Listener = 1,
    /// <summary>
    /// Indicates the connection was initiated locally to a remote server socket.
    /// </summary>
    Caller = 2,     
}

/// <summary>
/// Class for managing a SIP connection.
/// </summary>
public class SIPConnection
{
    /// <summary>
    /// Maximum allowed SIP message size
    /// </summary>
    private static int MaxSIPTCPMessageSize = SIPConstants.SIP_MAXIMUM_RECEIVE_LENGTH;
    private static string m_sipEOL = SIPConstants.CRLF;
    private static string m_sipMessageDelimiter = SIPConstants.CRLF + SIPConstants.CRLF;

    /// <summary>
    /// Stream for the transport
    /// </summary>
    /// <value></value>
    internal Stream SIPStream;

    /// <summary>
    /// Remote endpoint of the connection
    /// </summary>
    /// <value></value>
    public IPEndPoint RemoteEndPoint;

    /// <summary>
    /// Connection/transport protocol
    /// </summary>
    /// <value>For example: udp, tcp, tls, ws, wss.</value>
    public SIPProtocolsEnum ConnectionProtocol;

    /// <summary>
    /// Connection type
    /// </summary>
    /// <value>The available connection types are Listener or Caller</value>
    public SIPConnectionsEnum ConnectionType;

    /// <summary>
    /// Contains the time when a SIP packet was last sent or received.
    /// </summary>
    /// <value></value>
    public DateTime LastTransmission;

    /// <summary>
    /// Buffer for receiving a SIP message
    /// </summary>
    /// <value></value>
    internal byte[] SocketBuffer = new byte[2 * MaxSIPTCPMessageSize];

    /// <summary>
    /// Index of the end of the SocketBuffer
    /// </summary>
    /// <value></value>
    internal int SocketBufferEndPosition = 0;

    private SIPChannel m_owningChannel;
    private TcpClient _tcpClient;

    /// <summary>
    /// Fired when a complete SIP message is received
    /// </summary>
    public event SIPMessageReceivedDelegate? SIPMessageReceived;
    /// <summary>
    /// Fired when the SIP socket gets disconnected
    /// </summary>
    public event SIPConnectionDisconnectedDelegate? SIPSocketDisconnected;

    private Qos? m_Qos;

    /// <summary>
    /// Class for managing a bi-directional SIP connection. This class is used by the steam based
    /// channel classes (SIPTCPChannel and SIPTLSChannel).
    /// </summary>
    /// <param name="channel">Channel that owns this connection</param>
    /// <param name="tcpClient">TcpClient of the channel</param>
    /// <param name="sipStream">Underlying stream for the connection</param>
    /// <param name="remoteEndPoint">Remote IPEndPoint of the connection</param>
    /// <param name="connectionProtocol">Type of transport protocol used by the connection</param>
    /// <param name="connectionType">Either a Listener (server) or a Caller (client)</param>
    public SIPConnection(SIPChannel channel, TcpClient tcpClient, Stream sipStream, IPEndPoint
        remoteEndPoint, SIPProtocolsEnum connectionProtocol, SIPConnectionsEnum connectionType)
    {
        LastTransmission = DateTime.Now;
        m_owningChannel = channel;
        _tcpClient = tcpClient;
        SIPStream = sipStream;
        RemoteEndPoint = remoteEndPoint;
        ConnectionProtocol = connectionProtocol;
        ConnectionType = connectionType;

        m_Qos = new Qos();
        // Setup QOS DSCP packet marking
        m_Qos.SetTcpDscp(_tcpClient, DscpSettings.SipSignalingDscp, RemoteEndPoint);
    }

    /// <summary>
    /// Processes the receive buffer after a read from the connected socket.
    /// </summary>
    /// <param name="bytesRead">The number of bytes that were read into the receive buffer.</param>
    /// <returns>True if the receive was processed correctly, false if the socket returned 0 bytes or was
    /// disconnected.</returns>
    internal bool SocketReadCompleted(int bytesRead)
    {
        try
        {
            if (bytesRead > 0)
            {
                SocketBufferEndPosition += bytesRead;
                int bytesSkipped = 0;

                // Attempt to extract a SIP message from the receive buffer.
                byte[] sipMsgBuffer = ProcessReceive(SocketBuffer, 0, SocketBufferEndPosition, 
                    out bytesSkipped);

                while (sipMsgBuffer != null)
                {   // A SIP message is available.
                    SIPMessageReceived?.Invoke(m_owningChannel, new SIPEndPoint(SIPProtocolsEnum.tcp,
                        RemoteEndPoint), sipMsgBuffer);
                    LastTransmission = DateTime.Now;
                    SocketBufferEndPosition -= (sipMsgBuffer.Length + bytesSkipped);

                    if (SocketBufferEndPosition == 0)
                        break;
                    else
                    {
                        // Do a left shift on the receive array.
                        Array.Copy(SocketBuffer, sipMsgBuffer.Length + bytesSkipped, SocketBuffer, 0, 
                            SocketBufferEndPosition);

                        // Try and extract another SIP message from the receive buffer.
                        sipMsgBuffer = ProcessReceive(SocketBuffer, 0, SocketBufferEndPosition, 
                            out bytesSkipped);
                    }
                }

                return true;
            }
            else
            {
                Close();
                SIPSocketDisconnected?.Invoke(RemoteEndPoint);
                return false;
            }
        }
        catch (ObjectDisposedException)
        {
            // Will occur if the owning channel closed the connection.
            SIPSocketDisconnected?.Invoke(RemoteEndPoint);
            return false;
        }
        catch (SocketException)
        {
            // Will occur if the owning channel closed the connection.
            SIPSocketDisconnected?.Invoke(RemoteEndPoint);
            return false;
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// Processes a buffer from a TCP read operation to extract the first full SIP message. If no full
    /// SIP messages are available it returns null which indicates the next read should be appended to
    /// the current buffer and the process re-attempted.
    /// </summary>
    /// <param name="receiveBuffer">The buffer to check for the SIP message in.
    /// </param>
    /// <param name="start">The position in the buffer to start parsing for a SIP message.</param>
    /// <param name="length">The position in the buffer that indicates the end of the received bytes.
    /// </param>
    /// <param name="bytesSkipped">Number of bytes skipped.</param>
    /// <returns>A byte array holding a full SIP message or if no full SIP messages are avialble null.
    /// </returns>
    private static byte[]? ProcessReceive(byte[] receiveBuffer, int start, int length, out int bytesSkipped)
    {
        // NAT keep-alives can be interspersed between SIP messages. Treat any non-letter character at
        // the start of a receive as a non SIP transmission and skip over it.
        bytesSkipped = 0;
        bool letterCharFound = false;
        while (!letterCharFound && start < length)
        {
            if ((int)receiveBuffer[start] >= 65)
            {
                break;
            }
            else
            {
                start++;
                bytesSkipped++;
            }
        }

        if (start < length)
        {
            int endMessageIndex = ByteBufferInfo.GetStringPosition(receiveBuffer, start, length,
                m_sipMessageDelimiter, null);
            if (endMessageIndex != -1)
            {
                int contentLength = GetContentLength(receiveBuffer, start, endMessageIndex);
                int messageLength = endMessageIndex - start + m_sipMessageDelimiter.Length + contentLength;

                if (length - start >= messageLength)
                {
                    byte[] sipMsgBuffer = new byte[messageLength];
                    Buffer.BlockCopy(receiveBuffer, start, sipMsgBuffer, 0, messageLength);
                    return sipMsgBuffer;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Attempts to find the Content-Length header is a SIP header and extract it.
    /// </summary>
    /// <param name="buffer">The buffer to search in.</param>
    /// <param name="start">The position in the buffer to start the search from.</param>
    /// <param name="end">The position in the buffer to stop the search at.
    /// </param>
    /// <returns>Returns the content length. May be 0 or greater than 0.</returns>
    internal static int GetContentLength(byte[] buffer, int start, int end)
    {
        if (buffer == null || start > end || buffer.Length < end)
            return 0;
        else
        {
            byte[] contentHeaderBytes = Encoding.UTF8.GetBytes(m_sipEOL + 
                SIPHeaders.SIP_HEADER_CONTENTLENGTH.ToUpper());
            byte[] compactContentHeaderBytes = Encoding.UTF8.GetBytes(m_sipEOL + 
                SIPHeaders.SIP_COMPACTHEADER_CONTENTLENGTH.ToUpper());

            int inContentHeaderPosn = 0;
            int inCompactContentHeaderPosn = 0;
            bool possibleHeaderFound = false;
            int contentLengthValueStartPosn = 0;

            for (int index = start; index < end; index++)
            {
                if (possibleHeaderFound)
                {
                    // A possilbe match has been found for the Content-Length header. The next characters
                    // can only be whitespace or colon.
                    if (buffer[index] == ':')
                    {   // The Content-Length header has been found.
                        contentLengthValueStartPosn = index + 1;
                        break;
                    }
                    else if (buffer[index] == ' ' || buffer[index] == '\t')
                    {   // Skip any whitespace between the header and the colon.
                        continue;
                    }
                    else
                    {   // Additional characters indicate this is not the Content-Length header.
                        possibleHeaderFound = false;
                        inContentHeaderPosn = 0;
                        inCompactContentHeaderPosn = 0;
                    }
                }

                if (buffer[index] == contentHeaderBytes[inContentHeaderPosn] || 
                    buffer[index] == contentHeaderBytes[inContentHeaderPosn] + 32)
                {
                    inContentHeaderPosn++;

                    if (inContentHeaderPosn == contentHeaderBytes.Length)
                        possibleHeaderFound = true;
                }
                else
                    inContentHeaderPosn = 0;

                if (buffer[index] == compactContentHeaderBytes[inCompactContentHeaderPosn] || 
                    buffer[index] == compactContentHeaderBytes[inCompactContentHeaderPosn] + 32)
                {
                    inCompactContentHeaderPosn++;
                    if (inCompactContentHeaderPosn == compactContentHeaderBytes.Length)
                        possibleHeaderFound = true;
                }
                else
                    inCompactContentHeaderPosn = 0;
            }

            if (contentLengthValueStartPosn != 0)
            {   // The Content-Length header has been found, this block extracts the value of the header.
                string contentLengthValue = null;

                for (int index = contentLengthValueStartPosn; index < end; index++)
                {
                    if (contentLengthValue == null && (buffer[index] == ' ' || buffer[index] == '\t'))
                        // Skip any whitespace at the start of the header value.
                        continue;
                    else if (buffer[index] >= '0' && buffer[index] <= '9')
                        contentLengthValue += ((char)buffer[index]).ToString();
                    else
                        break;
                }

                if (string.IsNullOrEmpty(contentLengthValue) == false)
                    return Convert.ToInt32(contentLengthValue);
            }

            return 0;
        }
    }

    private bool m_Closed = false;
    private object m_CloseLock = new object();

    /// <summary>
    /// Closes the connection
    /// </summary>
    internal void Close()
    {
        Monitor.Enter(m_CloseLock);

        if (m_Closed == true)
        {  
            Monitor.Exit(m_CloseLock);
            return;
        }

        try
        {
            m_Qos.Shutdown();
            m_Qos = null;
            m_IsEnding = true;

            if (_tcpClient.GetStream() != null)
                _tcpClient.GetStream().Close(0);

            if (_tcpClient.Client != null && _tcpClient.Client.Connected == true)
            {
                _tcpClient.Client.Shutdown(SocketShutdown.Both);
                _tcpClient.Client.Close(0);
            }

            _tcpClient.Close();
        }
        catch (Exception)
        {
        }
        finally
        { 
            m_Closed = true;
            Monitor.Exit(m_CloseLock);
        }
    }

    /// <summary>
    /// Gets the IPEndpoint that this connection object is listening on.
    /// </summary>
    /// <value></value>
    public IPEndPoint? LocalEndpoint
    {
        get
        {
            if (_tcpClient == null)
                return null;
            else
                return (IPEndPoint) _tcpClient.Client?.LocalEndPoint;
        }
    }

    private bool m_IsEnding = false;
    private Thread? m_ReadThread = null;

    /// <summary>
    /// Sets up a dedicated thread for doing synchronous reads.
    /// </summary>
    internal void StartSynchronousRead()
    {
        if (m_ReadThread != null)
            return;     // Already reading

        m_ReadThread = new Thread(SyncReadLoop);
        m_ReadThread.IsBackground = true;
        m_ReadThread.Start();
    }

    /// <summary>
    /// Thread for doing synchronous reads. The thread loop will exit if there is an error on the socket.
    /// Else it operates until the connection is closed.
    /// </summary>
    private void SyncReadLoop()
    {
        int bytesRead;
        bool Success = true;
        SIPStream.ReadTimeout = System.Threading.Timeout.Infinite;

        while (m_IsEnding == false && Success == true)
        {
            try
            {
                bytesRead = SIPStream.Read(SocketBuffer, SocketBufferEndPosition, MaxSIPTCPMessageSize - 
                    SocketBufferEndPosition);
                Success = SocketReadCompleted(bytesRead);
            }
            catch (SocketException)
            {   // Occurs if the remote end gets disconnected
                Success = false;
            }
            catch (ObjectDisposedException)
            {   // Occurs if the stream is closed on this end
                Success = false;
            }
            catch (IOException)
            {   // This exception occurs if the remote endpoint resets (aborts) the connection by sending
                // a RST TCP packet.
                if (m_Closed == false)
                    SIPSocketDisconnected?.Invoke(RemoteEndPoint);
                Success = false;
            }
            catch (Exception)
            {
                Success = false;
            }
        } // end while m_IsEnding == false && ReadError = false
    }
}
