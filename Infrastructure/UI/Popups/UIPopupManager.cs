using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sapientia.Utility;

namespace UI.Popups
{
	/// <summary>
	/// Для управления используйте <see cref="UIPopupDispatcher"/>
	/// </summary>
	public partial class UIPopupManager : IDisposable
	{
		private IPopup _current;

		private readonly PopupPool _pool;

		private readonly UIRootWidgetQueue<IPopup, IPopupArgs> _queue;
		private readonly CancellationTokenSource _cts = new();

		internal event ShownDelegate Shown;
		internal event HiddenDelegate Hidden;
		internal event EnqueuedDelegate Enqueued;

		public UIPopupManager()
		{
			var factory = new UIPopupFactory();
			_pool = new PopupPool(factory);

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

		internal T Show<T>(IPopupArgs args, bool force = false)
			where T : UIWidget, IPopup
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

		internal bool IsActive<T>(T popup) where T : UIWidget, IPopup
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
		private void Show(IPopup popup, IPopupArgs args, bool fromQueue, bool force = false)
		{
			if (_current != null)
			{
				if (force)
				{
					//Tак как вызывали форсом добавляем текущий попап первым в очереди на след. показ
					Enqueue(_current, addToLast: true);

					HideInternal(_current, true);
				}
				else
				{
					Enqueue(popup, args);
					return;
				}
			}

			ShowInternal(popup, args, fromQueue);

			_current = popup;
		}

		private void Enqueue(IPopup popup, IPopupArgs args = null, bool addToLast = false)
		{
			args ??= popup.GetArgs();
			_queue.Enqueue(popup, args, addToLast);
			Enqueued?.Invoke(popup, args, addToLast);
		}

		private void TryHide(IPopup popup)
		{
			_queue.TryRemove(popup);

			//запускаем закрытие и открытие из очереди нового только в том случае если закрывается текущий активный попап
			if (_current != popup)
			{
				Release(popup);
				return;
			}

			TryReleasePreloadedLayout(popup);

			HideInternal(popup, false);

			//Нужно подождать пока отыграется анимация и только потом возвращать в пул
			WaitHideAndReleaseAsync(popup, _cts.Token).Forget();

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
			where T : UIWidget, IPopup
		{
			var popup = _pool.Get<T>();
			popup.RequestedClose += TryHide;
			return popup;
		}

		private async UniTaskVoid WaitHideAndReleaseAsync(IPopup popup, CancellationToken cancellationToken)
		{
			await UniTask.WaitWhile(() => popup.Visible, cancellationToken: cancellationToken);

			if (cancellationToken.IsCancellationRequested)
				return;

			Release(popup);
		}

		private void ShowInternal(IPopup popup, IPopupArgs args, bool fromQueue)
		{
			popup.Show(args);
			Shown?.Invoke(popup, fromQueue);
		}

		private void HideInternal(IPopup popup, bool fromQueue)
		{
			popup.Hide(!fromQueue);
			Hidden?.Invoke(popup, fromQueue);
		}

		private void Release(IPopup popup)
		{
			popup.RequestedClose -= TryHide;
			_pool.Release(popup);
		}

		#region Delegates

		public delegate void ShownDelegate(IPopup popup, bool fromQueue);

		public delegate void HiddenDelegate(IPopup popup, bool fromQueue);

		public delegate void EnqueuedDelegate(IPopup popup, IPopupArgs args, bool addToLast);

		#endregion
	}
}
