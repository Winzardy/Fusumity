using System;

namespace SharedLogic
{
	/// <summary>
	/// Команда с <see cref="IEquatable{T}"/> живёт в очереди в одном экземпляре: пока такая же ждёт исполнения,
	/// повтор отбрасывается как принятый. Нужно поллерам, которые шлют команду по состоянию каждую секунду,
	/// а состояние меняется только при исполнении: в окне после возврата из фона повтор ложился в отложенную
	/// очередь и падал на валидации при дренаже
	/// </summary>
	public class DistinctCommandRunner : CommandRunnerDecorator, IDisposable
	{
		private readonly ICommandRunner _inner;

		public DistinctCommandRunner(ICommandRunner inner) : base(inner)
		{
			_inner = inner;
		}

		public void Dispose()
		{
			// Деструктор сервиса диспозит только вершину цепочки, отложенный раннер внутри освобождаем сами
			if (_inner is IDisposable disposable)
				disposable.Dispose();
		}

		/// <inheritdoc/>
		public override bool Execute<T>(in T command)
		{
			if (CommandTraits<T>.IsEquatable && HasPending(in command))
				return true;

			return base.Execute(in command);
		}

		private static class CommandTraits<T>
			where T : struct, ICommand
		{
			public static readonly bool IsEquatable = typeof(IEquatable<T>).IsAssignableFrom(typeof(T));
		}
	}
}
