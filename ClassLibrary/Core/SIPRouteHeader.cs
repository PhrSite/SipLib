#region License
//-----------------------------------------------------------------------------
// Filename: SIPRouteHeader.cs
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
/// The SIPRoute class is used to represent both Route and Record-Route headers.
/// </summary>
/// <bnf>
/// Route               =  "Route" HCOLON route-param *(COMMA route-param)
/// route-param         =  name-addr *( SEMI rr-param )
/// 
/// Record-Route        =  "Record-Route" HCOLON rec-route *(COMMA rec-route)
/// rec-route           =  name-addr *( SEMI rr-param )
/// rr-param            =  generic-param
///
/// name-addr           =  [ display-name ] LAQUOT addr-spec RAQUOT
/// addr-spec           =  SIP-URI / SIPS-URI / absoluteURI
/// display-name        =  *(token LWS)/ quoted-string
/// generic-param       =  token [ EQUAL gen-value ]
/// gen-value           =  token / host / quoted-string
/// </bnf>
/// <remarks>
/// The Route and Record-Route headers only have parameters, no headers. 
/// Parameters of from ...;name=value;name2=value2.
/// There are no specific parameters.
/// </remarks>
public class SIPRoute
{
    private static string m_looseRouterParameter = SIPConstants.SIP_LOOSEROUTER_PARAMETER;
    private static char[] m_angles = new char[] { '<', '>' };
    private SIPUserField m_userField = null;

    /// <summary>
    /// Gets or set the host portion of the URI
    /// </summary>
    /// <value></value>
    public string Host
    {
        get { return m_userField?.URI?.Host; }
        set { m_userField.URI.Host = value; }
    }

    /// <summary>
    /// Gets the SIPURI from the Route header
    /// </summary>
    /// <value></value>
    public SIPURI URI
    {
        get { return m_userField?.URI; }
    }

    /// <summary>
    /// Returns true if using strict routing or false if using loose routing.
    /// </summary>
    /// <value></value>
    public bool IsStrictRouter
    {
        get { return !m_userField.URI.Parameters.Has(m_looseRouterParameter); }
        set
        {
            if (value)
            {
                m_userField.URI.Parameters.Remove(m_looseRouterParameter);
            }
            else
            {
                m_userField.URI.Parameters.Set(m_looseRouterParameter, null);
            }
        }
    }

    private SIPRoute()
    { }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="host">Name of th host</param>
    // <exception cref="SIPValidationException"></exception>
    public SIPRoute(string host)
    {
        if (string.IsNullOrWhiteSpace(host) == true)
        {
            throw new SIPValidationException(SIPValidationFieldsEnum.RouteHeader,
                "Cannot create a Route from an blank string.");
        }

        m_userField = SIPUserField.ParseSIPUserField(host);
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="host"></param>
    /// <param name="looseRouter"></param>
    // <exception cref="SIPValidationException"></exception>
    public SIPRoute(string host, bool looseRouter)
    {
        if (string.IsNullOrWhiteSpace(host) == true)
        {
            throw new SIPValidationException(SIPValidationFieldsEnum.RouteHeader,
                "Cannot create a Route from an blank string.");
        }

        m_userField = SIPUserField.ParseSIPUserField(host);
        this.IsStrictRouter = !looseRouter;
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="uri">SIPURI to build the Route header from</param>
    public SIPRoute(SIPURI uri)
    {
        m_userField = new SIPUserField();
        m_userField.URI = uri;
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="uri">SIPURI to build the Route header from</param>
    /// <param name="looseRouter">Should always be true</param>
    public SIPRoute(SIPURI uri, bool looseRouter)
    {
        m_userField = new SIPUserField();
        m_userField.URI = uri;
        this.IsStrictRouter = !looseRouter;
    }

    /// <summary>
    /// Parses a string into a SIPRoute header object.
    /// </summary>
    /// <param name="route">Input string to parse</param>
    /// <returns>Returns a new SIPRoute object</returns>
    // <exception cref="SIPValidationException"></exception>
    public static SIPRoute ParseSIPRoute(string route)
    {
        if (string.IsNullOrWhiteSpace(route) == true)
        {
            throw new SIPValidationException(SIPValidationFieldsEnum.RouteHeader,
                "Cannot create a Route from an blank string.");
        }

        try
        {
            SIPRoute sipRoute = new SIPRoute();
            sipRoute.m_userField = SIPUserField.ParseSIPUserField(route);
            return sipRoute;
        }
        catch (Exception excp)
        {
            throw new SIPValidationException(SIPValidationFieldsEnum.RouteHeader,
                excp.Message);
        }
    }

    /// <summary>
    /// Converts this object into a Route header string value
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return m_userField?.ToString();
    }

    /// <summary>
    /// Gets the SIPEndPoint of this object
    /// </summary>
    /// <returns></returns>
    public SIPEndPoint ToSIPEndPoint()
    {
        return URI?.ToSIPEndPoint();
    }
}
