namespace SharedLogic
{
	public interface ISharedLogicCenter : ICommandCenter
	{
	}

	/// <summary>
	/// Командный центр решает что с командой дальше делать, например: отправить на сервер, сохранить или еще что
	/// </summary>
	public interface ICommandCenter
	{
		/// <summary>
		/// Передаем команду в центр, чтобы он решил что с ней дальше делать, например: отправить на сервер, сохранить или еще что
		/// </summary>
		public void Submit<T>(in T command) where T : struct, ICommand;
	}
}
