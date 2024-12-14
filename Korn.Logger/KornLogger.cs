using Korn.Logger.Internal;
using Korn.Utils;
using System.Reflection;

namespace Korn;
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
        AppDomain.CurrentDomain.UnhandledException += UnhandledException;

        void UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            if (exception is null)
                return;

            switch (exception)
            {
                case KornException kornException:
                    if (kornException.ShowMessageBox)
                        Exception(kornException);
                    else WriteException(kornException);
                    break;

                case KornExpectedException kornExpectedException:
                    if (kornExpectedException.ShowMessageBox)
                        ExpectedException(kornExpectedException);
                    else WriteExpectedException(kornExpectedException);
                    break;

                case KornUnexpectedError kornError:
                    if (kornError.ShowMessageBox)
                        UnxepectedError(kornError.Message);
                    else WriteUnexpectedError(kornError.Message);
                    break;

                case KornError kornExpectedError:
                    if (kornExpectedError.ShowMessageBox)
                        Error(kornExpectedError.Message);
                    else WriteError(kornExpectedError.Message);
                    break;

                default:
                    WriteUnhandledException(exception);
                    break;
            }
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

    public static void WriteUnexpectedError(string errorMessage, bool appendStacktrace = false)
        => WriteLine("[Unexpected error] " + errorMessage + (appendStacktrace ? $"Stacktrace: \n{Environment.StackTrace}" : ""));

    public static void WriteError(string errorMessage, bool appendStacktrace = false)
    => WriteLine("[Error] " + errorMessage + (appendStacktrace ? $"Stacktrace: \n{Environment.StackTrace}" : ""));

    public static void Message(string[] messageLines) => Message(string.Join('\n', messageLines));
    public static void Message(string message)
    {
        WriteMessage(message);
        Interop.MessageBox(message, "Korn Message");
    }

    public static void Error(string[] messageLines) => UnxepectedError(string.Join(' ', messageLines));
    public static void UnxepectedError(string message)
    {
        WriteUnexpectedError(message);
        Interop.MessageBox(message, "Korn unexpected error");
    }

    public static void ExpectedError(string[] messageLines) => Error(string.Join(' ', messageLines));
    public static void Error(string message)
    {
        WriteError(message);
        Interop.MessageBox(message, "Korn error");
    }

    public static void Exception(Exception exception)
    {
        WriteException(exception);
        Interop.MessageBox(exception.ToString(), "Korn exception");
    }

    public static void ExpectedException(Exception exception)
    {
        WriteExpectedException(exception);
        Interop.MessageBox(exception.ToString(), "Korn expected exception");
    }

    public static void UnhandledException(Exception exception)
    {
        WriteUnhandledException(exception);
        Interop.MessageBox(exception.ToString(), "Korn unhandled exception");
    }
}

public abstract class BaseKornException : Exception
{
    static BaseKornException() => KornLogger.EnsureInitialized();

    public BaseKornException(string message, bool showMessageBox)
        => (Message, ShowMessageBox) = (message, showMessageBox);

    public BaseKornException(string[] messageLines, bool showMessageBox = true)
        : this(string.Join(' ', messageLines), showMessageBox) { }

    public new string Message;
    public bool ShowMessageBox;
}

public class KornException : BaseKornException
{
    public KornException(string message, Exception exception, bool showMessageBox = true)
        : base($"{message}: {exception}", showMessageBox) { }

    public KornException(Exception exception, bool showMessageBox = true)
        : base(exception.ToString(), showMessageBox) { }

    public KornException(string message, bool showMessageBox = true)
        : base(message, showMessageBox) { }

    public KornException(string[] messageLines, bool showMessageBox = true)
        : base(messageLines, showMessageBox) { }
}

public class KornExpectedException : BaseKornException
{
    public KornExpectedException(string message, Exception exception, bool showMessageBox = true)
    : base($"{message}: {exception}", showMessageBox) { }

    public KornExpectedException(Exception exception, bool showMessageBox = true)
        : base(exception.ToString(), showMessageBox) { }

    public KornExpectedException(string message, bool showMessageBox = true)
        : base(message, showMessageBox) { }

    public KornExpectedException(string[] messageLines, bool showMessageBox = true)
        : base(messageLines, showMessageBox) { }
}

public class KornUnexpectedError : BaseKornException
{
    public KornUnexpectedError(string message, bool showMessageBox = true)
        : base(message, showMessageBox) { }

    public KornUnexpectedError(string[] messageLines, bool showMessageBox = true)
        : base(messageLines, showMessageBox) { }
}

public class KornError : BaseKornException
{
    public KornError(string message, bool showMessageBox = true) 
        : base(message, showMessageBox) { }

    public KornError(string[] messageLines, bool showMessageBox = true)
        : base(messageLines, showMessageBox) { }
}