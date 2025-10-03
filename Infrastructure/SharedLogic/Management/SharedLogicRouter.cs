using Sapientia;

namespace SharedLogic
{
	public class SharedLogicRouter : ISharedLogicRouter
	{
		private ISharedRoot _root;
		private ICommandRunner _runner;
		private IDateTimeProvider _dateTimeProvider;
		private ISharedLogicLocalCacheInfoProvider _localCacheInfoProvider;

		public long Timestamp => _dateTimeProvider.DateTime.Ticks;

		public SharedLogicRouter(ISharedRoot root, IDateTimeProvider dateTimeProvider, ICommandRunner runner,
			ISharedLogicLocalCacheInfoProvider localCacheInfoProvider)
		{
			_root = root;
			_dateTimeProvider = dateTimeProvider;
			_runner = runner;
			_localCacheInfoProvider = localCacheInfoProvider;
		}

		public bool ExecuteCommand<T>(in T command) where T : struct, ICommand
		{
			var node = _root.GetNode<TimeSharedNode>();
			using (node.ProviderSuppressFlow())
			{
				_runner.Execute(in command);
				return true;
			}
		}

		public SharedLogicCacheInfo GetCacheInfo() => _localCacheInfoProvider.GetInfo();
	}
}
