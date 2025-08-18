using System;
using Sapientia.Pooling;

namespace UI.Popovers
{
	internal interface IPopoverToken
	{
		internal int Generation { get; }
		public IPopover RawPopover { get; }

		public void Release(bool immediate = false);
	}

	internal interface IPopoverToken<out T> : IPopoverToken, IDisposable
		where T : UIWidget, IPopover
	{
		public T Popover { get; }
		IPopover IPopoverToken.RawPopover => Popover;
	}

	public readonly struct PopoverToken<T> : IDisposable
		where T : UIWidget, IPopover
	{
		private readonly int _generation;
		private readonly IPopoverToken<T> _token;

		public T Popover => _token.Popover;

		internal PopoverToken(IPopoverToken<T> token, int generation)
		{
			_token = token;
			_generation = generation;
		}

		void IDisposable.Dispose() => Release(true);

		internal void Release(bool immediate = false)
		{
			if (!IsValid())
				throw new InvalidOperationException(
					$"[{nameof(WidgetGroupToken)}] Invalid token (token gen:{_token.Generation} != gen: {_generation})");

			_token.Release(immediate);
		}

		public static implicit operator PopoverToken(PopoverToken<T> token)
			=> new(token._token, token._generation);

		internal bool IsValid() => _token != null && _generation == _token.Generation;
	}

	public readonly struct PopoverToken : IDisposable
	{
		private readonly int _generation;
		private readonly IPopoverToken _token;

		public IPopover Popover => _token.RawPopover;

		internal PopoverToken(IPopoverToken token, int generation)
		{
			_token = token;
			_generation = generation;
		}

		void IDisposable.Dispose() => Release(true);

		internal void Release(bool immediate = false)
		{
			if (_token.Generation != _generation)
				throw new InvalidOperationException(
					$"[{nameof(WidgetGroupToken)}] Invalid token (token gen:{_token.Generation} != gen: {_generation})");

			_token.Release(immediate);
		}

		internal bool IsValid() => _generation == _token.Generation;
	}

	public static class PopoverTokenExtensions
	{
		public static void Release<T>(this ref PopoverToken<T> token, bool immediate = false)
			where T : UIWidget, IPopover
		{
			if (!token.IsValid())
				return;

			token.Release();
			token = default;
		}

		public static void Release<T>(this ref PopoverToken token, bool immediate = false)
			where T : UIWidget, IPopover
		{
			if (!token.IsValid())
				return;

			token.Release();
			token = default;
		}
	}

	internal class PooledPopoverToken<T> : IPopoverToken<T>, IPoolable
		where T : UIWidget, IPopover
	{
		private int _generation;

		private PopoverReleaser<T> _releaser;

		private T _popover;

		public T Popover => _popover;
		int IPopoverToken.Generation => _generation;

		internal void Bind(T popover, PopoverReleaser<T> releaser)
		{
			_releaser = releaser;
			_popover = popover;
		}

		public void Dispose() => Release(true);

		void IPopoverToken.Release(bool immediate) => Release(immediate);

		private void Release(bool immediate = false)
		{
			_releaser?.Invoke(this, immediate);
		}

		void IPoolable.Release()
		{
			_generation++;
			_releaser = null;
			_popover = null;
		}

		public static implicit operator PopoverToken<T>(PooledPopoverToken<T> token) => new(token, token._generation);
	}

	internal delegate void PopoverReleaser<T>(PooledPopoverToken<T> token, bool immediate = false)
		where T : UIWidget, IPopover;
}
