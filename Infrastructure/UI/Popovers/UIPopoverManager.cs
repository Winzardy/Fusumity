using System;
using System.Collections.Generic;
using System.Threading;
using JetBrains.Annotations;
using Sapientia.Collections;
using Sapientia.Pooling;
using Sapientia.Utility;
using UnityEngine;

namespace UI.Popovers
{
	public interface IPopoverShowPolicy : IDisposable
	{
		RectTransform Anchor { get; }

		bool IsShown { get; }

		event PopoverDelegate Shown;
		event PopoverDelegate Hidden;

		event Action AnchorUpdated;

		event PopoverDelegate Disposed;
	}

	/// <summary>
	/// Будет диспоузится вместе с токеном...
	/// </summary>
	public interface IAutoDisposePopoverShowPolicy : IPopoverShowPolicy
	{
	}

	public delegate void PopoverDelegate(bool immediate);

	/// <summary>
	/// Для управления используйте <see cref="UIPopoverDispatcher"/>
	/// </summary>
	public partial class UIPopoverManager : IDisposable
	{
		private readonly HashSet<IPopover> _active = new();
		private readonly List<IPopover> _queue = new();

		private readonly PopoverPool _pool;

		private readonly CancellationTokenSource _cts = new();

		internal event ShownDelegate Shown;
		internal event HiddenDelegate Hidden;

		internal IEnumerable<KeyValuePair<IPopover, object>> Active => EnumerateActive();

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

		internal void Show<T>(ref PopoverToken<T> token, [NotNull] IPopoverShowPolicy policy, object args, bool immediate = false)
			where T : UIWidget, IPopover
		{
			if (token.IsValid() && token.Popover.Visible)
			{
				token.Popover.Show(args, true);
				return;
			}

			var pooledToken = Pool<PooledPopoverToken<T>>.Get();

			if (policy is IAutoDisposePopoverShowPolicy)
				pooledToken.Released += OnTokenReleased;

			if (policy.IsShown)
				ShowInternal(immediate);

			policy.Shown         += OnPolicyShown;
			policy.Hidden        += OnPolicyHidden;
			policy.AnchorUpdated += OnPolicyAnchorUpdated;
			policy.Disposed      += OnPolicyDisposed;

			token = pooledToken;

			void OnTokenReleased()
			{
				Clear(false);
				policy.Dispose();
			}

			void OnPolicyShown(bool immediate) => ShowInternal(immediate);
			void OnPolicyHidden(bool immediate) => HideInternal(immediate);
			void OnPolicyAnchorUpdated() => pooledToken.Popover?.UpdateAnchor(policy.Anchor);
			void OnPolicyDisposed(bool immediate) => Clear(immediate);

			void ShowInternal(bool immediate)
			{
				var popover = pooledToken.Popover;
				if (popover == null)
				{
					popover = _pool.Get<T>(policy.Anchor);
					pooledToken.Bind(popover, Release);

					popover.RequestedClose += HandlePopoverRequestedClose;
					popover.Hidden         += OnPopoverHidden;

					_active.Add(popover);
					_queue.Add(popover);
				}

				popover.Show(args, immediate);
			}

			void HideInternal(bool immediate) => pooledToken.Popover?.SetActive(false, immediate);

			void Clear(bool immediate)
			{
				if (policy is IAutoDisposePopoverShowPolicy)
					pooledToken.Released -= OnTokenReleased;

				HideInternal(immediate);

				policy.Shown         -= OnPolicyShown;
				policy.Hidden        -= OnPolicyHidden;
				policy.Disposed      -= OnPolicyDisposed;
				policy.AnchorUpdated -= OnPolicyAnchorUpdated;
			}

			void OnPopoverHidden(IWidget widget)
			{
				var popover = widget as IPopover;
				popover!.Hidden -= OnPopoverHidden;

				Clear(false);
				ReleaseToken(pooledToken);
			}
		}

		private void ReleaseToken<T>(PooledPopoverToken<T> token)
			where T : UIWidget, IPopover
		{
			var popover = token.Popover;

			if (!_active.Remove(popover))
				return;

			popover.RequestedClose -= HandlePopoverRequestedClose;

			_pool.Release(popover);
			_queue.Remove(popover);
			Pool<PooledPopoverToken<T>>.Release(token);
		}

		private void HandlePopoverRequestedClose(IPopover popover)
		{
			if (!popover.Active)
				return;

			popover.SetActive(false);
		}

		private void Release<T>(PooledPopoverToken<T> token, bool immediate)
			where T : UIWidget, IPopover
		{
			var popover = token.Popover;
			popover.SetActive(false, immediate);
		}

		internal bool TryHideLast()
		{
			if (_queue.IsNullOrEmpty())
				return false;

			var last = _queue.Last();
			last.SetActive(false); // Логика дергает OnHidden он сам уберется из списка
			return true;
		}

		internal void HideAll()
		{
			foreach (var active in _active)
				active.SetActive(false, true);
		}

		internal IEnumerable<UIWidget> GetAllActive()
		{
			foreach (var popover in _active)
			{
				if (popover is UIWidget widget)
					yield return widget;
			}
		}

		private IEnumerable<KeyValuePair<IPopover, object>> EnumerateActive()
		{
			foreach (var popover in _active)
				yield return new KeyValuePair<IPopover, object>(popover, popover.GetArgs());
		}

		#region Delegates

		public delegate void ShownDelegate(UIWidget root, IPopover popover);

		public delegate void HiddenDelegate(UIWidget root, IPopover popover);

		#endregion
	}
}
