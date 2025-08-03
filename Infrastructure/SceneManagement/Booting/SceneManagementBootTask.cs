using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Fusumity.Reactive;
using SceneManagement;
using Sirenix.OdinInspector;

namespace Booting.SceneManagement
{
	[TypeRegistryItem(
		"\u2009Scene Management", //В начале делаем отступ из-за отрисовки...
		"",
		SdfIconType.Film)]
	[Serializable]
	public class SceneManagementBootTask : BaseBootTask
	{
		public override int Priority => HIGH_PRIORITY - 140;

		public override UniTask RunAsync(CancellationToken token = default)
		{
			var hub = new SceneLoaderHub();
			SceneManager.Initialize(hub);
			return UniTask.CompletedTask;
		}

		protected override void OnDispose()
		{
			SceneManager.Terminate();
		}
	}
}
