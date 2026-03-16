using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Localization;
using Sapientia;
using Sirenix.OdinInspector;

namespace Booting.Localization
{
	[TypeRegistryItem(
		"\u2009Localization Ready", //В начале делаем отступ из-за отрисовки...
		"",
		SdfIconType.Translate)]
	[Serializable]
	public class LocalizationReadyBootTask : BaseBootTask
	{
		public override int Priority => HIGH_PRIORITY;

		public override async UniTask RunAsync(Blackboard blackboard, CancellationToken token = default)
		{
			if (!LocManager.IsInitialized)
				await UniTask.WaitUntil(() => LocManager.IsInitialized, cancellationToken: token);

			await UniTask.WaitWhile(blackboard.Any<LocTableReference>, cancellationToken: token);
		}
	}
}
