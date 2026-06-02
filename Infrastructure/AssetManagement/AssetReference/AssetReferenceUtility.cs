using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sapientia.Pooling;
using UnityEngine;

namespace AssetManagement
{
	using UnityObject = Object;

	public static partial class AssetReferenceUtility
	{
		public static void Preload(this IEnumerable<IAssetReference> references,
			CancellationToken cancellationToken = default)
		{
			references.LoadAssetsAsync<UnityObject>(cancellationToken).Forget();
		}

		public static async UniTask PreloadAsync(this IEnumerable<IAssetReference> references,
			CancellationToken cancellationToken = default)
		{
			await references.LoadAssetsAsync<UnityObject>(cancellationToken);
		}

		public static void Release<T>(this IEnumerable<T> references)
			where T : IAssetReference
		{
			foreach (var asset in references)
			{
				asset.Release();
			}
		}

		public static void Preload<T>(this T reference, CancellationToken cancellationToken = default)
			where T : IAssetReference
		{
			AssetLoader.LoadAssetAsync<UnityObject>(reference, cancellationToken).Forget();
		}

		public static async UniTask PreloadAsync<T>(this T reference, CancellationToken cancellationToken = default)
			where T : IAssetReference
		{
			await AssetLoader.LoadAssetAsync<UnityObject>(reference, cancellationToken);
		}

		public static async UniTask<T> LoadAsync<T>(this IAssetReference<T> reference,
			CancellationToken cancellationToken = default)
			where T : UnityObject
		{
			return await AssetLoader.LoadAssetAsync<T>(reference, cancellationToken);
		}

		public static async UniTask<T> LoadAsync<T>(this IAssetReference reference,
			CancellationToken cancellationToken = default)
		{
			return await AssetLoader.LoadAssetAsync<T>(reference, cancellationToken);
		}

		public static async UniTask<T> LoadComponentAsync<T>(this IAssetReference reference,
			CancellationToken cancellationToken = default)
			where T : Component
		{
			return await AssetLoader.LoadComponentAsync<T>(reference, cancellationToken);
		}

		public static async UniTask<T> LoadAsync<T>(this AssetReference<T> reference,
			CancellationToken cancellationToken = default)
			where T : UnityObject
		{
			return await AssetLoader.LoadAssetAsync<T>(reference, cancellationToken);
		}

		public static async UniTask<T> LoadAsync<T>(this ComponentReference reference,
			CancellationToken cancellationToken = default)
			where T : Component
		{
			return await AssetLoader.LoadComponentAsync<T>(reference, cancellationToken);
		}

		public static async UniTask<T> LoadAsync<T>(this ComponentReference<T> reference,
			CancellationToken cancellationToken = default)
			where T : Component
		{
			return await AssetLoader.LoadComponentAsync<T>(reference, cancellationToken);
		}

		/// <param name="delayMs">Кастомная задержка перед выгрузкой: <br/>
		/// - Eсли не назначен, попытается достать из entry <br/>
		/// - Eсли назначен, возмет максимальное из entry </param>
		public static void ReleaseSafe<T>(this T reference, int? delayMs = 0) where T : IAssetReference
		{
			if (reference.IsEmptyOrInvalid())
				return;

			reference.Release(delayMs);
		}

		/// <param name="delayMs">Кастомная задержка перед выгрузкой: <br/>
		/// - Eсли не назначен, попытается достать из entry <br/>
		/// - Eсли назначен, возмет максимальное из entry </param>
		public static void Release<T>(this T reference, int? delayMs = 0)
			where T : IAssetReference
		{
			if (!AssetLoader.IsInitialized)
				return;

			AssetLoader.Release(reference, delayMs);
		}

		public static void Preload<T>(this AssetLabelReference label)
		{
			AssetLoader.LoadAssetsAsync<T>(label).Forget();
		}

		public static void Release(this AssetLabelReference label)
		{
			AssetLoader.ReleaseAssets(label);
		}

		public static async UniTask<IList<T>> LoadAsync<T>(this IEnumerable<IAssetReference> references,
			CancellationToken cancellationToken = default)
			where T : UnityObject
		{
			return await LoadAssetsAsync<T>(references, cancellationToken);
		}

		public static async UniTask<IList<T>> LoadAsync<T>(this IEnumerable<AssetReference<T>> references,
			CancellationToken cancellationToken = default)
			where T : UnityObject
		{
			return await LoadAssetsAsync<T>(references, cancellationToken);
		}

		public static async UniTask<IList<T>> LoadAsync<T>(this IEnumerable<ComponentReference> references,
			CancellationToken cancellationToken = default)
			where T : Component
		{
			return await LoadComponentsAsync<T>(references, cancellationToken);
		}

		public static async UniTask<IList<T>> LoadAsync<T>(this IEnumerable<ComponentReference<T>> references,
			CancellationToken cancellationToken = default)
			where T : Component
		{
			return await LoadComponentsAsync<T>(references, cancellationToken);
		}

		public static bool IsEmptyOrInvalid(this IAssetReference reference) =>
			reference == null || !(reference.AssetReference?.RuntimeKeyIsValid() ?? false);

		public static bool IsValid(this IAssetReference reference) =>
			reference is {AssetReference: not null} && reference.AssetReference.RuntimeKeyIsValid();

		private static async UniTask<IList<T>> LoadAssetsAsync<T>(this IEnumerable<IAssetReference> references,
			CancellationToken cancellationToken = default)
		{
			using (ListPool<IAssetReference>.Get(out var loaded))
			using (ListPool<UniTask>.Get(out var tasks))
			using (ListPool<T>.Get(out var assets))
			{
				foreach (var entry in references)
					tasks.Add(LoadAssetAsync(entry));

				var isCanceled = await UniTask.WhenAll(tasks)
					.SuppressCancellationThrow();

				if (isCanceled)
				{
					foreach (var entry in loaded)
						entry.Release();

					cancellationToken.ThrowIfCancellationRequested();
				}

				return assets.ToArray();

				async UniTask LoadAssetAsync(IAssetReference reference)
				{
					var asset = await reference.LoadAsync<T>(cancellationToken);
					assets.Add(asset);
					loaded.Add(reference);
				}
			}
		}

		private static async UniTask<IList<T>> LoadComponentsAsync<T>(this IEnumerable<ComponentReference> references,
			CancellationToken cancellationToken = default)
			where T : Component
		{
			using (ListPool<IAssetReference>.Get(out var loaded))
			using (ListPool<UniTask>.Get(out var tasks))
			using (ListPool<T>.Get(out var components))
			{
				foreach (var entry in references)
				{
					tasks.Add(LoadComponentAsync(entry));
				}

				var isCanceled = await UniTask.WhenAll(tasks)
					.SuppressCancellationThrow();

				if (isCanceled)
				{
					foreach (var entry in loaded)
					{
						entry.Release();
					}

					cancellationToken.ThrowIfCancellationRequested();
				}

				return components.ToArray();

				async UniTask LoadComponentAsync(ComponentReference reference)
				{
					var component = await reference.LoadAsync<T>(cancellationToken);
					components.Add(component);
					loaded.Add(reference);
				}
			}
		}
	}
}
