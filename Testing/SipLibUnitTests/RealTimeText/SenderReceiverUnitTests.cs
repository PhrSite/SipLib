/////////////////////////////////////////////////////////////////////////////////////
//  File:   SenderReceiverUnitTests.cs                              17 Oct 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

using SipLib.RealTimeText;
using SipLib.Rtp;

namespace SipLibUnitTests.RealTimeText;

[Trait("Category", "unit")]
public class RttSenderReceiverUnitTests
{

    private RttReceiver rttReceiver = null;

    /// <summary>
    /// Test the case where RTT redundancy is enabled , CPS = 0 (block send) and the first packet
    /// containing all of the new text is dropped.
    /// </summary>
    [Fact]
    public void MissedPacketWithRedundancy()
    {
        RttParameters rttParameters = new RttParameters();

        RttSender rttSender = new RttSender(rttParameters, RttRtpSendCallback);
        rttSender.Start();

        string strMessage = "Hello World!";
        rttReceiver = new RttReceiver(rttParameters);
        rttReceiver.RttCharactersReceived += (rxChars, SSRC) =>
        { Assert.True(rxChars == strMessage, "Received message mismatch"); };

        rttSender.SendMessage(strMessage);

        Thread.Sleep(200);
        rttSender.Stop();
    }

    private int m_PacketCount = 0;

    private void RttRtpSendCallback(RtpPacket packet)
    {
        m_PacketCount++;
        if (m_PacketCount != 1)
            rttReceiver.ProcessRtpPacket(packet);
    }
}
