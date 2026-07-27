using System;
using System.Collections.Generic;
using System.Threading;
using AssetManagement.AddressableAssets;
using Cysharp.Threading.Tasks;
using UnityEngine.ResourceManagement.AsyncOperations;
using Sapientia.Utility;

namespace AssetManagement
{
	public partial class AssetProvider
	{
		private sealed class AssetContainer : Container, IAssetContainer
		{
			private AssetProvider _owner;

			private int _releaseDelayMs;
			private double _releaseAt;
			private bool _releasePending;
			private bool _releaseTracked;

			public bool IsReleasePending { get => _releasePending; }

			public override double? ReleaseRemainingSeconds =>
				_releasePending && _usages <= 0
					? Math.Max(0d, _releaseAt - UnityEngine.Time.unscaledTimeAsDouble)
					: null;

			public AssetContainer(AssetProvider owner, object key, AsyncOperationHandle handle, int usages = 1)
				: base(key, handle, usages)
			{
				_owner = owner;
				_handle = handle;
			}

			public UniTask<T> LoadAsync<T>(CancellationToken cancellationToken = default, IProgress<float> progress = null) =>
				WaitForAssetAsync<T>(cancellationToken, progress);

			public void Release(bool full = false) => _owner?.ReleaseAssetContainer(this, full: full);

			public override void Retain()
			{
				base.Retain();
				_releasePending = false;
			}

			public bool TryUpdateReleaseDelay(int delayMs)
			{
				if (delayMs < _releaseDelayMs)
					return false;

				_releaseDelayMs = delayMs;
				return true;
			}

			public bool ReleaseUsage(int delayMs, double currentTime)
			{
				var updateDelay = TryUpdateReleaseDelay(delayMs);

				if (_usages > 0)
					_usages--;

				if (_usages > 0)
					return false;

				if (_releaseDelayMs <= 0)
					return true;

				if (!_releasePending || updateDelay)
					_releaseAt = currentTime + _releaseDelayMs / 1000d;

				_releasePending = true;
				return false;
			}

			public bool IsReleaseReady(double currentTime) =>
				_releasePending && _usages <= 0 && currentTime >= _releaseAt;

			public bool TryTrackRelease()
			{
				if (_releaseTracked)
					return false;

				_releaseTracked = true;
				return true;
			}

			public void StopTrackingRelease() => _releaseTracked = false;

			public async UniTask<T> GetAssetAsync<T>(CancellationToken cancellationToken, IProgress<float> progress = null)
			{
				Retain();

				try
				{
					return await WaitForAssetAsync<T>(cancellationToken, progress);
				}
				catch
				{
					Release();
					throw;
				}
			}

			private async UniTask<T> WaitForAssetAsync<T>(CancellationToken cancellationToken, IProgress<float> progress)
			{
				if (_cts == null || AsyncUtility.AnyCancellation(cancellationToken, _cts.Token))
					throw new OperationCanceledException(cancellationToken);

				AttachProgress(progress);
				try
				{
					using var linkedCts = _cts.Link(cancellationToken);
					var isCanceled = await _handle.ToUniTask(this, cancellationToken: linkedCts.Token)
						.SuppressCancellationThrow();

					if (isCanceled)
						linkedCts.Token.ThrowIfCancellationRequested();

					if (_handle.Status != AsyncOperationStatus.Succeeded)
						throw _handle.OperationException ?? AssetManagementDebug.Exception("Failed to load asset");

					Report(1f);
					return (T) _handle.Result;
				}
				finally
				{
					DetachProgress(progress);
				}
			}

			//Синхронное получение: форсим завершение хэндла (WaitForCompletion блокирует поток)
			public T GetAsset<T>()
			{
				Retain();

				_handle.WaitForCompletion();
				Report(1f);
				return (T) _handle.Result;
			}

			public override void Dispose()
			{
				base.Dispose();

				_releaseDelayMs = 0;
				_releaseAt = 0;
				_releasePending = false;
				_releaseTracked = false;

				_owner = null;
			}
		}

		private class AssetsContainer : Container
		{
			public override bool IsCollection { get => true; }

			public AssetsContainer(object key, AsyncOperationHandle handle, int usages = 1)
				: base(key, handle, usages)
			{
			}

			public async UniTask<IList<T>> GetAssetsAsync<T>(CancellationToken cancellationToken, IProgress<float> progress = null)
			{
				Retain();

				AttachProgress(progress);
				try
				{
					using var linkedCts = _cts.Link(cancellationToken);
					var isCanceled = await _handle.ToUniTask(this, cancellationToken: linkedCts.Token)
						.SuppressCancellationThrow();

					if (isCanceled)
					{
						ReleaseUsage();
						cancellationToken.ThrowIfCancellationRequested();
					}

					Report(1f);
					return (IList<T>) _handle.Result;
				}
				finally
				{
					DetachProgress(progress);
				}
			}
		}

		private abstract class Container : IDisposable, IProgress<float>, IAssetContainerState
		{
			private const string SOURCE_NAME = "Addressables";

			protected object _key;

			protected int _usages;
			protected AsyncOperationHandle _handle;

			protected CancellationTokenSource _cts;
			private AssetProgressRelay _progress;

			public IAssetContainerState State => this;
			public object Key { get => _key; }
			public object Asset { get => IsLoaded ? _handle.Result : null; }

			public bool IsLoaded { get => _handle.IsValid() && _handle is {IsDone: true, Status: AsyncOperationStatus.Succeeded}; }

			public int UsageCount { get => _usages; }
			public float Progress { get => _progress.Value; }

			public virtual double? ReleaseRemainingSeconds { get => null; }
			public virtual bool IsCollection { get => false; }
			string IAssetContainerState.SourceName { get => SOURCE_NAME; }

			protected Container(object key, AsyncOperationHandle handle, int usages = 1)
			{
				_key = key;
				_handle = handle;
				_usages = usages;

				_cts = new();
				_progress.Report(handle.IsValid() ? handle.PercentComplete : 0f);
			}

			public virtual void Dispose()
			{
				_cts?.Trigger();
				_cts = null;

				_progress.Dispose();
				_handle.ReleaseSafe();
				_handle = default;

				_usages = 0;
				_key = null;
			}

			public virtual void Retain() => _usages++;

			public void Report(float value) => _progress.Report(value);

			public void AttachProgress(IProgress<float> progress)
			{
				if (!ReferenceEquals(progress, this))
					_progress.Attach(progress);
			}

			public void DetachProgress(IProgress<float> progress)
			{
				if (!ReferenceEquals(progress, this))
					_progress.Detach(progress);
			}

			public bool ReleaseUsage()
			{
				if (_usages > 0)
					_usages--;

				if (_usages > 0)
					return false;

				Dispose();
				return true;
			}
		}
	}
}
