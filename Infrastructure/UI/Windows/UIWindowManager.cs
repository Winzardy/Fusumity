using System;
using System.Collections.Generic;

namespace UI.Windows
{
	/// <summary>
	/// Для управления используйте <see cref="UIWindowDispatcher"/>
	/// </summary>
	public partial class UIWindowManager : IDisposable
	{
		private Dictionary<Type, IWindow> _windows = new(8);
		private IWindow _current;

		private readonly UIWindowFactory _factory;

		private readonly UIRootWidgetQueue<IWindow, IWindowArgs> _queue;

		internal IWindow Current => _current;

		internal event ShownDelegate Shown;
		internal event HiddenDelegate Hidden;

		public UIWindowManager()
		{
			_factory = new();

			InitializeAssetsPreloader();

			_queue = new();
		}

		void IDisposable.Dispose()
		{
			ClearAll();

			_queue.Dispose();

			DisposeAssetsPreloader();

			_windows = null;
		}

		internal T Show<T>(IWindowArgs args)
			where T : UIWidget, IWindow
		{
			if (!TryGet<T>(out var window))
				window = Create<T>();

			Show(window, args);
			return window;
		}

		internal bool TryHideCurrent()
		{
			if (_current == null)
				return false;

			TryHide(_current);
			return true;
		}

		internal void HideAll()
		{
			foreach (var window in _windows.Values)
			{
				TryHide(window);
			}
		}

		internal bool TryHide<T>()
			where T : UIWidget, IWindow
		{
			if (TryGet<T>(out var window))
			{
				TryHide(window);
				return true;
			}

			return false;
		}

		internal void ClearAll()
		{
			foreach (var window in _windows.Values)
			{
				Clear(window, false);
			}

			_windows.Clear();
		}

		internal void Clear(IWindow window)
		{
			Clear(window, true);
		}

		internal bool IsActive<T>() where T : UIWidget, IWindow
		{
			if (TryGet<T>(out var window))
			{
				if (_current == window && _current.Active)
					return true;

				if (_queue.Contains(window))
					return true;
			}

			return false;
		}

		internal bool IsActive(string id)
		{
			if (_current?.Id == id)
				return true;

			foreach (var window in _queue)
				if (window.Id == id)
					return true;

			return false;
		}

		internal IEnumerable<UIWidget> GetAllActive()
		{
			if (_current is UIWidget castCurrent)
				yield return castCurrent;
		}

		private T Create<T>()
			where T : UIWidget, IWindow
		{
			var window = _factory.Create<T>();
			Register(window);
			return window;
		}

		private void Register<T>(T window)
			where T : class, IWindow
		{
			_windows[typeof(T)] = window;

			window.RequestedClose += OnRequestedClose;
		}

		private void Clear(IWindow window, bool full)
		{
			window.RequestedClose -= OnRequestedClose;
			window.Dispose();

			if (full)
				_windows.Remove(window.GetType());
		}

		private bool TryGet<T>(out T window)
			where T : UIWidget, IWindow
		{
			window = null;

			if (_windows != null && _windows.TryGetValue(typeof(T), out var value))
			{
				window = value as T;
				return true;
			}

			return false;
		}

		private void OnRequestedClose(IWindow window) => TryHide(window);

		private void Show(IWindow window, IWindowArgs args, bool fromQueue = false)
		{
			window.Show(args);

			if (_current != window)
				TryHideAndAddToQueue(_current);

			_current = window;

			Shown?.Invoke(window, fromQueue);
		}

		private void TryHide(IWindow window, bool fromQueue = false)
		{
			_queue.TryRemove(window);

			//запускаем закрытие и открытие из очереди нового только в том случае если закрывается текущее активно окно
			if (_current != window)
			{
				Hidden?.Invoke(window, false);
				return;
			}

			TryReleasePreloadedLayout(window);

			Hide(window, fromQueue);
			TryShowNext();
		}

		private void TryShowNext()
		{
			if (_queue.IsEmpty())
				return;

			var (window, args) = _queue.Dequeue();
			Show(window, args, true);
		}

		private void TryHideAndAddToQueue(IWindow window)
		{
			if (window == null)
				return;

			if (!window.Active)
				return;

			var args = window.GetArgs();
			_queue.Enqueue(window, args);

			//Аргументы очищаются при Hide, поэтому сначала GetArgs, потом Hide
			Hide(window, true);
		}

		private void Hide(IWindow window, bool fromQueue = false)
		{
			window.Hide(!fromQueue);

			Hidden?.Invoke(window, fromQueue);
		}

		#region Delegates

		public delegate void ShownDelegate(IWindow window, bool fromQueue);

		public delegate void HiddenDelegate(IWindow window, bool fromQueue);

		#endregion
	}
}
