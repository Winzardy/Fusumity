using System;
using System.Collections;
using System.Collections.Generic;

namespace UI.Windows
{
	public class UIWindowDispatcher : IWidgetDispatcher, IDisposable
	{
		private UIWindowManager _manager;

		/// <summary>
		/// Активация окна, даже если окно в очереди
		/// </summary>
		public event Action<IWindow> Activated;

		/// <summary>
		/// Деактивация окна, даже если окно в очереди
		/// </summary>
		public event Action<IWindow> Deactivated;

		/// <summary>
		/// Событие при показе окна, важно отметить что окно может показаться из очереди!
		/// Если не нужна реакция на показ окна из очереди используйте <see cref="Activated"/>
		/// </summary>
		public event Action<IWindow> Shown;

		/// <summary>
		/// Событие по скрытию окна, важно отметить что окно может скрыться в очередь!
		/// Если не нужна реакция на показ окна из очереди используйте <see cref="Deactivated"/>
		/// </summary>
		public event Action<IWindow> Hidden;

		public (IWindow window, object args) Current => _manager.Current;
		public IEnumerable<KeyValuePair<IWindow, object>> Queue => _manager.Queue;

		public UIWindowDispatcher(UIWindowManager manager)
		{
			_manager = manager;

			_manager.Shown += OnShown;
			_manager.Hidden += OnHidden;
		}

		public void Dispose()
		{
			_manager.Shown -= OnShown;
			_manager.Hidden -= OnHidden;

			_manager = null;
		}

		private void OnShown(IWindow window, bool fromQueue)
		{
			Shown?.Invoke(window);

			if (!fromQueue)
				Activated?.Invoke(window);
		}

		private void OnHidden(IWindow window, bool fromQueue)
		{
			Hidden?.Invoke(window);

			if (!fromQueue)
				Deactivated?.Invoke(window);
		}

		/// <summary>
		/// Проверяет находится окно в очереди или текущее окно такого типа
		/// </summary>
		public bool IsActive<T>()
			where T : UIWidget, IWindow
			=> _manager.IsActive<T>();

		public bool IsActive(string id) => _manager.IsActive(id);

		/// <summary>
		/// Показать экран по типу (убирает в очередь текущее)
		/// </summary>
		public T Show<T>(object args = null)
			where T : UIWidget, IWindow
			=> _manager.Show<T>(args);

		/// <summary>
		/// Закрыть окно по типу, если оно открыто или убрать из очереди
		/// </summary>
		public bool TryHide<T>()
			where T : UIWidget, IWindow
			=> _manager.TryHide<T>();

		public void TryHide(IWindow window)
			=> _manager.TryHide(window);

		/// <summary>
		/// Попробовать закрыть текущее окно
		/// </summary>
		/// <returns>Получилось ли закрыть?</returns>
		public bool TryHideCurrent() => _manager.TryHideCurrent();

		IEnumerable<UIWidget> IWidgetDispatcher.GetAllActive() => _manager.GetAllActive();
	}
}
