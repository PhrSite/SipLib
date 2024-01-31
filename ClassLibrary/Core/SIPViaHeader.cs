#region License
//-----------------------------------------------------------------------------
// Filename: SIPViaHeader.cs
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
//  Revised:    9 Nov 22 PHR Initial version. Moved here from SIPHeader.cs
//              29 Nov 22 PHR -- Modified to handle IPv6 addresses
/////////////////////////////////////////////////////////////////////////////////////

using System.Net;

namespace SipLib.Core;

// <bnf>
// Via               =  ( "Via" / "v" ) HCOLON via-parm *(COMMA via-parm)
// via-parm          =  sent-protocol LWS sent-by *( SEMI via-params )
// via-params        =  via-ttl / via-maddr / via-received / via-branch / 
//                      via-extension
// via-ttl           =  "ttl" EQUAL ttl
// via-maddr         =  "maddr" EQUAL host
// via-received      =  "received" EQUAL (IPv4address / IPv6address)
// via-branch        =  "branch" EQUAL token
// via-extension     =  generic-param
// sent-protocol     =  protocol-name SLASH protocol-version SLASH transport
// protocol-name     =  "SIP" / token
// protocol-version  =  token
// transport         =  "UDP" / "TCP" / "TLS" / "SCTP" / other-transport
// sent-by           =  host [ COLON port ]
// ttl               =  1*3DIGIT ; 0 to 255
// generic-param     =  token [ EQUAL gen-value ]
// gen-value         =  token / host / quoted-string
// </bnf>

/// <summary>
/// The Via header only has parameters, no headers. Parameters of from ...;name=
/// value;name2=value2
/// Specific parameters: ttl, maddr, received, branch.
/// 
/// From page 179 of RFC3261:
/// "Even though this specification mandates that the branch parameter be
/// present in all requests, the BNF for the header field indicates that
/// it is optional."
/// 
/// The branch parameter on a Via therefore appears to be optionally mandatory?!
///
/// Any SIP application element that uses transactions depends on the branch 
/// parameter for transaction matching.
/// Only the top Via header branch is used for transactions though so if the 
/// request has made it to this stack
/// with missing branches then in theory it should be safe to proceed. It will be 
/// left up to the SIPTransaction
/// class to reject any SIP requests that are missing the necessary branch.
/// </summary>
public class SIPViaHeader
{
    private static char m_paramDelimChar = ';';
    private static char m_hostDelimChar = ':';

    private static string m_receivedKey = SIPHeaderAncillary.SIP_HEADERANC_RECEIVED;
    private static string m_rportKey = SIPHeaderAncillary.SIP_HEADERANC_RPORT;
    private static string m_branchKey = SIPHeaderAncillary.SIP_HEADERANC_BRANCH;

    /// <summary>
    /// Version parameter
    /// </summary>
    /// <value></value>
    public string Version = null;

    /// <summary>
    /// Transport protocol
    /// </summary>
    /// <value></value>
    public SIPProtocolsEnum Transport;

    /// <summary>
    /// Host portion of the URI
    /// </summary>
    /// <value></value>
    public string Host = null;

    /// <summary>
    /// Port number
    /// </summary>
    /// <value></value>
    public int Port = 0;

    /// <summary>
    /// Gets or sets the branch parameter
    /// </summary>
    /// <value></value>
    public string Branch
    {
        get
        {
            if (ViaParameters != null && ViaParameters.Has(m_branchKey))
                return ViaParameters.Get(m_branchKey);
            else
                return null;
        }
        set { ViaParameters.Set(m_branchKey, value); }
    }

    /// <summary>
    /// IP Address contained in the recevied parameter.
    /// </summary>
    /// <value></value>
    public string ReceivedFromIPAddress
    {
        get
        {
            if (ViaParameters != null && ViaParameters.Has(m_receivedKey))
                return ViaParameters.Get(m_receivedKey);
            else
                return null;
        }
        set { ViaParameters.Set(m_receivedKey, string.Empty); }
    }

    /// <summary>
    /// Port contained in the rport parameter.
    /// </summary>
    /// <value></value>
    public int ReceivedFromPort
    {
        get
        {
            if (ViaParameters != null && ViaParameters.Has(m_rportKey))
            {
                string rportVal = ViaParameters.Get(m_rportKey);
                if (string.IsNullOrEmpty(rportVal) == false)
                    return Convert.ToInt32(ViaParameters.Get(m_rportKey));
                else
                    return 0;
            }
            else
                return 0;
        }
        set { ViaParameters.Set(m_rportKey, value.ToString()); }
    }

    /// <summary>
    /// Contains the Via header parameters
    /// </summary>
    /// <value></value>
    public SIPParameters ViaParameters = new SIPParameters(null, m_paramDelimChar);

    /// <summary>
    /// This the address placed into the Via header by the User Agent.
    /// </summary>
    /// <value></value>
    public string ContactAddress
    {
        get
        {
            if (IPSocket.TryParseIPEndPoint(Host, out var ipEndPoint))
            {
                if (ipEndPoint.Port == 0)
                {
                    if (Port != 0)
                    {
                        ipEndPoint.Port = Port;
                        return ipEndPoint.ToString();
                    }
                    else
                    {
                        if (ipEndPoint.Address.AddressFamily == System.Net.Sockets.AddressFamily.
                            InterNetworkV6)
                            return "[" + ipEndPoint.Address.ToString() + "]";
                        else
                            return ipEndPoint.Address.ToString();
                    }
                }
                else
                    return ipEndPoint.ToString();
            }
            else if (Port != 0)
                return Host + ":" + Port;
            else
                return Host;
        }
    }

    /// <summary>
    /// This is the socket the request was received on and is a combination of the Host and Received
    /// fields.
    /// </summary>
    /// <value></value>
    public string ReceivedFromAddress
    {
        get
        {
            if (ReceivedFromIPAddress != null && ReceivedFromPort != 0)
            {
                IPEndPoint ep = new IPEndPoint(IPAddress.Parse(ReceivedFromIPAddress), ReceivedFromPort);
                return ep.ToString();
            }
            else if (ReceivedFromIPAddress != null && Port != 0)
            {
                IPEndPoint ep = new IPEndPoint(IPAddress.Parse(ReceivedFromIPAddress), Port);
                return ep.ToString();
            }
            else if (ReceivedFromIPAddress != null)
                return ReceivedFromIPAddress;
            else if (ReceivedFromPort != 0)
            {
                if (IPAddress.TryParse(Host, out IPAddress hostip))
                {
                    IPEndPoint ep = new IPEndPoint(hostip, ReceivedFromPort);
                    return ep.ToString();
                }
                else
                    return Host + ":" + ReceivedFromPort;
            }
            else if (Port != 0)
            {
                if (IPAddress.TryParse(Host, out IPAddress hostip))
                {
                    IPEndPoint ep = new IPEndPoint(hostip, Port);
                    return ep.ToString();
                }
                else
                    return Host + ":" + Port;
            }
            else
                return Host;
        }

    }

    /// <summary>
    /// Constructor
    /// </summary>
    public SIPViaHeader()
    { }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="contactIPAddress">Contract address for the Via header</param>
    /// <param name="contactPort">Contact port number</param>
    /// <param name="branch">Via header branch parameter</param>
    /// <param name="protocol">Transport protocol</param>
    public SIPViaHeader(string contactIPAddress, int contactPort, string branch,
        SIPProtocolsEnum protocol)
    {
        Version = SIPConstants.SIP_FULLVERSION_STRING;
        Transport = protocol;
        Host = contactIPAddress;
        Port = contactPort;
        Branch = branch;
        ViaParameters.Set(m_rportKey, string.Empty);
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="contactIPAddress">Contact IP address as a string</param>
    /// <param name="contactPort">Contact port number</param>
    /// <param name="branch">Via branch parameter</param>
    public SIPViaHeader(string contactIPAddress, int contactPort, string branch) :
        this(contactIPAddress, contactPort, branch, SIPProtocolsEnum.udp)
    {
    }

    /// <summary>
    /// Constructs a SIPViaHeader object from the local SIPEndPoint and a branch parameter
    /// </summary>
    /// <param name="localEndPoint">Local SIPEndPoint</param>
    /// <param name="branch">Branch parameter</param>
    public SIPViaHeader(SIPEndPoint localEndPoint, string branch) :
        this(localEndPoint.Address.ToString(), localEndPoint.Port, branch, localEndPoint.Protocol)
    {
    }


    /// <summary>
    /// Constructs a new SIPViaHeader object from an IPEndPoint, a branch parameter and a protocol
    /// </summary>
    /// <param name="contactEndPoint">Contact IPEndPoint</param>
    /// <param name="branch">Branch parameter</param>
    /// <param name="protocol">Transport protocol</param>
    public SIPViaHeader(IPEndPoint contactEndPoint, string branch, SIPProtocolsEnum protocol) : 
        this(contactEndPoint.Address.ToString(), contactEndPoint.Port, branch, protocol)
    { }

    /// <summary>
    /// Parses a SIP Via header
    /// </summary>
    /// <param name="viaHeaderStr">Input string</param>
    /// <returns>Returns a new SIPViaHeader</returns>
    public static SIPViaHeader[] ParseSIPViaHeader(string viaHeaderStr)
    {
        List<SIPViaHeader> viaHeadersList = new List<SIPViaHeader>();

        if (string.IsNullOrEmpty(viaHeaderStr) == false)
        {
            viaHeaderStr = viaHeaderStr.Trim();

            // Multiple Via headers can be contained in a single line by 
            // separating them with a comma.
            string[] viaHeaders = SIPParameters.GetKeyValuePairsFromQuoted(viaHeaderStr, ',');

            if (viaHeaders == null)
                throw new SIPValidationException(SIPValidationFieldsEnum.ViaHeader,
                    "No Via headers provided");

            foreach (string viaHeaderStrItem in viaHeaders)
            {
                if (viaHeaderStrItem == null || viaHeaderStrItem.Trim().Length == 0)
                {
                    throw new SIPValidationException(SIPValidationFieldsEnum.
                        ViaHeader, "No Contact address.");
                }
                else
                {
                    SIPViaHeader viaHeader = new SIPViaHeader();
                    string header = viaHeaderStrItem.Trim();

                    int firstSpacePosn = header.IndexOf(" ");
                    if (firstSpacePosn == -1)
                        throw new SIPValidationException(SIPValidationFieldsEnum.ViaHeader,
                            "No Contact address.");
                    else
                    {
                        string versionAndTransport = header.Substring(0, firstSpacePosn);
                        viaHeader.Version = versionAndTransport.Substring(0,
                            versionAndTransport.LastIndexOf('/'));
                        viaHeader.Transport = SIPProtocolsType.GetProtocolType(
                            versionAndTransport.Substring(versionAndTransport.LastIndexOf('/') + 1));

                        string nextField = header.Substring(firstSpacePosn, header.Length - firstSpacePosn).Trim();

                        int delimIndex = nextField.IndexOf(';');
                        string contactAddress = null;

                        // Some user agents include branch but have the semi-colon missing, that's easy
                        // to cope with by replacing "branch" with ";branch".
                        if (delimIndex == -1 && nextField.Contains(m_branchKey))
                            nextField = nextField.Replace(m_branchKey, ";" + m_branchKey);
                            delimIndex = nextField.IndexOf(';');

                        if (delimIndex == -1)
                            contactAddress = nextField.Trim();
                        else
                        {
                            contactAddress = nextField.Substring(0, delimIndex).Trim();
                            viaHeader.ViaParameters = new SIPParameters(
                                nextField.Substring(delimIndex, nextField.Length - 
                                delimIndex), m_paramDelimChar);
                        }

                        if (contactAddress == null || contactAddress.Trim().Length == 0)
                        {   // Check that the branch parameter is present, without it the Via header
                            // is illegal.
                            throw new SIPValidationException(SIPValidationFieldsEnum.ViaHeader,
                                "No Contact address.");
                        }

                        // Parse the contact address.
                        if (IPSocket.TryParseIPEndPoint(contactAddress, out var ipEndPoint))
                        {
                            viaHeader.Host = ipEndPoint.Address.ToString();
                            if (ipEndPoint.Port != 0)
                                viaHeader.Port = ipEndPoint.Port;
                        }
                        else
                        {   // Now parsing non IP address contact addresses.
                            int colonIndex = contactAddress.IndexOf(m_hostDelimChar);
                            if (colonIndex != -1)
                            {
                                viaHeader.Host = contactAddress.Substring(0, colonIndex);

                                if (!int.TryParse(contactAddress.Substring(colonIndex + 1), out 
                                    viaHeader.Port))
                                    throw new SIPValidationException(SIPValidationFieldsEnum.ViaHeader, 
                                        "Non-numeric port for IP address.");
                                else if (viaHeader.Port > IPEndPoint.MaxPort)
                                    throw new SIPValidationException(SIPValidationFieldsEnum.ViaHeader, 
                                        "The port specified in a Via header exceeded the maximum allowed.");
                            }
                            else
                                viaHeader.Host = contactAddress;
                        }

                        viaHeadersList.Add(viaHeader);
                    }
                }
            }
        }

        if (viaHeadersList.Count > 0)
            return viaHeadersList.ToArray();
        else
            throw new SIPValidationException(SIPValidationFieldsEnum.ViaHeader, "Via list was empty.");
    }

    /// <summary>
    /// Convers this SIPViaHeader object to a string
    /// </summary>
    /// <returns></returns>
    public new string ToString()
    {
        string sipViaHeader = SIPHeaders.SIP_HEADER_VIA + ": " +
            this.Version + "/" + this.Transport.ToString().ToUpper() + " " +
            ContactAddress;
        sipViaHeader += (ViaParameters != null && ViaParameters.Count > 0) ?
            ViaParameters.ToString() : null;

        return sipViaHeader;
    }
}
