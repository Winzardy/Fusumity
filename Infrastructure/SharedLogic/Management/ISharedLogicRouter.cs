namespace SharedLogic
{
	public interface ISharedLogicRouter
	{
		public bool ExecuteCommand<T>(ref T command) where T : struct, ICommand;
	}
}
