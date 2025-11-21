using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sapientia.Utility;
using UnityEngine;

namespace UI.Popups
{
	public enum PopupShowPolicy
	{
		None,

		[Tooltip("Открывается поверх текущего")]
		Force,

		[Tooltip("Открывается вне очереди")]
		Standalone
	}

	/// <summary>
	/// Для управления используйте <see cref="UIPopupDispatcher"/>
	/// </summary>
	public partial class UIPopupManager : IDisposable
	{
		private IPopup _current;

		private readonly PopupPool _pool;

		private readonly UIRootWidgetQueue<IPopup, object> _queue;

		private Dictionary<IPopup, object> _standalonePopups;

		private readonly CancellationTokenSource _cts = new();

		internal event ShownDelegate Shown;
		internal event HiddenDelegate Hidden;
		internal event EnqueuedDelegate Enqueued;

		internal (IPopup, object) Current => (_current, _current?.GetArgs());

		internal IEnumerable<KeyValuePair<IPopup, object>> Queue => _queue;

		public UIPopupManager()
		{
			var factory = new UIPopupFactory();
			_pool = new PopupPool(factory);

			InitializeAssetsPreloader();

			_queue = new(false);
			_standalonePopups = new();
		}

		void IDisposable.Dispose()
		{
			_current?.Dispose();

			_pool.Dispose();

			DisposeAssetsPreloader();

			_queue.Dispose();

			_cts?.Trigger();

			foreach (var popup in _standalonePopups.Keys)
				popup.Dispose();
			_standalonePopups = null;
		}

		internal T Show<T>(object args, PopupShowPolicy policy = PopupShowPolicy.None)
			where T : UIWidget, IPopup
		{
			var popup = Get<T>();
			Show(popup, args, false, policy);
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

			foreach (var (popup, _) in _queue)
				if (popup.Id == id)
					return true;

			return false;
		}

		internal IEnumerable<UIWidget> GetAllActive()
		{
			if (_current is UIWidget castCurrent)
				yield return castCurrent;
		}

		internal void TryHide(IPopup popup)
		{
			if (_standalonePopups.Remove(popup))
			{
				PerformHide();
				return;
			}

			_queue.TryRemove(popup);

			//запускаем закрытие и открытие из очереди нового только в том случае если закрывается текущий активный попап
			if (_current != popup)
			{
				Release(popup);
				return;
			}

			PerformHide();
			_current = null;

			TryShowNext();

			void PerformHide()
			{
				TryReleasePreloadedLayout(popup);
				HideInternal(popup, false);

				//Нужно подождать пока отыграется анимация и только потом возвращать в пул
				WaitHideAndReleaseAsync(popup, _cts.Token)
					.Forget();
			}
		}

		private void Show(IPopup popup, object args, bool fromQueue, PopupShowPolicy policy = PopupShowPolicy.None)
		{
			if (policy == PopupShowPolicy.Standalone)
			{
				ShowInternal(popup, args, false);
				_standalonePopups.Add(popup, args);
				return;
			}

			if (_current != null)
			{
				if (policy == PopupShowPolicy.Force)
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

		private void Enqueue(IPopup popup, object args = null, bool addToLast = false)
		{
			args ??= popup.GetArgs();
			_queue.Enqueue(popup, args, addToLast);
			Enqueued?.Invoke(popup, args, addToLast);
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

		private void ShowInternal(IPopup popup, object args, bool fromQueue)
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

		public delegate void EnqueuedDelegate(IPopup popup, object args, bool addToLast);

		#endregion
	}
}
