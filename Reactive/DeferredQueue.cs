using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fusumity.Reactive
{
	/// <summary>
	/// Очередь, которая после возвращения приложения из фона пропускает не больше нескольких элементов
	/// за кадр, а остальное разбирает в следующих кадрах (борьба с ANR)
	/// </summary>
	public class DeferredQueue<T> : IDisposable
	{
		private readonly Action<T> _handler;
		private readonly Queue<T> _queue;

		private readonly DeferredGate _gate;
		private readonly bool _ownsGate;

		private bool _handling;

		public bool IsEmpty { get => _queue.Count == 0; }

		/// <inheritdoc cref="DeferredGate"/>
		public DeferredGate Gate { get => _gate; }

		public DeferredQueue(Action<T> handler, DeferredGate gate = null)
		{
			_handler = handler;
			_queue = new Queue<T>();

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
		}

		/// <summary>
		/// Можно ли обработать элемент прямо сейчас, не откладывая.
		/// Нужно тем, кому перед откладыванием требуется скопировать данные
		/// </summary>
		public bool CanHandleNow()
		{
			// Обработчик мог породить новый элемент — его пропускаем, чтобы не ломать порядок
			if (_handling)
				return true;

			if (!IsEmpty)
				return false;

			return !_gate.IsOpen || _gate.TryTakeBudget();
		}

		/// <summary>
		/// Обработать элемент сейчас или отложить, если бюджет кадра исчерпан
		/// </summary>
		public void Handle(T item)
		{
			if (CanHandleNow())
			{
				Invoke(item);
				return;
			}

			_queue.Enqueue(item);
		}

		public void Enqueue(T item) => _queue.Enqueue(item);

		/// <summary>
		/// Немедленно обработать всё отложенное и закрыть окно
		/// </summary>
		public void Flush()
		{
			_gate.Close();

			while (!IsEmpty)
				Invoke(_queue.Dequeue());
		}

		private void HandleUpdate()
		{
			if (IsEmpty)
				return;

			if (!_gate.IsHoldElapsed)
				return;

			while (!IsEmpty && _gate.TryTakeBudget())
				Invoke(_queue.Dequeue());
		}

		private void Invoke(T item)
		{
			_handling = true;

			try
			{
				_handler.Invoke(item);
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
			finally
			{
				_handling = false;
			}
		}
	}
}
