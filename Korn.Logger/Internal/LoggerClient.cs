using Korn.Logger.Core;
using Korn.Logger.Core.Client;
using Korn.Service;
using System;

class LoggerClient
{
    public LoggerClient() => Client = new Client(LoggerServerConfiguration.Instance);

    internal Client Client;

    public LoggerHandle GetLoggerHandle(string path)
    {
        var handle = LoggerHandle.Invalid;
        Client.SendAndWait(new CreateLoggerPacket(path), callback => handle = callback.LoggerHandle, TimeSpan.FromTicks(int.MaxValue));

        return handle;
    }

    public void Write(LoggerHandle handle, string message) => Client.Send(new WriteMessagePacket(handle, message));
    public void Clear(LoggerHandle handle) => Client.Send(new ClearLoggerPacket(handle));
}