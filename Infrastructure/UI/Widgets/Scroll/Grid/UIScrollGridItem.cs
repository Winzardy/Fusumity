using System.Collections;
using System.Collections.Generic;
using Sapientia.Collections;
using Sapientia.Pooling;

namespace UI.Scroll
{
	public interface IScrollGridItem<TItem, TItemLayout, TItemArgs>
		where TItem : UIWidget<TItemLayout, TItemArgs>, IScrollItem<TItemLayout>
		where TItemLayout : UIScrollItemLayout
		where TItemArgs : struct, IScrollListItemArgs
	{
		public Array<TItem> Items { get; }
	}

	public interface IScrollGridItemArgs<TArgs> : IScrollListItemArgs
	{
		protected internal void SetIndex(int index);

		public ArraySection<TArgs> Items { get; }
		protected internal void SetItems(in ArraySection<TArgs> items);
	}

	public struct UIScrollGridItemArgs<TArgs> : IScrollGridItemArgs<TArgs>
	{
		public int Index { get; private set; }

		void IScrollGridItemArgs<TArgs>.SetIndex(int index) => Index = index;

		public ArraySection<TArgs> Items { get; private set; }

		void IScrollGridItemArgs<TArgs>.SetItems(in ArraySection<TArgs> items) =>
			Items = items;

		public IEnumerator<int> GetEnumerator() => Items.GetEnumerator();
	}

	public class UIScrollGridItem<TItem, TItemLayout, TItemArgs> : UIScrollGridItem
	<
		UIScrollGridItemArgs<TItemArgs>,
		TItem,
		TItemLayout,
		TItemArgs
	>
		where TItemArgs : struct, IScrollListItemArgs
		where TItemLayout : UIScrollItemLayout
		where TItem : UIWidget<TItemLayout, TItemArgs>, IScrollItem<TItemLayout>
	{
		// public struct Args : IScrollGridItemArgs<TItemArgs>
		// {
		// 	public int Index { get; private set; }
		//
		// 	void IScrollGridItemArgs<TItemArgs>.SetIndex(int index) => Index = index;
		//
		// 	public ArraySection<TItemArgs> Items { get; private set; }
		//
		// 	void IScrollGridItemArgs<TItemArgs>.SetItems(ArraySection<TItemArgs> items) =>
		// 		Items = items;
		// }
	}

	public class UIScrollGridItem<TArgs, TItem, TItemLayout, TItemArgs> :
		UIWidget<UIScrollGridItemLayout, TArgs>,
		IScrollItem<UIScrollGridItemLayout>, IScrollGridItem<TItem, TItemLayout, TItemArgs>
		where TArgs : struct, IScrollGridItemArgs<TItemArgs>
		where TItemArgs : struct, IScrollListItemArgs
		where TItemLayout : UIScrollItemLayout
		where TItem : UIWidget<TItemLayout, TItemArgs>, IScrollItem<TItemLayout>
	{
		private Array<TItem> _items;
		public Array<TItem> Items => _items;

		protected override void OnLayoutInstalled()
		{
			var length = _layout.items.Length;
			ArrayPool<TItem>.Get(out _items, length);

			for (int i = 0; i < length; i++)
			{
				var itemLayout = (TItemLayout) _layout.items[i];
				_items[i] = CreateWidget<TItem, TItemLayout>(itemLayout);
			}
		}

		protected override void OnLayoutCleared()
		{
			_items?.ReleaseToStaticPool();
			_items = null;
		}

		protected override void OnShow(ref TArgs args)
		{
			for (int i = 0; i < _items.Length; i++)
				_items[i].Hide(true, true);

			var j = 0;
			foreach (var index in args.Items)
			{
				_items[j].Show(in args.Items[index], true, false);
				j++;
			}
		}

		protected override void OnHide(ref TArgs args)
		{
			for (int i = 0; i < _items.Length; i++)
				_items[i].Hide(true, true);
		}

		public IEnumerator<TItem> GetEnumerator() => _items.GetEnumerator();
	}
}
