using System;
using System.Threading;
using Sapientia.Pooling;
using Sapientia.Utility;

namespace UI.Popovers
{
	/// <summary>
	/// Для управления используйте <see cref="UIPopoverDispatcher"/>
	/// </summary>
	public partial class UIPopoverManager : IDisposable
	{
		private readonly PopoverPool _pool;

		private readonly CancellationTokenSource _cts = new();

		internal event ShownDelegate Shown;
		internal event HiddenDelegate Hidden;

		public UIPopoverManager()
		{
			var factory = new UIPopoverFactory();
			_pool = new PopoverPool(factory);

			InitializeAssetsPreloader();
		}

		void IDisposable.Dispose()
		{
			_pool.Dispose();

			DisposeAssetsPreloader();

			_cts?.Trigger();
		}

		internal void Show<T>(ref PopoverToken<T> token, UIWidget source, IPopoverArgs args)
			where T : UIWidget, IPopover
		{
			if (token.IsValid())
			{
				token.Popover.Show(args);
				return;
			}

			var popover = _pool.Get<T>(source);

			var pooledToken = GetToken(popover);

			popover.Show(args);
			popover.Hidden += OnHidden;
			popover.RequestedClose += OnRequestedClose;

			source.LayoutCleared += OnSourceLayoutCleared;

			token = pooledToken;

			void OnSourceLayoutCleared(UIBaseLayout _) => OnHidden(popover);

			void OnHidden(IWidget _)
			{
				source.LayoutCleared -= OnSourceLayoutCleared;

				popover.Hidden -= OnHidden;
				popover.RequestedClose -= OnRequestedClose;

				ReleaseToken(pooledToken);
			}

			void OnRequestedClose(IPopover _)
			{
				if (!popover.Active)
					return;

				popover.SetActive(false);
			}
		}

		private PooledPopoverToken<T> GetToken<T>(T popover)
			where T : UIWidget, IPopover
		{
			var token = Pool<PooledPopoverToken<T>>.Get();
			token.Bind(popover, Release);
			return token;
		}

		private void ReleaseToken<T>(PooledPopoverToken<T> token)
			where T : UIWidget, IPopover
		{
			var popover = token.Popover;
			_pool.Release(popover);
			Pool<PooledPopoverToken<T>>.Release(token);
		}

		private void Release<T>(PooledPopoverToken<T> token, bool immediate)
			where T : UIWidget, IPopover
		{
			var popover = token.Popover;
			popover.SetActive(false, immediate);
		}

		internal bool TryHideLast()
		{
			return false;
		}

		#region Delegates

		public delegate void ShownDelegate(UIWidget root, IPopover popover);

		public delegate void HiddenDelegate(UIWidget root, IPopover popover);

		#endregion
	}
}
