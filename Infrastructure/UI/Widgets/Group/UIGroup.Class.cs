using System.Collections.Generic;
using Sapientia.Collections;
using Sapientia.Pooling;

namespace UI
{
	public class UIGroupC<TWidget, TWidgetLayout, TValue> : UIGroup<TWidget, TWidgetLayout, WidgetСArgs<TValue>>
		where TWidget : UIWidgetC<TWidgetLayout, TValue>
		where TWidgetLayout : UIBaseLayout
		where TValue : class
	{
		//TODO: подумать над immediate для элементов
		public void Show(IEnumerable<TValue> args, bool immediate = false, bool equals = true)
		{
			Update(args, equals);

			if (Active)
				return;

			SetActive(true, immediate);
		}

		public void Update(IEnumerable<TValue> args, bool equals = true)
		{
			if (equals && Equals(args))
				return;

			using (ListPool<WidgetСArgs<TValue>>.Get(out var list))
			{
				foreach (var value in args)
					list.Add(value);

				var cacheActive = Active;

				if (cacheActive)
					SetActive(false, true);

				Update(list);

				if (cacheActive)
					SetActive(true, true);
			}
		}

		private bool Equals(IEnumerable<TValue> values)
		{
			if (_args == null)
				return false;

			foreach (var (arg, index) in values.WithIndexSafe())
			{
				if (_args.Count <= index)
					return false;

				if (!_args[index].Equals(arg))
					return false;
			}

			return true;
		}
	}
}
