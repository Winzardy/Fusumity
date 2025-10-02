using System;
using Sapientia;

namespace SharedLogic
{
	public class SharedLogicRouter : ISharedLogicRouter
	{
		private IDateTimeProvider _dateTimeProvider;
		private ICommandRunner _runner;
		private ISharedRoot _root;

		public long Timestamp => _dateTimeProvider.DateTime.Ticks;

		public SharedLogicRouter(ISharedRoot root, IDateTimeProvider dateTimeProvider, ICommandRunner runner)
		{
			_root = root;
			_runner = runner;
			_dateTimeProvider = dateTimeProvider;
		}

		public void SetupServerTime(DateTime newDateTime)
		{
			//throw new NotImplementedException();
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
	}
}
