using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Object = UnityEngine.Object;

namespace AssetManagement
{
	public static class ResourceRefUtility
	{
		[Obsolete("Not usually used Resources (Unity), only rare cases when it is really necessary...")]
		public static void Preload<T>(this T entry, CancellationToken cancellationToken = default)
			where T : IResourceRef
		{
			AssetLoader.LoadResourceAsync<Object>(entry, cancellationToken).Forget();
		}

		[Obsolete("Not usually used Resources (Unity), only rare cases when it is really necessary...")]
		public static async UniTask PreloadAsync<T>(this T entry, CancellationToken cancellationToken = default)
			where T : IResourceRef
		{
			await AssetLoader.LoadResourceAsync<Object>(entry, cancellationToken);
		}

		[Obsolete("Not usually used Resources (Unity), only rare cases when it is really necessary...")]
		public static async UniTask<T> LoadAsync<T>(this IResourceRef entry, CancellationToken cancellationToken = default)
			where T : Object
		{
			return await AssetLoader.LoadResourceAsync<T>(entry, cancellationToken);
		}

		[Obsolete("Not usually used Resources (Unity), only rare cases when it is really necessary...")]
		public static async UniTask<T> LoadAsync<T>(this ResourceRef<T> entry, CancellationToken cancellationToken = default)
			where T : Object
		{
			return await AssetLoader.LoadResourceAsync<T>(entry, cancellationToken);
		}

		public static void Release<T>(this T entry)
			where T : IResourceRef
		{
			AssetLoader.Release(entry);
		}
	}
}
