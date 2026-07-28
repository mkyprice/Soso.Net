using System;
using System.Collections.Generic;
using Soso.Net.Logging;
using Soso.Net.Packets;
using Soso.Net.Transports;

namespace Soso.Net;

public class SosoListener : ISocketProcessor
{
    private readonly List<SosoSocket> _sockets = new List<SosoSocket>();
    private readonly ISocketListener _listener;
    private readonly ISocketManager _socketManager;

    internal SosoListener(ISocketListener listener, ISocketManager socketManager)
    {
        _listener = listener;
        _socketManager = socketManager;
        _listener.Connected(OnConnected);
    }
    
    public IEnumerable<SosoSocket> Sockets => _sockets;

    private void OnConnected(ISocketConnection obj)
    {
        SosoSocket socket = new SosoSocket(obj, this);
        _sockets.Add(socket);
    }

    public void Shutdown()
    {
        _listener.Shutdown();
    }

    public void OnStateChanged(SosoSocket socket, CONNECTION_STATUS status)
    {
        _socketManager.ConnectionChanged(socket, status);
        if (status == CONNECTION_STATUS.Disconnected)
        {
            _sockets.Remove(socket);
        }
    }

    public void OnMessage(SosoSocket socket, int packetType, ReadOnlySpan<byte> data, int channel, long timestamp, long messageNumber)
    {
        if (socket.State.Status == CONNECTION_STATUS.Connecting ||
            socket.State.Status == CONNECTION_STATUS.None)
        {
            if (packetType == 1)
            {
                var negotiation = Negotiation.FromBytes(data);
                socket.State.SetId(negotiation.Id);
                socket.SetState(CONNECTION_STATUS.Connecting);

                if (_socketManager.AcceptConnection(socket))
                {
                    socket.SetState(CONNECTION_STATUS.Connected);
                    socket.Send(negotiation.ToBytes(), 0, 1);
                }
                else
                {
                    negotiation = new Negotiation();
                    negotiation.Id = 0;
                    socket.Send(negotiation.ToBytes(), 0, 1);
                }
            }
            else
            {
                NetworkLogger.Warn(NetworkLogger.CHANNEL.Default, "Received packet during socket handshake");
            }
				
        }
        else
        {
            _socketManager.OnMessage(socket, data, channel, timestamp, messageNumber);
        }
    }
}

public interface ISocketManager
{
    bool AcceptConnection(SosoSocket connection);
    void ConnectionChanged(SosoSocket connection, CONNECTION_STATUS status);
    void OnMessage(SosoSocket connection, ReadOnlySpan<byte> data, int channel, long timestamp, long messageNumber);
}