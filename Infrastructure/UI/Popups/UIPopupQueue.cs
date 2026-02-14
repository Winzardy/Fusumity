using System;
using System.Collections;
using System.Collections.Generic;

namespace UI.Popups
{
	public class UIPopupQueue<TArgs> : IDisposable, IEnumerable<KeyValuePair<IPopup, TArgs>>
	{
		private readonly bool _addToLast;

		private Dictionary<IPopup, TArgs> _args = new(8);

		private LinkedList<IPopup> _queue = new();

		public UIPopupQueue(bool addToLast = true)
		{
			_addToLast = addToLast;
		}

		public void Dispose()
		{
			_args = null;
			_queue = null;
		}

		public void Update(IPopup popup, TArgs args)
		{
			SetArgsInternal(popup, args);
		}

		public void Enqueue(IPopup popup, TArgs args)
		{
			Enqueue(popup, args, _addToLast);
		}

		public void Enqueue(IPopup popup, TArgs args, bool addToLast)
		{
			SetArgsInternal(popup, args);

			_queue.AddFirst(popup);
		}

		private void SetArgsInternal(IPopup popup, TArgs args)
		{
			_args[popup] = args;
		}

		public bool IsEmpty() => _queue.Count <= 0;

		public (IPopup, TArgs) Dequeue()
		{
			var last = _queue.Last.Value;
			var args = _args[last];

			_queue.RemoveLast();
			_args.Remove(last);

			return (last, args);
		}

		public bool TryRemove(IPopup popup)
		{
			if (!_queue.Contains(popup))
				return false;

			_queue.Remove(popup);
			_args.Remove(popup);
			return true;
		}

		public void Clear()
		{
			_queue.Clear();
			_args.Clear();
		}

		public bool Contains(IPopup popup) => _queue.Contains(popup);

		IEnumerator<KeyValuePair<IPopup, TArgs>> IEnumerable<KeyValuePair<IPopup, TArgs>>.GetEnumerator() => _args.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => _queue.GetEnumerator();
	}
}
