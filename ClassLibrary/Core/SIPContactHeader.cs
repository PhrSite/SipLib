#region License
//-----------------------------------------------------------------------------
// Filename: SIPContactHeader.cs
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
//  Revised:    10 Nov 22 PHR Moved here from SIPHeader.cs
/////////////////////////////////////////////////////////////////////////////////////

using System.Runtime.Serialization;

namespace SipLib.Core;

/// <bnf>
/// Contact        =  ("Contact" / "m" ) HCOLON ( STAR / (contact-param *(COMMA contact-param)))
/// contact-param  =  (name-addr / addr-spec) *(SEMI contact-params)
/// name-addr      =  [ display-name ] LAQUOT addr-spec RAQUOT
/// addr-spec      =  SIP-URI / SIPS-URI / absoluteURI
/// display-name   =  *(token LWS)/ quoted-string
///
/// contact-params     =  c-p-q / c-p-expires / contact-extension
/// c-p-q              =  "q" EQUAL qvalue
/// c-p-expires        =  "expires" EQUAL delta-seconds
/// contact-extension  =  generic-param
/// delta-seconds      =  1*DIGIT
/// generic-param  =  token [ EQUAL gen-value ]
/// gen-value      =  token / host / quoted-string
/// </bnf>
/// <remarks>
/// The Contact header only has parameters, no headers. Parameters of from ...;
/// name=value;name2=value2
/// Specific parameters: q, expires.
/// </remarks>
[DataContract]
public class SIPContactHeader
{
    private const string EXPIRES_PARAMETER_KEY = "expires";
    private const string QVALUE_PARAMETER_KEY = "q";

    //private static char[] m_nonStandardURIDelimChars = new char[] { '\n', 
    //'\r', ' ' };	// Characters that can delimit a SIP URI, supposed to be > 
    //but it is sometimes missing.

    private string? RawHeader = null;

    /// <summary>
    /// Gets or sets the Contact name field
    /// </summary>
    public string? ContactName
    {
        get { return m_userField.Name; }
        set { m_userField.Name = value; }
    }

    /// <summary>
    /// Gets or sets the Contact URI field
    /// </summary>
    public SIPURI? ContactURI
    {
        get { return m_userField.URI; }
        set { m_userField.URI = value; }
    }

    /// <summary>
    /// Gets or sets the Contact header URI parameters
    /// </summary>
    public SIPParameters ContactParameters
    {
        get { return m_userField.Parameters; }
        set { m_userField.Parameters = value; }
    }

    /// <summary>
    /// Gets or sets the Expires field. A value of -1 indicates the header did not contain an expires
    /// parameter setting.
    /// </summary>
    public int Expires
    {
        get
        {
            int expires = -1;

            if (ContactParameters.Has(EXPIRES_PARAMETER_KEY))
            {
                string expiresStr = ContactParameters.Get(EXPIRES_PARAMETER_KEY);
                if (string.IsNullOrEmpty(expiresStr) == false)
                    int.TryParse(expiresStr, out expires);
            }

            return expires;
        }
        set { ContactParameters.Set(EXPIRES_PARAMETER_KEY, value.ToString()); }
    }

    /// <summary>
    /// Gets or sets the Q value parameter
    /// </summary>
    public string? Q
    {
        get { return ContactParameters.Get(QVALUE_PARAMETER_KEY); }
        set { ContactParameters.Set(QVALUE_PARAMETER_KEY, string.Empty); }
    }

    private SIPUserField? m_userField;

    private SIPContactHeader()
    { }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="contactName">Name value for the Contact header value. Optional</param>
    /// <param name="contactURI">Contact header URI</param>
    public SIPContactHeader(string? contactName, SIPURI contactURI)
    {
        m_userField = new SIPUserField(contactName, contactURI, null!);
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="contactUserField">Input SIPUserField</param>
    public SIPContactHeader(SIPUserField contactUserField)
    {
        m_userField = contactUserField;
    }

    /// <summary>
    /// Parsed a Contact header value. Throws a SIPValidationException if an error is detected.
    /// </summary>
    /// <param name="contactHeaderStr">Input Contact header value</param>
    /// <returns>Returns a a list containg one or more SIPContactHeader objects</returns>
    // <exception cref="SIPValidationException"></exception>
    public static List<SIPContactHeader>? ParseContactHeader(string contactHeaderStr)
    {
        try
        {
            if (contactHeaderStr == null || contactHeaderStr.Trim().Length == 0)
                return null;

            string[] contactHeaders = SIPParameters.GetKeyValuePairsFromQuoted(
                contactHeaderStr, ',');

            List<SIPContactHeader> contactHeaderList = new
                List<SIPContactHeader>();

            if (contactHeaders != null)
            {
                foreach (string contactHeaderItemStr in contactHeaders)
                {
                    SIPContactHeader contactHeader = new SIPContactHeader();
                    contactHeader.RawHeader = contactHeaderStr;
                    contactHeader.m_userField = SIPUserField.ParseSIPUserField(
                        contactHeaderItemStr);
                    contactHeaderList.Add(contactHeader);
                }
            }

            return contactHeaderList;
        }
        catch (SIPValidationException)
        {
            throw;
        }
        catch (Exception)
        {
            throw new SIPValidationException(SIPValidationFieldsEnum.
                ContactHeader, "Contact header invalid.");
        }
    }

    /// <summary>
    /// Creates a new list of SIPContactHeader objects containing a single SIPContactHeader
    /// given a SIPURI object.
    /// </summary>
    /// <param name="sipURI">Input</param>
    /// <returns>Returns a single SIPContactHeader in a list.</returns>
    public static List<SIPContactHeader> CreateSIPContactList(SIPURI sipURI)
    {
        List<SIPContactHeader> contactHeaderList = new List<SIPContactHeader>();
        contactHeaderList.Add(new SIPContactHeader(null!, sipURI));

        return contactHeaderList;
    }

    /// <summary>
    /// Compares two contact headers to determine contact address equality.
    /// </summary>
    /// <returns>Returns true if the two SIPContact headers are equal</returns>
    public static bool AreEqual(SIPContactHeader contact1, SIPContactHeader
        contact2)
    {
        if (!SIPURI.AreEqual(contact1.ContactURI, contact2.ContactURI))
            return false;
        else
        {
            // Compare invaraiant parameters.
            string[] contact1Keys = contact1.ContactParameters.GetKeys();

            if (contact1Keys != null && contact1Keys.Length > 0)
            {
                foreach (string key in contact1Keys)
                {
                    if (key == EXPIRES_PARAMETER_KEY || key == QVALUE_PARAMETER_KEY)
                        continue;
                    else if (contact1.ContactParameters.Get(key) != contact2.ContactParameters.Get(key))
                        return false;
                }
            }

            // Need to do the reverse as well
            string[] contact2Keys = contact2.ContactParameters.GetKeys();

            if (contact2Keys != null && contact2Keys.Length > 0)
            {
                foreach (string key in contact2Keys)
                {
                    if (key == EXPIRES_PARAMETER_KEY || key == QVALUE_PARAMETER_KEY)
                        continue;
                    else if (contact2.ContactParameters.Get(key) != contact1.ContactParameters.Get(key))
                        return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Converts the SIPContactHeader to a string
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        if (m_userField.URI.Host == SIPConstants.SIP_REGISTER_REMOVEALL)
            return SIPConstants.SIP_REGISTER_REMOVEALL;
        else
            return m_userField.ToString();
    }

    /// <summary>
    /// Returns a deep copy of this SIPContactHeader object.
    /// </summary>
    /// <returns></returns>
    public SIPContactHeader CopyOf()
    {
        SIPContactHeader copy = new SIPContactHeader();
        copy.RawHeader = RawHeader;
        copy.m_userField = m_userField.CopyOf();

        return copy;
    }
}
