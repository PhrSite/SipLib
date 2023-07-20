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
        string end)
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
        return GetStringPosition(buffer, startPosition, endPosition, find, end) 
            != -1;
    }
}
