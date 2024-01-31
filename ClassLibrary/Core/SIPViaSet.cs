#region License
//-----------------------------------------------------------------------------
// Filename: SIPViaSet.cs
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
/// Class for managing a list of SIPViaHeader objects
/// </summary>
public class SIPViaSet
{
    private static string m_CRLF = SIPConstants.CRLF;

    private List<SIPViaHeader> m_viaHeaders = new List<SIPViaHeader>();

    /// <summary>
    /// Gets the number of Via headers in the Via set
    /// </summary>
    /// <value></value>
    public int Length
    {
        get { return m_viaHeaders.Count; }
    }

    /// <summary>
    /// Gets or sets the list of Via headers
    /// </summary>
    /// <value></value>
    public List<SIPViaHeader> Via
    {
        get { return m_viaHeaders; }
        set { m_viaHeaders = value; }
    }

    /// <summary>
    /// Gets the top-most SIPViaHeader
    /// </summary>
    /// <value></value>
    public SIPViaHeader TopViaHeader
    {
        get
        {
            if (m_viaHeaders != null && m_viaHeaders.Count > 0)
                return m_viaHeaders[0];
            else
                return null;
        }
    }

    /// <summary>
    /// Gets the SIPViaHeader at the bottom of the Via set
    /// </summary>
    /// <value></value>
    public SIPViaHeader BottomViaHeader
    {
        get
        {
            if (m_viaHeaders != null && m_viaHeaders.Count > 0)
                return m_viaHeaders[m_viaHeaders.Count - 1];
            else
                return null;
        }
    }

    /// <summary>
    /// Pops top Via header off the array.
    /// </summary>
    /// <returns>The top Via header</returns>
    public SIPViaHeader PopTopViaHeader()
    {
        SIPViaHeader topHeader = m_viaHeaders[0];
        m_viaHeaders.RemoveAt(0);

        return topHeader;
    }

    /// <summary>
    /// Adds a SIPViaHeader to the bottom of the Via set
    /// </summary>
    /// <param name="viaHeader"></param>
    public void AddBottomViaHeader(SIPViaHeader viaHeader)
    {
        m_viaHeaders.Add(viaHeader);
    }

    /// <summary>
    /// Updates the topmost Via header by setting the received and rport parameters to the IP address
    /// and port the request came from.
    /// </summary>
    /// <remarks>The setting of the received parameter is documented in RFC3261 section 18.2.1 and in
    /// RFC3581 section 4. RFC3581 states that the received parameter value must be set even if it's
    /// the same as the address in the sent from field. The setting of the rport parameter is 
    /// documented in RFC3581 section 4.
    /// An attempt was made to comply with the RFC3581 standard and only set the rport parameter if it
    /// was included by the client user agent however in the wild there are too many user agents that
    /// are behind symmetric NATs not setting an empty rport and if it's not added then they will not be 
    /// able to communicate.
    /// </remarks>
    /// <param name="msgRcvdEndPoint">The remote endpoint the request was received from.</param>
    public void UpateTopViaHeader(IPEndPoint msgRcvdEndPoint)
    {
        // Update the IP Address and port that this request was received on.
        SIPViaHeader topViaHeader = this.TopViaHeader;

        topViaHeader.ReceivedFromIPAddress = msgRcvdEndPoint.Address.ToString();
        topViaHeader.ReceivedFromPort = msgRcvdEndPoint.Port;
    }

    /// <summary>
    /// Pushes a new Via header onto the top of the array.
    /// </summary>
    /// <param name="viaHeader">The Via header to push onto the top of the Via set.</param>
    public void PushViaHeader(SIPViaHeader viaHeader)
    {
        m_viaHeaders.Insert(0, viaHeader);
    }

    /// <summary>
    /// Converts this SIPViaSet object to a string
    /// </summary>
    /// <returns></returns>
    public new string ToString()
    {
        string viaStr = null;

        if (m_viaHeaders != null && m_viaHeaders.Count > 0)
        {
            for (int viaIndex = 0; viaIndex < m_viaHeaders.Count; viaIndex++)
            {
                viaStr += (m_viaHeaders[viaIndex]).ToString() + m_CRLF;
            }
        }

        return viaStr;
    }
}
