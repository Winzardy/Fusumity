using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sapientia.Collections;
using Sapientia.Pooling;
using UnityEngine;

namespace UI
{
	public partial class UIGroup<TWidget, TWidgetLayout, TWidgetArgs> : UIWidget<UIGroupLayout>
		where TWidget : UIWidget<TWidgetLayout, TWidgetArgs>
		where TWidgetLayout : UIBaseLayout
		where TWidgetArgs : struct
	{
		protected SimpleList<TWidgetArgs> _args;

		private UIPool<TWidget, TWidgetLayout> _pool;
		private SimpleList<TWidget> _used = new();

		public event Action<TWidget> Registered;
		public event Action<TWidget> Unregistered;

		public TWidget this[int index] => _used[index];
		public int this[TWidget widget] => _used.IndexOf(widget);

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
			ReleaseAll();
			DisposePool();
		}

		protected override void OnDispose()
		{
			DisposePool();
		}

		public bool TryGet(int index, out TWidget widget)
		{
			widget = null;

			if (_used.IsNullOrEmpty())
				return false;

			if (index >= 0 && index < _used.Count)
				widget = _used[index];

			return widget != null;
		}

		#region Show/Hide

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
				ReleaseAll();
				return;
			}

			for (int i = 0; i < _args.Count; i++)
			{
				if (!TryGet(i, out var widget))
					widget = Add();

				widget.Show(in _args[i], _immediate);
			}

			var usedWidgetAmount = _used.Count;
			var count = usedWidgetAmount - _args.Count;

			for (int i = 0; i < count; i++)
				Unregister(usedWidgetAmount - 1 - i);

			if (_layout.forceRebuild)
				ForceRebuildLayout();
		}

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

		#endregion

		protected virtual void OnRegisteredElement(TWidget widget)
		{
		}

		private void Unregister(int index, bool release = true)
		{
			var widget = _used[index];
			Unregister(widget, release);
		}

		/// <param name="release">Отпустить в пул, если <c>false</c> то вероятно полностью очищаем</param>
		private void Unregister(TWidget widget, bool release = true)
		{
			Unregistered?.Invoke(widget);
			OnUnregisteredElement(widget);

			if (!release)
				return;

			if (_used.Remove(widget))
				_pool.Release(widget);
		}

		protected virtual void OnUnregisteredElement(TWidget widget)
		{
		}

		private void DisposePool()
		{
			if (_pool == null)
				return;

			_pool?.Dispose();
			_pool = null;
		}

		private void ReleaseAll()
		{
			if (_used.IsNullOrEmpty())
				return;

			foreach (var widget in _used)
				Unregister(widget, false);

			_used.ReleaseAndClear(_pool);
		}

		public override void Reset(bool deactivate = true)
		{
			if (deactivate)
				SetActive(false, true);

			_args?.Clear();
			ReleaseAll();

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
			_used.Add(widget);

			widget.SetSiblingIndex(_used.Count);

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
