using Cysharp.Threading.Tasks;
using Sapientia.Extensions;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace Fusumity.Utility
{
	public static class UnityObjectsFactory
	{
		public static async UniTask<T> CreateAsync<T>(T prefab, Vector3 pos, Quaternion rot, CancellationToken ct, Transform parent = null, string prefix = null, string name = null) where T : Object
		{
			var instance = await InstantiateAsync(prefab, pos, rot, parent, ct);
			UpdateName(prefab, instance, prefix, name);

			return instance;
		}

		public static async UniTask<T> CreateAsync<T>(T prefab, CancellationToken ct, Transform parent = null, string prefix = null, string name = null) where T : Object
		{
			var instance = await InstantiateAsync(prefab, parent, ct);
			UpdateName(prefab, instance, prefix, name);

			return instance;
		}

		public static T Create<T>(T prefab, Transform parent = null, string prefix = null, string name = null, bool worldPositionStays = false) where T : Object
		{
			var instance = Instantiate(prefab, parent, worldPositionStays);
			UpdateName(prefab, instance, prefix, name);

			return instance;
		}

		public static T Create<T>(T prefab, Vector3 pos, Quaternion rot, Transform parent = null, string prefix = null, string name = null) where T : Object
		{
			var instance = Instantiate(prefab, pos, rot, parent);
			UpdateName(prefab, instance, prefix, name);

			return instance;
		}

		public static T Create<T>(string name, Transform parent, bool worldPositionStays = true)
		{
			return Create<T>(name, parent, worldPositionStays, typeof(T));
		}

		public static T Create<T>(string name, Transform parent, params System.Type[] components)
		{
			return Create<T>(name, parent, true, components);
		}

		public static T Create<T>(string name, Transform parent, bool worldPositionStays, params System.Type[] components)
		{
			var instance = new GameObject(name, components);
			instance.transform.SetParent(parent, worldPositionStays);

			var component = instance.GetComponent<T>();

			if (component == null)
			{
				Debug.LogError($"Did not add component of type [ {typeof(T).Name} ]");
			}

			return component;
		}

		private static void UpdateName<T>(T prefab, T instance, string prefix, string name) where T : Object
		{
			if (TryGetObjectName(prefab, prefix, name, out var objectName))
			{
				if (instance is Component component)
				{
					component.gameObject.name = objectName;
				}
				else
				{
					instance.name = objectName;
				}
			}
		}

		private static bool TryGetObjectName<T>(T prefab, string prefix, string name, out string objectName) where T : Object
		{
			if (!prefix.IsNullOrEmpty() || !name.IsNullOrEmpty())
			{
				var endName = name.IsNullOrEmpty() ? prefab.name : name;
				if (!prefix.IsNullOrEmpty())
				{
					endName = $"{prefix} {endName}";
				}

				objectName = endName;
				return true;
			}

			objectName = null;
			return false;
		}

		private static T Instantiate<T>(T prefab, Transform parent, bool worldPositionStays = false) where T : Object
		{
			return parent != null ?
					Object.Instantiate(prefab, parent, worldPositionStays) :
					Object.Instantiate(prefab);
		}

		private static T Instantiate<T>(T prefab, Vector3 position, Quaternion rotation, Transform parent) where T : Object
		{
			return parent != null ?
					Object.Instantiate(prefab, position, rotation, parent) :
					Object.Instantiate(prefab, position, rotation);
		}

		private static async UniTask<T> InstantiateAsync<T>(T prefab, Transform parent, CancellationToken ct) where T : Object
		{
			var op = parent != null ?
					Object.InstantiateAsync(prefab, parent) :
					Object.InstantiateAsync(prefab);

			await op.WithCancellation(ct);
			ct.ThrowIfCancellationRequested();

			//NOTE: do not use LINQ with InstantiateAsync
			//https://discussions.unity.com/t/regarding-instantiateasync-unsafeutility-as-was-used-to-forcibly-convert-the-object-causing-il2cpp-runtime-errors/1535375
			return
				op.Result?.Length > 0 ?
				op.Result[0] :
				null;
		}

		private static async UniTask<T> InstantiateAsync<T>(T prefab, Vector3 position, Quaternion rotation, Transform parent, CancellationToken ct) where T : Object
		{
			var op = parent != null ?
					Object.InstantiateAsync(prefab, parent, position, rotation) :
					Object.InstantiateAsync(prefab, position, rotation);

			await op.WithCancellation(ct);
			ct.ThrowIfCancellationRequested();

			return
				op.Result?.Length > 0 ?
				op.Result[0] :
				null;
		}
	}
}
