using System;
using System.Threading;
using Sapientia.Pooling;
using Sapientia.Utility;
using UnityEngine;

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

		/// <param name="host">Виджет к которому привязан поповер</param>
		internal void Show<T>(ref PopoverToken<T> token, UIWidget host, IPopoverArgs args, RectTransform customAnchor = null)
			where T : UIWidget, IPopover
		{
			if (token.IsValid() && token.Popover.Visible)
			{
				token.Popover.Show(args);
				return;
			}

			var popover = _pool.Get<T>(host, customAnchor);

			var pooledToken = GetToken(popover);

			popover.Show(args);
			popover.Hidden += OnHidden;
			popover.RequestedClose += OnRequestedClose;

			host.LayoutCleared += OnSourceLayoutCleared;

			token = pooledToken;

			// Принудительно убрать поповер в пул (что происходит при OnHidden)
			void OnSourceLayoutCleared(UIBaseLayout _) => OnHidden(popover);

			void OnHidden(IWidget _)
			{
				host.LayoutCleared -= OnSourceLayoutCleared;

				popover.Hidden -= OnHidden;
				popover.RequestedClose -= OnRequestedClose;

				ReleaseToken(pooledToken);
			}

			void OnRequestedClose(IPopover _)
			{
				if (!popover.Active)
					return;

				popover.SetActive(false); // -> приведет к OnHidden
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
