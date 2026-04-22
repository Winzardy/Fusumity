using Sapientia;

namespace SharedLogic
{
	public class AddTimestampCommandRunner : CommandRunnerDecorator
	{
		private readonly ISharedRoot _root;
		private readonly ISystemTimeProvider _dateTimeProvider;

		public AddTimestampCommandRunner(ISharedRoot root, ISystemTimeProvider dateTimeProvider, ICommandRunner runner)
			: base(runner)
		{
			_root             = root;
			_dateTimeProvider = dateTimeProvider;
		}

		/// <inheritdoc/>
		public override bool Execute<T>(in T command)
		{
			var node = _root.GetNode<TimeSharedNode>();
			using (node.ProviderSuppressScope())
			{
				if (IsEmpty)
				{
					var timeSetCommand = new TimeSetCommand(_dateTimeProvider.SystemTime.Ticks, _root.Revision);
					if (!base.Execute(in timeSetCommand))
						return false;
				}

				return base.Execute(in command);
			}
		}
	}
}
