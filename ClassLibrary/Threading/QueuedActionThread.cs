/////////////////////////////////////////////////////////////////////////////////////
//  File:   QueuedActionThread.cs                                   31 Oct 24 PHR
/////////////////////////////////////////////////////////////////////////////////////

using SipLib.Logging;
using System.Collections.Concurrent;

namespace SipLib.Threading;

/// <summary>
/// <para>
/// This class is intended to be the base class for other classes that need to queue actions and
/// execute them within the context of a single long-running Thread. It is generally not used directly in a stand-alone manner.
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
public class QueuedActionThread
{
    private const int DEFAULT_WAIT_INTERVAL_MS = 100;
    private const int MINIMUM_WAIT_INTERVAL_MS = 0;
    private const ThreadPriority DEFAULT_THREAD_PRIORITY = ThreadPriority.Normal;

    private bool m_IsStarted = false;
    private bool m_IsShutdown = false;
    private ConcurrentQueue<Action> m_WorkQueue = new ConcurrentQueue<Action>();
    private SemaphoreSlim m_Semaphore = new SemaphoreSlim(0, int.MaxValue);
    private int m_WaitIntervalMsec = DEFAULT_WAIT_INTERVAL_MS;
    private Thread m_Thread;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="Priority">Specifies the thread priority. Optional. The default is Normal</param>
    /// <param name="WaitIntervalMsec">Specifies the maximum number of milliseconds that the worker thread spends sleeping. Optional. The
    /// default value is 100 milliseconds. The minimum value is 0 msec. There is no maximum value.</param>
    public QueuedActionThread(ThreadPriority Priority = DEFAULT_THREAD_PRIORITY, int WaitIntervalMsec = DEFAULT_WAIT_INTERVAL_MS)
    {
        if (WaitIntervalMsec < MINIMUM_WAIT_INTERVAL_MS)
            m_WaitIntervalMsec = MINIMUM_WAIT_INTERVAL_MS;
        else
            m_WaitIntervalMsec = WaitIntervalMsec;

        m_Thread = new Thread(ThreadLoop);
        m_Thread.IsBackground = true;
        m_Thread.Priority = Priority;
    }

    /// <summary>
    /// Starts the thread that executes queued actions.
    /// </summary>
    public void Start()
    {
        if (m_IsStarted == true || m_IsShutdown == true)
            return;

        m_IsStarted = true;
        m_Thread.Start();
    }

    /// <summary>
    /// Causes the background thread that executes actions to terminate. The background worker task may terminate until the action that is 
    /// currently being executed completes or a maximum of 500 ms elapses.
    /// </summary>
    public void Shutdown()
    {
        if (m_IsStarted == false || m_IsShutdown == true)
            return;

        m_IsShutdown = true;
        m_Semaphore.Release();
        m_Thread.Join(500);
    }

    /// <summary>
    /// Enqueues an action and signals the background worker thread to wake up and dequeue actions from the internal action queue and 
    /// perform them.
    /// </summary>
    /// <param name="action">Action to be queued and executed.</param>
    public void EnqueueWork(Action action)
    {
        m_WorkQueue.Enqueue(action);
        m_Semaphore.Release();
    }

    /// <summary>
    /// This method is called from within the thread context of the worker thread managed by this class. The base class implementation
    /// performs no work. This method must be overridden by the derived class if periodic actions are required.
    /// <para>The method is called once for each iteration of the thread's main loop. The maximum period between calls is specified by the 
    /// WaitIntervalMsec constructor parameter.</para>
    /// </summary>
    protected virtual void DoTimedEvents()
    {
    }

    private void ThreadLoop()
    {
        while (m_IsShutdown == false)
        {
            m_Semaphore.Wait(m_WaitIntervalMsec);

            DoTimedEvents();

            while (m_IsShutdown == false && m_WorkQueue.TryDequeue(out Action? action) == true)
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
}
