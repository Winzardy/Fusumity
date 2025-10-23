using System;
using System.Collections.Generic;
using Fusumity.Utility;

namespace UI.Screens
{
	/// <summary>
	/// Для управления используйте <see cref="UIScreenDispatcher"/>
	/// </summary>
	public partial class UIScreenManager : IInitializable, IDisposable
	{
		private Dictionary<Type, IScreen> _screens = new(8);
		private IScreen _current;
		private (IScreen screen, object args) _default;

		private readonly UIScreenFactory _factory;

		private readonly UIRootWidgetQueue<IScreen, object> _queue;

		internal (IScreen, object) Current => (_current, _current?.GetArgs());
		internal (IScreen, object) Default => _default;
		internal IEnumerable<KeyValuePair<IScreen, object>> Queue => _queue;

		internal event ShownDelegate Shown;
		internal event HiddenDelegate Hidden;

		public UIScreenManager()
		{
			_factory = new();

			_queue = new();
		}

		void IInitializable.Initialize()
		{
			var types = ReflectionUtility.GetAllTypes<IScreen>();
			foreach (var type in types)
			{
				if (type.TryGetAttribute<DefaultScreenAttribute>(out var attribute))
				{
					var screen = Create(type);

					if (_default.screen != null)
					{
						GUIDebug.LogError($"More than one default screen [ {type.Name}");
						continue;
					}

					SetDefault(screen, attribute.autoShow);
				}
			}

			if (_default.screen == null)
			{
				GUIDebug.LogWarning($"No default screen");
			}

			InitializeAssetsPreloader();
		}

		void IDisposable.Dispose()
		{
			ClearAll();

			_queue.Dispose();

			DisposeAssetsPreloader();

			_screens = null;
		}

		internal T Get<T>()
			where T : UIWidget, IScreen
		{
			if (!TryGetOrCreate<T>(out var screen))
				throw new Exception($"Error on getting screen by type [ {typeof(T)} ]");

			return screen;
		}

		internal T Show<T>(object args)
			where T : UIWidget, IScreen
		{
			if (!TryGet<T>(out var screen))
				screen = Create<T>();

			Show(screen, args);
			return screen;
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
			foreach (var screen in _screens.Values)
			{
				TryHide(screen);
			}
		}

		internal bool TryHide<T>()
			where T : UIWidget, IScreen
		{
			if (TryGet<T>(out var screen))
			{
				TryHide(screen);
				return true;
			}

			return false;
		}

		internal void TryHide(IScreen screen)
		{
			TryHide(screen, false);
		}

		internal IDisposable Prepare<T>(Action callback) where T : UIWidget, IScreen
			=> Get<T>().Prepare(callback);

		internal void ClearAll()
		{
			foreach (var screen in _screens.Values)
			{
				Clear(screen, false);
			}

			_screens.Clear();
		}

		internal void Clear(IScreen screen)
		{
			Clear(screen, true);
		}

		internal bool IsActive<T>() where T : UIWidget, IScreen
		{
			if (TryGet<T>(out var screen))
			{
				if (_current == screen && _current.Active)
					return true;

				if (_queue.Contains(screen))
					return true;
			}

			return false;
		}

		internal bool IsActive(string id)
		{
			if (_current?.Id == id)
				return true;

			foreach (var (screen, args) in _queue)
				if (screen.Id == id)
					return true;

			return false;
		}

		internal bool IsCurrent<T>() where T : UIWidget, IScreen
			=> _current.GetType() == typeof(T);

		internal bool IsCurrent(string id)
			=> _current.Id == id;

		internal bool IsDefault<T>() where T : UIWidget, IScreen
			=> _default.GetType() == typeof(T);

		internal IEnumerable<UIWidget> GetAllActive()
		{
			if (_current is UIWidget widget)
				yield return widget;
		}

		private T Create<T>()
			where T : UIWidget, IScreen
		{
			var screen = _factory.Create<T>();
			Register(screen);
			return screen;
		}

		private void Register(IScreen screen)
		{
			_screens[screen.GetType()] = screen;

			screen.RequestedClose += OnRequestedClose;
		}

		private void Clear(IScreen screen, bool full)
		{
			screen.RequestedClose -= OnRequestedClose;
			screen.Dispose();

			if (full)
				_screens.Remove(screen.GetType());
		}

		private bool TryGet<T>(out T screen)
			where T : UIWidget, IScreen
		{
			screen = null;

			if (_screens != null && _screens.TryGetValue(typeof(T), out var value))
			{
				screen = value as T;
				return true;
			}

			return false;
		}

		private bool TryGetOrCreate<T>(out T screen)
			where T : UIWidget, IScreen
		{
			if (TryGet(out screen))
				return true;

			screen = _factory.Create<T>();
			Register(screen);

			return screen != null;
		}

		private void SetDefault(IScreen screen, bool autoShow = true)
		{
			if (_default.screen == screen)
				return;

			if (_current != null && _current == _default.screen)
				TryHide(_default.screen, false);

			_default.screen = screen;
			_default.args = screen.GetArgs();

			if (autoShow)
				Show(_default.screen, _default.args, false);
		}

		private void OnRequestedClose(IScreen screen) => TryHide(screen);

		private void Show(IScreen screen, object args, bool fromQueue = false)
		{
			screen.Show(args);

			if (_current != screen)
				TryHideAndAddToQueue(_current);

			SetCurrent(screen);

			Shown?.Invoke(screen, fromQueue);
		}

		private void TryHide(IScreen screen, bool fromQueue)
		{
			_queue.TryRemove(screen);

			//запускаем закрытие и открытие из очереди нового только в том случае если закрывается текущее активно окно
			if (_current != screen)
			{
				Hidden?.Invoke(screen, false);
				return;
			}

			if (_default.screen == screen && _queue.IsEmpty())
			{
				GUIDebug.LogWarning("Can't hide default screen");
				return;
			}

			TryReleasePreloadedLayout(screen);

			Hide(screen, fromQueue);
			SetCurrent(null);

			TryShowNext();
		}

		private void TryShowNext()
		{
			if (_queue.IsEmpty())
			{
				if (_default.screen != null)
					Show(_default.screen, _default.args);
				return;
			}

			var (screen, args) = _queue.Dequeue();
			Show(screen, args, true);
		}

		public void TryHideAll()
		{
			_queue.Clear();

			_current?.Hide(false);
			SetCurrent(null);

			if (_default.screen != null)
				Show(_default.screen, _default.args);
		}

		private void TryHideAndAddToQueue(IScreen screen)
		{
			if (screen == null)
				return;

			if (!screen.Active)
				return;

			var args = screen.GetArgs();
			_queue.Enqueue(screen, args);

			//Аргументы очищаются при Hide, поэтому сначала GetArgs, потом Hide
			Hide(screen, true);
		}

		private void Hide(IScreen screen, bool fromQueue = false)
		{
			screen.Hide(!fromQueue);
			Hidden?.Invoke(screen, fromQueue);
		}

		private IScreen Create(Type type)
		{
			var screen = _factory.Create(type);
			Register(screen);
			return screen;
		}

		private void SetCurrent(IScreen screen)
		{
			_current = screen;
		}

		#region Delegates

		public delegate void ShownDelegate(IScreen screen, bool fromQueue);

		public delegate void HiddenDelegate(IScreen screen, bool fromQueue);

		#endregion
	}
}
