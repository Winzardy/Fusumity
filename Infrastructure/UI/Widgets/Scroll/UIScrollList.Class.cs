using System.Collections.Generic;
using Sapientia.Collections;
using Sapientia.Pooling;

namespace UI.Scroll
{
	public class UIScrollListC<TItem, TItemLayout, TValue> : UIScrollList<TItem, TItemLayout, ScrollListItemСArgs<TValue>>
		where TItem : UIWidget<TItemLayout, ScrollListItemСArgs<TValue>>, IScrollItem<TItemLayout>
		where TItemLayout : UIScrollItemLayout
		where TValue : class
	{
		public UIScrollListC(UIScrollLayout layout) : base(layout)
		{
			SetActive(true);
		}

		public UIScrollListC()
		{
		}

		public void Show(IEnumerable<TValue> data, bool preservePosition = false, bool immediate = false)
		{
			using (ListPool<ScrollListItemСArgs<TValue>>.Get(out var list))
			{
				foreach (var (value, i) in data.WithIndexSafe())
				{
					list.Add(new ScrollListItemСArgs<TValue>(i)
					{
						value = value
					});
				}

				Show(list.ToArray(), preservePosition, immediate);
			}
		}

		public void Update(IEnumerable<TValue> data, bool preservePosition = false)
			=> Update(data, preservePosition ? _layout.NormalizedScrollPosition : 0);

		public void Update(IEnumerable<TValue> data, float normalizedScrollPosition)
		{
			using (ListPool<ScrollListItemСArgs<TValue>>.Get(out var list))
			{
				foreach (var (value, i) in data.WithIndexSafe())
				{
					list.Add(new ScrollListItemСArgs<TValue>(i)
					{
						value = value
					});
				}

				Update(list.ToArray(), normalizedScrollPosition);
			}
		}
	}
}
