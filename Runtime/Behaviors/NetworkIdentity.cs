using Soso.Net.Extensions;
using Soso.Net.Logging;
using UnityEditor;
using UnityEngine;
using CHANNEL = Soso.Net.Logging.NetworkLogger.CHANNEL;

namespace Soso.Net.Behaviors
{
	public sealed class NetworkIdentity : BaseNetworkInstance
	{
		[SerializeField] public bool IsServerAuthority;
		[SerializeField] public bool IsClientAuthority;
		
		public override bool IsOwner
		{
			get
			{
				if (Network == null || IsServerAuthority && Network.IsHost || (IsServerAuthority && IsClientAuthority))
				{
					return true;
				}
				return OwnerId == Network.SessionId || Network.Network.IsOffline;
			}
		}
#if UNITY_EDITOR
		public void BakeSceneId(ulong sequenceNumber)
		{
			InstanceId = new NetworkInstanceId(0, gameObject.scene.GetNetworkId(), sequenceNumber);
			EditorUtility.SetDirty(this);
		}
#endif

		private void OnDestroy()
		{
			if (IsOwner && InstanceId != 0 && gameObject.scene.isLoaded)
			{
				INetworkManager.GetSpawner()?.Despawn(this);
			}
		}
		
		public override void Despawn()
		{
			if (InstanceId == 0)
			{
				NetworkLogger.Error(CHANNEL.Default, "{name} was not initialized", gameObject.name);
				return;
			}
			INetworkManager.GetSpawner()?.Despawn(this);
		}
		
		public override void DespawnLocal()
		{
			if (InstanceId == 0)
			{
				NetworkLogger.Error(CHANNEL.Default, "{name} was not initialized", gameObject.name);
				return;
			}
			INetworkManager.GetSpawner()?.DespawnLocal(this);
		}
	}
}
