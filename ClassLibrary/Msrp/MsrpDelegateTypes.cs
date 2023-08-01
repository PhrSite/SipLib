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

