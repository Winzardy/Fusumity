namespace SharedLogic
{
	public interface ISharedLogicRouter
	{
		public bool ExecuteCommand<T>(in T command) where T : struct, ICommand;
	}
}
