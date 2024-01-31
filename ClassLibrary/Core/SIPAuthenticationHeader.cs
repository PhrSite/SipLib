#region License
//-----------------------------------------------------------------------------
// Filename: SIPFromHeader.cs
//
// Description: SIP Header.
// 
// History:
// 17 Sep 2005	Aaron Clauson	Created.
//
// License: 
// This software is licensed under the BSD License http://www.opensource.org/licenses/bsd-license.php
//
// Copyright (c) 2006 Aaron Clauson (aaron@sipsorcery.com), SIP Sorcery PTY LTD, Hobart, Australia (www.sipsorcery.com)
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
//  Revised:    10 Nov 22 PHR Initial version. Moved here from SIPHeader.cs
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Core;

/// <summary>
/// Class for the SIP WWW-Authenticate and Authorization headers
/// </summary>
public class SIPAuthenticationHeader
{
    /// <summary>
    /// SIP digest
    /// </summary>
    /// <value></value>
    public SIPAuthorisationDigest SIPDigest;

    private SIPAuthenticationHeader()
    {
        SIPDigest = new SIPAuthorisationDigest();
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="sipDigest">Authorization digest</param>
    public SIPAuthenticationHeader(SIPAuthorisationDigest sipDigest)
    {
        SIPDigest = sipDigest;
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="authorisationType">Type of authorization</param>
    /// <param name="realm">Realm</param>
    /// <param name="nonce">Nonce</param>
    public SIPAuthenticationHeader(SIPAuthorisationHeadersEnum authorisationType,
        string realm, string nonce)
    {
        SIPDigest = new SIPAuthorisationDigest(authorisationType);
        SIPDigest.Realm = realm;
        SIPDigest.Nonce = nonce;
    }

    /// <summary>
    /// Parses an authentication or authorization header
    /// </summary>
    /// <param name="authorizationType">Type of authorization or authentication</param>
    /// <param name="headerValue">String header value</param>
    /// <returns>Returns a new SIPAuthenticationHeader if successful or null if unable to
    /// parse the input header value.</returns>
    public static SIPAuthenticationHeader ParseSIPAuthenticationHeader(SIPAuthorisationHeadersEnum authorizationType,
        string headerValue)
    {
        try
        {
            SIPAuthenticationHeader authHeader = new SIPAuthenticationHeader();
            authHeader.SIPDigest = SIPAuthorisationDigest.
                ParseAuthorisationDigest(authorizationType, headerValue);
            return authHeader;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Converts this object into a string
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        if (SIPDigest != null)
        {
            string authHeader = null;
            SIPAuthorisationHeadersEnum authorisationHeaderType = (SIPDigest.
                AuthorisationResponseType != SIPAuthorisationHeadersEnum.
                Unknown) ? SIPDigest.AuthorisationResponseType : SIPDigest.
                AuthorisationType;

            if (authorisationHeaderType == SIPAuthorisationHeadersEnum.Authorize)
                authHeader = SIPHeaders.SIP_HEADER_AUTHORIZATION + ": ";
            else if (authorisationHeaderType == SIPAuthorisationHeadersEnum.ProxyAuthenticate)
                authHeader = SIPHeaders.SIP_HEADER_PROXYAUTHENTICATION + ": ";
            else if (authorisationHeaderType == SIPAuthorisationHeadersEnum.ProxyAuthorization)
                authHeader = SIPHeaders.SIP_HEADER_PROXYAUTHORIZATION + ": ";
            else if (authorisationHeaderType == SIPAuthorisationHeadersEnum.WWWAuthenticate)
                authHeader = SIPHeaders.SIP_HEADER_WWWAUTHENTICATE + ": ";
            else
                authHeader = SIPHeaders.SIP_HEADER_AUTHORIZATION + ": ";

            return authHeader + SIPDigest.ToString();
        }
        else
        {
            return string.Empty;
        }
    }
}
