/////////////////////////////////////////////////////////////////////////////////////
//  File:   MsrpSteamParser.cs                                      27 Jul 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Msrp;

using SipLib.Core;
using System.Text;

/// <summary>
/// This class parses each byte received in a TCP/TLS stream and extracts complete MSRP
/// messages from the steam.
/// </summary>
public class MsrpStreamParser
{
    private static readonly string EndLinePrefixString = "-------";
    private static readonly byte[] CrLfBytes = Encoding.UTF8.GetBytes("\r\n");
    private static readonly byte[] MsrpBytePattern = Encoding.UTF8.GetBytes("MSRP");

    /// <summary>
    /// Default length for the buffer used to build up MSRP messages.
    /// </summary>
    private const int DEFAULT_BUFFER_LENGTH = 10000;

    /// <summary>
    /// Buffer for building up a complete MSRP transaction message.
    /// </summary>
    private byte[] m_MessageBuffer = new byte[DEFAULT_BUFFER_LENGTH];

    /// <summary>
    /// Number of bytes written into m_MessageBuffer
    /// </summary>
    private int m_CurrentLength = 0;

    private ParsingStateEnum m_ParsingState = ParsingStateEnum.Idle;

    private int m_MsrpPatternIndex = 0;

    private byte[]? m_EndLineBytePattern = null;

    // The bytes to collect after the end line pattern is detected are the continuation flag byte ($, + or #),
    // CR and LF.
    private const int PostEndLinePatternBytes = 3;
    private int m_PostEndEndLinePatternBytesCollected = 0;

    private int m_MaxMsrpMessageLength;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="MaxMsrpMessageLength"></param>
    public MsrpStreamParser(int MaxMsrpMessageLength)
    {
        m_MaxMsrpMessageLength = MaxMsrpMessageLength;
    }

    /// <summary>
    /// Processes the next byte received from the TCP/TLS network stream and searhes for a complete MSRP
    /// message transaction.
    /// </summary>
    /// <param name="NextByte">Next byte to process</param>
    /// <returns>Returns true if a complete MSRP message transaction block is detected. Return false
    /// if a message is not available yet. The caller must immediately call GetMessageBytes() to
    /// retrive the new message if this method return true.</returns>
    public bool ProcessByte(byte NextByte)
    {
        bool MessageFound = false;
        if (m_CurrentLength >= m_MessageBuffer.Length)
        {   // Need to re-allocate the message buffer
            int NewLength = m_MessageBuffer.Length + DEFAULT_BUFFER_LENGTH;
            if (NewLength > m_MaxMsrpMessageLength)
                Reset();
            else
            {
                byte[] NewArray = new byte[NewLength];
                Array.ConstrainedCopy(m_MessageBuffer, 0, NewArray, 0, m_MessageBuffer.Length);
                m_MessageBuffer = NewArray;
            }
        }

        m_MessageBuffer[m_CurrentLength++] = NextByte;

        int index;
        if (m_ParsingState == ParsingStateEnum.Idle)
        {
            index = ByteBufferInfo.FindFirstBytePattern(m_MessageBuffer, 0, MsrpBytePattern);
            if (index >= 0)
            {
                m_ParsingState = ParsingStateEnum.MsrpPatternFound;
                m_MsrpPatternIndex = index;
            }
        }
        else if (m_ParsingState == ParsingStateEnum.MsrpPatternFound)
        {   // Search for the CRLF byte string after the "MSRP" characters
            index = ByteBufferInfo.FindFirstBytePattern(m_MessageBuffer, 0, CrLfBytes);
            if (index > 0)
            {   // Found the CRLF bytes after the "MSRP" characters so process the first line
                int FirstLineLength = index - m_MsrpPatternIndex;
                byte[] FirstLineBytes = new byte[FirstLineLength];
                Array.ConstrainedCopy(m_MessageBuffer, m_MsrpPatternIndex, FirstLineBytes, 0, FirstLineLength);
                string FirstLine = Encoding.UTF8.GetString(FirstLineBytes);
                string[] Fields = FirstLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (Fields == null || Fields.Length < 3)
                    Reset();
                else
                {   // The second field (index = 1) of the first MSRP message line is the transaction ID
                    m_EndLineBytePattern = Encoding.UTF8.GetBytes(EndLinePrefixString + Fields[1]);
                    m_ParsingState = ParsingStateEnum.EndLineSearch;
                }
            }
        }
        else if (m_ParsingState == ParsingStateEnum.EndLineSearch)
        {
            index = FindEndLinePattern(m_MessageBuffer, m_CurrentLength - 1, m_EndLineBytePattern!);
            if (index > 0)
            {   // The end line pattern was found
                m_ParsingState = ParsingStateEnum.EndLineFound;
                m_PostEndEndLinePatternBytesCollected = 0;
            }
        }
        else if (m_ParsingState == ParsingStateEnum.EndLineFound)
        {
            m_PostEndEndLinePatternBytesCollected += 1;
            if (m_PostEndEndLinePatternBytesCollected == PostEndLinePatternBytes)
                MessageFound = true;
        }

        return MessageFound;
    }

    /// <summary>
    /// Searches for the MSRP end line pattern byte array pattern within an array by searching from the
    /// end of the read buffer.
    /// </summary>
    /// <param name="SrcArray">Array to search in.</param>
    /// <param name="LastSrcIndex">Last index in the source array to include in the search range</param>
    /// <param name="BytePattern">Array of bytes containing the end line pattern to search for.</param>
    /// <returns>The index within the search array of the start of the pattern to search for. Returns -1
    /// if the pattern is not found.
    /// </returns>
    public static int FindEndLinePattern(byte[] SrcArray, int LastSrcIndex, byte[] BytePattern)
    {
        int Idx = -1;
        if ((LastSrcIndex - 1) < BytePattern.Length)
            return Idx;

        bool Found = false;
        int SrcIdx = LastSrcIndex;
        int i;

        Found = true;   // Assume success
        for (i = BytePattern.Length - 1; (i > 0 && Found == true); i--)
        {
            if (SrcArray[SrcIdx] != BytePattern[i])
                Found = false;  // Mismatch found

            SrcIdx -= 1;
        } // end for i

        if (Found == true)
            Idx = SrcIdx;

        return Idx;
    }

    /// <summary>
    /// Gets the full MSRP message transaction from the current stream buffer. This method
    /// must be called immediately if the ProcessByte() method returns true.
    /// </summary>
    /// <returns>Returns a byte array containing the full MSRP message transaction. Returns null
    /// if this method is not called immediately after ProcessByte() returns true.</returns>
    public byte[]? GetMessageBytes()
    {
        byte[] MessageBytes = null;
        int MessageLength = m_CurrentLength - m_MsrpPatternIndex;
        if (MessageLength < 0)
            return null;    // Error
        else
        {
            MessageBytes = new byte[MessageLength];
            Array.ConstrainedCopy(m_MessageBuffer, m_MsrpPatternIndex, MessageBytes, 0, MessageLength);
        }

        Reset();    // Get ready for the next message

        return MessageBytes;
    }

    private void Reset()
    {
        m_ParsingState = ParsingStateEnum.Idle;
        m_MessageBuffer = new byte[DEFAULT_BUFFER_LENGTH];
        m_CurrentLength = 0;
        m_MsrpPatternIndex = 0;
        m_EndLineBytePattern = null;
        m_PostEndEndLinePatternBytesCollected = 0;
    }
}

internal enum ParsingStateEnum
{
    Idle,
    MsrpPatternFound,
    FirstLineFound,
    EndLineSearch,
    EndLineFound
}