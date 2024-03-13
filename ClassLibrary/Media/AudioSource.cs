/////////////////////////////////////////////////////////////////////////////////////
//  File:   AudioSource.cs                                          5 Mar 24 PHR
/////////////////////////////////////////////////////////////////////////////////////

using SipLib.Rtp;
using SipLib.Sdp;
using System.Collections.Concurrent;

namespace SipLib.Media;

/// <summary>
/// Class for sending sourced audio (from a microphone or a recording) to a remote endpoint via RTP packets
/// over an RtpChannel.
/// </summary>
public class AudioSource
{
    private int m_AudioPayloadType = 0;
    private int m_TelephoneEventPayloadType = 101;
    private bool m_TelephoneEventEnabled = false;
    private int m_TelephoneEventClockRate = 8000;

    private RtpChannel m_RtpChannel;
    private IAudioEncoder? m_AudioEncoder = null;
    private uint m_SamplesPerPacket;
    private const int PACKET_TIME_MS = 20;

    private uint m_SSRC;
    private ushort m_SequenceNumber = 0;
    private uint m_Timestamp = 0;

    /// <summary>
    /// The number of DTMF event RTP packets to send.
    /// </summary>
    private const int DTMF_PACKETS = 4;
    /// <summary>
    /// The number of DTMF end packets to send
    /// </summary>
    private const int DTMF_END_PACKETS = 3;

    private int m_DtmfPacketsSent = 0;
    private int m_DtmfEndPacketsSent = 0;
    private DtmfEventState m_DtmfEventState = DtmfEventState.Idle;
    private DtmfEventEnum m_DtmfEvent;
    private ushort m_DtmfDuration = 0;
    private ConcurrentQueue<DtmfEventEnum> m_DtmfQueue = new ConcurrentQueue<DtmfEventEnum>();

    /// <summary>
    /// Stores the current state of the audio source.
    /// </summary>
    /// <value></value>
    private AudioSourceStateEnum AudioSourceState = AudioSourceStateEnum.Stopped;

    private int m_SampleRate;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="AnsweredMediaDescription">The MediaDescription object that was sent as the answer to
    /// the offered MediaDescription. This object contains the negotiated media type and codec type.</param>
    /// <param name="Encoder">IAudioEncoder to use to encode linear 16-bit PCM sample data that will be
    /// sent on the RtpChannel</param>
    /// <param name="rtpChannel">The RtpChannel to send RTP packets on</param>
    public AudioSource(MediaDescription AnsweredMediaDescription, IAudioEncoder Encoder, RtpChannel rtpChannel)
    {
        m_RtpChannel = rtpChannel;
        m_AudioEncoder = Encoder;
        m_SampleRate = m_AudioEncoder.SampleRate;

        foreach (RtpMapAttribute Rma in AnsweredMediaDescription.RtpMapAttributes)
        {
            switch (Rma.EncodingName.ToUpper())
            {
                case "TELEPHONE-EVENT":
                    m_TelephoneEventEnabled = true;
                    m_TelephoneEventPayloadType = Rma.PayloadType;
                    m_TelephoneEventClockRate = Rma.ClockRate;
                    break;
                default:
                    m_AudioPayloadType = Rma.PayloadType;
                    break;
            }
        }

        m_SSRC = rtpChannel.SSRC;
        m_SamplesPerPacket = (uint)(m_SampleRate * PACKET_TIME_MS) / 1000;

    }

    /// <summary>
    /// 
    /// </summary>
    public void Start()
    {
        if (AudioSourceState != AudioSourceStateEnum.Stopped)
            return;

        AudioSourceState = AudioSourceStateEnum.Playing;
    }

    /// <summary>
    /// 
    /// </summary>
    public void Stop()
    {
        if (AudioSourceState == AudioSourceStateEnum.Stopped)
            return;

        AudioSourceState = AudioSourceStateEnum.Stopped;
    }

    /// <summary>
    /// Pauses transmission of RTP packets.
    /// </summary>
    public virtual void Pause()
    {
        if (AudioSourceState == AudioSourceStateEnum.Playing)
            AudioSourceState = AudioSourceStateEnum.Paused;
    }

    /// <summary>
    /// Resumes generation and transmission of RTP packets.
    /// </summary>
    public virtual void Resume()
    {
        if (AudioSourceState == AudioSourceStateEnum.Paused)
            AudioSourceState = AudioSourceStateEnum.Playing;
    }

    /// <summary>
    /// Event handler for the AudioSamplesReady event of the IAudioSampleSource object that is providing
    /// audio samples to send to the remote endpoint via the RtpChannel.
    /// </summary>
    /// <param name="AudioSamples"></param>
    /// <param name="SampleRate"></param>
    public void OnAudioSamplesReady(short[] AudioSamples, int SampleRate)
    {
        if (AudioSourceState != AudioSourceStateEnum.Playing)
            return;

        if (m_DtmfEventState == DtmfEventState.Idle)
        {   // Check to see if there is a new DTMF event to send
            if (m_DtmfQueue.TryDequeue(out m_DtmfEvent) == true)
                StartDtmfEvent();
            else
                SendNextAudioRtpPacket(AudioSamples, SampleRate);
        }
        else
            SendNextDtmfEventPacket();
    }

    private void SendNextAudioRtpPacket(short[] NewSamples, int SampleRate)
    {
        short[] SamplesToSend = GetNextAudioSamples(NewSamples, SampleRate);

        byte[] PayloadBytes = m_AudioEncoder!.Encode(SamplesToSend);
        RtpPacket rtpPacket = new RtpPacket(RtpPacket.MIN_PACKET_LENGTH + PayloadBytes.Length);
        rtpPacket.PayloadType = m_AudioPayloadType;
        rtpPacket.SSRC = m_SSRC;
        rtpPacket.SequenceNumber = m_SequenceNumber;
        rtpPacket.Timestamp = m_Timestamp;
        rtpPacket.SetPayloadBytes(PayloadBytes);

        m_RtpChannel.Send(rtpPacket);
        m_SequenceNumber += 1;
        m_Timestamp += m_SamplesPerPacket;

    }

    private short[] GetNextAudioSamples(short[] NewSamples, int SampleRate)
    {
        // TODO: Interpolate, decimate or return the input array of new samples depending on the sample
        // rate of the new samples and the sample rate required by the m_AudioEncoder object.

        return NewSamples;
    }

    /// <summary>
    /// Sends a DTMF event. This event sends a single DTMF event. The event length is 80 ms and three end
    /// packets are sent. This class does not ensure the minimum inter-digit gap of 40 ms so the application is
    /// responsible for doing this. Ensuring the minimum inter-digit gap is not an issue because DTMF digits
    /// are typically sent by a user by typing them on a keypad.
    /// </summary>
    /// <param name="dtmfEvent">Event to send.</param>
    public void SendDtmfEvent(DtmfEventEnum dtmfEvent)
    {
        if (m_TelephoneEventEnabled == false) return;
        if (AudioSourceState != AudioSourceStateEnum.Playing) return;

        m_DtmfQueue.Enqueue(dtmfEvent);
    }

    private void StartDtmfEvent()
    {
        m_DtmfEventState = DtmfEventState.SendingDtmfEventPackets;
        m_DtmfPacketsSent = 0;
        m_DtmfEndPacketsSent = 0;
        m_DtmfDuration = 0;
        SendNextDtmfEventPacket();
    }

    private void SendNextDtmfEventPacket()
    {
        RtpPacket RtpPckt = new RtpPacket(DtmfPacket.DTMF_PACKET_LENGTH);
        RtpPckt.SSRC = m_SSRC;
        RtpPckt.Timestamp = m_Timestamp;
        RtpPckt.SequenceNumber = m_SequenceNumber;
        RtpPckt.PayloadType = m_TelephoneEventPayloadType;

        DtmfPacket DtmfPckt = new DtmfPacket();     // Using the default Volume setting
        DtmfPckt.Event = m_DtmfEvent;

        if (m_DtmfEventState == DtmfEventState.SendingDtmfEventPackets)
        {
            m_DtmfPacketsSent += 1;
            if (m_DtmfPacketsSent == 1)
                RtpPckt.Marker = true;

            DtmfPckt.Duration = m_DtmfDuration;
            m_DtmfDuration = (ushort)(m_DtmfDuration + m_TelephoneEventClockRate);
            if (m_DtmfPacketsSent >= DTMF_PACKETS)
                m_DtmfEventState = DtmfEventState.SendingDtmfEndPackets;
        }
        else
        {   // Send event end packets
            m_DtmfEndPacketsSent += 1;
            DtmfPckt.Duration = m_DtmfDuration;     // Do not increment for end packets
            DtmfPckt.Eflag = true;
            if (m_DtmfEndPacketsSent >= DTMF_END_PACKETS)
                m_DtmfEventState = DtmfEventState.Idle;
        }

        RtpPckt.SetPayloadBytes(DtmfPckt.GetPacketBytes());
        m_RtpChannel.Send(RtpPckt);
        // Note: The Timestamp is not incremented for DTMF event packets
        m_SequenceNumber += 1;
    }

    /// <summary>
    /// Enumeration of the possible states of an audio source
    /// </summary>
    /// <value></value>
    private enum AudioSourceStateEnum
    {
        /// <summary></summary>
        Stopped,
        /// <summary></summary>
        Paused,
        /// <summary></summary>
        Playing,
    }

    private enum DtmfEventState
    {
        Idle,
        SendingDtmfEventPackets,
        SendingDtmfEndPackets
    }

}
