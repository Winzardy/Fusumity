using System;
using System.Collections.Generic;
using Fusumity.Utility;

// /// <summary>
// /// Для управления используйте <see cref="UIScreenDispatcher"/>
// /// </summary>
// public partial class UIScreenManager : IInitializable, IDisposable
// {
// 	private IScreen _default;
// 	private IScreen _current;
//
// 	private Dictionary<Type, IScreen> _screens = new(2);
//
// 	private readonly HashSet<object> _blockers = new(2);
//
// 	private readonly UIScreenFactory _factory = new();
//
// 	internal IScreen Current => _current;
// 	internal IScreen Default => _default;
//
// 	internal event Action<IScreen> Shown;
// 	internal event Action<IScreen> Hidden;
//
// 	void IInitializable.Initialize()
// 	{
// 		var types = ReflectionUtility.GetAllTypes<IScreen>();
// 		foreach (var type in types)
// 		{
// 			if (type.TryGetAttribute<DefaultScreenAttribute>(out var attribute))
// 			{
// 				var screen = Create(type);
//
// 				if (_default != null)
// 				{
// 					GUIDebug.LogError($"More than one default screen [ {type.Name}");
// 					continue;
// 				}
//
// 				SetDefault(screen, attribute.autoShow);
// 			}
// 		}
//
// 		if (_default == null)
// 		{
// 			GUIDebug.LogWarning($"No default screen");
// 		}
//
// 		InitializeAssetsPreloader();
// 	}
//
// 	void IDisposable.Dispose()
// 	{
// 		foreach (var screen in _screens.Values)
// 			screen.Dispose();
//
// 		_screens = null;
//
// 		DisposeAssetsPreloader();
// 	}
//
// 	internal void SetDefault<T>()
// 		where T : UIWidget, IScreen
// 	{
// 		var screen = Get<T>();
// 		SetDefault(screen);
// 	}
//
// 	internal T Get<T>()
// 		where T : UIWidget, IScreen
// 	{
// 		if (!TryGet<T>(out var screen, true))
// 			throw new Exception($"Error on getting screen by type [ {typeof(T)} ]");
//
// 		return screen;
// 	}
//
// 	internal void TryShowDefault(bool checkCurrent = true)
// 	{
// 		if (checkCurrent && _current != null)
// 			return;
//
// 		if (_default == null)
// 			return;
//
// 		SetCurrent(_default);
// 	}
//
// 	internal T Show<T>()
// 		where T : UIWidget, IScreen
// 	{
// 		var screen = Get<T>();
// 		SetCurrent(screen);
// 		return screen;
// 	}
//
// 	internal bool TryGet<T>(out T screen)
// 		where T : UIWidget, IScreen =>
// 		TryGet(out screen, false);
//
// 	internal void Hide()
// 	{
// 		if (_current == null)
// 			return;
//
// 		TryHide(_current);
// 	}
//
// 	internal bool IsActive<T>() where T : UIWidget, IScreen
// 		=> IsCurrent<T>() || IsDefault<T>();
//
// 	internal bool IsCurrent<T>() where T : UIWidget, IScreen
// 		=> _current.GetType() == typeof(T);
//
// 	internal bool IsCurrent(string id)
// 		=> _current.Id == id;
//
// 	internal bool IsDefault<T>() where T : UIWidget, IScreen
// 		=> _default.GetType() == typeof(T);
//
// 	internal IDisposable Prepare<T>(Action callback) where T : UIWidget, IScreen
// 		=> Get<T>().Prepare(callback);
//
// 	internal IEnumerable<UIWidget> GetAllActive()
// 	{
// 		if (_current is UIWidget castCurrent)
// 			yield return castCurrent;
// 	}
//
// 	private void SetCurrent(IScreen screen)
// 	{
// 		if (_current == screen)
// 			return;
//
// 		if (_current != null)
// 			TryHide(_current, false);
//
// 		_current = screen;
// 		TryShow(screen);
// 	}
//
// 	private void SetDefault(IScreen screen, bool autoShow = true)
// 	{
// 		if (_default == screen)
// 			return;
//
// 		if (_current != null && _current == _default)
// 			TryHide(_default, false);
//
// 		_default = screen;
//
// 		if (autoShow)
// 			TryShowDefault();
// 	}
//
// 	private void TryHide(IScreen screen, bool checkDefault = true, bool clearCurrent = true)
// 	{
// 		if (screen == null)
// 			return;
//
// 		if (checkDefault && _default == screen)
// 			return;
//
// 		if (_current != screen)
// 			return;
//
// 		TryReleasePreloadedLayout(screen);
//
// 		HideScreenInternal(screen);
//
// 		if (clearCurrent)
// 			_current = null;
//
// 		if (checkDefault)
// 			TryShowDefault();
// 	}
//
// 	private void HideScreenInternal(IScreen screen)
// 	{
// 		screen.Hide(true);
// 		Hidden?.Invoke(screen);
// 	}
//
// 	private IScreen Create(Type type)
// 	{
// 		var screen = _factory.Create(type);
// 		Register(type, screen);
// 		return screen;
// 	}
//
// 	private void Register(Type type, IScreen screen)
// 	{
// 		_screens[type] = screen;
// 	}
//
// 	internal bool AddShowBlocker(object blocker)
// 	{
// 		if (_blockers.Add(blocker))
// 		{
// 			TryHide(_current, false, false);
// 			return true;
// 		}
//
// 		return false;
// 	}
//
// 	internal void RemoveShowBlocker(object blocker)
// 	{
// 		if (_blockers.Remove(blocker))
// 		{
// 			if (_blockers.Count > 0)
// 				return;
//
// 			TryShowCurrent();
// 		}
// 	}
//
// 	private void TryShowCurrent()
// 	{
// 		if (_current == null)
// 			return;
//
// 		TryShow(_current);
// 	}
//
// 	private void TryShow(IScreen screen)
// 	{
// 		if (_blockers.Count > 0)
// 		{
// 			GUIDebug.LogWarning($"Block show screen (blockers ({_blockers.Count}):\n" +
// 				$"{_blockers.GetCompositeString(getter: (x) => x.GetType().Name.ToString())})");
//
// 			return;
// 		}
//
// 		screen?.Show();
// 		Shown?.Invoke(screen);
// 	}
//
// 	private bool TryGet<T>(out T screen, bool create)
// 		where T : UIWidget, IScreen
// 	{
// 		screen = default;
//
// 		if (_screens.TryGetValue(typeof(T), out var value))
// 		{
// 			screen = value as T;
// 			return true;
// 		}
//
// 		if (create)
// 		{
// 			screen = _factory.Create<T>();
// 			Register(typeof(T), screen);
// 		}
//
// 		return screen != null;
// 	}
// }

namespace UI.Screens
{
	/// <summary>
	/// Для управления используйте <see cref="UIScreenDispatcher"/>
	/// </summary>
	public partial class UIScreenManager : IInitializable, IDisposable
	{
		private Dictionary<Type, IScreen> _screens = new(8);
		private IScreen _current;
		private (IScreen screen, IScreenArgs args) _default;

		private readonly UIScreenFactory _factory;

		private readonly UIRootWidgetQueue<IScreen, IScreenArgs> _queue;

		internal IScreen Current => _current;
		internal IScreen Default => _default.screen;

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

		internal T Show<T>(IScreenArgs args)
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

			foreach (var screen in _queue)
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

		private void Show(IScreen screen, IScreenArgs args, bool fromQueue = false)
		{
			screen.Show(args);

			if (_current != screen)
				TryHideAndAddToQueue(_current);

			_current = screen;

			Shown?.Invoke(screen, fromQueue);
		}

		private void TryHide(IScreen screen, bool fromQueue = false)
		{
			_queue.TryRemove(screen);

			//запускаем закрытие и открытие из очереди нового только в том случае если закрывается текущее активно окно
			if (_current != screen)
			{
				Hidden?.Invoke(screen, false);
				return;
			}

			TryReleasePreloadedLayout(screen);

			Hide(screen, fromQueue);
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
			_current = null;

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

		#region Delegates

		public delegate void ShownDelegate(IScreen screen, bool fromQueue);

		public delegate void HiddenDelegate(IScreen screen, bool fromQueue);

		#endregion
	}
}
