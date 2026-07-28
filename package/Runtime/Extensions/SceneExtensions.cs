using UnityEngine;
using UnityEngine.SceneManagement;

namespace Soso.Net.Extensions
{
	public static class SceneExtensions
	{
		public static ushort GetNetworkId(this Scene scene)
		{
			if (scene.name == "DontDestroyOnLoad")
			{
				return ushort.MaxValue;
			}
			return (ushort)scene.buildIndex;
		}
	}
}
