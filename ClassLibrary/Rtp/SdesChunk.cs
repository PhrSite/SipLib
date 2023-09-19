/////////////////////////////////////////////////////////////////////////////////////
//  File:   SdesChunk.cs                                            19 Sep 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Rtp;

/// <summary>
/// Class for parsing and building a SDES chunk for an RTCP SDES packet.
/// </summary>
public class SdesChunk
{
    private UInt32 m_Ssrc;
    /// <summary>
    /// Length of the SSRC field plus any SDES items in this chunk.
    /// </summary>
    private int m_Length = 0;

    private List<SdesItem> m_SdesItems = new List<SdesItem>();

    /// <summary>
    /// The minimun length is the length of the SSRC (4) + the length of the chunk type field (1).
    /// </summary>
    private const int MinSdesChunkLength = 5;

    /// <summary>
    /// Constructs a new SdesChunk item from the bytes array containing a RTCP packet. Use this constructor
    /// when parsing a RTCP packet.
    /// </summary>
    /// <param name="Bytes">Bytes containing the RTCP packet.</param>
    /// <param name="StartIdx">Index in Bytes of the first byte of the SDES chunk.</param>
    public SdesChunk(byte[] Bytes, int StartIdx)
    {
        if (Bytes.Length - StartIdx < MinSdesChunkLength)
            return;	

        m_Ssrc = RtpUtils.GetDWord(Bytes, StartIdx);

        // Get the list of SDES items that are in this SDES chunk.
        int CurIdx = StartIdx + 4;		// Get past the SSRC field.
        m_Length = 4;					// Length of the SSRC field.
        SdesItem Si;
        try
        {
            while (CurIdx < Bytes.Length)
            {
                if (Bytes[CurIdx] == 0)
                    break;      // An item type of 0 indicates completion.

                Si = new SdesItem(Bytes, CurIdx);
                m_SdesItems.Add(Si);
                m_Length += Si.SdesItemLength;
                CurIdx += Si.SdesItemLength;
            }
        }
        catch (Exception)
        {

        }
    }

    /// <summary>
    /// Constructs a new SdesChunk for sending as part of a RTCP packet.
    /// </summary>
    /// <param name="Ssrc">SSRC that identifies the media source.</param>
    /// <param name="Items">List of SDES items. The list should include at least one SDES item.</param>
    public SdesChunk(UInt32 Ssrc, List<SdesItem> Items)
    {
        m_Ssrc = Ssrc;
        m_SdesItems = Items;
    }

    /// <summary>
    /// Constructs a new SdesChunk for sending as part of a RTCP packet given the SSRC and a SdesItem.
    /// </summary>
    /// <param name="Ssrc">SSRC that identifies the media source.</param>
    /// <param name="Sdi">SdesItem object to add to the list of items.</param>
    public SdesChunk(uint Ssrc, SdesItem Sdi)
    {
        m_Ssrc = Ssrc;
        m_SdesItems.Add(Sdi);
    }

    /// <summary>
    /// Gets the total length of the SDES chunk that was received and parsed. Do not use this property if
    /// constructing a SdesChunk item to send.
    /// </summary>
    public Int32 TotalLength
    {
        get { return m_Length; }
    }

    /// <summary>
    /// Gets or sets the synchronization source (SSRC) of this chunk.
    /// </summary>
    public uint SSRC
    {
        get { return m_Ssrc; }
        set { m_Ssrc = value; }
    }

    /// <summary>
    /// Gets the list of SDES items in this chunk.
    /// </summary>
    public List<SdesItem> Items
    {
        get { return m_SdesItems; }
    }

    /// <summary>
    /// Converts this object to a byte array for sending it.
    /// </summary>
    /// <returns>Returns a byte array containing the binary version of this object. Returns null if there is
    /// no payload or the SDES chunk is not valid.</returns>
    public byte[] ToByteArray()
    {
        // Calculate the length required by each SDES item.
        int TotalLength = 4;
        byte[] CurItem = null;
        List<byte[]> Arrays = new List<byte[]>();

        foreach (SdesItem Si in m_SdesItems)
        {
            CurItem = Si.ToByteArray();
            TotalLength += CurItem.Length;
            Arrays.Add(CurItem);
        } // end foreach

        // Leave room for the null terminator that indicates the end of the
        // SDES items.
        TotalLength += 1;

        // Make sure that the total length is divisible by 4 because each
        // SDES chunk must start on a 4 byte boundary in a SDES packet.
        if (TotalLength % 4 != 0)
            TotalLength += (4 - (TotalLength % 4));
        byte[] RetVal = new byte[TotalLength];
        
        RtpUtils.SetDWord(RetVal, 0, m_Ssrc);
        int CurIdx = 4;		// Get past the SSRC field.

        foreach (byte[] Ar in Arrays)
        {
            Array.ConstrainedCopy(Ar, 0, RetVal, CurIdx, Ar.Length);
            CurIdx += Ar.Length;
        }

        RetVal[CurIdx] = 0;		// Indicates the last SDES item.
        return RetVal;
    }
}
