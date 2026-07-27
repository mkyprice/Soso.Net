using System;
using System.Collections.Concurrent;
using System.Threading;
using Soso.Net.Models;
using Soso.Net.Models.Packets;
using UnityEngine;

namespace Soso.Net.Behaviors.Rpc
{
    public class RequestHandler
    {
        private readonly ConcurrentDictionary<int, ResponsePacket> _responses = new ConcurrentDictionary<int, ResponsePacket>();
        
        private int _requestId = 0;
        private readonly INetworkManager _network;

        public RequestHandler(INetworkManager network)
        {
	        _network = network;
	        network.ServerProcessor.Subscribe<RequestPacket>(0, OnRequestReceived);
	        network.ClientProcessor.Subscribe<ResponsePacket>(0, OnResponseReceived);
        }

        public async Awaitable<T> RequestAsync<T>(CancellationToken token, string path, params object[] requestArgs)
        {
	        token.ThrowIfCancellationRequested();
	        
	        RequestPacket request = new RequestPacket()
	        {
		        RequestId = Interlocked.Increment(ref _requestId),
		        Path = path,
		        Args = requestArgs
	        };

	        _network.Send(request, 0, SOSO_SEND_TYPE.Reliable);

	        ResponsePacket response;
	        while (_responses.TryRemove(request.RequestId, out response) == false)
	        {
		        await Awaitable.NextFrameAsync(token);
	        }
	        if (response.Response == null)
	        {
		        return default;
	        }
	        return (T)response.Response;
        }

        public void Clear()
        {
	        _responses.Clear();
        }

        private void OnRequestReceived(RequestPacket request, long arg2, long arg3, IUserConnection connection)
        {
	        string method = $"{request.Path}({request.Args?.Length ?? 0})";

	        object response = null;
	        if (RequestCache.TryGetMethod(method, out ResponseMethod responseMethod))
	        {
		        response = responseMethod.MethodInfo.Invoke(responseMethod.Target, request.Args);
	        }

	        ResponsePacket responsePacket = new ResponsePacket()
	        {
		        RequestId = request.RequestId,
		        Request = request.Path,
		        Response = response,
	        };
	        connection.Send(responsePacket,0, SOSO_SEND_TYPE.Reliable);
        }

        private void OnResponseReceived(ResponsePacket response, long arg2, long arg3, IUserConnection connection)
        {
	        _responses.TryAdd(response.RequestId, response);
        }
    }
}