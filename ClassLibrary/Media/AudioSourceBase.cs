/////////////////////////////////////////////////////////////////////////////////////
//  File:   AudioSourceBase.cs                                      2 Jan 24 PHR
/////////////////////////////////////////////////////////////////////////////////////

using SipLib.Sdp;
using SipLib.Rtp;
using System.Collections.Concurrent;

namespace SipLib.Media;

/// <summary>
/// Base class for all audio sources. This base class implementation provides an audio source that provides
/// silence audio samples to the RtpChannel. It also implements DTMF event functionality to support sending
/// of DTMF event codes via the RTP channel.
/// </summary>
public class AudioSourceBase
{
    private int m_AudioPayloadType = 0;
    private int m_TelephoneEventPayloadType = 101;
    private bool m_TelephoneEventEnabled = false;
    private int m_TelephoneEventClockRate = 8000;

    private RtpChannel m_RtpChannel;
    private IAudioEncoder m_AudioEncoder = null;
    private uint m_SamplesPerPacket;

    private const int PACKET_TIME_MS = 20;

    private uint m_SSRC;
    private ushort m_SequenceNumber = 0;
    private uint m_Timestamp  = 0;

    /// <summary>
    /// The sample rate in samples per second of the audio source
    /// </summary>
    /// <value></value>
    protected int SampleRate = 8000;

    /// <summary>
    /// Stores the current state of the audio source.
    /// </summary>
    /// <value></value>
    protected AudioSourceStateEnum AudioSourceState = AudioSourceStateEnum.Stopped;

    private HighResolutionTimer m_HighResolutionTimer = null;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="AnsweredMediaDescription">The MediaDescription that was sent in response to the offered
    /// MediaDescription. Contains the negotiated encoding type and payload information.</param>
    /// <param name="Encoder">Audio encoder to use</param>
    /// <param name="rtpChannel">Channel to send the generated audio data on.</param>
    /// <param name="HiResTimer">High resolution timer to use. If null, then a low resolution timer
    /// (System.Threading.Timer) will be used.</param>
    public AudioSourceBase(MediaDescription AnsweredMediaDescription, IAudioEncoder Encoder, RtpChannel rtpChannel, 
        HighResolutionTimer HiResTimer)
    {
        m_RtpChannel = rtpChannel;
        m_AudioEncoder = Encoder;
        SampleRate = m_AudioEncoder.SampleRate;

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

        m_SamplesPerPacket = (uint)(SampleRate * PACKET_TIME_MS) / 1000;
        m_HighResolutionTimer = HiResTimer;
    }

    /// <summary>
    /// Sets the RTP packet SEQ number and Timestamp to start with when switching from another audio source.
    /// Call this method before calling the Start() or the Resume() methods.
    /// </summary>
    /// <param name="SequenceNumber">The SEQ number from the previous audio source</param>
    /// <param name="Timestamp">The Timestamp from the previous audio source</param>
    public void SetSequenceNumberAndTimestamp(ushort SequenceNumber, uint Timestamp)
    {
        m_SequenceNumber = SequenceNumber;
        m_Timestamp = Timestamp;
    }

    /// <summary>
    /// Gets the RTP packet SEQ number and Timestamp fields to use for the next audio when changing audio
    /// sources for the RtpChannel. Call this method after calling the Stop() or the Pause() methods.
    /// </summary>
    /// <param name="SequenceNumber">Next SEQ number to use</param>
    /// <param name="Timestamp">Next Timestamp to use</param>
    public void GetSequenceNumberAndTimestamp(out ushort SequenceNumber, out uint Timestamp)
    {
        SequenceNumber = m_SequenceNumber;
        Timestamp = m_Timestamp;
    }

    private Timer m_Timer = null;

    private void OnTimerElapsed(object state)
    {
        if (AudioSourceState != AudioSourceStateEnum.Playing)
            return;

        if (m_DtmfEventState == DtmfEventState.Idle)
        {   // Check to see if there is a new DTMF event to send
            if (m_DtmfQueue.TryDequeue(out m_DtmfEvent) == true)
                StartDtmfEvent();
            else
                SendNextAudioRtpPacket();
        }
        else
            SendNextDtmfEventPacket();
    }

    private void SendNextAudioRtpPacket()
    {
        short[] NewSamples = new short[m_SamplesPerPacket];
        GetNextAudioSamples(NewSamples);
        byte[] PayloadBytes = m_AudioEncoder.Encode(NewSamples);
        RtpPacket rtpPacket = new RtpPacket(RtpPacket.MIN_PACKET_LENGTH + PayloadBytes.Length);
        rtpPacket.SSRC = m_SSRC;
        rtpPacket.SequenceNumber = m_SequenceNumber;
        rtpPacket.Timestamp = m_Timestamp;
        rtpPacket.SetPayloadBytes(PayloadBytes);

        m_RtpChannel.Send(rtpPacket);
        m_SequenceNumber += 1;
        m_Timestamp += m_SamplesPerPacket;
    }

    private void OnHighResolutionTimer()
    {
        OnTimerElapsed(null);
    }

    /// <summary>
    /// Starts the generation and transmission of RTP packets containing audio data.
    /// </summary>
    public virtual void Start()
    {
        if (AudioSourceState != AudioSourceStateEnum.Stopped)
            return;

        AudioSourceState = AudioSourceStateEnum.Playing;
        if (m_HighResolutionTimer != null)
            m_HighResolutionTimer.TimerExpired += OnHighResolutionTimer;
        else
            m_Timer = new Timer(OnTimerElapsed, null, 0, 20);
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
    /// Stops the audio source. This audio source cannot be used after calling this method.
    /// </summary>
    public virtual void Stop()
    {
        if (AudioSourceState == AudioSourceStateEnum.Stopped)
            return;
        
        AudioSourceState = AudioSourceStateEnum.Stopped;

        if (m_Timer != null)
        {
            m_Timer.Dispose();
            m_Timer = null;
        }
        else if (m_HighResolutionTimer != null)
            m_HighResolutionTimer.TimerExpired -= OnHighResolutionTimer;
    }

    /// <summary>
    /// Gets the next 16-bit linear PCM raw samples to send. This base class generates silence.
    /// </summary>
    /// <param name="Samples">Destination of 16-bit linear PCM audio.</param>
    protected virtual void GetNextAudioSamples(short[] Samples)
    {
        for (int i = 0; i < Samples.Length; i++)
            Samples[i] = 0;
    }

    private ConcurrentQueue<DtmfEventEnum> m_DtmfQueue = new ConcurrentQueue<DtmfEventEnum>();
    
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

    /// <summary>
    /// The number of DTMf event RTP packets to send.
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
    protected enum AudioSourceStateEnum
    {
        /// <summary></summary>
        Stopped,
        /// <summary></summary>
        Paused,
        /// <summary></summary>
        Playing,
    }

}

internal enum DtmfEventState
{
    Idle,
    SendingDtmfEventPackets,
    SendingDtmfEndPackets
}

