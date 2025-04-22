/////////////////////////////////////////////////////////////////////////////////////
//  File:   IncomingTestCallManager.cs                              26 Mar 25 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.TestCalls;

using SipLib.Sdp;
using SipLib.Threading;
using SipLib.Transactions;
using System.Threading.Tasks;
using SipLib.Core;
using System.Collections.Concurrent;

/// <summary>
/// Class for managing NG9-1-1 incoming test calls.
/// <para>To use this class, construct an instance of it and then call the Start() method.</para>
/// <para>Call the Shutdown() method when the application or the object that is using this class is shutting down.</para>
/// </summary>
public class IncomingTestCallManager : QueuedActionWorkerTask
{
    private SdpAnswerSettings m_AnswerSettings;
    private IncomingTestCallSettings m_TestCallSettings;
    private ConcurrentDictionary<string, IncomingTestCall> m_Calls = new ConcurrentDictionary<string, IncomingTestCall>();
    private string m_UserName;
    
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="answerSettings">Settings that determine how to answer the test call request.</param>
    /// <param name="testCallSettings">Settings that determine how to handle test calls.</param>
    /// <param name="userName">SIP user agent name</param>
    public IncomingTestCallManager(SdpAnswerSettings answerSettings, IncomingTestCallSettings testCallSettings, string userName) : base(100)
    {
        m_AnswerSettings = answerSettings;
        m_TestCallSettings = testCallSettings;
        m_UserName = userName;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override async Task Shutdown()
    {
        ManualResetEventSlim Mre = new ManualResetEventSlim(false);
        EnqueueWork(() =>
        {
            
        });

        Mre.Wait(500);

        await base.Shutdown();
    }

    /// <summary>
    /// Processes an INVITE request for an NG9-1-1 test call. Call this method only if TestCallUtils.IsNg911TestCall()
    /// returns true.
    /// </summary>
    /// <param name="sipRequest">Incoming INVITE request for an NG9-1-1 test call.</param>
    /// <param name="remoteEndPoint">Sender of the INVITE request</param>
    /// <param name="sipTransport">SipTransport that the request was received on.</param>
    public void ProcessTestCallInviteRequest(SIPRequest sipRequest, SIPEndPoint remoteEndPoint, SipTransport sipTransport)
    {
        if (m_TestCallSettings.Enable == false)
        {
            RejectTestCall(sipRequest, remoteEndPoint, sipTransport);
            return;
        }

        if (IncomingTestCall.TestCallIsValid(sipRequest) == false)
        {
            RejectTestCall(sipRequest, remoteEndPoint, sipTransport);
            return;
        }

        // Make sure that the test call does not already exist. If it does then reject the request because re-INVITE
        // requests are not allows for test calls.
        IncomingTestCall? call = GetCall(sipRequest.Header.CallId);
        if (call != null)
        {
            RejectTestCall(sipRequest, remoteEndPoint, sipTransport);
            return;
        }

        if (m_Calls.Count > m_TestCallSettings.MaxTestCalls)
        {
            SIPResponse busy = SipUtils.BuildResponse(sipRequest, SIPResponseStatusCodesEnum.BusyHere, "Busy Here",
                sipTransport.SipChannel, m_UserName);
            sipTransport.StartServerInviteTransaction(sipRequest, remoteEndPoint.GetIPEndPoint(), null, busy);
            return;
        }

        EnqueueWork(() =>
        {
            IncomingTestCall call = new IncomingTestCall(sipRequest, remoteEndPoint, sipTransport, m_AnswerSettings, m_TestCallSettings);
            m_Calls.TryAdd(sipRequest.Header.CallId, call);
            call.TestCallEnded += OnTestCallEnded;
            call.StartCall();
        });
    }

    private void OnTestCallEnded(string callId)
    {
        EnqueueWork(async () => 
        {
            IncomingTestCall? call = GetCall(callId);
            if (call == null)
                return;         // The call has already ended

            await call.Shutdown();
            m_Calls.TryRemove(callId, out IncomingTestCall? testCall);           
        });
    }

    private void RejectTestCall(SIPRequest sipRequest, SIPEndPoint remoteEndPoint, SipTransport sipTransport)
    {
        SIPResponse response = SipUtils.BuildResponse(sipRequest, SIPResponseStatusCodesEnum.NotAcceptable,
            "Not Acceptable", sipTransport.SipChannel, m_UserName);
        // Final response so just fire and forget.
        sipTransport.StartServerInviteTransaction(sipRequest, remoteEndPoint.GetIPEndPoint(), null, response);
    }

    /// <summary>
    /// Tests to see if a SIP request is for an active test call.
    /// </summary>
    /// <param name="sipRequest">SIP request that was received</param>
    /// <returns>Returns true if the request is for an active test call or false if is not.</returns>
    public bool IsActiveTestCall(SIPRequest sipRequest)
    {
        IncomingTestCall? call = GetCall(sipRequest.Header.CallId);
        if (call == null)
            return false;
        else
            return true;
    }

    /// <summary>
    /// Processes a BYE request for a test call.
    /// </summary>
    /// <param name="sipRequest">The SIP BYE request.</param>
    /// <param name="remoteEndPoint">Sender of the BYE request.</param>
    /// <param name="sipTransport">Transport that received the request.</param>
    public void ProcessTestCallByeRequest(SIPRequest sipRequest, SIPEndPoint remoteEndPoint, SipTransport sipTransport)
    {
        IncomingTestCall? call = GetCall(sipRequest.Header.CallId);
        if (call == null)
        {
            SIPResponse response = SipUtils.BuildResponse(sipRequest, SIPResponseStatusCodesEnum.CallLegTransactionDoesNotExist,
                "Dialog Does Not Exist", sipTransport.SipChannel, m_UserName);
            sipTransport.StartServerNonInviteTransaction(sipRequest, remoteEndPoint.GetIPEndPoint(), null, response);
        }
        else
            call.ProcessByeRequest(sipRequest, remoteEndPoint, sipTransport);
    }

    /// <summary>
    /// 
    /// </summary>
    protected override void DoTimedEvents()
    {
        
    }

    /// <summary>
    /// Gets the TestCall object for a specified call ID
    /// </summary>
    /// <param name="callID">Call-ID header value for the call.</param>
    /// <returns>Returns the TestCall object if it exists or null if it does not</returns>
    private IncomingTestCall? GetCall(string? callID)
    {
        if (string.IsNullOrEmpty(callID))
            return null;
        else
            return m_Calls.GetValueOrDefault(callID);
    }

}
