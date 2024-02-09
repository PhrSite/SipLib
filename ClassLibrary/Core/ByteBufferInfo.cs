#region License
//-----------------------------------------------------------------------------
// Filename: ByteBuffer.cs
//
// Description: Provides some useful methods for working with byte[] buffers.
//
// History:
// 04 May 2006	Aaron Clauson	Created.
//-----------------------------------------------------------------------------
#endregion

/////////////////////////////////////////////////////////////////////////////////////
//	Revised:	7 Nov 22 PHR -- Initial version.
//				28 Nov 22 PHR
//				  -- Fixed so that the match for the first byte in the
//				     findArray is tested again when the findPos index is reset to 0.
//				  -- Fixed code formatting and added some documentation comments.
//              26 Jul 23 PHR
//                -- Added the FindFirstBytePattern, FindLastBytePattern and
//                   ExtractDelimitedByteArray functions
/////////////////////////////////////////////////////////////////////////////////////

using System.Text;

namespace SipLib.Core;

/// <summary>
/// Class for parsing a buffer of bytes
/// </summary>
public class ByteBufferInfo
{
    /// <summary>
    /// Searches a binary buffer for a string up until a specified end string.
    /// </summary>
    /// <param name="buffer">The byte array to search for an instance of the specified string.</param>
    /// <param name="startPosition">The position in the array that the search should be started from.</param>
    /// <param name="endPosition">An index that if reached indicates the search should be halted.</param>
    /// <param name="find">The string that is being searched for.</param>
    /// <param name="end">If the end string is found the search is halted and a negative result returned.
    /// </param>
    /// <returns>The start position in the buffer of the requested string or -1 if not found.</returns>
    public static int GetStringPosition(byte[] buffer, int startPosition, int endPosition, string find,
        string? end)
    {
        if (buffer == null || buffer.Length == 0 || find == null)
            return -1;
        else
        {
            byte[] findArray = Encoding.UTF8.GetBytes(find);
            byte[] endArray = (end != null) ? Encoding.UTF8.GetBytes(end) : null;

            int findPosn = 0;
            int endPosn = 0;

            for (int index = startPosition; index < endPosition; index++)
            {
                if (buffer[index] == findArray[findPosn])
                    findPosn++;
                else
                {
                    findPosn = 0;
                    // 28 Nov 22 PHR
                    // Must check again when the find position is reset because the character at index
                    // could match the start of the findArray.
                    if (buffer[index] == findArray[findPosn])
                        findPosn++;
                }

                if (endArray != null && buffer[index] == endArray[endPosn])
                    endPosn++;
                else
                {
                    endPosn = 0;
                    // 28 Nov 22 PHR -- Check again when reseting
                    if (endArray != null && buffer[index] == endArray[endPosn])
                        endPosn++;
                }

                if (findPosn == findArray.Length)
                    return index - findArray.Length + 1;
                else if(endArray != null && endPosn == endArray.Length)
                    return -1;
            }

            return -1;
        }
    }

    /// <summary>
    /// Tests to see if a binary array contains a string.
    /// </summary>
    /// <param name="buffer">The byte array to search for an instance of the specified string.</param>
    /// <param name="startPosition">The position in the array that the search should be started from.</param>
    /// <param name="endPosition">An index that if reached indicates the search should be halted.</param>
    /// <param name="find">The string that is being searched for.</param>
    /// <param name="end">If this string is not null and is found before the find string is found, then
    /// false is returned.</param>
    /// <returns>Returns true if the array contains the specified string or false if it does not</returns>
    public static bool HasString(byte[] buffer, int startPosition, int 
        endPosition, string find, string end)
    {
        return GetStringPosition(buffer, startPosition, endPosition, find, end) != -1;
    }

    /// <summary>
    /// Finds the first occurrence of byte array pattern within an array.
    /// </summary>
    /// <param name="SrcArray">Array to search in.</param>
    /// <param name="StartIndex">Index to start looking at</param>
    /// <param name="BytePattern">Array of bytes containing the pattern to search for.</param>
    /// <returns>The index within the search array of the start of the pattern to search for. Returns 
    /// -1 if the pattern is not found.
    /// </returns>
    public static int FindFirstBytePattern(byte[] SrcArray, int StartIndex, byte[] BytePattern)
    {
        int Idx = -1;
        if (StartIndex + BytePattern.Length > SrcArray.Length)
            return -1;      // The source array is too short

        int SrcIdx = StartIndex;
        int i;

        bool Done = false;
        bool Found = false;

        while (Done == false)
        {
            Found = true;   // Assume success
            for (i = 0; (i < BytePattern.Length && Found == true); i++)
            {
                if (SrcArray[SrcIdx] != BytePattern[i])
                    Found = false;
                SrcIdx += 1;
            }

            if (Found == true)
            {
                Idx = SrcIdx - BytePattern.Length;
                Done = true;
            }
            else
            {
                if ((SrcArray.Length - SrcIdx) < BytePattern.Length)
                    Done = true;
            }
        }

        return Idx;
    }

    /// <summary>
    /// Finds the last occurrence of byte array pattern within an array.
    /// </summary>
    /// <param name="SrcArray">Array to search in.</param>
    /// <param name="LastSrcIndex">Last index in the source array to include in the search range</param>
    /// <param name="BytePattern">Array of bytes containing the pattern to search for.</param>
    /// <returns>The index within the search array of the start of the pattern to search for. Returns -1
    /// if the pattern is not found.
    /// </returns>
    public static int FindLastBytePattern(byte[] SrcArray, int LastSrcIndex, byte[] BytePattern)
    {
        int Idx = -1;
        if ((LastSrcIndex - 1) < BytePattern.Length)
            return Idx;

        bool Done = false;
        bool Found = false;
        int SrcIdx = LastSrcIndex;
        int i;

        while (Done == false)
        {
            Found = true;   // Assume success
            for (i = BytePattern.Length - 1; (i > 0 && Found == true); i--)
            {
                if (SrcArray[SrcIdx] != BytePattern[i])
                    Found = false;  // Mismatch found

                SrcIdx -= 1;
            } // end for i

            if (Found == true)
            {
                Idx = SrcIdx;
                Done = true;
            }
            else
            {
                if (SrcIdx < BytePattern.Length - 1)
                    Done = true;
            }

        } // end while

        return Idx;
    }

    /// <summary>
    /// Extracts a byte array that is delimited by two byte array patterns
    /// </summary>
    /// <param name="SrcArray">The input source array</param>
    /// <param name="StartIndex">The stating index in the source array</param>
    /// <param name="FirstPattern">The first byte pattern</param>
    /// <param name="SecondPattern">The second byte array</param>
    /// <returns>Returns a new byte array if there is one between the FirstPattern and the
    /// SecondPattern or null if the FirstPattern and the SecondPattern are not found</returns>
    public static byte[]? ExtractDelimitedByteArray(byte[] SrcArray, int StartIndex, byte[] FirstPattern,
        byte[] SecondPattern)
    {
        byte[] DstArray = null;
        int FirstIndex = FindFirstBytePattern(SrcArray, StartIndex, FirstPattern);
        if (FirstIndex == -1)
            return null;
        int SecondIndex = FindFirstBytePattern(SrcArray, StartIndex, SecondPattern);
        if (SecondIndex == -1)
            return null;

        int DstStartIndex = FirstIndex + FirstPattern.Length;
        int DstLength = SecondIndex - DstStartIndex;
        if (DstStartIndex + DstLength > SrcArray.Length)
            return null;    // Error

        DstArray = new byte[DstLength];
        Array.ConstrainedCopy(SrcArray, DstStartIndex, DstArray, 0, DstLength);

        return DstArray;
    }
}
