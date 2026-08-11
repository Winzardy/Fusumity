using System;
using Sapientia.Collections;
using UnityEngine;

namespace Fusumity.Reactive
{
	public delegate void DeferredHandler<T>(in T item);

	/// <summary>
	/// Очередь, которая после возвращения приложения из фона пропускает не больше нескольких элементов
	/// за кадр, а остальное разбирает в следующих кадрах (борьба с ANR)
	/// </summary>
	public class DeferredQueue<T> : IDisposable
	{
		private readonly DeferredHandler<T> _handler;

		/// <remarks>
		/// Список вместо очереди, чтобы отдавать элемент обработчику по ссылке, без копии структуры.
		/// Голова двигается индексом, массив чистится когда очередь разобрана
		/// </remarks>
		private readonly SimpleList<T> _items;
		private int _head;

		private readonly DeferredGate _gate;
		private readonly bool _ownsGate;

		private bool _handling;

		public bool IsEmpty { get => _head >= _items.Count; }

		/// <inheritdoc cref="DeferredGate"/>
		public DeferredGate Gate { get => _gate; }

		public DeferredQueue(DeferredHandler<T> handler, DeferredGate gate = null)
		{
			_handler = handler;
			_items = new SimpleList<T>();

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

			_items.Dispose();
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
		public void Handle(in T item)
		{
			if (CanHandleNow())
			{
				Invoke(in item);
				return;
			}

			_items.Add(in item);
		}

		/// <remarks>
		/// ⚠️ Обработчик получает элемент по ссылке на внутренний массив, поэтому пополнять очередь
		/// во время разбора нельзя — <see cref="CanHandleNow"/> это гарантирует
		/// </remarks>
		public void Enqueue(in T item) => _items.Add(in item);

		/// <summary>
		/// Немедленно обработать всё отложенное и закрыть окно
		/// </summary>
		public void Flush()
		{
			_gate.Close();

			while (!IsEmpty)
				InvokeNext();
		}

		private void HandleUpdate()
		{
			if (IsEmpty)
				return;

			if (!_gate.IsHoldElapsed)
				return;

			while (!IsEmpty && _gate.TryTakeBudget())
				InvokeNext();
		}

		private void InvokeNext()
		{
			Invoke(in _items[_head]);
			_head++;

			if (!IsEmpty)
				return;

			_items.ClearPartial();
			_head = 0;
		}

		private void Invoke(in T item)
		{
			_handling = true;

			try
			{
				_handler.Invoke(in item);
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
