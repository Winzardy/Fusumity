using System;
using Fusumity.Reactive;

namespace SharedLogic
{
	/// <summary>
	/// После возвращения приложения из фона держит лимит команд на кадр: всё сверх лимита уходит
	/// в очередь и разбирается порциями в следующих кадрах, чтобы не грузить первый кадр (борьба с ANR)
	/// </summary>
	public class DeferredCommandRunner : CommandRunnerDecorator, IDisposable
	{
		private CommandBuffer _buffer;

		private readonly DeferredGate _gate;
		private readonly bool _ownsGate;

		private bool _executing;

		public override bool IsEmpty { get => _buffer.IsEmpty && base.IsEmpty; }

		/// <inheritdoc cref="DeferredGate"/>
		public DeferredGate Gate { get => _gate; }

		public DeferredCommandRunner(ICommandRunner runner, DeferredGate gate = null) : base(runner)
		{
			_buffer = new CommandBuffer();

			_ownsGate = gate == null;
			_gate = gate ?? new DeferredGate();
			_gate.Closed += Flush;

			UnityLifecycle.UpdateEvent.Subscribe(HandleUpdate);
		}

		public void Dispose()
		{
			UnityLifecycle.UpdateEvent.UnSubscribe(HandleUpdate);

			_gate.Closed -= Flush;

			Flush();

			if (_ownsGate)
				_gate.Dispose();

			_buffer.Dispose();
			_buffer = null;
		}

		/// <summary>
		/// Немедленно исполнить все отложенные команды и закрыть окно
		/// </summary>
		public void Flush()
		{
			_gate.Close();

			if (_buffer.IsEmpty)
				return;

			Drain(false);
		}

		/// <inheritdoc/>
		public override bool Execute<T>(in T command)
		{
			// Команда во время исполнения может породить новые — они идут мимо очереди,
			// их порядок держит буфер ClientCommandRunner
			if (_executing)
				return base.Execute(in command);

			// Порядок команд важен: пока очередь не разобрана, новые уходят в её конец
			if (_buffer.IsEmpty && !_gate.IsOpen)
				return Run(in command);

			// Команду, которая требует немедленной реакции, не откладываем, но сохраняем порядок
			if (command.Priority == CommandPriority.Immediately)
			{
				Flush();
				return Run(in command);
			}

			if (_buffer.IsEmpty && _gate.TryTakeBudget())
				return Run(in command);

			_buffer.Enqueue(in command);
			return true;
		}

		private bool Run<T>(in T command)
			where T : struct, ICommand
		{
			_executing = true;

			try
			{
				return base.Execute(in command);
			}
			finally
			{
				_executing = false;
			}
		}

		private void HandleUpdate()
		{
			if (_buffer.IsEmpty)
				return;

			if (!_gate.IsHoldElapsed)
				return;

			Drain(true);
		}

		private void Drain(bool budgeted)
		{
			_executing = true;

			try
			{
				while (!_buffer.IsEmpty && (!budgeted || _gate.TryTakeBudget()))
				{
					try
					{
						_buffer.Run(this);
					}
					catch (Exception exception)
					{
						SLDebug.LogException(exception);
					}
					finally
					{
						_buffer.Dequeue();
					}
				}
			}
			finally
			{
				_executing = false;

				if (_buffer.IsEmpty)
					_buffer.Clear();
			}
		}
	}
}
