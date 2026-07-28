using System;
using Soso.Net.Transports;

namespace Soso.Net;

public abstract class SosoConnectionManager : IConnectionManager
{
    public readonly SosoConnection Connection;

    public SosoConnectionManager(ulong id, ISocketConnection connection)
    {
        Connection = new SosoConnection(id, connection, this);
    }

    public void Send(Span<byte> data, int channel)
    {
        Connection.Send(data, channel);
    }

    public void Shutdown()
    {
        Connection.Shutdown();
    }
    
    public abstract void OnStateChanged(CONNECTION_STATUS status);

    public abstract void OnMessage(ReadOnlySpan<byte> data, int channel, long timestamp, long messageNumber);
}