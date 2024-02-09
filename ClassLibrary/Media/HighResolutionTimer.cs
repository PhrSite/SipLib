/////////////////////////////////////////////////////////////////////////////////////
//  File:   HighResolutionTimer.cs                                  3 Jan 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

using System.Diagnostics;

namespace SipLib.Media;

/// <summary>
/// Delegate type for the TimerExpired event of the HighResolutionTimer class.
/// </summary>
public delegate void HighResolutionTimerDelegate();

/// <summary>
/// This class implements a high resolution periodic timer for generating media samples. It provides a timer
/// that uses a dedicated thread and is capable of providing timed events with a maximum jitter of less than
/// a millisecond and an average jitter of less that 0.1 milliseconds. An instance of this class may be used
/// for multiple media sources.
/// </summary>
public class HighResolutionTimer
{
    private double m_TimerPeriodMs;

    /// <summary>
    /// Event that is fired when the timer expires.
    /// </summary>
    /// <value></value>
    public event HighResolutionTimerDelegate? TimerExpired = null;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="timerPeriodMs">Timer period in milliseconds. Typically 20 ms for audio sources and
    /// 33.3 milliseconds for video sources.</param>
    public HighResolutionTimer(double timerPeriodMs)
    {
        m_TimerPeriodMs = timerPeriodMs;
    }

    private bool m_IsEnding = false;
    private Thread? m_Thread;

    /// <summary>
    /// Starts the timer.
    /// </summary>
    public void Start()
    {
        if (m_IsEnding == true)
            return;

        m_IsEnding = false;
        m_Thread = new Thread(TimerThread);
        
        m_Thread.Priority = ThreadPriority.Highest;
        m_Thread.IsBackground = true;
        m_Thread.Start();
    }

    /// <summary>
    /// Stops the timer. Do not call Start() after Stop() is called.
    /// </summary>
    public void Stop()
    {
        m_IsEnding = true;
        m_Thread.Join();
        m_Thread = null;
    }

    private void TimerThread()
    {
        Stopwatch stopwatch = new Stopwatch();
        long PeriodInTicks = (long)(Stopwatch.Frequency * (m_TimerPeriodMs / 1000));
        long CurrentPeriodInTicks = PeriodInTicks;
        long Delta;
        long ElapsedTicks;
        stopwatch.Start();

        while (m_IsEnding == false)
        {
            ElapsedTicks = stopwatch.ElapsedTicks;
            if (ElapsedTicks >= CurrentPeriodInTicks)
            {
                TimerExpired?.Invoke();
                Thread.Sleep(0);
                Delta = stopwatch.ElapsedTicks - ElapsedTicks;
                CurrentPeriodInTicks = PeriodInTicks - Delta;
                stopwatch.Restart();
            }
        }
    }
}
