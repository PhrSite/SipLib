/////////////////////////////////////////////////////////////////////////////////////
//  File:   SIPChannelDelegates.cs                                  22 Nov 22 PHR	
/////////////////////////////////////////////////////////////////////////////////////

using SipLib.Core;
using System.Net;
using SipLib.Transactions;

namespace SipLib.Channels;

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
/// <param name="sipTransportManager">SipTransportManager that fired the event.</param>
/// <seealso cref="SipTransport.SipRequestReceived"/>
public delegate void SipRequestReceivedDelegate(SIPRequest sipRequest, SIPEndPoint remoteEndPoint,
    SipTransport sipTransportManager);

/// <summary>
/// Delegate type for the SipResponseReceived event of the SipTransportManager class.
/// </summary>
/// <param name="sipResponse">Request that was received</param>
/// <param name="remoteEndPoint">Remote endpoint that sent the response</param>
/// <param name="sipTransportManager">SipTransportManager that fired the event.</param>
/// <seealso cref="SipTransport.SipResponseReceived"/>
public delegate void SipResponseReceivedDelegate(SIPResponse sipResponse, SIPEndPoint remoteEndPoint,
    SipTransport sipTransportManager);

/// <summary>
/// Delegate type for the method that the SipTransport will call when a SIP transaction has been completed.
/// </summary>
/// <param name="sipRequest">SIP request for the transaction.</param>
/// <param name="sipResponse">SIP response that was received. Will be null if the transaction timed out.</param>
/// <param name="remoteEndPoint">Endpoint that send the response. Will be null if the transaction timed out.
/// </param>
/// <param name="sipTransport">SipTransport that called this method.</param>
/// <param name="Transaction">Transaction that completed.</param>
public delegate void SipTransactionCompleteDelegate(SIPRequest sipRequest, SIPResponse sipResponse,
    IPEndPoint remoteEndPoint, SipTransport sipTransport, SipTransactionBase Transaction);

/// <summary>
/// Delegate type for the LogSipRequest event of the SipTransport class.
/// </summary>
/// <param name="sipRequest">SIPRequest that was sent or received</param>
/// <param name="remoteEndPoint">If Sent is true then this is the IPEndPoint that the request was
/// sent to. If Sent is false, then this is the IPEndPoint that the request was received from.</param>
/// <param name="Sent">True if the SipTransport sent the SIPRequest. False if the SipTransport received
/// the SIPRequest.</param>
/// <param name="sipTransport">SipTransport object that fired the event.</param>
/// <seealso cref="SipTransport.LogSipRequest"/>
public delegate void LogSipRequestDelegate(SIPRequest sipRequest, IPEndPoint remoteEndPoint, bool Sent,
    SipTransport sipTransport);

/// <summary>
/// Delegate type for the LogSipResponse event of the SipTransport class.
/// </summary>
/// <param name="sipResponse">SIPResponse that was sent or received</param>
/// <param name="remoteEndPoint">If Sent is true then this is the IPEndPoint that the response was
/// sent to. If Sent is false, then this is the IPEndPoint from which the response was received from.</param>
/// <param name="Sent">If true then the SipTransport sent the SIPResponse. If false then the SipTransport
/// received the response.</param>
/// <param name="sipTransport">SipTransport object that fired the event.</param>
/// <seealso cref="SipTransport.LogSipResponse"/>
public delegate void LogSipResponseDelegate(SIPResponse sipResponse, IPEndPoint remoteEndPoint, bool Sent,
    SipTransport sipTransport);

/// <summary>
/// Delegate type for the LogInvalidSipMessage event of the SipTransport class.
/// </summary>
/// <param name="msgBytes">Byte array containing the received message.</param>
/// <param name="remoteEndPoint">Remote endpoint that sent the message.</param>
/// <param name="messageType">Message type</param>
/// <param name="sipTransport">SipTransport object that fired the event.</param>
/// <seealso cref="SipTransport.LogInvalidSipMessage"/>
public delegate void LogInvalidSipMessageDelegate(byte[] msgBytes, IPEndPoint remoteEndPoint,
    SIPMessageTypesEnum messageType, SipTransport sipTransport);
