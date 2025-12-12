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
		public static void Preload(this IEnumerable<IAssetReferenceEntry> entries,
			CancellationToken cancellationToken = default)
		{
			entries.LoadAssetsAsync<UnityObject>(cancellationToken).Forget();
		}

		public static async UniTask PreloadAsync(this IEnumerable<IAssetReferenceEntry> entries,
			CancellationToken cancellationToken = default)
		{
			await entries.LoadAssetsAsync<UnityObject>(cancellationToken);
		}

		public static void Release<T>(this IEnumerable<T> entries)
			where T : IAssetReferenceEntry
		{
			foreach (var asset in entries)
			{
				asset.Release();
			}
		}

		public static void Preload<T>(this T entry, CancellationToken cancellationToken = default)
			where T : IAssetReferenceEntry
		{
			AssetLoader.LoadAssetAsync<UnityObject>(entry, cancellationToken).Forget();
		}

		public static async UniTask PreloadAsync<T>(this T entry, CancellationToken cancellationToken = default)
			where T : IAssetReferenceEntry
		{
			await AssetLoader.LoadAssetAsync<UnityObject>(entry, cancellationToken);
		}

		public static async UniTask<T> LoadAsync<T>(this IAssetReferenceEntry<T> entry,
			CancellationToken cancellationToken = default)
			where T : UnityObject
		{
			return await AssetLoader.LoadAssetAsync<T>(entry, cancellationToken);
		}

		public static async UniTask<T> LoadAsync<T>(this IAssetReferenceEntry entry,
			CancellationToken cancellationToken = default)
		{
			return await AssetLoader.LoadAssetAsync<T>(entry, cancellationToken);
		}

		public static async UniTask<T> LoadComponentAsync<T>(this IAssetReferenceEntry entry,
			CancellationToken cancellationToken = default)
			where T : Component
		{
			return await AssetLoader.LoadComponentAsync<T>(entry, cancellationToken);
		}

		public static async UniTask<T> LoadAsync<T>(this AssetReferenceEntry<T> entry,
			CancellationToken cancellationToken = default)
			where T : UnityObject
		{
			return await AssetLoader.LoadAssetAsync<T>(entry, cancellationToken);
		}

		public static async UniTask<T> LoadAsync<T>(this ComponentReferenceEntry entry,
			CancellationToken cancellationToken = default)
			where T : Component
		{
			return await AssetLoader.LoadComponentAsync<T>(entry, cancellationToken);
		}

		public static async UniTask<T> LoadAsync<T>(this ComponentReferenceEntry<T> entry,
			CancellationToken cancellationToken = default)
			where T : Component
		{
			return await AssetLoader.LoadComponentAsync<T>(entry, cancellationToken);
		}

		/// <param name="delayMs">Кастомная задержка перед выгрузкой: <br/>
		/// - Eсли не назначен, попытается достать из entry <br/>
		/// - Eсли назначен, возмет максимальное из entry </param>
		public static void ReleaseSafe<T>(this T asset, int? delayMs = 0) where T : IAssetReferenceEntry
		{
			if (asset.IsEmptyOrInvalid())
				return;

			asset.Release(delayMs);
		}

		/// <param name="delayMs">Кастомная задержка перед выгрузкой: <br/>
		/// - Eсли не назначен, попытается достать из entry <br/>
		/// - Eсли назначен, возмет максимальное из entry </param>
		public static void Release<T>(this T asset, int? delayMs = 0)
			where T : IAssetReferenceEntry
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

		public static async UniTask<IList<T>> LoadAsync<T>(this IEnumerable<IAssetReferenceEntry> entries,
			CancellationToken cancellationToken = default)
			where T : UnityObject
		{
			return await LoadAssetsAsync<T>(entries, cancellationToken);
		}

		public static async UniTask<IList<T>> LoadAsync<T>(this IEnumerable<AssetReferenceEntry<T>> entries,
			CancellationToken cancellationToken = default)
			where T : UnityObject
		{
			return await LoadAssetsAsync<T>(entries, cancellationToken);
		}

		public static async UniTask<IList<T>> LoadAsync<T>(this IEnumerable<ComponentReferenceEntry> entries,
			CancellationToken cancellationToken = default)
			where T : Component
		{
			return await LoadComponentsAsync<T>(entries, cancellationToken);
		}

		public static async UniTask<IList<T>> LoadAsync<T>(this IEnumerable<ComponentReferenceEntry<T>> entries,
			CancellationToken cancellationToken = default)
			where T : Component
		{
			return await LoadComponentsAsync<T>(entries, cancellationToken);
		}

		public static bool IsEmptyOrInvalid(this IAssetReferenceEntry entry) =>
			entry == null || !(entry.AssetReference?.RuntimeKeyIsValid() ?? false);

		private static async UniTask<IList<T>> LoadAssetsAsync<T>(this IEnumerable<IAssetReferenceEntry> entries,
			CancellationToken cancellationToken = default)
		{
			using (ListPool<IAssetReferenceEntry>.Get(out var loaded))
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

				async UniTask LoadAssetAsync(IAssetReferenceEntry entry)
				{
					var asset = await entry.LoadAsync<T>(cancellationToken);
					assets.Add(asset);
					loaded.Add(entry);
				}
			}
		}

		private static async UniTask<IList<T>> LoadComponentsAsync<T>(this IEnumerable<ComponentReferenceEntry> entries,
			CancellationToken cancellationToken = default)
			where T : Component
		{
			using (ListPool<IAssetReferenceEntry>.Get(out var loaded))
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

				async UniTask LoadComponentAsync(ComponentReferenceEntry entry)
				{
					var component = await entry.LoadAsync<T>(cancellationToken);
					components.Add(component);
					loaded.Add(entry);
				}
			}
		}
	}
}
