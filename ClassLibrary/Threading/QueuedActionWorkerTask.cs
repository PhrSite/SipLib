/////////////////////////////////////////////////////////////////////////////////////
//  File:   QueuedActionWorkerTask.cs                               22 Oct 24 PHR
/////////////////////////////////////////////////////////////////////////////////////

using SipLib.Logging;
using System.Collections.Concurrent;

namespace SipLib.Threading;

/// <summary>
/// <para>
/// This class is intended to be the base class for other classes that need to queue actions and
/// execute them on a single thread context. It is generally not used directly in a stand-alone manner.
/// </para>
/// <para>
/// After constructing a class derived from this class, the derived class must call the Start() method
/// to start the background task. When the derived class is shutting down, it must call the Shutdown() method.
/// </para>
/// <para>
/// Actions are queued for execution by calling the EnqueueAction() method.
/// </para>
/// <para>
/// Derived classes can override the DoTimedEvents() methods so that they can do work periodically within
/// the tread context of this class. The DoTimedEvents() method is called each time through the task loop.
/// The maximum period of execution is the WaitIntervalMsec value provided in the constructor.
/// </para>
/// </summary>
public class QueuedActionWorkerTask
{
    private const int DEFAULT_WAIT_INTERVAL_MS = 100;
    private const int MINIMUM_WAIT_INTERVAL_MS = 0;

    private bool m_IsStarted = false;
    private bool m_IsShutdown = false;
    private ConcurrentQueue<Action> m_WorkQueue = new ConcurrentQueue<Action>();
    private CancellationTokenSource m_CancellationTokenSource = new CancellationTokenSource();
    private Task? m_Task = null;
    private SemaphoreSlim m_Semaphore = new SemaphoreSlim(0, int.MaxValue);
    private int m_WaitIntervalMsec = DEFAULT_WAIT_INTERVAL_MS;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="WaitIntervalMsec">Specifies the maximum number of milliseconds that the worker
    /// task spends sleeping. The minimum value is 10 msec. There is no maximum value.</param>
    public QueuedActionWorkerTask(int WaitIntervalMsec = DEFAULT_WAIT_INTERVAL_MS)
    {
        if (WaitIntervalMsec < MINIMUM_WAIT_INTERVAL_MS)
            m_WaitIntervalMsec = MINIMUM_WAIT_INTERVAL_MS;
        else
            m_WaitIntervalMsec = WaitIntervalMsec;
    }

    /// <summary>
    /// Starts the backgroud task that executes queued actions.
    /// </summary>
    public void Start()
    {
        if (m_IsStarted == true || m_IsShutdown == true)
            return;

        m_IsStarted = true;
        //Task.Factory.StartNew(() => { WorkerTask(m_CancellationTokenSource.Token); }, TaskCreationOptions.LongRunning);
        m_Task = Task.Run(() => { WorkerTask(m_CancellationTokenSource.Token); });
    }

    /// <summary>
    /// Causes the background task that executes actions to terminate. The background worker task may 
    /// not terminate until the action that is currently being executed completes or a maximum of
    /// 500 ms elapses.
    /// </summary>
    public async Task Shutdown()
    {
        if (m_IsStarted == false || m_IsShutdown == true)
            return;

        m_CancellationTokenSource.Cancel();
        if (m_Task != null)
            await m_Task.WaitAsync(TimeSpan.FromMilliseconds(500));

        m_Task = null;
    }
    
    /// <summary>
    /// Enqueues an action and signals the background worker task to wake up and dequeue actions from
    /// the internal action queue and perform them.
    /// </summary>
    /// <param name="action">Action to be queued and executed.</param>
    public void EnqueueWork(Action action)
    {
        m_WorkQueue.Enqueue(action);
        m_Semaphore.Release();
    }

    private void WorkerTask(CancellationToken cancellationToken)
    {
        //CancellationToken token = cancellationToken;
        while (cancellationToken.IsCancellationRequested == false)
        {
            if (m_WaitIntervalMsec != 0)
                m_Semaphore.Wait(m_WaitIntervalMsec);

            DoTimedEvents();

            while (cancellationToken.IsCancellationRequested == false && m_WorkQueue.TryDequeue(out Action? action) == true)
            {
                if (action != null)
                {
                    try
                    {
                        action();
                    }
                    catch (Exception actionException)
                    {
                        SipLogger.LogError(actionException, "");
                    }
                }
            }
        } // end while
    }

    /// <summary>
    /// 
    /// </summary>
    public virtual void DoTimedEvents()
    {
    }
}
