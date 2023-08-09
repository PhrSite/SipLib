#region License
//-----------------------------------------------------------------------------
// Filename: Crypto.cs
//
// Description: Encrypts and decrypts data.
//
// History:
// 16 Jul 2005	Aaron Clauson	Created.
// 10 Sep 2009  Aaron Clauson   Updated to use RNGCryptoServiceProvider in place 
//                              of Random.
//
// License:
// Aaron Clauson
//-----------------------------------------------------------------------------
#endregion

/////////////////////////////////////////////////////////////////////////////////////
//	Revised:	7 Nov 22 PHR -- Initial version from sipsorcery.
//              21 Nov 22 PHR
//                - Changed m_randomProvider from a RNGCryptoServiceProvider to
//                  a RandomNumberGenerator class because RNGCryptoServiceProvider
//                  is now obsolete.
//                - Deleted commented out code.
//              3 Aug 23 PHR
//                - Added numeric digits 0-9 to the CHARS array.
//                - Modified GetRandomString to return lower case characters
/////////////////////////////////////////////////////////////////////////////////////

using System.Text;
using System.Security.Cryptography;
    
namespace SipLib.Core;

/// <summary>
/// Class containing various cryptography utilities
/// </summary>
public class Crypto
{
    /// <summary>
    /// Number of digits to return for default random numbers.
    /// </summary>
    private const int DEFAULT_RANDOM_LENGTH = 10;    
    private const int AES_KEY_SIZE = 32;
    private const int AES_IV_SIZE = 16;
    private const string CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    private static Random _rng = new Random();
    //private static RNGCryptoServiceProvider m_randomProvider = new
    //    RNGCryptoServiceProvider();

    // 21 Nov 22 PHR
    private static RandomNumberGenerator m_randomProvider = RandomNumberGenerator.Create();

    private static byte[] GetFixedLengthByteArray(string value, int length)
    {
        if (value.Length < length)
        {
            while (value.Length < length)
            {
                value += 0x00;
            }
        }
        else if (value.Length > length)
        {
            value = value.Substring(0, length);
        }

        return Encoding.UTF8.GetBytes(value);
    }

    /// <summary>
    /// Creates a string of random characters
    /// </summary>
    /// <param name="length">Desired length of the string</param>
    /// <returns>Random string of random alphabet characters</returns>
    public static string GetRandomString(int length)
    {
        char[] buffer = new char[length];

        for (int i = 0; i < length; i++)
            buffer[i] = CHARS[_rng.Next(CHARS.Length)];

        return new string(buffer).ToLower();
    }

    /// <summary>
    /// Creates a random string of characters. The length is DEFAULT_RANDOM_LENGTH
    /// </summary>
    /// <returns>The random string</returns>
    public static string GetRandomString()
    {
        return GetRandomString(DEFAULT_RANDOM_LENGTH);
    }

    /// <summary>
    /// Returns a 10 digit random number.
    /// </summary>
    /// <returns></returns>
    public static int GetRandomInt()
    {
        return GetRandomInt(DEFAULT_RANDOM_LENGTH);
    }

    /// <summary>
    /// Returns a random number of a specified length.
    /// </summary>
    public static int GetRandomInt(int length)
    {
        int randomStart = 1000000000;
        int randomEnd = int.MaxValue;

        if (length > 0 && length < DEFAULT_RANDOM_LENGTH)
        {
            randomStart = Convert.ToInt32(Math.Pow(10, length - 1));
            randomEnd = Convert.ToInt32(Math.Pow(10, length) - 1);
        }

        return GetRandomInt(randomStart, randomEnd);
    }

    /// <summary>
    /// Generates a 32-bit random number between a minimum and a maximum value.
    /// </summary>
    /// <param name="minValue"></param>
    /// <param name="maxValue"></param>
    /// <returns></returns>
    // <exception cref="ArgumentOutOfRangeException"></exception>
    // <exception cref="ApplicationException"></exception>
    public static int GetRandomInt(int minValue, int maxValue)
    {

        if (minValue > maxValue)
        {
            throw new ArgumentOutOfRangeException("minValue");
        }
        else if (minValue == maxValue)
        {
            return minValue;
        }

        long diff = maxValue - minValue + 1;
        int attempts = 0;
        while (attempts < 10)
        {
            byte[] uint32Buffer = new byte[4];
            m_randomProvider.GetBytes(uint32Buffer);
            uint rand = BitConverter.ToUInt32(uint32Buffer, 0);

            long max = (1 + (Int64)UInt32.MaxValue);
            long remainder = max % diff;
            if (rand <= max - remainder)
                return (int)(minValue + (rand % diff));

            attempts++;
        }

        throw new ApplicationException("GetRandomInt did not return an " + 
            "appropriate random number within 10 attempts.");
    }

    /// <summary>
    /// Gets a 16 bit unsigned random number
    /// </summary>
    /// <returns></returns>
    public static ushort GetRandomUInt16()
    {
        byte[] uint16Buffer = new byte[2];
        m_randomProvider.GetBytes(uint16Buffer);
        return BitConverter.ToUInt16(uint16Buffer, 0);
    }

    /// <summary>
    /// Gets a unsigned 32 bit random number
    /// </summary>
    /// <returns></returns>
    public static UInt32 GetRandomUInt()
    {
        byte[] uint32Buffer = new byte[4];
        m_randomProvider.GetBytes(uint32Buffer);
        return BitConverter.ToUInt32(uint32Buffer, 0);
    }

    /// <summary>
    /// Gets an "X2" string representation of a random number.
    /// </summary>
    /// <param name="byteLength">The byte length of the random number string 
    /// to obtain.</param>
    /// <returns>A string representation of the random number. It will be twice 
    /// the length of byteLength.</returns>
    public static string GetRandomByteString(int byteLength)
    {
        byte[] myKey = new byte[byteLength];
        m_randomProvider.GetBytes(myKey);
        string sessionID = null;
        myKey.ToList().ForEach(b => sessionID += b.ToString("x2"));
        return sessionID;
    }

    /// <summary>
    /// Gets the SHA1 hash of an array of string values
    /// </summary>
    /// <param name="values"></param>
    /// <returns></returns>
    public static byte[] GetSHAHash(params string[] values)
    {
        //SHA1 sha = new SHA1Managed();
        SHA1 sha = SHA1.Create();  // 21 Nov 22 PHR
        string plainText = null;
        foreach (string value in values)
            plainText += value;

        return sha.ComputeHash(Encoding.UTF8.GetBytes(plainText));
    }

    /// <summary>
    /// Returns the hash with each byte as an X2 string. This is useful for situations where
    /// the hash needs to only contain safe ASCII characters.
    /// </summary>
    /// <param name="values">The list of string to concantenate and hash.</param>
    /// <returns>A string with "safe" (0-9 and A-F) characters representing the hash.</returns>
    public static string GetSHAHashAsHex(params string[] values)
    {
        byte[] hash = GetSHAHash(values);
        string hashStr = null;
        hash.ToList().ForEach(b => hashStr += b.ToString("x2"));
        return hashStr;
    }
}
