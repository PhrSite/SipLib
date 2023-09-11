#region License
//-----------------------------------------------------------------------------
// Filename: SIPMessage.cs
//
// Desciption: Functionality to determine whether a SIP message is a request or
// a response and break a message up into its constituent parts.
//
// History:
// 04 May 2006	Aaron Clauson	Created.
//
// License: 
// This software is licensed under the BSD License http://www.opensource.org/licenses/bsd-license.php
//
// Copyright (c) 2006 Aaron Clauson (aaron@sipsorcery.com), SIP Sorcery PTY LTD, 
// Hobart, Australia (www.sipsorcery.com)
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without modification, are permitted 
// provided that the following conditions are met:
//
// Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer. 
// Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following 
// disclaimer in the documentation and/or other materials provided with the distribution. Neither the name of SIP Sorcery PTY LTD. 
// nor the names of its contributors may be used to endorse or promote products derived from this software without specific 
// prior written permission. 
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, 
// BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. 
// IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, 
// OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, 
// OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, 
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE 
// POSSIBILITY OF SUCH DAMAGE.
//-----------------------------------------------------------------------------
#endregion

/////////////////////////////////////////////////////////////////////////////////////
//	Revised:	7 Nov 22 PHR -- Initial version.
/////////////////////////////////////////////////////////////////////////////////////

using System.Text;

namespace SipLib.Core;

// <bnf>
// generic-message  =  start-line
//                     *message-header
//                     CRLF
//                     [ message-body ]
// start-line       =  Request-Line / Status-Line
// </bnf>

/// <summary>
/// Represents an entire SIP message.
/// </summary>
public class SIPMessage
{		
    private const string SIP_RESPONSE_PREFIX = "SIP";
    // String that must be in a message buffer to be recognised as a SIP message and processed.
    private const string SIP_MESSAGE_IDENTIFIER = "SIP";	
    private static int m_minFirstLineLength = 7;
    private static string m_CRLF = SIPConstants.CRLF;

    /// <summary>
    /// Contains the entire message as a string.
    /// </summary>
    public string RawMessage;
    /// <summary>
    /// Identifies the type of SIP message -- i.e. a request or a response.
    /// </summary>
    public SIPMessageTypesEnum SIPMessageType = SIPMessageTypesEnum.Unknown;
    /// <summary>
    /// Contains the first line of the message. For example: "SIP/2.0 100 Trying"
    /// or "INVITE sip:1189990001@10.1.221.8 SIP/2.0"
    /// </summary>
    public string FirstLine;
    /// <summary>
    /// Contains any array of SIP header lines with one header line per array element.
    /// </summary>
    public string[] SIPHeaders;
    /// <summary>
    /// Contains the entire SIP message body as a string. Set to null if the message does not have a body.
    /// </summary>
    public string Body;
    /// <summary>
    /// Contains the raw byte array containing the entire message.
    /// </summary>
    public byte[] RawBuffer;

    /// <summary>
    /// The remote IP socket the message was received from or sent to. 
    /// </summary>
    public SIPEndPoint RemoteSIPEndPoint;
    /// <summary>
    /// The local SIP socket the message was received on or sent from. 
    /// </summary>
    public SIPEndPoint LocalSIPEndPoint;

    /// <summary>
    /// Parses a byte array containing a SIP message and returns a SIPMessage object.
    /// </summary>
    /// <param name="buffer">Byte array containing the SIP message that was received from the transport
    /// layer.</param>
    /// <param name="localSIPEndPoint">The local SIP socket the message was received on or sent from.</param>
    /// <param name="remoteSIPEndPoint">The remote IP socket the message was received from or sent to.</param>
    /// <returns>Returns a SIPMessage object if successful. Returns null if the message is not valid.</returns>
    // <exception cref="ArgumentException">Thrown if the SIP message exceeds that maximum allowable
    // length defined in: SIPConstants.SIP_MAXIMUM_RECEIVE_LENGTH.</exception>
    // <exception cref="Exception">Thrown if an unexpected error occurs.</exception>
    public static SIPMessage ParseSIPMessage(byte[] buffer, SIPEndPoint localSIPEndPoint, 
        SIPEndPoint remoteSIPEndPoint)
    {
        string message = null;											  

        try
        {
            if(buffer == null || buffer.Length < m_minFirstLineLength)
                return null;
            else if (buffer.Length > SIPConstants.SIP_MAXIMUM_RECEIVE_LENGTH)
                throw new ArgumentException("SIP message received that " +
                    "exceeded the maximum allowed message length, ignoring.");
            else if(!ByteBufferInfo.HasString(buffer, 0, buffer.Length, SIP_MESSAGE_IDENTIFIER, m_CRLF))
                // Message does not contain "SIP" anywhere on the first line, ignore.
                return null;
            else
            {
                message = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
                SIPMessage sipMessage = ParseSIPMessage(message, localSIPEndPoint, remoteSIPEndPoint);

                if (sipMessage != null)
                {
                    sipMessage.RawBuffer = buffer;
                    return sipMessage;
                }
                else
                    return null;
            }
        }
        catch(Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Parses a byte array containing a SIP message and returns a SIPMessage </summary>
    /// <param name="message">Input string that contains the entire SIP message.</param>
    /// <param name="localSIPEndPoint">The local SIP socket the message was received on or sent from.</param>
    /// <param name="remoteSIPEndPoint">The remote IP socket the message was received from or sent to.</param>
    /// <returns>Returns a SIPMessage object if successful. Returns null if the message is not valid.</returns>
    public static SIPMessage ParseSIPMessage(string message, SIPEndPoint 
        localSIPEndPoint, SIPEndPoint remoteSIPEndPoint)
    {
        try
        {
            SIPMessage sipMessage = new SIPMessage();
            sipMessage.LocalSIPEndPoint = localSIPEndPoint;
            sipMessage.RemoteSIPEndPoint = remoteSIPEndPoint;

            sipMessage.RawMessage = message;
            int endFistLinePosn = message.IndexOf(m_CRLF);

            if (endFistLinePosn != -1)
            {
                sipMessage.FirstLine = message.Substring(0, endFistLinePosn);

                if (sipMessage.FirstLine.Substring(0, 3) == SIP_RESPONSE_PREFIX)
                    sipMessage.SIPMessageType = SIPMessageTypesEnum.Response;
                else
                    sipMessage.SIPMessageType = SIPMessageTypesEnum.Request;

                int endHeaderPosn = message.IndexOf(m_CRLF + m_CRLF);
                if (endHeaderPosn == -1)
                {   // Assume flakey implementation if message does not contain the required CRLFCRLF
                    // sequence and treat the message as having no body.
                    string headerString = message.Substring(endFistLinePosn + 2, message.Length -
                        endFistLinePosn - 2);
                    sipMessage.SIPHeaders = SIPHeader.SplitHeaders(headerString); 
                }
                else
                {
                    string headerString = message.Substring(endFistLinePosn + 2, endHeaderPosn -
                        endFistLinePosn - 2);
                    sipMessage.SIPHeaders = SIPHeader.SplitHeaders(headerString); 

                    if (message.Length > endHeaderPosn + 4)
                        sipMessage.Body = message.Substring(endHeaderPosn + 4);
                }

                return sipMessage;
            }
            else
                return null;
        }
        catch(Exception)
        {
            return null;
        }
    }
}
