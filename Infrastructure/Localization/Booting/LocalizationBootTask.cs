using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Localization;
using Sapientia;
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
		public override int Priority => HIGH_PRIORITY - 200;

		public LocTableReference tableReference;

		public override async UniTask RunAsync(Blackboard _, CancellationToken token = default)
		{
			var resolver = new LocalizationResolver(in tableReference);
			await resolver.InitializeAsync(token);
			LocManager.Set(resolver);
		}

		protected override void OnDispose()
		{
			LocManager.Clear();
		}
	}
}
