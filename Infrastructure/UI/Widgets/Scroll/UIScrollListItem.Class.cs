using System;
using System.Collections.Generic;

namespace UI.Scroll
{
	/// <summary>
	/// Обертка для аргументов виджета, чтобы передавать class вместо struct
	/// </summary>
	public struct ScrollListItemСArgs<TValue> : IEquatable<ScrollListItemСArgs<TValue>>, IScrollListItemArgs
		where TValue : class
	{
		public TValue value;

		public int Index { get; }

		public ScrollListItemСArgs(int index) : this() => Index = index;

		public static implicit operator TValue(ScrollListItemСArgs<TValue> args) => args.value;
		public static implicit operator bool(ScrollListItemСArgs<TValue> args) => !args.IsEmpty;
		public bool IsEmpty => value == null;

		public bool Equals(ScrollListItemСArgs<TValue> other) => EqualityComparer<TValue>.Default.Equals(value, other.value);
		public override bool Equals(object obj) => obj is ScrollListItemСArgs<TValue> other && Equals(other);
		public override int GetHashCode() => EqualityComparer<TValue>.Default.GetHashCode(value);
	}

	//TODO: придумать название получше
	/// <summary>
	/// Обертка для виджета, чтобы передавать class вместо struct
	/// </summary>
	public abstract class UIScrollListItemC<TLayout, TValue> : UIWidget<TLayout, ScrollListItemСArgs<TValue>>, IScrollItem<TLayout>
		where TLayout : UIScrollItemLayout
		where TValue : class
	{
		public int Index => _args.Index;

		/// <summary>
		/// Для кейсов когда нам нужно обрабатывать в OnShow/OnHide null
		/// </summary>
		protected virtual bool UseEmpty => false;

		public bool IsEmpty => _args;

		public TValue Value => _args.value;

		public void Show(TValue value, bool immediate = false, bool equals = true)
		{
			Update(value, equals);

			if (Active)
				return;

			SetActive(true, immediate);
		}

		public void Update(TValue value, bool equals = true)
		{
			//Зачем обновлять если там одно и тоже
			if (equals && _args.Equals(value))
				return;

			var cacheActive = Active;

			if (cacheActive)
				SetActive(false, true);

			_args = args;

			if (cacheActive)
				SetActive(true, true);
		}

		protected sealed override void OnShow(ref ScrollListItemСArgs<TValue> args)
		{
			if (args.IsEmpty && !UseEmpty)
			{
				Reset();
				return;
			}

			OnShow(args.value);
		}

		protected sealed override void OnHide(ref ScrollListItemСArgs<TValue> args)
		{
			if (args.IsEmpty && !UseEmpty)
				return;

			OnHide(args.value);
		}

		protected abstract void OnShow(TValue value);

		protected virtual void OnHide(TValue value)
		{
		}
	}
}
