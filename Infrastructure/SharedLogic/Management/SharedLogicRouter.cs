using System;
using Sapientia;

namespace SharedLogic
{
	public class SharedLogicRouter : ISharedLogicRouter
	{
		private IDateTimeProvider _dateTimeProvider;
		private ICommandRunner _runner;

		public SharedLogicRouter(ISharedRoot root, IDateTimeProvider dateTimeProvider, ICommandRunner runner)
		{
			_runner = runner;
			_dateTimeProvider = dateTimeProvider;
		}

		public IDateTimeProvider DateProvider => _dateTimeProvider;

		public void SetupServerTime(DateTime newDateTime)
		{
			//throw new NotImplementedException();
		}

		public bool ExecuteCommand<T>(in T command) where T : struct, ICommand
		{
			//	throw new NotImplementedException();
			_runner.Execute(in command);
			return true;
		}
	}
}
