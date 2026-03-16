using System;
using System.Collections.Generic;
using Sapientia.Pooling;
using UI.Layers;
using UI.Popovers;
using UnityEngine;

namespace UI
{
	public class UIPopoverDispatcher : IWidgetDispatcher, IDisposable
	{
		private UIPopoverManager _manager;

		public event Action<IPopover> Shown;
		public event Action<IPopover> Hidden;

		public IEnumerable<KeyValuePair<IPopover, object>> Active => _manager.Active;

		public UIPopoverDispatcher(UIPopoverManager manager)
		{
			_manager = manager;

			_manager.Shown  += OnShown;
			_manager.Hidden += OnHidden;
		}

		public void Dispose()
		{
			_manager.Shown  -= OnShown;
			_manager.Hidden -= OnHidden;

			_manager = null;
		}

		/// <inheritdoc cref="Show{T}(ref PopoverToken{T}, IPopoverShowPolicy, object, bool)"/>
		public void Show<T>(ref PopoverToken<T> token,
			UIWidget host,
			object args = null,
			RectTransform customAnchor = null,
			bool immediate = false)
			where T : UIWidget, IPopover
		{
			if (TryUpdate(ref token, args))
				return;

			token = Show<T>(host, args, customAnchor, immediate);
		}

		/// <inheritdoc cref="Show{T}(ref PopoverToken{T}, IPopoverShowPolicy, object, bool)"/>
		public void Show<T>(ref PopoverToken<T> token, UIBaseLayout anchor, object args = null, bool immediate = false)
			where T : UIWidget, IPopover
		{
			if (TryUpdate(ref token, args))
				return;

			token = Show<T>(anchor, args, immediate);
		}

		/// <inheritdoc cref="Show{T}(ref PopoverToken{T}, IPopoverShowPolicy, object, bool)"/>
		public void Show<T>(ref PopoverToken<T> token, object args = null, bool immediate = false)
			where T : UIWidget, IPopover
		{
			if (TryUpdate(ref token, args))
				return;

			token = Show<T>(args, immediate);
		}

		/// <summary>
		/// Показывает поповер с возможностью переиспользовать ранее полученный токен.
		/// В отличие от <see cref="Show{T}(object, bool)"/>,
		/// повторные вызовы с тем же токеном позволяют переоткрывать поповер из одной и той же точки.
		/// </summary>
		public void Show<T>(ref PopoverToken<T> token, IPopoverShowPolicy policy, object args = null, bool immediate = false)
			where T : UIWidget, IPopover
		{
			if (TryUpdate(ref token, args))
				return;

			token = Show<T>(policy, args, immediate);
		}

		public PopoverToken<T> Show<T>(UIBaseLayout anchor,
			object args = null,
			bool immediate = false)
			where T : UIWidget, IPopover
		{
			var policy = Pool<DefaultPopoverShowPolicy>.Get();
			policy.Bind(anchor, OnReleaseRequested);
			return Show<T>(policy, args, immediate);
			void OnReleaseRequested() => Pool<DefaultPopoverShowPolicy>.Release(policy);
		}

		public PopoverToken<T> Show<T>(UIWidget host,
			object args = null,
			RectTransform customAnchor = null,
			bool immediate = false)
			where T : UIWidget, IPopover
		{
			var policy = Pool<DefaultWidgetPopoverShowPolicy>.Get();
			policy.Bind(host, OnReleaseRequested, customAnchor);
			return Show<T>(policy, args, immediate);
			void OnReleaseRequested() => Pool<DefaultWidgetPopoverShowPolicy>.Release(policy);
		}

		public PopoverToken<T> Show<T>(object args = null, bool immediate = false)
			where T : UIWidget, IPopover
		{
			var policy = Pool<DefaultPopoverShowPolicy>.Get();
			var anchor = UIDispatcher.GetLayer(LayerType.POPOVERS);
			policy.Bind(anchor, OnReleaseRequested);
			return Show<T>(policy, args, immediate);
			void OnReleaseRequested() => Pool<DefaultPopoverShowPolicy>.Release(policy);
		}

		public PopoverToken<T> Show<T>(IPopoverShowPolicy policy, object args = null, bool immediate = false)
			where T : UIWidget, IPopover
		{
			return _manager.Show<T>(policy, args, immediate);
		}

		private bool TryUpdate<T>(ref PopoverToken<T> token, object args)
			where T : UIWidget, IPopover
		{
			if (token.IsValid() && token.Popover.Visible)
			{
				token.Popover.Show(args, true);
				return true;
			}

			return false;
		}

		/// <summary>
		/// Попробовать закрыть последний открытый поповер
		/// </summary>
		/// <returns>Получилось ли закрыть?</returns>
		public bool TryHideLast() => _manager.TryHideLast();

		private void OnShown(UIWidget _, IPopover popover)
		{
			Shown?.Invoke(popover);
		}

		private void OnHidden(UIWidget _, IPopover popover)
		{
			Hidden?.Invoke(popover);
		}

		IEnumerable<UIWidget> IWidgetDispatcher.GetAllActive() => _manager.GetAllActive();
		void IWidgetDispatcher.ClearAll() => _manager.HideAll();
	}
}
