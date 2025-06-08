using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sapientia.Utility;

namespace UI
{
	/// <summary>
	/// Прослойка-класс в основном от которого наследуются все корневые виджеты (Window, Popup)
	/// </summary>
	public abstract class UIClosableRootWidget<TLayout> : UIBaseRootWidget<TLayout>
		where TLayout : UIBaseLayout
	{
		private CancellationTokenSource _closableCts;

		protected CancellationToken ClosableCancellationToken => ClosableCancellationTokenSource.Token;
		protected CancellationTokenSource ClosableCancellationTokenSource => _closableCts ??= new CancellationTokenSource();

		protected internal override void OnBeganClosingInternal()
		{
			AsyncUtility.Trigger(ref _closableCts);
			base.OnBeganClosingInternal();
		}

		public abstract void RequestClose();

		protected async UniTask RequestCloseAsync(int delayMs = 500)
		{
			if (!Active)
				return;

			using var linked = ClosableCancellationTokenSource.Link(DisposeCancellationToken);
			await UniTask.Delay(delayMs, cancellationToken: linked.Token);

			if (_closableCts != null)
				RequestClose();
		}
	}

	/// <summary>
	/// Прослойка-класс в основном от которого наследуются все корневые виджеты (Screen, Window, Popup)
	/// </summary>
	public abstract class UIBaseRootWidget<TLayout> : UISelfConstructedLayerWidget<TLayout>
		where TLayout : UIBaseLayout
	{
		protected override bool UseSetAsLastSibling => true;

		/// <summary>
		/// Дает возможность использовать одинаковые настройки для разных типов
		/// Можно конечно выдавать entry по типу... Но тогда совсем жесткая привязка <br/><br/>
		/// ВАЖНО! <br/>
		/// Нельзя использовать разные EntryId для экранов (<see cref="UIScreen"/>)
		/// </summary>
		protected abstract string Id { get; }
	}

	/// <summary>
	/// Кастомная очередь для Window и Popup, в случае Windows новые элементы встают в начале (Stack),
	/// а у Popup в конце (Queue)
	/// </summary>
	public class PanelQueue<TPanel, TArgs> : IDisposable, IEnumerable<TPanel>
	{
		private bool _addToLast;

		private Dictionary<TPanel, TArgs> _args = new(8);

		//LinkedList, чтобы иметь дешевую возможность удалять из середины
		private LinkedList<TPanel> _queue = new();

		public PanelQueue(bool addToLast = true)
		{
			_addToLast = addToLast;
		}

		public void Dispose()
		{
			_args = null;
			_queue = null;
		}

		public void Enqueue(TPanel panel, TArgs args)
		{
			Enqueue(panel, args, _addToLast);
		}

		public void Enqueue(TPanel panel, TArgs args, bool addToLast)
		{
			_args[panel] = args;

			if (addToLast)
			{
				_queue.AddLast(panel);
			}
			else
			{
				_queue.AddFirst(panel);
			}
		}

		public bool IsEmpty() => _queue.Count <= 0;

		public (TPanel, TArgs) Dequeue()
		{
			var last = _queue.Last.Value;
			var args = _args[last];

			_queue.RemoveLast();
			_args.Remove(last);

			return (last, args);
		}

		public bool TryRemove(TPanel panel)
		{
			if (!_queue.Contains(panel))
				return false;

			_queue.Remove(panel);
			_args.Remove(panel);
			return true;
		}

		public bool Contains(TPanel window) => _queue.Contains(window);

		IEnumerator<TPanel> IEnumerable<TPanel>.GetEnumerator() => _queue.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => _queue.GetEnumerator();
	}
}
