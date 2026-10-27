using System;
using System.Collections.Generic;
using Soso.Net.Behaviors.Rpc;
using Soso.Net.Extensions;
using Soso.Net.Logging;
using Soso.Net.Models;
using Soso.Net.Objects;
using UnityEditor;
using UnityEngine;
using CHANNEL = Soso.Net.Logging.NetworkLogger.CHANNEL;

namespace Soso.Net.Behaviors
{
	public class NetworkIdentity : BaseNetworkInstance
	{
		[SerializeField] public bool IsServerAuthority;
		[SerializeField] public bool IsClientAuthority;
		[SerializeField] public NetworkInstanceId BakedId;

		public override ushort AuthorityId
		{
			get
			{
				if (_isAuthorityOverridden)
				{
					return _authorityOverrideId;
				}
				return InstanceId.SessionId;
			}
		}

		public override bool IsAuthority
		{
			get
			{
				_isAuthority = DetermineAuthority();
				return _isAuthority;
			}
		}
		[SerializeField] private bool _isAuthority = false;
		[SerializeField] private bool _isAuthorityOverridden = false;
		[SerializeField] private ushort _authorityOverrideId = 0;
		private List<INetworkReceiver> _receivers;
		
#if UNITY_EDITOR
		public void BakeSceneId(ulong sequenceNumber)
		{
			if (IsServerAuthority == false)
			{
				NetworkLogger.Error(NetworkLogger.CHANNEL.Default, "{netId} - {name} is not set to server authority. Baking failed", nameof(NetworkIdentity), gameObject.name);
				return;
			}
			BakedId = new NetworkInstanceId(SessionInfo.SERVER_SESSION_ID, gameObject.scene.GetNetworkId(), sequenceNumber);
			EditorUtility.SetDirty(this);
		}
#endif

		public override void Initialize(NetworkInstance instance)
		{
			base.Initialize(instance);

			_receivers = new List<INetworkReceiver>();
			var receivers = GetComponentsInChildren<INetworkReceiver>(true);
			for (ushort i = 0; i < receivers.Length; i++)
			{
				AddReceiver(receivers[i]);
			}
		}

		private void Start()
		{
			// Starting but have not been initialized
			if (IsInitialized == false)
			{
				NetworkLogger.Error(NetworkLogger.CHANNEL.Default, "{netId} - {name} has not been initialized", nameof(NetworkIdentity), gameObject.name);
			}
		}

		private void OnDestroy()
		{
			if (InstanceId != NetworkInstanceId.Invalid && IsAuthority && gameObject.scene.isLoaded)
			{
				INetworkManager.GetSpawner()?.Despawn(this);
			}
		}
		
		public override void Despawn()
		{
			if (InstanceId == NetworkInstanceId.Invalid)
			{
				NetworkLogger.Error(CHANNEL.Default, "{name} was not initialized", gameObject.name);
				return;
			}
			INetworkManager.GetSpawner()?.Despawn(this);
		}
		
		public override void DespawnLocal()
		{
			if (InstanceId == NetworkInstanceId.Invalid)
			{
				NetworkLogger.Error(CHANNEL.Default, "{name} was not initialized", gameObject.name);
				return;
			}
			INetworkManager.GetSpawner()?.DespawnLocal(this);
		}

		protected override void ResetInstance()
		{
			base.ResetInstance();
			
			_receivers.Clear();
		}

		#region Receivers
		
		public void Deregister(INetworkReceiver receiver)
		{
			if (_receivers == null || IsInitialized == false) return;
			
			ushort index = (ushort)(_receivers.IndexOf(receiver));
			if (index > 0)
			{
				RemoveRpcTarget(index);
				_receivers.Remove(receiver);
			}
		}

		private bool AddReceiver(INetworkReceiver receiver)
		{
			if (_receivers.Contains(receiver))
			{
				return false;
			}
			ushort id = (ushort)(_receivers.Count);
			receiver.Initialize(id, this);
			_receivers.Add(receiver);
			AddRpcTarget(receiver, id);
			return true;
		}

		private void AddReceiverTree(GameObject root)
		{
			var receivers = root.GetComponentsInChildren<INetworkReceiver>(true);
			foreach (var receiver in receivers)
			{
				AddReceiver(receiver);
			}
		}

		public T AddComponent<T>() 
			where T : INetworkReceiver
		{
			Rpc(RpcAddComponent, typeof(T).AssemblyQualifiedName);
			return GetComponent<T>();
		}
		
		[SosoRpc(RPC_CALL_TYPE.Client, true, true)]
		private void RpcAddComponent(string typeName)
		{
			Type type = Type.GetType(typeName);
			if (type == null)
			{
				NetworkLogger.Error(NetworkLogger.CHANNEL.Default, "Cannot find type {typeName}", typeName);
				return;
			}
			var receiver = gameObject.AddComponent(type) as INetworkReceiver;
			AddReceiver(receiver);
		}
		
		#endregion

		#region Ownership

		private bool DetermineAuthority()
		{
			if (Network == null) return true;
			if (_isAuthorityOverridden)
			{
				return _authorityOverrideId == Network.SessionId;
			}
			if (AuthorityId == Network.SessionId) return true;
				
			if (IsServerAuthority && Network.IsHost || (IsServerAuthority && IsClientAuthority))
			{
				return true;
			}
			return false;
		}

		public void RequestOwnership()
		{
			if (IsAuthority) return;

			NetworkLogger.Info(CHANNEL.Default, "Requesting ownership of {a}", InstanceId);

			if (IsServer)
			{
				Rpc(RpcSetOwner, SessionInfo.SERVER_SESSION_ID);
			}
			else
			{
				Rpc(RpcRequestOwnership, INetworkManager.SessionId);
			}
		}

		public void RequestTransferOwnership(ushort id)
		{
			NetworkLogger.Info(CHANNEL.Default, "Requesting ownership of {a} to {b}", InstanceId, id);
			Rpc(RpcRequestOwnership, id);
		}

		public void GiveUpOwnership()
		{
			if (IsAuthority == false) return;

			NetworkLogger.Info(CHANNEL.Default, "Giving up ownership of {a}", InstanceId);
			Rpc(RpcRequestOwnership, InstanceId.SessionId);
		}

		[SosoRpc(RPC_CALL_TYPE.Server, false, false)]
		private void RpcRequestOwnership(ushort sessionId)
		{
			if (AuthorityId == sessionId)
			{
				return;
			}
			NetworkLogger.Info(CHANNEL.Default, "Request for ownership of {a} from {b}", InstanceId, sessionId);
			if (IsAuthority || IsServer || InstanceId.SessionId == sessionId)
			{
				Rpc(RpcSetOwner, sessionId);
			}
		}

		[SosoRpc(RPC_CALL_TYPE.Client, false, true)]
		private void RpcSetOwner(ushort id)
		{
			var previous = AuthorityId;
			var next = id;
			_isAuthorityOverridden = next != InstanceId.SessionId;
			_authorityOverrideId = next;
			foreach (var receiver in _receivers)
			{
				receiver.OnAuthorityTransferred(previous, next);
			}
			NetworkLogger.Info(CHANNEL.Default, "Transferred ownership from {a} to {b}", InstanceId.SessionId, id);
		}

		#endregion
	}
}
