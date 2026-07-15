using System;
using System.Threading;
using AssetManagement;
using Cysharp.Threading.Tasks;
using Sapientia;
using UnityEngine.Scripting;
using Sirenix.OdinInspector;

namespace Booting.AssetManagement
{
	[TypeRegistryItem(
		"\u2009Asset Management", //В начале делаем отступ из-за отрисовки...
		"",
		SdfIconType.BoxSeam)]
	[Preserve]
	public class AssetManagementBootTask : BaseBootTask
	{
		public override int Priority => HIGH_PRIORITY - 10;

#if UNITY_EDITOR
		protected override bool ShouldSkipDispose { get => false; }
#endif

		public bool @await;
		public AssetLabelReference[] dependencyLabels;

		private Exception _initializationException;

		public override async UniTask RunAsync(Blackboard _, CancellationToken token = default)
		{
			_initializationException = null;

			var provider = new AssetProvider();
			AssetLoader.Set(provider);

			if (@await)
				await InitializeAsync(provider, token);
			else
				InitializeAsync(provider, token)
					.Forget(exception => AssetManagementDebug.LogException(exception));
		}

		private async UniTask InitializeAsync(AssetProvider provider, CancellationToken token)
		{
			try
			{
				await provider.InitializeAsync(dependencyLabels, token);
			}
			catch (Exception exception)
			{
				_initializationException = exception;
				throw;
			}
		}

		protected override void OnDispose()
		{
			AssetLoader.Clear();
		}

		public override bool IsReady() => _initializationException != null || AssetLoader.IsInitialized;
	}
}
