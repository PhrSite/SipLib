/////////////////////////////////////////////////////////////////////////////////////
//  File:   SipLogger.cs                                            5 Feb 24 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;
using System.Reflection;

/// <summary>
/// <para>
/// Class for logging debug, information, warning, error and critical messages. This class is used by the
/// classes in the SipLib class library to log application messages. By default, this class logs all messages
/// to a NullLogger. This means that no messages are logged.
/// </para>
/// <para>
/// The consumer of the SipLib class libray must configure this class by constructing an Ilogger interface and
/// call the Log set property. It is possible to configure this class to log messages to a separate logging
/// destination. It is also possible to configure this class to log messages to the same logging destination as 
/// the consummer (an application or another class library) of this class library. It is also possible for the
/// consumer of this class library to use this class directly to log application messages.
/// </para>
/// <para>
/// See <a href="~/articles/ConfigureSipLibLogging.md">Configuring SipLib Logging</a>.
/// </para>
/// </summary>
public static class SipLogger
{
    /// <summary>
    /// Sets the ILogger interface that this class will use to log messages.
    /// </summary>
    /// <value></value>
    public static ILogger Log { private get; set; } = NullLogger.Instance;

    private static object m_lock = new object();

    private static string FormatMessage(string message)
    {
        // Go down two levels in the stack to skip the call to this method and to skip the call to the
        // LogXXX() method that called this method.
        MethodBase? m = new StackTrace()?.GetFrame(2)?.GetMethod();
        string strClass = m?.ReflectedType?.Name ?? "Unknown";
        string strMethod = m?.Name ?? "Unknown";
        return $"{strClass}.{strMethod}() {message}";
    }

    /// <summary>
    /// Logs a Debug level message
    /// </summary>
    /// <param name="message">Message to log</param>
    public static void LogDebug(string message)
    {
        lock (m_lock)
        {
            if (Log != NullLogger.Instance)
                Log.LogDebug(FormatMessage(message));
        }
    }

    /// <summary>
    /// Logs a Debug level message with an exception that occurred
    /// </summary>
    /// <param name="exception">Exception that occurred</param>
    /// <param name="message">Message to log</param>
    public static void LogDebug(Exception exception, string message)
    {
        lock (m_lock)
        {
            if (Log != NullLogger.Instance)
                Log.LogDebug(exception, FormatMessage(message));
        }
    }

    /// <summary>
    /// Logs an Information level message.
    /// </summary>
    /// <param name="message">Message to log</param>
    public static void LogInformation(string message)
    {
        lock (m_lock)
        {
            if (Log != NullLogger.Instance)
                Log.LogInformation(FormatMessage(message));
        }
    }

    /// <summary>
    /// Logs an Information level message with an exception that occurred.
    /// </summary>
    /// <param name="exception">Exception that occurred</param>
    /// <param name="message">Message to log</param>
    public static void LogInformation(Exception exception, string message)
    {
        lock (m_lock)
        {
            if (Log != NullLogger.Instance)
                Log.LogInformation(exception, FormatMessage(message));
        }
    }

    /// <summary>
    /// Logs a Warning level message
    /// </summary>
    /// <param name="message">Message to log</param>
    public static void LogWarning(string message)
    {
        lock (m_lock)
        {
            if (Log != NullLogger.Instance)
                Log.LogWarning(FormatMessage(message));
        }
    }

    /// <summary>
    /// Logs a Warning level message with an exception
    /// </summary>
    /// <param name="exception">Exception that occurred</param>
    /// <param name="message">Message to log</param>
    public static void LogWarning(Exception exception, string message)
    {
        lock (m_lock)
        {
            if (Log != NullLogger.Instance)
                Log.LogWarning(exception, FormatMessage(message));
        }
    }

    /// <summary>
    /// Logs an Error level message
    /// </summary>
    /// <param name="message">Message to log</param>
    public static void LogError(string message)
    {
        lock (m_lock)
        {
            if (Log != NullLogger.Instance)
                Log.LogError(FormatMessage(message));
        }
    }

    /// <summary>
    /// Logs an Error level message with an exception that occurred
    /// </summary>
    /// <param name="exception">Exception that occurred</param>
    /// <param name="message">Message to log</param>
    public static void LogError(Exception exception, string message)
    {
        lock (m_lock)
        {
            if (Log != NullLogger.Instance)
                Log.LogError(exception, FormatMessage(message));
        }
    }

    /// <summary>
    /// Logs a Critical level message
    /// </summary>
    /// <param name="message">Message to log</param>
    public static void LogCritical(string message)
    {
        lock (m_lock)
        {
            if (Log != NullLogger.Instance)
                Log.LogCritical(FormatMessage(message));
        }
    }

    /// <summary>
    /// Logs a Critical level message with an exception that occurred
    /// </summary>
    /// <param name="exception">Exception that occurred</param>
    /// <param name="message">Message to log</param>
    public static void LogCritical(Exception exception, string message)
    {
        lock (m_lock)
        {
            if (Log != NullLogger.Instance)
                Log.LogCritical(exception, FormatMessage(message));
        }
    }
}
