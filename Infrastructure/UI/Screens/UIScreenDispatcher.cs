using System;
using System.Collections.Generic;

namespace UI.Screens
{
	public class UIScreenDispatcher : IWidgetDispatcher, IDisposable
	{
		private UIScreenManager _manager;

		/// <summary>
		/// Активация окна, даже если окно в очереди
		/// </summary>
		public event Action<IScreen> Activated;

		/// <summary>
		/// Деактивация окна, даже если окно в очереди
		/// </summary>
		public event Action<IScreen> Deactivated;

		/// <summary>
		/// Событие при показе окна, важно отметить что окно может показаться из очереди!
		/// Если не нужна реакция на показ окна из очереди используйте <see cref="Activated"/>
		/// </summary>
		public event Action<IScreen> Shown;

		/// <summary>
		/// Событие по скрытию окна, важно отметить что окно может скрыться в очередь!
		/// Если не нужна реакция на показ окна из очереди используйте <see cref="Deactivated"/>
		/// </summary>
		public event Action<IScreen> Hidden;

		public (IScreen screen, object args) Current => _manager.Current;
		public (IScreen screen, object args) Default => _manager.Default;
		public IEnumerable<KeyValuePair<IScreen, object>> Queue => _manager.Queue;

		public UIScreenDispatcher(UIScreenManager manager)
		{
			_manager = manager;

			_manager.Shown += OnShown;
			_manager.Hidden += OnHidden;
		}

		public UIScreenDispatcher()
		{
		}

		public void Dispose()
		{
			_manager.Shown -= OnShown;
			_manager.Hidden -= OnHidden;

			_manager = null;
		}

		private void OnShown(IScreen window, bool fromQueue)
		{
			Shown?.Invoke(window);

			if (!fromQueue)
				Activated?.Invoke(window);
		}

		private void OnHidden(IScreen window, bool fromQueue)
		{
			Hidden?.Invoke(window);

			if (!fromQueue)
				Deactivated?.Invoke(window);
		}

		public IDisposable Prepare<T>(Action callback = null)
			where T : UIWidget, IScreen
			=> _manager.Prepare<T>(callback);

		/// <summary>
		/// Проверяет находится окно в очереди или текущее окно такого типа
		/// </summary>
		public bool IsActive<T>()
			where T : UIWidget, IScreen
			=> _manager.IsActive<T>();

		public bool IsActive(string id) => _manager.IsActive(id);

		/// <summary>
		/// Это текущий экран?
		/// </summary>
		public bool IsCurrent<T>()
			where T : UIWidget, IScreen
			=> _manager.IsCurrent<T>();

		/// <summary>
		/// Это текущий экран?
		/// </summary>
		public bool IsCurrent(string id)
			=> _manager.IsCurrent(id);

		/// <summary>
		/// Это экран дефолтный?
		/// </summary>
		public bool IsDefault<T>()
			where T : UIWidget, IScreen
			=> _manager.IsDefault<T>();

		/// <summary>
		/// Показать экран по типу (убирает в очередь текущее)
		/// </summary>
		public T Show<T>(object args = null)
			where T : UIWidget, IScreen
			=> _manager.Show<T>(args);

		/// <summary>
		/// Закрыть окно по типу, если оно открыто или убрать из очереди
		/// </summary>
		public bool TryHide<T>()
			where T : UIWidget, IScreen
			=> _manager.TryHide<T>();

		public void TryHide(IScreen screen)
			=> _manager.TryHide(screen);

		public void TryHideAll() => _manager.TryHideAll();

		public T Get<T>()
			where T : UIWidget, IScreen =>
			_manager.Get<T>();

		public bool TryGet<T>(out T screen)
			where T : UIWidget, IScreen =>
			_manager.TryGet(out screen);

		/// <summary>
		/// Попробовать закрыть текущее окно
		/// </summary>
		/// <returns>Получилось ли закрыть?</returns>
		public bool TryHideCurrent() => _manager.TryHideCurrent();

		IEnumerable<UIWidget> IWidgetDispatcher.GetAllActive() => _manager.GetAllActive();
		void IWidgetDispatcher.ClearAll() => _manager.ClearAll();
	}
}
