using System;
using Sapientia.Collections;
using Sapientia.Extensions;
using Sapientia.Pooling;

namespace UI.Scroll
{
	/// <summary>
	/// Дефолтная реализация, для сложных кейсов есть Generic
	///
	/// TODO: была идея засунуть логику поведения Grid в ScrollList, который смотрел бы на тип "item layout" и если он
	/// UIScrollGridItemLayout то начинал себя вести как Grid, иначе бы как простой ScrollList...
	/// Но сомневаюсь что это будет прозрачно и удобно.
	/// </summary>
	public class UIScrollGrid<TItem, TItemLayout, TItemArgs> : UIScrollGrid
	<
		UIScrollGridItem<TItem, TItemLayout, TItemArgs>,
		UIScrollGridItemLayout,
		UIScrollGridItemArgs<TItemArgs>,
		TItem,
		TItemLayout,
		TItemArgs
	>
		where TItemArgs : struct, IScrollListItemArgs
		where TItemLayout : UIScrollItemLayout
		where TItem : UIWidget<TItemLayout, TItemArgs>, IScrollItem<TItemLayout>
	{
		public UIScrollGrid(UIScrollLayout layout) : base(layout)
		{
		}

		public UIScrollGrid() : base()
		{
		}
	}

	//Дикое обобщение, кто смотрит сюда не суди строга :D это для сложных кейсов когда нужно контролировать все
	public class UIScrollGrid<TGridItem, TGridItemLayout, TGridItemArgs, TItem, TItemLayout, TItemArgs> : UIScrollList<
		TGridItem,
		TGridItemLayout, TGridItemArgs>
		where TGridItem : UIWidget<TGridItemLayout, TGridItemArgs>, IScrollItem<TGridItemLayout>,
		IScrollGridItem<TItem, TItemLayout, TItemArgs>
		where TGridItemLayout : UIScrollGridItemLayout
		where TGridItemArgs : struct, IScrollGridItemArgs<TItemArgs>
		where TItemArgs : struct, IScrollListItemArgs
		where TItemLayout : UIScrollItemLayout
		where TItem : UIWidget<TItemLayout, TItemArgs>, IScrollItem<TItemLayout>
	{
		protected TItemArgs[] _rawData { get; private protected set; }

		public ref TItemArgs this[int index] => ref _rawData[index];

		public int ItemAmountInGridItem => _template.items.Length;

		public UIScrollGrid(UIScrollLayout layout) : base(layout)
		{
		}

		public UIScrollGrid() : base()
		{
		}

		public void Show(TItemArgs[] data, bool preservePosition = false, bool immediate = false)
		{
			Update(data, preservePosition);

			if (Active)
				return;

			SetActive(true, immediate);
		}

		public void Update(TItemArgs[] data, bool preservePosition = false)
			=> Update(data, preservePosition ? _layout.NormalizedScrollPosition : 0);

		public void Update(TItemArgs[] data, float normalizedPosition)
		{
			_rawData = data;

			var gridArgs = Create(ItemAmountInGridItem, data);
			Update(gridArgs, normalizedPosition);
		}

		public TGridItemArgs[] Create(int length, TItemArgs[] source)
		{
			if (length == 0)
				throw new Exception("Length can't be zero!");

			var groupCount = 0;
			for (int i = 0; i < source.Length; i += length)
				groupCount++;

			using (ListPool<TGridItemArgs>.Get(out var list))
			{
				for (int i = 0; i < groupCount; i++)
				{
					var args = default(TGridItemArgs);
					args.SetIndex(i);

					var start = i * length;

					var end = (start + length).Min(source.Length);
					//Так как аргументы структуры то ссылаться можно только по индексу...
					args.SetItems(new ArraySection<TItemArgs>(source, start, end));

					list.Add(args);
				}

				return list.ToArray();
			}
		}

		public override void MoveTo(int itemIndex,
			UIScrollLayout.TweenType tweenType = UIScrollLayout.TweenType.immediate, float time = 0,
			Action onComplete = null, bool completeCachedTween = true)
		{
			var gridItemIndex = itemIndex / ItemAmountInGridItem;

			base.MoveTo(gridItemIndex, tweenType, time, onComplete, completeCachedTween);
		}

		protected sealed override void OnItemInitialized(TGridItem item)
		{
			for (int i = 0; i < item.Items.Length; i++)
			{
				OnItemInitialized(item.Items[i]);
			}
		}

		protected sealed override void OnItemDisposed(TGridItem item)
		{
			for (int i = 0; i < item.Items.Length; i++)
			{
				OnItemDisposed(item.Items[i]);
			}
		}

		protected sealed override void OnItemUpdated(TGridItem item)
		{
			for (int i = 0; i < item.Items.Length; i++)
			{
				OnItemUpdated(item.Items[i]);
			}
		}

		protected sealed override void OnCellVisibilityChanged(TGridItem item)
		{
			for (int i = 0; i < item.Items.Length; i++)
			{
				OnItemVisibilityChanged(item.Items[i]);
			}
		}

		protected virtual void OnItemInitialized(TItem item)
		{
		}

		protected virtual void OnItemDisposed(TItem item)
		{
		}

		protected virtual void OnItemUpdated(TItem item)
		{
		}

		protected virtual void OnItemVisibilityChanged(TItem item)
		{
		}

		public TItem FindItemByIndex(int index)
		{
			if (index < 0)
				return null;

			var gridItemIndex = index / ItemAmountInGridItem;
			var cell = _layout.GetCellAtDataIndex(gridItemIndex) as TGridItemLayout;
			if (cell == null)
				return null;

			var gridItem = _cells[cell];
			foreach (var item in gridItem.Items)
			{
				if (item.args.Index == index)
					return item;
			}

			return null;
		}
	}
}
