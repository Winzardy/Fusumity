using System.Collections.Generic;
using UnityEngine;

namespace Fusumity.Utility
{
	public static class TransformUtility
	{
		public static void SetParent(this IEnumerable<Transform> components, Transform parent)
		{
			foreach (var component in components)
			{
				component.SetParent(parent);
			}
		}

		public static T CreateChild<T>(this T parent, string name)
			where T : Transform
		{
			var obj = new GameObject(name, typeof(T));
			obj.TryGetComponent(out T transform);
			transform.SetParent(parent);
			transform.Reset();
			return transform;
		}

		public static void Reset<T>(this T transform)
			where T : Transform
		{
			transform.localScale = Vector3.one;
			transform.localRotation = Quaternion.identity;
			transform.localPosition = Vector3.zero;
		}


		public static void SetLayerRecursive(this Transform transform, int layer)
		{
			transform.gameObject.layer = layer;

			var childCount = transform.childCount;

			for (int i = 0; i < childCount; i++)
				SetLayerRecursive(transform.GetChild(i), layer);
		}
	}
}
