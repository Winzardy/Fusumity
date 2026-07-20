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
		public override int Priority { get => HIGH_PRIORITY - 80; }
		public override string Name { get => $"{base.Name} ({tableReference})"; }

		public LocTableReference tableReference;

		public bool await;

		private bool _ready = true;

		protected override async UniTask RunTaskAsync(Blackboard blackboard, IProgress<BootProgressInfo> progress = null, CancellationToken token = default)
		{
			_ready = false;

			if (@await)
				await ImportAndMarkReadyAsync(blackboard, token);
			else
				ImportAndMarkReadyAsync(blackboard, token)
					.Forget(LocalizationDebug.LogException);
		}

		public override bool IsReady() => _ready;

		private async UniTask ImportAndMarkReadyAsync(Blackboard blackboard, CancellationToken token)
		{
			try
			{
				await ImportAsync(blackboard, token);
			}
			finally
			{
				_ready = true;
			}
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
