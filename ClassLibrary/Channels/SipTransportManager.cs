/////////////////////////////////////////////////////////////////////////////////////
//  File:   SipTransportManager.cs                                  29 Aug 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

using SipLib.Core;
using SipLib.Body;
using System.Collections.Concurrent;
using SipLib.Collections;
using System.Net;


namespace SipLib.Channels;

/// <summary>
/// This class manages sending and receiving SIP messages on a single SIPChannel. It also manages SIP
/// transactions and thread-safe send operations on the SIPChannel.
/// </summary>
public class SipTransportManager
{
    private SIPChannel m_SipChannel;
    private Thread m_Thread = null;
    private bool m_IsEnding = false;
    private SemaphoreSlim m_Semaphore = new SemaphoreSlim(0, int.MaxValue);
    private const int MAX_WAIT_TIME_MS = 100;
    private ConcurrentQueue<SipMessageReceivedParams> m_ReceiveQueue = new ConcurrentQueue<SipMessageReceivedParams>();

    /// <summary>
    /// Event that is fired when a SIP request is received
    /// </summary>
    public event SipRequestReceivedDelegate SipRequestReceived = null;

    /// <summary>
    /// Event that is fired when a SIP response is received
    /// </summary>
    public event SipResponseReceivedDelegate SipResponseReceived = null;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="sipChannel">SIPChannel to use for sending and receiving SIP messages.</param>
    public SipTransportManager(SIPChannel sipChannel)
    {
        m_SipChannel = sipChannel;
      

    }

    /// <summary>
    /// Call this method after hooking the events to start the messaging processing thread.
    /// </summary>
    public void Start()
    {
        if (m_Thread != null)
            return;     // Already started

        m_Thread = new Thread(ThreadLoop);
        m_Thread.Priority = ThreadPriority.AboveNormal;
        m_Thread.Start();
        m_SipChannel.SIPMessageReceived = SipMessageReceived;
    }

    /// <summary>
    /// Call this method to shutdown the processing thread and close the SIP channel and all current
    /// connections.
    /// </summary>
    public void Shutdown()
    {
        if (m_IsEnding == true)
            return;

        m_IsEnding = true;
        m_Semaphore.Release();
        m_Thread.Join(500);
    }

    /// <summary>
    /// Gets the SIPChannel object that this class is managing.
    /// </summary>
    public SIPChannel SipChannel
    {
        get { return m_SipChannel; }
    }

    private void ThreadLoop()
    {
        while (m_IsEnding == false)
        {
            m_Semaphore.Wait(MAX_WAIT_TIME_MS);
            if (m_IsEnding == true)
                break;

            while (m_IsEnding == false && m_ReceiveQueue.TryDequeue(out SipMessageReceivedParams Smr) == true)
            {
                ProcessSipReceivedSipMessage(Smr);
            }

        }
    }


    private void ProcessSipReceivedSipMessage(SipMessageReceivedParams Smr)
    {
        SIPMessage sipMessage = null;

        try
        {
            sipMessage = SIPMessage.ParseSIPMessage(Smr.buffer, m_SipChannel.SIPChannelEndPoint,
                Smr.RemoteEndPoint);
        }
        catch (ArgumentException) { }
        catch (Exception) { }

        if (sipMessage == null)
        {
            // TODO: Handle the invalid SIP message
        }

        if (sipMessage.SIPMessageType == SIPMessageTypesEnum.Request)
        {
            SIPRequest sipRequest = null;
            try
            {
                sipRequest = SIPRequest.ParseSIPRequest(sipMessage);
            }
            catch (SIPValidationException) { }
            catch (Exception) { }

            if (sipRequest == null)
            {
                // TODO: handle an invalid SIP request
            }
            else
                ProcessSipRequest(sipRequest, Smr.RemoteEndPoint, Smr.buffer);
        }
        else if (sipMessage.SIPMessageType == SIPMessageTypesEnum.Response)
        {
            SIPResponse sipResponse = null;
            try
            {
                sipResponse = SIPResponse.ParseSIPResponse(sipMessage);
            }
            catch (SIPValidationException) { }
            catch (Exception) { }

            if (sipResponse == null)
            {
                // TODO: handle an invalid SIP response
            }
            else
                ProcessSipResponse(sipResponse, Smr.RemoteEndPoint, Smr.buffer);
        }
        else
        {
            // TODO: Handle the unknown SIP message type case
        }
    }

    private void ProcessSipRequest(SIPRequest sipRequest, SIPEndPoint RemoteEndPoint, byte[] MsgBytes)
    {
        SIPValidationFieldsEnum error;
        string strReason;
        if (sipRequest.IsValid(out error, out strReason) == false)
        {
            // TODO: handle an invalid SIP Request
            return;
        }

        List<MessageContentsContainer> ContentsList = null;
        if (string.IsNullOrEmpty(sipRequest.Body) == false)
            ContentsList = BinaryBodyParser.ParseSipBody(MsgBytes, sipRequest.Header.ContentType);

        SipRequestReceived?.Invoke(sipRequest, RemoteEndPoint, ContentsList, this);
    }

    private void ProcessSipResponse(SIPResponse sipResponse, SIPEndPoint RemoteEndPoint, byte[] MsgBytes)
    {
    }

    /// <summary>
    /// Checks to see if a SIP response message is for the request of this transaction.
    /// </summary>
    /// <remarks>See Section 17.1.3 of RFC 3261.</remarks>
    /// <param name="Req">Original SIP request.</param>
    /// <param name="Res">SIPResponse message.</param>
    /// <returns>Returns true is the response is for the request or false if it does not.</returns>
    private bool ResponseMatchesRequest(SIPRequest Req, SIPResponse Res)
    {
        if (Req.Header.Vias.TopViaHeader.Branch == Res.Header.Vias.TopViaHeader.Branch && Req.Header.
            CSeqMethod == Res.Header.CSeqMethod)
            return true;
        else
            return false;
    }

    private void SipMessageReceived(SIPChannel sipChannel, SIPEndPoint remoteEndPoint, byte[] buffer)
    {
        m_ReceiveQueue.Enqueue(new SipMessageReceivedParams(sipChannel, remoteEndPoint, buffer));
        m_Semaphore.Release();  // Signal the thread to wake up
    }

    private object m_SendLock = new object();

    /// <summary>
    /// Sends a SIP request on the SIPChannel
    /// </summary>
    /// <param name="Request">SIP request to send</param>
    /// <param name="DestEp">Destination endpoint</param>
    public void SendSipRequest(SIPRequest Request, IPEndPoint DestEp)
    {
        lock (m_SendLock)
        {
            m_SipChannel.Send(DestEp, Request.ToByteArray());
        }
    }

    /// <summary>
    /// Sends a SIP response message on the SIPChannel
    /// </summary>
    /// <param name="Response"></param>
    /// <param name="DestEp"></param>
    public void SendSipResponse(SIPResponse Response, IPEndPoint DestEp)
    {
        lock (m_SendLock)
        {
            m_SipChannel.Send(DestEp, Response.ToByteArray());
        }
    }
}
