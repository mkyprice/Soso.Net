using Soso.Net.Behaviors;

namespace Soso.Net.Objects
{
	public readonly struct NetworkInstanceData
	{
		public readonly int? Type;
		public readonly NetworkInstanceId Id;
		public readonly BaseNetworkInstance Identity;
		
		public NetworkInstanceData(int? type, BaseNetworkInstance identity, NetworkInstanceId id)
		{
			Type = type;
			Id = id;
			Identity = identity;
		}

		public override int GetHashCode()
		{
			return Identity.GetHashCode();
		}

		public override string ToString()
		{
			return $"[Type:{Type}, Id:{Id}, Name:{(Identity ? Identity.name : "?")}]";
		}
	}
}
