using System;
using System.Threading;
using Content;
using Cysharp.Threading.Tasks;
using Sapientia;
using Sapientia.ServiceManagement;
using SharedLogic;
using Sirenix.OdinInspector;
using Survivor.Interop.SharedLogic;

namespace Booting.SharedLogic
{
	[TypeRegistryItem(
		"\u2009Shared Logic",
		"",
		SdfIconType.Gear)]
	[Serializable]
	public class SharedLogicBootTask : BaseBootTask
	{
		private ISharedRoot _sharedRoot;

		public override async UniTask RunAsync(CancellationToken token = default)
		{
			await Initialize();
			await UniTask.NextFrame(token);
		}

		private async UniTask Initialize()
		{
			// Часть сервисов инициализируется здесь, т.к. есть зависимости на другие бут таски
			// Перенести в SharedLogicInitializer
			IDateTimeProvider dateTimeProvider = new SharedDateTimeProvider().RegisterAsService();
			dateTimeProvider.RegisterAsService();

			var registrar = new SurvivorSharedNodesRegistrar();
			_sharedRoot = new SharedRoot(registrar, dateTimeProvider, SLDebug.logger);
			_sharedRoot.Initialize();
			_sharedRoot.RegisterAsService();
		}

		protected override void OnDispose()
		{
			ServiceLocator<ISharedRoot>.UnRegister();

			_sharedRoot?.Dispose();
		}
	}
}
