/////////////////////////////////////////////////////////////////////////////////////
//  File:   MsrpDelegateTypes.cs                                    1 Aug 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Msrp;

/// <summary>
/// Delegate type for the MsrpMessageReceived event of the MsrpConnection class.
/// </summary>
/// <param name="ContentType">Value of the Content-Type header minus any header parameters.</param>
/// <param name="Contents">Binary contents for this message. If the message was chunked then this array
/// will contain all of the chunks concatenated together.</param>
public delegate void MsrpMessageReceivedDelegate(string ContentType, byte[] Contents);

/// <summary>
/// Delegate type for the MsrpConnectionEstablisned and the MsrpConnectionFailed events of the MsrpConnection
/// class.
/// </summary>
/// <param name="ConnectionIsPassive">If true then the connection is passive, i.e., the MsrpConnection
/// object is the server. False if the MsrpConnection is the client.</param>
/// <param name="RemoteMsrpUri">MrspUri of the remote endpoint.</param>
public delegate void MsrpConnectionStatusDelegate(bool ConnectionIsPassive, MsrpUri RemoteMsrpUri);

/// <summary>
/// Delegate type for the MsrpMessageDeliveryFailed event of the MsrpConnection class.
/// </summary>
/// <param name="message">Message that was sent and rejected by the remote endpoint</param>
/// <param name="remoteMsrpUri">MsrpUri of the remote endpoint</param>
/// <param name="StatusCode">Status code that was returned in the respoinse message.</param>
/// <param name="StatusText">Status text that was returned in the response message. May be null.</param>
public delegate void MsrpMessageDeliveryFailedDelegate(MsrpMessage message, MsrpUri remoteMsrpUri, 
    int StatusCode, string StatusText);

/// <summary>
/// Delegate type for the ReportReceived event of the MsrpConnection class. This event is fired
/// when it receives a MSRP REPORT request if a success report was request in call to the SendMsrpMessage
/// method.
/// </summary>
/// <param name="messageID">Message ID parameter that was specified in the SendMsrpMessage call</param>
/// <param name="TotalBytes">Total bytes that were received by the remote endpoint</param>
/// <param name="StatusCode">Status code from the REPORT request</param>
/// <param name="StatusText">Status text from the REPORT request. May be null.</param>
public delegate void ReportReceivedDelegate(string messageID, int TotalBytes, int StatusCode, string 
    StatusText);

