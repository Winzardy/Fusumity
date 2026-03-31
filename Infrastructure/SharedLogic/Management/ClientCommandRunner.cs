using System;
using ILogger = Sapientia.ILogger;

namespace SharedLogic
{
	public class ClientCommandRunner : ICommandRunner, IDisposable
	{
		private readonly ISharedRoot _root;
		private readonly ICommandCenter _center;
		private readonly ILogger _logger;

		private CommandBuffer _buffer;

		public bool IsEmpty { get => _buffer.IsEmpty; }

		public ClientCommandRunner(
			ISharedRoot root,
			ICommandCenter center,
			// IExceptionHandler exceptionHandler,
			ILogger logger = null)
		{
			_center = center;
			_root = root;
			_logger = logger;

			_buffer = new CommandBuffer();
		}

		public void Dispose()
		{
			_buffer.Dispose();
			_buffer = null;
		}

		public void Execute<T>(in T command)
			where T : struct, ICommand
		{
			var isFirstInQueue = _buffer.Count == 0;

			// Добавляем команду в очередь, поскольку вызов Execute() может породить новые команды, и нам нужно, чтобы они были упорядочены
			_buffer.Enqueue(in command);

			// Только первый в очереди отвечает за выполнение и отправку команд.
			if (!isFirstInQueue)
				return;

			while (!_buffer.IsEmpty)
			{
				// В большинстве случае эта валидация уже была, непосредственно перед Execute().
				// Необходимость в валидации, если предыдущая команда каким-то образом сделала команду в очереди недействительной,
				// клиент никогда не узнает и выполнит эту команду без повторного запуска
				if (!_buffer.Validate(_root, out var exception))
				{
					_logger?.LogException(exception);
					// TODO: КОСТЯ ТУТ НАДО!
					// exceptionHandler.Handle(exception)
					break;
				}

				//try
				//{
				_buffer.Send(_center);

				_buffer.Execute(_root);
				_buffer.OnExecute(_root);

				//}
				// catch (Exception e)
				// {
				// 	_logger?.Exception(e);
				// TODO: КОСТЯ ТУТ НАДО!
				// exceptionHandler.Handle(exception)
				// }
				// finally
				// {
				_buffer.Dequeue();
				//}
			}

			_buffer.Clear();
		}
	}
}
