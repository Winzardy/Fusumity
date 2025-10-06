using System;
using System.Threading;
using Content;
using Cysharp.Threading.Tasks;
using Sapientia;
using Sapientia.ServiceManagement;
using SharedLogic;
using Sirenix.OdinInspector;

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
		private ICommandCenter _center;
		private ISharedDataStreamer _dataStreamer;

		public override async UniTask RunAsync(CancellationToken token = default)
		{
			await Initialize();
			await UniTask.NextFrame(token);
		}

		private async UniTask Initialize()
		{
			IDateTimeProvider dateTimeProvider = new SharedDateTimeProvider();
			dateTimeProvider.RegisterAsService();

			var configuration = ContentManager.Get<SharedLogicConfiguration>();

			var registrar = configuration.registrarFactory.Create();
			_sharedRoot = new SharedRoot(registrar, dateTimeProvider, SLDebug.logger);
			_sharedRoot.Initialize();
			_sharedRoot.RegisterAsService();

			_dataStreamer = configuration.dataStreamerFactory.Create(_sharedRoot);
			_dataStreamer.RegisterAsService();

			_center = configuration.center.Create(_dataStreamer);
			await _center.InitializeAsync();

			var commandRunner = new ClientCommandRunner(_sharedRoot, _center, SLDebug.logger);
			var router = new SharedLogicRouter(_sharedRoot, dateTimeProvider, commandRunner);
			SharedLogicManager.Initialize(router);
		}

		protected override void OnDispose()
		{
			ServiceLocator<ISharedRoot>.UnRegister();

			_sharedRoot?.Dispose();

			if (_center is IDisposable center)
				center.Dispose();

			if (_dataStreamer is IDisposable dataHandler)
				dataHandler.Dispose();
		}
	}
}
