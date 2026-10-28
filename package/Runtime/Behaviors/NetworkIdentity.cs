using System;
using System.Collections.Generic;
using Soso.Net.Behaviors.Rpc;
using Soso.Net.Core.Sync;
using Soso.Net.Extensions;
using Soso.Net.Logging;
using Soso.Net.Models;
using Soso.Net.Objects;
using UnityEditor;
using UnityEngine;
using CHANNEL = Soso.Net.Logging.NetworkLogger.CHANNEL;

namespace Soso.Net.Behaviors
{
	public class NetworkIdentity : BaseNetworkIdentity
	{
		[SerializeField] public bool IsServerAuthority;
		[SerializeField] public bool IsClientAuthority;
		[SerializeField] public NetworkInstanceId BakedId;

		public override ushort AuthorityId
		{
			get
			{
				if (_authorityOverride != 0)
				{
					return _authorityOverride;
				}
				return InstanceId.SessionId;
			}
		}
		
		[SerializeField] private bool _isAuthority = false;
		[SerializeField] private ushort _authorityOverrideDebug;
		[SerializeField] private SosoSync<ushort> _authorityOverride = new SosoSync<ushort>(false);

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
			_authorityOverride.OnChanged -= OnAuthorityChanged;
			_authorityOverride.OnChanged += OnAuthorityChanged;
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
			if (InstanceId != NetworkInstanceId.Invalid && IsAuthority() && gameObject.scene.isLoaded)
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

			_authorityOverride.Reset();
			_receivers.Clear();
		}

		#region Receivers
		
		public void Deregister(INetworkReceiver receiver)
		{
			if (_receivers == null || IsInitialized == false) return;
			
			RemoveRpcTarget(receiver);
			_receivers.Remove(receiver);
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
			AddRpcTarget(receiver);
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

		public override bool IsAuthority()
		{
			if (Network == null || Network.Network.IsConnected == false)
			{
				_isAuthority = true;
				return _isAuthority;
			}
				
			if (IsServerAuthority && IsClientAuthority)
			{
				_isAuthority = true;
				return _isAuthority;
			}

			ushort sessionId;
			if (IsServerAuthority && IsServer)
			{
				sessionId = SessionInfo.SERVER_SESSION_ID;
			}
			else
			{
				sessionId = INetworkManager.SessionId;
			}
			if (_authorityOverride != 0)
			{
				_isAuthority = _authorityOverride == sessionId;
				return _isAuthority;
			}
			if (AuthorityId == sessionId)
			{
				_isAuthority = true;
				return _isAuthority;
			}
			_isAuthority = false;
			return _isAuthority;
		}

		public void TakeOwnership()
		{
			if (IsAuthority())
			{
				return;
			}
			
			var sessionId = INetworkManager.SessionId;
			ushort id;
			if (InstanceId.SessionId == sessionId || (IsServer && IsServerAuthority))
			{
				id = 0;
			}
			else
			{
				id = sessionId;
			}

			NetworkLogger.Debug(CHANNEL.Default, "Attempting to take ownership of {a} to {b}. Current authority: {c}", gameObject.name, id, AuthorityId);
			_authorityOverride.Set(id);
		}

		public void GiveUpOwnership()
		{
			if (IsAuthority() == false || _authorityOverride == 0)
			{
				return;
			}
			NetworkLogger.Debug(CHANNEL.Default, "Giving {id} up ownership of {a}", AuthorityId, gameObject.name);
			_authorityOverride.Set(0);
		}

		private void OnAuthorityChanged(ushort previous, ushort next)
		{
			_authorityOverrideDebug = next;
			NetworkLogger.Debug(CHANNEL.Default, "Ownership of {a} transferred from {b} to {c}", gameObject.name, previous, next);
			foreach (var receiver in _receivers)
			{
				receiver.OnAuthorityTransferred(previous, next);
			}
		}

		#endregion
	}
}
