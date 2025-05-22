/////////////////////////////////////////////////////////////////////////////////////
//  File:   AmrWbDecoder.cs                                         24 Sep 24 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Media;
using AmrWbLib;
using SipLib.Logging;

/// <summary>
/// Class for decoding AMR-WB encoded audio data into 16-bit linear PCM samples
/// </summary>
public class AmrWbDecoder : IAudioDecoder
{
    private AmrWb? m_Decoder;

    /// <summary>
    /// Constructor. Initializes the decoder
    /// </summary>
    public AmrWbDecoder()
    {
        m_Decoder = AmrWb.CreateAsDecoder();
    }

    /// <summary>
    /// Gets the sample rate in samples per second
    /// </summary>
    public int SampleRate
    {
        get { return 16000; }
    }

    /// <summary>
    /// Closes the decoder
    /// </summary>
    public void CloseDecoder()
    {
        m_Decoder = null;
    }

    private const int PACKET_HEADER_INDEX = 0;
    private const int FIRST_TOC_INDEX = 1;
    private const byte F_BIT_MASK = 0x80;
    private const int MODE_SHIFT = 3;
    private const byte MODE_MASK = 0x0f;

    // Limit the number of decoding errors that are logged so as not to fill up the log.
    private const int MAX_DECODE_LOG_ERRORS = 20;
    private int m_MaxDecodeLogErrors;

    /// <summary>
    /// Decodes the AMR-WB encoded data from the payload of an RTP packet into linear 16-bit PCM samples. Sections 
    /// 4.2 and 4.3 of RFC 4867 describe the format of the RTP packet payload for the AMR-WB codec.
    /// </summary>
    /// <param name="EncodedData">AMR-WB encoded input data.</param>
    /// <returns></returns>
    public short[] Decode(byte[] EncodedData)
    {
        if (EncodedData.Length < 2)
            return null!;

        List<short> SamplesList = new List<short>();

        try
        {
            // Determine the number of voice frames in the EncodedData from an RTP packet.
            // There may be more than 1.
            int TocIndex = FIRST_TOC_INDEX;
            bool Done = false;

            // Table of Contents byte
            byte Toc;

            List<byte> TocList = new List<byte>();
            byte Header = EncodedData[PACKET_HEADER_INDEX];

            // If the F bit in the TOC is set, then there are more voice frames to follow this TOC. The
            // first TOC byte with the F bit = 0 is the last TOC.
            while (Done == false)
            {
                Toc = EncodedData[TocIndex++];
                TocList.Add(Toc);
                if ((byte) (Toc & F_BIT_MASK) == 0)
                    Done = true;
            }

            short[] samples;
            // Simple case of 1 voice frame so just decode it and return.
            if (TocList.Count == 1)
            {
                samples = m_Decoder!.DecodePacketPayload(EncodedData);
                return samples!;
            }

            // The encoded AMR-WB data in the RTP packet payload contains multiple voice frames but
            // the decoder can only handle a single voice frame at a time. Split the input encoded
            // data into packets with each packet containing the encoded data for a single voice frame.
            short Mode;
            int PackedSize;
            int SourceIndex = TocList.Count + 1;   // Skip the Header and the TOCs
            int DestIndex;

            for (int i = 0; i < TocList.Count; i++)
            {
                Toc = TocList[i];
                Mode = (short)((Toc >> MODE_SHIFT) & MODE_MASK);
                PackedSize = m_Decoder!.GetPacketSize(Mode);
                
                byte[] NewEncodedData = new byte[PackedSize + 2];
                NewEncodedData[PACKET_HEADER_INDEX] = Header;
                NewEncodedData[FIRST_TOC_INDEX] = (byte)(Toc & ~F_BIT_MASK);    // Clear the F bit
                if (PackedSize > 0)
                {
                    DestIndex = 2;
                    for (int k = 0; k < PackedSize; k++)
                        NewEncodedData[DestIndex++] = EncodedData[SourceIndex++];
                }

                samples = m_Decoder!.DecodePacketPayload(NewEncodedData);
                if (samples != null && samples.Length > 0)
                {
                    for (int j = 0; j < samples.Length; j++)
                        SamplesList.Add(samples[j]);
                }
            }
        }
        catch (Exception e)
        {
            if (m_MaxDecodeLogErrors < MAX_DECODE_LOG_ERRORS)
            {
                SipLogger.LogError(e, "Unexpected error occurred while processing an RTP packet " +
                    "containing multiple AMR-WB voice frames.");
                m_MaxDecodeLogErrors += 1;
            }
        }

        return SamplesList.ToArray();
    }
}
