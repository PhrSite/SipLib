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
//              30 Jan 24 PHR
//                -- Added the SIPHeader Header property.
//                -- Made this class the base class for SIPRequest and SIPResponse.
/////////////////////////////////////////////////////////////////////////////////////

using System.Text;
using SipLib.Body;

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
    /// Identifies the type of SIP message -- i.e. a request or a response.
    /// </summary>
    /// <value></value>
    public SIPMessageTypesEnum SIPMessageType = SIPMessageTypesEnum.Unknown;
    /// <summary>
    /// Contains the first line of the message. For example: "SIP/2.0 100 Trying" or 
    /// "INVITE sip:1189990001@10.1.221.8 SIP/2.0"
    /// </summary>
    /// <value></value>
    public string? FirstLine = null;

    /// <summary>
    /// Contains any array of SIP header lines with one header line per array element.
    /// </summary>
    /// <value></value>
    public string[]? SIPHeaders = null;

    /// <summary>
    /// Contains the entire SIP message body as a string. Set to null if the message does not have a body.
    /// </summary>
    /// <value></value>
    public string? Body = null;

    /// <summary>
    /// Contains the raw byte array containing the entire message. This field is used when parsing a SIP
    /// request or a SIP response that was received over a SIP network connection. It must not be used when
    /// creating a new SIPRequest or a SIPResponse locally.
    /// </summary>
    /// <value></value>
    public byte[]? RawBuffer = null;

    /// <summary>
    /// The remote IP socket the message was received from or sent to. 
    /// </summary>
    /// <value></value>
    public SIPEndPoint? RemoteSIPEndPoint = null;
    /// <summary>
    /// The local SIP socket the message was received on or sent from. 
    /// </summary>
    /// <value></value>
    public SIPEndPoint? LocalSIPEndPoint = null;

    /// <summary>
    /// Contains all headers in the request
    /// </summary>
    /// <value></value>
    public SIPHeader? Header = new SIPHeader();

    /// <summary>
    /// Parses a byte array containing a SIP message and returns a SIPMessage object.
    /// </summary>
    /// <param name="buffer">Byte array containing the SIP message that was received from the transport
    /// layer.</param>
    /// <param name="localSIPEndPoint">The local SIP socket the message was received on or sent from.</param>
    /// <param name="remoteSIPEndPoint">The remote IP socket the message was received from or sent to.</param>
    /// <returns>Returns a SIPMessage object if successful. Returns null if the message is not valid.</returns>
    /// <exception cref="ArgumentException">Thrown if the SIP message exceeds the maximum allowable
    /// length defined in: SIPConstants.SIP_MAXIMUM_RECEIVE_LENGTH.</exception>
    /// <exception cref="Exception">Thrown if an unexpected error occurs.</exception>
    public static SIPMessage? ParseSIPMessage(byte[] buffer, SIPEndPoint localSIPEndPoint, 
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
    /// Parses a string containing a SIP message and returns a SIPMessage </summary>
    /// <param name="message">Input string that contains the entire SIP message.</param>
    /// <param name="localSIPEndPoint">The local SIP socket the message was received on or sent from.</param>
    /// <param name="remoteSIPEndPoint">The remote IP socket the message was received from or sent to.</param>
    /// <returns>Returns a SIPMessage object if successful. Returns null if the message is not valid.</returns>
    public static SIPMessage? ParseSIPMessage(string message, SIPEndPoint? localSIPEndPoint, SIPEndPoint? 
        remoteSIPEndPoint)
    {
        try
        {
            SIPMessage sipMessage = new SIPMessage();
            sipMessage.LocalSIPEndPoint = localSIPEndPoint;
            sipMessage.RemoteSIPEndPoint = remoteSIPEndPoint;

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

                sipMessage.RawBuffer = Encoding.UTF8.GetBytes(message);
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

    /// <summary>
    /// Returns true if this message has a body.
    /// </summary>
    /// <value></value>
    public bool HasBody
    {
        get
        {
            if (Header.ContentLength > 0)
                return true;
            else
                return false;
        }
    }

    private List<MessageContentsContainer>? m_ContentsContainer = null;

    /// <summary>
    /// Gets a string containing the body contents for a specified content type. This method only returns non-binary
    /// content types.
    /// </summary>
    /// <param name="contentType">Specifies the MIME type of the content block to get. For example: 
    /// "application/sdp". Use the values defined in the ContentTypes class to ensure consistancy.
    /// This parameter may be a value taken from a message's Content-Type header but it must not include any
    /// header parameters.</param>
    /// <returns>Returns a string that contains the body content block. Returns null if the specified 
    /// content type is not found.</returns>
    public string? GetContentsOfType(string contentType)
    {
        if (HasBody == false || RawBuffer == null)
            return null;

        if (m_ContentsContainer == null)
            m_ContentsContainer = BodyParser.ParseSipBody(RawBuffer, Header.ContentType);

        if (m_ContentsContainer.Count == 0)
            return null;

        foreach (MessageContentsContainer Mcc in m_ContentsContainer)
        {
            if (Mcc.ContentType.ToLower().Contains(contentType.ToLower()) && Mcc.IsBinaryContents == false)
                return Mcc.StringContents;
        }

        return null;
    }

    /// <summary>
    /// Gets the MessageContentsContainer containing the body contents for a specified content type.
    /// </summary>
    /// <param name="contentType">Specifies the MIME type of the content block to get. For example: 
    /// "application/sdp". Use the values defined in the ContentTypes class to ensure consistancy.
    /// This parameter may be a value taken from a message's Content-Type header but it must not include any
    /// header parameters.</param>
    /// <returns>Returns a MessageContentsContainer object containing the specified body contents block.
    /// Returns null if the specified content type is not found.</returns>
    public MessageContentsContainer? GetContentsContainer(string contentType)
    {
        if (HasBody == false || RawBuffer == null)
            return null;

        if (m_ContentsContainer == null)
            m_ContentsContainer = BodyParser.ParseSipBody(RawBuffer, Header.ContentType);

        if (m_ContentsContainer.Count == 0)
            return null;

        foreach (MessageContentsContainer Mcc in m_ContentsContainer)
        {
            if (Mcc.ContentType.ToLower().Contains(contentType.ToLower()) && Mcc.IsBinaryContents == false)
                return Mcc;
        }

        return null;
    }
}
