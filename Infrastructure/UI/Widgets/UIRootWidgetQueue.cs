using System;
using System.Collections;
using System.Collections.Generic;

namespace UI
{
	public class UIRootWidgetQueue<TWidget, TArgs> : IDisposable, IEnumerable<KeyValuePair<TWidget, TArgs>>
	{
		private LinkedList<QueueItem> _queue = new();

		public void Dispose()
		{
			_queue = null;
		}

		public void Enqueue(TWidget widget, in TArgs args)
		{
			_queue.AddLast(new QueueItem(widget, args));
		}

		public bool IsEmpty() => _queue.Count <= 0;

		public (TWidget, TArgs) Dequeue()
		{
			var last = _queue.Last.Value;

			_queue.RemoveLast();

			return (last.Widget, last.Args);
		}

		public bool TryRemove(TWidget widget)
		{
			var removed = false;
			var comparer = EqualityComparer<TWidget>.Default;
			var node = _queue.First;

			while (node != null)
			{
				var next = node.Next;

				if (comparer.Equals(node.Value.Widget, widget))
				{
					_queue.Remove(node);
					removed = true;
				}

				node = next;
			}

			return removed;
		}

		public void Clear()
		{
			_queue.Clear();
		}

		public bool Contains(TWidget widget)
		{
			var comparer = EqualityComparer<TWidget>.Default;
			var node = _queue.First;

			while (node != null)
			{
				if (comparer.Equals(node.Value.Widget, widget))
					return true;

				node = node.Next;
			}

			return false;
		}

		IEnumerator<KeyValuePair<TWidget, TArgs>> IEnumerable<KeyValuePair<TWidget, TArgs>>.GetEnumerator()
		{
			foreach (var entry in _queue)
				yield return new KeyValuePair<TWidget, TArgs>(entry.Widget, entry.Args);
		}

		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<KeyValuePair<TWidget, TArgs>>)this).GetEnumerator();

		private readonly struct QueueItem
		{
			public readonly TWidget Widget;
			public readonly TArgs Args;

			public QueueItem(TWidget widget, TArgs args)
			{
				Widget = widget;
				Args = args;
			}
		}
	}
}
