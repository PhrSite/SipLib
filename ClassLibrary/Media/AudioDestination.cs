/////////////////////////////////////////////////////////////////////////////////////
//  File:   AudioDestination.cs                                     6 Mar 24 PHR
/////////////////////////////////////////////////////////////////////////////////////

using SipLib.Rtp;
using SipLib.Sdp;

namespace SipLib.Media;

/// <summary>
/// Delegate type for the audio destination handler function that the AudioDestination class will call
/// to process decoded audio packets.
/// </summary>
/// <param name="PcmSamples">Contains the decoded, linear 16-bit audio samples.</param>
public delegate void AudioDestinationDelegate(short[] PcmSamples);

/// <summary>
/// Delegate type for the DtmfDigitReceived event of the AudioDestination class.
/// </summary>
/// <param name="digit">DTMF digit that was received.</param>
public delegate void DtmfDigitReceivedDelegate(DtmfEventEnum digit);

/// <summary>
/// This class receives audio RTP packets from an RtpChannel, decodes them and sends them to an audio destination
/// by calling a delegate function that processes the received audio samples.
/// <para>
/// The clock rate (sample rate) of the audio handled by this class must be either 8000 or 16000.
/// </para>
/// </summary>
public class AudioDestination
{
    private int m_AudioPayloadType = 0;
    private int m_TelephoneEventPayloadType = 101;
    private RtpChannel m_RtpChannel;

    private IAudioDecoder m_AudioDecoder;
    private int m_DestinationSampleRate;
    private int m_DecoderSampleRate;

    private AudioDestinationDelegate? DestinationHandler = null;

    /// <summary>
    /// This event is fired when the end of a DTMF event is detected. This event is fired only once for
    /// each detected DTMF event even though multiple RTP packets for the same event may be received.
    /// </summary>
    public event DtmfDigitReceivedDelegate? DtmfDigitReceived = null;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="AnsweredMediaDescription">The MediaDescription object that was sent as the answer to
    /// the offered MediaDescription. This object contains the negotiated media type and codec type.</param>
    /// <param name="Decoder">IAudioDecoder to use to decode the received audio samples</param>
    /// <param name="rtpChannel">RtpChannel to receive audio RTP packets on.</param>
    /// <param name="destinationHandler">Function to call to handle the decoded audio packets. If this is
    /// null then RTP packets are ignored.</param>
    /// <param name="DestinationSampleRate">Sample rate expected by the destination handler</param>
    /// <exception cref="ArgumentException">Thrown if the audio clock rate is not 8000 or 16000.</exception>
    public AudioDestination(MediaDescription AnsweredMediaDescription, IAudioDecoder Decoder, RtpChannel rtpChannel,
        AudioDestinationDelegate? destinationHandler, int DestinationSampleRate)
    {
        m_AudioDecoder = Decoder;
        m_RtpChannel = rtpChannel;
        DestinationHandler = destinationHandler;
        m_DestinationSampleRate = DestinationSampleRate;
        m_DecoderSampleRate = Decoder.SampleRate;

        foreach (RtpMapAttribute Rma in AnsweredMediaDescription.RtpMapAttributes)
        {
            switch (Rma.EncodingName.ToUpper())
            {
                case "TELEPHONE-EVENT":
                    m_TelephoneEventPayloadType = Rma.PayloadType;
                    break;
                default:
                    m_AudioPayloadType = Rma.PayloadType;
                    if (Rma.ClockRate != 8000 && Rma.ClockRate != 16000)
                        throw new ArgumentException($"The clock rate is: {Rma.ClockRate}, it must be " +
                            "8000 or 16000");

                    break;
            }
        }

        m_RtpChannel.RtpPacketReceived += ProcessRtpPacket;
    }

    private void ProcessRtpPacket(RtpPacket rtpPacket)
    {
        if (DestinationHandler == null)
            return;

        if (rtpPacket.PayloadType == m_AudioPayloadType)
        {
            short[] decodedPacket = m_AudioDecoder.Decode(rtpPacket.Payload);
            if (decodedPacket == null)
            {
                // TODO: Figure out what to do?
                return;
            }

            // Fix the sample rate if necessary
            if (m_DecoderSampleRate < m_DestinationSampleRate)
                // Interpolate the decoded samples
                decodedPacket = SampleRateFixer.DoubleSampleRate(decodedPacket);
            else if (m_DecoderSampleRate > m_DestinationSampleRate)
                // Decimate the decoded samples
                decodedPacket = SampleRateFixer.HalveSampleRate(decodedPacket);

            DestinationHandler?.Invoke(decodedPacket);

        }
        else if (rtpPacket.PayloadType == m_TelephoneEventPayloadType)
        {
            byte[] dtmfPayload = rtpPacket.Payload;
            if (dtmfPayload == null || dtmfPayload.Length < DtmfPacket.DTMF_PACKET_LENGTH)
                return;

            DtmfPacket dtmfPacket = DtmfPacket.Parse(rtpPacket.Payload, 0);
            ProcessDtmfPacket(dtmfPacket);
        }
    }


    // State variable for processing telephone event DTMF digits
    private bool m_ReceivingDtmfDigit = false;

    /// <summary>
    /// A DTMF event sender will typically send several RTP packets for a single DTMF digit to allow for
    /// packet loss in the network. It will typicall sent two or more packets with the E flag bit cleared to 
    /// indicate the event itself and two or more packets with the E flag bit set to indicate the end of
    /// the DTMF event.
    /// </summary>
    /// <param name="dtmfPacket"></param>
    private void ProcessDtmfPacket(DtmfPacket dtmfPacket)
    {
        if (m_ReceivingDtmfDigit == true)
        {
            if (dtmfPacket.Eflag == true)
            {
                DtmfDigitReceived?.Invoke(dtmfPacket.Event);
                m_ReceivingDtmfDigit = false;
            }
        }
        else
        {   // The packet is the first of a new DTMF event or a duplicate RTP packet in the event.
            if (dtmfPacket.Eflag == false)
                m_ReceivingDtmfDigit = true;
        }
    }

    /// <summary>
    /// Sets the delegate function to call to process received audio packets.
    /// </summary>
    /// <param name="destinationHandler">Audio samples destination handler function. If null then
    /// received RTP packets will be ignored but DTMF events will still be processed.</param>
    public void SetDestionationHandler(AudioDestinationDelegate? destinationHandler)
    {
        DestinationHandler = destinationHandler;
    }

}
