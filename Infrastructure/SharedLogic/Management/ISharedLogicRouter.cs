namespace SharedLogic
{
	public interface ISharedLogicRouter
	{
		public long Timestamp { get; }
		public bool ExecuteCommand<T>(in T command) where T : struct, ICommand;

		public SharedLogicCacheInfo GetCacheInfo();
	}
}
