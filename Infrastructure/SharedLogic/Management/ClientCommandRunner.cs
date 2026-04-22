using System;
using System.Runtime.CompilerServices;

namespace SharedLogic
{
	public class ClientCommandRunner : ICommandRunner, IDisposable
	{
		private readonly ISharedRoot _root;
		private readonly ICommandCenter _center;

		private CommandBuffer _buffer;

		public event Action<Exception, string> OnException;

		public bool IsEmpty { get => _buffer.IsEmpty; }

		public ClientCommandRunner(ISharedRoot root, ICommandCenter center)
		{
			_center = center;
			_root = root;

			_buffer = new CommandBuffer();
		}

		public void Dispose()
		{
			_buffer.Dispose();
			_buffer = null;
		}

		/// <inheritdoc/>
		public bool Execute<T>(in T command) where T : struct, ICommand
		{
			// По идее валидация здесь не нужна, а возможно даже вредна.
			// Например, если бы команды не запускались синхронно, а клались в буфер:
			// 1. имеется 0 монет
			// 2. добавляем 10 монет (валидация проходит успешно)
			// 3. отнимает 10 монет (валидация будет провалена на момент добавления команды, но прошла бы, на момент исполнения)
			//
			// Данная проверка здесь, потому что код из другого места приехал сюда, т.е. по факту текущий написанный код опирается на это поведение.
			ref var hackDefensiveCopy = ref Unsafe.AsRef(in command);
			if (!hackDefensiveCopy.Validate(_root, out var exception1))
			{
				OnException?.Invoke(exception1, "validation on start");
				return false;
			}

			var isFirstInQueue = _buffer.Count == 0;

			// Добавляем команду в очередь, поскольку вызов Execute() может породить новые команды, и нам нужно, чтобы они были упорядочены
			_buffer.Enqueue(in command);

			// Только первый в очереди отвечает за выполнение и отправку команд.
			if (!isFirstInQueue)
				return true;

			// Если команда первая в очереди, то мы только что ее провадилировали.
			// Дополнительно валидировать нужно только те команды, которые реактивно добавились в буфер
			bool needValidate = false;

			while (!_buffer.IsEmpty)
			{
				// В большинстве случае эта валидация уже была, непосредственно перед Execute().
				// Необходимость в валидации, если предыдущая команда каким-то образом сделала команду в очереди недействительной,
				// клиент никогда не узнает и выполнит эту команду без повторного запуска
				if (needValidate && !_buffer.Validate(_root, out var exception))
				{
					OnException?.Invoke(exception, "cycle validation");
					break;
				}

				needValidate = true;

				try
				{
					_buffer.Execute(_root);
					_buffer.OnExecute(_root);

					_buffer.Send(_center);
					_buffer.Dequeue();
				}
				catch (Exception ex)
				{
					OnException?.Invoke(ex, "exception");
					break;
				}
			}

			bool success = _buffer.IsEmpty;
			_buffer.Clear();
			return success;
		}
	}
}
