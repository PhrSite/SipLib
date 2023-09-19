/////////////////////////////////////////////////////////////////////////////////////
//  File:   SdesPacket.cs                                           19 Sep 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Rtp;

/// <summary>
/// Class for parsing and building SDES (Source Description) RTCP packets.
/// </summary>
public class SdesPacket
{
    private RtcpHeader m_Header = null;
    private List<SdesChunk> m_SdesChunks = new List<SdesChunk>();

    /// <summary>
    /// Constructs a new SdesPacket object from a RTCP packet that was received from the network.
    /// </summary>
    /// <param name="Bytes">Byte array containing the RTCP packet.</param>
    /// <param name="StartIdx">Index in Bytes of the first byte of the RTCP header for the SDES RTCP type
    /// packet.</param>
    public SdesPacket(byte[] Bytes, int StartIdx)
    {
        if ((Bytes.Length - StartIdx) < RtcpHeader.HeaderLength)
            return; // Error: Not enough bytes in the input array for a header

        m_Header = new RtcpHeader(Bytes, StartIdx);

        int CurIdx = StartIdx + RtcpHeader.HeaderLength;
        SdesChunk Sc = null;
        int NumChunks = m_Header.Count;
        int Count = 0;

        try
        {
            while (CurIdx < Bytes.Length && Count < NumChunks)
            {
                Sc = new SdesChunk(Bytes, CurIdx);
                m_SdesChunks.Add(Sc);

                // Each SDES chunk in a SDES packet must start on a 4 byte index boundary.
                int Len = Sc.TotalLength;
                if (Len % 4 == 0)
                    // Account for the terminating 0 byte
                    Len += 1;       
                if (Len % 4 != 0)
                    // Account for the added 0 padding bytes to make the next chunk start on a 32 bit (4 byte)
                    // boundary
                    Len += 4 - (Len % 4);
                CurIdx += Len;

                Count += 1;
            } // end while
        }
        catch (Exception)
        {

        }
    }

    /// <summary>
    /// Constructs a new SDES RTCP packet for sending.
    /// </summary>
    /// <param name="Scs">List of SdesChunk objects. The list should contain at least 1 SdesChunk object.</param>
    public SdesPacket(List<SdesChunk> Scs)
    {
        m_Header = new RtcpHeader();
        m_Header.Count = Scs.Count;
        m_Header.PacketType = RtcpPacketType.SourceDescriptionReport;
        m_SdesChunks = Scs;
    }

    /// <summary>
    /// Constructs a new SdesPacke for sending given a SSRC and a SdesItem.
    /// </summary>
    /// <param name="SSRC">SSRC that identifies the media source.</param>
    /// <param name="Sdi">SDES item to send.</param>
    public SdesPacket(uint SSRC, SdesItem Sdi)
    {
        m_Header = new RtcpHeader();
        m_Header.Count = 1;
        m_Header.PacketType = RtcpPacketType.SourceDescriptionReport;
        m_SdesChunks.Add(new SdesChunk(SSRC, Sdi));
    }

    /// <summary>
    /// Gets the RTCP header.
    /// </summary>
    public RtcpHeader Header
    {
        get { return m_Header; }
    }

    /// <summary>
    /// Gets the list of SDES chunks in the packet.
    /// </summary>
    public List<SdesChunk> Chunks
    {
        get { return m_SdesChunks; }
    }

    /// <summary>
    /// Converts this SdesPacket object to a byte array for sending it over the network.
    /// </summary>
    /// <returns>Returns this object converted to a byte array.</returns>
    public byte[] ToByteArray()
    {
        byte[] RetVal = null;

        // Build the byte array for each SDES chunk and figure out how
        // many bytes are required.
        byte[] Ary = null;
        int TotalLen = 0;
        List<byte[]> AryList = new List<byte[]>();

        foreach (SdesChunk Sc in m_SdesChunks)
        {
            Ary = Sc.ToByteArray();
            TotalLen += Ary.Length;
            AryList.Add(Ary);
        }

        TotalLen += RtcpHeader.HeaderLength;
        RetVal = new byte[TotalLen];
        m_Header.Length = (ushort)(TotalLen / 4 - 1);
        m_Header.Count = m_SdesChunks.Count;
        m_Header.LoadBytes(RetVal, 0);
        int CurIdx = RtcpHeader.HeaderLength;

        foreach (byte[] SdesChunkAry in AryList)
        {
            Array.ConstrainedCopy(SdesChunkAry, 0, RetVal, CurIdx, SdesChunkAry.
                Length);
            CurIdx += SdesChunkAry.Length;
        }

        return RetVal;
    }
}
