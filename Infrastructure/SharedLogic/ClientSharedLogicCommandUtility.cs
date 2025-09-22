namespace SharedLogic
{
	public static class ClientSharedLogicCommandUtility
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
			var timeSetCommand = new TimeSetCommand(commandRunner.Timestamp);
			commandRunner.Execute(in timeSetCommand);

			if (!command.Validate(root, out _))
				return false;

			commandRunner.Execute(in command);
			return true;
		}
	}
}
