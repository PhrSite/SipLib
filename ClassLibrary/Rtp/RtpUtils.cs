/////////////////////////////////////////////////////////////////////////////////////
//  File:   RtpUtils.cs                                             19 Sep 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

using System.Diagnostics;
using System.Text;

namespace SipLib.Rtp;

/// <summary>
/// This class contains various static functions for reading and writing words, double words (4 bytes) and
/// double double words (8 bytes) from byte arrays.
/// </summary>
public class RtpUtils
{
    /// <summary>
    /// Gets a ushort value (16-bit WORD) from a byte array that contains the MSB first (Big Endian format).
    /// </summary>
    /// <param name="SrcBytes">Source array. Must contain at least 2 bytes starting at the idx value.</param>
    /// <param name="i">Starting index of the 2 byte long field within the array.</param>
    /// <returns>Returns the word value.</returns>
    public static ushort GetWord(byte[] SrcBytes, int i)
    {
        return Convert.ToUInt16((SrcBytes[i] << 8) + (SrcBytes[i + 1] & 0x00ff));
    }

    /// <summary>
    /// Loads a ushort value (16-bit WORD) into a byte array starting at the specified index value with the MSB
    /// first (Big Endian format).
    /// </summary>
    /// <param name="DestBytes">Destination byte array. Must contain at least 2 bytes starting at the Start
    /// value.</param>
    /// <param name="Start">Starting index of the 2 byte long field.</param>
    /// <param name="Word">16-bit value to load.</param>
    public static void SetWord(byte[] DestBytes, int Start, ushort Word)
    {
            DestBytes[Start] = Convert.ToByte(Word >> 8);
            DestBytes[Start + 1] = Convert.ToByte(Word & 0x00ff);
    }

    /// <summary>
    /// Gets a uint (32-bit DWORD) value from a byte array starting at the specified index value with the MSB
    /// first (Big Endian format).
    /// </summary>
    /// <param name="SrcBytes">Source byte array. Must contain at least 4 bytes starting at the idx value.</param>
    /// <param name="idx">Starting index of the 4 byte long field.</param>
    /// <returns>Returns the uint value.</returns>
    public static uint GetDWord(byte[] SrcBytes, int idx)
    {
        uint RetVal = 0;
        for (int i = 0; i < 4; i++)
            RetVal = (RetVal << 8) | SrcBytes[idx++];

        return RetVal;
    }

    /// <summary>
    /// Loads a uint value (32-bit DWORD) into a byte array starting at the specified index value with the MSB
    /// first (Big Endian format).
    /// </summary>
    /// <param name="DestBytes">Destination byte array. Must contain at least 4 bytes
    /// starting at the Start value.</param>
    /// <param name="Start">Starting index of the 4 byte long field.</param>
    /// <param name="DWord">32-bit value to load.</param>
    public static void SetDWord(byte[] DestBytes, int Start, uint DWord)
    {
            DestBytes[Start] = Convert.ToByte((DWord >> 24) & 0xff);
            DestBytes[Start + 1] = Convert.ToByte((DWord >> 16) & 0xff);
            DestBytes[Start + 2] = Convert.ToByte((DWord >> 8) & 0xff);
            DestBytes[Start + 3] = Convert.ToByte(DWord & 0xff);
    }

    /// <summary>
    /// Gets a 24-bit value from a 3-byte array into a UInt32.
    /// </summary>
    /// <param name="SrcBytes">Source byte array. Must contain at least 3 bytes starting at the idx value.
    /// The byte order must be Big-Endian (MSB first).
    /// </param>
    /// <param name="idx">Starting index of the 24-bit field.</param>
    /// <returns>The 24-bit value packeted into the least significant 3 bytes of a uint value</returns>
    public static uint Get3Bytes(byte[] SrcBytes, int idx)
    {
        UInt32 RetVal = 0;
        for (int i = 0; i < 3; i++)
            RetVal = (RetVal << 8) | SrcBytes[idx++];

        return RetVal;
    }

    /// <summary>
    /// Loads the 3 least significant bytes into a 3-byte long destination byte array with the MSB first
    /// (Big Endian format).
    /// </summary>
    /// <param name="DestBytes">Destination byte aray. Must contain at least 3 bytes starting at the idx value.
    /// </param>
    /// <param name="i">Starting index of the 24-bit field.</param>
    /// <param name="DWord">Contains the 24-bit value to load.</param>
    public static void Set3Bytes(byte[] DestBytes, int i, uint DWord)
    {
        DestBytes[i] = Convert.ToByte((DWord >> 16) & 0xff);
        DestBytes[i + 1] = Convert.ToByte((DWord >> 8) & 0xff);
        DestBytes[i + 2] = Convert.ToByte(DWord & 0xff);
    }

    /// <summary>
    /// Gets a ulong (64-bit) value from a byte array starting at the specified index value with the MSB first
    /// (Big Endian format).
    /// </summary>
    /// <param name="SrcBytes">Source byte array. Must contain at least 8 bytes starting at the idx value.</param>
    /// <param name="idx">Starting index of the 8 byte long field.</param>
    /// <returns>Returns the ulong value.</returns>
    public static ulong Get8ByteWord(byte[] SrcBytes, int idx)
    {
        ulong RetVal = 0;
        for (int i = 0; i < 8; i++)
            RetVal = (RetVal << 8) | SrcBytes[idx++];

        return RetVal;
    }

    /// <summary>
    /// Loads a ulong value (64-bit word) into a byte array starting at the specified index value with the MSB
    /// first (Big Endian format).
    /// </summary>
    /// <param name="DestBytes">Destination byte array. Must contain at least 8 bytes starting at the Start
    /// value.</param>
    /// <param name="Start">Starting index of the 8 byte long field.</param>
    /// <param name="DDw">64-bit value to load.</param>
    public static void Set8ByteWord(byte[] DestBytes, int Start, ulong DDw)
    {
        DestBytes[Start] = Convert.ToByte((DDw >> 56) & 0xff);
        DestBytes[Start + 1] = Convert.ToByte((DDw >> 48) & 0xff);
        DestBytes[Start + 2] = Convert.ToByte((DDw >> 40) & 0xff);
        DestBytes[Start + 3] = Convert.ToByte((DDw >> 32) & 0xff);
        DestBytes[Start + 4] = Convert.ToByte((DDw >> 24) & 0xff);
        DestBytes[Start + 5] = Convert.ToByte((DDw >> 16) & 0xff);
        DestBytes[Start + 6] = Convert.ToByte((DDw >> 8) & 0xff);
        DestBytes[Start + 7] = Convert.ToByte((DDw) & 0xff);
    }

    private static DateTime UtcEpoch2036 = new DateTime(2036, 2, 7, 6, 28, 16, DateTimeKind.Utc);
    private static DateTime UtcEpoch1900 = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static DateTime UtcEpoch1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Converts specified DateTime value to long NTP time.
    /// </summary>
    /// <param name="value">DateTime value to convert. This value must be in local time.</param>
    /// <returns>Returns NTP Timestamp value.</returns>
    /// <notes>
    /// Wallclock time (absolute date and time) is represented using the timestamp format of the Network
    /// Time Protocol (NTP), which is in seconds relative to 0h UTC on 1 January 1900.  The full
    /// resolution NTP timestamp is a 64-bit unsigned fixed-point number with the integer part in the first
    /// 32 bits and the fractional part in the last 32 bits. In some fields where a more compact representation
    /// is appropriate, only the middle 32 bits are used; that is, the low 16 bits of the integer part and the
    /// high 16 bits of the fractional part. The high 16 bits of the integer part must be determined
    /// independently.
    /// </notes>
    public static ulong DateTimeToNtpTimestamp(DateTime value)
    {
        DateTime baseDate = value >= UtcEpoch2036 ? UtcEpoch2036 : UtcEpoch1900;
        TimeSpan elapsedTime = value > baseDate ? value.ToUniversalTime() - baseDate.ToUniversalTime() : 
            baseDate.ToUniversalTime() - value.ToUniversalTime();

        uint MSW = (uint)elapsedTime.TotalSeconds;
        double dLSW = (elapsedTime.TotalSeconds - MSW) * uint.MaxValue;
        uint LSW = (uint)dLSW;
        ulong RetVal = MSW;
        RetVal = RetVal << 32;
        RetVal = RetVal + LSW;

        return RetVal;
    }

    /// <summary>
    /// Converts a NTP timestamp value (as received in an RTCP packet or from an NTP server) to a UTC DateTime
    /// value.
    /// </summary>
    /// <param name="NtpTimeStamp">NTP timestamp value to convert.</param>
    /// <returns>Return a UTC DateTime value. Returns DateTime.MinValue if the input NtpTimeStamp parameter is
    /// 0 or otherwise not valid.</returns>
    /// <remarks>The caller must be prepared to deal with the case where the returned DateTime value is equal
    /// to DateTime.MinValue.</remarks>
    public static DateTime NtpTimeStampToDateTime(ulong NtpTimeStamp)
    {
        DateTime NetworkDateTime;
        DateTime BaseDate;

        if (NtpTimeStamp == 0)
            return DateTime.MinValue;

        ulong intPart = NtpTimeStamp >> 32;
        ulong fractPart = NtpTimeStamp & 0x00000000ffffffff;
        if ((intPart & 0x80000000) == 0x80000000)
            // The MS bit is 1 -- time is in the range of 1900 - 2036.
            BaseDate = UtcEpoch1900;
        else
            // The MS bit is 0 because a wrap around occurred -- this means that time is in the range of
            // 2036 - 2104. See pages 7-8 of RFC 4330.
            BaseDate = UtcEpoch2036;

        double dfract = (fractPart * 1000) / uint.MaxValue; ;
        ulong milliseconds = (intPart * 1000) + (ulong)dfract;

        NetworkDateTime = DateTime.UtcNow;
        try
        {
            NetworkDateTime = BaseDate.AddMilliseconds(milliseconds);
        }
        catch (ArgumentOutOfRangeException)
        {
            NetworkDateTime = DateTime.MinValue;
        }
        catch (ArgumentException) { NetworkDateTime = DateTime.MinValue; }
        catch (Exception) { NetworkDateTime = DateTime.MinValue; }

        return NetworkDateTime;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="Ary"></param>
    public static void DumpByteArray(byte[] Ary)
    {
        StringBuilder Sb = new StringBuilder(1024);
        foreach (byte b in Ary)
        {
            Sb.AppendFormat("{0:X2}", b);
        }

        Debug.WriteLine(Sb.ToString());
    }

}

