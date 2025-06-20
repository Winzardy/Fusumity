using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sapientia.Collections;
using Sapientia.Utility;

namespace AssetManagement
{
	public class AssetsPreloader : IDisposable
	{
		private bool _preloaded;

		private CancellationTokenSource _cts;
		private HashSet<IAssetReferenceEntry> _assets;

		public void Dispose()
		{
			TryRelease();
		}

		public void TryRelease()
		{
			AsyncUtility.Trigger(ref _cts);

			if (!_preloaded)
				return;

			_assets?.Release();
			_assets = null;

			_preloaded = false;
		}

		public void Preload(params IAssetReferenceEntry[] assets)
		{
			TryRelease();

			if (assets.IsNullOrEmpty())
				return;

			_assets = new(assets);

			if (_assets.IsNullOrEmpty())
				return;

			PreloadAsync().Forget();
		}

		public bool TryRelease(IAssetReferenceEntry entry)
		{
			if (_assets.IsNullOrEmpty())
				return false;

			if (!_assets.Remove(entry))
				return false;

			entry.Release();
			return true;
		}

		private async UniTaskVoid PreloadAsync()
		{
			_cts = new CancellationTokenSource();

			try
			{
				await _assets.PreloadAsync(_cts.Token);
				_preloaded = true;
			}
			finally
			{
				_cts?.Trigger();
				_cts = null;
			}
		}
	}
}
