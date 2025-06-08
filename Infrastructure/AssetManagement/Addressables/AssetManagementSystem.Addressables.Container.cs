using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Fusumity.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Sapientia.Utility;

namespace AssetManagement
{
	public partial class AssetManagement
	{
		private class AssetContainer : Container
		{
			public AssetContainer(object key, AsyncOperationHandle handle, int usages = 1) : base(key, handle, usages)
			{
				_handle = handle;
			}

			public async UniTask<T> GetAssetAsync<T>(CancellationToken cancellationToken)
			{
				_usages++;

				if (AsyncUtility.AnyCancellation(cancellationToken, _cts.Token))
				{
					Release();
					cancellationToken.ThrowIfCancellationRequested();
				}

				using var linkedCts = _cts.Link(cancellationToken);
				var isCanceled = await _handle.WithCancellation(linkedCts.Token)
				   .SuppressCancellationThrow();

				if (isCanceled)
				{
					Release();
					cancellationToken.ThrowIfCancellationRequested();
				}
				return (T) _handle.Result;
			}
		}

		private class AssetsContainer : Container
		{
			public AssetsContainer(object key, AsyncOperationHandle handle, int usages = 1)
				: base(key, handle, usages)
			{
			}

			public async UniTask<IList<T>> GetAssetsAsync<T>(CancellationToken cancellationToken)
			{
				_usages++;

				using var linkedCts = _cts.Link(cancellationToken);
				var isCanceled = await _handle.WithCancellation(linkedCts.Token)
				   .SuppressCancellationThrow();

				if (isCanceled)
				{
					Release();
					cancellationToken.ThrowIfCancellationRequested();
				}

				return (IList<T>) _handle.Result;
			}
		}

		private abstract class Container : IDisposable
		{
			private object _key;

			protected int _usages;
			protected AsyncOperationHandle _handle;

			protected CancellationTokenSource _cts;

			protected Container(object key, AsyncOperationHandle handle, int usages = 1)
			{
				_key = key;
				_handle = handle;
				_usages = usages;

				_cts = new();
			}

			public void Dispose()
			{
				_cts?.Trigger();
				_cts = null;

				_handle.ReleaseSafe();
				_handle = default;

				_key = null;
			}

			public bool Release()
			{
				_usages--;

				if (_usages > 0)
					return false;

				Dispose();
				return true;
			}
		}
	}
}
