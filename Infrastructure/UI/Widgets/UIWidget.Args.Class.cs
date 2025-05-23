using System;
using System.Collections.Generic;

namespace UI
{
	/// <summary>
	/// Обертка для виджета, чтобы передавать class вместо struct
	/// </summary>
	public struct WidgetСArgs<TValue> : IEquatable<WidgetСArgs<TValue>>
		where TValue : class
	{
		public TValue value;

		private WidgetСArgs(TValue value) : this()
			=> this.value = value;

		public static implicit operator WidgetСArgs<TValue>(TValue value) => new(value);
		public static implicit operator TValue(WidgetСArgs<TValue> args) => args.value;
		public static implicit operator bool(WidgetСArgs<TValue> args) => !args.IsEmpty;
		public bool IsEmpty => value == null;

		public bool Equals(TValue other) => EqualityComparer<TValue>.Default.Equals(value, other);
		public bool Equals(WidgetСArgs<TValue> other) => EqualityComparer<TValue>.Default.Equals(value, other.value);

		public override bool Equals(object obj) => obj is WidgetСArgs<TValue> other && Equals(other);
		public override int GetHashCode() => EqualityComparer<TValue>.Default.GetHashCode(value);
	}

	//TODO: придумать название получше
	/// <summary>
	/// Обертка для виджета, чтобы передавать class вместо struct
	/// </summary>
	public abstract class UIWidgetC<TLayout, TValue> : UIWidget<TLayout, WidgetСArgs<TValue>>
		where TLayout : UIBaseLayout
		where TValue : class
	{
		protected bool _equals;

		/// <summary>
		/// Для кейсов когда нам нужно обрабатывать в OnShow/OnHide null
		/// </summary>
		protected virtual bool UseEmptyArgs => false;

		public ref TValue Value => ref _args.value;

		public bool IsEmpty => _args;

		//TODO: тупо что прыгает сюда, было бы прикольно что при попытке сюда прыгнуть прыгал бы на OnShow() хотя... чисто мысль
		public void Show(TValue value, bool immediate = false, bool equals = true)
		{
			Update(value, equals);

			if (Active)
				return;

			SetActive(true, immediate);
		}

		public void Update(TValue value, bool equals = true)
		{
			_equals = equals;

			//Зачем обновлять если там одно и тоже
			if (equals && _args.Equals(value))
				return;

			var cacheActive = Active;

			if (cacheActive)
				SetActive(false, true);

			_args.value = value;

			if (cacheActive)
				SetActive(true, true);
		}

		protected sealed override void OnShow(ref WidgetСArgs<TValue> args)
		{
			if (args.IsEmpty && !UseEmptyArgs)
			{
				Reset(false);
				return;
			}

			OnShow(args.value);
		}

		protected sealed override void OnHide(ref WidgetСArgs<TValue> args)
		{
			if (args.IsEmpty && !UseEmptyArgs)
				return;

			OnHide(args.value);
		}

		protected abstract void OnShow(TValue value);

		protected virtual void OnHide(TValue value)
		{
		}

		public static implicit operator TValue(UIWidgetC<TLayout, TValue> widget) => widget.Value;
	}
}
