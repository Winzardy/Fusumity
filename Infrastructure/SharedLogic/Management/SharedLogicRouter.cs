using Sapientia;

namespace SharedLogic
{
	public class SharedLogicRouter : ISharedLogicRouter
	{
		private ISharedRoot _root;
		private ICommandRunner _runner;
		private ISystemTimeProvider _dateTimeProvider;

		public long Timestamp { get => _dateTimeProvider.SystemTime.Ticks; }

		public SharedLogicRouter(ISharedRoot root, ISystemTimeProvider dateTimeProvider, ICommandRunner runner)
		{
			_root             = root;
			_dateTimeProvider = dateTimeProvider;
			_runner           = runner;
		}

		/// <inheritdoc/>
		public bool ExecuteCommand<T>(ref T command) where T : struct, ICommand
		{
			var node = _root.GetNode<TimeSharedNode>();
			using (node.ProviderSuppressScope())
			{
				if (_runner.IsEmpty)
				{
					var timeSetCommand = new TimeSetCommand(_dateTimeProvider.SystemTime.Ticks, _root.Revision);
					if (!timeSetCommand.Validate(_root, out var timeException))
					{
						SLDebug.LogException(timeException);
						return false;
					}

					_runner.Execute(in timeSetCommand);
				}

				if (!command.Validate(_root, out var exception))
				{
					SLDebug.LogException(exception);
					return false;
				}

				_runner.Execute(in command);
				return true;
			}
		}
	}
}
