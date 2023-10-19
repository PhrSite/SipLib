/////////////////////////////////////////////////////////////////////////////////////
//  File:   H264RtpSender.cs                                        18 Oct 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Video;

using SipLib.Rtp;

/// <summary>
/// Delegate type for a function that sends an RTP packet. Used for the H264RtpSender class when
/// an RTP packet needs to be sent.
/// </summary>
/// <param name="rtpPckt"></param>
public delegate void RtpSendDelegate(RtpPacket rtpPckt);

/// <summary>
/// Class that processes H264 encoded access units and packetizes H264 NAL units into RTP packets so the
/// H264 encoded data can be sent over the network.
/// </summary>
public class H264RtpSender
{
    private ushort m_SequenceNumber = 0;
    private uint m_Timestamp = 0;
    private uint m_SSRC = 0;
    private int m_PayloadType = 0;
    private uint m_TimestampIncrement = 0;
    private RtpSendDelegate RtpSender = null;

    private const uint H264_CLOCK_RATE = 90000;
    private static Random m_Rnd = new Random();
    private const int MAX_RTP_PAYLOAD = 1200;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="payloadType">RTP payload number to use</param>
    /// <param name="frameRate">Video frame rate in frames per second</param>
    /// <param name="sender">Delegate to use to send RTP packets.</param>
    public H264RtpSender(int payloadType, uint frameRate, RtpSendDelegate sender)
    {
        m_PayloadType = payloadType;
        m_TimestampIncrement = H264_CLOCK_RATE / frameRate;
        RtpSender = sender;
        m_SSRC = Convert.ToUInt32(m_Rnd.Next());
    }

    // See https://www.itu.int/rec/dologin_pub.asp?lang=e&id=T-REC-H.264-201602-S!!PDF-E&type=items 
    // Annex B for the byte stream specification. 
    /// <summary>
    /// Processes an H264 frame contained in an H264 Access Unit.
    /// An Access Unit can contain one or more NAL's. The NAL's have to be parsed in order to be able to package 
    /// in RTP packets.
    /// </summary>
    /// <param name="accessUnit">Input H264 access unit</param>
    public void ProcessH264Frame(byte[] accessUnit)
    {
        foreach (var nal in H264Packetiser.ParseNals(accessUnit))
        {
            SendH264Nal(nal.NAL, nal.IsLast);
        }
    }

    private void SendH264Nal(byte[] nal, bool isLastNal)
    {
        byte nal0 = nal[0];

        if (nal.Length <= MAX_RTP_PAYLOAD)
        {
            // Send as Single-Time Aggregation Packet (STAP-A).
            byte[] payload = new byte[nal.Length];
            //int markerBit = isLastNal ? 1 : 0;   // There is only ever one packet in a STAP-A.
            Buffer.BlockCopy(nal, 0, payload, 0, nal.Length);

            SendRtpPacket(payload, isLastNal);
        }
        else
        {
            nal = nal.Skip(1).ToArray();

            // Send as Fragmentation Unit A (FU-A):
            for (int index = 0; index * MAX_RTP_PAYLOAD < nal.Length; index++)
            {
                int offset = index * MAX_RTP_PAYLOAD;
                int payloadLength = ((index + 1) * MAX_RTP_PAYLOAD < nal.Length) ? MAX_RTP_PAYLOAD : 
                    nal.Length - index * MAX_RTP_PAYLOAD;

                bool isFirstPacket = index == 0;
                bool isFinalPacket = (index + 1) * MAX_RTP_PAYLOAD >= nal.Length;
                //int markerBit = (isLastNal && isFinalPacket) ? 1 : 0;
                bool markerBit = isLastNal && isFinalPacket;
                byte[] h264RtpHdr = H264Packetiser.GetH264RtpHeader(nal0, isFirstPacket, isFinalPacket);

                byte[] payload = new byte[payloadLength + h264RtpHdr.Length];
                Buffer.BlockCopy(h264RtpHdr, 0, payload, 0, h264RtpHdr.Length);
                Buffer.BlockCopy(nal, offset, payload, h264RtpHdr.Length, payloadLength);

                SendRtpPacket(payload, markerBit);
            }
        }

        if (isLastNal)
            m_Timestamp += m_TimestampIncrement;
    }

    private void SendRtpPacket(byte[] payload, bool markerBit)
    {
        RtpPacket rtpPacket = new RtpPacket(payload.Length);
        rtpPacket.SSRC = m_SSRC;
        rtpPacket.SequenceNumber = m_SequenceNumber;
        rtpPacket.Timestamp = m_Timestamp;
        rtpPacket.Marker = markerBit;
        rtpPacket.PayloadType = m_PayloadType;
        rtpPacket.Payload = payload;
        RtpSender?.Invoke(rtpPacket);

        m_SequenceNumber += 1;
    }
}
