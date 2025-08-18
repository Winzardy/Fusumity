using System;
using UI.Popovers;

namespace UI
{
	public class UIPopoverDispatcher : IWidgetDispatcher, IDisposable
	{
		private UIPopoverManager _manager;

		public event Action<IPopover> Shown;
		public event Action<IPopover> Hidden;

		public UIPopoverDispatcher(UIPopoverManager manager)
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

		public void Show<T>(ref PopoverToken<T> token, UIWidget source, IPopoverArgs args = null)
			where T : UIWidget, IPopover
			=> _manager.Show(ref token, source, args);

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
	}
}
