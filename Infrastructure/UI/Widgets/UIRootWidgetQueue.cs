using System;
using System.Collections;
using System.Collections.Generic;

namespace UI
{
	/// <summary>
	/// Кастомная очередь для Window и Popup, в случае Windows новые элементы встают в начале (Stack),
	/// а у Popup в конце (Queue)
	/// </summary>
	public class UIRootWidgetQueue<TWidget, TArgs> : IDisposable, IEnumerable<TWidget>
	{
		private readonly bool _addToLast;

		private Dictionary<TWidget, TArgs> _args = new(8);

		// LinkedList, чтобы иметь дешевую возможность удалять из середины
		private LinkedList<TWidget> _queue = new();

		public UIRootWidgetQueue(bool addToLast = true)
		{
			_addToLast = addToLast;
		}

		public void Dispose()
		{
			_args = null;
			_queue = null;
		}

		public void Enqueue(TWidget widget, TArgs args)
		{
			Enqueue(widget, args, _addToLast);
		}

		public void Enqueue(TWidget widget, TArgs args, bool addToLast)
		{
			_args[widget] = args;

			if (addToLast)
				_queue.AddLast(widget);
			else
				_queue.AddFirst(widget);
		}

		public bool IsEmpty() => _queue.Count <= 0;

		public (TWidget, TArgs) Dequeue()
		{
			var last = _queue.Last.Value;
			var args = _args[last];

			_queue.RemoveLast();
			_args.Remove(last);

			return (last, args);
		}

		public bool TryRemove(TWidget widget)
		{
			if (!_queue.Contains(widget))
				return false;

			_queue.Remove(widget);
			_args.Remove(widget);
			return true;
		}

		public bool Contains(TWidget widget) => _queue.Contains(widget);

		IEnumerator<TWidget> IEnumerable<TWidget>.GetEnumerator() => _queue.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => _queue.GetEnumerator();
	}
}
