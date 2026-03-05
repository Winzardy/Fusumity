using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Localization;
using Sapientia;
using Sirenix.OdinInspector;

namespace Booting.Localization
{
	[TypeRegistryItem(
		"\u2009Localization Import", //В начале делаем отступ из-за отрисовки...
		"",
		SdfIconType.Translate)]
	[Serializable]
	public class LocalizationImportBootTask : BaseBootTask
	{
		public override int Priority => HIGH_PRIORITY - 80;

		public LocTableReference tableReference;

		public override async UniTask RunAsync(Blackboard _, CancellationToken token = default)
		{
			if (!LocManager.IsInitialized)
				await UniTask.WaitUntil(() => LocManager.IsInitialized, cancellationToken: token);
			await LocManager.AddTableAsync(tableReference, token);
		}
	}
}
