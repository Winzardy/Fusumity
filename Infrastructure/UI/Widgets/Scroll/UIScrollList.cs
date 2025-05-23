using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sapientia.Collections;
using UI.Scroll.Pagination;
using UnityEngine;
using UnityEngine.EventSystems;
using ZenoTween;
using ZenoTween.Utility;

namespace UI.Scroll
{
	//TODO: Сделать Reverse, сейчас костылем решают это в одном месте делая scale: -1
	/// <summary>
	/// Виджет для скролла, имеет внутренний пул виджетов (cell), которые переиспользует при отображении элементов (item)
	/// </summary>
	/// <typeparam name="TItemArgs">Данные с которыми работает виджет (cell)</typeparam>
	/// <typeparam name="TItem">Виджет элемента (cell) </typeparam>
	/// <typeparam name="TItemLayout">Разметка ячейки</typeparam>
	/// <see cref="https://www.notion.so/winzardy/Scroll-e6e5430307af4694906cf9a8832efcd6?pvs=4"/>
	public class UIScrollList<TItem, TItemLayout, TItemArgs> : UIScroll<TItemLayout>, IScrollList<TItemArgs>
		where TItem : UIWidget<TItemLayout, TItemArgs>, IScrollItem<TItemLayout>
		where TItemLayout : UIScrollItemLayout
		where TItemArgs : struct, IScrollListItemArgs
	{
		public float ScrollPosition => _layout != null ? _layout.ScrollPosition : 0;
		public float NormalizedScrollPosition => _layout != null ? _layout.NormalizedScrollPosition : 0;

		protected TItemArgs[] _data;

		private protected Dictionary<TItemLayout, TItem> _cells = new();

		public int ItemsCount => GetNumberOfItems();

		public TItemArgs[] Data => _data;
		public IEnumerable<TItem> Cells => _cells.Values;

		//Диспоузится как и все Children виджета, потому что является чилдом)
		private IScrollPagination<TItemArgs> _pagination;

		public event Action<TItemArgs[], float> Updated;

		protected sealed override void OnCellInitialized(UIScrollItemLayout cell)
		{
			var layout = (TItemLayout) cell;

			var item = CreateWidget<TItem, TItemLayout>(layout, false);
			_cells.Add(layout, item);

			OnItemInitialized(item);
		}

		private protected override void OnDisposeInternal()
		{
			base.OnDisposeInternal();

			_data = null;
			_cells = null;
		}

		protected override void OnChildDispose(UIWidget child)
		{
			if (child is TItem item)
				OnItemDisposed(item);
		}

		public void Show(TItemArgs[] data, bool preservePosition = false, bool immediate = false)
		{
			Update(data, preservePosition);

			if (Active)
				return;

			SetActive(true, immediate);
		}

		public void Hide(bool immediate = false) => SetActive(false, immediate: immediate);

		/// <summary>
		/// Лучше использовать Show(in TArgs args) и потом WaitUntilIsVisible(), но это минор
		/// </summary>
		public async UniTask ShowAsync(TItemArgs[] data, bool preservePosition = false, bool immediate = false)
		{
			Show(data, preservePosition, immediate: immediate);
			await WaitUntilIsVisible();
		}

		public async UniTask HideAsync(bool reset = true)
		{
			Hide();
			await WaitUntilIsNotVisible();
			if (reset)
				Reset();
		}

		public void Update(TItemArgs[] data, bool preservePosition = false)
		{
			Update(data, preservePosition ? _layout.NormalizedScrollPosition : 0);
		}

		public void Update(TItemArgs[] data, float normalizedScrollPosition)
		{
			_data = data;

			_layout.ReloadData(normalizedScrollPosition);

			Updated?.Invoke(data, normalizedScrollPosition);
			OnUpdated(normalizedScrollPosition);
		}

		public void Clear()
		{
			Update(null);
		}

		public virtual void Reset()
		{
			OnReset();

			if (_cells.IsNullOrEmpty())
				return;

			foreach (var item in _cells.Values)
			{
				item.Reset();
			}
		}

		public void Refresh()
		{
			if (_cells.IsNullOrEmpty())
				return;

			foreach (var item in _cells.Values)
			{
				item.Refresh();
			}
		}

		public void RefreshVisible()
		{
			if (_cells.IsNullOrEmpty())
				return;

			for (int i = 0; i < _layout.ActiveCells.Count; i++)
			{
				var cell = _layout.ActiveCells[i] as TItemLayout;
				if (_cells.TryGetValue(cell!, out var item))
					item.Refresh();
			}
		}

		/// <param name="time">время анимации (в секундах)</param>
		public void SmoothMoveTo(int itemIndex,
			UIScrollLayout.TweenType tweenType = UIScrollLayout.TweenType.easeOutQuad,
			float time = 0.5f,
			Action onComplete = null,
			bool completeCachedTween = true)
		{
			if (tweenType == UIScrollLayout.TweenType.immediate)
			{
				GUIDebug.LogWarning(
					"Trying smooth scroll move by immediate type, for immediate case need use default MoveTo method!");
				tweenType = UIScrollLayout.TweenType.easeOutQuad;
			}

			MoveTo(itemIndex, tweenType, time, onComplete, completeCachedTween);
		}

		/// <param name="time">время анимации (в секундах)</param>
		public virtual void MoveTo(int itemIndex,
			UIScrollLayout.TweenType tweenType = UIScrollLayout.TweenType.immediate,
			float time = 0f,
			Action onComplete = null,
			bool completeCachedTween = true)
		{
			if (!_layout)
			{
				GUIDebug.LogWarning("Trying scroll move to for scroller with empty layout!");
				return;
			}

			_layout.TweenTo(itemIndex, tweenType, time, onComplete, completeCachedTween);
		}

		void IScrollList<TItemArgs>.TweenTo(int itemIndex,
			UIScrollLayout.TweenType tweenType = UIScrollLayout.TweenType.immediate,
			float time = 0f,
			Action onComplete = null,
			bool completeCachedTween = true) =>
			_layout.TweenTo(itemIndex, tweenType, time, onComplete, completeCachedTween);

		protected virtual void OnUpdated(float normalizedScrollPosition)
		{
		}

		protected virtual void OnItemInitialized(TItem item)
		{
		}

		protected virtual void OnItemUpdated(TItem item)
		{
		}

		protected virtual void OnItemDisposed(TItem item)
		{
		}

		protected virtual void OnCellVisibilityChanged(TItem item)
		{
		}

		protected virtual void OnReset()
		{
			foreach (var item in _cells.Values)
				item.Reset();
		}

		protected override float GetCellSize(int dataIndex) => _data[dataIndex].GetCellSize(
			_layout.ScrollDirection == UIScrollLayout.ScrollDirectionEnum.Horizontal
				? _template.rectTransform.rect.width
				: _template.rectTransform.rect.height);

		protected override int GetNumberOfItems() => _data?.Length ?? 0;

		protected override TItemLayout UpdateCell(int dataIndex, int cellIndex)
		{
			var cell = _layout.GetCell(_template) as TItemLayout;
			var item = _cells[cell!];

			item.Show(in _data[dataIndex], true, false);
			OnItemUpdated(item);

			return cell;
		}

		protected sealed override void OnCellVisibilityChanged(UIScrollItemLayout cell)
		{
			if (_cells.TryGetValue((TItemLayout) cell, out var item))
			{
				item.SetActive(cell.Active);

				OnCellVisibilityChanged(item);

				if (!cell.Active)
					item.Reset();
			}
		}

		protected TItem FindCellAtDataIndex(int dataIndex)
		{
			var cell = _layout.GetCellAtDataIndex(dataIndex) as TItemLayout;
			return cell != null ? _cells[cell] : null;
		}

		protected bool TryGetCellAtDataIndex(int dataIndex, out TItem item)
		{
			item = FindCellAtDataIndex(dataIndex);
			return item != null;
		}

		private static TItemLayout GetNativeItemLayout(UIScrollLayout scrollLayout)
		{
			if (scrollLayout.template)
			{
				GUIDebug.LogError(
					$"Template used in scroll is null. Added 'Prefab' to constructor or to template in scroll layout.",
					scrollLayout);
				return null;
			}

			var layout = scrollLayout.template as TItemLayout;
			if (layout == null)
			{
				GUIDebug.LogError(
					$"Template used in scroll is invalid type [{scrollLayout.template.GetType()}] (need:{typeof(TItemLayout)})",
					scrollLayout);
			}

			return layout;
		}

		#region Pagination

		protected override void OnSetupPagination()
		{
			if (_pagination != null)
				return;

			if (_layout.pagination == null)
				return;

			//Дефолтная реализация
			SetPagination<
				UIScrollPagination<UIScrollPage<UIScrollPageLayout, TItemArgs>, UIScrollPageLayout, TItemArgs>>();
		}

		/// <summary>
		/// Pagination - дополнительный скролл, который добавляет навигацию по "bind" скроллу
		/// </summary>
		protected void SetPagination<TPage, TPageLayout>()
			where TPage : UIScrollPage<TPageLayout, TItemArgs>
			where TPageLayout : UIScrollPageLayout
		{
			SetPagination<UIScrollPagination<TPage, TPageLayout, TItemArgs>>();
		}

		/// <summary>
		/// Pagination - дополнительный скролл, который добавляет навигацию по "bind" скроллу
		/// </summary>
		protected void SetPagination<T>()
			where T : UIWidget, IScrollPagination<TItemArgs>, new()
		{
			_pagination?.Dispose();

			if (_layout.pagination == null)
			{
				GUIDebug.LogError("Trying create pagination, but pagination layout is not set!");
				return;
			}

			_pagination = CreateRawWidget<T>();
			_pagination.SetupLayout(_layout.pagination);
			_pagination.Initialize();

			_pagination.Bind(this);
			_pagination.SetActive(true);
		}

		#endregion
	}

	public abstract class UIScroll<TItemLayout> : UIWidget<UIScrollLayout>, IScrollListDelegate
		where TItemLayout : UIScrollItemLayout
	{
		protected TItemLayout _template;

		/// <summary>
		/// Sequence привязанный к нормализованному Scroll Position (аля параллакс эффект)
		/// </summary>
		private Sequence _scrollSequence;

		public event Action<UIScrollLayout, Vector2, float> Scrolled;

		public bool IsScrolling { get; private set; }

		public override void SetupLayout(UIScrollLayout layout)
		{
			base.SetupLayout(layout);

			OnSetupPagination();

			layout.Initialize();

			if (layout.preserveTemplate)
				SetupItemLayout(layout.template);
		}

		public void SetupItemLayout(UIScrollItemLayout itemTemplate)
		{
			var template = itemTemplate as TItemLayout;

			if (!template)
				throw new Exception("Invalid item template!");

			_template = template;

			_layout.Delegate = this;
			_layout.cellVisibilityChanged = OnCellVisibilityChanged;
			_layout.cellWillRecycled = CellWillRecycled;
			_layout.scrollBeganSnapping = OnScrollBeganSnapping;
			_layout.scrollSnapped = OnScrollSnapped;
			_layout.scrollScrolled = OnScroll;
			_layout.scrollScrollingChanged = OnScrollChanged;
			_layout.cellInstantiated = OnCellInitialized;
			_layout.beginDrag = OnBeginDrag;
			_layout.endedDrag = OnEndDrag;
		}

		protected internal sealed override void OnLayoutInstalledInternal()
		{
			if (_layout.useScrollSequence)
			{
				CreateScrollSequence();
				_layout.RectTransformDimensionsChanged += OnRectTransformDimensionsChanged;
			}

			base.OnLayoutInstalledInternal();
		}

		protected internal sealed override void OnLayoutClearedInternal()
		{
			_scrollSequence?.KillSafe();
			_scrollSequence = null;

			if (_layout.useScrollSequence)
				_layout.RectTransformDimensionsChanged -= OnRectTransformDimensionsChanged;

			base.OnLayoutClearedInternal();
		}

		private void OnRectTransformDimensionsChanged() => CreateScrollSequence();

		protected abstract TItemLayout UpdateCell(int dataIndex, int cellIndex);
		protected abstract int GetNumberOfItems();
		protected abstract float GetCellSize(int dataIndex);

		public int GetNumberOfItems(UIScrollLayout layout) => GetNumberOfItems();

		public float GetCellSize(UIScrollLayout layout, int dataIndex) => GetCellSize(dataIndex);

		public UIScrollItemLayout GetCell(UIScrollLayout layout, int dataIndex, int cellIndex) =>
			UpdateCell(dataIndex, cellIndex);

		private void OnScroll(UIScrollLayout layout, Vector2 val, float scrollPosition)
		{
			OnScroll(val, scrollPosition);

			Scrolled?.Invoke(layout, val, scrollPosition);
			UpdateScrollSequence();
		}

		private void OnScrollChanged(UIScrollLayout layout, bool scrolling)
		{
			IsScrolling = scrolling;
			OnScrollChanged(scrolling);
		}

		protected virtual void OnScrollBeganSnapping(UIScrollLayout layout, int destinationItemIndex)
		{
		}

		private void OnScrollSnapped(UIScrollLayout layout, int cellIndex, int dataIndex,
			UIScrollItemLayout cell)
			=> OnScrollSnapped(cellIndex, dataIndex, cell);

		private void OnCellInitialized(UIScrollLayout layout, UIScrollItemLayout cell)
			=> OnCellInitialized(cell);

		protected virtual void OnScroll(Vector2 val, float scrollPosition)
		{
		}

		protected virtual void OnScrollChanged(bool isScrolling)
		{
		}

		protected virtual void OnCellVisibilityChanged(UIScrollItemLayout cell)
		{
		}

		protected virtual void CellWillRecycled(UIScrollItemLayout cell)
		{
		}

		protected virtual void OnScrollSnapped(int itemIndex, int dataIndex, UIScrollItemLayout cell)
		{
		}

		protected virtual void OnCellInitialized(UIScrollItemLayout cell)
		{
		}

		protected virtual void OnEndDrag(PointerEventData data)
		{
		}

		protected virtual void OnBeginDrag(PointerEventData data)
		{
		}

		protected virtual void OnSetupPagination()
		{
		}

		private void CreateScrollSequence()
		{
			_scrollSequence?.KillSafe();

			if (_layout.scrollSequence.IsNullOrEmpty())
				return;

			_scrollSequence = DOTween.Sequence();
			_layout.scrollSequence.Participate(ref _scrollSequence);
			_scrollSequence
			   .SetAutoKill(false)
			   .Pause();

			UpdateScrollSequence();
		}

		private void UpdateScrollSequence() =>
			_scrollSequence?.Goto(_layout.ContentSize > _layout.ViewportSize ? _layout.NormalizedScrollPosition : 0);
	}
}
