using System;
using System.Collections;
using System.Collections.Generic;

namespace UI
{
	// TODO: переделать...
	public class UIRootWidgetQueue<TWidget, TArgs> : IDisposable, IEnumerable<KeyValuePair<TWidget, TArgs>>
	{
		private Dictionary<TWidget, TArgs> _args = new(8);
		private LinkedList<TWidget> _queue = new();

		public void Dispose()
		{
			_args = null;
			_queue = null;
		}

		public void Enqueue(TWidget widget, in TArgs args)
		{
			_args[widget] = args;

			_queue.AddLast(widget);
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

		public void Clear()
		{
			_queue.Clear();
			_args.Clear();
		}

		public bool Contains(TWidget widget) => _queue.Contains(widget);

		IEnumerator<KeyValuePair<TWidget, TArgs>> IEnumerable<KeyValuePair<TWidget, TArgs>>.GetEnumerator() => _args.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => _queue.GetEnumerator();
	}
}
