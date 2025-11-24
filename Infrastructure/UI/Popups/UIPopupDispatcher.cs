using System;
using System.Collections.Generic;
using UI.Popups;

namespace UI
{
	public class UIPopupDispatcher : IWidgetDispatcher, IDisposable
	{
		private UIPopupManager _manager;

		/// <summary>
		/// Активация попапа. Разница между показом в том что показ вызывается даже когда окно ушло в очередь.
		/// А активация вызывается лишь когда окно полностью закрыли
		/// </summary>
		public event Action<IPopup, object> Activated;

		/// <summary>
		/// Деактивация попапа. Разница между показом в том что показ вызывается даже когда окно ушло в очередь.
		/// А деактивации вызывается лишь когда попап полностью закрыли
		/// </summary>
		public event Action<IPopup> Deactivated;

		/// <summary>
		/// Событие при показе попапа, важно отметить что окно может показаться из очереди!
		/// Если не нужна реакция на показ попапа из очереди используйте <see cref="Activated"/>
		/// </summary>
		public event Action<IPopup> Shown;

		/// <summary>
		/// Событие по скрытию попапа, важно отметить что окно может скрыться в очередь!
		/// Если не нужна реакция на показ попапа из очереди используйте <see cref="Deactivated"/>
		/// </summary>
		public event Action<IPopup> Hidden;

		public IEnumerable<KeyValuePair<IPopup, object>> Queue => _manager.Queue;
		public IEnumerable<KeyValuePair<IPopup, object>> Standalones => _manager.Standalones;

		public (IPopup popup, object args) Current => _manager.Current;

		public UIPopupDispatcher(UIPopupManager manager)
		{
			_manager = manager;

			_manager.Shown += OnShown;
			_manager.Hidden += OnHidden;
			_manager.Enqueued += OnEnqueued;
		}

		public void Dispose()
		{
			_manager.Shown -= OnShown;
			_manager.Hidden -= OnHidden;
			_manager.Enqueued -= OnEnqueued;

			_manager = null;
		}

		/// <summary>
		/// Проверяет находится попап в очереди или по текущему попапу
		/// </summary>
		public bool IsActive<T>(T popup)
			where T : UIWidget, IPopup
			=> _manager.IsActive<T>(popup);

		public bool IsActive(string id)
			=> _manager.IsActive(id);

		/// <summary>
		/// Показать попап по типу (убирает в очередь текущее)
		/// Внимательно смотреть за передаваемыми аргументами,
		/// если реализация попапа активно использует их, то нельзя активно передавать null как у окно,
		/// возможны ошибки
		/// </summary>
		/// <param name="force">Убрать текущий попап в очередь (возможно понадобится priority вместо force, но пока так)</param>
		public T Show<T>(object args = null, PopupMode? overrideMode = null)
			where T : UIWidget, IPopup
			=> _manager.Show<T>(args, overrideMode);

		public void TryHide(IPopup popup)
			=> _manager.TryHide(popup);

		/// <summary>
		/// Попробовать закрыть текущий попап
		/// </summary>
		/// <returns>Получилось ли закрыть?</returns>
		public bool TryHideCurrent() => _manager.TryHideCurrent();

		private void OnEnqueued(IPopup popup, object args, bool addToLast)
		{
			if (!addToLast)
				Activated?.Invoke(popup, args);
		}

		private void OnShown(IPopup popup, bool fromQueue)
		{
			Shown?.Invoke(popup);

			if (!fromQueue)
				Activated?.Invoke(popup, popup.GetArgs());
		}

		private void OnHidden(IPopup popup, bool fromQueue)
		{
			Hidden?.Invoke(popup);

			if (!fromQueue)
				Deactivated?.Invoke(popup);
		}

		IEnumerable<UIWidget> IWidgetDispatcher.GetAllActive() => _manager.GetAllActive();
	}
}
