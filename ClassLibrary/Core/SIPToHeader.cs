#region License
//-----------------------------------------------------------------------------
// Filename: SIPToHeader.cs
//
// Description: SIP To Header.
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
/// To				=  ( "To" / "t" ) HCOLON ( name-addr / addr-spec ) *
/// ( SEMI to-param )
/// to-param		=  tag-param / generic-param
/// name-addr		=  [ display-name ] LAQUOT addr-spec RAQUOT
/// addr-spec		=  SIP-URI / SIPS-URI / absoluteURI
/// tag-param       =  "tag" EQUAL token
/// generic-param   =  token [ EQUAL gen-value ]
/// gen-value       =  token / host / quoted-string
/// </bnf>
/// 
/// <summary>
/// Class for handling a SIP To header
/// </summary>
/// <remarks>
/// The To header only has parameters, no headers. Parameters of from ...;name=value;name2=value2.
/// Specific parameters: tag.
/// </remarks>
public class SIPToHeader
{
    private const string PARAMETER_TAG = SIPHeaderAncillary.SIP_HEADERANC_TAG;

    /// <summary>
    /// Gets or sets the name field of the To header
    /// </summary>
    /// <value></value>
    public string? ToName
    {
        get { return m_userField.Name; }
        set { m_userField.Name = value; }
    }

    /// <summary>
    /// Gets or sets the URI of the To header
    /// </summary>
    /// <value></value>
    public SIPURI? ToURI
    {
        get { return m_userField.URI; }
        set { m_userField.URI = value; }
    }

    /// <summary>
    /// Gets or sets the To header tag value
    /// </summary>
    /// <value></value>
    public string? ToTag
    {
        get { return ToParameters.Get(PARAMETER_TAG); }
        set
        {
            if (value != null && value.Trim().Length > 0)
                ToParameters.Set(PARAMETER_TAG, value);
            else
            { 
                if (ToParameters.Has(PARAMETER_TAG))
                    ToParameters.Remove(PARAMETER_TAG);
            }
        }
    }

    /// <summary>
    /// Gets or sets the To header parameters
    /// </summary>
    /// <value></value>
    public SIPParameters ToParameters
    {
        get { return m_userField.Parameters; }
        set { m_userField.Parameters = value; }
    }

    private SIPUserField? m_userField;

    /// <summary>
    /// Gets or sets the SIPUserField for the To header
    /// </summary>
    /// <value></value>
    public SIPUserField? ToUserField
    {
        get { return m_userField; }
        set { m_userField = value; }
    }

    private SIPToHeader()
    { }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="toName">Name field for the To header. Optional</param>
    /// <param name="toURI">SIPURI to build the To header from</param>
    /// <param name="toTag">To header tag. Optional initially.</param>
    public SIPToHeader(string? toName, SIPURI toURI, string? toTag)
    {
        m_userField = new SIPUserField(toName, toURI, null);
        ToTag = toTag;
    }

    /// <summary>
    /// Parses a string into a SIPToHeader object.
    /// </summary>
    /// <param name="toHeaderStr">Input string</param>
    /// <returns>Returns a new SIPToHeader</returns>
    // <exception cref="SIPValidationException"></exception>
    public static SIPToHeader ParseToHeader(string toHeaderStr)
    {
        try
        {
            SIPToHeader toHeader = new SIPToHeader();
            toHeader.m_userField = SIPUserField.ParseSIPUserField(toHeaderStr);
            return toHeader;
        }
        catch (ArgumentException argExcp)
        {
            throw new SIPValidationException(SIPValidationFieldsEnum.ToHeader,
                argExcp.Message);
        }
        catch
        {
            throw new SIPValidationException(SIPValidationFieldsEnum.ToHeader,
                "The SIP To header was invalid.");
        }
    }

    /// <summary>
    /// Converts this SIPToHeader object into a header value string
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return m_userField.ToString();
    }
}
