using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sapientia.Pooling;
using UnityEngine;

namespace AssetManagement
{
	using UnityObject = Object;

	public static partial class AssetReferenceEntryUtility
	{
		public static void Preload(this IEnumerable<IAssetRef> entries,
			CancellationToken cancellationToken = default)
		{
			entries.LoadAssetsAsync<UnityObject>(cancellationToken).Forget();
		}

		public static async UniTask PreloadAsync(this IEnumerable<IAssetRef> entries,
			CancellationToken cancellationToken = default)
		{
			await entries.LoadAssetsAsync<UnityObject>(cancellationToken);
		}

		public static void Release<T>(this IEnumerable<T> entries)
			where T : IAssetRef
		{
			foreach (var asset in entries)
			{
				asset.Release();
			}
		}

		public static void Preload<T>(this T entry, CancellationToken cancellationToken = default)
			where T : IAssetRef
		{
			AssetLoader.LoadAssetAsync<UnityObject>(entry, cancellationToken).Forget();
		}

		public static async UniTask PreloadAsync<T>(this T entry, CancellationToken cancellationToken = default)
			where T : IAssetRef
		{
			await AssetLoader.LoadAssetAsync<UnityObject>(entry, cancellationToken);
		}

		public static async UniTask<T> LoadAsync<T>(this IAssetRef<T> entry,
			CancellationToken cancellationToken = default)
			where T : UnityObject
		{
			return await AssetLoader.LoadAssetAsync<T>(entry, cancellationToken);
		}

		public static async UniTask<T> LoadAsync<T>(this IAssetRef entry,
			CancellationToken cancellationToken = default)
		{
			return await AssetLoader.LoadAssetAsync<T>(entry, cancellationToken);
		}

		public static async UniTask<T> LoadComponentAsync<T>(this IAssetRef entry,
			CancellationToken cancellationToken = default)
			where T : Component
		{
			return await AssetLoader.LoadComponentAsync<T>(entry, cancellationToken);
		}

		public static async UniTask<T> LoadAsync<T>(this AssetRef<T> entry,
			CancellationToken cancellationToken = default)
			where T : UnityObject
		{
			return await AssetLoader.LoadAssetAsync<T>(entry, cancellationToken);
		}

		public static async UniTask<T> LoadAsync<T>(this ComponentRef entry,
			CancellationToken cancellationToken = default)
			where T : Component
		{
			return await AssetLoader.LoadComponentAsync<T>(entry, cancellationToken);
		}

		public static async UniTask<T> LoadAsync<T>(this ComponentRef<T> entry,
			CancellationToken cancellationToken = default)
			where T : Component
		{
			return await AssetLoader.LoadComponentAsync<T>(entry, cancellationToken);
		}

		/// <param name="delayMs">Кастомная задержка перед выгрузкой: <br/>
		/// - Eсли не назначен, попытается достать из entry <br/>
		/// - Eсли назначен, возмет максимальное из entry </param>
		public static void ReleaseSafe<T>(this T asset, int? delayMs = 0) where T : IAssetRef
		{
			if (asset.IsEmptyOrInvalid())
				return;

			asset.Release(delayMs);
		}

		/// <param name="delayMs">Кастомная задержка перед выгрузкой: <br/>
		/// - Eсли не назначен, попытается достать из entry <br/>
		/// - Eсли назначен, возмет максимальное из entry </param>
		public static void Release<T>(this T asset, int? delayMs = 0)
			where T : IAssetRef
		{
			if (!AssetLoader.IsInitialized)
				return;

			AssetLoader.Release(asset, delayMs);
		}

		public static void Preload<T>(this AssetLabelReferenceEntry label)
		{
			AssetLoader.LoadAssetsAsync<T>(label).Forget();
		}

		public static void Release(this AssetLabelReferenceEntry label)
		{
			AssetLoader.ReleaseAssets(label);
		}

		public static async UniTask<IList<T>> LoadAsync<T>(this IEnumerable<IAssetRef> entries,
			CancellationToken cancellationToken = default)
			where T : UnityObject
		{
			return await LoadAssetsAsync<T>(entries, cancellationToken);
		}

		public static async UniTask<IList<T>> LoadAsync<T>(this IEnumerable<AssetRef<T>> entries,
			CancellationToken cancellationToken = default)
			where T : UnityObject
		{
			return await LoadAssetsAsync<T>(entries, cancellationToken);
		}

		public static async UniTask<IList<T>> LoadAsync<T>(this IEnumerable<ComponentRef> entries,
			CancellationToken cancellationToken = default)
			where T : Component
		{
			return await LoadComponentsAsync<T>(entries, cancellationToken);
		}

		public static async UniTask<IList<T>> LoadAsync<T>(this IEnumerable<ComponentRef<T>> entries,
			CancellationToken cancellationToken = default)
			where T : Component
		{
			return await LoadComponentsAsync<T>(entries, cancellationToken);
		}

		public static bool IsEmptyOrInvalid(this IAssetRef entry) =>
			entry == null || !(entry.AssetReference?.RuntimeKeyIsValid() ?? false);

		public static bool IsValid(this IAssetRef entry) =>
			entry is { AssetReference: not null } && entry.AssetReference.RuntimeKeyIsValid();

		private static async UniTask<IList<T>> LoadAssetsAsync<T>(this IEnumerable<IAssetRef> entries,
			CancellationToken cancellationToken = default)
		{
			using (ListPool<IAssetRef>.Get(out var loaded))
			using (ListPool<UniTask>.Get(out var tasks))
			using (ListPool<T>.Get(out var assets))
			{
				foreach (var entry in entries)
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

				async UniTask LoadAssetAsync(IAssetRef entry)
				{
					var asset = await entry.LoadAsync<T>(cancellationToken);
					assets.Add(asset);
					loaded.Add(entry);
				}
			}
		}

		private static async UniTask<IList<T>> LoadComponentsAsync<T>(this IEnumerable<ComponentRef> entries,
			CancellationToken cancellationToken = default)
			where T : Component
		{
			using (ListPool<IAssetRef>.Get(out var loaded))
			using (ListPool<UniTask>.Get(out var tasks))
			using (ListPool<T>.Get(out var components))
			{
				foreach (var entry in entries)
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

				async UniTask LoadComponentAsync(ComponentRef entry)
				{
					var component = await entry.LoadAsync<T>(cancellationToken);
					components.Add(component);
					loaded.Add(entry);
				}
			}
		}
	}
}
