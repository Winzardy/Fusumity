using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace UI.Windows
{
	public enum WindowMode
	{
		[Tooltip("Обычный режим: текущее окно закрывается и помещается в очередь перед открытием следующего")]
		Default,

		[Tooltip("Overlay-режим: окно открывается поверх текущего, не закрывая его, и не закрывается следующими окнами")]
		Overlay
	}

	public struct WindowQueueContext
	{
		public IWindow window;

		/// <summary>
		/// Было ли окно перекрыто другим окном (overlay),
		/// и поэтому не закрыто при вытеснении
		/// </summary>
		public bool overlaid;

		public WindowMode? mode;
		public object args;

		internal void ActualArgs()
		{
			args = window.GetArgs();
		}
	}

	// TODO: нужно переделать позже
	// - убрать переиспользуемость окна в очереди
	// - убрать авто дестрой верстки
	/// <summary>
	/// Для управления используйте <see cref="UIWindowDispatcher"/>
	/// </summary>
	public partial class UIWindowManager : IInitializable, IDisposable
	{
		private Dictionary<Type, IWindow> _windows = new(8);

		private WindowQueueContext _current;

		private readonly UIWindowFactory _factory;

		private readonly UIRootWidgetQueue<IWindow, WindowQueueContext> _queue;

		internal ref readonly WindowQueueContext Current { get => ref _current; }

		internal IEnumerable<KeyValuePair<IWindow, WindowQueueContext>> Queue => _queue;

		internal event ShownDelegate Shown;
		internal event HiddenDelegate Hidden;

		public UIWindowManager()
		{
			_factory = new();

			_queue = new();
		}

		void IInitializable.Initialize()
		{
			InitializeAssetsPreloader();
		}

		void IDisposable.Dispose()
		{
			ClearAll();

			_queue.Dispose();

			DisposeAssetsPreloader();

			_windows = null;
		}

		internal T Show<T>(object args, WindowMode mode = WindowMode.Default)
			where T : UIWidget, IWindow
		{
			if (!TryGet(out T window))
				window = Create<T>();

			Show(window, new WindowQueueContext
			{
				args = args,
				mode = mode
			});
			return window;
		}

		internal bool TryHideCurrent()
		{
			if (_current.window == null)
				return false;

			TryHide(_current.window);
			return true;
		}

		internal void HideAll() => TryHideAll(true);

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

		internal void TryHide(IWindow window)
		{
			TryHide(window, false);
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
				if (_current.window == window && _current.window.Active)
					return true;

				if (_queue.Contains(window))
					return true;
			}

			return false;
		}

		internal bool IsActive(string id)
		{
			if (_current.window?.Id == id && _current.window!.IsActive())
				return true;

			foreach (var (window, _) in _queue)
				if (window.Id == id)
					return true;

			return false;
		}

		internal IEnumerable<UIWidget> GetAllActive()
		{
			if (_current.window is UIWidget widget)
				yield return widget;
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

		private void Show([NotNull] IWindow window, in WindowQueueContext context, bool fromQueue = false)
		{
			var immediate = false;

			if (fromQueue)
				immediate = context.overlaid;

			window.Show(context.args, immediate);

			if (_current.window != window)
			{
				_current.overlaid = context.mode!.Value == WindowMode.Overlay;
				TryAddToQueueAndHide(ref _current);
			}

			SetCurrent(window, context.mode);
			Shown?.Invoke(window, fromQueue);
		}

		private void TryHide(IWindow window, bool fromQueue)
		{
			_queue.TryRemove(window);

			// Запускаем закрытие и открытие из очереди нового только в том случае если закрывается текущее активно окно
			if (_current.window != window)
			{
				HideInternal(window, fromQueue);
				return;
			}

			TryReleasePreloadedLayout(window);

			HideInternal(window, fromQueue);
			SetCurrent(null, null);

			TryShowNext();
		}

		private void TryShowNext()
		{
			if (_queue.IsEmpty())
				return;

			var (window, context) = _queue.Dequeue();
			Show(window, in context, true);
		}

		public void TryHideAll(bool immediate = false)
		{
			_queue.Clear();

			_current.window?.Hide(true, immediate);
			SetCurrent(null, null);
		}

		private bool TryAddToQueueAndHide(ref WindowQueueContext context)
		{
			if (context.window == null)
				return false;

			if (!context.window.Active)
				return false;

			context.ActualArgs();
			_queue.Enqueue(context.window, in context);

			if (!context.overlaid)
				HideInternal(context.window, true);

			return true;
		}

		private void HideInternal(IWindow window, bool fromQueue = false)
		{
			window.Hide(!fromQueue);
			Hidden?.Invoke(window, fromQueue);
		}

		private void SetCurrent(IWindow window, WindowMode? mode)
		{
			_current.window = window;
			_current.mode = mode;
		}

		#region Delegates

		public delegate void ShownDelegate(IWindow window, bool fromQueue);

		public delegate void HiddenDelegate(IWindow window, bool fromQueue);

		#endregion
	}
}
