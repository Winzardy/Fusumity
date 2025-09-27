using System;
using Sapientia;

namespace SharedLogic
{
	public class ClientCommandRunner : ICommandRunner, IDisposable
	{
		private readonly ISharedRoot _root;
		private readonly ICommandSender _sender;
		private readonly IDateTimeProvider _dateTimeProvider;
		private readonly ILogger _logger;

		private CommandBuffer _buffer;

		public long Timestamp => _dateTimeProvider.DateTime.Ticks;

		public ClientCommandRunner(ISharedRoot root,
			ICommandSender sender,
			IDateTimeProvider dateTimeProvider,
			ILogger logger = null)
		{
			_sender = sender;
			_root = root;
			_logger = logger;
			_dateTimeProvider = dateTimeProvider;

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

			while (_buffer.Count > 0)
			{
				// В большинстве случае эта валидация уже была, непосредственно перед Execute().
				// Необходимость в валидации, если предыдущая команда каким-то образом сделала команду в очереди недействительной,
				// клиент никогда не узнает
				// и выполнит эту команду без повторного запуска
				if (!_buffer.Validate(_root, out var exception))
				{
					_logger?.Exception(exception);
					_buffer.Dequeue();
					break;
				}

				try
				{
					_buffer.Execute(_root);
					_buffer.Send(_sender);
				}
				catch (Exception e)
				{
					_logger?.Exception(e);
				}
				finally
				{
					_buffer.Dequeue();
				}
			}

			_buffer.Clear();
		}
	}
}
