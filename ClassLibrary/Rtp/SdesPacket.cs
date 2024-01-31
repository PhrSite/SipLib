/////////////////////////////////////////////////////////////////////////////////////
//  File:   SdesPacket.cs                                           29 Sep 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Rtp;

/// <summary>
/// Class for parsing and building SDES (Source Description) RTCP packets. See Section 6.5 of RFC 3550.
/// </summary>
public class SdesPacket
{
    private RtcpHeader m_Header = null;
    private List<SdesChunk> m_SdesChunks = new List<SdesChunk>();

    private SdesPacket()
    {
    }

    /// <summary>
    /// Parses the data in a byte array from a RTCP packet received from the network into a SdesPacket
    /// object.
    /// </summary>
    /// <param name="Bytes">Input byte array</param>
    /// <param name="StartIdx">Stating index of the SdesPacket data in the input byte array</param>
    /// <returns>Returns a SdesPacket object if successful or null if an error occurred</returns>
    public static SdesPacket Parse(byte[] Bytes, int StartIdx)
    {
        SdesPacket Sp = new SdesPacket();
        if ((Bytes.Length - StartIdx) < RtcpHeader.HeaderLength)
            return null; // Error: Not enough bytes in the input array for a header

        Sp.m_Header = new RtcpHeader(Bytes, StartIdx);

        int CurIdx = StartIdx + RtcpHeader.HeaderLength;
        SdesChunk Sc = null;
        int NumChunks = Sp.m_Header.Count;
        int Count = 0;

        try
        {
            while (CurIdx < Bytes.Length && Count < NumChunks)
            {
                Sc = SdesChunk.Parse(Bytes, CurIdx);
                Sp.m_SdesChunks.Add(Sc);

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
            return null;
        }

        return Sp;
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
    /// <value></value>
    public RtcpHeader Header
    {
        get { return m_Header; }
    }

    /// <summary>
    /// Gets the list of SDES chunks in the packet.
    /// </summary>
    /// <value></value>
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
