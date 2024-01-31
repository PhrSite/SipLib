#region License
//-----------------------------------------------------------------------------
// Filename: SIPChannel.cs
//
// Description: Generic items for SIP channels.
// 
// History:
// 19 Apr 2008	Aaron Clauson	Created (split from original SIPUDPChannel).
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
//	Revised:	7 Nov 22 PHR -- Initial version. Added documentation comments
/////////////////////////////////////////////////////////////////////////////////////

using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

using SipLib.Core;

namespace SipLib.Channels;

/// <summary>
/// Base class for all SIP channels.
/// </summary>
public abstract class SIPChannel
{
    // Wait this long before starting the prune checks, there will be no connections to prune initially
    // and the CPU is needed elsewhere.
    private const int INITIALPRUNE_CONNECTIONS_DELAY = 60000;

    // The period at which to prune the connections.
    private const int PRUNE_CONNECTIONS_INTERVAL = 60000;

    // The number of minutes after which if no transmissions are sent or received a connection will be
    // pruned.
    private const int PRUNE_NOTRANSMISSION_MINUTES = 70;

    /// <summary>
    /// Keeps a list of TCP sockets this process is listening on to prevent it establishing TCP
    /// connections to itself.
    /// </summary>
    /// <value></value>
    public List<string> LocalTCPSockets = new List<string>();

    /// <summary>
    /// This is the local SIPURI
    /// </summary>
    /// <value></value>
    protected SIPURI SipUri = null;

    /// <summary>
    /// This is set to true when the main listener thread has been started.
    /// </summary>
    /// <value></value>
    public bool ChannelStarted = false;

    /// <summary>
    /// This is the local SIPEndPoint
    /// </summary>
    /// <value></value>
    protected SIPEndPoint LocalSIPEndPoint = null;

    /// <summary>
    /// Gets the local SIPEndPoint for the SIP channel
    /// </summary>
    /// <value></value>
    public SIPEndPoint SIPChannelEndPoint
    {
        get { return LocalSIPEndPoint; }
    }

    /// <summary>
    /// Sets the local contact SIPURI for this SIPChannel. This method must be called in the constructor
    /// of all derived classes.
    /// </summary>
    /// <param name="User">Specifies the User part of the SIPURI. This may be null.</param>
    protected void SetupContactURI(string User)
    {
        SIPSchemesEnum Sse = IsTLS == true ? SIPSchemesEnum.sips : SIPSchemesEnum.sip;
        SipUri = new SIPURI(Sse, LocalSIPEndPoint);
        SipUri.User = User;
    }

    /// <summary>
    /// This is the URI to be used for contacting this SIP channel.
    /// </summary>
    /// <value></value>
    public SIPURI SIPChannelContactURI
    {
        get
        {
            //return LocalSIPEndPoint.ToString();
            return SipUri;
        }
    }

    /// <summary>
    /// If the underlying transport channel is reliable, such as TCP, this will be set to true;
    /// </summary>
    /// <value></value>
    protected bool m_isReliable;

    /// <summary>
    /// If the underlying transport channel is reliable, such as TCP, this will be set to true;
    /// </summary>
    /// <value></value>
    public bool IsReliable
    {
        get { return m_isReliable; }
    }

    /// <summary>
    /// True if the channel is using Transport Layer Security (TLS)
    /// </summary>
    /// <value></value>
    protected bool m_IsTLS;

    /// <summary>
    /// Gets the m_IsTLS property. If true then the SIPConnection uses Transport Layer Security (TLS).
    /// </summary>
    /// <value></value>
    public bool IsTLS
    {
        get { return m_IsTLS; }
    }

    /// <summary>
    /// True if the connection has been closed.
    /// </summary>
    /// <value></value>
    protected bool Closed;

    /// <summary>
    /// Delegate (callback function) that gets called when a SIP message is received.
    /// </summary>
    /// <value></value>
    public SIPMessageReceivedDelegate SIPMessageReceived;

    /// <summary>
    /// Sends a SIP message to the specified destination IPEndPoint
    /// </summary>
    /// <param name="destinationEndPoint">Destination to send the message to</param>
    /// <param name="message">String message to send</param>
    public abstract void Send(IPEndPoint destinationEndPoint, string message);

    /// <summary>
    /// Sends a SIP message to the specified destination IPEndPoint
    /// </summary>
    /// <param name="destinationEndPoint">Destination to send the message to</param>
    /// <param name="buffer">Byte array containing the SIP message</param>
    public abstract void Send(IPEndPoint destinationEndPoint, byte[] buffer);

    /// <summary>
    /// Sends a SIP message to a destination IPEndPoint given a byte array and the namd of the server's 
    /// X.509 certificate name
    /// </summary>
    /// <param name="destinationEndPoint">Destination to send the message to</param>
    /// <param name="buffer">Byte array containing the SIP message</param>
    /// <param name="serverCertificateName">Name of the server's X.509 certificate</param>
    public abstract void Send(IPEndPoint destinationEndPoint, byte[] buffer, string serverCertificateName);

    /// <summary>
    /// Closes the connection
    /// </summary>
    public abstract void Close();

    /// <summary>
    /// Returns true if there is an established connection the specified IPEndPoint
    /// </summary>
    /// <param name="remoteEndPoint"></param>
    /// <returns></returns>
    public abstract bool IsConnectionEstablished(IPEndPoint remoteEndPoint);

    /// <summary>
    /// Gets reference to the dictionary of the connections. The string is the string version of the
    /// remote endpoints IPEndPoint.
    /// </summary>
    /// <returns></returns>
    /// <value></value>
    protected abstract Dictionary<string, SIPConnection> GetConnectionsList();

    /// <summary>
    /// Periodically checks the established connections and closes any that have not had a transmission
    /// for a specified period or where the number of connections allowed per IP address has been
    /// exceeded. Only relevant for connection oriented channels such as TCP and TLS.
    /// </summary>
    protected void PruneConnections(string threadName)
    {
        try
        {
            Thread.CurrentThread.Name = threadName;

            Thread.Sleep(INITIALPRUNE_CONNECTIONS_DELAY);

            while (!Closed)
            {
                bool checkComplete = false;

                while (!checkComplete)
                {
                    try
                    {
                        SIPConnection inactiveConnection = null;
                        Dictionary<string, SIPConnection> connections = GetConnectionsList();

                        lock (connections)
                        {
                            var inactiveConnectionKey = 
                                (from connection in connections
                                where connection.Value.LastTransmission 
                                <DateTime.Now.AddMinutes(PRUNE_NOTRANSMISSION_MINUTES * -1)
                                select connection.Key).FirstOrDefault();

                            if (inactiveConnectionKey != null)
                            {
                                inactiveConnection = connections[inactiveConnectionKey];
                                connections.Remove(inactiveConnectionKey);
                            }
                        }

                        if (inactiveConnection != null)
                            inactiveConnection.Close();
                        else
                            checkComplete = true;
                    }
                    catch (SocketException)
                    {
                        // Will be thrown if the socket is already closed.
                    }
                    catch (Exception)
                    {
                        checkComplete = true;
                    }
                }

                Thread.Sleep(PRUNE_CONNECTIONS_INTERVAL);
                checkComplete = false;
            }

        }
        catch (Exception)
        {
        }
    }

    /// <summary>
    /// Gets the transport protocol used for this channel. 
    /// </summary>
    /// <returns>Returns a SIPProtocolsEnum value.</returns>
    public SIPProtocolsEnum GetProtocol()
    {
        SIPProtocolsEnum Result;
        if (this.GetType() == typeof(SIPUDPChannel))
            Result = SIPProtocolsEnum.udp;
        else if (GetType() == typeof(SIPTCPChannel))
            Result = SIPProtocolsEnum.tcp;
        else if (GetType() == typeof(SIPTLSChannel))
            Result = SIPProtocolsEnum.tls;
        else
            // Actually not possible but use a default
            Result = SIPProtocolsEnum.udp;

        return Result;
    }

    /// <summary>
    /// Virtual function to get the remote certificate for a connection that is being managed by this
    /// SIPChannel object.
    /// </summary>
    /// <param name="strRemoteEp">String version of the IP endpoint of the connection.</param>
    /// <returns>Returns null if there is no remote certificate available.</returns>
    public virtual X509Certificate2 GetRemoteCertificate2(string strRemoteEp)
    {
        return null;
    }

    /// <summary>
    /// Virtual function to get the remote certificate for a connection that is being managed by this
    /// SIPChannel object.
    /// </summary>
    /// <param name="strRemoteEp">String version of the IP endpoint of the connection.</param>
    /// <returns>Returns null if there is no remote certificate available.</returns>
    public virtual X509Certificate GetRemoteCertificate(string strRemoteEp)
    {
        return null;
    }
}
