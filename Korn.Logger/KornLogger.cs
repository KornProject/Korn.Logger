using Korn.Logger.Internal;
using Korn.Utils;
using System.Reflection;

namespace Korn;
public static class KornLogger
{
    public static void Write(string text) => Internal.Write(text);
    public static void WriteLineWithoutTags(string text) => Internal.WriteLineWithoutTags(text);
    public static void WriteLine(object obj) => Internal.WriteLine(obj is null ? "{null}" : obj.ToString() ?? "{null}", Assembly.GetCallingAssembly());
    public static void WriteMessage(string message) => Internal.WriteMessage(message, Assembly.GetCallingAssembly());
    public static void WriteException(Exception exception) => Internal.WriteException(exception, Assembly.GetCallingAssembly());
    public static void WriteUnhandledException(Exception exception) => Internal.WriteUnhandledException(exception, Assembly.GetCallingAssembly());
    public static void WriteExpectedException(Exception exception) => Internal.WriteExpectedException(exception, Assembly.GetCallingAssembly());
    public static void WriteUnexpectedError(string errorMessage, bool appendStacktrace = false) => Internal.WriteUnexpectedError(errorMessage, appendStacktrace, Assembly.GetCallingAssembly());        
    public static void WriteError(string errorMessage, bool appendStacktrace = false) => Internal.WriteLine("[Error] " + errorMessage + (appendStacktrace ? $"Stacktrace: \n{Environment.StackTrace}" : ""), Assembly.GetCallingAssembly());
    public static void Message(string[] messageLines) => Internal.Message(messageLines, Assembly.GetCallingAssembly());
    public static void Message(string message) => Internal.Message(message, Assembly.GetCallingAssembly());
    public static void UnxepectedError(string[] messageLines) => Internal.UnxepectedError(messageLines, Assembly.GetCallingAssembly());
    public static void UnxepectedError(string message) => Internal.UnxepectedError(message, Assembly.GetCallingAssembly());
    public static void Error(string[] messageLines) => Internal.Error(messageLines, Assembly.GetCallingAssembly());
    public static void Error(string message) => Internal.Error(message, Assembly.GetCallingAssembly());
    public static void Exception(Exception exception) => Internal.Exception(exception, Assembly.GetCallingAssembly());
    public static void ExpectedException(Exception exception) => Internal.ExpectedException(exception, Assembly.GetCallingAssembly());
    public static void UnhandledException(Exception exception) => Internal.UnhandledException(exception, Assembly.GetCallingAssembly());

    public static class Internal
    {
        static Internal()
        {
            LogPath =
                (KornPath = SystemVariablesUtils.GetKornPath()) is not null
                ? Path.Combine(KornPath, @"Data\log.txt")
                : GetTempLogFilePath();
        }

        static readonly string? KornPath;
        static readonly string LogPath;

        static string GetTempLogFilePath()
        {
            var tempLogFileName = $"KornTempLog-{DateTime.Now.ToString("yy'_'MM'_'dd' 'HH'_'mm'_'ss")}";
            var tempLogFilePath = Path.Combine(Path.GetTempPath(), tempLogFileName);

            if (!File.Exists(tempLogFilePath))
                File.Create(tempLogFilePath).Dispose();

            return tempLogFilePath;
        }

        public static void InternalWrite(string text)
        {
            using (var mutex = new Mutex(false, "Korn logger mutex"))
            {
                mutex.WaitOne();
                File.AppendAllText(LogPath, text);
                mutex.ReleaseMutex();
            }
        }

        public static void HandleException(Exception exception, Assembly assembly)
        {
            switch (exception)
            {
                case KornException kornException:
                    if (kornException.ShowMessageBox)
                        Exception(kornException, assembly);
                    else WriteException(kornException, assembly);
                    break;

                case KornExpectedException kornExpectedException:
                    if (kornExpectedException.ShowMessageBox)
                        ExpectedException(kornExpectedException, assembly);
                    else WriteExpectedException(kornExpectedException, assembly);
                    break;

                case KornUnexpectedError kornError:
                    if (kornError.ShowMessageBox)
                        UnxepectedError(kornError.Message, assembly);
                    else WriteUnexpectedError(kornError.Message, true, assembly);
                    break;

                case KornError kornExpectedError:
                    if (kornExpectedError.ShowMessageBox)
                        Error(kornExpectedError.Message, assembly);
                    else WriteError(kornExpectedError.Message, true, assembly);
                    break;

                default:
                    WriteUnhandledException(exception, assembly);
                    break;
            }
        }

        public static void Write(string text) => InternalWrite(text);
        public static void WriteLineWithoutTags(string text) => Write(text + '\n');
        public static void WriteLine(string text, Assembly assembly) => Write($"{DateTime.Now:yy/MM/dd HH:mm:ss.fff} [{assembly.GetName().Name}{(string.IsNullOrEmpty(Thread.CurrentThread.Name) ? "" : $"/{Thread.CurrentThread.Name}")}] " + text + '\n');
        public static void WriteMessage(string message, Assembly assembly) => WriteLine(message, assembly);
        public static void WriteException(Exception exception, Assembly assembly) => WriteLine("[Exception] " + exception.ToString(), assembly);
        public static void WriteUnhandledException(Exception exception, Assembly assembly) => WriteLine("[Unhandled exception] " + exception.ToString(), assembly);
        public static void WriteExpectedException(Exception exception, Assembly assembly) => WriteLine("[Expected exception] " + exception.ToString(), assembly);
        public static void WriteUnexpectedError(string errorMessage, bool appendStacktrace, Assembly assembly) => WriteLine("[Unexpected error] " + errorMessage + (appendStacktrace ? $"Stacktrace: \n{Environment.StackTrace}" : ""), assembly);
        public static void WriteError(string errorMessage, bool appendStacktrace, Assembly assembly) => WriteLine("[Error] " + errorMessage + (appendStacktrace ? $"Stacktrace: \n{Environment.StackTrace}" : ""), assembly);
        public static void Message(string[] messageLines, Assembly assembly) => Message(string.Join('\n', messageLines), assembly);
        
        public static void Message(string message, Assembly assembly)
        {
            WriteMessage(message, assembly);
            Interop.MessageBox(message, "Korn message");
        }

        public static void UnxepectedError(string[] messageLines, Assembly assembly) => UnxepectedError(string.Join(' ', messageLines), assembly);
        public static void UnxepectedError(string message, Assembly assembly)
        {
            WriteUnexpectedError(message, true, assembly);
            Interop.MessageBox(message, "Korn unexpected error");
        }

        public static void Error(string[] messageLines, Assembly assembly) => Error(string.Join(' ', messageLines), assembly);
        public static void Error(string message, Assembly assembly)
        {
            WriteError(message, false, assembly);
            Interop.MessageBox(message, "Korn error");
        }

        public static void Exception(Exception exception, Assembly assembly)
        {
            WriteException(exception, assembly);
            Interop.MessageBox(exception.ToString(), "Korn exception");
        }

        public static void ExpectedException(Exception exception, Assembly assembly)
        {
            WriteExpectedException(exception, assembly);
            Interop.MessageBox(exception.ToString(), "Korn expected exception");
        }

        public static void UnhandledException(Exception exception, Assembly assembly)
        {
            WriteUnhandledException(exception, assembly);
            Interop.MessageBox(exception.ToString(), "Korn unhandled exception");
        }
    }
}

public abstract class BaseKornException : Exception
{
    public BaseKornException(string[] messageLines, bool showMessageBox = true)
        : this(string.Join(' ', messageLines), showMessageBox) { }

    public BaseKornException(Exception exception, bool showMessageBox = true)
        : base("Empty exception message", exception) { }

    public BaseKornException(string message, Exception exception, bool showMessageBox = true)
        : base(message, exception) { }

    public BaseKornException(string message, bool showMessageBox = true) : base(message)
    {
        ShowMessageBox = showMessageBox;
        KornLogger.Internal.HandleException(this, Assembly.GetCallingAssembly());
    }

    public bool ShowMessageBox;
}

public class KornException : BaseKornException
{
    public KornException(string message, Exception exception, bool showMessageBox = true)
        : base(message, exception, showMessageBox) { }

    public KornException(Exception exception, bool showMessageBox = true)
        : base(exception, showMessageBox) { }

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