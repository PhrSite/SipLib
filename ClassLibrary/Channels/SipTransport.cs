/////////////////////////////////////////////////////////////////////////////////////
//  File:   SipTransport.cs                                         29 Aug 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

using SipLib.Core;
using SipLib.Body;
using System.Collections.Concurrent;
using System.Net;
using SipLib.Transactions;
using SipLib.SipTransactions;

namespace SipLib.Channels;

/// <summary>
/// This class manages sending and receiving SIP messages on a single SIPChannel. It also manages SIP
/// transactions for transactions on that SIPChannel.
/// </summary>
public class SipTransport
{
    private SIPChannel m_SipChannel;
    private Thread m_Thread = null;
    private bool m_IsEnding = false;
    private SemaphoreSlim m_Semaphore = new SemaphoreSlim(0, int.MaxValue);
    private const int MAX_WAIT_TIME_MS = 100;
    private ConcurrentQueue<SipMessageReceivedParams> m_ReceiveQueue = new ConcurrentQueue<SipMessageReceivedParams>();

    /// <summary>
    /// The key is the TransactionID
    /// </summary>
    private ConcurrentDictionary<string, SipTransactionBase> m_Transactions = new ConcurrentDictionary<string, 
        SipTransactionBase>();

    /// <summary>
    /// Event that is fired when a SIP request is received
    /// </summary>
    public event SipRequestReceivedDelegate SipRequestReceived = null;

    /// <summary>
    /// Event that is fired when a SIP response is received
    /// </summary>
    public event SipResponseReceivedDelegate SipResponseReceived = null;

    private DateTime m_LastDoTimedEvents = DateTime.Now - TimeSpan.FromMilliseconds(100);
    private const int DoTimedEventsIntervalMs = 100;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="sipChannel">SIPChannel to use for sending and receiving SIP messages.</param>
    public SipTransport(SIPChannel sipChannel)
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

        m_SipChannel.Close();
        m_IsEnding = true;
        m_Semaphore.Release();
        m_Thread.Join(500);
    }

    /// <summary>
    /// Gets the number of active transactions.
    /// </summary>
    public int TransactionCount
    {
        get
        {
            return m_Transactions.Count;
        }
    }

    /// <summary>
    /// Starts a new SIP transaction.
    /// </summary>
    /// <param name="sipTransaction">New SIP transaction to add.</param>
    private void StartSipTransaction(SipTransactionBase sipTransaction)
    {
        m_Transactions.TryAdd(sipTransaction.TransactionID, sipTransaction);
        bool Terminated = sipTransaction.StartTransaction();
        if (Terminated == true)
        {
            SipTransactionBase TempOut = null;
            m_Transactions.Remove(sipTransaction.TransactionID, out TempOut);
        }
    }

    /// <summary>
    /// Creates and starts a client non-INVITE SIP transaction
    /// </summary>
    /// <param name="request">SIP request to send</param>
    /// <param name="remoteEndPoint">Destination to send the request to</param>
    /// <param name="completeDelegate">Notification callback. Called when the transaction is completed or
    /// terminated. May be null if a notification is not required.</param>
    /// <param name="FinalResponseTimeoutMs">Number of milliseconds to wait for a final response.
    /// This corresponds to Timer F shown in Figure 6 of RFC 3261.</param>
    /// <returns>Returns a new ClientNonInviteTransaction object</returns>
    // <exception cref="ArgumentException">Thrown if the request is not an INVITE</exception>
    public ClientNonInviteTransaction StartClientNonInviteTransaction(SIPRequest request, IPEndPoint
        remoteEndPoint, SipTransactionCompleteDelegate completeDelegate, int FinalResponseTimeoutMs)
    {
        if (request.Method == SIPMethodsEnum.INVITE)
            throw new ArgumentException("This method cannot be used for client INVITE transactions");

        ClientNonInviteTransaction Cnit = new ClientNonInviteTransaction(request, remoteEndPoint,
            completeDelegate, this, FinalResponseTimeoutMs);
        StartSipTransaction(Cnit);
        return Cnit;
    }

    /// <summary>
    /// Creates and starts a client INVITE transaction.
    /// </summary>
    /// <param name="request">SIP INVITE request to send</param>
    /// <param name="remoteEndPoint">Destination to send the request to</param>
    /// <param name="completeDelegate">Notification callback. Called when the transaction is completed or
    /// terminated. May be null if a notification is not required.</param>
    /// <param name="responseReceivedDelegate">Callback function to call when a response is received
    /// for the transaction. Optional, may be null. This may be used when the client transaction user
    /// need to be informed of provisional responses (ex. 180 Ringing or 183 Session Progress)</param>
    /// <returns>Returns a new ClientInviteTransaction object</returns>
    // <exception cref="ArgumentException">Thrown if the request is not an INVITE</exception>
    public ClientInviteTransaction StartClientInviteTransaction(SIPRequest request, IPEndPoint
        remoteEndPoint, SipTransactionCompleteDelegate completeDelegate, TransactionResponseReceivedDelegate
        responseReceivedDelegate)
    {
        if (request.Method != SIPMethodsEnum.INVITE)
            throw new ArgumentException("The request must be an INVITE");

        ClientInviteTransaction Cit = new ClientInviteTransaction(request, remoteEndPoint, completeDelegate,
            this);
        if (responseReceivedDelegate != null)
            Cit.ResponseReceived = responseReceivedDelegate;
        StartSipTransaction(Cit);
        return Cit;
    }

    /// <summary>
    /// Creates and starts a server non-INVITE transaction.
    /// </summary>
    /// <param name="request">SIP request that was received by the server.</param>
    /// <param name="remoteEndPoint">IP endpoint of the remote client that sent the request.</param>
    /// <param name="completeDelegate">Notification callback. Called when the transaction is completed or
    /// terminated. May be null if a notification is not required.</param>
    /// <param name="ResponseToSend">Initial response to send to the client. Will be sent when the transport
    /// layer calls the StartTransaction() method.</param>
    /// <returns>Returns a new ServerNonInviteTransaction object</returns>
    // <exception cref="ArgumentException">Thrown if the request is an INVITE</exception>
    public ServerNonInviteTransaction StartServerNonInviteTransaction(SIPRequest request, IPEndPoint remoteEndPoint,
        SipTransactionCompleteDelegate completeDelegate, SIPResponse ResponseToSend)
    {
        if (request.Method == SIPMethodsEnum.INVITE)
            throw new ArgumentException("This method cannot be used for INVITE transactions");

        ServerNonInviteTransaction Snit = new ServerNonInviteTransaction(request, remoteEndPoint, completeDelegate,
            this, ResponseToSend);
        StartSipTransaction(Snit);
        return Snit;
    }

    /// <summary>
    /// Creates and starts a server INVITE transaction.
    /// </summary>
    /// <param name="request">INVITE request that was received.</param>
    /// <param name="remoteEndPoint">IP endpoint of the remote client that sent the request.</param>
    /// <param name="completeDelegate">Notification callback. Called when the transaction is completed or
    /// terminated. May be null if a notification is not required.</param>
    /// <param name="ResponseToSend">Initial response to send to the client.  Will be sent when the transport
    /// layer calls the StartTransaction() method.</param>
    /// <returns>Returns a new ServerInviteTransaction object.</returns>
    // <exception cref="ArgumentException">Thrown if the request is not an INVITE</exception>
    public ServerInviteTransaction StartServerInviteTransaction(SIPRequest request, IPEndPoint remoteEndPoint,
        SipTransactionCompleteDelegate completeDelegate, SIPResponse ResponseToSend)
    {
        if (request.Method != SIPMethodsEnum.INVITE)
            throw new ArgumentException("The request must be an INVITE");

        ServerInviteTransaction Sit = new ServerInviteTransaction(request, remoteEndPoint, completeDelegate,
            this, ResponseToSend);
        StartSipTransaction(Sit);
        return Sit;
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
        DateTime Now;
        while (m_IsEnding == false)
        {
            Now = DateTime.Now;
            m_Semaphore.Wait(MAX_WAIT_TIME_MS);
            if (m_IsEnding == true)
                break;

            while (m_IsEnding == false && m_ReceiveQueue.TryDequeue(out SipMessageReceivedParams Smr) == true)
            {
                ProcessSipReceivedSipMessage(Smr);
            }

            if ((Now - m_LastDoTimedEvents).TotalMilliseconds > DoTimedEventsIntervalMs)
            {
                DoTimedEvents();
                m_LastDoTimedEvents = Now;
            }
        }
    }

    private void DoTimedEvents()
    {
        List<SipTransactionBase> TransactionsToTerminate = new List<SipTransactionBase>();
        bool Terminate;
        foreach (SipTransactionBase sipTransaction in m_Transactions.Values)
        {
            Terminate = sipTransaction.DoTimedEvents();
            if (Terminate == true)
                TransactionsToTerminate.Add(sipTransaction);
        }

        SipTransactionBase outTransaction;
        foreach (SipTransactionBase Transaction in TransactionsToTerminate)
            m_Transactions.TryRemove(Transaction.TransactionID, out outTransaction);
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

        // For debug only
        string strRequest = sipRequest.ToString();

        if (sipRequest.IsValid(out error, out strReason) == false)
        {
            // TODO: handle an invalid SIP Request
            return;
        }

        string TransactionID = SipTransactionBase.GetServerTransactionID(sipRequest);
        SipTransactionBase sipTransaction;
        if (m_Transactions.TryGetValue(TransactionID, out sipTransaction) == true)
        {
            bool Terminated = sipTransaction.HandleSipRequest(sipRequest, RemoteEndPoint.GetIPEndPoint());
            if (Terminated == true)
                m_Transactions.TryRemove(TransactionID, out sipTransaction);
        }
        else
            // There is no transaction related to this request, so pass the request up to the transport
            // user(s).
            SipRequestReceived?.Invoke(sipRequest, RemoteEndPoint, this);
    }

    private void ProcessSipResponse(SIPResponse sipResponse, SIPEndPoint RemoteEndPoint, byte[] MsgBytes)
    {
        string TransactionID = SipTransactionBase.GetClientTransactionID(sipResponse);
        SipTransactionBase sipTransaction;
        if (m_Transactions.TryGetValue(TransactionID, out sipTransaction) == true)
        {
            bool Terminated = sipTransaction.HandleSipResponse(sipResponse, RemoteEndPoint.GetIPEndPoint());
            if (Terminated == true)
                m_Transactions.TryRemove(TransactionID, out sipTransaction);
        }
        else
            // There is no transaction for this response, so pass the response up to the transport user(s)
            SipResponseReceived?.Invoke(sipResponse, RemoteEndPoint, this);
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
