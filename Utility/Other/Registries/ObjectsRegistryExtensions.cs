using Cysharp.Threading.Tasks;
using Sapientia.Collections;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace Fusumity.Utility
{
	public static class ObjectsRegistryExtensions
	{
		public static void WhenModified<T>(this IObjectsRegistry registry, Action handler)
		{
			registry.GetCatalog<T>().Modified += handler;
		}

		public static void WhenRegistered<T>(this IObjectsRegistry registry, Action<T> handler)
		{
			registry.GetCatalog<T>().ObjectAdded += handler;
		}

		public static void WhenUnregistered<T>(this IObjectsRegistry registry, Action<T> handler)
		{
			registry.GetCatalog<T>().ObjectRemoved += handler;
		}

		public static void UnsubscribeModified<T>(this IObjectsRegistry registry, Action handler)
		{
			registry.GetCatalog<T>().Modified -= handler;
		}

		public static void UnsubscribeRegistered<T>(this IObjectsRegistry registry, Action<T> handler)
		{
			registry.GetCatalog<T>().ObjectAdded -= handler;
		}

		public static void UnsubscribeUnregistered<T>(this IObjectsRegistry registry, Action<T> handler)
		{
			registry.GetCatalog<T>().ObjectRemoved -= handler;
		}

		public static void WhenAvailable<T>(this IObjectsRegistry registry, Action<T> handler)
		{
			if (registry.TryGetObject(out T obj))
			{
				handler?.Invoke(obj);
			}
			else
			{
				Action<T> registrationDelegate = null;
				registrationDelegate = (obj) =>
				{
					registry.UnsubscribeRegistered(registrationDelegate);
					handler?.Invoke(obj);
				};

				registry.WhenRegistered(registrationDelegate);
			}
		}

		public static T GetObject<T>(this IObjectsRegistry registry)
		{
			return registry.GetCatalog<T>().Objects.FirstOrDefault();
		}

		public static bool TryGetObject<T>(this IObjectsRegistry registry, out T obj)
		{
			obj = registry.GetObject<T>();
			return obj != null;
		}

		public static bool TryGetObjects<T>(this IObjectsRegistry registry, out ReadOnlyCollection<T> objects)
		{
			objects = registry.GetObjects<T>();
			return !objects.IsNullOrEmpty();
		}

		public static bool TryGetCatalog<T>(this IObjectsRegistry registry, out ObjectsCatalog<T> catalog)
		{
			catalog = registry.GetCatalog<T>();
			return catalog != null;
		}

		public static void AddCatalog<T>(this IObjectsRegistry registry) where T : ObjectsCatalog, new()
		{
			var catalog = new T();
			registry.AddCatalog(catalog);
		}

		public static void ForEachExisting<T>(this IObjectsRegistry registry, Action<T> doAction)
		{
			var objects = registry.GetObjects<T>();
			foreach (var obj in objects)
			{
				doAction?.Invoke(obj);
			}
		}

		public static bool HasAny<T>(this IObjectsRegistry registry)
		{
			var objects = registry.GetObjects<T>();
			return !objects.IsNullOrEmpty();
		}

		public static async UniTask<T> AwaitFirstAvailable<T>(this IObjectsRegistry registry, CancellationToken token)
		{
			if (registry.TryGetObject(out T obj))
				return obj;

			T added = default;
			await Wait.Until(() => registry.TryGetObject(out added), token);
			token.ThrowIfCancellationRequested();

			return added;
		}
	}
}
