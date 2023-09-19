/////////////////////////////////////////////////////////////////////////////////////
//  File:   SdesItem.cs                                             19 Sep 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

using System.Text;

namespace SipLib.Rtp;

/// <summary>
/// Enumeration for identifying a SDES chunk type.
/// </summary>
public enum SdesItemType
{
    /// <summary>
    /// Canonical End-Point Identifier SDES item
    /// </summary>
    CNAME = 1,
    /// <summary>
    /// User name SDES item
    /// </summary>
    NAME = 2,
    /// <summary>
    /// Electronic Mail Address SDES Item
    /// </summary>
    EMAIL = 3,
    /// <summary>
    /// Phone Number SDES Item
    /// </summary>
    PHONE = 4,
    /// <summary>
    /// Geographic User Location SDES Item
    /// </summary>
    LOC = 5,
    /// <summary>
    /// Appplication or Tool Name SDES Item
    /// </summary>
    TOOL = 6,
    /// <summary>
    /// Notice/Status SDES Item.
    /// </summary>
    NOTE = 7,
    /// <summary>
    /// Private Extensions SDES Item
    /// </summary>
    PRIV = 8,
}

/// <summary>
/// Class for parsing and building SDES items. Each SDES item 
/// </summary>
public class SdesItem
{
    private SdesItemType m_SdesItemType;
    private int m_Length = 0;
    private byte[] m_Payload = null;

    private const int MinSdesItemLength = 2;
    private const int SdesTypeIdx = 0;
    private const int PayloadLengthIdx = 1;
    private const int PayloadIdx = 2;

    /// <summary>
    /// Constructs a SdesItem object from a byte array. Use this constructor when parsing a SDES item that was
    /// received as part of an SDES chunk.
    /// </summary>
    /// <param name="Bytes">Byte array containing the SDES item.</param>
    /// <param name="StartIdx">Index of the first byte of the SDES item.</param>
    public SdesItem(byte[] Bytes, int StartIdx)
    {
        if (Bytes.Length - StartIdx < MinSdesItemLength)
            return;		// Error the array is not long enough

        m_SdesItemType = (SdesItemType)Bytes[StartIdx + SdesTypeIdx];
        m_Length = Bytes[StartIdx + PayloadLengthIdx];
        if (m_Length > 0)
        {
            m_Payload = new byte[m_Length];
            Array.ConstrainedCopy(Bytes, StartIdx + PayloadIdx, m_Payload, 0, m_Length);
        }
    }

    /// <summary>
    /// Constructs a new SdesItem for sending as part of a SDES chunk.
    /// </summary>
    /// <param name="Sit">Type of SDES item.</param>
    /// <param name="strPayload">String containing the payload.</param>
    public SdesItem(SdesItemType Sit, String strPayload)
    {
        m_SdesItemType = Sit;
        m_Payload = Encoding.UTF8.GetBytes(strPayload);
        m_Length = m_Payload.Length;
        if (m_Length > Byte.MaxValue)
            m_Length = Byte.MaxValue;
    }

    /// <summary>
    /// Gets the SDES item type.
    /// </summary>
    public SdesItemType ItemType
    {
        get { return m_SdesItemType; }
    }

    /// <summary>
    /// Gets the string value of the payload. Returns null if there is no 
    /// payload. </summary>
    public String Payload
    {
        get
        {
            if (m_Length == 0 || m_Payload == null)
                return null;
            else
                return Encoding.UTF8.GetString(m_Payload);
        }
    }

    /// <summary>
    /// Gets the total number of bytes in this SDES item. This includes the
    /// SDES item byte byte, the length byte and the payload bytes.
    /// </summary>
    public Int32 SdesItemLength
    {
        get { return m_Length + MinSdesItemLength; }
    }

    /// <summary>
    /// Converts this SdesItem to a byte array for loading it into a SDES chunk.
    /// </summary>
    /// <returns>Returns the byte array for this object. Returns null if there
    /// is no payload.</returns>
    public byte[] ToByteArray()
    {
        if (m_Payload == null || m_Length == 0)
            return null;

        int RequiredLen = m_Length + MinSdesItemLength;
        byte[] RetVal = new byte[RequiredLen];

        RetVal[SdesTypeIdx] = Convert.ToByte(m_SdesItemType);
        RetVal[PayloadLengthIdx] = Convert.ToByte(m_Length & 0xff);
        Array.ConstrainedCopy(m_Payload, 0, RetVal, PayloadIdx, m_Length);
        return RetVal;
    }
}
