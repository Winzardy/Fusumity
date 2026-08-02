using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sapientia.Pooling;
using UnityEngine;

namespace AssetManagement
{
	using UnityObject = UnityEngine.Object;

	public static class AssetReferenceRuntimeExtensions
	{
		public static bool SameAsset(this IAssetReference a, IAssetReference b)
		{
			if (ReferenceEquals(a, b))
				return true;

			if (a is null || b is null)
				return false;

			var aKey = a.RuntimeKey;
			var bKey = b.RuntimeKey;

			if (aKey == null || bKey == null)
				return false;

			if (aKey is string aString && bKey is string bString)
				return string.Equals(aString, bString, StringComparison.OrdinalIgnoreCase);

			return Equals(aKey, bKey);
		}

		public static void Preload(this IEnumerable<IAssetReference> references,
			CancellationToken cancellationToken = default, IProgress<float> progress = null)
		{
			references.LoadAssetsAsync<UnityObject>(cancellationToken, progress).Forget();
		}

		public static async UniTask PreloadAsync(this IEnumerable<IAssetReference> references,
			CancellationToken cancellationToken = default, IProgress<float> progress = null)
		{
			await references.LoadAssetsAsync<UnityObject>(cancellationToken, progress);
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

		public static void Preload<T>(this T reference, CancellationToken cancellationToken,
			IProgress<float> progress)
			where T : IAssetReference
		{
			AssetLoader.LoadAssetAsync<UnityObject>(reference, cancellationToken, progress).Forget();
		}

		public static async UniTask PreloadAsync<T>(this T reference, CancellationToken cancellationToken = default)
			where T : IAssetReference
		{
			await AssetLoader.LoadAssetAsync<UnityObject>(reference, cancellationToken);
		}

		public static async UniTask PreloadAsync<T>(this T reference, CancellationToken cancellationToken,
			IProgress<float> progress)
			where T : IAssetReference
		{
			await AssetLoader.LoadAssetAsync<UnityObject>(reference, cancellationToken, progress);
		}

		/// <inheritdoc cref="AssetLoader.LoadAssetAsync{T}(IAssetReference,System.Threading.CancellationToken,System.IProgress{float})"/>
		public static async UniTask<T> LoadAsync<T>(this IAssetReference<T> reference,
			CancellationToken cancellationToken = default, IProgress<float> progress = null)
			where T : UnityObject
		{
			return await AssetLoader.LoadAssetAsync<T>(reference, cancellationToken, progress);
		}

		/// <inheritdoc cref="AssetLoader.LoadAssetAsync{T}(IAssetReference,System.Threading.CancellationToken,System.IProgress{float})"/>
		public static async UniTask<T> LoadAsync<T>(this IAssetReference reference,
			CancellationToken cancellationToken = default, IProgress<float> progress = null)
		{
			return await AssetLoader.LoadAssetAsync<T>(reference, cancellationToken, progress);
		}

		/// <inheritdoc cref="AssetLoader.LoadComponentAsync{T}(IAssetReference,System.Threading.CancellationToken,System.IProgress{float})"/>
		public static async UniTask<T> LoadComponentAsync<T>(this IAssetReference reference,
			CancellationToken cancellationToken = default, IProgress<float> progress = null)
			where T : Component
		{
			return await AssetLoader.LoadComponentAsync<T>(reference, cancellationToken, progress);
		}

		/// <inheritdoc cref="AssetLoader.LoadAsset{T}(IAssetReference)"/>
		public static T Load<T>(this IAssetReference<T> reference)
			where T : UnityObject
		{
			return AssetLoader.LoadAsset<T>(reference);
		}

		/// <inheritdoc cref="AssetLoader.LoadAsset{T}(IAssetReference)"/>
		public static T Load<T>(this IAssetReference reference)
		{
			return AssetLoader.LoadAsset<T>(reference);
		}

		/// <inheritdoc cref="AssetLoader.LoadComponent{T}(IAssetReference)"/>
		public static T LoadComponent<T>(this IAssetReference reference)
			where T : Component
		{
			return AssetLoader.LoadComponent<T>(reference);
		}

		/// <inheritdoc cref="AssetLoader.LoadAssetAsync{T}(IAssetReference,System.Threading.CancellationToken,System.IProgress{float})"/>
		public static async UniTask<T> LoadAsync<T>(this AssetReference<T> reference,
			CancellationToken cancellationToken = default, IProgress<float> progress = null)
			where T : UnityObject
		{
			return await AssetLoader.LoadAssetAsync<T>(reference, cancellationToken, progress);
		}

		/// <inheritdoc cref="AssetLoader.LoadAsset{T}(IAssetReference)"/>
		public static T LoadOrNull<T>(this AssetReference<T> reference)
			where T : UnityObject
		{
			if (reference.IsEmptyOrInvalid())
				return null;

			return AssetLoader.LoadAsset<T>(reference);
		}

		/// <inheritdoc cref="AssetLoader.LoadAsset{T}(IAssetReference)"/>
		public static T Load<T>(this AssetReference<T> reference)
			where T : UnityObject
		{
			return AssetLoader.LoadAsset<T>(reference);
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

		public static void Preload<T>(this AssetLabelReference label,
			CancellationToken cancellationToken = default, IProgress<float> progress = null)
		{
			AssetLoader.LoadAssetsAsync<T>(label, cancellationToken, progress).Forget();
		}

		public static void Release(this AssetLabelReference label)
		{
			AssetLoader.ReleaseAssets(label);
		}

		public static async UniTask<IList<T>> LoadAsync<T>(this IEnumerable<IAssetReference> references,
			CancellationToken cancellationToken = default, IProgress<float> progress = null)
			where T : UnityObject
		{
			return await LoadAssetsAsync<T>(references, cancellationToken, progress);
		}

		public static async UniTask<IList<T>> LoadAsync<T>(this IEnumerable<AssetReference<T>> references,
			CancellationToken cancellationToken = default, IProgress<float> progress = null)
			where T : UnityObject
		{
			return await LoadAssetsAsync<T>(references, cancellationToken, progress);
		}

		public static bool IsEmptyOrInvalid(this IAssetReference reference) =>
			reference == null || !reference.IsRuntimeKeyValid;

		public static bool IsValid(this IAssetReference reference) =>
			reference is {IsRuntimeKeyValid: true};

		private static async UniTask<IList<T>> LoadAssetsAsync<T>(this IEnumerable<IAssetReference> references,
			CancellationToken cancellationToken = default, IProgress<float> progress = null)
		{
			using (ListPool<IAssetReference>.Get(out var loaded))
			using (ListPool<UniTask>.Get(out var tasks))
			using (ListPool<T>.Get(out var assets))
			{
				var progressValues = progress != null ? new List<float>() : null;
				foreach (var entry in references)
				{
					var progressIndex = progressValues?.Count ?? -1;
					progressValues?.Add(0f);
					tasks.Add(LoadAssetAsync(entry, CreateCollectionProgress(progress, progressValues, progressIndex)));
				}

				var isCanceled = await UniTask.WhenAll(tasks)
					.SuppressCancellationThrow();

				if (isCanceled)
				{
					foreach (var entry in loaded)
						entry.Release();

					cancellationToken.ThrowIfCancellationRequested();
				}

				return assets.ToArray();

				progress?.Report(1f);
				return assets.ToArray();

				async UniTask LoadAssetAsync(IAssetReference reference, IProgress<float> assetProgress)
				{
					var asset = await reference.LoadAsync<T>(cancellationToken, assetProgress);
					assets.Add(asset);
					loaded.Add(reference);
				}
			}
		}

		private static IProgress<float> CreateCollectionProgress(IProgress<float> progress, IList<float> progressValues, int index)
		{
			if (progress == null)
				return null;

			return new CollectionProgress(progress, progressValues, index);
		}

		private sealed class CollectionProgress : IProgress<float>
		{
			private readonly IProgress<float> _progress;
			private readonly IList<float> _values;
			private readonly int _index;

			public CollectionProgress(IProgress<float> progress, IList<float> values, int index)
			{
				_progress = progress;
				_values = values;
				_index = index;
			}

			public void Report(float value)
			{
				_values[_index] = Mathf.Clamp01(value);

				var total = 0f;
				for (var i = 0; i < _values.Count; i++)
				{
					total += _values[i];
				}

				_progress.Report(total / _values.Count);
			}
		}
	}
}
