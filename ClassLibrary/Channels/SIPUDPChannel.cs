#region License
//-----------------------------------------------------------------------------
// Filename: SIPUDPChannel.cs
//
// Description: SIP transport for UDP.
// 
// History:
// 17 Oct 2005	Aaron Clauson	Created.
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
// Revised: 8 Nov PHR Initial version
//          -- Increased the RX buffer size to 2 MB for better performance.
//          -- Changed the void Send(IPEndPoint dstEndPoint, byte[] buffer, string
//             serverCertificateName) method so that it calls the Send(IPEndPoint
//             dstEndPoint, byte[] buffer) method instead of throwing an
//             ApplicationException.
//          -- Added the User parameter to the constructor.
//          -- Added the DisableConnectionReset() static method.
//          -- Added support for QOS packet marking.
//          -- Added ChannelStarted
//          -- Added documentation comments
//          20 Jul 23 PHR
//          -- Modified to use m_Qos.SetUdpDscp()
//          31 Aug 23 PHR
//          -- Made the Send() method thread-safe.
/////////////////////////////////////////////////////////////////////////////////////

using System.Net;
using System.Net.Sockets;
using System.Text;

using SipLib.Core;

namespace SipLib.Channels;

/// <summary>
/// Class for managing a SIP connection using the UDP transport protocol.
/// </summary>
public class SIPUDPChannel : SIPChannel
{
    private const string THREAD_NAME = "sipchanneludp-";
    private const int SIO_UDP_CONNRESET = -1744830452;

    private UdpClient m_sipConn = null;
    private Qos m_Qos;

    /// <summary>
    /// Constructs a new SIPUDPChannel.
    /// </summary>
    /// <param name="endPoint">Local IPEndpoint to listen on.</param>
    /// <param name="User">Specifies the User part of the SIPURI for the local contact URI. This
    /// parameter is optional and defaults to null.</param>
    public SIPUDPChannel(IPEndPoint endPoint, string User = null)
    {
        LocalSIPEndPoint = new SIPEndPoint(SIPProtocolsEnum.udp, endPoint);
        Initialise();
        SetupContactURI(User);
    }

    /// <summary>
    /// Performs a IOControl call to disable SocketExceptions that occur when ICMP port unreachable
    /// messages occur if the remote client is not listening on its port. This method can be called
    /// if the platform is Windows.
    /// </summary>
    /// <param name="Client">UdpClient to disable SocketExceptions on.</param>
    public static void DisableConnectionReset(UdpClient Client)
    {
        Client.Client.IOControl((IOControlCode)SIO_UDP_CONNRESET, new byte[] { 0, 0, 0, 0 }, null);
    }

    private void Initialise()
    {
        try
        {
            m_sipConn = new UdpClient(LocalSIPEndPoint.GetIPEndPoint());
            m_Qos = new Qos();
            m_Qos.SetUdpDscp(m_sipConn, DscpSettings.SipSignalingDscp);

            if (LocalSIPEndPoint.Port == 0)
                LocalSIPEndPoint = new SIPEndPoint(SIPProtocolsEnum.udp, (IPEndPoint)m_sipConn.
                    Client.LocalEndPoint);

            m_sipConn.Client.ReceiveBufferSize = 2000000;
            Thread listenThread = new Thread(new ThreadStart(Listen));
            listenThread.Name = THREAD_NAME + Crypto.GetRandomString(4);
            listenThread.Start();
        }
        catch (Exception)
        {
            throw;
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

    /// <summary>
    /// This is the thread loop that listens on the UDP socket for datagram packets. This thread
    /// loop does not exit until the UDP socket is forcibly closed on this end.
    /// </summary>
    private void Listen()
    {
        ChannelStarted = true;
        try
        {
            byte[] buffer = null;

            while (Closed == false)
            {
                IPEndPoint inEndPoint = new IPEndPoint(IPAddress.Any, 0);

                try
                {
                    buffer = m_sipConn.Receive(ref inEndPoint);
                }
                catch (SocketException)
                {
                    // Pretty sure these exceptions get thrown when an ICMP 
                    // message comes back indicating there is no listening
                    // socket on the other end. It would be nice to be able to 
                    // relate that back to the socket that the data was sent to
                    // so that we know to stop sending.

                    // SocketExceptions also occur the UdpClient is forcibly 
                    // closed while blocking in Receive()
                    continue;
                }
                catch (Exception)
                {
                    // There is no point logging this as without processing the
                    // ICMP message it's not possible to know which socket the 
                    // rejection came from.

                    inEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    continue;
                }

                if (buffer == null || buffer.Length == 0)
                {   // No need to care about zero byte packets.
                }
                else
                {
                    SIPMessageReceived?.Invoke(this, new SIPEndPoint(SIPProtocolsEnum.udp, inEndPoint),
                        buffer);
                }
            }

        }
        catch (Exception)
        {
        }
    }

    /// <summary>
    /// Sends a string message.
    /// </summary>
    /// <param name="destinationEndPoint">IPEndPoint to send the message to.
    /// </param>
    /// <param name="message">Input message to send.</param>
    public override void Send(IPEndPoint destinationEndPoint, string message)
    {
        byte[] messageBuffer = Encoding.UTF8.GetBytes(message);
        Send(destinationEndPoint, messageBuffer);
    }

    // 31 Aug 23 PHR
    object m_SendLockObj = new object();

    /// <summary>
    /// Sends a byte array
    /// </summary>
    /// <param name="destinationEndPoint">IPEndPoint to send the message to.</param>
    /// <param name="buffer">Message to send.</param>
    public override void Send(IPEndPoint destinationEndPoint, byte[] buffer)
    {
        lock (m_SendLockObj)
        {
            m_sipConn.Send(buffer, buffer.Length, destinationEndPoint);
        }
    }

    /// <summary>
    /// Sends a byte array.
    /// </summary>
    /// <param name="dstEndPoint">IPEndPoint to send the message to.</param>
    /// <param name="buffer">Message to send.</param>
    /// <param name="serverCertificateName">Not used. May be null.</param>
    public override void Send(IPEndPoint dstEndPoint, byte[] buffer, string serverCertificateName)
    {
        Send(dstEndPoint, buffer);
    }

    /// <summary>
    /// Gets the connection status.
    /// </summary>
    /// <param name="remoteEndPoint">Endpoint to test.</param>
    /// <returns>Always returns true for UDP because it is not a connected transport protocol.</returns>
    public override bool IsConnectionEstablished(IPEndPoint remoteEndPoint)
    {
        return true;
    }

    /// <summary>
    /// Not used for UDP.
    /// </summary>
    /// <returns></returns>
    protected override Dictionary<string, SIPConnection> GetConnectionsList()
    {
        return null;
    }

    /// <summary>
    /// Closes the UDP client.
    /// </summary>
    public override void Close()
    {
        try
        {
            m_Qos.Shutdown();
            m_Qos = null;
            Closed = true;
            m_sipConn.Close();
        }
        catch (Exception)
        {
        }
    }
}
