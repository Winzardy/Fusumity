namespace SharedLogic
{
	public interface ISharedLogicRouter
	{
		// TODO: имеет смысл переназвать метод в Schedule, Submit или что-то более подходящее
		/// <returns>
		/// <c>true</c> — команда успешно добавлена в буфер;
		/// <c>false</c> — не удалось добавить команду в буфер
		/// </returns>
		/// <remarks>
		/// Команда может быть выполнена асинхронно или с задержкой.
		/// Возврат <c>true</c> означает только факт добавления в буфер
		/// и не отражает результат её выполнения.
		/// <para/>
		/// ⚠️ Не используйте возвращаемое значение для проверки успешности выполнения
		/// </remarks>
		bool ExecuteCommand<T>(ref T command) where T : struct, ICommand;
	}
}
