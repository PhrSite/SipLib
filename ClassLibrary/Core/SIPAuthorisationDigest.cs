#region License
//-----------------------------------------------------------------------------
// Filename: Digest.cs
//
// Description: Implements Digest Authentication as defined in RFC2617.
//
// History:
// 08 Sep 2005	Aaron Clauson	Created.
//
// License: 
// This software is licensed under the BSD License http://www.opensource.org/licenses/bsd-license.php
//
// Copyright (c) 2006-2008 Aaron Clauson (aaron@sipsorcery.com), SIP Sorcery PTY LTD, Hobart, Australia (www.sipsorcery.com)
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
//	Revised:	7 Nov 22 PHR -- Initial version.
//              21 Nov 22 PHR -- Changed GetMD5HashBinHex() to MD5.Create()
/////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace SipLib.Core;

/// <summary>
/// Enumeration of SIP authorization/authentication header types
/// </summary>
public enum SIPAuthorisationHeadersEnum
{
    /// <summary>
    /// Unknown
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Authorize
    /// </summary>
    Authorize = 1,
    /// <summary>
    /// Proxy-Authenticate
    /// </summary>
    ProxyAuthenticate = 2,
    /// <summary>
    /// Proxy-Authorization
    /// </summary>
    ProxyAuthorization = 3,
    /// <summary>
    /// WWW-Authenticate
    /// </summary>
    WWWAuthenticate = 4,
}

/// <summary>
/// Class for the SIP authorization digest
/// </summary>
public class SIPAuthorisationDigest
{
    /// <summary>
    /// MD5 authorization algorithm
    /// </summary>
    /// <value></value>
    public const string AUTH_ALGORITHM = "MD5";
    /// <summary>
    /// QOP authentication value
    /// </summary>
    /// <value></value>
    public const string QOP_AUTHENTICATION_VALUE = "auth";
    private const int NONCE_DEFAULT_COUNT = 1;

    private static char[] m_headerFieldRemoveChars = new char[] { ' ', '"', '\'' };

    /// <summary>
    /// This is the type of authorisation request received.
    /// </summary>
    /// <value></value>
    public SIPAuthorisationHeadersEnum AuthorisationType { get; private set; }

    /// <summary>
    /// If this is set it's the type of authorisation response to use otherwise 
    /// use the same as the request 
    /// </summary>
    /// <value></value>
    public SIPAuthorisationHeadersEnum AuthorisationResponseType { get; private set; }

    /// <summary>
    /// Realm value
    /// </summary>
    /// <value></value>
    public string Realm = string.Empty;

    /// <summary>
    /// User name value
    /// </summary>
    /// <value></value>
    public string Username = string.Empty;

    /// <summary>
    /// Password value
    /// </summary>
    /// <value></value>
    public string Password = string.Empty;

    /// <summary>
    /// Destination URL value
    /// </summary>
    /// <value></value>
    public string DestinationURL = string.Empty;

    /// <summary>
    /// URI value
    /// </summary>
    /// <value></value>
    public string URI = string.Empty;

    /// <summary>
    /// Nonce value
    /// </summary>
    /// <value></value>
    public string Nonce = string.Empty;

    /// <summary>
    /// Request type value
    /// </summary>
    /// <value></value>
    public string RequestType = string.Empty;

    /// <summary>
    ///  Response value
    /// </summary>
    /// <value></value>
    public string Response = string.Empty;

    /// <summary>
    /// Algorithm value
    /// </summary>
    /// <value></value>
    public string Algorithhm = string.Empty;

    /// <summary>
    /// Client nonce (used with WWW-Authenticate and qop=auth).
    /// </summary>
    /// <value></value>
    public string Cnonce = string.Empty;

    /// <summary>
    /// Quality of Protection. Values permitted are auth (authentication) and 
    /// auth-int (authentication with integrity protection).
    /// </summary>
    /// <value></value>
    public string Qop = string.Empty;

    /// <summary>
    /// Client nonce Count.
    /// </summary>
    private int NonceCount = 0;

    /// <summary>
    /// Opaque value
    /// </summary>
    /// <value></value>
    public string Opaque = string.Empty;

    /// <summary>
    /// Gets the digest value
    /// </summary>
    /// <value></value>
    public string Digest
    {
        get
        {
            Algorithhm = AUTH_ALGORITHM;

            // Just to make things difficult For some authorisation requests 
            // the header changes when the authenticated response is generated.
            if (AuthorisationType == SIPAuthorisationHeadersEnum.ProxyAuthenticate)
                AuthorisationResponseType = SIPAuthorisationHeadersEnum.ProxyAuthorization;
            else if (AuthorisationType == SIPAuthorisationHeadersEnum.WWWAuthenticate)
                AuthorisationResponseType = SIPAuthorisationHeadersEnum.Authorize;

            // If the authorisation header has specified quality of protection 
            // equal to "auth" a client nonce needs to be supplied.
            string nonceCountStr = string.Empty;
            if (Qop == QOP_AUTHENTICATION_VALUE)
            {
                NonceCount = (NonceCount != 0) ? NonceCount : NONCE_DEFAULT_COUNT;
                nonceCountStr = GetPaddedNonceCount(NonceCount);

                if (Cnonce == null || Cnonce.Trim().Length == 0)
                    Cnonce = Crypto.GetRandomInt().ToString();
            }

            if (Nonce == null)
                Nonce = Crypto.GetRandomString(12);

            return HTTPDigest.DigestCalcResponse(
                AUTH_ALGORITHM,
                Username,
                Realm,
                Password,
                URI,
                Nonce,
                nonceCountStr,
                Cnonce,
                Qop,
                RequestType,
                null!,
                null!);
        }
    }

    /// <summary>
    /// Constructor
    /// </summary>
    public SIPAuthorisationDigest()
    {
        AuthorisationType = SIPAuthorisationHeadersEnum.ProxyAuthorization;
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="authorisationType">Specifies the type of authorization/authentication</param>
    public SIPAuthorisationDigest(SIPAuthorisationHeadersEnum authorisationType)
    {
        AuthorisationType = authorisationType;
    }

    /// <summary>
    /// Parses an authorization digitest
    /// </summary>
    /// <param name="authorisationType">Authorization type</param>
    /// <param name="authorisationRequest"></param>
    /// <returns></returns>
    public static SIPAuthorisationDigest ParseAuthorisationDigest(
        SIPAuthorisationHeadersEnum authorisationType, string authorisationRequest)
    {
        SIPAuthorisationDigest authRequest = new SIPAuthorisationDigest(authorisationType);

        string noDigestHeader = Regex.Replace(authorisationRequest, @"^\s*Digest\s*", "", 
            RegexOptions.IgnoreCase);
        string[] headerFields = noDigestHeader.Split(',');

        if (headerFields != null && headerFields.Length > 0)
        {
            foreach (string headerField in headerFields)
            {
                int equalsIndex = headerField.IndexOf('=');

                if (equalsIndex != -1 && equalsIndex < headerField.Length)
                {
                    string headerName = headerField.Substring(0, equalsIndex).Trim();
                    string headerValue = headerField.Substring(equalsIndex + 1).Trim(
                        m_headerFieldRemoveChars);

                    if (Regex.Match(headerName, "^" + AuthHeaders.AUTH_REALM_KEY + "$", 
                        RegexOptions.IgnoreCase).Success)
                        authRequest.Realm = headerValue;
                    else if (Regex.Match(headerName, "^" + AuthHeaders.AUTH_NONCE_KEY + "$", 
                        RegexOptions.IgnoreCase).Success)
                        authRequest.Nonce = headerValue;
                    else if (Regex.Match(headerName, "^" + AuthHeaders.AUTH_USERNAME_KEY + "$",
                        RegexOptions.IgnoreCase).Success)
                        authRequest.Username = headerValue;
                    else if (Regex.Match(headerName, "^" + AuthHeaders.AUTH_RESPONSE_KEY + "$",
                        RegexOptions.IgnoreCase).Success)
                        authRequest.Response = headerValue;
                    else if (Regex.Match(headerName, "^" + AuthHeaders.AUTH_URI_KEY + "$",
                        RegexOptions.IgnoreCase).Success)
                        authRequest.URI = headerValue;
                    else if (Regex.Match(headerName, "^" + AuthHeaders.AUTH_CNONCE_KEY + "$",
                        RegexOptions.IgnoreCase).Success)
                        authRequest.Cnonce = headerValue;
                    else if (Regex.Match(headerName, "^" + AuthHeaders.AUTH_NONCECOUNT_KEY + "$",
                        RegexOptions.IgnoreCase).Success)
                        int.TryParse(headerValue, out authRequest.NonceCount);
                    else if (Regex.Match(headerName, "^" + AuthHeaders.AUTH_QOP_KEY + "$",
                        RegexOptions.IgnoreCase).Success)
                        authRequest.Qop = headerValue.ToLower();
                    else if (Regex.Match(headerName, "^" + AuthHeaders.AUTH_OPAQUE_KEY + "$",
                        RegexOptions.IgnoreCase).Success)
                        authRequest.Opaque = headerValue;
                    else if (Regex.Match(headerName, "^" + AuthHeaders.AUTH_ALGORITHM_KEY + "$",
                        RegexOptions.IgnoreCase).Success)
                        authRequest.Algorithhm = headerValue;
                }
            }
        }

        return authRequest;
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="authorisationType"></param>
    /// <param name="realm"></param>
    /// <param name="username"></param>
    /// <param name="password"></param>
    /// <param name="uri"></param>
    /// <param name="nonce"></param>
    /// <param name="request"></param>
    public SIPAuthorisationDigest(SIPAuthorisationHeadersEnum authorisationType, string realm, 
        string username, string password, string uri, string nonce, string request)
    {
        AuthorisationType = authorisationType;
        Realm = realm;
        Username = username;
        Password = password;
        URI = uri;
        Nonce = nonce;
        RequestType = request;
    }

    /// <summary>
    /// Sets the credentials
    /// </summary>
    /// <param name="username"></param>
    /// <param name="password"></param>
    /// <param name="uri"></param>
    /// <param name="method"></param>
    public void SetCredentials(string username, string password, string uri, string method)
    {
        Username = username;
        Password = password;
        URI = uri;
        RequestType = method;
    }

    /// <summary>
    /// Converts the authorization digest to a string
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        string authHeader = AuthHeaders.AUTH_DIGEST_KEY + " ";

        authHeader += (Username != null && Username.Trim().Length != 0) ? 
            AuthHeaders.AUTH_USERNAME_KEY + "=\"" + Username + "\"" : null;
        authHeader += (authHeader.IndexOf('=') != -1) ? "," + AuthHeaders.
            AUTH_REALM_KEY + "=\"" + Realm + "\"" : AuthHeaders.AUTH_REALM_KEY + 
            "=\"" + Realm + "\"";
        authHeader += (Nonce != null) ? "," + AuthHeaders.AUTH_NONCE_KEY + 
            "=\"" + Nonce + "\"" : null;
        authHeader += (URI != null && URI.Trim().Length != 0) ? "," + 
            AuthHeaders.AUTH_URI_KEY + "=\"" + URI + "\"" : null;
        authHeader += (Response != null && Response.Length != 0) ? "," + 
            AuthHeaders.AUTH_RESPONSE_KEY + "=\"" + Response + "\"" : null;
        authHeader += (Cnonce != null) ? "," + AuthHeaders.AUTH_CNONCE_KEY + 
            "=\"" + Cnonce + "\"" : null;
        authHeader += (NonceCount != 0) ? "," + AuthHeaders.
            AUTH_NONCECOUNT_KEY + "=" + GetPaddedNonceCount(NonceCount) : null;
        authHeader += (Qop != null) ? "," + AuthHeaders.AUTH_QOP_KEY + "=" + 
            Qop : null;
        authHeader += (Opaque != null) ? "," + AuthHeaders.AUTH_OPAQUE_KEY + 
            "=\"" + Opaque + "\"": null;
        authHeader += (Algorithhm != null) ? "," + AuthHeaders.
            AUTH_ALGORITHM_KEY + "=" + Algorithhm : null;

        return authHeader;
    }

    private string GetPaddedNonceCount(int count)
    {
        return "00000000".Substring(0, 8 - NonceCount.ToString().Length) + count;
    }
}

/// <summary>
/// Class for processing an HTTP digest
/// </summary>
public class HTTPDigest
{
    /// <summary>
    /// Calculate H(A1) as per HTTP Digest specification.
    /// </summary>
    public static string DigestCalcHA1(
        string username,
        string realm,
        string password)
    {
        string a1 = String.Format("{0}:{1}:{2}", username, realm, password);
        return GetMD5HashBinHex(a1);
    }

    /// <summary>
    /// Calculate H(A2) as per HTTP Digest specification.
    /// </summary>
    public static string DigestCalcHA2(string method, string uri)
    {
        string A2 = String.Format("{0}:{1}", method, uri);
        
        return GetMD5HashBinHex(A2);
    }

    /// <summary>
    /// Calculates the digest response
    /// </summary>
    /// <param name="algorithm"></param>
    /// <param name="username"></param>
    /// <param name="realm"></param>
    /// <param name="password"></param>
    /// <param name="uri"></param>
    /// <param name="nonce"></param>
    /// <param name="nonceCount"></param>
    /// <param name="cnonce"></param>
    /// <param name="qop"></param>
    /// <param name="method"></param>
    /// <param name="digestURL"></param>
    /// <param name="hEntity"></param>
    /// <returns></returns>
    public static string DigestCalcResponse(
        string algorithm,
        string username,
        string realm,
        string password,
        string uri,		
        string nonce,
        string nonceCount,
        string cnonce,
        string qop,			// qop-value: "", "auth", "auth-int".
        string method,
        string digestURL,
        string hEntity
        )
    {
        string HA1, HA2;

        HA1 = DigestCalcHA1(username, realm, password);
        HA2 = DigestCalcHA2(method, uri);
        
        string unhashedDigest= null;
        if (nonceCount != null && cnonce != null && qop != null)
            unhashedDigest = String.Format("{0}:{1}:{2}:{3}:{4}:{5}",
                HA1, nonce, nonceCount, cnonce, qop, HA2);
        else
            unhashedDigest = String.Format("{0}:{1}:{2}", HA1, nonce, HA2);

        return GetMD5HashBinHex(unhashedDigest);
    }

    private static string GetMD5HashBinHex(string val)
    {
        //MD5 md5 = new MD5CryptoServiceProvider();
        MD5 md5 = MD5.Create();     // 21 Nov 22 PHR
        byte[] bHA1 = md5.ComputeHash(Encoding.UTF8.GetBytes(val));
        string HA1 = null;
        for (int i = 0 ; i < 16 ; i++)
            HA1 += String.Format("{0:x02}",bHA1[i]);
        return HA1!;
    }
}
