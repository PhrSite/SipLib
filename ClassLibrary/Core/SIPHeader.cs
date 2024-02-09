#region License
//-----------------------------------------------------------------------------
// Filename: SIPHeader.cs
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
//  Revised:    7 Nov 22 PHR Initial version.
//              -- Added support for multiple Call-Info headers.
//              -- Added support for the GeoLocation, Geolocation-Routing and the
//                 Geolocation-Error headers defined in RFC 6442.
//              -- Added support for the P-Asserted-Identity and the P-Preferred-
//                 Identity headers.
//              -- Added support for the Resource-Priority header.
//              -- Added parsing code for the Server header in ParseSIPHeaders()
//              -- Moved the classes for SIP Headers to individual files.
//              -- Added documentation comments.
/////////////////////////////////////////////////////////////////////////////////////

using System.Text;
using System.Text.RegularExpressions;

namespace SipLib.Core;

/// <bnf>
/// header  =  "header-name" HCOLON header-value *(COMMA header-value) 
/// field-name: field-value CRLF
/// </bnf>
/// <summary>
/// Class for handling the SIP headers portion of a SIP request or a SIP response message.
/// </summary>
/// <remarks>
/// For header fields with string values, a null value indicates that the header field is not present.
/// For header fields with numeric values, a value of -1 indicates that the header field is not present.
/// </remarks>
public class SIPHeader
{
    private const int DEFAULT_CSEQ = 100;
    private static string CRLF = SIPConstants.CRLF;

    /// <summary>
    /// Accept header field. See Section 20.1 of RFC 3261.
    /// </summary>
    /// <value></value>
    public string? Accept = null;
    /// <summary>
    /// Accept-Encoding header field. See Section 20.2 of RFC 3261.
    /// </summary>
    /// <value></value>
    public string? AcceptEncoding = null;
    /// <summary>
    /// Accept-Language header field. See Section 20.3 of RFC 3261.
    /// </summary>
    /// <value></value>
    public string? AcceptLanguage = null;
    /// <summary>
    /// Alert-Info header field. See Section 20.4 of RFC 3261.
    /// </summary>
    /// <value></value>
    public string? AlertInfo = null;
    /// <summary>
    /// Allow header field. See Section 20.5 of RFC 3261.
    /// </summary>
    /// <value></value>
    public string? Allow = null;
    /// <summary>
    /// Allow-Events header field. The Allow header field lists the set of 
    /// methods supported by the UA generating the message. See Section 3.3.7 of
    /// RFC 3265.
    /// </summary>
    /// <value></value>
    public string? AllowEvents = null;
    /// <summary>
    /// Authentication-Info header field. See Section 20.6 of RFC 3261
    /// </summary>
    /// <value></value>
    public string? AuthenticationInfo = null;
    /// <summary>
    /// WWW-Authenticate header field. See Section 20.44 of RFC 3261.
    /// </summary>
    /// <value></value>
    public SIPAuthenticationHeader? AuthenticationHeader = null;
    /// <summary>
    /// Call-ID header field. See Section 20.8 of RFC 3261.
    /// </summary>
    /// <value></value>
    public string? CallId = null;

    /// <summary>
    /// Call-Info header field. See Section 20.9 of RFC 3261.
    /// An empty list indicates that there are no Call-Info header fields.
    /// </summary>
    /// <value></value>
    public List<SIPCallInfoHeader> CallInfo = new List<SIPCallInfoHeader>();
    /// <summary>
    /// Contact header field. See Section 20.10 of RFC 3261.
    /// An empty list indicates that there are no Contact header fiels.
    /// </summary>
    /// <value></value>
    public List<SIPContactHeader>? Contact = new List<SIPContactHeader>();
    /// <summary>
    /// Content-Disposition header field. See Section 20.11 of RFC 3261.
    /// </summary>
    /// <value></value>
    public string? ContentDisposition = null;
    /// <summary>
    /// Content-Encoding header field. See Section 20.12 of RFC 3261.
    /// </summary>
    /// <value></value>
    public string? ContentEncoding = null;
    /// <summary>
    /// Content-Language header field. See Section 20.13 of RFC 3261.
    /// </summary>
    /// <value></value>
    public string? ContentLanguage = null;
    /// <summary>
    /// Content-Type header field. See Section 20.15 of RFC 3261.
    /// </summary>
    /// <value></value>
    public string? ContentType = null;
    /// <summary>
    /// Content-Length header field. See Section 20.14 of RFC 3261.
    /// </summary>
    /// <value></value>
    public int ContentLength = 0;

    /// <summary>
    /// Specifies the numeric portion of the CSeq header field. See Section 20.16 of RFC 3261.
    /// A value of -1 indicates that there is no CSeq field.
    /// </summary>
    /// <value></value>
    public int CSeq = -1;
    /// <summary>
    /// Specifies the method portion of the CSeq header field. See Section 20.16 of RFC 3261.
    /// </summary>
    /// <value></value>
    public SIPMethodsEnum CSeqMethod = SIPMethodsEnum.NONE;

    /// <summary>
    /// Date header field. See Section 20.17 of RFC 3261.
    /// </summary>
    /// <value></value>
    public string? Date = null;
    /// <summary>
    /// Error-Info header field. See Section 20.18 of RFC 3261.
    /// </summary>
    /// <value></value>
    public string? ErrorInfo = null;
    /// <summary>
    /// Event header field. See RFC 3265 and RFC 6665.
    /// </summary>
    /// <value></value>
    public string? Event = null;
    /// <summary>
    /// Expires header field. See Section 20.19 of RFC 3261.
    /// </summary>
    /// <value></value>
    public int Expires = -1;
    /// <summary>
    /// From header field. See Section 20.20 of RFC 3261.
    /// </summary>
    /// <value></value>
    public SIPFromHeader? From = null;
    /// <summary>
    /// P-Asserted-Identity header field. See RFC 3325.
    /// </summary>
    /// <value></value>
    public SIPPaiHeader? PAssertedIdentity;
    /// <summary>
    /// P-Preferred-Identity header field. See RFC 3325.
    /// </summary>
    /// <value></value>
    public SIPPpiHeader? PPreferredIdentity;

    /// <summary>
    /// Contains the header field values of 1 or more Geolocation headers as defined in RFC 6442. 
    /// An empty list indicates that no Geolocation headers are present.
    /// </summary>
    /// <value></value>
    public List<SIPGeolocationHeader> Geolocation = new List<SIPGeolocationHeader>();
    /// <summary>
    /// Contains the header value of a Geolocation-Routing header as defined in RFC 6442. The header
    /// value may be either yes or no.
    /// </summary>
    /// <value></value>
    public string? GeolocationRouting = null;
    /// <summary>
    /// Contains the header value of a Geolocation-Error header as defined in RFC 6442. The value is
    /// numeric code between 1 and 3 digits long.
    /// </summary>
    /// <value></value>
    public string? GeolocationError = null;
    /// <summary>
    /// In-Reply-To header field. See Section 20.21 of RFC 3261.
    /// </summary>
    /// <value></value>
    public string? InReplyTo = null;
    /// <summary>
    /// Min-Expires header field. See Section 20.23 of RFC 3261.
    /// </summary>
    /// <value></value>
    public int MinExpires = -1;
    /// <summary>
    /// Max-Forwards header field. See Section 20.22 of RFC 3261.
    /// </summary>
    /// <value></value>
    public int MaxForwards = SIPConstants.DEFAULT_MAX_FORWARDS;
    /// <summary>
    /// MIME-Version header field. See Section 20.24 of RFC 3261.
    /// </summary>
    /// <value></value>
    public string? MIMEVersion = null;
    /// <summary>
    /// Organization header field. See Section 20.25 of RFC 3261.
    /// </summary>
    /// <value></value>
    public string?Organization = null;
    /// <summary>
    /// Priority header field. See Section 20.26 of RFC 3261.
    /// </summary>
    /// <value></value>
    public string? Priority = null;
    /// <summary>
    /// Proxy-Require header field. See Section 20.29 of RFC 3261.
    /// </summary>
    /// <value></value>
    public string? ProxyRequire = null;
    /// <summary>
    /// Reason header field. See RFC 3326.
    /// </summary>
    /// <value></value>
    public string? Reason = null;
    /// <summary>
    /// Record-Route header field. See Section 20.30 of RFC 3261.
    /// </summary>
    /// <value></value>
    public SIPRouteSet RecordRoutes = new SIPRouteSet();

    /// <summary>
    /// The Referred-By header field. See RFC 3515.
    /// </summary>
    /// <value></value>
    public string? ReferredBy = null;

    /// <summary>
    /// Refer-Sub header field. See RFC 4488. If set to false indicates the implict REFER subscription
    /// should not be created.
    /// </summary>
    /// <value></value>
    public string? ReferSub = null;

    /// <summary>
    /// Refer-To header field. See RFC 3515.
    /// </summary>
    /// <value></value>
    public string? ReferTo = null;
    /// <summary>
    /// Reply-To header field. See Section 20.31 of RFC 3261.
    /// </summary>
    /// <value></value>
    public string? ReplyTo = null;
    /// <summary>
    /// Require header field. See Section 20.32 of RFC 3261.
    /// </summary>
    /// <value></value>
    public string? Require = null;
    /// <summary>
    /// Resource-Priority header field. See RFC 4412.
    /// </summary>
    /// <value></value>
    public string? ResourcePriority = null;
    /// <summary>
    /// Retry-After header field. See Section 20.33 of RFC 3261.
    /// </summary>
    /// <value></value>
    public string? RetryAfter = null;
    /// <summary>
    /// Route header field. See Section 20.34 of RFC 3261.
    /// </summary>
    /// <value></value>
    public SIPRouteSet Routes = new SIPRouteSet();
    /// <summary>
    /// Server header field. See Section 20.35 of RFC 3261.
    /// </summary>
    /// <value></value>
    public string? Server = null;
    /// <summary>
    /// Subject header field. See Section 20.36 of RFC 3261.
    /// </summary>
    /// <value></value>
    public string? Subject = null;
    /// <summary>
    /// Subscription-State header field. See RFC 3265 and RFC 6665.
    /// </summary>
    /// <value></value>
    public string? SubscriptionState = null;
    /// <summary>
    /// Supported header field. See Section 20.37 of RFC 3261.
    /// </summary>
    /// <value></value>
    public string? Supported = null;
    /// <summary>
    /// Timestamp header field. See Section 20.38 of RFC 3261.
    /// </summary>
    /// <value></value>
    public string? Timestamp = null;
    /// <summary>
    /// To header field. See Section 20.39 of RFC 3261.
    /// </summary>
    /// <value></value>
    public SIPToHeader? To = null;
    /// <summary>
    /// Unsupported header field. See Section 20.40 of RFC 3261.
    /// </summary>
    /// <value></value>
    public string? Unsupported = null;
    /// <summary>
    /// User-Agent header field. See Section 20.41 of RFC 3261.
    /// </summary>
    /// <value></value>
    public string? UserAgent = null;
    /// <summary>
    /// Via header field. See Section 20.42 of RFC 3261.
    /// </summary>
    /// <value></value>
    public SIPViaSet Vias = new SIPViaSet();
    /// <summary>
    /// Warning header field. See Section 20.43 of RFC 3261.
    /// </summary>
    /// <value></value>
    public string? Warning;

    /// <summary>
    /// Holds any unrecognized headers. Each item in the list is the original header line.
    /// </summary>
    /// <value></value>
    public List<string> UnknownHeaders = new List<string>();

    /// <summary>
    /// Default constructor.
    /// </summary>
    public SIPHeader()
    { }

    /// <summary>
    /// Constructs a new SIPHeader object.
    /// </summary>
    /// <param name="fromHeader">From header field value.</param>
    /// <param name="toHeader">To header field value.</param>
    /// <param name="cseq">CSeq header field value.</param>
    /// <param name="callId">Call-ID header field value.</param>
    /// <exception cref="ApplicationException">Thrown if the fromHeader, toHeader
    /// or callId parameters are null or empty.</exception>
    public SIPHeader(string fromHeader, string toHeader, int cseq, string callId)
    {
        SIPFromHeader from = SIPFromHeader.ParseFromHeader(fromHeader);
        SIPToHeader to = SIPToHeader.ParseToHeader(toHeader);
        Initialise(null!, from, to, cseq, callId);
    }

    /// <summary>
    /// Constructs a new SIPHeader object.
    /// </summary>
    /// <param name="fromHeader">From header field value.</param>
    /// <param name="toHeader">To header field value.</param>
    /// <param name="contactHeader">Contact header field value</param>
    /// <param name="cseq">CSeq header field value.</param>
    /// <param name="callId">Call-ID header field value.</param>
    /// <exception cref="ApplicationException">Thrown if the fromHeader, toHeader or callId parameters
    /// are null or empty.</exception>
    public SIPHeader(string fromHeader, string toHeader, string contactHeader, 
        int cseq, string callId)
    {
        SIPFromHeader from = SIPFromHeader.ParseFromHeader(fromHeader);
        SIPToHeader to = SIPToHeader.ParseToHeader(toHeader);
        List<SIPContactHeader> contact = SIPContactHeader.ParseContactHeader(contactHeader);
        Initialise(contact, from, to, cseq, callId);
    }

    /// <summary>
    /// Constructs a new SIPHeader object.
    /// </summary>
    /// <param name="from">SIPFromHeader object</param>
    /// <param name="to">SIPToHeader object</param>
    /// <param name="cseq">Numeric portion of the CSeq header field</param>
    /// <param name="callId">Call-ID header field value</param>
    /// <exception cref="ApplicationException">Thrown if the from, to or callId parameters are null or
    /// empty.</exception>
    public SIPHeader(SIPFromHeader from, SIPToHeader to, int cseq, string callId)
    {
        Initialise(null!, from, to, cseq, callId);
    }

    /// <summary>
    /// Constructs a new SIPHeader object.
    /// </summary>
    /// <param name="contact">SIPContact object</param>
    /// <param name="from">SIPFromHeader object</param>
    /// <param name="to">SIPToHeader object</param>
    /// <param name="cseq">Numeric portion of the CSeq header field</param>
    /// <param name="callId">Call-ID header field value</param>
    /// <exception cref="ApplicationException">Thrown if the from, to or callId parameters are null or
    /// empty.</exception>
    public SIPHeader(SIPContactHeader contact, SIPFromHeader from, 
        SIPToHeader to, int cseq, string callId)
    {
        List<SIPContactHeader> contactList = new List<SIPContactHeader>();
        if (contact != null)
            contactList.Add(contact);

        Initialise(contactList, from, to, cseq, callId);
    }

    /// <summary>
    /// Constructs a new SIPHeader object
    /// </summary>
    /// <param name="contactList">List of SIPContactHeader objects</param>
    /// <param name="from">SIPFromHeader object</param>
    /// <param name="to">SIPToHeader object</param>
    /// <param name="cseq">Numeric portion of the CSeq header field</param>
    /// <param name="callId">Call-ID header field value</param>
    /// <exception cref="ApplicationException">Thrown if the from, to or callId parameters are null or
    /// empty.</exception>
    public SIPHeader(List<SIPContactHeader> contactList, SIPFromHeader from, SIPToHeader to, int cseq,
        string callId)
    {
        Initialise(contactList, from, to, cseq, callId);
    }

    private void Initialise(List<SIPContactHeader> contact, SIPFromHeader from, SIPToHeader to, int cseq,
        string callId)
    {
        if (from == null)
            throw new ApplicationException( "The From header cannot be " + 
                "empty when creating a new SIP header.");

        if (to == null)
            throw new ApplicationException("The To header cannot be empty " + 
                "when creating a new SIP header.");

        if (callId == null || callId.Trim().Length == 0)
            throw new ApplicationException("The CallId header cannot be " + 
                "empty when creating a new SIP header.");

        From = from;
        To = to;
        Contact = contact;
        CallId = callId;

        if (cseq > 0 && cseq < int.MaxValue)
            CSeq = cseq;
        else
            CSeq = DEFAULT_CSEQ;
    }

    /// <summary>
    /// Splits a string containing the header portion of a SIP message into an array of strings. Handles
    /// header field folding.
    /// </summary>
    /// <param name="message"></param>
    /// <returns>Each string contains a header line</returns>
    public static string[] SplitHeaders(string message)
    {
        // SIP headers can be extended across lines if the first character of 
        // the next line is at least one whitespace character.
        message = Regex.Replace(message, CRLF + @"\s+", " ", RegexOptions.Singleline);

        // Some user agents couldn't get the \r\n bit right.
        //message = Regex.Replace(message, "\r ", CRLF, RegexOptions.Singleline);

        return Regex.Split(message, CRLF);
    }

    /// <summary>
    /// Parses an array of header lines and creates a new SIPHeader object.
    /// </summary>
    /// <param name="headersCollection">Each line contains a single SIP header.</param>
    /// <returns>Returns a new SIPHeader object</returns>
    /// <exception cref="SIPValidationException">Thrown if unable to parse the header fields due to
    /// invalid SIP formatting or illegal values.</exception>
    /// <exception cref="Exception">Thrown if an unknown error occurs. </exception>
    public static SIPHeader ParseSIPHeaders(string[] headersCollection)
    {
        try
        {
            SIPHeader sipHeader = new SIPHeader();
            // This allows detection of whether this header is present or not.
            sipHeader.MaxForwards = -1;

            ParseAllHeaders(headersCollection, sipHeader);
            sipHeader.Validate();

            return sipHeader;
        }
        catch (SIPValidationException)
        {
            throw;
        }
        catch (Exception)
        {
            throw new SIPValidationException(SIPValidationFieldsEnum.Headers, "Unknown error parsing Headers.");
        }
    }

    /// <summary>
    /// Parses all of the headers in the input string array.
    /// </summary>
    /// <param name="headersCollection">Input array of header lines.</param>
    /// <param name="sipHeader">Headers are added to this object.</param>
    /// <exception cref="SIPValidationException">Thrown if a parsing error was detected.</exception>
    private static void ParseAllHeaders(string[] headersCollection, SIPHeader sipHeader)
    {
        string lastHeader = null;

        for (int lineIndex = 0; lineIndex < headersCollection.Length; lineIndex++)
        {
            string headerLine = headersCollection[lineIndex];

            if (string.IsNullOrWhiteSpace(headerLine) == true)
                // No point processing blank headers.
                continue;

            string headerName = null;
            string headerValue = null;

            // If the first character of a line is whitespace it's a contiuation of the previous line.
            if (headerLine.StartsWith(" "))
            {
                headerName = lastHeader;
                headerValue = headerLine.Trim();
            }
            else
            {
                headerLine = headerLine.Trim();
                int delimiterIndex = headerLine.IndexOf(SIPConstants.
                    HEADER_DELIMITER_CHAR);

                if (delimiterIndex == -1)
                    continue;

                headerName = headerLine.Substring(0, delimiterIndex).Trim();
                headerValue = headerLine.Substring(delimiterIndex + 1).Trim();
            }

            string headerNameLower = headerName.ToLower();

            try
            {
                if (headerNameLower == SIPHeaders.SIP_COMPACTHEADER_VIA ||
                    headerNameLower == SIPHeaders.SIP_HEADER_VIA.ToLower())
                    ParseViaHeaders(headerValue, sipHeader);
                else if (headerNameLower == SIPHeaders.SIP_COMPACTHEADER_CALLID ||
                    headerNameLower == SIPHeaders.SIP_HEADER_CALLID.ToLower())
                    sipHeader.CallId = headerValue;
                else if (headerNameLower == SIPHeaders.SIP_HEADER_CSEQ.ToLower())
                    ParseCSeq(headerValue, sipHeader);
                else if (headerNameLower == SIPHeaders.SIP_HEADER_EXPIRES.ToLower())
                {
                    if (int.TryParse(headerValue, out sipHeader.Expires) == false)
                        // 10 Nov 22 PHR
                        throw new SIPValidationException(SIPValidationFieldsEnum.Expires,
                            "The Expires header is not valid");
                }
                else if (headerNameLower == SIPHeaders.SIP_HEADER_MINEXPIRES.ToLower())
                {
                    if (int.TryParse(headerValue, out sipHeader.MinExpires) == false)
                    {   // Don't care because this header is not often used
                    }
                }
                else if (headerNameLower == SIPHeaders.SIP_COMPACTHEADER_CONTACT ||
                    headerNameLower == SIPHeaders.SIP_HEADER_CONTACT.ToLower())
                {
                    List<SIPContactHeader> contacts = SIPContactHeader.ParseContactHeader(headerValue);
                    if (contacts != null && contacts.Count > 0)
                        sipHeader.Contact.AddRange(contacts);
                }
                else if (headerNameLower == SIPHeaders.SIP_COMPACTHEADER_FROM ||
                     headerNameLower == SIPHeaders.SIP_HEADER_FROM.ToLower())
                    sipHeader.From = SIPFromHeader.ParseFromHeader(headerValue);
                else if (headerNameLower == SIPHeaders.SIP_COMPACTHEADER_TO ||
                    headerNameLower == SIPHeaders.SIP_HEADER_TO.ToLower())
                    sipHeader.To = SIPToHeader.ParseToHeader(headerValue);
                else if (headerNameLower == SIPHeaders.SIP_HEADER_WWWAUTHENTICATE.ToLower())
                    sipHeader.AuthenticationHeader = SIPAuthenticationHeader.
                        ParseSIPAuthenticationHeader(SIPAuthorisationHeadersEnum.WWWAuthenticate,
                        headerValue);
                else if (headerNameLower == SIPHeaders.SIP_HEADER_AUTHORIZATION.ToLower())
                    sipHeader.AuthenticationHeader = SIPAuthenticationHeader.
                        ParseSIPAuthenticationHeader(SIPAuthorisationHeadersEnum.Authorize, 
                        headerValue);
                else if (headerNameLower == SIPHeaders.SIP_HEADER_PROXYAUTHENTICATION.ToLower())
                    sipHeader.AuthenticationHeader = SIPAuthenticationHeader.
                        ParseSIPAuthenticationHeader(SIPAuthorisationHeadersEnum.ProxyAuthenticate,
                        headerValue);
                else if (headerNameLower == SIPHeaders.SIP_HEADER_PROXYAUTHORIZATION.ToLower())
                    sipHeader.AuthenticationHeader = SIPAuthenticationHeader.
                        ParseSIPAuthenticationHeader(SIPAuthorisationHeadersEnum.ProxyAuthorization,
                        headerValue);
                else if (headerNameLower == SIPHeaders.SIP_HEADER_USERAGENT.ToLower())
                    sipHeader.UserAgent = headerValue;
                else if (headerNameLower == SIPHeaders.SIP_HEADER_MAXFORWARDS.ToLower())
                {
                    if (int.TryParse(headerValue, out sipHeader.MaxForwards) == false)
                        throw new SIPValidationException(SIPValidationFieldsEnum.MaxForwards,
                            "The Max-Forwards header is not valid");
                }
                else if (headerNameLower == SIPHeaders.SIP_COMPACTHEADER_CONTENTLENGTH ||
                    headerNameLower == SIPHeaders.SIP_HEADER_CONTENTLENGTH.ToLower())
                {
                    if (int.TryParse(headerValue, out sipHeader.ContentLength) == false)
                        throw new SIPValidationException(SIPValidationFieldsEnum.ContentLength,
                            "The Content-Length header is not valid");
                }
                else if (headerNameLower == SIPHeaders.SIP_COMPACTHEADER_CONTENTTYPE ||
                    headerNameLower == SIPHeaders.SIP_HEADER_CONTENTTYPE.ToLower())
                    sipHeader.ContentType = headerValue;
                else if (headerNameLower == SIPHeaders.SIP_HEADER_ACCEPT.ToLower())
                    sipHeader.Accept = headerValue;
                else if (headerNameLower == SIPHeaders.SIP_HEADER_ROUTE.ToLower())
                    ParseRoute(headerValue, sipHeader);
                else if (headerNameLower == SIPHeaders.SIP_HEADER_RECORDROUTE.ToLower())
                    ParseRecordRoute(headerValue, sipHeader);
                else if (headerNameLower == SIPHeaders.SIP_HEADER_ALLOW_EVENTS || 
                    headerNameLower == SIPHeaders.SIP_COMPACTHEADER_ALLOWEVENTS)
                    sipHeader.AllowEvents = headerValue;
                else if (headerNameLower == SIPHeaders.SIP_HEADER_EVENT.ToLower() || 
                    headerNameLower == SIPHeaders.SIP_COMPACTHEADER_EVENT)
                    sipHeader.Event = headerValue;
                else if (headerNameLower == SIPHeaders.SIP_HEADER_SUBSCRIPTION_STATE.ToLower())
                    sipHeader.SubscriptionState = headerValue;
                else if (headerNameLower == SIPHeaders.SIP_HEADER_TIMESTAMP.ToLower())
                    sipHeader.Timestamp = headerValue;
                else if (headerNameLower == SIPHeaders.SIP_HEADER_DATE.ToLower())
                    sipHeader.Date = headerValue;
                else if (headerNameLower == SIPHeaders.SIP_HEADER_REFERSUB.ToLower())
                {
                    if (sipHeader.ReferSub == null)
                        sipHeader.ReferSub = headerValue;
                    else
                        throw new SIPValidationException(SIPValidationFieldsEnum.ReferToHeader,
                            "Only a single Refer-Sub header is permitted.");
                }
                else if (headerNameLower == SIPHeaders.SIP_HEADER_REFERTO.ToLower() ||
                    headerNameLower == SIPHeaders.SIP_COMPACTHEADER_REFERTO)
                {
                    if (sipHeader.ReferTo == null)
                        sipHeader.ReferTo = headerValue;
                    else
                        throw new SIPValidationException(SIPValidationFieldsEnum.ReferToHeader,
                            "Only a single Refer-To header is permitted.");
                }
                else if (headerNameLower == SIPHeaders.SIP_HEADER_REFERREDBY.ToLower())
                    sipHeader.ReferredBy = headerValue;
                else if (headerNameLower == SIPHeaders.SIP_HEADER_REQUIRE.ToLower())
                    sipHeader.Require = headerValue;
                else if (headerNameLower == SIPHeaders.SIP_HEADER_RESOURCE_PRIORITY.ToLower())
                    sipHeader.ResourcePriority = headerValue;
                else if (headerNameLower == SIPHeaders.SIP_HEADER_REASON.ToLower())
                    sipHeader.Reason = headerValue;
                else if (headerNameLower == SIPHeaders.SIP_COMPACTHEADER_SUPPORTED ||
                    headerNameLower == SIPHeaders.SIP_HEADER_SUPPORTED.ToLower())
                    sipHeader.Supported = headerValue;
                else if (headerNameLower == SIPHeaders.SIP_HEADER_AUTHENTICATIONINFO.ToLower())
                    sipHeader.AuthenticationInfo = headerValue;
                else if (headerNameLower == SIPHeaders.SIP_HEADER_ACCEPTENCODING.ToLower())
                    sipHeader.AcceptEncoding = headerValue;
                else if (headerNameLower == SIPHeaders.SIP_HEADER_ACCEPTLANGUAGE.ToLower())
                    sipHeader.AcceptLanguage = headerValue;
                else if (headerNameLower == SIPHeaders.SIP_HEADER_ALERTINFO.ToLower())
                    sipHeader.AlertInfo = headerValue;
                else if (headerNameLower == SIPHeaders.SIP_HEADER_ALLOW.ToLower())
                    sipHeader.Allow = headerValue;
                else if (headerNameLower == SIPHeaders.SIP_HEADER_CALLINFO.ToLower())
                {
                    List<SIPCallInfoHeader> CiList = SIPCallInfoHeader.ParseCallInfoHeader(headerValue);
                    if (CiList != null && CiList.Count > 0)
                        sipHeader.CallInfo.AddRange(CiList);
                }
                else if (headerNameLower == SIPHeaders.SIP_HEADER_PAI.ToLower())
                    sipHeader.PAssertedIdentity = SIPPaiHeader.ParseFromHeader(headerValue);
                else if (headerNameLower == SIPHeaders.SIP_HEADER_PPI.ToLower())
                    sipHeader.PPreferredIdentity = SIPPpiHeader.ParseFromHeader(headerValue);
                else if (headerNameLower == SIPHeaders.SIP_HEADER_GEOLOCATION.ToLower())
                {
                    List<SIPGeolocationHeader> GeoHdrs = SIPGeolocationHeader.ParseGeolocationHeader(
                        headerValue);
                    if (GeoHdrs != null && GeoHdrs.Count > 0)
                        sipHeader.Geolocation.AddRange(GeoHdrs);
                }
                else if (headerNameLower == SIPHeaders.SIP_HEADER_GEOLOCATION_ROUTING.ToLower())
                    sipHeader.GeolocationRouting = headerValue;
                else if (headerNameLower == SIPHeaders.SIP_HEADER_GEOLOCATION_ERROR.ToLower())
                    sipHeader.GeolocationError = headerValue;
                else if (headerNameLower == SIPHeaders.SIP_HEADER_CONTENT_DISPOSITION.ToLower())
                    sipHeader.ContentDisposition = headerValue;
                else if (headerNameLower == SIPHeaders.SIP_HEADER_CONTENT_ENCODING.ToLower())
                    sipHeader.ContentEncoding = headerValue;
                else if (headerNameLower == SIPHeaders.SIP_HEADER_CONTENT_LANGUAGE.ToLower())
                    sipHeader.ContentLanguage = headerValue;
                else if (headerNameLower == SIPHeaders.SIP_HEADER_ERROR_INFO.ToLower())
                    sipHeader.ErrorInfo = headerValue;
                else if (headerNameLower == SIPHeaders.SIP_HEADER_IN_REPLY_TO.ToLower())
                    sipHeader.InReplyTo = headerValue;
                else if (headerNameLower == SIPHeaders.SIP_HEADER_MIME_VERSION.ToLower())
                    sipHeader.MIMEVersion = headerValue;
                else if (headerNameLower == SIPHeaders.SIP_HEADER_ORGANIZATION.ToLower())
                    sipHeader.Organization = headerValue;
                else if (headerNameLower == SIPHeaders.SIP_HEADER_PRIORITY.ToLower())
                    sipHeader.Priority = headerValue;
                else if (headerNameLower == SIPHeaders.SIP_HEADER_PROXY_REQUIRE.ToLower())
                    sipHeader.ProxyRequire = headerValue;
                else if (headerNameLower == SIPHeaders.SIP_HEADER_REPLY_TO.ToLower())
                    sipHeader.ReplyTo = headerValue;
                else if (headerNameLower == SIPHeaders.SIP_HEADER_RETRY_AFTER.ToLower())
                    sipHeader.RetryAfter = headerValue;
                else if (headerNameLower == SIPHeaders.SIP_HEADER_SUBJECT.ToLower())
                    sipHeader.Subject = headerValue;
                else if (headerNameLower == SIPHeaders.SIP_HEADER_SERVER.ToLower())
                    sipHeader.Server = headerValue;
                else if (headerNameLower == SIPHeaders.SIP_HEADER_UNSUPPORTED.ToLower())
                    sipHeader.Unsupported = headerValue;
                else if (headerNameLower == SIPHeaders.SIP_HEADER_WARNING.ToLower())
                    sipHeader.Warning = headerValue;
                else
                    sipHeader.UnknownHeaders.Add(headerLine);

                lastHeader = headerName;
            }
            catch (SIPValidationException)
            {
                throw;
            }
            catch (Exception)
            {
                throw new SIPValidationException(SIPValidationFieldsEnum.
                    Headers, "Unknown error parsing Header.");
            }
        } // end for
    }

    private static void ParseCSeq(string headerValue, SIPHeader sipHeader)
    {
        string[] cseqFields = headerValue.Split(' ');
        if (cseqFields == null || cseqFields.Length == 0)
        {
            throw new SIPValidationException(SIPValidationFieldsEnum.CSeq, "The CSeq header is not valid");
        }
        else
        {
            if (int.TryParse(cseqFields[0], out sipHeader.
                CSeq) == false)
            {   // Error -- The CSeq number is not valid
                throw new SIPValidationException(SIPValidationFieldsEnum.CSeq,
                    "The CSeq value is out of range");
            }

            if (cseqFields != null && cseqFields.Length > 1)
                sipHeader.CSeqMethod = SIPMethods.GetMethod(cseqFields[1]);
            else
                throw new SIPValidationException(SIPValidationFieldsEnum.CSeq,
                    "The CSeq header is not valid");
        }
    }

    private static void ParseViaHeaders(string headerValue, SIPHeader sipHeader)
    {
        SIPViaHeader[] viaHeaders = SIPViaHeader.ParseSIPViaHeader(headerValue);

        if (viaHeaders != null && viaHeaders.Length > 0)
        {
            foreach (SIPViaHeader viaHeader in viaHeaders)
                sipHeader.Vias.AddBottomViaHeader(viaHeader);
        }
    }

    private static void ParseRoute(string headerValue, SIPHeader sipHeader)
    {
        SIPRouteSet routeSet = SIPRouteSet.ParseSIPRouteSet(
            headerValue);
        if (routeSet != null)
        {
            while (routeSet.Length > 0)
            {
                sipHeader.Routes.AddBottomRoute(routeSet.
                    PopRoute());
            }
        }
    }

    private static void ParseRecordRoute(string headerValue, SIPHeader sipHeader)
    {
        SIPRouteSet recordRouteSet = SIPRouteSet.ParseSIPRouteSet(headerValue);
        if (recordRouteSet != null)
        {
            while (recordRouteSet.Length > 0)
            {
                sipHeader.RecordRoutes.AddBottomRoute(recordRouteSet.PopRoute());
            }
        }
    }
    /// <summary>
    /// Puts the SIP headers together into a string ready for transmission.
    /// </summary>
    /// <returns>String representing the SIP headers.</returns>
    public new string ToString()
    {
        try
        {
            StringBuilder headersBuilder = new StringBuilder();
            headersBuilder.Append(Vias.ToString());

            string cseqField = null;
            if (this.CSeq != 0)
                cseqField = (this.CSeqMethod != SIPMethodsEnum.NONE) ? 
                    CSeq + " " + CSeqMethod.ToString() : CSeq.ToString();

            headersBuilder.Append((To != null) ? SIPHeaders.SIP_HEADER_TO + 
                ": " + To.ToString() + CRLF : null);
            headersBuilder.Append((From != null) ? SIPHeaders.SIP_HEADER_FROM + 
                ": " + From.ToString() + CRLF : null);
            headersBuilder.Append((CallId != null) ? SIPHeaders.
                SIP_HEADER_CALLID + ": " + CallId + CRLF : null);
            headersBuilder.Append((CSeq > 0) ? SIPHeaders.SIP_HEADER_CSEQ + 
                ": " + cseqField + CRLF : null);

            #region Appending Contact header.

            if (Contact != null && Contact.Count == 1)
            {
                headersBuilder.Append(SIPHeaders.SIP_HEADER_CONTACT + ": " + 
                    Contact[0].ToString() + CRLF);
            }
            else if (Contact != null && Contact.Count > 1)
            {
                StringBuilder contactsBuilder = new StringBuilder();
                contactsBuilder.Append(SIPHeaders.SIP_HEADER_CONTACT + ": ");

                bool firstContact = true;
                foreach (SIPContactHeader contactHeader in Contact)
                {
                    if (firstContact)
                        contactsBuilder.Append(contactHeader.ToString());
                    else
                        contactsBuilder.Append("," + contactHeader.ToString());

                    firstContact = false;
                }

                headersBuilder.Append(contactsBuilder.ToString() + CRLF);
            }

            #endregion

            headersBuilder.Append((MaxForwards >= 0) ? SIPHeaders.
                SIP_HEADER_MAXFORWARDS + ": " + MaxForwards + CRLF : null);
            headersBuilder.Append((Routes != null && Routes.Length > 0) ? 
                SIPHeaders.SIP_HEADER_ROUTE + ": " + Routes.ToString() + 
                CRLF : null);
            headersBuilder.Append((RecordRoutes != null && RecordRoutes.
                Length > 0) ? SIPHeaders.SIP_HEADER_RECORDROUTE + ": " + 
                RecordRoutes.ToString() + CRLF : null);
            headersBuilder.Append((UserAgent != null && UserAgent.Trim().
                Length != 0) ? SIPHeaders.SIP_HEADER_USERAGENT + ": " + 
                UserAgent + CRLF : null);
            headersBuilder.Append((Expires != -1) ? SIPHeaders.
                SIP_HEADER_EXPIRES + ": " + Expires + CRLF : null);
            headersBuilder.Append((MinExpires != -1) ? SIPHeaders.
                SIP_HEADER_MINEXPIRES + ": " + MinExpires + CRLF : null);
            headersBuilder.Append((Accept != null) ? SIPHeaders.
                SIP_HEADER_ACCEPT + ": " + Accept + CRLF : null);
            headersBuilder.Append((AcceptEncoding != null) ? SIPHeaders.
                SIP_HEADER_ACCEPTENCODING + ": " + AcceptEncoding + CRLF : 
                null);
            headersBuilder.Append((AcceptLanguage != null) ? SIPHeaders.
                SIP_HEADER_ACCEPTLANGUAGE + ": " + AcceptLanguage + 
                CRLF : null);
            headersBuilder.Append((Allow != null) ? SIPHeaders.
                SIP_HEADER_ALLOW + ": " + Allow + CRLF : null);
            headersBuilder.Append((AlertInfo != null) ? SIPHeaders.
                SIP_HEADER_ALERTINFO + ": " + AlertInfo + CRLF : null);
            headersBuilder.Append((AuthenticationInfo != null) ? SIPHeaders.
                SIP_HEADER_AUTHENTICATIONINFO + ": " + AuthenticationInfo + 
                CRLF : null);
            headersBuilder.Append((AuthenticationHeader != null) ? 
                AuthenticationHeader.ToString() + CRLF : null);

            if (PAssertedIdentity != null)
                headersBuilder.AppendFormat("{0}: {1}{2}", SIPHeaders.
                    SIP_HEADER_PAI, PAssertedIdentity.ToString(), CRLF);
            if (PPreferredIdentity != null)
                headersBuilder.AppendFormat("{0}: {1}{2}", SIPHeaders.
                    SIP_HEADER_PPI, PPreferredIdentity.ToString(), CRLF);

            foreach (SIPCallInfoHeader Sci in CallInfo)
            {
                headersBuilder.AppendFormat("{0}: {1}{2}", SIPHeaders.SIP_HEADER_CALLINFO, Sci.ToString(),
                    CRLF);
            }

            // Unknown SIP headers
            foreach (string unknownHeader in UnknownHeaders)
            {
                headersBuilder.Append(unknownHeader + CRLF);
            }

            foreach (SIPGeolocationHeader Sgh in Geolocation)
            {
                headersBuilder.AppendFormat("{0}: {1}{2}", SIPHeaders.SIP_HEADER_GEOLOCATION, 
                    Sgh.ToString(), CRLF);
            }

            if (string.IsNullOrEmpty(GeolocationRouting) == false)
                headersBuilder.AppendFormat("{0}: {1}{2}", SIPHeaders.SIP_HEADER_GEOLOCATION_ROUTING, 
                    GeolocationRouting, CRLF);

            if (string.IsNullOrEmpty(GeolocationError) == false)
                headersBuilder.AppendFormat("{0}: {1}{2}", SIPHeaders.SIP_HEADER_GEOLOCATION_ERROR,
                    GeolocationError, CRLF);

            headersBuilder.Append((ContentDisposition != null) ? SIPHeaders.
                SIP_HEADER_CONTENT_DISPOSITION + ": " + ContentDisposition + CRLF : null);
            headersBuilder.Append((ContentEncoding != null) ? SIPHeaders.
                SIP_HEADER_CONTENT_ENCODING + ": " + ContentEncoding + CRLF : null);
            headersBuilder.Append((ContentLanguage != null) ? SIPHeaders.
                SIP_HEADER_CONTENT_LANGUAGE + ": " + ContentLanguage + CRLF : null);
            headersBuilder.Append((Date != null) ? SIPHeaders.SIP_HEADER_DATE + 
                ": " + Date + CRLF : null);
            headersBuilder.Append((ErrorInfo != null) ? SIPHeaders.
                SIP_HEADER_ERROR_INFO + ": " + ErrorInfo + CRLF : null);
            headersBuilder.Append((InReplyTo != null) ? SIPHeaders.
                SIP_HEADER_IN_REPLY_TO + ": " + InReplyTo + CRLF : null);
            headersBuilder.Append((Organization != null) ? SIPHeaders.
                SIP_HEADER_ORGANIZATION + ": " + Organization + CRLF : null);
            headersBuilder.Append((Priority != null) ? SIPHeaders.
                SIP_HEADER_PRIORITY + ": " + Priority + CRLF : null);
            headersBuilder.Append((ProxyRequire != null) ? SIPHeaders.
                SIP_HEADER_PROXY_REQUIRE + ": " + this.ProxyRequire + 
                CRLF : null);
            headersBuilder.Append((ReplyTo != null) ? SIPHeaders.
                SIP_HEADER_REPLY_TO + ": " + ReplyTo + CRLF : null);
            headersBuilder.Append((Require != null) ? SIPHeaders.
                SIP_HEADER_REQUIRE + ": " + Require + CRLF : null);
            headersBuilder.Append((ResourcePriority != null) ?
                SIPHeaders.SIP_HEADER_RESOURCE_PRIORITY + ": " +
                ResourcePriority + CRLF : null);

            headersBuilder.Append((RetryAfter != null) ? SIPHeaders.
                SIP_HEADER_RETRY_AFTER + ": " + RetryAfter + CRLF : null);
            headersBuilder.Append((Server != null && Server.Trim().
                Length != 0) ? SIPHeaders.SIP_HEADER_SERVER + ": " + 
                Server + CRLF : null);
            headersBuilder.Append((Subject != null) ? SIPHeaders.
                SIP_HEADER_SUBJECT + ": " + Subject + CRLF : null);
            headersBuilder.Append((Supported != null) ? SIPHeaders.
                SIP_HEADER_SUPPORTED + ": " + Supported + CRLF : null);
            headersBuilder.Append((Timestamp != null) ? SIPHeaders.
                SIP_HEADER_TIMESTAMP + ": " + Timestamp + CRLF : null);
            headersBuilder.Append((Unsupported != null) ? SIPHeaders.
                SIP_HEADER_UNSUPPORTED + ": " + Unsupported + CRLF : null);
            headersBuilder.Append((Warning != null) ? SIPHeaders.
                SIP_HEADER_WARNING + ": " + Warning + CRLF : null);
            headersBuilder.Append(SIPHeaders.SIP_HEADER_CONTENTLENGTH + ": " + 
                ContentLength + CRLF);
            if (this.ContentType != null && this.ContentType.Trim().Length > 0)
                headersBuilder.Append(SIPHeaders.SIP_HEADER_CONTENTTYPE + ": " + 
                    ContentType + CRLF);

            // Non-core SIP headers.
            headersBuilder.Append((AllowEvents != null) ? SIPHeaders.
                SIP_HEADER_ALLOW_EVENTS + ": " + AllowEvents + CRLF : null);
            headersBuilder.Append((Event != null) ? SIPHeaders.SIP_HEADER_EVENT +
                ": " + Event + CRLF : null);
            headersBuilder.Append((SubscriptionState != null) ? SIPHeaders.
                SIP_HEADER_SUBSCRIPTION_STATE + ": " + SubscriptionState + 
                CRLF : null);
            headersBuilder.Append((ReferSub != null) ? SIPHeaders.
                SIP_HEADER_REFERSUB + ": " + ReferSub + CRLF : null);
            headersBuilder.Append((ReferTo != null) ? SIPHeaders.
                SIP_HEADER_REFERTO + ": " + ReferTo + CRLF : null);
            headersBuilder.Append((ReferredBy != null) ? SIPHeaders.
                SIP_HEADER_REFERREDBY + ": " + ReferredBy + CRLF : null);
            headersBuilder.Append((Reason != null) ? SIPHeaders.
                SIP_HEADER_REASON + ": " + Reason + CRLF : null);
            
            return headersBuilder.ToString();
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// Validates the this SIPHeader object.
    /// </summary>
    /// <exception cref="SIPValidationException"></exception>
    private void Validate()
    {
        if (Vias == null || Vias.Length == 0)
        {
            throw new SIPValidationException(SIPValidationFieldsEnum.
                ViaHeader, "Invalid header, no Via.");
        }
    }

    /// <summary>
    /// Sets the Date header field to the current time.
    /// </summary>
    public void SetDateHeader()
    {
        Date = DateTime.UtcNow.ToString("ddd, dd MMM yyyy HH:mm:ss ") + "GMT";
    }

    /// <summary>
    /// Creates a deep copy of this SIPHeader object.
    /// </summary>
    /// <returns></returns>
    public SIPHeader Copy()
    {
        string headerString = this.ToString();
        string[] sipHeaders = SIPHeader.SplitHeaders(headerString);
        return ParseSIPHeaders(sipHeaders);
    }

    /// <summary>
    /// Unknown SIP headers are put into the UnknownHeaders member of this object by the ParseSIPHeaders()
    /// function. Each string is this string array contains the whole header line (for example:
    /// UnknownHeader: value).
    /// This function retrieves the entire header line given the input header name.
    /// </summary>
    /// <remarks>
    /// This function assumes that there will be only a single header line for a given header name.
    /// </remarks>
    /// <param name="unknownHeaderName">Name of the unknown SIP Header</param>
    /// <returns>Returns null if the header is not found or the complete header line if it exists.</returns>
    public string? GetUnknownHeaderValue(string unknownHeaderName)
    {
        if (string.IsNullOrEmpty(unknownHeaderName) == true)
            return null;
        else if (UnknownHeaders == null || UnknownHeaders.Count == 0)
            return null;
        else
        {
            foreach (string unknonwHeader in UnknownHeaders)
            {
                string trimmedHeader = unknonwHeader.Trim();
                int delimiterIndex = trimmedHeader.IndexOf(SIPConstants.HEADER_DELIMITER_CHAR);

                if (delimiterIndex == -1)
                    continue;

                string headerName = trimmedHeader.Substring(0, delimiterIndex).Trim();

                if (headerName.ToLower() == unknownHeaderName.ToLower())
                    return trimmedHeader.Substring(delimiterIndex + 1).Trim();
            }

            return null;
        }
    }
}
