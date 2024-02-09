#region License
//-----------------------------------------------------------------------------
// Filename: TypeExtensions.cs
//
// Description: Helper methods.
//
// Author(s):
// Aaron Clauson
//
// History:
// ??	Aaron Clauson	Created.
// 21 Jan 2020  Aaron Clauson   Added HexStr and ParseHexStr (borrowed from
//                              Bitcoin Core source).
//
// License:
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//-----------------------------------------------------------------------------
#endregion

/////////////////////////////////////////////////////////////////////////////////////
//  Revised:    15 Nov 22 PHR -- Added from sipsorcery
/////////////////////////////////////////////////////////////////////////////////////

using System.Net;

namespace SipLib.Core;

/// <summary>
/// Type extensions used in this class library
/// </summary>
public static class TypeExtensions
{
    /// <summary>
    /// The Trim method only trims 0x0009, 0x000a, 0x000b, 0x000c, 0x000d, 0x0085, 0x2028, and 0x2029.
    /// This array adds in control characters.
    /// </summary>
    /// <value></value>
    public static readonly char[] WhiteSpaceChars = new char[] { (char)0x00, (char)0x01, (char)0x02, (char)0x03, (char)0x04, (char)0x05,
        (char)0x06, (char)0x07, (char)0x08, (char)0x09, (char)0x0a, (char)0x0b, (char)0x0c, (char)0x0d, (char)0x0e, (char)0x0f,
        (char)0x10, (char)0x11, (char)0x12, (char)0x13, (char)0x14, (char)0x15, (char)0x16, (char)0x17, (char)0x18, (char)0x19, (char)0x20,
        (char)0x1a, (char)0x1b, (char)0x1c, (char)0x1d, (char)0x1e, (char)0x1f, (char)0x7f, (char)0x85, (char)0x2028, (char)0x2029 };

    private static readonly sbyte[] _hexDigits =
        { -1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,
          -1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,
          -1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,
          0,1,2,3,4,5,6,7,8,9,-1,-1,-1,-1,-1,-1,
          -1,0xa,0xb,0xc,0xd,0xe,0xf,-1,-1,-1,-1,-1,-1,-1,-1,-1,
          -1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,
          -1,0xa,0xb,0xc,0xd,0xe,0xf,-1,-1,-1,-1,-1,-1,-1,-1,-1,
          -1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,
          -1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,
          -1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,
          -1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,
          -1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,
          -1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,
          -1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,
          -1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,
          -1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1, };

    private static readonly char[] hexmap = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 
        'b', 'c', 'd', 'e', 'f' };

    /// <summary>    
    /// Gets a value that indicates whether or not the collection is empty.    
    /// </summary>    
    public static bool IsNullOrBlank(this string s)
    {
        if (s == null || s.Trim(WhiteSpaceChars).Length == 0)
            return true;
        else
            return false;
    }

    /// <summary>
    /// Returns true if the string is not null or blank
    /// </summary>
    /// <param name="s">Input string</param>
    /// <returns></returns>
    public static bool NotNullOrBlank(this string s)
    {
        if (s == null || s.Trim(WhiteSpaceChars).Length == 0)
            return false;
        else
            return true;
    }

    /// <summary>
    /// Returns a slice from a string that is delimited by the first instance of a 
    /// start and end character. The delimiting characters are not included.
    /// 
    /// <code>
    /// "sip:127.0.0.1:5060;connid=1234".slice(':', ';') => "127.0.0.1:5060"
    /// </code>
    /// </summary>
    /// <param name="s">The input string to extract the slice from.</param>
    /// <param name="startDelimiter">The character to start the slice from. The first instance of 
    /// the character found is used.</param>
    /// <param name="endDelimeter">The character to end the slice on. The first instance of the 
    /// character found is used.</param>
    /// <returns>A slice of the input string or null if the slice is not possible.</returns>
    public static string? Slice(this string s, char startDelimiter, char endDelimeter)
    {
        if (String.IsNullOrEmpty(s))
            return null;
        else
        {
            int startPosn = s.IndexOf(startDelimiter);
            int endPosn = s.IndexOf(endDelimeter) - 1;

            if (endPosn > startPosn)
                return s.Substring(startPosn + 1, endPosn - startPosn);
            else
                return null;
        }
    }

    /// <summary>
    /// Returns true if the IPAddress is a private address
    /// </summary>
    /// <param name="address"></param>
    /// <returns></returns>
    public static bool IsPrivate(this IPAddress address)
    {
        return IPSocket.IsPrivateAddress(address.ToString());
    }

    /// <summary>
    /// Converts a byte array to a hexadecimal formatted string
    /// </summary>
    /// <param name="buffer">Input byte array to convert</param>
    /// <param name="separator">Separator character. May be null.</param>
    /// <returns>Returns the formatted hexadecimal string</returns>
    public static string HexStr(this byte[] buffer, char? separator = null)
    {
        return buffer.HexStr(buffer.Length, separator);
    }

    /// <summary>
    /// Converts a byte array to a hexadecimal formatted string
    /// </summary>
    /// <param name="buffer">Input byte array to convert</param>
    /// <param name="length">Number of bytes to process in the input array</param>
    /// <param name="separator">Separator character. May be null.</param>
    /// <returns>Returns the formatted hexadecimal string</returns>
    public static string HexStr(this byte[] buffer, int length, char? separator = null)
    {
        string rv = string.Empty;

        for (int i = 0; i < length; i++)
        {
            var val = buffer[i];
            rv += hexmap[val >> 4];
            rv += hexmap[val & 15];

            if (separator != null && i != length - 1)
            {
                rv += separator;
            }
        }

        return rv.ToUpper();
    }

    /// <summary>
    /// Parses an unformatted (no separators) hexadecimal string into a byte array
    /// </summary>
    /// <param name="hexStr">Input hexadecimal string</param>
    /// <returns>Returns a byte array</returns>
    public static byte[] ParseHexStr(string hexStr)
    {
        List<byte> buffer = new List<byte>();
        var chars = hexStr.ToCharArray();
        int posn = 0;
        while (posn < hexStr.Length)
        {
            while (char.IsWhiteSpace(chars[posn]))
            {
                posn++;
            }
            sbyte c = _hexDigits[chars[posn++]];
            if (c == -1)
            {
                break;
            }
            sbyte n = (sbyte)(c << 4);
            c = _hexDigits[chars[posn++]];
            if (c == -1)
            {
                break;
            }
            n |= c;
            buffer.Add((byte)n);
        }
        return buffer.ToArray();
    }
}
