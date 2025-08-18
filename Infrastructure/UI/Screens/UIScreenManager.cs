using System;
using System.Collections.Generic;
using Fusumity.Utility;
using Sapientia.Extensions;

namespace UI.Screens
{
	/// <summary>
	/// Для управления используйте <see cref="UIScreenDispatcher"/>
	/// </summary>
	public partial class UIScreenManager : IInitializable, IDisposable
	{
		private IScreen _default;
		private IScreen _current;

		private Dictionary<Type, IScreen> _screens = new(2);

		private readonly HashSet<object> _blockers = new(2);

		private readonly UIScreenFactory _factory = new();

		internal IScreen Current => _current;
		internal IScreen Default => _default;

		internal event Action<IScreen> Shown;
		internal event Action<IScreen> Hidden;

		void IInitializable.Initialize()
		{
			var types = ReflectionUtility.GetAllTypes<IScreen>();
			foreach (var type in types)
			{
				if (type.TryGetAttribute<DefaultScreenAttribute>(out var attribute))
				{
					var screen = Create(type);

					if (_default != null)
					{
						GUIDebug.LogError($"More than one default screen [ {type.Name}");
						continue;
					}

					SetDefault(screen, attribute.autoShow);
				}
			}

			if (_default == null)
			{
				GUIDebug.LogWarning($"No default screen");
			}

			InitializeAssetsPreloader();
		}

		void IDisposable.Dispose()
		{
			foreach (var screen in _screens.Values)
				screen.Dispose();

			_screens = null;

			DisposeAssetsPreloader();
		}

		internal void SetDefault<T>()
			where T : UIWidget, IScreen
		{
			var screen = Get<T>();
			SetDefault(screen);
		}

		internal T Get<T>()
			where T : UIWidget, IScreen
		{
			if (!TryGet<T>(out var screen, true))
				throw new Exception($"Error on getting screen by type [ {typeof(T)} ]");

			return screen;
		}

		internal void TryShowDefault(bool checkCurrent = true)
		{
			if (checkCurrent && _current != null)
				return;

			if (_default == null)
				return;

			SetCurrent(_default);
		}

		internal T Show<T>()
			where T : UIWidget, IScreen
		{
			var screen = Get<T>();
			SetCurrent(screen);
			return screen;
		}

		internal bool TryGet<T>(out T screen)
			where T : UIWidget, IScreen =>
			TryGet(out screen, false);

		internal void Hide()
		{
			if (_current == null)
				return;

			TryHide(_current);
		}

		internal bool IsActive<T>() where T : UIWidget, IScreen
			=> IsCurrent<T>() || IsDefault<T>();

		internal bool IsCurrent<T>() where T : UIWidget, IScreen
			=> _current.GetType() == typeof(T);

		internal bool IsCurrent(string id)
			=> _current.Id == id;

		internal bool IsDefault<T>() where T : UIWidget, IScreen
			=> _default.GetType() == typeof(T);

		internal IDisposable Prepare<T>(Action callback) where T : UIWidget, IScreen
			=> Get<T>().Prepare(callback);

		private void SetCurrent(IScreen screen)
		{
			if (_current == screen)
				return;

			if (_current != null)
				TryHide(_current, false);

			_current = screen;
			TryShow(screen);
		}

		private void SetDefault(IScreen screen, bool autoShow = true)
		{
			if (_default == screen)
				return;

			if (_current != null && _current == _default)
				TryHide(_default, false);

			_default = screen;

			if (autoShow)
				TryShowDefault();
		}

		private void TryHide(IScreen screen, bool checkDefault = true, bool clearCurrent = true)
		{
			if (screen == null)
				return;

			if (checkDefault && _default == screen)
				return;

			if (_current != screen)
				return;

			TryReleasePreloadedLayout(screen);

			HideScreenInternal(screen);

			if (clearCurrent)
				_current = null;

			if (checkDefault)
				TryShowDefault();
		}

		private void HideScreenInternal(IScreen screen)
		{
			screen.Hide(true);
			Hidden?.Invoke(screen);
		}

		private IScreen Create(Type type)
		{
			var screen = _factory.Create(type);
			Register(type, screen);
			return screen;
		}

		private void Register(Type type, IScreen screen)
		{
			_screens[type] = screen;
		}

		internal bool AddShowBlocker(object blocker)
		{
			if (_blockers.Add(blocker))
			{
				TryHide(_current, false, false);
				return true;
			}

			return false;
		}

		internal void RemoveShowBlocker(object blocker)
		{
			if (_blockers.Remove(blocker))
			{
				if (_blockers.Count > 0)
					return;

				TryShowCurrent();
			}
		}

		private void TryShowCurrent()
		{
			if (_current == null)
				return;

			TryShow(_current);
		}

		private void TryShow(IScreen screen)
		{
			if (_blockers.Count > 0)
			{
				GUIDebug.LogWarning($"Block show screen (blockers ({_blockers.Count}):\n" +
					$"{_blockers.GetCompositeString(getter: (x) => x.GetType().Name.ToString())})");

				return;
			}

			screen?.Show();
			Shown?.Invoke(screen);
		}

		private bool TryGet<T>(out T screen, bool create)
			where T : UIWidget, IScreen
		{
			screen = default;

			if (_screens.TryGetValue(typeof(T), out var value))
			{
				screen = value as T;
				return true;
			}

			if (create)
			{
				screen = _factory.Create<T>();
				Register(typeof(T), screen);
			}

			return screen != null;
		}
	}
}
