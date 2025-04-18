/////////////////////////////////////////////////////////////////////////////////////
//  File:   TestCallParams.cs                                       3 Apr 25 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.TestCalls;

using SipLib.Rtp;
using SipLib.Sdp;
using SipLib.Core;

internal class TestCallParams
{
    public int PayloadType = 0;

    public uint Timestamp = 0;

    public uint SSRC = 0;

    public ushort SequenceNumber = 0;

    public uint ClockRate = 8000;

    public bool IsMediaLoopback = false;

    public PacketLoopbackTypeEnum PacketLoopbackType = PacketLoopbackTypeEnum.DirectPacketLoopback;

    private int m_NumPacketsReceived = 0;
    private object m_LockObj = new object();

    /// <summary>
    /// Default constructor
    /// </summary>
    public TestCallParams()
    {
    }

    public TestCallParams(MediaDescription AnsweredMd, RtpChannel channel)
    {
        SdpAttribute loopbackAttribute = AnsweredMd.GetNamedAttribute(TestCallConstants.LoopbackAttributeName)!;
        if (loopbackAttribute.Value == TestCallConstants.RtpMediaLoopbackAttributeValue)
        {   // Its media loopback
            IsMediaLoopback = true;
            PayloadType = AnsweredMd.PayloadTypes[0];
            RtpMapAttribute? map = AnsweredMd.GetRtpMapForPayloadType(PayloadType);
            if (map != null)
                ClockRate = (uint) map.ClockRate;
            else
                ClockRate = 8000;
        }
        else
        {   // Its RTP packet loopback
            IsMediaLoopback = false;
            RtpMapAttribute? EncapsulatedMap = AnsweredMd.GetRtpMapForCodecType(TestCallConstants.EncapsulatedRtpLoopback);
            RtpMapAttribute? DirectMap = AnsweredMd.GetRtpMapForCodecType(TestCallConstants.DirectRtpLoopback);
            if (EncapsulatedMap != null)
            {
                PacketLoopbackType = PacketLoopbackTypeEnum.EncapsulatedPacketLoopback;
                PayloadType = EncapsulatedMap.PayloadType;
                ClockRate = (uint) EncapsulatedMap.ClockRate;
            }
            else if (DirectMap != null)
            {
                PacketLoopbackType = PacketLoopbackTypeEnum.DirectPacketLoopback;
                PayloadType = DirectMap.PayloadType;
                ClockRate = (uint) DirectMap.ClockRate;
            }
        }

        SSRC = channel.SSRC;
        SequenceNumber = Crypto.GetRandomUInt16();
        Timestamp = Crypto.GetRandomUInt();
    }

    public void IncrementPacketsReceived()
    {
        lock (m_LockObj)
        {
            m_NumPacketsReceived += 1;           
        }
    }

    public int PacketsReceived
    {
        get
        {
            int Result = 0;
            lock (m_LockObj)
            {
                Result = m_NumPacketsReceived;
            }
            return Result;
        }
    }
}

internal enum PacketLoopbackTypeEnum
{
    DirectPacketLoopback,
    EncapsulatedPacketLoopback
}