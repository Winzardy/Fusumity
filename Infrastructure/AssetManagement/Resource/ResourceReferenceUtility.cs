using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Object = UnityEngine.Object;

namespace AssetManagement
{
	public static class ResourceReferenceUtility
	{
		[Obsolete("Not usually used Resources (Unity), only rare cases when it is really necessary...")]
		public static void Preload<T>(this T reference, CancellationToken cancellationToken = default)
			where T : IResourceReference
		{
			AssetLoader.LoadResourceAsync<Object>(reference, cancellationToken).Forget();
		}

		[Obsolete("Not usually used Resources (Unity), only rare cases when it is really necessary...")]
		public static async UniTask PreloadAsync<T>(this T reference, CancellationToken cancellationToken = default)
			where T : IResourceReference
		{
			await AssetLoader.LoadResourceAsync<Object>(reference, cancellationToken);
		}

		[Obsolete("Not usually used Resources (Unity), only rare cases when it is really necessary...")]
		public static async UniTask<T> LoadAsync<T>(this IResourceReference reference, CancellationToken cancellationToken = default)
			where T : Object
		{
			return await AssetLoader.LoadResourceAsync<T>(reference, cancellationToken);
		}

		[Obsolete("Not usually used Resources (Unity), only rare cases when it is really necessary...")]
		public static async UniTask<T> LoadAsync<T>(this ResourceReference<T> reference, CancellationToken cancellationToken = default)
			where T : Object
		{
			return await AssetLoader.LoadResourceAsync<T>(reference, cancellationToken);
		}

		public static void Release<T>(this T entry)
			where T : IResourceReference
		{
			AssetLoader.Release(entry);
		}
	}
}
