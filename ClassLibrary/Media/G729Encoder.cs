/////////////////////////////////////////////////////////////////////////////////////
//  File:   G729Encoder.cs                                          24 Apr 25 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Media;

/// <summary>
/// Encodes 16-bit PCM audio samples into G.729 RTP payload bytes.
/// </summary>
public class G729Encoder : Ld8k, IAudioEncoder
{

    // Initialization of the coder.
    private byte[] _leftover = new byte[0];

    // Initialize the Ld8k Coder
    private readonly CodLd8k codLd8k = new CodLd8k();

    // Initialize the PreProc
    private readonly PreProc preProc = new PreProc();

    // Initialize the transmitted parameter
    private readonly int[] prm = new int[PRM_SIZE];

    /// <summary>
    /// Constructor
    /// </summary>
    public G729Encoder()
    {
        preProc.init_pre_process();
        codLd8k.init_coder_ld8k(); /* Initialize the coder             */
    }

    private static void Fill<T>(T[] array, int start, int end, T value)
    {
        for (var i = start; i < end; i++)
            array[i] = value;
    }

    private void packetize(short[] serial, byte[] outFrame, int outFrameOffset)
    {
        Fill(outFrame, outFrameOffset, outFrameOffset + L_FRAME / 8, (byte)0);

        for (var s = 0; s < L_FRAME; s++)
            if (BIT_1 == serial[2 + s])
            {
                var o = outFrameOffset + s / 8;
                int out_ = outFrame[o];

                out_ |= 1 << (7 - s % 8);
                outFrame[o] = (byte)(out_ & 0xFF);
            }
    }

    /**
     * Process <code>L_FRAME</code> short of speech.
     *
     * @param sp16      input : speach short array
     * @param serial    output : serial array encoded in bits_ld8k
     */

    private void ProcessPacket(short[] sp16, short[] serial)
    {
        var new_speech = codLd8k.new_speech; /* Pointer to new speech data   */
        var new_speech_offset = codLd8k.new_speech_offset;

        for (var i = 0; i < L_FRAME; i++)
            new_speech[new_speech_offset + i] = sp16[i];

        preProc.pre_process(new_speech, new_speech_offset, L_FRAME);
        codLd8k.coder_ld8k(prm);
        Bits.prm2bits_ld8k(prm, serial);

    }

    /**
     * Usage : coder  speech_file  bitstream_file
     *
     * Format for speech_file:
     *  Speech is read form a binary file of 16 bits data.
     *
     * Format for bitstream_file:
     *   One word (2-bytes) to indicate erasure.
     *   One word (2 bytes) to indicate bit rate
     *   80 words (2-bytes) containing 80 bits.
     *
     * @param args speech_file  bitstream_file
     * @throws java.io.IOException
     */
    private byte[] Process(byte[] speech)
    {
        var sp16 = new short[L_FRAME]; /* Buffer to read 16 bits speech */
        var serial = new short[SERIAL_SIZE]; /* Output bit stream buffer      */
        var packet = new byte[L_FRAME / 8];
        var output = new MemoryStream();
        var buffer = new MemoryStream();

        buffer.Write(_leftover, 0, _leftover.Length);
        buffer.Write(speech, 0, speech.Length);
        var input = buffer.ToArray();

        /*-------------------------------------------------------------------------*
         * Loop for every analysis/transmission frame.                             *
         * -New L_FRAME data are read. (L_FRAME = number of speech data per frame) *
         * -Conversion of the speech data from 16 bit integer to real              *
         * -Call cod_ld8k to encode the speech.                                    *
         * -The compressed serial output stream is written to a file.              *
         * -The synthesis speech is written to a file                              *
         *-------------------------------------------------------------------------*
         */

        var frame = 0;
        try
        {
            // Iterate over each frame 
            int i;
            for (i = 0; i <= input.Length - L_FRAME * 2 /* must have a complete frame left */; i += L_FRAME * 2)
            {
                frame++;
                Buffer.BlockCopy(input, i, sp16, 0, L_FRAME * 2);
                ProcessPacket(sp16, serial);
                packetize(serial, packet, 0);
                output.Write(packet, 0, packet.Length);
            }

            _leftover = new byte[input.Length - i];
            Array.Copy(input, i, _leftover, 0, _leftover.Length);
        }
        catch (Exception)
        {
            // No logging as we could get huge log files if any issues arises decoding
        }

        return output.ToArray();
    }

    /// <summary>
    /// Gets the RTP clock rate in samples/second
    /// </summary>
    public int ClockRate
    {
        get { return 8000; }
    }

    /// <summary>
    /// Gets the input audio sample rate in samples/second
    /// </summary>
    public int SampleRate
    {
        get { return 8000; }
    }

    /// <summary>
    /// Amount to increment the RTP packet Time Stamp field by for each new packet.
    /// </summary>
    public uint TimeStampIncrement
    {
        get { return 160; }
    }

    /// <summary>
    /// Closes the encoder so that it can release any memory or resources it has been using.
    /// </summary>
    public void CloseEncoder()
    {
    }

    /// <summary>
    /// Encodes linear 16-bit PCM samples into G.729 encoded bytes to send as the payload of an RTP packet.
    /// </summary>
    /// <param name="InputSamples">Input linear 16-bit PCM samples</param>
    public byte[] Encode(short[] InputSamples)
    {
        byte[] pcmBytes = new byte[InputSamples.Length * sizeof(short)];
        Buffer.BlockCopy(InputSamples, 0, pcmBytes, 0, pcmBytes.Length);
        byte[] encodedBytes = Process(pcmBytes);
        return encodedBytes;
    }
}
