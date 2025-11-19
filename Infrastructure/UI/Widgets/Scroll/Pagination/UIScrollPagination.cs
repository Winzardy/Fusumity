using System;
using Sapientia.Collections;
using Sapientia.Pooling;
using UnityEngine;

namespace UI.Scroll.Pagination
{
	public struct UIScrollPageArgs<TArgs> : IScrollListItemArgs
		where TArgs : IScrollListItemArgs
	{
		public bool selected;
		public ArrayReference<TArgs> Reference { get; }

		public int Index { get; }

		public bool IsEmpty => Reference.IsEmpty;

		public UIScrollPageArgs(in ArrayReference<TArgs> reference, int index) : this()
		{
			Reference = reference;
			Index = index;
		}
	}

	public interface IScrollPagination<TItemArgs> : IWidget<UIScrollLayout>, IDisposable
		where TItemArgs : struct, IScrollListItemArgs
	{
		void Bind(IScrollList<TItemArgs> scroll);
	}

	//TODO: Сырой вариант, какие-то кейсы возможно не учтены...
	public class UIScrollPagination<TPage, TPageLayout, TItemArgs> : UIScrollList<TPage, TPageLayout,
		UIScrollPageArgs<TItemArgs>>, IScrollPagination<TItemArgs>
		where TPage : UIScrollPage<TPageLayout, TItemArgs>
		where TPageLayout : UIScrollPageLayout
		where TItemArgs : struct, IScrollListItemArgs
	{
		private IScrollList<TItemArgs> _scroll;
		private Selection<UIScrollPageArgs<TItemArgs>> _selection;

		public sealed override void Initialize()
		{
			_selection = new Selection<UIScrollPageArgs<TItemArgs>>(OnSelected, SelectionParameters.Single);
			base.Initialize();
		}

		private protected override void OnDisposedInternal()
		{
			base.OnDisposedInternal();

			_selection.Dispose();
			TryClearScroll();
		}

		protected sealed override void OnItemInitialized(TPage item)
		{
			item.Clicked += OnPageClicked;
			OnPageInitialized(item);
		}

		protected sealed override void OnItemDisposed(TPage item)
		{
			item.Clicked -= OnPageClicked;
			OnPageDisposed(item);
		}

		private void OnPageClicked(UIScrollPageArgs<TItemArgs> args)
		{
			_scroll?.TweenTo(args.Index, UIScrollLayout.TweenType.easeOutQuad, 0.15f);
		}

		private void OnSelected(int index, bool selected, bool immediate)
		{
			_data[index].selected = selected;
			FindCellAtDataIndex(index)?.SetSelected(selected, immediate);
		}

		public void Bind(IScrollList<TItemArgs> scroll)
		{
			TryClearScroll();

			_scroll = scroll;

			Update(_scroll.Data);
			_scroll.Updated += OnUpdated;
			_scroll.Scrolled += OnScrolled;
		}

		private void OnScrolled(UIScrollLayout layout, Vector2 val, float scrollPosition)
		{
			if (layout.IsTweening)
				return;

			var index = layout.GetMiddleCellDataIndex();
			Select(index);
		}

		private void Select(int center)
		{
			_selection.TrySelect(center);
		}

		private void TryClearScroll()
		{
			if (_scroll == null)
				return;

			_scroll.Updated -= OnUpdated;
			_scroll.Scrolled -= OnScrolled;

			_scroll = null;
		}

		private void OnUpdated(TItemArgs[] data, float normalizedScrollPosition) =>
			Update(data, normalizedScrollPosition);

		private void Update(TItemArgs[] data, float normalizedScrollPosition = 0)
		{
			var pagesData = CollectPageArgs(data);
			Update(pagesData, normalizedScrollPosition);
			_selection.Bind(pagesData);
		}

		private UIScrollPageArgs<TItemArgs>[] CollectPageArgs(TItemArgs[] args)
		{
			using (ListPool<UIScrollPageArgs<TItemArgs>>.Get(out var list))
			{
				if (!args.IsNullOrEmpty())
				{
					for (int i = 0; i < args.Length; i++)
					{
						var reference = new ArrayReference<TItemArgs>(args, i);
						list.Add(new UIScrollPageArgs<TItemArgs>(reference, i));
					}
				}

				return list.ToArray();
			}
		}

		protected virtual void OnPageInitialized(TPage page)
		{
		}

		protected virtual void OnPageDisposed(TPage page)
		{
		}
	}
}
