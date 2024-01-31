/////////////////////////////////////////////////////////////////////////////////////
//  File:   ByteRangeHeader.cs                                      22 Jul 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Msrp;

/// <summary>
/// Class for parsing and processing the Byte-Range header of a MSRP message. See RFC 4975.
/// </summary>
public class ByteRangeHeader
{
    /// <summary>
    /// Starting byte number.
    /// </summary>
    /// <value></value>
    public int Start = 1;
    /// <summary>
    /// Ending byte number. A value of -1 indicates that the ending byte number is not specified --
    /// its string value is "*".
    /// </summary>
    /// <value></value>
    public int End = -1;
    /// <summary>
    /// Total number of bytes in the MSRP message. A value of -1 indicates that the total number of 
    /// bytes is not specified -- its string value "*".
    /// </summary>
    /// <value></value>
    public int Total = -1;

    /// <summary>
    /// Constructor
    /// </summary>
    public ByteRangeHeader()
    {
    }

    /// <summary>
    /// Parses a string into a ByteRangeHeader object.
    /// </summary>
    /// <param name="strValue">Input value of the Byte-Range header</param>
    /// <returns>Returns a new ByteRangeHeader object. Returns null if the input string is not a
    /// properly formatted Byte-Range header.
    /// </returns>
    public static ByteRangeHeader ParseByteRangeHeader(string strValue)
    {
        ByteRangeHeader header = new ByteRangeHeader();
        int DashIdx = strValue.IndexOf("-");
        int SlashIdx = strValue.IndexOf("/");
        if (DashIdx == -1 || SlashIdx == -1)
            return null;     // Error: Not properly formatted.

        string strStart = strValue.Substring(0, DashIdx);
        string strEnd = strValue.Substring(DashIdx + 1, SlashIdx - DashIdx - 1);
        string strTotal = strValue.Substring(SlashIdx + 1);

        // Member variables will be left at their default values if parsing fails
        if (int.TryParse(strStart, out header.Start) == false)
            return null;    // Error: The Start field must be an integer

        if (strEnd == "*")
            header.End = -1;
        else if (int.TryParse(strEnd, out header.End) == false)
            return null;

        if (strTotal == "*")
            header.Total = -1;
        else if (int.TryParse(strTotal, out header.Total) == false)
            return null;

        return header;
    }

    /// <summary>
    /// Builds a Byte-Range header value as a string
    /// </summary>
    /// <returns>Returns the fully formatted Byte-Range header.</returns>
    public override string ToString()
    {
        return string.Format("{0}-{1}/{2}", Start, (End == -1) ? "*" : End.ToString(),
            (Total == -1) ? "*" : Total.ToString());
    }
}
