/////////////////////////////////////////////////////////////////////////////////////
//  File:   AudioDestination.cs                                     6 Mar 24 PHR
/////////////////////////////////////////////////////////////////////////////////////

using SipLib.Rtp;
using SipLib.Sdp;

namespace SipLib.Media;

/// <summary>
/// Delegate function for the audio destination handler function that the AudioDestination class will call
/// to process decoded audio packets.
/// </summary>
/// <param name="PcmSamples"></param>
public delegate void AudioDestinationDelegate(short[] PcmSamples);

/// <summary>
/// This class receives audio RTP packets from an RtpChannel, decodes them and sends them to an audio destination
/// by calling a delegate function that processes the received audio samples.
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
    /// Constructor
    /// </summary>
    /// <param name="AnsweredMediaDescription">The MediaDescription object that was sent as the answer to
    /// the offered MediaDescription. This object contains the negotiated media type and codec type.</param>
    /// <param name="Decoder">IAudioDecoder to use to decode the received audio samples</param>
    /// <param name="rtpChannel">RtpChannel to receive audio RTP packets on.</param>
    /// <param name="destinationHandler">Function to call to handle the decoded audio packets. If this is
    /// null then RTP packets are ignored.</param>
    /// <param name="DestinationSampleRate">Sample rate expected by the destination handler</param>
    public AudioDestination(MediaDescription AnsweredMediaDescription, IAudioDecoder Decoder, RtpChannel rtpChannel,
        AudioDestinationDelegate? destinationHandler, int DestinationSampleRate)
    {
        m_AudioDecoder = Decoder;
        m_RtpChannel = rtpChannel;
        DestinationHandler = destinationHandler;
        m_DecoderSampleRate = DestinationSampleRate;
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

            // TODO: Fix the sample rate if necessary

            DestinationHandler?.Invoke(decodedPacket);

        }
        else if (rtpPacket.PayloadType == m_TelephoneEventPayloadType)
        {
            
        }
    }

    /// <summary>
    /// Sets the delegate function to call to process received audio packets.
    /// </summary>
    /// <param name="destinationHandler">Audio samples destination handler function. If null then
    /// received RTP packets will be ignored.</param>
    public void SetDestionationHandler(AudioDestinationDelegate? destinationHandler)
    {
        DestinationHandler = destinationHandler;
    }

}
