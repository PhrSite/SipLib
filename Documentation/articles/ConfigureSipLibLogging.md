# Configuring SipLib Logging
The SipLib class library includes a class called [SipLogger](~/api/SipLib.Logging.SipLogger.yml) that the other classes in this class library can use for logging application log messages.

By default, the SipLogger class logs to a NullLogger. This means that no messages are logged. The consumer of the SipLib class librarry must configure the SipLogger class so that it writes log messages to a logging destination such as a file or the console.

The best way to configure SipLib logging is to use a logging framework such as [Serilog](https://serilog.net) to create a ILogger interface and then pass that interface to SipLogger using the Log setter property. The following code snippet shows how an application can configure the SipLibLogger class.

```
using Serilog;
using Serilog.Core;
using Serilog.Extensions.Logging;
using SipLib.Logging;

internal static class Program
{
    private const string LoggingDirectory = @"\LoggingFolder";
    private const string LoggingFileName = "LoggingFile.log";
    private const int LoggingFileSizeBytes = 1000000;
	private const int MaxLogFiles = 5;

    private static LoggingLevelSwitch m_LevelSwitch = new LoggingLevelSwitch();

    [STAThread]
    static void Main()
    {
        if (Directory.Exists(LoggingDirectory) == false)
            Directory.CreateDirectory(LoggingDirectory);

        string LoggingPath = Path.Combine(LoggingDirectory, LoggingFileName);
        Logger logger = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(m_LevelSwitch)
            .WriteTo.File(LoggingPath, fileSizeLimitBytes: LoggingFileSizeBytes, retainedFileCountLimit: MaxLogFiles,
            outputTemplate: "{Timestamp:yyyy-MM-ddTHH:mm:ss.ffffffzzz} [{Level}] {Message}{NewLine}{Exception}")
            .CreateLogger();

        SerilogLoggerFactory factory = new SerilogLoggerFactory(loggger);
        SipLogger.Log = factory.CreateLogger("MyCategoryName");

        SipLogger.LogInformation("Starting the application now");

        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        Application.Run(new Form1());

        SipLogger.LogInformation("Exiting the application now");
    }

    public static void SetLoggingLevel(LogEventLevel level)
    {
        m_LevelSwitch.MinimumLevel = level;
    }
}

```

The SipLogger class adds the class name and the method name of the method logging a message to the message being logged. For example, the configuration shown above will result in log messages that look like:

```
2024-02-05T19:57:18.127145-08:00 [Information] Program.Main() Starting the application now
```

The LoggingLevelSwitch object can be used by the application to dynamically control the logging level. A call to the SetLoggingLevel method with a value of LogEventLevel.Error sets the minimum logging level to Error.


