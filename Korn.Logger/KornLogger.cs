using Korn.Logger.Internal;
using Korn.Shared.Internal;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;

namespace Korn
{
    public class KornLogger
    {
        public KornLogger(string logPath)
        {
            Implementation = new Internal(logPath, $"{Process.GetCurrentProcess().Id:X}".PadRight(5));

            if (!File.Exists(logPath))
                File.Create(logPath).Dispose();
        }

        Internal Implementation;

        public void Write(string text) => Implementation.Write(text);
        public void WriteLineWithoutTags(string text) => Implementation.WriteLineWithoutTags(text);
        public void WriteLine(object obj) => Implementation.WriteLine(obj is null ? "{null}" : obj.ToString() ?? "{null}", Assembly.GetCallingAssembly());
        public void WriteMessage(string message) => Implementation.WriteMessage(message, Assembly.GetCallingAssembly());
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

        class Internal
        {
            public Internal(string logPath, string instanceId)
            {
                LogPath = logPath;
                InstanceID = instanceId;
            }

            public readonly string LogPath;
            public readonly string InstanceID;

            void InternalWrite(string text)
            {
                using (var mutex = new Mutex(false, $"{LogPath.GetHashCode():X}lm"))
                {
                    mutex.WaitOne();
                    File.AppendAllText(LogPath, text);
                    mutex.ReleaseMutex();
                }                    
            }

            public void Write(string text) => InternalWrite(text);
            public void WriteLineWithoutTags(string text) => Write(text + '\n');
            public void WriteLine(string text, Assembly assembly) => Write($"{DateTime.Now:yy/MM/dd HH:mm:ss.fff} {InstanceID} [{assembly.GetName().Name}{(string.IsNullOrEmpty(Thread.CurrentThread.Name) ? "" : $"/{Thread.CurrentThread.Name}")}] " + text + '\n');
            public void WriteMessage(string message, Assembly assembly) => WriteLine(message, assembly);
            public void WriteException(Exception exception, Assembly assembly) => WriteLine("[Exception] " + exception.ToString(), assembly);
            public void WriteUnhandledException(Exception exception, Assembly assembly) => WriteLine("[Unhandled exception] " + exception.ToString(), assembly);
            public void WriteExpectedException(Exception exception, Assembly assembly) => WriteLine("[Expected exception] " + exception.ToString(), assembly);
            public void WriteUnexpectedError(string errorMessage, bool appendStacktrace, Assembly assembly) => WriteLine("[Unexpected error] " + errorMessage + (appendStacktrace ? $"Stacktrace: \n{Environment.StackTrace}" : ""), assembly);
            public void WriteError(string errorMessage, bool appendStacktrace, Assembly assembly) => WriteLine("[Error] " + errorMessage + (appendStacktrace ? $"Stacktrace: \n{Environment.StackTrace}" : ""), assembly);
            public void Message(string[] messageLines, Assembly assembly) => Message(string.Join("\n", messageLines), assembly);

            public void Message(string message, Assembly assembly)
            {
                WriteMessage(message, assembly);
                Interop.MessageBox(message, "Korn message");
            }

            public void UnxepectedError(string[] messageLines, Assembly assembly) => UnxepectedError(string.Join(" ", messageLines), assembly);
            public void UnxepectedError(string message, Assembly assembly)
            {
                WriteUnexpectedError(message, true, assembly);
                Interop.MessageBox(message, "Korn unexpected error");
            }

            public void Error(string[] messageLines, Assembly assembly) => Error(string.Join(" ", messageLines), assembly);
            public void Error(string message, Assembly assembly)
            {
                WriteError(message, false, assembly);
                Interop.MessageBox(message, "Korn error");
            }

            public void Exception(Exception exception, Assembly assembly)
            {
                WriteException(exception, assembly);
                Interop.MessageBox(exception.ToString(), "Korn exception");
            }

            public void ExpectedException(Exception exception, Assembly assembly)
            {
                WriteExpectedException(exception, assembly);
                Interop.MessageBox(exception.ToString(), "Korn expected exception");
            }
        }
    }
}