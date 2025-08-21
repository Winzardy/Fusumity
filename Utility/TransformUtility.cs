using System.Collections.Generic;
using UnityEngine;

namespace Fusumity.Utility
{
	public static partial class TransformUtility
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
			transform.ResetTransform();
			return transform;
		}

		public static void ResetTransformSafe<T>(this T component)
			where T : Component
		{
			if (!component)
				return;

			Reset(component.transform);
		}

		public static void ResetTransform<T>(this T component) where T : Component
			=> Reset(component.transform);

		public static void ResetSafe(this Transform transform)
		{
			if (!transform)
				return;

			Reset(transform);
		}

		public static void Reset(this Transform transform)
			=> TransformEntry.identity.ApplyTo(transform, TransformSpace.Local);

		public static void SetLayerRecursive(this Transform transform, int layer)
		{
			transform.gameObject.layer = layer;

			var childCount = transform.childCount;

			for (int i = 0; i < childCount; i++)
				SetLayerRecursive(transform.GetChild(i), layer);
		}
	}
}
