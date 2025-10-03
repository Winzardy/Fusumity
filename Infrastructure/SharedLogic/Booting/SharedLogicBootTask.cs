using System;
using System.IO;
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
		"\u2009Shared Logic", //В начале делаем отступ из-за отрисовки...
		"",
		SdfIconType.Gear)]
	[Serializable]
	public class SharedLogicBootTask : BaseBootTask
	{
		private ISharedRoot _sharedRoot;
		private ICommandSender _sender;
		private ISharedDataManipulator _dataManipulator;

		public override async UniTask RunAsync(CancellationToken token = default)
		{
			await Initialize();
			await UniTask.NextFrame(token);
		}

		private UniTask Initialize()
		{
			IDateTimeProvider dateTimeProvider = new SharedDateTimeProvider();
			dateTimeProvider.RegisterAsService();

			var configuration = ContentManager.Get<SharedLogicConfiguration>();
			var registrar = configuration.registrarFactory.Create();

			_sharedRoot = new SharedRoot(registrar, dateTimeProvider, SLDebug.logger);
			_sender = configuration.commandSender.Create();

			var commandRunner = new ClientCommandRunner(_sharedRoot, _sender, SLDebug.logger);
			_dataManipulator = configuration.dataManipulatorFactory.Create(_sharedRoot);
			_dataManipulator.RegisterAsService();

			_sharedRoot.Initialize();
			_sharedRoot.RegisterAsService();

			var localCacheInfoProvider = configuration.localCacheInfoProvider;
			var cacheInfo = localCacheInfoProvider.GetInfo();
			if (File.Exists(cacheInfo.FullPath))
			{
				var json = File.ReadAllText(cacheInfo.FullPath);
				_dataManipulator.Load(json);
			}

			// var userProfileService = new UserProfileService()
			//    .RegisterAsService();
			//
			// var remoteUser = new RemoteUser(authSettings.RemoteUrl, GetClientVersion())
			//    .RegisterAsService();
			//
			// new AuthManager(remoteUser)
			//    .RegisterAsService();

			var router = new SharedLogicRouter(_sharedRoot, dateTimeProvider, commandRunner, localCacheInfoProvider);
			SharedLogicManager.Initialize(router);
			return UniTask.CompletedTask;
		}

		protected override void OnDispose()
		{
			ServiceLocator<ISharedRoot>.UnRegister();

			_sharedRoot?.Dispose();

			if (_sender is IDisposable sender)
				sender.Dispose();

			if (_dataManipulator is IDisposable dataHandler)
				dataHandler.Dispose();
		}

		// [Pure]
		// private static string GetClientVersion()
		// {
		// 	return AuthSettings.overrideClientVersion ?? ContentManager.Get<ProjectInfo>().version;
		// }
	}
}
