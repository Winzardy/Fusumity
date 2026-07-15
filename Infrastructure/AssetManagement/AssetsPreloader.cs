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
		private HashSet<IAssetReference> _references;

		public void Dispose()
		{
			TryRelease();
		}

		public void TryRelease()
		{
			AsyncUtility.TriggerAndSetNull(ref _cts);

			if (!_preloaded)
				return;

			_references?.Release();
			_references = null;

			_preloaded = false;
		}

		public void Preload(params IAssetReference[] references)
		{
			TryRelease();

			if (references.IsNullOrEmpty())
				return;

			_references = new(references);

			if (_references.IsNullOrEmpty())
				return;

			_cts = new CancellationTokenSource();
			PreloadAsync(_references, _cts)
				.Forget(exception => AssetManagementDebug.LogException(exception));
		}

		public bool TryRelease(IAssetReference reference)
		{
			if (_references.IsNullOrEmpty())
				return false;

			if (!_references.Remove(reference))
				return false;

			reference.Release();
			return true;
		}

		private async UniTask PreloadAsync(HashSet<IAssetReference> references, CancellationTokenSource cts)
		{
			try
			{
				await references.PreloadAsync(cts.Token);

				if (ReferenceEquals(_references, references))
					_preloaded = true;
			}
			finally
			{
				cts.Trigger();

				if (ReferenceEquals(_cts, cts))
					_cts = null;
			}
		}
	}
}
