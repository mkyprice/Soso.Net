using System;
using Soso.Net.Behaviors;
using Soso.Net.Extensions;

namespace Soso.Net.Objects
{
	public readonly struct NetworkInstanceData
	{
		public readonly ulong? Type;
		public readonly NetworkInstanceId Id;
		public readonly BaseNetworkInstance Identity;
		
		public NetworkInstanceData(ulong? type, BaseNetworkInstance identity, NetworkInstanceId id)
		{
			Type = type;
			Id = id;
			Identity = identity;
		}

		public TEnum? GetType<TEnum>()
			where TEnum : unmanaged, Enum
		{
			if (TryGetType<TEnum>(out var type))
			{
				return type;
			}
			return null;
		}

		public bool TryGetType<TEnum>(out TEnum type)
			where TEnum : unmanaged, Enum
		{
			if (Type == null)
			{
				type = default(TEnum);
				return false;
			}
			type = Type.Value.ToEnum<TEnum>();
			return true;
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
