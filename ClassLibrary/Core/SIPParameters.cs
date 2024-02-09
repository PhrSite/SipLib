#region License
//-----------------------------------------------------------------------------
// Filename: SIPParameters.cs
//
// Description: SIP parameters as used in Contact, To, From and Via SIP headers.
//
// History:
// 06 May 2006	Aaron Clauson	Created.
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
//	Revised:	9 Nov 22 PHR -- Initial version.
//              -- Added operators == and !=
/////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace SipLib.Core;


// SIP URI with parameters:
// sip:1234@sip.com;key1=value1;key2=value2
// 
// SIP URI with headers:
// sip:1234@sip.com?key1=value1&key2=value2
// 
// SIP URI with parameters and headers (paramters always come first):
// sip:1234@sip.com;key1=value1;key2=value2?key1=value1&key2=value2

// BNF
// generic-param  =  token [ EQUAL gen-value ]
// gen-value      =  token / host / quoted-string

/// <summary>
/// Represents a series of name value pairs that are optionally included in SIP URIs and also as an
/// additional optional setting on some SIP Headers (Contact, To, From, Via). This class also treats
/// the header value of a SIP URI as a special case of a SIP parameter. The difference between a 
/// parameter and a SIP URI header is the start and delimiter characters used.
/// </summary>
public class SIPParameters
{
    private const char TAG_NAME_VALUE_SEPERATOR = '=';
    private const char QUOTE = '"';
    private const char BACK_SLASH = '\\';
    private const char DEFAULT_PARAMETER_DELIMITER = ';';

    private char TagDelimiter = DEFAULT_PARAMETER_DELIMITER;

    private Dictionary<string, string> m_dictionary = new Dictionary<string, 
        string>(StringComparer.CurrentCultureIgnoreCase);

    /// <summary>
    /// Gets the number of parameters
    /// </summary>
    /// <value></value>
    public int Count
    {
        get { return (m_dictionary != null) ? m_dictionary.Count : 0;  }
    }

    internal SIPParameters()
    {
        m_dictionary = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
    }

    /// <summary>
    /// Parses the name value pairs from a SIP parameter or header string.
    /// </summary>
    /// <param name="sipString">Input string containing the parameters</param>
    /// <param name="delimiter">Delimeter to use for parsing</param>
    public SIPParameters(string? sipString, char delimiter)
    {
        Initialise(sipString!, delimiter);
    }

    private void Initialise(string sipString, char delimiter)
    {
        TagDelimiter = delimiter;

        string[] keyValuePairs = GetKeyValuePairsFromQuoted(sipString, delimiter);

        if (keyValuePairs != null && keyValuePairs.Length > 0)
        {
            foreach (string keyValuePair in keyValuePairs)
            {
                AddKeyValuePair(keyValuePair, m_dictionary);
            }
        }   
    }

    /// <summary>
    /// Gets the key value pairs from a quoted string into a string array
    /// </summary>
    /// <param name="quotedString">Input quoted string</param>
    /// <param name="delimiter">Delimeter to use. For example ','</param>
    /// <returns>Returns a string array containing the name value pairs</returns>
    public static string[]? GetKeyValuePairsFromQuoted(string quotedString, char delimiter)
    {
        try
        {
            List<string> keyValuePairList = new List<string>();

            if (quotedString == null || quotedString.Trim().Length == 0)
                return null;
            else if(quotedString.IndexOf(delimiter) == -1)
                return new string[] {quotedString};
            else
            {
                int startParameterPosn = 0;
                int inParameterPosn = 0;
                bool inQuotedStr = false;

                while (inParameterPosn != -1 && inParameterPosn < quotedString.Length)
                {
                    inParameterPosn = quotedString.IndexOfAny(new char[] { 
                        delimiter, QUOTE }, inParameterPosn);

                    // Determine if the delimiter position represents the end of the parameter or is in
                    // a quoted string.
                    if (inParameterPosn != -1)
                    {
                        if (inParameterPosn <= startParameterPosn && 
                            quotedString[inParameterPosn] == delimiter)
                        {
                            // Initial or doubled up Parameter delimiter character, ignore and move on.
                            inQuotedStr = false;
                            inParameterPosn++;
                            startParameterPosn = inParameterPosn;
                        }
                        else if (quotedString[inParameterPosn] == QUOTE)
                        {
                            if (inQuotedStr && inParameterPosn > 0 && 
                                quotedString[inParameterPosn - 1] != BACK_SLASH)
                            {   // If in a quoted string and this quote has not been escaped close the
                                // quoted string.
                                inQuotedStr = false;
                            }
                            else if (inQuotedStr && inParameterPosn > 0 && 
                                quotedString[inParameterPosn - 1] == BACK_SLASH)
                            {
                                // Do nothing, quote has been escaped in a quoted string.
                            }
                            else if (!inQuotedStr)
                            {
                                // Start quoted string.
                                inQuotedStr = true;
                            }

                            inParameterPosn++;
                        }
                        else
                        {
                            if (!inQuotedStr)
                            {   // Parameter delimiter found and not in quoted string therefore this is
                                // a parameter separator.
                                string keyValuePair = quotedString.Substring(
                                    startParameterPosn, inParameterPosn - 
                                    startParameterPosn);

                                keyValuePairList.Add(keyValuePair);

                                inParameterPosn++;
                                startParameterPosn = inParameterPosn;
                            }
                            else
                            {
                                // Do nothing, separator character is within a quoted string.
                                inParameterPosn++;
                            }
                        }
                    }
                }

                // Add the last parameter.
                if (startParameterPosn < quotedString.Length)
                {
                    // Parameter delimiter found and not in quoted string therefore this is a parameter
                    // separator.
                    keyValuePairList.Add(quotedString.Substring(startParameterPosn));
                }
            }

            return keyValuePairList.ToArray();
        }
        catch (Exception)
        {
            throw;
        }
    }

    private void AddKeyValuePair(string keyValuePair, Dictionary<string, string> dictionary)
    {
        if (keyValuePair != null && keyValuePair.Trim().Length > 0)
        {
            int seperatorPosn = keyValuePair.IndexOf(TAG_NAME_VALUE_SEPERATOR);
            if (seperatorPosn != -1)
            {
                string keyName = keyValuePair.Substring(0, seperatorPosn).Trim();

                // If this is not the parameter that is being removed put it back on.
                if (!dictionary.ContainsKey(keyName))
                    dictionary.Add(keyName, keyValuePair.Substring(seperatorPosn + 1).Trim());
            }
            else
            {   // Keys with no values are valid in SIP so they get added to the collection with a null
                // value.
                if (!dictionary.ContainsKey(keyValuePair))
                    dictionary.Add(keyValuePair, string.Empty);
            }
        }
    }

    /// <summary>
    /// Sets a key name to a specified value. If the dictionary does not contain the key then a new
    /// key value pair is added.
    /// </summary>
    /// <param name="name">Name of the parameter (key)</param>
    /// <param name="value">Value of the parameter</param>
    public void Set(string name, string? value)
    {
        if (m_dictionary.ContainsKey(name))
            m_dictionary[name] = value!;
        else
            m_dictionary.Add(name, value!);
    }

    /// <summary>
    /// Gets the value of a specified parameter
    /// </summary>
    /// <param name="name">Name of the parameter</param>
    /// <returns>Returns the value of the parameter. Returns null if the dictionary is empty
    /// or if the parameter is not in the dictionary.</returns>
    public string? Get(string name)
    {
        if (m_dictionary != null)
        {
            if (m_dictionary.ContainsKey(name))
                return SIPEscape.SIPURIParameterUnescape(m_dictionary[name]);
            else
                return null;
        }
        else
            return null;
    }

    /// <summary>
    /// Determines if a parameter exists in the dictionary
    /// </summary>
    /// <param name="name">Name of the parameter</param>
    /// <returns>Returns true if the parameter exists or false if it does not.</returns>
    public bool Has(string name)
    {
        if (m_dictionary != null)
            return m_dictionary.ContainsKey(name);
        else
            return false;
    }

    /// <summary>
    /// Removes a named parameter if it exists.
    /// </summary>
    /// <param name="name">Name of the parameter to remove</param>
    public void Remove(string name)
    {
        if (name != null)
        {
            m_dictionary.Remove(name);
        }
    }

    /// <summary>
    /// Clears the dictionary.
    /// </summary>
    public void RemoveAll()
    {
        m_dictionary = new Dictionary<string, string>();
    }

    /// <summary>
    /// Gets an array of all parameter names (keys)
    /// </summary>
    /// <returns>Returns an array of all parameter names or null if the dictionary is empty</returns>
    public string[]? GetKeys()
    {
        if (m_dictionary == null || m_dictionary.Count == 0)
            return null;
        else
        {
            string[] keys = new string[m_dictionary.Count];
            int index = 0;
            foreach (KeyValuePair<string, string> entry in m_dictionary)
            {
                keys[index++] = entry.Key as string;
            }

            return keys;
        }
    }

    /// <summary>
    /// Converts the name value pairs to a string.
    /// </summary>
    /// <returns></returns>
    public new string ToString() 
    {
        string paramStr = string.Empty;

        if (m_dictionary != null)
        {
            foreach (KeyValuePair<string, string> param in m_dictionary)
            {
                if (param.Value != null && param.Value.Trim().Length > 0)
                {
                    paramStr += TagDelimiter + param.Key + 
                    TAG_NAME_VALUE_SEPERATOR + SIPEscape.SIPURIParameterEscape(param.Value);
                }
                else {
                    paramStr += TagDelimiter + param.Key;
                }
            }
        }

        return paramStr;
    }

    /// <summary>
    /// Calculates the has value for this object.
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode()
    {
        if (m_dictionary != null && m_dictionary.Count > 0)
        {
            SortedList sortedParams = new SortedList();
            foreach (KeyValuePair<string, string> param in m_dictionary)
            {
                sortedParams.Add(param.Key.ToLower(), (string)param.Value);
            }

            StringBuilder sortedParamBuilder = new StringBuilder();
            foreach (DictionaryEntry sortedEntry in sortedParams)
            {
                sortedParamBuilder.Append((string)sortedEntry.Key + (string)
                    sortedEntry.Value);
            }

            return sortedParamBuilder.ToString().GetHashCode();
        }
        else
        {
            return 0;
        }
    }

    /// <summary>
    /// Determines if SIPParameters objects are equal. Two SIPParameters objects are considered equal if
    /// they have the same keys and values. The order of the keys does not affect the equality comparison.
    /// </summary>
    /// <param name="params1">First SIPParameters object</param>
    /// <param name="params2">Second SIPParameters object.</param>
    /// <returns>Returns true if they are equal or false if they are not</returns>
    public static bool AreEqual(SIPParameters params1, SIPParameters params2)
    {
        return params1 == params2;
    }

    /// <summary>
    /// Determines if a SIPParameters object is equal to this one. Two SIPParameters objects are 
    /// considered equal if they have the same keys and values. The order of the keys does not affect
    /// the equality comparison.
    /// </summary>
    /// <param name="obj">Input SIPParameters object</param>
    /// <returns>Returns true if the input SIPParameters object is equal to this one or false if it
    /// is not.</returns>
    public override bool Equals(object? obj)
    {
        return AreEqual(this, (SIPParameters)obj!);
    }

    /// <summary>
    /// Two SIPParameters objects are considered equal if they have the same keys and values. The
    /// order of the keys does not affect the equality comparison.
    /// </summary>
    /// <param name="x">Left-hand SIPParameters object</param>
    /// <param name="y">Right-hand SIPParameters object</param>
    /// <returns>True if the two SIPParameters objects are equal or false if they are not.</returns>
    public static bool operator ==(SIPParameters x, SIPParameters y)
    {
        if (x is null && y is null)
        {
            return true;
        }
        else if (x is null || y is null)
        {
            return false;
        }
        else if (x.m_dictionary == null && y.m_dictionary == null)
        {
            return true;
        }
        else if (x.m_dictionary == null || y.m_dictionary == null)
        {
            return false;
        }

        return x.m_dictionary.Count == y.m_dictionary.Count && x.m_dictionary.Keys.All(k => y.m_dictionary.ContainsKey(k)
           && String.Equals(x.m_dictionary[k], y.m_dictionary[k], StringComparison.InvariantCultureIgnoreCase));
    }

    /// <summary>
    /// Not equals operator
    /// </summary>
    /// <param name="x">Left-hand SIPParameters object</param>
    /// <param name="y">Right-hand SIPParameters object</param>
    /// <returns>Returns true if the two objects are not equal or false if they are equal</returns>
    public static bool operator !=(SIPParameters x, SIPParameters y)
    {
        return !(x == y);
    }

    /// <summary>
    /// Creates a deep copy of this SIPParameters object
    /// </summary>
    /// <returns></returns>
    public SIPParameters CopyOf()
    {
        SIPParameters copy = new SIPParameters(ToString(), TagDelimiter);
        return copy;
    }
}
