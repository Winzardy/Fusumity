using System;
using System.Collections.Generic;
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

		public void Show<T>(ref PopoverToken<T> popoverToken,
			UIWidget host,
			object args = null,
			RectTransform customAnchor = null,
			bool immediate = false)
			where T : UIWidget, IPopover
		{
			var policy = new DefaultWidgetPopoverShowPolicy(host, customAnchor);
			Show(ref popoverToken, policy, args, immediate);
		}

		public PopoverToken<T> Show<T>(UIWidget host,
			object args = null,
			RectTransform customAnchor = null,
			bool immediate = false)
			where T : UIWidget, IPopover
		{
			var policy = new DefaultWidgetPopoverShowPolicy(host, customAnchor);
			return Show<T>(policy, args, immediate);
		}

		public PopoverToken<T> Show<T>(IPopoverShowPolicy policy, object args = null, bool immediate = false)
			where T : UIWidget, IPopover
		{
			var token = new PopoverToken<T>();
			_manager.Show(ref token, policy, args, immediate);
			return token;
		}

		/// <summary>
		/// Показывает поповер с возможностью переиспользовать ранее полученный токен.
		/// В отличие от <see cref="Show{T}(UIWidget, UIBaseLayout, object)"/>,
		/// повторные вызовы с тем же токеном позволяют переоткрывать поповер из одной и той же точки.
		/// </summary>
		public void Show<T>(ref PopoverToken<T> token, IPopoverShowPolicy policy, object args = null, bool immediate = false)
			where T : UIWidget, IPopover
			=> _manager.Show(ref token, policy, args, immediate);

		public void Show<T>(ref PopoverToken<T> token, UIBaseLayout baseLayout, object args = null, bool immediate = false)
			where T : UIWidget, IPopover
			=> Show(ref token, new DefaultPopoverShowPolicy(baseLayout), args, immediate);

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
