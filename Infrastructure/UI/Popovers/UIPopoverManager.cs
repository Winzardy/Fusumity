using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sapientia.Extensions;

namespace UI.Popovers
{
	/// <summary>
	/// Для управления используйте <see cref="UIPopoverDispatcher"/>
	/// </summary>
	//TODO:доделать!!!
	public partial class UIPopoverManager : IDisposable
	{
		private IPopover _current;

		private readonly PopoverPool _pool;

		private readonly PanelQueue<IPopover, IPopoverArgs> _queue;
		private readonly CancellationTokenSource _cts = new();

		internal event ShownDelegate Shown;
		internal event HiddenDelegate Hidden;
		internal event EnqueuedDelegate Enqueued;

		public UIPopoverManager()
		{
			var factory = new UIPopoverFactory();
			_pool = new PopoverPool(factory);

			InitializeAssetsPreloader();

			_queue = new(false);
		}

		void IDisposable.Dispose()
		{
			_current?.Dispose();

			_pool.Dispose();

			DisposeAssetsPreloader();

			_queue.Dispose();

			_cts?.Trigger();
		}

		internal T Show<T>(IPopoverArgs args, bool force = false)
			where T : UIWidget, IPopover
		{
			var popup = Get<T>();
			Show(popup, args, false, force);
			return popup;
		}

		internal bool TryHideCurrent()
		{
			if (_current == null)
				return false;

			TryHide(_current);
			return true;
		}

		internal bool IsActive<T>(T popup) where T : UIWidget, IPopover
		{
			if (_current == popup && _current.Active)
				return true;

			return _queue.Contains(popup);
		}

		internal bool IsActive(string id)
		{
			if (_current.Id == id && _current.Active)
				return true;

			foreach (var popup in _queue)
				if (popup.Id == id)
					return true;

			return false;
		}

		//TODO: добавить приоритет вместо force
		private void Show(IPopover popover, IPopoverArgs args, bool fromQueue, bool force = false)
		{
			if (_current != null)
			{
				if (force)
				{
					//Tак как вызывали форсом добавляем текущий попап первым в очереди на след. показ
					Enqueue(_current, popover.GetArgs(), addToLast: true);

					HideInternal(_current, true);
				}
				else
				{
					Enqueue(popover, args);
					return;
				}
			}

			ShowInternal(popover, ref args, fromQueue);

			_current = popover;
		}

		private void Enqueue(IPopover popover, IPopoverArgs args, bool addToLast = false)
		{
			_queue.Enqueue(_current, args, addToLast);

			Enqueued?.Invoke(popover, args, addToLast);
		}

		private void TryHide(IPopover popover)
		{
			_queue.TryRemove(popover);

			//запускаем закрытие и открытие из очереди нового только в том случае если закрывается текущий активный попап
			if (_current != popover)
			{
				Release(popover);
				return;
			}

			TryReleasePreloadedLayout(popover);

			HideInternal(popover, false);

			//Нужно подождать пока отыграется анимация и только потом возвращать в пул
			WaitHideAndReleaseAsync(popover, _cts.Token).Forget();

			_current = null;

			TryShowNext();
		}

		private void TryShowNext()
		{
			if (_queue.IsEmpty())
				return;

			var (popup, args) = _queue.Dequeue();
			Show(popup, args, true);
		}

		private T Get<T>()
			where T : UIWidget, IPopover
		{
			var popup = _pool.Get<T>();
			popup.RequestedClose += TryHide;
			return popup;
		}

		private async UniTaskVoid WaitHideAndReleaseAsync(IPopover popover, CancellationToken cancellationToken)
		{
			await UniTask.WaitWhile(() => popover.Visible, cancellationToken: cancellationToken);

			if (cancellationToken.IsCancellationRequested)
				return;

			Release(popover);
		}

		private void ShowInternal(IPopover popover, ref IPopoverArgs args, bool fromQueue)
		{
			popover.Show(ref args);
			Shown?.Invoke(popover, fromQueue);
		}

		private void HideInternal(IPopover popover, bool fromQueue)
		{
			popover.Hide();
			Hidden?.Invoke(popover, fromQueue);
		}

		private void Release(IPopover popover)
		{
			popover.RequestedClose -= TryHide;
			_pool.Release(popover);
		}

		#region Delegates

		public delegate void ShownDelegate(IPopover popup, bool fromQueue);

		public delegate void HiddenDelegate(IPopover popup, bool fromQueue);

		public delegate void EnqueuedDelegate(IPopover popup, IPopoverArgs args, bool addToLast);

		#endregion
	}
}
