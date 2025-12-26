using System;
using System.Collections.Generic;
using Sapientia.Collections;
using Sapientia.Pooling;

namespace UI.Scroll
{
	/// <summary>
	/// Реализация для работы с классами в аргументах вместо структур
	/// </summary>
	public class UIScrollGridC<TItem, TItemLayout, TValue> : UIScrollGrid<TItem, TItemLayout, ScrollListItemСArgs<TValue>>
		where TItem : UIWidget<TItemLayout, ScrollListItemСArgs<TValue>>, IScrollItem<TItemLayout>
		where TItemLayout : UIScrollItemLayout
		where TValue : class
	{
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

				Update(list.ToArray(), preservePosition);

				if (Active)
					return;

				SetActive(true, immediate);
			}
		}

		public void Update(IEnumerable<TValue> data, bool preservePosition = false) =>
			Update(data, preservePosition ? _layout.NormalizedScrollPosition : 0);

		public void Update(IEnumerable<TValue> data, float normalizePosition)
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

				_rawData = list.ToArray();
				var gridArgs = Create(ItemAmountInGridItem, _rawData);
				Update(gridArgs, normalizePosition);
			}
		}

		public bool TryGetRowDataIndex(TValue value, out int rowIndex)
		{
			if (value == null)
			{
				rowIndex = -1;
				return false;
			}

			return TryGetRowDataIndex((x) => x == value, out rowIndex);
		}

		public bool TryGetRowDataIndex(Func<TValue, bool> predicate, out int rowIndex)
		{
			foreach (var rowData in _data)
			{
				foreach (var itemIndex in rowData)
				{
					var item = rowData.Items[itemIndex];
					if (predicate.Invoke(item))
					{
						rowIndex = rowData.Index;
						return true;
					}
				}
			}

			rowIndex = -1;
			return false;
		}

		public bool TryGetItemDataIndex(TValue value, out int itemIndex)
		{
			if (value == null)
			{
				itemIndex = -1;
				return false;
			}

			return TryGetItemDataIndex((x) => x == value, out itemIndex);
		}

		public bool TryGetItemDataIndex(Func<TValue, bool> predicate, out int itemIndex)
		{
			foreach (var rowData in _data)
			{
				foreach (var index in rowData)
				{
					var item = rowData.Items[index];
					if (predicate.Invoke(item))
					{
						itemIndex = index;
						return true;
					}
				}
			}

			itemIndex = -1;
			return false;
		}
	}
}
