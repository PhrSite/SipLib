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

/// <bnf>
/// From            =  ( "From" / "f" ) HCOLON from-spec
/// from-spec       =  ( name-addr / addr-spec ) *( SEMI from-param )
/// from-param      =  tag-param / generic-param
/// name-addr		=  [ display-name ] LAQUOT addr-spec RAQUOT
/// addr-spec		=  SIP-URI / SIPS-URI / absoluteURI
/// tag-param       =  "tag" EQUAL token
/// generic-param   =  token [ EQUAL gen-value ]
/// gen-value       =  token / host / quoted-string
/// </bnf>
/// <summary>
/// The From header only has parameters, no headers. Parameters of from ...;name=value;name2=value2.
/// Specific parameters: tag.
/// </summary>
public class SIPFromHeader
{
    private const string DEFAULT_FROM_URI = SIPConstants.SIP_DEFAULT_FROMURI;
    private const string PARAMETER_TAG = SIPHeaderAncillary.SIP_HEADERANC_TAG;

    /// <summary>
    /// Gets or sets the name field of the From header value
    /// </summary>
    /// <value></value>
    public string? FromName
    {
        get { return m_userField?.Name; }
        set { m_userField.Name = value; }
    }

    /// <summary>
    /// Gets or sets the SIPURI portion of the From header value
    /// </summary>
    /// <value></value>
    public SIPURI? FromURI
    {
        get { return m_userField.URI; }
        set { m_userField.URI = value; }
    }

    /// <summary>
    /// Gets or sets the From tag parameter value
    /// </summary>
    /// <value></value>
    public string? FromTag
    {
        get { return FromParameters.Get(PARAMETER_TAG); }
        set
        {
            if (value != null && value.Trim().Length > 0)
                FromParameters.Set(PARAMETER_TAG, value);
            else
            {
                if (FromParameters.Has(PARAMETER_TAG))
                    FromParameters.Remove(PARAMETER_TAG);
            }
        }
    }

    /// <summary>
    /// Gets or sets the From header parameters
    /// </summary>
    /// <value></value>
    public SIPParameters FromParameters
    {
        get { return m_userField.Parameters; }
        set { m_userField.Parameters = value; }
    }

    private SIPUserField m_userField = new SIPUserField();

    /// <summary>
    /// Gets or or sets the SIPUserField object for the From header
    /// </summary>
    /// <value></value>
    public SIPUserField FromUserField
    {
        get { return m_userField; }
        set { m_userField = value; }
    }

    private SIPFromHeader()
    { }

    /// <summary>
    /// Constructs a new From header object from a name, a SIPURI and a From tag value.
    /// </summary>
    /// <param name="fromName">Name field for the From header. Optional.</param>
    /// <param name="fromURI">SIPURI to build the From header from.</param>
    /// <param name="fromTag">From header tag parameter</param>
    public SIPFromHeader(string fromName, SIPURI fromURI, string fromTag)
    {
        m_userField = new SIPUserField(fromName, fromURI, null!);
        FromTag = fromTag;
    }

    /// <summary>
    /// Parses a string into a SIPFromHeader object.
    /// </summary>
    /// <param name="fromHeaderStr">Input string</param>
    /// <returns>Returns a SIPFromHeader object.</returns>
    // <exception cref="SIPValidationException"></exception>
    public static SIPFromHeader ParseFromHeader(string fromHeaderStr)
    {
        try
        {
            SIPFromHeader fromHeader = new SIPFromHeader();
            fromHeader.m_userField = SIPUserField.ParseSIPUserField(fromHeaderStr);

            return fromHeader;
        }
        catch (ArgumentException argExcp)
        {
            throw new SIPValidationException(SIPValidationFieldsEnum.FromHeader,
                argExcp.Message);
        }
        catch
        {
            throw new SIPValidationException(SIPValidationFieldsEnum.FromHeader,
                "The SIP From header was invalid.");
        }
    }

    /// <summary>
    /// Converts this object into a From header value string
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return m_userField.ToString();
    }
}

