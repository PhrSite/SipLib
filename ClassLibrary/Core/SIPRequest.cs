#region License
//-----------------------------------------------------------------------------
// Filename: SIPRequest.cs
//
// Description: SIP Request.
//
// History:
// 20 Oct 2005	Aaron Clauson	Created.
//
// License: 
// This software is licensed under the BSD License http://www.opensource.org/licenses/bsd-license.php
//
// Copyright (c) 2010 Aaron Clauson (aaron@sipsorcery.com), SIP Sorcery Ltd, (www.sipsorcery.com)
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without modification, are permitted provided that 
// the following conditions are met:
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
//	Revised:	7 Nov 22 PHR -- Initial version. Added documentation comments
//              10 Nov 22 PHR -- Added more validity checks in IsValid()
/////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Text;

namespace SipLib.Core;

// <bnf>
//  Method SP Request-URI SP SIP-Version CRLF
//  *message-header
//	 CRLF
//	 [ message-body ]
//	 
//	 Methods: REGISTER, INVITE, ACK, CANCEL, BYE, OPTIONS
//	 SIP-Version: SIP/2.0
//	 
//	 SIP-Version    =  "SIP" "/" 1*DIGIT "." 1*DIGIT
// </bnf>
/// <summary>
/// Class for a SIP request message
/// </summary>
public class SIPRequest
{
    private delegate bool IsLocalSIPSocketDelegate(string socket, SIPProtocolsEnum protocol);

    private static string m_CRLF = SIPConstants.CRLF;
    private static string m_sipFullVersion = SIPConstants.SIP_FULLVERSION_STRING;
    private static string m_sipVersion = SIPConstants.SIP_VERSION_STRING;
    private static int m_sipMajorVersion = SIPConstants.SIP_MAJOR_VERSION;
    private static int m_sipMinorVersion = SIPConstants.SIP_MINOR_VERSION;

    private string SIPVersion = m_sipVersion;
    private int SIPMajorVersion = m_sipMajorVersion;
    private int SIPMinorVersion = m_sipMinorVersion;

    /// <summary>
    /// Request method
    /// </summary>
    public SIPMethodsEnum Method;

    private string UnknownMethod = null;

    /// <summary>
    /// Request URI
    /// </summary>
    public SIPURI URI;

    /// <summary>
    /// Contains all headers in the request
    /// </summary>
    public SIPHeader Header;

    /// <summary>
    /// Body content as a string
    /// </summary>
    public string Body;

    /// <summary>
    /// The remote IP socket the request was received from or sent to.
    /// </summary>
    public SIPEndPoint RemoteSIPEndPoint;

    /// <summary>
    /// The local SIP socket the request was received on or sent from.
    /// </summary>
    public SIPEndPoint LocalSIPEndPoint;

    private SIPRequest()
    {
    }

    /// <summary>
    /// Constructs a SIPRequest object. Use this constructor when creating a new request.
    /// </summary>
    /// <param name="method">The SIP method for the request.</param>
    /// <param name="uri">The URI to use in the request line. Must be a valid SIPURI.</param>
    public SIPRequest(SIPMethodsEnum method, string uri)
    {
        try
        {
            Method = method;
            URI = SIPURI.ParseSIPURI(uri);
            SIPVersion = m_sipFullVersion;
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// Constructs a SIPRequest object. Use this constructor when creating a new request.
    /// </summary>
    /// <param name="method"></param>
    /// <param name="uri"></param>
    public SIPRequest(SIPMethodsEnum method, SIPURI uri)
    {
         Method = method;
         URI = uri;
         SIPVersion = m_sipFullVersion;
    }

    /// <summary>
    /// Parses a received SIPMessage object that contains a SIP request message and creates a new
    /// SIPRequest object.
    /// </summary>
    /// <param name="sipMessage">SIPMessage object to build the SIPRequest from /// </param>
    /// <returns>Returns a new SIPRequest object.</returns>
    // <exception cref="SIPValidationException"></exception>
    public static SIPRequest ParseSIPRequest(SIPMessage sipMessage)
    {
        string uriStr = null;

        try
        {
            SIPRequest sipRequest = new SIPRequest();
            sipRequest.LocalSIPEndPoint = sipMessage.LocalSIPEndPoint;
            sipRequest.RemoteSIPEndPoint = sipMessage.RemoteSIPEndPoint;

            string statusLine = sipMessage.FirstLine;

            int firstSpacePosn = statusLine.IndexOf(" ");

            string method = statusLine.Substring(0, firstSpacePosn).Trim();
            sipRequest.Method = SIPMethods.GetMethod(method);
            if (sipRequest.Method == SIPMethodsEnum.UNKNOWN)
                sipRequest.UnknownMethod = method;

            statusLine = statusLine.Substring(firstSpacePosn).Trim();
            int secondSpacePosn = statusLine.IndexOf(" ");

            if (secondSpacePosn != -1)
            {
                uriStr = statusLine.Substring(0, secondSpacePosn);

                sipRequest.URI = SIPURI.ParseSIPURI(uriStr);
                sipRequest.SIPVersion = statusLine.Substring(secondSpacePosn, statusLine.Length -
                    secondSpacePosn).Trim();
                sipRequest.Header = SIPHeader.ParseSIPHeaders(sipMessage.SIPHeaders);
                sipRequest.Body = sipMessage.Body;

                return sipRequest;
            }
            else
            {
                throw new SIPValidationException(SIPValidationFieldsEnum.Request, "URI was missing on Request.");
            }
        }
        catch (SIPValidationException)
        {
            throw;
        }
        catch (Exception)
        {
            throw new SIPValidationException(SIPValidationFieldsEnum.Request, 
                "Unknown error parsing SIP Request");
        }
    }

    /// <summary>
    /// Parses a string containing a SIPMessage into a SIPRequest object. 
    /// </summary>
    /// <param name="sipMessageStr">String containing a SIPMessage object</param>
    /// <returns></returns>
    // <exception cref="SIPValidationException">Thrown if the SIPMessage is not a valid SIP request.</exception>
    public static SIPRequest ParseSIPRequest(string sipMessageStr)
    {
        try
        {
            SIPMessage sipMessage = SIPMessage.ParseSIPMessage(sipMessageStr, 
                null, null);
            return SIPRequest.ParseSIPRequest(sipMessage);
        }
        catch (SIPValidationException)
        {
            throw;
        }
        catch (Exception)
        {
            throw new SIPValidationException(SIPValidationFieldsEnum.Request, 
                "Unknown error parsing SIP Request");
        }
    }

    /// <summary>
    /// Converts this SIP request to a string.
    /// </summary>
    /// <returns>Returns the string represention of this SIPRequest object</returns>
    // <exception cref="Exception">Thrown if an unexpected error occured</exception>
    public new string ToString()
    {
        try
        {
            string methodStr = (Method != SIPMethodsEnum.UNKNOWN) ? Method.ToString() : UnknownMethod;
            
            string message = methodStr + " " + URI.ToString() + " " + SIPVersion + m_CRLF + 
                this.Header.ToString();

            if(Body != null)
                message += m_CRLF + Body;
            else
                message += m_CRLF;
        
            return message;
        }
        catch(Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// Converts this message to a byte array using UTF8 encoding.
    /// </summary>
    /// <returns>Returns a UTF-8 encoded byte array</returns>
    public byte[] ToByteArray()
    {
        return Encoding.UTF8.GetBytes(ToString());
    }

    /// <summary>
    /// Creates an identical copy of the SIP Request for the caller. This is a deep copy.
    /// </summary>
    /// <returns>New copy of the SIPRequest.</returns>
    public SIPRequest Copy()
    {
        return ParseSIPRequest(this.ToString());
    }
    
    /// <summary>
    /// Creates the branch-id for the Via header
    /// </summary>
    /// <returns></returns>
    public string CreateBranchId()
    {
        string routeStr = (Header.Routes != null) ? Header.Routes.ToString() : null;
        string toTagStr = (Header.To != null) ? Header.To.ToTag : null;
        string fromTagStr = (Header.From != null) ? Header.From.FromTag : null;
        string topViaStr = (Header.Vias != null && Header.Vias.TopViaHeader != 
            null) ? Header.Vias.TopViaHeader.ToString() : null;

        return CallProperties.CreateBranchId(
            SIPConstants.SIP_BRANCH_MAGICCOOKIE,
            toTagStr,
            fromTagStr,
            Header.CallId,
            URI.ToString(),
            topViaStr,
            Header.CSeq,
            routeStr,
            Header.ProxyRequire,
            null);
    }
    
    /// <summary>
    /// Determines if this SIP header is a looped header. The basis for the decision is the branch
    /// ID in the Via header. If the branch ID for a new header computes to the same branchid as a
    /// Via header already in the SIP header then it is considered a loop.
    /// </summary>
    /// <returns>True if this header is a loop otherwise false.</returns>
    public bool IsLoop(string ipAddress, int port, string currentBranchId)
    {			
        foreach(SIPViaHeader viaHeader in Header.Vias.Via)
        {
            if(viaHeader.Host == ipAddress && viaHeader.Port == port)
            {
                if(viaHeader.Branch == currentBranchId)
                {
                    return true;
                }
            }
        }
            
        return false;
    }

    /// <summary>
    /// Determines if this SIPRequest object is vallid
    /// </summary>
    /// <param name="errorField">Identifies the header field that is not valid</param>
    /// <param name="errorMessage">Explanation of the error.</param>
    /// <returns>Returns true if the SIPMessage is valid.</returns>
    public bool IsValid(out SIPValidationFieldsEnum errorField, out string errorMessage)
    {
        errorField = SIPValidationFieldsEnum.Unknown;
        errorMessage = null;

        if (Header.Vias == null || Header.Vias.Length == 0)
        {
            errorField = SIPValidationFieldsEnum.ViaHeader;
            errorMessage = "No Via headers";
            return false;
        }

        // 10 Nov 22 PHR
        if (Header.MaxForwards > 70)
        {
            errorField = SIPValidationFieldsEnum.MaxForwards;
            errorMessage = "The Max-Forwards value is out of range";
            return false;
        }

        if (string.IsNullOrEmpty(Header.CallId) == true)
        {
            errorField = SIPValidationFieldsEnum.CallID;
            errorMessage = "There is no Call-ID header";
            return false;
        }

        if (Header.From == null)
        {
            errorField = SIPValidationFieldsEnum.FromHeader;
            errorMessage = "There is no From header";
            return false;
        }

        if (Header.To == null)
        {
            errorField = SIPValidationFieldsEnum.ToHeader;
            errorMessage = "There is no To header";
            return false;
        }

        if (SIPVersion != "2.0")
        {
            errorField = SIPValidationFieldsEnum.SipVersion;
            errorMessage = "The SIP version number in the request line " +
                "is not valid";
            return false;
        }

        if (Header.CSeq == -1 || Header.CSeqMethod == SIPMethodsEnum.UNKNOWN)
        {   // There is no CSeq header in the request
            errorField = SIPValidationFieldsEnum.CSeq;
            errorMessage = "There is no CSeq header";
            return false;
        }

        if (Header.CSeqMethod != this.Method)
        {
            errorField = SIPValidationFieldsEnum.CSeq;
            errorMessage = "CSeq method does not equal the request line method";
            return false;
        }

        if (Header.ContentLength != 0 && string.IsNullOrEmpty(Header.ContentType) == true)
        {
            errorField = SIPValidationFieldsEnum.ContentType;
            errorMessage = "No Content-Type header provided";
            return false;
        }

        return true;
    }
}
