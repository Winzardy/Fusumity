using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sapientia.Collections;

namespace UI
{
	public partial class UIGroup<TWidget, TWidgetLayout, TWidgetArgs> : UIWidget<UIGroupLayout>
		where TWidget : UIWidget<TWidgetLayout, TWidgetArgs>
		where TWidgetLayout : UIBaseLayout
		where TWidgetArgs : struct
	{
		private bool _immediate;

		protected SimpleList<TWidgetArgs> _args;

		private UIPool<TWidget, TWidgetLayout> _pool;

		private SimpleList<TWidget> _widgets = new();

		public event Action<TWidget> Registered;
		public event Action<TWidget> Unregistered;

		public TWidget this[int index] => _widgets[index];
		public int this[TWidget widget] => _widgets.IndexOf(widget);

		protected override void OnLayoutInstalled()
		{
			var template = _layout.template as TWidgetLayout;

			if (template == null)
			{
				GUIDebug.LogError("Invalid template!", _layout);
				return;
			}

			_pool = new(this, template, _layout.parent, autoActivation: false);
		}

		protected override void OnLayoutCleared()
		{
			TryClearWidgets();
			TryDisposePool();
		}

		private bool TryDisposePool()
		{
			if (_pool == null)
				return false;

			_pool?.Dispose();
			_pool = null;

			return true;
		}

		protected override void OnDispose()
		{
			TryDisposePool();
		}

		public bool TryGet(int index, out TWidget widget)
		{
			widget = null;

			if (_widgets.IsNullOrEmpty())
				return false;

			if (index >= 0 && index < _widgets.Count)
				widget = _widgets[index];

			return widget != null;
		}

		#region Show/Hide

		public void Update(IEnumerable<TWidgetArgs> items, bool immediate = true) //, bool equals = true)
		{
			// //Зачем обновлять если там одно и тоже
			// if (equals && Equals(items))
			// 	return;

			_immediate = immediate;

			var cacheActive = Active;

			if (cacheActive)
				SetActive(false, immediate);

			if (_args == null)
				_args = new SimpleList<TWidgetArgs>();
			else
				_args.Clear();

			if (!items.IsNullOrEmpty())
				foreach (var item in items)
					_args.Add(item);

			if (cacheActive)
				SetActive(true, immediate);
		}

		// private bool Equals(IEnumerable<TWidgetArgs> items)
		// {
		// 	if (_args == args)
		// 		return true;
		//
		// 	if (_args == null && args != null)
		// 		return false;
		//
		// 	if (_args != null && args != null)
		// 	{
		// 		if (_args.Length != args.Length)
		// 			return false;
		//
		// 		for (var i = 0; i < args.Length; i++)
		// 		{
		// 			if (!_args[i].Equals(args[i]))
		// 				return false;
		// 		}
		// 	}
		//
		// 	return true;
		// }

		/// <summary>
		/// Обновляем аргументы и активируем виджет, если он не был активирован
		/// </summary>
		/// <param name="equals">Проверяет новые аргументы с предыдущими, если они равны, то не обновляет</param>
		public void Show(IEnumerable<TWidgetArgs> args, bool immediate = false)
		{
			Update(args, immediate);

			if (Active)
				return;

			SetActive(true, immediate);
		}

		/// <summary>
		/// Аналог SetActive(false), но дополнительно сбрасывает аргументы
		/// </summary>
		public void Hide(bool reset = true, bool immediate = false)
		{
			if (!Active)
				return;

			SetActive(false, immediate);

			if (reset)
				Reset();
		}

		public IGroupToken Add(in TWidgetArgs args, bool immediate = false)
		{
			var widget = Add();
			widget.Show(in args, immediate);

			if (_layout.forceRebuild)
				ForceRebuildLayout();

			return new WidgetGroupToken(widget, Release);
		}

		private void Release(TWidget widget, bool immediate = false)
			=> ReleaseAsync(widget, immediate).Forget();

		private async UniTaskVoid ReleaseAsync(TWidget widget, bool immediate = false)
		{
			if (!immediate)
				await widget.HideAsync();
			else
				widget.Hide(immediate: true);

			Unregister(widget);
		}

		protected sealed override void OnShow()
		{
			if (_args.IsNullOrEmpty())
			{
				TryClearWidgets();
				return;
			}

			for (int i = 0; i < _args.Count; i++)
			{
				if (!TryGet(i, out var widget))
					widget = Add();

				widget.Show(in _args[i], _immediate);
			}

			var usedWidgetAmount = _widgets.Count;
			var count = usedWidgetAmount - _args.Count;

			for (int i = 0; i < count; i++)
				Unregister(usedWidgetAmount - 1 - i);

			if (_layout.forceRebuild)
				ForceRebuildLayout();
		}

		#endregion

		protected virtual void OnRegisteredElement(TWidget widget)
		{
		}

		private void Unregister(int index, bool remove = true)
		{
			var widget = _widgets[index];
			Unregister(widget, remove);
		}

		private void Unregister(TWidget widget, bool remove = true)
		{
			Unregistered?.Invoke(widget);
			OnUnregisteredElement(widget);

			widget.SetActive(false, _immediate);

			if (remove)
			{
				_widgets.Remove(widget);
				_pool.Release(widget);
			}
		}

		protected virtual void OnUnregisteredElement(TWidget widget)
		{
		}

		private void TryClearWidgets()
		{
			if (_widgets.IsNullOrEmpty())
				return;

			foreach (var widget in _widgets)
				Unregister(widget, false);

			_widgets.Clear();
		}

		public override void Reset(bool deactivate = true)
		{
			if (deactivate)
				SetActive(false, true);

			_args?.Clear();
			TryClearWidgets();

			base.Reset(deactivate);
		}

		private TWidget Add()
		{
			var widget = _pool.Get();
			Register(widget);
			return widget;
		}

		private void Register(TWidget widget)
		{
			_widgets.Add(widget);

			widget.SetSiblingIndex(_widgets.Count);

			OnRegisteredElement(widget);
			Registered?.Invoke(widget);
		}

		public readonly struct WidgetGroupToken : IGroupToken
		{
			private readonly TWidget _widget;
			private readonly Action<TWidget, bool> _onRelease;

			public WidgetGroupToken(TWidget widget, Action<TWidget, bool> onRelease)
			{
				_widget = widget;
				_onRelease = onRelease;
			}

			public void Dispose() => Release();

			public void Release(bool immediate = false) => _onRelease.Invoke(_widget, immediate);
		}
	}

	public interface IGroupToken : IDisposable
	{
		public void Release(bool immediate = false);
	}
}
