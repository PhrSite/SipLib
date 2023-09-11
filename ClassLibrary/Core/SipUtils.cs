/////////////////////////////////////////////////////////////////////////////////////
//  File:   SipUtils.cs                                             24 Aug 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

using SipLib.Channels;
using SipLib.Sdp;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace SipLib.Core;

/// <summary>
/// Static class containing various SIP related utility functions.
/// </summary>
public static class SipUtils
{
    /// <summary>
    /// Creates a new SIPRequest containing all the basic headers such as: From, To, Via, Contact and
    /// CSeq.
    /// </summary>
    /// <param name="Method">SIP method of the request.</param>
    /// <param name="reqUri">Request URI. May be the same as the ToSipUri or it may be different.</param>
    /// <param name="ToSipUri">To URI.</param>
    /// <param name="ToDisplayName">Display name for the To header. Optional, may be null.</param>
    /// <param name="FromSipUri">From URI. A From-Tag is automatically created</param>
    /// <param name="FromDisplayName">Display name for the From header. Optional, may be null.</param>
    /// <returns>Returns a new SIPRequest object.</returns>
    public static SIPRequest CreateBasicRequest(SIPMethodsEnum Method, SIPURI reqUri, SIPURI ToSipUri,
        string ToDisplayName, SIPURI FromSipUri, string FromDisplayName)
    {
        SIPRequest Req = new SIPRequest(Method, reqUri);
        SIPToHeader To = new SIPToHeader(ToDisplayName, ToSipUri, null);
        SIPFromHeader From = new SIPFromHeader(FromDisplayName, FromSipUri, CallProperties.CreateNewTag());

        Req.LocalSIPEndPoint = FromSipUri.ToSIPEndPoint();
        SIPHeader Header = new SIPHeader(From, To, Crypto.GetRandomInt(100, int.MaxValue / 2),
            CallProperties.CreateNewCallId());
        Header.From.FromTag = CallProperties.CreateNewTag();

        Header.Contact = new List<SIPContactHeader>();
        Header.Contact.Add(new SIPContactHeader(FromSipUri.User, FromSipUri));
        Header.CSeqMethod = Method;
        Req.Header = Header;

        SIPViaHeader ViaHeader = new SIPViaHeader(FromSipUri.ToSIPEndPoint().GetIPEndPoint(), CallProperties.
            CreateBranchId(), FromSipUri.Protocol);
        Header.Vias.PushViaHeader(ViaHeader);

        return Req;
    }

    /// <summary>
    /// Builds a SIP response message. A simple response is something like 100 Trying, 180 Ringing, or 
    /// 404 Not Found that does not have a message body.
    /// </summary>
    /// <param name="Sr">he SIP request to respond to.</param>
    /// <param name="ReasonPhrase">The response reason text. Required. Must not be empty or null.</param>
    /// <param name="sipChannel">Contains transport information. Must be the SIPChannel on which the
    /// request message was received.</param>
    /// <param name="StatCode">Status code to use for the response.</param>
    /// <param name="SipUserName">SIP user name to use for the User part of the SIPURI. Optional, may be null.
    /// </param>
    /// <returns>Returns a SIPResponse that can be sent.</returns>
    public static SIPResponse BuildResponse(SIPRequest Sr, SIPResponseStatusCodesEnum StatCode, 
        string ReasonPhrase, SIPChannel sipChannel, string SipUserName)
    {
        SIPResponse Resp = new SIPResponse(StatCode, ReasonPhrase, sipChannel.SIPChannelEndPoint);

        // Because the constructor doesn't set the ReasonPhrase
        Resp.ReasonPhrase = ReasonPhrase;

        Resp.Header.To = Sr.Header.To;
        Resp.Header.From = Sr.Header.From;
        Resp.Header.Vias = Sr.Header.Vias;
        Resp.Header.RecordRoutes = Sr.Header.RecordRoutes;
        Resp.Header.CSeq = Sr.Header.CSeq;
        Resp.Header.CSeqMethod = Sr.Header.CSeqMethod;
        Resp.Header.CallId = Sr.Header.CallId;

        SIPURI sipUri = sipChannel.SIPChannelContactURI.CopyOf();
        sipUri.User = SipUserName;
        SIPContactHeader Sch = new SIPContactHeader(SipUserName, sipUri);
        Resp.Header.Contact.Add(Sch);
        // The Content-Length and Max-Forwards headers are set to default values.

        return Resp;
    }

    /// <summary>
    /// Builds an ACK SIP Request message to send in response to a SIP response message. This method can be
    /// used to build an ACK request to send in response to a 200 OK response or a 4XX response message that
    /// requires an ACK request.
    /// </summary>
    /// <param name="Res">SIP response message to build the ACK request for.</param>
    /// <param name="SipChan">SIPChannel on which the SIP response was received.</param>
    /// <returns>Returns an ACK SIPRequest object.</returns>
    public static SIPRequest BuildAckRequest(SIPResponse Res, SIPChannel SipChan)
    {
        SIPRequest AckRequest;
        if (Res.Header.Contact == null || Res.Header.Contact.Count == 0)
            AckRequest = new SIPRequest(SIPMethodsEnum.ACK, Res.Header.To.ToURI);
        else
        {
            SIPURI AckRuri = Res.Header.Contact[0].ContactURI.CopyOf();
            AckRequest = new SIPRequest(SIPMethodsEnum.ACK, AckRuri);
        }

        SIPEndPoint ChannelEndPoint = SipChan.SIPChannelEndPoint;
        AckRequest.LocalSIPEndPoint = ChannelEndPoint;
        AckRequest.Header = new SIPHeader(Res.Header.From, Res.Header.To, Res.Header.CSeq, Res.Header.CallId);
        AckRequest.Header.CSeqMethod = SIPMethodsEnum.ACK;
        SIPViaHeader ViaHeader = null;
        if (Res.StatusCode >= 200 && Res.StatusCode <= 299)
            // Need a new Via and branch for an ACK to a 200 resonse
            ViaHeader = new SIPViaHeader(ChannelEndPoint.GetIPEndPoint(), CallProperties.CreateBranchId(), 
                ChannelEndPoint.Protocol);
        else
            ViaHeader = Res.Header.Vias.TopViaHeader;

        AckRequest.Header.Vias.PushViaHeader(ViaHeader);

        if (Res.Header.RecordRoutes != null && Res.Header.RecordRoutes.Length > 0)
            AckRequest.Header.Routes = Res.Header.RecordRoutes.Reversed();

        // Use the defaults for Max-Forwards and the Content-Length
        return AckRequest;
    }

    /// <summary>
    /// Builds a 200 OK SIPResponse message for an INVITE request.
    /// </summary>
    /// <param name="InvReq">INVITE request message to build the response for.</param>
    /// <param name="SipChan">SIPChannel on which the INVITE message was received on and the 200 OK response
    /// will be sent on.</param>
    /// <param name="strBody">Body of the 200 OK.</param>
    /// <param name="strContentType">Value of the Content-Type header that describes the format of the message
    /// body. For example: "application/sdp" or "multipart/mixed; boundary=boundary1"</param>
    /// <returns>Returns a SIPResponse object containing the 200 OK response message.</returns>
    public static SIPResponse BuildOkToInvite(SIPRequest InvReq, SIPChannel SipChan, string strBody, string 
        strContentType)
    {
        SIPResponse okResponse = new SIPResponse(SIPResponseStatusCodesEnum.Ok, "OK", InvReq.LocalSIPEndPoint);
        okResponse.Header = new SIPHeader(new SIPContactHeader(null,
            new SIPURI(InvReq.URI.Scheme, SipChan.SIPChannelEndPoint)), InvReq.Header.From, InvReq.Header.To,
            InvReq.Header.CSeq, InvReq.Header.CallId);
        okResponse.Header.Contact = new List<SIPContactHeader>();
        okResponse.Header.Contact.Add(new SIPContactHeader(null, SipChan.SIPChannelContactURI));

        if (string.IsNullOrEmpty(InvReq.Header.To.ToTag) == true)
            okResponse.Header.To.ToTag = CallProperties.CreateNewTag();
        else
            okResponse.Header.To.ToTag = InvReq.Header.To.ToTag;

        okResponse.Header.CSeqMethod = InvReq.Header.CSeqMethod;
        okResponse.Header.Vias = InvReq.Header.Vias;
        // Don't send a MaxForwards header
        okResponse.Header.MaxForwards = int.MinValue;
        okResponse.Header.RecordRoutes = InvReq.Header.RecordRoutes;

        okResponse.Body = strBody;
        okResponse.Header.ContentType = strContentType;
        okResponse.Header.ContentLength = okResponse.Body.Length;
        return okResponse;
    }

    /// <summary>
    /// Determines of a request is in dialog or not.
    /// </summary>
    /// <param name="Req">Input SIP INVITE request</param>
    /// <returns>Returns true if the request is in-dialog or false if it is not.</returns>
    public static bool IsInDialog(SIPRequest Req)
    {
        bool IsReInvite = false;
        if (Req.Header.To.ToTag != null && Req.Header.From.FromTag != null)
            IsReInvite = true;

        return IsReInvite;
    }

    /// <summary>
    /// Returns a reason text statement given a SIPResponseStatusCodesEnum value
    /// </summary>
    /// <param name="Code">Code to return a statement for.</param>
    /// <returns>Returns a text string. Return "Unknown" if the input code not found.</returns>
    public static string GetResponseReason(SIPResponseStatusCodesEnum Code)
    {
        string Reason = "Unknown";

        switch (Code)
        {
            case SIPResponseStatusCodesEnum.Trying:
                Reason = "Trying";
                break;
            case SIPResponseStatusCodesEnum.Ringing:
                Reason = "Ringing";
                break;
            case SIPResponseStatusCodesEnum.CallIsBeingForwarded:
                Reason = "Forwarding Call";
                break;
            case SIPResponseStatusCodesEnum.Queued:
                Reason = "Queued";
                break;
            case SIPResponseStatusCodesEnum.SessionProgress:
                Reason = "Session Progress";
                break;
            case SIPResponseStatusCodesEnum.Ok:
                Reason = "OK";
                break;
            case SIPResponseStatusCodesEnum.Accepted:
                Reason = "Accepted";
                break;
            case SIPResponseStatusCodesEnum.NoNotification:
                Reason = "No Notification";
                break;
            case SIPResponseStatusCodesEnum.MultipleChoices:
                Reason = "Multiple Choices";
                break;
            case SIPResponseStatusCodesEnum.MovedPermanently:
                Reason = "Moved Permanently";
                break;
            case SIPResponseStatusCodesEnum.MovedTemporarily:
                Reason = "Moved Temporarily";
                break;
            case SIPResponseStatusCodesEnum.UseProxy:
                Reason = "Use Proxy";
                break;
            case SIPResponseStatusCodesEnum.AlternativeService:
                Reason = "Alternative Service";
                break;
            case SIPResponseStatusCodesEnum.BadRequest:
                Reason = "Bad Request";
                break;
            case SIPResponseStatusCodesEnum.Unauthorised:
                Reason = "Unauthorized";
                break;
            case SIPResponseStatusCodesEnum.PaymentRequired:
                Reason = "Payment Required";
                break;
            case SIPResponseStatusCodesEnum.Forbidden:
                Reason = "Forbidden";
                break;
            case SIPResponseStatusCodesEnum.NotFound:
                Reason = "Not Found";
                break;
            case SIPResponseStatusCodesEnum.MethodNotAllowed:
                Reason = "Method Not Allowed";
                break;
            case SIPResponseStatusCodesEnum.NotAcceptable:
                Reason = "Not Acceptable";
                break;
            case SIPResponseStatusCodesEnum.ProxyAuthenticationRequired:
                Reason = "Proxy Authentication Required";
                break;
            case SIPResponseStatusCodesEnum.RequestTimeout:
                Reason = "Request Timeout";
                break;
            case SIPResponseStatusCodesEnum.Gone:
                Reason = "Gone";
                break;
            case SIPResponseStatusCodesEnum.ConditionalRequestFailed:
                Reason = "Conditional Request Failed";
                break;
            case SIPResponseStatusCodesEnum.RequestEntityTooLarge:
                Reason = "Request Entity Too Large";
                break;
            case SIPResponseStatusCodesEnum.RequestURITooLong:
                Reason = "Request URI Too Long";
                break;
            case SIPResponseStatusCodesEnum.UnsupportedMediaType:
                Reason = "Unsupported Media Type";
                break;
            case SIPResponseStatusCodesEnum.UnsupportedURIScheme:
                Reason = "Unsupported URI Scheme";
                break;
            case SIPResponseStatusCodesEnum.UnknownResourcePriority:
                Reason = "Unknown Resource Priority";
                break;
            case SIPResponseStatusCodesEnum.BadExtension:
                Reason = "Bad Extension";
                break;
            case SIPResponseStatusCodesEnum.ExtensionRequired:
                Reason = "Extension Required";
                break;
            case SIPResponseStatusCodesEnum.SessionIntervalTooSmall:
                Reason = "Session Interval Too Short";
                break;
            case SIPResponseStatusCodesEnum.IntervalTooBrief:
                Reason = "Interval Too Short";
                break;
            case SIPResponseStatusCodesEnum.UseIdentityHeader:
                Reason = "Use Identity Header";
                break;
            case SIPResponseStatusCodesEnum.ProvideReferrerIdentity:
                Reason = "Provide Referrer Identity";
                break;
            case SIPResponseStatusCodesEnum.FlowFailed:
                Reason = "Flow Failed";
                break;
            case SIPResponseStatusCodesEnum.AnonymityDisallowed:
                Reason = "Anonymity Not Allowed";
                break;
            case SIPResponseStatusCodesEnum.BadIdentityInfo:
                Reason = "Bad Identity Info";
                break;
            case SIPResponseStatusCodesEnum.UnsupportedCertificate:
                Reason = "Unsupported Certificate";
                break;
            case SIPResponseStatusCodesEnum.InvalidIdentityHeader:
                Reason = "Invalid Identity Header";
                break;
            case SIPResponseStatusCodesEnum.FirstHopLacksOutboundSupport:
                Reason = "First Hop Lacks Outbound Support";
                break;
            case SIPResponseStatusCodesEnum.MaxBreadthExceeded:
                Reason = "Max Breadth Exceeded";
                break;
            case SIPResponseStatusCodesEnum.ConsentNeeded:
                Reason = "Consent Needed";
                break;
            case SIPResponseStatusCodesEnum.TemporarilyUnavailable:
                Reason = "Temporarily Unavailable";
                break;
            case SIPResponseStatusCodesEnum.CallLegTransactionDoesNotExist:
                Reason = "Dialog Does Not Exist";
                break;
            case SIPResponseStatusCodesEnum.LoopDetected:
                Reason = "Loop Detected";
                break;
            case SIPResponseStatusCodesEnum.TooManyHops:
                Reason = "Too Many Hops";
                break;
            case SIPResponseStatusCodesEnum.AddressIncomplete:
                Reason = "Address Incomplete";
                break;
            case SIPResponseStatusCodesEnum.Ambiguous:
                Reason = "Ambiguous";
                break;
            case SIPResponseStatusCodesEnum.BusyHere:
                Reason = "Busy Here";
                break;
            case SIPResponseStatusCodesEnum.RequestTerminated:
                Reason = "Request Terminated";
                break;
            case SIPResponseStatusCodesEnum.NotAcceptableHere:
                Reason = "Not Acceptable Here";
                break;
            case SIPResponseStatusCodesEnum.BadEvent:
                Reason = "Bad Event";
                break;
            case SIPResponseStatusCodesEnum.RequestPending:
                Reason = "Request Pending";
                break;
            case SIPResponseStatusCodesEnum.Undecipherable:
                Reason = "Undecipherable";
                break;
            case SIPResponseStatusCodesEnum.InternalServerError:
                Reason = "Internal Server Error";
                break;
            case SIPResponseStatusCodesEnum.NotImplemented:
                Reason = "Not Implemented";
                break;
            case SIPResponseStatusCodesEnum.BadGateway:
                Reason = "Bad Gateway";
                break;
            case SIPResponseStatusCodesEnum.ServiceUnavailable:
                Reason = "Service Not Available";
                break;
            case SIPResponseStatusCodesEnum.ServerTimeout:
                Reason = "Server Timeout";
                break;
            case SIPResponseStatusCodesEnum.SIPVersionNotSupported:
                Reason = "SIP Version Not Supported";
                break;
            case SIPResponseStatusCodesEnum.MessageTooLarge:
                Reason = "Message Too Large";
                break;
            case SIPResponseStatusCodesEnum.PreconditionFailure:
                Reason = "Precondition Failure";
                break;
            case SIPResponseStatusCodesEnum.BusyEverywhere:
                Reason = "Busy Everywhere";
                break;
            case SIPResponseStatusCodesEnum.Decline:
                Reason = "Decline";
                break;
            case SIPResponseStatusCodesEnum.DoesNotExistAnywhere:
                Reason = "Does Not Exist Anywhere";
                break;
            case SIPResponseStatusCodesEnum.NotAcceptableAnywhere:
                Reason = "Not Acceptable Anywhere";
                break;
        } // end switch

        return Reason;
    }

    /// <summary>
    /// Builds an OK response to a BYE or a CANCEL request.
    /// </summary>
    /// <param name="Req">Request message to send the OK response to.</param>
    /// <param name="RemIp">Verified end point of the request originator.</param>
    /// <returns>Returns a 200 OK SIPResponse object.</returns>
    public static SIPResponse BuildOkToByeOrCancel(SIPRequest Req, SIPEndPoint RemIp)
    {
        SIPResponse okResponse = null;

        okResponse = new SIPResponse(SIPResponseStatusCodesEnum.Ok, null, RemIp);

        SIPHeader requestHeader = Req.Header;
        okResponse.Header = new SIPHeader(new SIPContactHeader(null, new SIPURI(Req.URI.Scheme, RemIp)),
            requestHeader.From, requestHeader.To, requestHeader.CSeq, requestHeader.CallId);

        okResponse.Header.To.ToTag = requestHeader.To.ToTag;
        okResponse.Header.CSeqMethod = requestHeader.CSeqMethod;
        okResponse.Header.Vias = requestHeader.Vias;
        okResponse.Header.MaxForwards = int.MinValue;   // Don't send a MaxForwards header
        okResponse.Header.RecordRoutes = requestHeader.RecordRoutes;
        okResponse.Header.Contact = null;

        return okResponse;
    }

    /// <summary>
    /// Checks for the case where a message is received on the UDP transport protocol and the top most Via
    /// header does not have an rport parameter but does have a port number that is different than the source
    /// port that the message was received on.
    /// </summary>
    /// <param name="SipMsg">Received message.</param>
    /// <param name="RemIp">SIPEndPoint that the message was received from./// </param>
    /// <param name="SipChannel">SIPChannel that received the request message.</param>
    /// <returns>Returns the original end point if the transport protocol is not UDP or if it is UDP but the
    /// top most Via contains an rport parameter. Else returns a new endpoint constructed from the Sent By part
    /// of the Via header provided it has a port number specified. If not port number is specified, then the
    /// original end point is returned.</returns>
    public static SIPEndPoint VerifyUdpEndPoint(SIPRequest SipMsg, SIPEndPoint RemIp, SIPChannel SipChannel)
    {
        if (SipChannel.SIPChannelEndPoint.Protocol != SIPProtocolsEnum.udp)
            return RemIp;

        SIPViaHeader TopVia = SipMsg.Header.Vias.TopViaHeader;
        if (TopVia.ViaParameters.Get("rport") != null)
            // If the rport parameter is present, always respond using the end point that the message was
            // received on.
            return RemIp;
        else
        {   // Build a new SIPEndpoint from the top most Via header.
            int Port = TopVia.Port;
            if (Port == 0)
                Port = 5060;    // Actually an error, so try to fix it

            IPAddress IpAdr = IPAddress.Parse(TopVia.Host);
            return new SIPEndPoint(new IPEndPoint(IpAdr, Port));
        }
    }

    /// <summary>
    /// Builds a BYE or a CANCEL for in incoming or an outgoing call.
    /// </summary>
    /// <param name="InvReq">Original INVITE message for the call.</param>
    /// <param name="SipChan">SIPChannel used to communicate with the remote endpoint.</param>
    /// <param name="RemIpe">IPEndPoint to send the request to.</param>
    /// <param name="BuildBye">If true, then build a BYE request. Else build a CANCEL request.</param>
    /// <param name="IncomingCall">If true then the call was an incoming call, i.e. the INVITE request was
    /// received. Else, the call was an outgoing call, i.e. the INVITE request was sent.</param>
    /// <param name="LastCSeqNumber">Last CSeq number for the call dialog. </param>
    /// <param name="InviteOkResponse">OK message that was received in response to the INVITE request.
    /// This parameter is required if the call was an outgoing one and is currently on-line (BuildBye is true).
    /// </param>
    /// <returns>Returns a SIPRequest object containing the BYE or the CANCEL request.</returns>
    public static SIPRequest BuildByeOrCancelRequest(SIPRequest InvReq, SIPChannel SipChan, IPEndPoint RemIpe,
        bool BuildBye, bool IncomingCall, int LastCSeqNumber, SIPResponse InviteOkResponse)
    {
        SIPRequest ByeRequest;
        SIPURI RemoteTarget;
        SIPMethodsEnum Method = SIPMethodsEnum.BYE;
        SIPFromHeader ByeFromHeader;
        SIPToHeader ByeToHeader;

        string strBranch = CallProperties.CreateBranchId();
        SIPMethodsEnum CSeqMethod = SIPMethodsEnum.BYE;
        int CSeqNum = LastCSeqNumber += 1;
        SIPRouteSet RouteSet = null;
        SIPEndPoint RemoteSipEndPoint = null;

        if (IncomingCall == true)
        {
            RemoteTarget = GetRemoteSipUri(InvReq.Header);
            ByeFromHeader = new SIPFromHeader(InvReq.Header.To.ToName, InvReq.Header.To.ToURI, InvReq.Header.
                To.ToTag);
            ByeToHeader = new SIPToHeader(InvReq.Header.From.FromName, InvReq.Header.From.FromURI,
                InvReq.Header.From.FromTag);
            RouteSet = InvReq.Header.RecordRoutes;
            RemoteSipEndPoint = InvReq.RemoteSIPEndPoint;
        }
        else
        {   // Its an outgoing call
            SIPEndPoint Sep = new SIPEndPoint(SipChan.SIPChannelEndPoint.Protocol, RemIpe);
            RemoteSipEndPoint = Sep;
            RemoteTarget = new SIPURI(SIPSchemesEnum.sip, Sep);
            RemoteTarget.User = InvReq.Header.To.ToURI.User;

            ByeFromHeader = InvReq.Header.From;
            ByeToHeader = InvReq.Header.To;
            if (BuildBye == false)
            {
                Method = SIPMethodsEnum.CANCEL;
                strBranch = InvReq.Header.Vias.TopViaHeader.Branch;
                CSeqMethod = SIPMethodsEnum.CANCEL;
                CSeqNum = InvReq.Header.CSeq;
            }
            else
            {
                if (InviteOkResponse != null && InviteOkResponse.Header != null && InviteOkResponse.Header.To
                    != null)
                {
                    ByeToHeader.ToTag = InviteOkResponse.Header.To.ToTag;
                    RemoteTarget = InviteOkResponse.Header.Contact[0].ContactURI;
                    RouteSet = InviteOkResponse.Header.RecordRoutes;
                }
            }
        }

        ByeRequest = new SIPRequest(Method, RemoteTarget);

        SIPHeader ByeHeader = new SIPHeader(ByeFromHeader, ByeToHeader, CSeqNum, InvReq.Header.CallId);
        ByeHeader.CSeqMethod = CSeqMethod;
        ByeRequest.Header = ByeHeader;
        ByeRequest.Header.Routes = RouteSet;
        ByeRequest.RemoteSIPEndPoint = RemoteSipEndPoint;
        SIPViaHeader ViaHeader = new SIPViaHeader(SipChan.SIPChannelEndPoint, strBranch);
        ByeRequest.Header.Vias.PushViaHeader(ViaHeader);

        return ByeRequest;
    }

    /// <summary>
    /// Builds a BYE SIPRequest for in incoming or an outgoing call.
    /// </summary>
    /// <param name="InvReq">Original INVITE message for the call.</param>
    /// <param name="SipChan">SIPChannel used to communicate with the remote endpoint.</param>
    /// <param name="RemIpe">IPEndPoint to send the request to.</param>
    /// <param name="IncomingCall">If true then the call was an incoming call, i.e. the INVITE request was
    /// received. Else, the call was an outgoing call, i.e. the INVITE request was sent.</param>
    /// <param name="LastCSeqNumber">Last CSeq number for the call dialog. </param>
    /// <param name="InviteOkResponse">OK message that was received in response to the INVITE request.
    /// This parameter is required if the call was an outgoing one and is currently on-line.</param>
    /// <returns>Returns a SIPRequest object containing the BYE or the CANCEL request.</returns>
    public static SIPRequest BuildByeRequest(SIPRequest InvReq, SIPChannel SipChan, IPEndPoint RemIpe,
        bool IncomingCall, int LastCSeqNumber, SIPResponse InviteOkResponse)
    {
        SIPRequest ByeRequest;
        SIPURI RemoteTarget;
        SIPMethodsEnum Method = SIPMethodsEnum.BYE;
        SIPFromHeader ByeFromHeader;
        SIPToHeader ByeToHeader;

        string strBranch = CallProperties.CreateBranchId();
        SIPMethodsEnum CSeqMethod = SIPMethodsEnum.BYE;
        int CSeqNum = LastCSeqNumber += 1;
        SIPRouteSet RouteSet = null;
        SIPEndPoint RemoteSipEndPoint = null;

        if (IncomingCall == true)
        {
            RemoteTarget = GetRemoteSipUri(InvReq.Header);
            ByeFromHeader = new SIPFromHeader(InvReq.Header.To.ToName, InvReq.Header.To.ToURI, InvReq.Header.
                To.ToTag);
            ByeToHeader = new SIPToHeader(InvReq.Header.From.FromName, InvReq.Header.From.FromURI,
                InvReq.Header.From.FromTag);
            RouteSet = InvReq.Header.RecordRoutes;
            RemoteSipEndPoint = InvReq.RemoteSIPEndPoint;
        }
        else
        {   // Its an outgoing call
            SIPEndPoint Sep = new SIPEndPoint(SipChan.SIPChannelEndPoint.Protocol, RemIpe);
            RemoteSipEndPoint = Sep;
            RemoteTarget = new SIPURI(SIPSchemesEnum.sip, Sep);
            RemoteTarget.User = InvReq.Header.To.ToURI.User;

            ByeFromHeader = InvReq.Header.From;
            ByeToHeader = InvReq.Header.To;
            if (InviteOkResponse != null && InviteOkResponse.Header != null && InviteOkResponse.Header.To
                != null)
            {
                ByeToHeader.ToTag = InviteOkResponse.Header.To.ToTag;
                RemoteTarget = InviteOkResponse.Header.Contact[0].ContactURI;
                RouteSet = InviteOkResponse.Header.RecordRoutes;
            }
        }

        ByeRequest = new SIPRequest(Method, RemoteTarget);
        SIPHeader ByeHeader = new SIPHeader(ByeFromHeader, ByeToHeader, CSeqNum, InvReq.Header.CallId);
        ByeHeader.CSeqMethod = CSeqMethod;
        ByeRequest.Header = ByeHeader;
        ByeRequest.Header.Routes = RouteSet;
        ByeRequest.RemoteSIPEndPoint = RemoteSipEndPoint;
        SIPViaHeader ViaHeader = new SIPViaHeader(SipChan.SIPChannelEndPoint, strBranch);
        ByeRequest.Header.Vias.PushViaHeader(ViaHeader);

        return ByeRequest;
    }

    /// <summary>
    /// Builds a BYE or a CANCEL for in incoming or an outgoing call.
    /// </summary>
    /// <param name="InvReq">Original INVITE message for the call.</param>
    /// <param name="SipChan">SIPChannel used to communicate with the remote endpoint.</param>
    /// <param name="RemIpe">IPEndPoint to send the request to.</param>
    /// <param name="LastCSeqNumber">Last CSeq number for the call dialog. </param>
    /// <returns>Returns a SIPRequest object containing the BYE or the CANCEL request.</returns>
    public static SIPRequest BuildCancelRequest(SIPRequest InvReq, SIPChannel SipChan, IPEndPoint RemIpe,
        int LastCSeqNumber)
    {
        SIPRequest CancelRequest;
        SIPURI RemoteTarget;
        SIPMethodsEnum Method = SIPMethodsEnum.CANCEL;
        SIPFromHeader FromHeader;
        SIPToHeader ToHeader;

        string strBranch = CallProperties.CreateBranchId();
        SIPMethodsEnum CSeqMethod = SIPMethodsEnum.BYE;
        int CSeqNum = LastCSeqNumber += 1;
        SIPRouteSet RouteSet = null;
        SIPEndPoint RemoteSipEndPoint = null;

        SIPEndPoint Sep = new SIPEndPoint(SipChan.SIPChannelEndPoint.Protocol, RemIpe);
        RemoteSipEndPoint = Sep;
        RemoteTarget = new SIPURI(SIPSchemesEnum.sip, Sep);
        RemoteTarget.User = InvReq.Header.To.ToURI.User;

        FromHeader = InvReq.Header.From;
        ToHeader = InvReq.Header.To;
        strBranch = InvReq.Header.Vias.TopViaHeader.Branch;
        CSeqMethod = SIPMethodsEnum.CANCEL;
        CSeqNum = InvReq.Header.CSeq;

        CancelRequest = new SIPRequest(Method, RemoteTarget);
        SIPHeader Header = new SIPHeader(FromHeader, ToHeader, CSeqNum, InvReq.Header.CallId);
        Header.CSeqMethod = CSeqMethod;
        CancelRequest.Header = Header;
        CancelRequest.Header.Routes = RouteSet;
        CancelRequest.RemoteSIPEndPoint = RemoteSipEndPoint;
        SIPViaHeader ViaHeader = new SIPViaHeader(SipChan.SIPChannelEndPoint, strBranch);
        CancelRequest.Header.Vias.PushViaHeader(ViaHeader);

        return CancelRequest;
    }

    /// <summary>
    ///  Gets the URI of the remote party for an incoming call.
    /// </summary>
    /// <remarks>This method returns the URI specified in the top Record-Route header if it has a "lr"
    /// parameter or the URI in Contact header. If there is no Contact header then this method returns the
    /// URI from the From header.</remarks>
    /// <param name="Hdr">The headers from the INVITE request of the incoming call.</param>
    /// <returns>Returns the URI to send a request to for the call, such as a BYE or CANCEL request.</returns>
    public static SIPURI GetRemoteSipUri(SIPHeader Hdr)
    {
        SIPRoute Sr = (Hdr.RecordRoutes != null && Hdr.RecordRoutes.Length > 0) ?
            Hdr.RecordRoutes.GetAt(0) : null;

        if (Sr != null && Sr.IsStrictRouter == true && Sr.URI != null)
        {
            SIPURI Suri = Sr.URI.CopyOf();
            Suri.Parameters.RemoveAll();
            return Suri;
        }

        // Is loose routing, check for a URI in the Contact header.
        if (Hdr.Contact != null && Hdr.Contact.Count > 0 && Hdr.Contact[0].
            ContactURI != null)
            return Hdr.Contact[0].ContactURI;

        // Still don't have a URI to send to so use the URI in the From header.
        if (Hdr.From != null)
            return Hdr.From.FromURI;
        else
            return null;
    }


    /// <summary>
    ///  Gets the URI of the remote party for an incoming call. Only call this method for incoming calls.
    /// </summary>
    /// <remarks>This method returns the URI specified in the top Record-Route header if it has a "lr"
    /// parameter or the URI in Contact header. If there is no Contact header then this method returns the
    /// URI from the From header.</remarks>
    /// <param name="InvReq">INVITE request of the incoming call.</param>
    /// <returns>Returns the URI to send a request to for the call, such as a BYE or CANCEL request.</returns>
    public static SIPURI GetRemoteUri(SIPRequest InvReq)
    {
        SIPHeader Hdr = InvReq.Header;
        SIPRoute Sr = (Hdr.RecordRoutes != null && Hdr.RecordRoutes.Length > 0) ?
            Hdr.RecordRoutes.GetAt(0) : null;
        if (Sr != null && Sr.IsStrictRouter == false && Sr.URI != null)
        {
            SIPURI Suri = Sr.URI.CopyOf();
            Suri.Parameters.RemoveAll();
            return Suri;
        }

        // Not loose routing, check for a URI in the Contact header.
        if (Hdr.Contact != null && Hdr.Contact.Count > 0 && Hdr.Contact[0].
            ContactURI != null)
            return Hdr.Contact[0].ContactURI;

        // Still don't have a URI to send to so use the URI in the From header.
        if (Hdr.From != null)
            return Hdr.From.FromURI;
        else
            return null;
    }

    /// <summary>
    /// Builds an in-dialog SIP request.
    /// </summary>
    /// <param name="Method">Request method for the new in-dialog request</param>
    /// <param name="SipChan">SIPChannel that the call is on</param>
    /// <param name="IncomingCall">True if the original call was incoming or false if it was an outgoing call
    /// </param>
    /// <param name="InvReq">The original INVITE request</param>
    /// <param name="LocalTag">The local tag for the SIP dialog</param>
    /// <param name="RemoteTag">The remote tag for the SIP dialog</param>
    /// <param name="InviteOkResponse">The original OK response to the INVITE request</param>
    /// <param name="LastCSeqNumber">The last CSeq number. This value is updated by this function.</param>
    /// <returns>Returns a new SIPRequest object.</returns>
    public static SIPRequest BuildInDialogRequest(SIPMethodsEnum Method, SIPChannel SipChan, bool IncomingCall,
        SIPRequest InvReq, string LocalTag, string RemoteTag, SIPResponse InviteOkResponse,
        ref int LastCSeqNumber)
    {
        SIPRequest Req;
        SIPURI RemoteTarget;
        SIPFromHeader FromHeader;
        SIPToHeader ToHeader;

        string strBranch = CallProperties.CreateBranchId();
        int CSeqNum = LastCSeqNumber += 1;
        LastCSeqNumber = CSeqNum;
        SIPRouteSet RouteSet;
        SIPEndPoint RemoteEndPoint;
        if (IncomingCall == true)
        {
            RemoteTarget = InvReq.Header.Contact[0].ContactURI;
            FromHeader = new SIPFromHeader(InvReq.Header.To.ToName,
                InvReq.Header.To.ToURI, LocalTag);
            ToHeader = new SIPToHeader(InvReq.Header.From.FromName,
                InvReq.Header.From.FromURI, RemoteTag);
            RouteSet = InvReq.Header.RecordRoutes;
            RemoteEndPoint = InvReq.RemoteSIPEndPoint;
        }
        else
        {   // Its an outgoing call
            RemoteTarget = InviteOkResponse.Header.Contact[0].ContactURI;
            FromHeader = InvReq.Header.From;
            ToHeader = InvReq.Header.To;
            ToHeader.ToTag = RemoteTag;
            RouteSet = InviteOkResponse.Header.RecordRoutes;
            RemoteEndPoint = InviteOkResponse.RemoteSIPEndPoint;
        }

        if (RemoteEndPoint == null)
        {
            if (RouteSet != null && RouteSet.Length > 0)
                RemoteEndPoint = RouteSet.TopRoute.ToSIPEndPoint();
            else
                RemoteEndPoint = RemoteTarget.ToSIPEndPoint();
        }

        FromHeader.FromTag = LocalTag;
        Req = new SIPRequest(Method, RemoteTarget);
        SIPHeader Header = new SIPHeader(FromHeader, ToHeader, CSeqNum, InvReq.Header.CallId);
        Header.CSeqMethod = Method;
        Req.Header = Header;
        Req.Header.Routes = RouteSet;
        Req.RemoteSIPEndPoint = RemoteEndPoint;
        SIPViaHeader ViaHeader = new SIPViaHeader(SipChan.SIPChannelEndPoint, strBranch);
        Req.Header.Vias.PushViaHeader(ViaHeader);

        Header.Contact = new List<SIPContactHeader>();
        Header.Contact.Add(new SIPContactHeader(null, SipChan.SIPChannelContactURI));

        return Req;
    }

    /// <summary>
    /// Gets the SIPURI of the top-most Contact header from a SIP request or a SIP response.
    /// </summary>
    /// <param name="Hdr">SIPHeader containing the Contact header list.</param>
    /// <returns>Returns the SIPURI of the top-most Contact header if there is one or null if there are no
    /// Contact header.</returns>
    public static SIPURI GetTopContactSipUri(SIPHeader Hdr)
    {
        if (Hdr.Contact != null && Hdr.Contact.Count > 0 && Hdr.Contact[0].
            ContactURI != null)
            return Hdr.Contact[0].ContactURI;
        else
            return null;
    }

    /// <summary>
    /// Parses a SipFrag, checks for errors and returns the status code from the the SIP message fragment.
    /// </summary>
    /// <param name="SipFrag">String containing the sipfrag, for example: "SIP/2.0 200 OK"</param>
    /// <returns>Returns the status code. Returns 0 if the sipfrag is not valid.</returns>
    public static int GetSipFragResponseCode(string SipFrag)
    {
        int RespCode = 0;
        string strTemp = SipFrag.Trim();
        string[] Fields = strTemp.Split(new char[] { ' ' }, StringSplitOptions.
            RemoveEmptyEntries);
        if (Fields == null || Fields.Length < 3)
            return 0;

        if (Fields[0].IndexOf("SIP") != 0)
            return 0;

        if (int.TryParse(Fields[1], out RespCode) == false)
            return 0;

        return RespCode;
    }

    /// <summary>
    /// Builds a fingerprint SDP attribute for a X.509 certificate. See RFC 4572.
    /// </summary>
    /// <param name="Cert">The X.509 certificate to build the attribute for.</param>
    /// <returns>Returns the SdpAttribute object that can be added to the SDP.</returns>
    public static SdpAttribute BuildFingerprintAttr(X509Certificate2 Cert)
    {
        SdpAttribute Sa = null;
        string HashAlg = Cert.SignatureAlgorithm.
            FriendlyName;

        int NumFlds = Cert.Thumbprint.Length / 2;
        if (Cert.Thumbprint.Length % 2 != 0)
            NumFlds += 1;

        int Idx = 0;
        StringBuilder Sb = new StringBuilder();
        int SubLen;
        for (int i = 0; i < NumFlds; i++)
        {
            SubLen = Idx + 2 <= Cert.Thumbprint.Length ? 2 : 1;
            Sb.Append(Cert.Thumbprint.Substring(Idx, SubLen));
            if (i != NumFlds - 1)
                Sb.Append(":");
            Idx += 2;
        }

        string Thumprint = Sb.ToString();
        if (HashAlg.IndexOf("sha1") > -0)
            HashAlg = "SHA-1";
        else if (HashAlg.IndexOf("sha256") >= 0)
            HashAlg = "SHA-256";
        else if (HashAlg.IndexOf("sha512") >= 0)
            HashAlg = "SHA-512";
        else if (HashAlg.IndexOf("md5") >= 0)
            HashAlg = "MD5";
        else
            HashAlg = "unknown";

        string strAttrVal = string.Format("{0} {1}", HashAlg, Thumprint);
        Sa = new SdpAttribute("fingerprint", strAttrVal);

        return Sa;
    }

    /// <summary>
    /// Gets the SIP URI from the P-Asserted-Identity header if present or from the From header if the the
    /// PAI header is not present.
    /// </summary>
    /// <param name="Sh">Input request headers</param>
    /// <returns>Returns a copy of the SIPURI</returns>
    public static SIPURI GetPaiOrFromUri(SIPHeader Sh)
    {
        SIPURI Result = null;
        if (Sh.PAssertedIdentity != null)
            Result = Sh.PAssertedIdentity.URI;
        else
            Result = Sh.From.FromURI;

        return Result.CopyOf();
    }

    /// <summary>
    /// Builds a emergency URN identifier string used for a Call Identifier or a Incident Identifier. 
    /// See Sectionss 2.1.6 and 2.1.7 of NENA-STA-010.3.
    /// </summary>
    /// <param name="IdType">Must be either "callid" or "incidentid"</param>
    /// <param name="strElemId">Element Identifier of the element that is inserting the Call-Info header.
    /// For example: "bcf.state.pa.us".</param>
    /// <returns>Returns a formatted Emergency Incident or Call ID URN.</returns>
    public static string BuildEmergencyIdUrn(string IdType, string strElemId)
    {
        return string.Format("urn:emergency:uid:{0}:{1}:{2}", IdType, Crypto.GetRandomString(10), strElemId);
    }

    /// <summary>
    /// Adds a Call-Info header to a SIP request containing a purpose parameter
    /// of emergency-IncidentId or emergency-CallId. See Sectionss 2.1.6 and 2.1.7 of NENA-STA-010.3.
    /// </summary>
    /// <param name="Req">SIPRequest to add the Call-Info header to.</param>
    /// <param name="strIdUrn">String containing the Emergency ID URN built using the BuildEmergencyIdUrn()
    /// function. Must be a valid URN.</param>
    /// <param name="PurposeParam">Purpose parameter to add to the Call-Info header. Must be either 
    /// "emergency-CallId" or "emergency-IncidentId".</param>
    public static void AddEmergencyIdUrnCallInfoHeader(SIPRequest Req, string strIdUrn, string PurposeParam)
    {
        SIPURI Suri = SIPURI.ParseSIPURI(strIdUrn);
        SIPCallInfoHeader Cih = new SIPCallInfoHeader(Suri, PurposeParam);
        Req.Header.CallInfo.Add(Cih);
    }

    /// <summary>
    /// Returns the header value of a Call-Info header that has a specified "purpose=" header parameter.
    /// </summary>
    /// <param name="Sh">SIPHeader object containing the SIP headers for the request or response to search in.
    /// </param>
    /// <param name="strPurpose">Purpose parameter to search for.</param>
    /// <returns>Returns the header value of the matching Call-Info header. Returns null if there is no
    /// Call-Info header with the specified purpose parameter.</returns>
    public static string GetCallInfoValueForPurpose(SIPHeader Sh, string strPurpose)
    {
        string Result = null;
        if (Sh.CallInfo == null)
            return Result;

        string str;
        foreach (SIPCallInfoHeader Cih in Sh.CallInfo)
        {
            str = Cih.CallInfoField.Parameters.Get("purpose");
            if (string.IsNullOrEmpty(str) == false && str == strPurpose)
            {
                Result = Cih.CallInfoField.URI.ToString();
                break;
            }
        }

        return Result;
    }

    /// <summary>
    /// Gets the SIPCallInfoHeader for the Call-Info header that has a specified purpose parameter.
    /// </summary>
    /// <param name="Sh">SIPHeader object containing the SIP headers for the request or response to search in.
    /// </param>
    /// <param name="strPurpose">Purpose parameter to search for.</param>
    /// <returns>Returns a SIPCallInfoHeader object if the specified Call-Info header was found or null if it
    /// was not.</returns>
    public static SIPCallInfoHeader GetCallInfoHeaderForPurpose(SIPHeader Sh, string strPurpose)
    {
        SIPCallInfoHeader Result = null;
        if (Sh.CallInfo == null || strPurpose == null)
            return Result;

        string str;
        foreach (SIPCallInfoHeader Cih in Sh.CallInfo)
        {
            str = Cih.CallInfoField.Parameters.Get("purpose");
            if (string.IsNullOrEmpty(str) == false && str == strPurpose)
            {
                Result = Cih;
                break;
            }
        }

        return Result;
    }

}
