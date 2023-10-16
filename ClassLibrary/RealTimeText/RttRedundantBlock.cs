/////////////////////////////////////////////////////////////////////////////////////
//  File:   RttRedundantBlock.cs                                    12 Oct 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

using SipLib.Rtp;

namespace SipLib.RealTimeText;

/// <summary>
/// Class for representing redundant RTT blocks in an RTP packet. See RFC 4103.
/// </summary>
public class RttRedundantBlock
{
    /// <summary>
    /// Mask for detecting the marker bit in a redundant text block.
    /// </summary>
    public const byte RTT_MARKER_MASK = 0x80;

    /// <summary>
    /// Length of a redundant block header.
    /// </summary>
    public const int RED_HEADER_LENGTH = 4;

    private const int PayloadTypeIndex = 0;
    private const int TimeOffsetIndex = 1;
    private const int BlockLenIndex = 2;
    private const ushort BlockLengthMask = 0x03ff;

    /// <summary>
    /// Gets or sets the payload type.
    /// </summary>
    public byte T140PayloadType { get; set; }

    /// <summary>
    /// Gets or sets the timestamp offset value.
    /// </summary>
    public ushort TimeOffset { get; set; }

    /// <summary>
    /// Gets or sets the length of the redundant block.
    /// </summary>
    public ushort BlockLength { get; set; }

    /// <summary>
    /// Default constructor
    /// </summary>
    public RttRedundantBlock()
    {
    }

    /// <summary>
    /// Constructs a new RttRedundantBlock object from 4 consecutive bytes in a byte array starting at
    /// a specified index. Use this constructor when parsing an RTP packet containing redundant RTT
    /// characters.
    /// </summary>
    /// <param name="pHeader">Byte array containing the RttRedundantBlock</param>
    /// <param name="StartIndex">Index to the header of the redunant block.</param>
    public RttRedundantBlock(byte[] pHeader, int StartIndex)
    {
        T140PayloadType = Convert.ToByte(pHeader[StartIndex + PayloadTypeIndex] & ~RTT_MARKER_MASK);
        // The timestamp offset is only 14 bits long.
        TimeOffset = Convert.ToUInt16(RtpUtils.GetWord(pHeader, StartIndex + TimeOffsetIndex) >> 2);
        BlockLength = Convert.ToUInt16(RtpUtils.GetWord(pHeader, StartIndex + BlockLenIndex) & BlockLengthMask);
    }

    /// <summary>
    /// Constructs a new RttRedundantBlock object. Use this constructor when building a new RttRedundantBlock
    /// to send.
    /// </summary>
    /// <param name="Pt">T.140 payload type. This is the value for the primary block as set in the SDP.</param>
    /// <param name="TmOffset">Time offset in milliseconds from the beginning of the text session.</param>
    /// <param name="Payload"></param>
    public RttRedundantBlock(byte Pt, ushort TmOffset, byte[] Payload)
    {
        T140PayloadType = Pt;
        TimeOffset = TmOffset;
        BlockLength = Convert.ToUInt16(Payload.Length & 0xffff);
        m_PayloadBytes = Payload;
    }

    private byte[] m_PayloadBytes = null;

    /// <summary>
    /// Returns a byte array containing the formatted redundant payload header. Call this method after 
    /// setting the T140PayloadType, BlockLength and TimeOffset properties.
    /// </summary>
    /// <returns>Returns the formatted 4-byte redundant block header.</returns>
    public byte[] GetRedundantPayloadHeader()
    {
        byte[] HeaderBytes = new byte[RED_HEADER_LENGTH];
        HeaderBytes[PayloadTypeIndex] = Convert.ToByte(T140PayloadType | RTT_MARKER_MASK);
        uint Temp = Convert.ToUInt32((TimeOffset << 10) + (BlockLength & BlockLengthMask));
        RtpUtils.Set3Bytes(HeaderBytes, TimeOffsetIndex, Temp);
        return HeaderBytes;
    }

    /// <summary>
    /// Gets or sets the payload bytes. The setter also sets the BlockLength property
    /// </summary>
    public byte[] PayloadBytes
    {
        get { return m_PayloadBytes; } 
        set
        {
            m_PayloadBytes = value;
            if (m_PayloadBytes != null)
                BlockLength = Convert.ToUInt16(m_PayloadBytes.Length & 0xffff);
            else
                BlockLength = 0;
        }
    }
}
