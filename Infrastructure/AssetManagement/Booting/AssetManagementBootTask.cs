using System.Threading;
using AssetManagement;
using Cysharp.Threading.Tasks;
using Fusumity.Reactive;
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

		public override UniTask RunAsync(CancellationToken token = default)
		{
			var management = new AssetProvider();
			AssetLoader.Initialize(management);
			return UniTask.CompletedTask;
		}

		protected override void OnDispose()
		{
			if (UnityLifecycle.ApplicationQuitting)
				return;

			AssetLoader.Terminate();
		}
	}
}
