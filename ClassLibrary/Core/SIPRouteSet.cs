#region License
//-----------------------------------------------------------------------------
// Filename: SIPRouteSet.cs
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

using System.Net;

namespace SipLib.Core;

/// <summary>
/// Class for managing a SIP Route set
/// </summary>
public class SIPRouteSet
{
    private List<SIPRoute> m_sipRoutes = new List<SIPRoute>();

    /// <summary>
    /// Gets or sets the number of routes in the set
    /// </summary>
    /// <value></value>
    public int Length
    {
        get { return m_sipRoutes.Count; }
    }

    /// <summary>
    /// Parses a SIPRouteSet from a string
    /// </summary>
    /// <param name="routeSet">Input string. Route sets are separated by commas</param>
    /// <returns></returns>
    public static SIPRouteSet ParseSIPRouteSet(string routeSet)
    {
        SIPRouteSet sipRouteSet = new SIPRouteSet();

        string[] routes = SIPParameters.GetKeyValuePairsFromQuoted(routeSet, ',');

        if (routes != null)
        {
            foreach (string route in routes)
            {
                SIPRoute sipRoute = SIPRoute.ParseSIPRoute(route);
                sipRouteSet.AddBottomRoute(sipRoute);
            }
        }

        return sipRouteSet;
    }

    /// <summary>
    /// Gets the SIPRoute at a specified index
    /// </summary>
    /// <param name="index">Index of the route set</param>
    /// <returns></returns>
    public SIPRoute GetAt(int index)
    {
        return m_sipRoutes[index];
    }

    /// <summary>
    /// Sets the route at a specific index position
    /// </summary>
    /// <param name="index">Index</param>
    /// <param name="sipRoute">New route set</param>
    public void SetAt(int index, SIPRoute sipRoute)
    {
        m_sipRoutes[index] = sipRoute;
    }

    /// <summary>
    /// Gets the top route
    /// </summary>
    /// <value></value>
    public SIPRoute TopRoute
    {
        get
        {
            if (m_sipRoutes != null && m_sipRoutes.Count > 0)
                return m_sipRoutes[0];
            else
                return null;
        }
    }

    /// <summary>
    /// Gets the bottom route
    /// </summary>
    /// <value></value>
    public SIPRoute BottomRoute
    {
        get
        {
            if (m_sipRoutes != null && m_sipRoutes.Count > 0)
                return m_sipRoutes[m_sipRoutes.Count - 1];
            else
                return null;
        }
    }

    /// <summary>
    /// Adds a route to a the top of the route set
    /// </summary>
    /// <param name="route"></param>
    public void PushRoute(SIPRoute route)
    {
        m_sipRoutes.Insert(0, route);
    }

    /// <summary>
    /// Pushes a route give the host of the route
    /// </summary>
    /// <param name="host"></param>
    public void PushRoute(string host)
    {
        m_sipRoutes.Insert(0, new SIPRoute(host, true));
    }

    /// <summary>
    /// Pushes a new route onto the set given the IPEndpoint, scheme and protocol
    /// </summary>
    /// <param name="socket"></param>
    /// <param name="scheme"></param>
    /// <param name="protcol"></param>
    public void PushRoute(IPEndPoint socket, SIPSchemesEnum scheme, SIPProtocolsEnum protcol)
    {
        m_sipRoutes.Insert(0, new SIPRoute(scheme + ":" + socket.ToString(), true));
    }

    /// <summary>
    /// Adds a route to the end of the route set
    /// </summary>
    /// <param name="route"></param>
    public void AddBottomRoute(SIPRoute route)
    {
        m_sipRoutes.Insert(m_sipRoutes.Count, route);
    }

    /// <summary>
    /// Removes the top route and returns it
    /// </summary>
    /// <returns>Returns the top route or null if the route set is empty</returns>
    public SIPRoute PopRoute()
    {
        SIPRoute route = null;

        if (m_sipRoutes.Count > 0)
        {
            route = m_sipRoutes[0];
            m_sipRoutes.RemoveAt(0);
        }

        return route;
    }

    /// <summary>
    /// Removes the bottom most route
    /// </summary>
    public void RemoveBottomRoute()
    {
        if (m_sipRoutes.Count > 0)
        {
            m_sipRoutes.RemoveAt(m_sipRoutes.Count - 1);
        };
    }

    /// <summary>
    /// Reverses the order of the route set
    /// </summary>
    /// <returns>Returns a new SIPRouteSet</returns>
    public SIPRouteSet Reversed()
    {
        if (m_sipRoutes != null && m_sipRoutes.Count > 0)
        {
            SIPRouteSet reversedSet = new SIPRouteSet();

            for (int index = 0; index < m_sipRoutes.Count; index++)
                reversedSet.PushRoute(m_sipRoutes[index]);

            return reversedSet;
        }
        else
            return null;
    }

    /// <summary>
    /// If a route set is travelling from the public side of a proxy to the private side it can be
    /// required that the Record-Route set is modified.
    /// </summary>
    /// <param name="origSocket">The socket string in the original route set that needs to be replace.
    /// </param>
    /// <param name="replacementSocket">The socket string the original route is being replaced with.
    /// </param>
    public void ReplaceRoute(string origSocket, string replacementSocket)
    {
        foreach (SIPRoute route in m_sipRoutes)
        {
            if (route.Host == origSocket)
            {
                route.Host = replacementSocket;
            }
        }
    }

    /// <summary>
    /// Converts the route set header to a string
    /// </summary>
    /// <returns></returns>
    public new string ToString()
    {
        string routeStr = null;

        if (m_sipRoutes != null && m_sipRoutes.Count > 0)
        {
            for (int routeIndex = 0; routeIndex < m_sipRoutes.Count;
                routeIndex++)
            {
                routeStr += (routeStr != null) ? "," + m_sipRoutes[routeIndex].
                    ToString() : m_sipRoutes[routeIndex].ToString();
            }
        }

        return routeStr;
    }
}
