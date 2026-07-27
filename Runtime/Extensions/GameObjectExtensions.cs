using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Soso.Net.Extensions
{
	public static class GameObjectExtensions
	{
		public static string GetFullPathName(this GameObject obj, char? sepChar = '/')
		{
			StringBuilder result = new StringBuilder();
			if (sepChar.HasValue)
			{
				result.Append(sepChar.Value);
			}
			result.Append(obj.name);
			result.Append(obj.transform.GetSiblingIndex());
			while (obj.transform.parent != null)
			{
				obj = obj.transform.parent.gameObject;
				int index = 0;
				if (sepChar.HasValue && obj.transform.parent != null)
				{
					result.Insert(0, sepChar.Value);
					index++;
				}
				result.Insert(index, obj.name);
				result.Insert(index + obj.name.Length, obj.transform.GetSiblingIndex());
			}
			return result.ToString();
		}

		public static ulong GetLongHashCode(this string value)
		{
			var sha = SHA256.Create();
			var result = sha.ComputeHash(Encoding.ASCII.GetBytes(value));
			return BitConverter.ToUInt64(result);
		}

		public static bool TryGetComponentParentTree<T>(this Component go, out T component) 
			where T : Object
		{
			var next = go.transform.parent;
			while (next != null)
			{
				if (next.TryGetComponent(out component))
				{
					return true;
				}
				next = next.parent;
			}
			component = null;
			return false;
		}
	}
}
