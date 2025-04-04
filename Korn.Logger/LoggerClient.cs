using Korn.Logger.Core;
using Korn.Logger.Core.Client;
using Korn.Service;
using System;

class LoggerClient
{
    public LoggerClient()
    {
        var serverConfiguration = new LoggerServerConfiguration();
        client = new Client(serverConfiguration);        
    }

    Client client;

    public LoggerHandle GetLoggerHandle(string path)
    {
        var handle = LoggerHandle.Invalid;
        client.SendAndWait(new CreateLoggerPacket(path), callback => handle = callback.LoggerHandle);

        if (handle.Handle == LoggerHandle.Invalid.Handle)
            throw new Exception($"Korn.Logger.LoggerClient: the server didn't return a handle for the logger");

        return handle;
    }

    public void Write(LoggerHandle handle, string message) => client.Send(new WriteMessagePacket(handle, message));
    public void Clear(LoggerHandle handle) => client.Send(new ClearLoggerPacket(handle));
}