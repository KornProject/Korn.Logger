using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;
using Korn.Logger.Core;
using System.Threading;
using System;

namespace Korn.Logger
{
    public class KornLogger
    {
        internal static LoggerClient LoggerClient = new LoggerClient();

        public KornLogger(string logPath)
        {
            var instanceId = Convert.ToString(Process.GetCurrentProcess().Id, 16).PadRight(5);
            Implementation = new InternalImplementation(instanceId);
            Task.Run(() =>
            {
                var loggerHandle = LoggerClient.GetLoggerHandle(logPath);
                Implementation.SetLoggerHandle(loggerHandle);
            });
        }

        internal InternalImplementation Implementation;

        public void Write(string text) => Implementation.Write(text);
        public void Clear() => Implementation.Clear();
        public void WriteLineWithoutTags(string text) => Implementation.WriteLineWithoutTags(text);
        public void WriteLine(object obj) => Implementation.WriteLine(obj is null ? "{null}" : obj.ToString() ?? "{null}", Assembly.GetCallingAssembly());
        public void WriteMessage(string message) => Implementation.WriteMessage(message, Assembly.GetCallingAssembly());
        public void WriteWarning(params string[] message) => Implementation.WriteWarning(message, Assembly.GetCallingAssembly());
        public void WriteException(Exception exception) => Implementation.WriteException(exception, Assembly.GetCallingAssembly());
        public void WriteUnhandledException(Exception exception) => Implementation.WriteUnhandledException(exception, Assembly.GetCallingAssembly());
        public void WriteExpectedException(Exception exception) => Implementation.WriteExpectedException(exception, Assembly.GetCallingAssembly());
        public void WriteUnexpectedError(string errorMessage, bool appendStacktrace = false) => Implementation.WriteUnexpectedError(errorMessage, appendStacktrace, Assembly.GetCallingAssembly());
        public void WriteError(string errorMessage, bool appendStacktrace = false) => Implementation.WriteLine("[Error] " + errorMessage + (appendStacktrace ? $"Stacktrace: \n{Environment.StackTrace}" : ""), Assembly.GetCallingAssembly());
        public void Message(string[] messageLines) => Implementation.Message(messageLines, Assembly.GetCallingAssembly());
        public void Message(string message) => Implementation.Message(message, Assembly.GetCallingAssembly());
        public void UnxepectedError(string[] messageLines) => Implementation.UnxepectedError(messageLines, Assembly.GetCallingAssembly());
        public void UnxepectedError(string message) => Implementation.UnxepectedError(message, Assembly.GetCallingAssembly());
        public void Error(string[] messageLines) => Implementation.Error(messageLines, Assembly.GetCallingAssembly());
        public void Error(string message) => Implementation.Error(message, Assembly.GetCallingAssembly());
        public void Exception(Exception exception) => Implementation.Exception(exception, Assembly.GetCallingAssembly());

        internal class InternalImplementation
        {
            public InternalImplementation(string instanceId)
                => InstanceID = instanceId;

            public LoggerHandle LoggerHandle = LoggerHandle.Invalid;
            public readonly string InstanceID;

            public void SetLoggerHandle(LoggerHandle handle)
            {
                LoggerHandle = handle;

                if (handle.IsValid)
                    HasConnection();
            }

            bool hasNoConnectionData;
            List<string> noConnectionWriteBuffer = new List<string>();
            void InternalWrite(string text)
            {
                if (HasConnection())
                    LoggerClient.Write(LoggerHandle, text);
                else noConnectionWriteBuffer.Add(text);        
            }

            bool noConnectionClearRequest;
            void InternalClear()
            {
                if (HasConnection())
                    LoggerClient.Clear(LoggerHandle);
                else
                {
                    noConnectionClearRequest = true;
                    noConnectionWriteBuffer.Clear();
                }
            }

            bool HasConnection()
            {
                var handle = LoggerHandle;
                var isValid = handle.IsValid;

                if (isValid)
                {
                    if (hasNoConnectionData)
                    {
                        hasNoConnectionData = false;

                        if (noConnectionClearRequest)
                            LoggerClient.Clear(handle);

                        var buffer = noConnectionWriteBuffer;
                        foreach (var message in buffer)
                            LoggerClient.Write(handle, message);
                        buffer.Clear();
                    }
                }
                else hasNoConnectionData = true;

                return isValid;
            }

            string GetThreadTag() => string.IsNullOrEmpty(Thread.CurrentThread.Name) ? "" : Thread.CurrentThread.Name == ".NET TP Worker" ? "" : $"/{Thread.CurrentThread.Name}";

            public void Clear() => InternalClear();
            public void Write(string text) => InternalWrite(text);
            public void WriteLineWithoutTags(string text) => Write(text + '\n');
            public void WriteLine(string text, Assembly assembly) => Write($"{DateTime.Now:yy/MM/dd HH:mm:ss.fff} " + /*$"{InstanceID} [{assembly.GetName().Name}{GetThreadTag()}] " +*/ text + '\n');
            public void WriteMessage(string message, Assembly assembly) => WriteLine(message, assembly);
            public void WriteWarning(string[] message, Assembly assembly) => WriteLine("[Warning] " + string.Join(" ", message), assembly);
            public void WriteException(Exception exception, Assembly assembly) => WriteLine("[Exception] " + exception.ToString(), assembly);
            public void WriteUnhandledException(Exception exception, Assembly assembly) => WriteLine("[Unhandled exception] " + exception.ToString(), assembly);
            public void WriteExpectedException(Exception exception, Assembly assembly) => WriteLine("[Expected exception] " + exception.ToString(), assembly);
            public void WriteUnexpectedError(string errorMessage, bool appendStacktrace, Assembly assembly) => WriteLine("[Unexpected error] " + errorMessage + (appendStacktrace ? $"Stacktrace: \n{Environment.StackTrace}" : ""), assembly);
            public void WriteError(string errorMessage, bool appendStacktrace, Assembly assembly) => WriteLine("[Error] " + errorMessage + (appendStacktrace ? $"Stacktrace: \n{Environment.StackTrace}" : ""), assembly);
            public void Message(string[] messageLines, Assembly assembly) => Message(string.Join("\n", messageLines), assembly);

            public void Message(string message, Assembly assembly)
            {
                WriteMessage(message, assembly);
                LocalInterop.MessageBox(message, "Korn message");
            }

            public void UnxepectedError(string[] messageLines, Assembly assembly) => UnxepectedError(string.Join(" ", messageLines), assembly);
            public void UnxepectedError(string message, Assembly assembly)
            {
                WriteUnexpectedError(message, true, assembly);
                LocalInterop.MessageBox(message, "Korn unexpected error");
            }

            public void Error(string[] messageLines, Assembly assembly) => Error(string.Join(" ", messageLines), assembly);
            public void Error(string message, Assembly assembly)
            {
                WriteError(message, false, assembly);
                LocalInterop.MessageBox(message, "Korn error");
            }

            public void Exception(Exception exception, Assembly assembly)
            {
                WriteException(exception, assembly);
                LocalInterop.MessageBox(exception.ToString(), "Korn exception");
            }

            public void ExpectedException(Exception exception, Assembly assembly)
            {
                WriteExpectedException(exception, assembly);
                LocalInterop.MessageBox(exception.ToString(), "Korn expected exception");
            }

            public void HandleException(Exception exception, Assembly assembly)
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
        }
    }

    public abstract class BaseKornException : Exception
    {
        static BaseKornException()
        {
            var assembly = AppDomain.CurrentDomain.Load("Korn.Core");
            if (assembly == null)
                return;

            var type = assembly.GetType("Korn.Shared.KornShared");
            if (type == null)
                return;

            RuntimeHelpers.RunClassConstructor(type.TypeHandle);
        }

        static KornLogger ExceptionLogger;
        public static bool HasBindedLogger() => ExceptionLogger != null;
        public static void BindLogger(KornLogger logger) => ExceptionLogger = logger;

        public BaseKornException(string[] messageLines, bool showMessageBox = true)
            : this(string.Join(string.Empty, messageLines), showMessageBox) { }

        public BaseKornException(Exception exception, bool showMessageBox = true)
            : base("Empty exception message", exception) { }

        public BaseKornException(string message, Exception exception, bool showMessageBox = true)
            : base(message, exception) { }

        public BaseKornException(string message, bool showMessageBox = true) : base(message)
        {
            ShowMessageBox = showMessageBox;
            ExceptionLogger.Implementation.HandleException(this, Assembly.GetCallingAssembly());

            throw this;
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

        public KornException(string[] messageLines, bool showMessageBox)
            : base(messageLines, showMessageBox) { }

        public KornException(params string[] messageLines)
            : base(messageLines, true) { }
    }

    public class KornExpectedException : BaseKornException
    {
        public KornExpectedException(string message, Exception exception, bool showMessageBox = true)
            : base($"{message}: {exception}", showMessageBox) { }

        public KornExpectedException(Exception exception, bool showMessageBox = true)
            : base(exception.ToString(), showMessageBox) { }

        public KornExpectedException(string message, bool showMessageBox = true)
            : base(message, showMessageBox) { }

        public KornExpectedException(string[] messageLines, bool showMessageBox)
            : base(messageLines, showMessageBox) { }

        public KornExpectedException(params string[] messageLines)
            : base(messageLines, true) { }
    }

    public class KornUnexpectedError : BaseKornException
    {
        public KornUnexpectedError(string message, bool showMessageBox = true)
            : base(message, showMessageBox) { }

        public KornUnexpectedError(string[] messageLines, bool showMessageBox)
            : base(messageLines, showMessageBox) { }

        public KornUnexpectedError(params string[] messageLines)
            : base(messageLines, true) { }
    }

    public class KornError : BaseKornException
    {
        public KornError(string message, bool showMessageBox = true)
            : base(message, showMessageBox) { }

        public KornError(string[] messageLines, bool showMessageBox)
            : base(messageLines, showMessageBox) { }

        public KornError(params string[] messageLines)
            : base(messageLines, true) { }
    }
}