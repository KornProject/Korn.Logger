using Korn.Utils.System;
using System.Diagnostics;
using System.Reflection;

namespace Korn.Utils.Logger;
public static class KornLogger
{
    static bool initialized;
    public static void EnsureInitialized()
    {
        if (initialized) 
            return;
        initialized = true;

        Initialize();
    }

    static string? cachedKornLogPath;
    static string? cachedTempKornLogPath;
    const string KORN_PATH_VARIABLE_NAME = "KORN_PATH";
    public static string GetLogFilePath()
    {
        if (cachedKornLogPath is not null)
            return cachedKornLogPath;

        var kornPath = SystemVariablesUtils.GetVariable(KORN_PATH_VARIABLE_NAME);
        if (kornPath is null)
            return GetTempLogFilePath();

        var logFilePath = Path.Combine(kornPath, "log.txt");
        if (!File.Exists(logFilePath))
            File.Create(logFilePath).Dispose();

        return cachedKornLogPath = logFilePath;
    }

    static string GetTempLogFilePath()
    {
        if (cachedTempKornLogPath is not null)
            return cachedTempKornLogPath;

        var tempLogFileName = $"KornTempLog-{DateTime.Now.ToString("yy'_'MM'_'dd' 'HH'_'mm'_'ss")}";
        var tempLogFilePath = Path.Combine(Path.GetTempPath(), tempLogFileName);

        if (!File.Exists(tempLogFilePath))
            File.Create(tempLogFilePath).Dispose();

        return cachedTempKornLogPath = tempLogFilePath;
    }

    static void Initialize()
    {
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            if (exception is null)
                return;

            WriteUnhandledException(exception);
        }
    }

    static void InternalWrite(string text)
    {
        using (var mutex = new Mutex(false, "Korn logger mutex"))
        {
            mutex.WaitOne();
            File.AppendAllText(GetLogFilePath(), text);
            mutex.ReleaseMutex();
        }
    }

    public static void Write(string text) => InternalWrite(text);

    public static void RawWriteLine(string text) => Write(text + '\n');

    public static void WriteLine(object obj) => WriteLine(obj is null ? "{null}" : obj.ToString()??"{null}");
        
    public static void WriteLine(string text)
        => Write($"{DateTime.Now:yy/MM/dd HH:mm:ss.fff} [{Assembly.GetExecutingAssembly().GetName().Name}/{Thread.CurrentThread.Name}] " + text + '\n');

    public static void WriteMessage(string message) => WriteLine("[Message] " + message);

    public static void WriteException(Exception exception) => WriteLine("[Exception] " + exception.ToString());

    public static void WriteUnhandledException(Exception exception) => WriteLine("[Unhandled exception] " + exception.ToString());

    public static void WriteExpectedException(Exception exception) => WriteLine("[Expected exception] " + exception.ToString());

    public static void WriteError(string errorMessage, bool appendStacktrace = false)
        => WriteLine("[Error] " + errorMessage + (appendStacktrace ? $"Stacktrace: \n{Environment.StackTrace}" : ""));

    public static void WriteExpectedError(string errorMessage, bool appendStacktrace = false)
    => WriteLine("[Expected error] " + errorMessage + (appendStacktrace ? $"Stacktrace: \n{Environment.StackTrace}" : ""));

    public static void Message(string message)
    {
        WriteMessage(message);
        Interop.MessageBox(message, "Message");
    }

    public static void Error(string message)
    {
        WriteError(message);
        Interop.MessageBox(message, "Error");
    }

    public static void ExpectedError(string message)
    {
        WriteExpectedError(message);
        Interop.MessageBox(message, "Expected error");
    }

    public static void Exception(Exception exception)
    {
        WriteException(exception);
        Interop.MessageBox(exception.ToString(), "Exception");
    }

    public static void ExpectedException(Exception exception)
    {
        WriteExpectedException(exception);
        Interop.MessageBox(exception.ToString(), "Expected exception");
    }

    public static void UnhandledException(Exception exception)
    {
        WriteUnhandledException(exception);
        Interop.MessageBox(exception.ToString(), "Unhandled exception");
    }
}