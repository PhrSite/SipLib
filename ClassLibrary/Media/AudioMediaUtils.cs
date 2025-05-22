/////////////////////////////////////////////////////////////////////////////////////
//  File:   AudioMediaUtils.cs                                      23 Apr 25 PHR
/////////////////////////////////////////////////////////////////////////////////////

using SipLib.Sdp;

namespace SipLib.Media;

/// <summary>
/// Utility function relating to audio media handling
/// </summary>
public static class AudioMediaUtils
{
    /// <summary>
    /// Creates an IAudioEncoder given the negotiated MediaDescription object.
    /// </summary>
    /// <param name="mediaDescription">Input negotiated MediaDescription</param>
    /// <returns>Returns the negotiated IAudioEncoder. Returns null if the encoder is unknown.</returns>
    public static IAudioEncoder? GetAudioEncoder(MediaDescription mediaDescription)
    {
        IAudioEncoder? encoder = null;
        if (mediaDescription.RtpMapAttributes.Count > 0)
        {
            foreach (RtpMapAttribute Rma in mediaDescription.RtpMapAttributes)
            {
                switch (Rma.EncodingName!.ToUpper())
                {
                    case "PCMU":
                        encoder = new PcmuEncoder();
                        break;
                    case "PCMA":
                        encoder = new PcmaEncoder();
                        break;
                    case "G722":
                        encoder = new G722Encoder();
                        break;
                    case "G729":
                        encoder = new G729Encoder();
                        break;
                    case "AMR-WB":
                        encoder = new AmrWbEncoder();
                        break;
                }

                if (encoder != null)
                    break;      // Expect only one so pick the first one found
            }
        }

        if (encoder == null)
        {   // Unable to find the encoder to use from the list of rtpmap attributes so try
            // the well known payload (code) numbers
            foreach (int i in mediaDescription.PayloadTypes)
            {
                if (i == PCMU_DEFAULT_PAYLOAD_TYPE)
                    return new PcmuEncoder();
                else if (i == PCMA_DEFAULT_PAYLOAD_TYPE)
                    return new PcmaEncoder();
                else if (i == G722_DEFAULT_PAYLOAD_TYPE)
                    return new G722Encoder();
                else if (i == G729_DEFAULT_PAYLOAD_TYPE)
                    return new G729Encoder();
            }
        }

        return encoder;
    }

    /// <summary>
    /// Creates an IAudioDecoder object given the negotiated MediaDescription object.
    /// </summary>
    /// <param name="mediaDescription">Input negotiated MediaDescription</param>
    /// <returns>Returns the negotiated IAudioDecoder. Returns null if the decoder is unknown.</returns>
    public static IAudioDecoder? GetAudioDecoder(MediaDescription mediaDescription)
    {
        IAudioDecoder? decoder = null;
        if (mediaDescription.RtpMapAttributes.Count > 0)
        {
            foreach (RtpMapAttribute Rma in mediaDescription.RtpMapAttributes)
            {
                switch (Rma.EncodingName!.ToUpper())
                {
                    case "PCMU":
                        decoder = new PcmuDecoder();
                        break;
                    case "PCMA":
                        decoder = new PcmaDecoder();
                        break;
                    case "G722":
                        decoder = new G722Decoder();
                        break;
                    case "G729":
                        decoder = new G729Decoder();
                        break;
                    case "AMR-WB":
                        decoder = new AmrWbDecoder();
                        break;
                }

                if (decoder != null)
                    break;
            }

        }

        if (decoder == null)
        {   // Unable to find the decoder to use from the list of rtpmap attributes so try
            // the well known payload (code) numbers
            foreach (int i in mediaDescription.PayloadTypes)
            {
                if (i == PCMU_DEFAULT_PAYLOAD_TYPE)
                    return new PcmuDecoder();
                else if (i == PCMA_DEFAULT_PAYLOAD_TYPE)
                    return new PcmaDecoder();
                else if (i == G722_DEFAULT_PAYLOAD_TYPE)
                    return new G722Decoder();
                else if (i == G729_DEFAULT_PAYLOAD_TYPE)
                    return new G729Decoder();
            }
        }

        return decoder;
    }

    /// <summary>
    /// Default payload type number for the G.711 PCMU codec
    /// </summary>
    public const int PCMU_DEFAULT_PAYLOAD_TYPE = 0;

    /// <summary>
    /// Default payload type number for the G.711 PCMA codec
    /// </summary>
    public const int PCMA_DEFAULT_PAYLOAD_TYPE = 8;

    /// <summary>
    /// Default payload type number for the G.722 codec
    /// </summary>
    public const int G722_DEFAULT_PAYLOAD_TYPE = 9;

    /// <summary>
    /// Default payload type number for the G.729 codec
    /// </summary>
    public const int G729_DEFAULT_PAYLOAD_TYPE = 18;
}
