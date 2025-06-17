using JetBrains.Annotations;
using Sapientia.Collections;
using Sapientia.Pooling;

namespace UI.Scroll.Pagination
{
	public abstract class UIScrollPageC<TLayout, TValue> : UIScrollPage<TLayout, UIScrollGridItemArgs<ScrollListItemСArgs<TValue>>>
		where TLayout : UIScrollPageLayout
		where TValue : class
	{
		protected sealed override void OnShow(in ArrayReference<UIScrollGridItemArgs<ScrollListItemСArgs<TValue>>> reference)
		{
			if (reference.IsEmpty)
			{
				OnShow(null);
				return;
			}

			using (ListPool<TValue>.Get(out var list))
			{
				foreach (var index in reference.Value.Items)
				{
					ref readonly var value = ref reference.Value.Items[index].value;
					list.Add(value);
				}

				OnShow(list.ToArray());
			}
		}

		protected sealed override void OnHide(in ArrayReference<UIScrollGridItemArgs<ScrollListItemСArgs<TValue>>> reference)
		{
			if (reference.IsEmpty)
			{
				OnHide(null);
				return;
			}

			using (ListPool<TValue>.Get(out var list))
			{
				foreach (var index in reference.Value.Items)
				{
					ref readonly var value = ref reference.Value.Items[index].value;
					list.Add(value);
				}

				OnHide(list.ToArray());
			}
		}

		protected abstract void OnShow([CanBeNull] TValue[] items);

		protected virtual void OnHide([CanBeNull] TValue[] items)
		{
		}
	}
}
