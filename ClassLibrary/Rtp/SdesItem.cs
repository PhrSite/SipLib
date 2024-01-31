/////////////////////////////////////////////////////////////////////////////////////
//  File:   SdesItem.cs                                             28 Sep 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

using System.Text;

namespace SipLib.Rtp;

/// <summary>
/// Enumeration for identifying a SDES chunk type. See Section 6.5 of RFC 3550.
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
/// Class for parsing and building SDES items. See Section 6.5 of RFC 3550.
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

    private SdesItem()
    {
    }

    /// <summary>
    /// Parses a byte array into an SdesItem
    /// </summary>
    /// <param name="Bytes">Input byte array containing the SdesItem data to parse</param>
    /// <param name="StartIdx">Starting index in the input array</param>
    /// <returns>Returns an SdesItem if successful or null if an error occurred</returns>
    public static SdesItem Parse(byte[] Bytes, int StartIdx)
    {
        SdesItem Si = new SdesItem();
        if (Bytes.Length - StartIdx < MinSdesItemLength)
            return null;		// Error the array is not long enough

        Si.m_SdesItemType = (SdesItemType)Bytes[StartIdx + SdesTypeIdx];
        Si.m_Length = Bytes[StartIdx + PayloadLengthIdx];
        if (Si.m_Length > 0)
        {
            Si.m_Payload = new byte[Si.m_Length];
            Array.ConstrainedCopy(Bytes, StartIdx + PayloadIdx, Si.m_Payload, 0, Si.m_Length);
        }

        return Si;
    }

    /// <summary>
    /// Constructs a new SdesItem for sending as part of a SDES chunk.
    /// </summary>
    /// <param name="Sit">Type of SDES item.</param>
    /// <param name="strPayload">String containing the payload.</param>
    public SdesItem(SdesItemType Sit, string strPayload)
    {
        m_SdesItemType = Sit;
        m_Payload = Encoding.UTF8.GetBytes(strPayload);
        m_Length = m_Payload.Length;
        if (m_Length > byte.MaxValue)
            m_Length = byte.MaxValue;
    }

    /// <summary>
    /// Gets the SDES item type.
    /// </summary>
    /// <value></value>
    public SdesItemType ItemType
    {
        get { return m_SdesItemType; }
    }

    /// <summary>
    /// Gets the string value of the payload. Returns null if there is no payload. </summary>
    /// <value></value>
    public string Payload
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
    /// Gets the total number of bytes in this SDES item. This includes the SDES item byte byte, the length
    /// byte and the payload bytes.
    /// </summary>
    /// <value></value>
    public int SdesItemLength
    {
        get { return m_Length + MinSdesItemLength; }
    }

    /// <summary>
    /// Converts this SdesItem to a byte array for loading it into a SDES chunk.
    /// </summary>
    /// <returns>Returns the byte array for this object. Returns null if there is no payload.</returns>
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
