#region License
//-----------------------------------------------------------------------------
// Filename: SIPUserField.cs
//
// Description: 
// Encapsulates the format for the SIP Contact, From and To headers
//
// History:
// 21 Apr 2006	Aaron Clauson	Created.
// 04 Sep 2008  Aaron Clauson   Changed display name to always use quotes. Some SIP stacks were
//                              found to have porblems with a comma in a non-quoted display name.
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
//	Revised:	7 Nov 22 PHR -- Initial version. Added some documentation comments.
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Core;

// name-addr      =  [ display-name ] LAQUOT addr-spec RAQUOT
// addr-spec      =  SIP-URI / SIPS-URI / absoluteURI
// SIP-URI          =  "sip:" [ userinfo ] hostport
// uri-parameters [ headers ]
// SIPS-URI         =  "sips:" [ userinfo ] hostport
// uri-parameters [ headers ]
// userinfo         =  ( user / telephone-subscriber ) [ ":" password ] "@"
//

/// <summary>
/// Class for parsing and building the user field of a SIP URI.
/// </summary>
public class SIPUserField
{
    private const char PARAM_TAG_DELIMITER = ';';

    /// <summary>
    /// Name portion of the user field for a header
    /// </summary>
    /// <value></value>
    public string? Name = null;

    /// <summary>
    /// SIPURI portion of the header user field
    /// </summary>
    /// <value></value>
    public SIPURI? URI = null;

    /// <summary>
    /// Header parameters
    /// </summary>
    /// <value></value>
    public SIPParameters Parameters = new SIPParameters(null, PARAM_TAG_DELIMITER);

    /// <summary>
    /// Default constructor
    /// </summary>
    public SIPUserField()
    { }

    /// <summary>
    /// Constructs a new SIPUserField from the user name, a SIPURI and the parameters and headers.
    /// </summary>
    /// <param name="name">The User name. Optional.</param>
    /// <param name="uri">A valid SIPURI.</param>
    /// <param name="paramsAndHeaders">A string containing the parameters and
    /// embedded headers. Optional.</param>
    public SIPUserField(string? name, SIPURI uri, string? paramsAndHeaders)
    {
        Name = name;
        URI = uri;

        Parameters = new SIPParameters(paramsAndHeaders, PARAM_TAG_DELIMITER);
    }

    /// <summary>
    /// Tests a string to determine if it contains a valid SIPUserField.
    /// </summary>
    /// <param name="userFieldStr">Input string</param>
    /// <returns>Returns a new SIPUserField object if successful or null if a parsing error occurred.
    /// </returns>
    public static SIPUserField? TryParseSIPUserField(string userFieldStr)
    {
        SIPUserField Suf = null;
        try
        {
            Suf = ParseSIPUserField(userFieldStr);
        }
        catch (ArgumentException) { }
        catch (SIPValidationException) { }
        catch (Exception) { }

        return Suf;
    }

    /// <summary>
    /// Parses a string into a SIPUriField. Throws exceptions if parsing errors are detected.
    /// </summary>
    /// <param name="userFieldStr">Input string to parse.</param>
    /// <returns>Returns a valid SIPUserField.</returns>
    // <exception cref="ArgumentException"></exception>
    // <exception cref="SIPValidationException"></exception>
    public static SIPUserField ParseSIPUserField(string userFieldStr)
    {
        if (string.IsNullOrEmpty(userFieldStr) == true)
            throw new ArgumentException("A SIPUserField cannot be parsed from an empty string.");

        SIPUserField userField = new SIPUserField();
        string trimUserField = userFieldStr.Trim();

        int position = trimUserField.IndexOf('<');

        if (position == -1)
        {   // Treat the field as a URI only, except that all parameters are Header parameters and
            // not URI parameters (RFC3261 section 20.39 which refers to 20.10 for parsing rules).
            string uriStr = trimUserField;
            int paramDelimPosn = trimUserField.IndexOf(PARAM_TAG_DELIMITER);

            if (paramDelimPosn != -1)
            {
                string paramStr = trimUserField.Substring(paramDelimPosn + 1).Trim();
                userField.Parameters = new SIPParameters(paramStr, PARAM_TAG_DELIMITER);
                uriStr = trimUserField.Substring(0, paramDelimPosn);
            }

            userField.URI = SIPURI.ParseSIPURI(uriStr);
        }
        else
        {
            if (position > 0)
            {
                userField.Name = trimUserField.Substring(0, position).Trim().Trim('"');
                trimUserField = trimUserField.Substring(position, trimUserField.Length - position);
            }

            int addrSpecLen = trimUserField.Length;
            position = trimUserField.IndexOf('>');
            if (position != -1)
            {
                addrSpecLen = trimUserField.Length - 1;
                if (position != -1)
                {
                    addrSpecLen = position - 1;

                    string paramStr = trimUserField.Substring(position + 1).Trim();
                    userField.Parameters = new SIPParameters(paramStr, PARAM_TAG_DELIMITER);
                }

                string addrSpec = trimUserField.Substring(1, addrSpecLen);
                userField.URI = SIPURI.ParseSIPURI(addrSpec);
            }
            else
                throw new SIPValidationException(SIPValidationFieldsEnum.ContactHeader,
                    "A SIPUserField was missing the right quote, " + userFieldStr + ".");
        }

        return userField;
    }

    /// <summary>
    /// Converts this object into a string.
    /// </summary>
    /// <returns>The string version of this object.</returns>
    // <exception cref="NullReferenceException">Thrown if the URI field is null </exception>
    // <exception cref="Exception">Thrown if an unexpected error occurs.</exception>
    public override string ToString()
    {
        try
        {
            string userFieldStr = null;

            if (Name != null)
                userFieldStr = "\"" + Name + "\" ";

            if (URI! == null!)
                throw new NullReferenceException("The URI field is null");

            userFieldStr += "<" + URI.ToString() + ">" + Parameters.ToString();

            return userFieldStr;
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// Formats the string for use in the header value of a CPIM message. The difference is that if
    /// the name portion is present, it is not quoted.
    /// </summary>
    /// <returns>Returns the string formatted for usin in a CPIM message.</returns>
    /// <exception cref="NullReferenceException"></exception>
    public string ToCpimFormatString()
    {
        string userFieldStr = null;

        if (Name != null)
            userFieldStr = Name + " ";

        if (URI! == null!)
            throw new NullReferenceException("The URI field is null");

        userFieldStr += "<" + URI.ToString() + ">" + Parameters.ToString();
        return userFieldStr;
    }

    /// <summary>
    /// Converts this object into a string containing a SIPURI that contains no parameters.
    /// </summary>
    /// <returns>The SIPURI portion only with no parameters.</returns>
    // <exception cref="NullReferenceException">Thrown if the SIPURI is null.</exception>
    // <exception cref="Exception">Thrown if an unexpected error occured.</exception>
    public string ToParameterlessString()
    {
        try
        {
            string userFieldStr = null;

            if (Name != null)
                userFieldStr = "\"" + Name + "\" ";

            if (URI! == null!)
                throw new NullReferenceException("The URI field is null");

            userFieldStr += "<" + URI.ToParameterlessString() + ">";

            return userFieldStr;
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// Creates a deep copy of this object.
    /// </summary>
    /// <returns></returns>
    public SIPUserField CopyOf()
    {
        SIPUserField copy = new SIPUserField();
        copy.Name = Name;
        if (URI! == null!)
            copy.URI = null;
        else
            copy.URI = URI.CopyOf();

        copy.Parameters = Parameters.CopyOf();

        return copy;
    }
}
