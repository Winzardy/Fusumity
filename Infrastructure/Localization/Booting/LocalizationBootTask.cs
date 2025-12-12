using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Localization;
using Sirenix.OdinInspector;

namespace Booting.Localization
{
	[TypeRegistryItem(
		"\u2009Localization", //В начале делаем отступ из-за отрисовки...
		"",
		SdfIconType.Translate)]
	[Serializable]
	public class LocalizationBootTask : BaseBootTask
	{
		public override int Priority => HIGH_PRIORITY - 80;

		public LocTableReference tableReference;

		public override UniTask RunAsync(CancellationToken token = default)
		{
			var resolver = new LocalizationResolver(in tableReference);
			resolver.InitializeAsync(token)
			   .Forget();
			LocManager.Initialize(resolver);
			return UniTask.CompletedTask;
		}

		protected override void OnDispose()
		{
			LocManager.Terminate();
		}
	}
}
