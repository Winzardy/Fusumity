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

		public bool await;

		public override async UniTask RunAsync(Blackboard blackboard, CancellationToken token = default)
		{
			if (@await)
				await ImportAsync(blackboard, token);
			else
				ImportAsync(blackboard, token)
					.Forget();
		}

		private async UniTask ImportAsync(Blackboard blackboard, CancellationToken token)
		{
			using (blackboard.Register(tableReference, tableReference))
			{
				if (!LocManager.IsInitialized)
					await UniTask.WaitUntil(() => LocManager.IsInitialized, cancellationToken: token);
				await LocManager.AddTableAsync(tableReference, token);
			}
		}
	}
}
