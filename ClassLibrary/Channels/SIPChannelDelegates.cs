#region Licenses
// ============================================================================
// FileName: SIPChannelDelegates.cs
//
// Description:
// A list of function delegates that are used by the SIP Server Agents.
//
// Author(s):
// Aaron Clauson
//
// History:
// 14 Nov 2008	Aaron Clauson	Created.
//
// License: 
// This software is licensed under the BSD License http://www.opensource.org/licenses/bsd-license.php
//
// Copyright (c) 2008 Aaron Clauson (aaron@sipsorcery.com), SIP Sorcery PTY LTD, Hobart, Australia (www.sipsorcery.com)
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
// ============================================================================
#endregion

/////////////////////////////////////////////////////////////////////////////////////
//	Revised:	8 Nov 22 PHR -- Initial version.
/////////////////////////////////////////////////////////////////////////////////////

using SipLib.Body;
using SipLib.Core;
using System.Net;

namespace SipLib.Channels;

// SIP Channel delegates.

/// <summary>
/// Delegate definition for SIPMessageSent event of the SIPConnection class.
/// </summary>
/// <param name="sipChannel">SIPChannel derived object that the SIP message was received on.</param>
/// <param name="remoteEndPoint">SIPEndPoint of the receiver of the message.</param>
/// <param name="buffer">Contains the binary bytes of the SIP message.</param>
public delegate void SIPMessageSentDelegate(SIPChannel sipChannel, SIPEndPoint remoteEndPoint, byte[]
    buffer);

/// <summary>
/// Delegate definition for the SIPMessageReceived event of the SIPConnection class.
/// </summary>
/// <param name="sipChannel">SIPChannel derived object that the SIP message was received on.</param>
/// <param name="remoteEndPoint">SIPEndPoint of the sender of the message.</param>
/// <param name="buffer">Contains the binary bytes of the SIP message.</param>
public delegate void SIPMessageReceivedDelegate(SIPChannel sipChannel, SIPEndPoint remoteEndPoint, 
    byte[] buffer);

// 23 Aug 23 PHR
/// <summary>
/// Delegate type for the SIPSocketDisconnected event of the SIPConnection class.
/// </summary>
/// <param name="remoteEndPoint">IPEndPoint of the remote endpoint</param>
public delegate void SIPConnectionDisconnectedDelegate(IPEndPoint remoteEndPoint);

// 23 Aug 23 PHR
/// <summary>
/// Delegate type for the SIPConnectionFailed event of the SIPTCPChannel and the SIPTLSChannel classes.
/// </summary>
/// <param name="sipChannel">SIPChannel derived object that the connection request failed on.</param>
/// <param name="remoteEndPoint">Remote IPEndPoint</param>
public delegate void SipConnectionFailedDelegate(SIPChannel sipChannel, IPEndPoint remoteEndPoint);

/// <summary>
/// Delegate type for the SipRequestReceived event of the SipTransportManager class.
/// </summary>
/// <param name="sipRequest">Request that was received.</param>
/// <param name="remoteEndPoint">Remote endpoint that received the message.</param>
/// <param name="ContentsList">List of the contents contained in the body of the request. Will be null
/// if the request does not have a body.</param>
/// <param name="sipTransportManager">SipTransportManager that fired the event.</param>
public delegate void SipRequestReceivedDelegate(SIPRequest sipRequest, SIPEndPoint remoteEndPoint,
    List<MessageContentsContainer> ContentsList, SipTransportManager sipTransportManager);

/// <summary>
/// Delegate type for the SipResponseReceived event of the SipTransportManager class.
/// </summary>
/// <param name="sipResponse">Request that was received</param>
/// <param name="remoteEndPoint">Remote endpoint that sent the response</param>
/// <param name="sipTransportManager">SipTransportManager that fired the event.</param>
public delegate void SipResponseReceivedDelegate(SIPResponse sipResponse, SIPEndPoint remoteEndPoint,
    SipTransportManager sipTransportManager);

/// <summary>
/// Delegate type for the method that the SipTransportManager will call when a SIP transaction has been
/// completed.
/// </summary>
/// <param name="sipRequest">SIP request for the transaction.</param>
/// <param name="sipResponse">SIP response that was received. Will be null if the transaction timed out.</param>
/// <param name="remoteEndPoint">Endpoint that send the response. Will be null if the transaction timed out.
/// </param>
/// <param name="sipTransportManager">SipTransportManager that called this method.</param>
public delegate void SipTransactionCompleteDelegate(SIPRequest sipRequest, SIPResponse sipResponse,
    IPEndPoint remoteEndPoint, SipTransportManager sipTransportManager);