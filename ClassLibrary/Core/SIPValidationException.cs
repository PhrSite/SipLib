#region License
//-----------------------------------------------------------------------------
// Filename: SIPValidationException.cs
//
// Description: Exception class for SIP validation errors.
//
// History:
// 15 Mar 2009	Aaron Clauson	Created.
//
// License: 
// This software is licensed under the BSD License http://www.opensource.org/licenses/bsd-license.php
//
// Copyright (c) 2009 Aaron Clauson (aaron@sipsorcery.com), SIP Sorcery PTY LTD, Hobart, Australia (www.sipsorcery.com)
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


namespace SipLib.Core
{

#pragma warning disable CS1591
    /// <summary>
    /// Enumeration of SIP validation fields
    /// </summary>
    public enum SIPValidationFieldsEnum
    {
        Unknown,
        Request,
        Response,
        URI,
        Headers,
        ContactHeader,
        FromHeader,
        RouteHeader,
        ToHeader,
        ViaHeader,
        NoSIPString,
        ReferToHeader,
        PAssertedIdentityHeader,
        PPreferredIdentityHeader,
        CSeq,           // 10 Nov 22 PHR
        MaxForwards,    // 10 Nov 22 PHR
        Expires,        // 10 Nov 22 PHR
        SipVersion,     // 11 Nov 22 PHR
        CallID,         // 11 Nov 22 PHR
        ContentLength,  // 15 Nov 22 PHR
        ContentType,    // 29 Nov 23 PHR
    }
#pragma warning restore CS1591

    /// <summary>
    /// Exception class for SIP validation errors
    /// </summary>
    public class SIPValidationException : Exception
    {
        /// <summary>
        /// Specifies the field that is in error
        /// </summary>
        public SIPValidationFieldsEnum SIPErrorField;

        /// <summary>
        /// Specifies the status code
        /// </summary>
        public SIPResponseStatusCodesEnum SIPResponseErrorCode;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="sipErrorField"></param>
        /// <param name="message"></param>
        public SIPValidationException(SIPValidationFieldsEnum sipErrorField, string
            message) : base(message)
        {
            SIPErrorField = sipErrorField;
            SIPResponseErrorCode = SIPResponseStatusCodesEnum.BadRequest;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="sipErrorField"></param>
        /// <param name="responseErrorCode"></param>
        /// <param name="message"></param>
        public SIPValidationException(SIPValidationFieldsEnum sipErrorField, 
            SIPResponseStatusCodesEnum responseErrorCode, string message) : base(message)
        {
            SIPErrorField = sipErrorField;
            SIPResponseErrorCode = responseErrorCode;
        }
    }
}
