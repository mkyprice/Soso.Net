using Soso.Net.Logging;

namespace Soso.Net;

public class SocketState
{
    public ulong Id { get; private set; }
    public int SocketId {  get; private set; }
    public CONNECTION_STATUS Status  { get; private set; }

    internal SocketState(int socketId)
    {
        SocketId = socketId;
        SetState(CONNECTION_STATUS.None);
    }

    internal void SetState(CONNECTION_STATUS status)
    {
        Status = status;
        NetworkLogger.Debug(NetworkLogger.CHANNEL.Default, "Socket({SocketId})-Id({Id}) - Set status: {Status}", SocketId, Id, Status);
    }

    internal void SetId(ulong id)
    {
        Id = id;
        NetworkLogger.Debug(NetworkLogger.CHANNEL.Default, "Socket({SocketId})- Set Id: {Id}", SocketId, Id);
    }
}