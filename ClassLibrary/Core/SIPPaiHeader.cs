#region License
//-----------------------------------------------------------------------------
// Filename: SIPPaiHeader.cs
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
/// P-Asserted-Identity =  ( "P-Asserted-Identity" ) HCOLON pai-spec
/// pai-spec            =  name-addr / addr-spec
/// name-addr		    =  [ display-name ] LAQUOT addr-spec RAQUOT
/// addr-spec		    =  SIP-URI / SIPS-URI / absoluteURI
/// </bnf>
/// <summary>
/// Class for the SIP P-Asserted-Identity header.
/// </summary>
public class SIPPaiHeader
{
    /// <summary>
    /// Gets or sets the name field.
    /// </summary>
    public string Name
    {
        get { return m_userField.Name; }
        set { m_userField.Name = value; }
    }

    /// <summary>
    /// Gets or sets the SIPURI field
    /// </summary>
    public SIPURI URI
    {
        get { return m_userField.URI; }
        set { m_userField.URI = value; }
    }

    private SIPUserField m_userField = new SIPUserField();

    /// <summary>
    /// Gets or sets the SIPUserField
    /// </summary>
    public SIPUserField UserField
    {
        get { return m_userField; }
        set { m_userField = value; }
    }

    private SIPPaiHeader()
    { }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="PaiName">Name field of the header. Optional.</param>
    /// <param name="PaiURI">SIPURI to build the SIPPaiHeader from</param>
    public SIPPaiHeader(string PaiName, SIPURI PaiURI)
    {
        m_userField = new SIPUserField(PaiName, PaiURI, null);
    }

    /// <summary>
    /// Parses a string into a SIPPaiHeader object.
    /// </summary>
    /// <param name="PaiHeaderStr">Input string</param>
    /// <returns>Returns a new SIPPaiHeader object</returns>
    // <exception cref="SIPValidationException"></exception>
    public static SIPPaiHeader ParseFromHeader(string PaiHeaderStr)
    {
        try
        {
            SIPPaiHeader PaiHeader = new SIPPaiHeader();
            PaiHeader.m_userField = SIPUserField.ParseSIPUserField(PaiHeaderStr);
            return PaiHeader;
        }
        catch (ArgumentException argExcp)
        {
            throw new SIPValidationException(SIPValidationFieldsEnum.PAssertedIdentityHeader, 
                argExcp.Message);
        }
        catch
        {
            throw new SIPValidationException(SIPValidationFieldsEnum.
                PAssertedIdentityHeader,
                "The SIP P-Asserted-Identity header was invalid.");
        }
    }

    /// <summary>
    /// Converts this object into a P-Asserted-Identity header value
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return m_userField.ToString();
    }
}
