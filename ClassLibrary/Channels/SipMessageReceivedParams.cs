/////////////////////////////////////////////////////////////////////////////////////
//  File:   SipMessageReceivedParams.cs                             29 Aug 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Channels;

using SipLib.Core;

/// <summary>
/// Container class for carrying the information about a SIP message that was received
/// from a SIP connection.
/// </summary>
internal class SipMessageReceivedParams
{
    public SIPChannel SipChannel;
    public SIPEndPoint RemoteEndPoint;
    public byte[] buffer;

    public SipMessageReceivedParams(SIPChannel sipChannel, SIPEndPoint remoteEndPoint, byte[] buffer)
    {
        SipChannel = sipChannel;
        RemoteEndPoint = remoteEndPoint;
        this.buffer = buffer;
    }
}
