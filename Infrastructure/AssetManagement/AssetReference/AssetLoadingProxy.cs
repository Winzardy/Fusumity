using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AssetManagement
{
	public class GameObjectLoadingProxy : AssetLoadingProxy<IAssetReferenceEntry, GameObject>
	{
		public GameObjectLoadingProxy(IAssetReferenceEntry entry) : base(entry)
		{
		}

		public async UniTask<TComponent> LoadComponentAsync<TComponent>(CancellationToken token = default) where TComponent : Component
		{
			var go = await LoadAsync(token);
			token.ThrowIfCancellationRequested();

			if (go == null || !go.TryGetComponent(out TComponent component))
			{
				throw new Exception($"Could not load component of type [ {typeof(Component)} ]");
			}

			return component;
		}
	}

	public class AssetLoadingProxy<TAsset> : AssetLoadingProxy<IAssetReferenceEntry<TAsset>, TAsset>
		where TAsset : Object
	{
		public AssetLoadingProxy(IAssetReferenceEntry<TAsset> entry) : base(entry)
		{
		}
	}

	/// <summary>
	/// Used for consequitive loads on a single asset reference,
	/// to prevent refcount bloating.
	/// </summary>
	public class AssetLoadingProxy<TEntry, TAsset> : IDisposable
		where TEntry : IAssetReferenceEntry
		where TAsset : Object
	{
		private IAssetReferenceEntry _entry;

		private TAsset _loadedAsset;
		private UniTaskCompletionSource<TAsset> _loadingTcs;
		private CancellationTokenSource _cts;

		private bool _disposed;

		public AssetLoadingProxy(IAssetReferenceEntry entry)
		{
			_entry = entry;
		}

		public async UniTask<TAsset> LoadAsync(CancellationToken token = default)
		{
			if (_disposed)
				throw new ObjectDisposedException(nameof(AssetLoadingProxy<TEntry, TAsset>));

			if (_loadedAsset != null)
			{
				return _loadedAsset;
			}

			if (_loadingTcs != null)
			{
				using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, _cts.Token))
				{
					return await _loadingTcs.Task.AttachExternalCancellation(linkedCts.Token);
				}
			}

			_loadingTcs = new UniTaskCompletionSource<TAsset>();
			_cts = new CancellationTokenSource();

			try
			{
				// do not interrupt initial asset load with external token
				var result = await _entry.LoadAsync<TAsset>(_cts.Token);

				if (result == null)
				{
					throw new Exception(
						$"Could not load asset of type [ {typeof(TAsset).Name} ] " +
						$"from  [ {_entry.AssetReference.AssetGUID} ]");
				}

				_loadedAsset = result;
				_loadingTcs.TrySetResult(_loadedAsset);

				// throw if external token was cancelled
				token.ThrowIfCancellationRequested();
			}
			catch (OperationCanceledException)
			{
				_loadingTcs.TrySetCanceled();
				throw;
			}
			catch (Exception ex)
			{
				_loadingTcs.TrySetException(ex);
				throw;
			}
			finally
			{
				_cts?.Dispose();
				_cts = null;
				_loadingTcs = null;
			}

			return _loadedAsset;
		}

		public void Dispose()
		{
			if (_disposed)
				return;

			_disposed = true;

			_cts?.Cancel();
			_loadingTcs?.TrySetCanceled();

			_cts?.Dispose();
			_cts = null;
			_loadingTcs = null;

			if (_loadedAsset != null)
			{
				_entry.ReleaseSafe();
			}
		}
	}

	public class GameObjectLoadingMediator : AssetLoadingProxiesMediator<GameObject>
	{
		public async UniTask<T> LoadComponentAsync<T>(IAssetReferenceEntry<GameObject> entry, CancellationToken token) where T : Component
		{
			var go = await LoadAsync(entry, token);
			token.ThrowIfCancellationRequested();

			return go.GetComponent<T>();
		}
	}

	public class AssetLoadingProxiesMediator<T> : IDisposable where T : Object
	{
		private Dictionary<IAssetReferenceEntry<T>, AssetLoadingProxy<T>> _loadingProxies = new Dictionary<IAssetReferenceEntry<T>, AssetLoadingProxy<T>>();

		//TODO: add simultaneous assets limit (FIFO clearing after N)

		public void Dispose()
		{
			Clear();
		}

		public UniTask<T> LoadAsync(IAssetReferenceEntry<T> entry, CancellationToken token)
		{
			if (!_loadingProxies.TryGetValue(entry, out var proxy))
			{
				proxy = new AssetLoadingProxy<T>(entry);
				_loadingProxies.Add(entry, proxy);
			}

			return proxy.LoadAsync(token);
		}

		public void Clear(IAssetReferenceEntry<T> entry)
		{
			if (_loadingProxies.TryGetValue(entry, out var proxy))
			{
				_loadingProxies.Remove(entry);
				proxy.Dispose();
			}
		}

		public void Clear()
		{
			foreach (var proxy in _loadingProxies.Values)
			{
				proxy.Dispose();
			}

			_loadingProxies.Clear();
		}
	}
}
