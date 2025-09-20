namespace SharedLogic
{
	public static class SharedCommandUtility
	{
		public static ICommandRunner commandRunner;

		public static bool ExecuteCommand<T>(this ISharedRoot root)
			where T : struct, ICommand
		{
			return ExecuteCommand(root, new T());
		}

		public static bool ExecuteCommand<T>(this ISharedRoot root, in T command)
			where T : struct, ICommand
		{
			if (command is ICommand)
			{
				var timedCommand = new TimedCommand(commandRunner.Timestamp);
				commandRunner.Execute(in timedCommand);
			}

			if (!command.Validate(root, out _))
				return false;

			commandRunner.Execute(in command);
			return true;
		}
	}
}
