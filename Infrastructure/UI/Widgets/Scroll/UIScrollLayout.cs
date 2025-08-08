using System;
using System.Collections;
using ZenoTween;
using Fusumity.Utility;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Sapientia.Collections;
using UnityEngine.Serialization;

namespace UI.Scroll
{
	/// <summary>
	/// The Scroll allows you to easily set up a dynamic Scroll that will recycle widgets (views) for you. This means
	/// that using only a handful of views, you can display thousands of rows. This will save memory and processing
	/// power in your application.
	/// </summary>
	[RequireComponent(typeof(ScrollRect))]
	public class UIScrollLayout : UIBaseLayout, IBeginDragHandler, IEndDragHandler, IDragHandler
	{
		#region Animations

		[SerializeField]
		private bool _useAnimations;

		public override bool UseLayoutAnimations => _useAnimations;

		[Tooltip(
			"Последовательность (sequence) которая завязан на нормализованную позицию скролла. Можно использовать для паралакс эффекта.")]
		public bool useScrollSequence;

		[SerializeReference]
		public SequenceParticipant scrollSequence;

		#endregion

		#region Public

		/// <summary>
		/// The direction this Scroll is handling
		/// </summary>
		public enum ScrollDirectionEnum
		{
			Vertical,
			Horizontal
		}

		/// <summary>
		/// Which side of a cell to reference.
		/// For vertical Scrolls, before means above, after means below.
		/// For horizontal Scrolls, before means to left of, after means to the right of.
		/// </summary>
		public enum CellPositionEnum
		{
			Before,
			After
		}

		/// <summary>
		/// This will set how the scroll bar should be shown based on the data. If no scrollbar
		/// is attached, then this is ignored. OnlyIfNeeded will hide the scrollbar based on whether
		/// the Scroll is looping or there aren't enough items to scroll.
		/// </summary>
		public enum ScrollbarVisibilityEnum
		{
			OnlyIfNeeded,
			Always,
			Never
		}

		public enum PaddingMode
		{
			None,

			Center,
			CenterByContent
		}

		[Space]
		[SerializeField]
		private ScrollRect _scrollRect;

		/// <summary>
		/// The direction the Scroll is handling
		/// </summary>
		[SerializeField, FormerlySerializedAs("scrollDirection")]
		private ScrollDirectionEnum _scrollDirection;

		public ScrollDirectionEnum ScrollDirection => _scrollDirection;

		/// <summary>
		/// The number of pixels between cells, starting after the first cell
		/// </summary>
		public float spacing;

		/// <summary>
		/// The padding inside of the Scroll: top, bottom, left, right.
		/// </summary>
		public RectOffset padding;

		/// <summary>
		/// Add padding to the center of the Scroll
		/// </summary>
		public PaddingMode paddingMode;

		/// <summary>
		/// Whether the Scroll should loop the cells
		/// </summary>
		[SerializeField]
		private bool _loop;

		/// <summary>
		/// Whether the Scroll should process loop jumping while being dragged.
		/// Note: if this is turned off while using a small list size, you may
		/// see elements missing while dragging near the edges of the list. Turning
		/// this value off can sometimes help with Unity adding a lot of velocity
		/// while dragging near the end of a list that loops. If this value is turned
		/// off, you can mitigate the large inertial velocity by setting the maxVelocity
		/// value to a non-zero amount (see maxVelocity).
		/// </summary>
		public bool loopWhileDragging = true;

		/// <summary>
		/// The maximum speed the Scroll can go. This can be useful to eliminate
		/// aggressive scrolling by the user. It can also be used to mitigate the
		/// large inertial velocity that Unity adds in the ScrollRect when dragging
		/// and looping near the edge of the list (See loopWhileDragging).
		/// </summary>
		public float maxVelocity;

		/// <summary>
		/// Once the cell has been snapped to the Scroll location, this
		/// value will determine how the cell is centered on that Scroll
		/// location.
		/// Typically, the offset is in the range 0..1, with 0 being
		/// the top / left of the cell and 1 being the bottom / right.
		/// </summary>
		public float cellCenterNormalizedOffset;

		/// <summary>
		/// The snap location to move the cell to. When the snap occurs,
		/// this location in the Scroll will be where the snapped cell
		/// is moved to.
		/// Typically, the offset is in the range 0..1, with 0 being
		/// the top / left of the Scroll and 1 being the bottom / right.
		/// In most situations the watch offset and the jump offset
		/// will be the same, they are just separated in case you need
		/// that added functionality.
		/// </summary>
		public float cellTweenToNormalizedOffset;

		/// <summary>
		/// Whether the scollbar should be shown
		/// </summary>
		[SerializeField]
		private ScrollbarVisibilityEnum _scrollbarVisibility;

		/// <summary>
		/// Whether snapping is turned on
		/// </summary>
		public bool snapping;

		/// <summary>
		/// This is the speed that will initiate the snap. When the
		/// Scroll slows down to this speed it will snap to the location
		/// specified.
		/// </summary>
		public float snapVelocityThreshold = 50;

		/// <summary>
		/// The snap offset to watch for. When the snap occurs, this
		/// location in the Scroll will be how which cell to snap to
		/// is determined.
		/// Typically, the offset is in the range 0..1, with 0 being
		/// the top / left of the Scroll and 1 being the bottom / right.
		/// In most situations the watch offset and the jump offset
		/// will be the same, they are just separated in case you need
		/// that added functionality.
		/// </summary>
		public float snapWatchOffset;

		/// <summary>
		/// Whether to include the spacing between items when determining the
		/// item offset centering.
		/// </summary>
		public bool snapUseItemSpacing;

		/// <summary>
		/// What function to use when interpolating between the current
		/// scroll position and the snap location. This is also known as easing.
		/// If you want to go immediately to the snap location you can either
		/// set the snapTweenType to immediate or set the snapTweenTime to zero.
		/// </summary>
		public TweenType snapTweenType;

		/// <summary>
		/// The time it takes to interpolate between the current scroll
		/// position and the snap location.
		/// If you want to go immediately to the snap location you can either
		/// set the snapTweenType to immediate or set the snapTweenTime to zero.
		/// </summary>
		public float snapTweenTime;

		/// <summary>
		/// While true keeps snapping while the Scroll is dragged.
		/// While false, this will disable snapping until the dragging stops.
		/// </summary>
		public bool snapWhileDragging;

		/// <summary>
		/// Will cause a snap to occur (if snapping is true) when the scroller stops
		/// dragging. Useful if the touch has moved the scroller, but then is static
		/// before releasing.
		/// </summary>
		public bool forceSnapOnEndDrag;

		/// <summary>
		/// Will stop the snap tweening if the touch re-engages the scroller
		/// </summary>
		public bool interruptTweeningOnDrag;

		[Tooltip
		("Leave object that is used by Scroll as placeholder for ScrollRect intact. " +
			"Could be used as prefab template object.")]
		public bool preserveTemplate = true;

		public UIScrollItemLayout template;

		[Tooltip("Дополнительный Scroll для навигации...")]
		public UIScrollLayout pagination;

		/// <summary>
		/// The amount of space to look ahead before the Scroll position.
		/// This allows items to be loaded before the first visible cell even if they
		/// are not displayed yet. Good for tweening and loading external resources.
		/// </summary>
		private float _lookAheadBefore;

		public float lookAheadBefore { get => _lookAheadBefore; set => _lookAheadBefore = Mathf.Abs(value); }

		/// <summary>
		/// The amount of space to look ahead after the last visible cell.
		/// This allows items to be loaded before the last visible cell even if they
		/// are not displayed yet. Good for tweening and loading external resources.
		/// </summary>
		private float _lookAheadAfter;

		public float lookAheadAfter { get => _lookAheadAfter; set => _lookAheadAfter = Mathf.Abs(value); }

		private bool _forceUpdatePaddingRequest;

		/// <summary>
		/// This delegate is called when a cell is hidden or shown
		/// </summary>
		public CellVisibilityChangedDelegate cellVisibilityChanged;

		/// <summary>
		/// This delegate is called just before a cell is hidden by recycling
		/// </summary>
		public CellWillRecycleDelegate cellWillRecycled;

		/// <summary>
		/// This delegate is called when the scroll rect scrolls
		/// </summary>
		public ScrollScrolledDelegate scrollScrolled;

		/// <summary>
		/// This delegate is called when the Scroll has snapped to a position
		/// </summary>
		public ScrollSnappedDelegate scrollSnapped;

		/// <summary>
		/// This delegate is called when the Scroll just began snapping.
		/// Returns index of cell that scroll will be snapped to.
		/// </summary>
		public ScrollBeganSnappingDelegate scrollBeganSnapping;

		/// <summary>
		/// This delegate is called when the Scroll has started or stopped scrolling
		/// </summary>
		public ScrollScrollingChangedDelegate scrollScrollingChanged;

		/// <summary>
		/// This delegate is called when the Scroll has started or stopped tweening
		/// </summary>
		public ScrollTweeningChangedDelegate scrollTweeningChanged;

		/// <summary>
		/// This delegate is called when begin drag.
		/// </summary>
		public BeginDragDelegate beginDrag;

		/// <summary>
		/// This delegate is called when end drag.
		/// </summary>
		public EndDragDelegate endedDrag;

		/// <summary>
		/// This delegate is called when the Scroll creates a new cell from scratch
		/// </summary>
		public CellInstantiatedDelegate cellInstantiated;

		/// <summary>
		/// This delegate is called when the Scroll reuses a recycled item
		/// </summary>
		public CellReusedDelegate cellReused;

		/// <summary>
		/// The Delegate is what the Scroll will call when it needs to know information about
		/// the underlying data or views. This allows a true MVC process.
		/// </summary>
		public IScrollListDelegate Delegate
		{
			get => _delegate;
			set
			{
				_delegate = value;
				_reloadDataRequest = true;
			}
		}

		/// <summary>
		/// The absolute position in pixels from the start of the Scroll
		/// </summary>
		public float ScrollPosition
		{
			get => _scrollPosition;
			set
			{
				if (_loop)
				{
					// if we are looping, we need to make sure the new position isn't past the jump trigger.
					// if it is we need to reset back to the jump position on the other side of the area.

					//if (value > _loopLastJumpTrigger)
					//{
					//	value = _loopFirstScrollPosition + (value - _loopLastJumpTrigger);
					//}
					//else if (value < _loopFirstJumpTrigger)
					//{
					//	value = _loopLastScrollPosition - (_loopFirstJumpTrigger - value);
					//}
				}
				else
				{
					// make sure the position is in the bounds of the current set of views
					value = Mathf.Clamp(value, 0, ActiveCellsSize);
				}

				// only if the value has changed
				if (Math.Abs(_scrollPosition - value) > float.Epsilon)
				{
					_scrollPosition = value;
					if (_scrollDirection == ScrollDirectionEnum.Vertical)
					{
						// set the vertical position
						_scrollRect.verticalNormalizedPosition = 1f - (_scrollPosition / ActiveCellsSize);
					}
					else
					{
						// set the horizontal position
						_scrollRect.horizontalNormalizedPosition = (_scrollPosition / ActiveCellsSize);
					}

					// flag that we need to refresh
					//_refreshActive = true;
				}
			}
		}

		/// <summary>
		/// The size of the active cell container minus the visibile portion
		/// of the Scroll
		/// </summary>
		public float ActiveCellsSize
		{
			get
			{
				if (_scrollDirection == ScrollDirectionEnum.Vertical)
					return Mathf.Max(_container.rect.height - rectTransform.rect.height, 0);

				return Mathf.Max(_container.rect.width - rectTransform.rect.width, 0);
			}
		}

		public float ContentSize
		{
			get
			{
				if (_container == null)
					return 0;

				var isHorizontal = _scrollDirection == ScrollDirectionEnum.Horizontal;
				return isHorizontal ? _container.rect.width : _container.rect.height;
			}
		}

		/// <summary>
		/// Content size without padding
		/// </summary>
		public float ScrollSize
		{
			get
			{
				if (_container == null)
					return 0;

				var isHorizontal = _scrollDirection == ScrollDirectionEnum.Horizontal;
				var startPadding = isHorizontal ? padding.left : padding.top;
				var endPadding = isHorizontal ? padding.right : padding.bottom;
				return ContentSize - startPadding - endPadding;
			}
		}

		public float ViewportSize =>
			_scrollDirection == ScrollDirectionEnum.Vertical ? rectTransform.rect.height : rectTransform.rect.width;

		/// <summary>
		/// The normalized position of the Scroll between 0 and 1
		/// </summary>
		public float NormalizedScrollPosition
		{
			get
			{
				var scrollPosition = ScrollPosition;
				return (scrollPosition <= 0 ? 0 : _scrollPosition / ActiveCellsSize);
			}
		}

		/// <summary>
		/// Whether the Scroll should loop the resulting cells.
		/// Looping creates three sets of internal size data, attempting
		/// to keep the Scroll in the middle set. If the Scroll goes
		/// outside of this set, it will jump back into the middle set,
		/// giving the illusion of an infinite set of data.
		/// </summary>
		public bool Loop
		{
			get => _loop;
			set
			{
				// only if the value has changed
				if (_loop != value)
				{
					// get the original position so that when we turn looping on
					// we can jump back to this position
					var originalScrollPosition = _scrollPosition;

					_loop = value;

					// call resize to generate more internal elements if loop is on,
					// remove the elements if loop is off
					_Resize(false);

					if (_loop)
					{
						// set the new scroll position based on the middle set of data + the original position
						ScrollPosition = _loopFirstScrollPosition + originalScrollPosition;
					}
					else
					{
						// set the new scroll position based on the original position and the first loop position
						ScrollPosition = originalScrollPosition - _loopFirstScrollPosition;
					}

					// update the scrollbars
					ScrollbarVisibility = _scrollbarVisibility;
				}
			}
		}

		/// <summary>
		/// Sets how the visibility of the scrollbars should be handled
		/// </summary>
		public ScrollbarVisibilityEnum ScrollbarVisibility
		{
			get => _scrollbarVisibility;
			set
			{
				_scrollbarVisibility = value;

				// only if the scrollbar exists
				if (_scrollbar != null)
				{
					// make sure we actually have some cells
					if (_cellOffsetArray != null && _cellOffsetArray.Count > 0)
					{
						if (_scrollDirection == ScrollDirectionEnum.Vertical)
						{
							scrollRect.verticalScrollbar = _scrollbar;
						}
						else
						{
							scrollRect.horizontalScrollbar = _scrollbar;
						}

						if (_cellOffsetArray.Last() < ScrollRectSize || _loop)
						{
							// if the size of the scrollable area is smaller than the Scroll
							// or if we have looping on, hide the scrollbar unless the visibility
							// is set to Always.
							_scrollbar.gameObject.SetActive(_scrollbarVisibility == ScrollbarVisibilityEnum.Always);
						}
						else
						{
							// if the size of the scrollable areas is larger than the Scroll
							// or looping is off, then show the scrollbars unless visibility
							// is set to Never.
							_scrollbar.gameObject.SetActive(_scrollbarVisibility != ScrollbarVisibilityEnum.Never);
						}

						if (!_scrollbar.gameObject.activeSelf)
						{
							scrollRect.verticalScrollbar = null;
							scrollRect.horizontalScrollbar = null;
						}
					}
				}
			}
		}

		/// <summary>
		/// This is the velocity of the Scroll.
		/// </summary>
		public Vector2 Velocity { get => _scrollRect.velocity; set => _scrollRect.velocity = value; }

		/// <summary>
		/// The linear velocity is the velocity on one axis.
		/// The Scroll should only be moving one one axix.
		/// </summary>
		public float LinearVelocity
		{
			get =>
				// return the velocity component depending on which direction this is scrolling
				_scrollDirection == ScrollDirectionEnum.Vertical ? _scrollRect.velocity.y : _scrollRect.velocity.x;
			set
			{
				// set the appropriate component of the velocity
				if (_scrollDirection == ScrollDirectionEnum.Vertical)
				{
					_scrollRect.velocity = new Vector2(0, value);
				}
				else
				{
					_scrollRect.velocity = new Vector2(value, 0);
				}
			}
		}

		/// <summary>
		/// Whether the Scroll is scrolling or not
		/// </summary>
		public bool IsScrolling { get; private set; }

		/// <summary>
		/// Whether the Scroll is tweening or not
		/// </summary>
		public bool IsTweening { get; private set; }

		/// <summary>
		/// This is the first cell index showing in the Scroll's visible area
		/// </summary>
		public int StartCellIndex => _activeCellsStartIndex;

		/// <summary>
		/// This is the last cell index showing in the Scroll's visible area
		/// </summary>
		public int EndCellIndex => _activeCellsEndIndex;

		/// <summary>
		/// This is the first data index showing in the Scroll's visible area
		/// </summary>
		public int StartDataIndex => _activeCellsStartIndex % NumberOfItems;

		/// <summary>
		/// This is the last data index showing in the Scroll's visible area
		/// </summary>
		public int EndDataIndex => _activeCellsEndIndex % NumberOfItems;

		/// <summary>
		/// This is the number of items in the Scroll
		/// </summary>
		public int NumberOfItems => _delegate?.GetNumberOfItems(this) ?? 0;

		/// <summary>
		/// This is a convenience link to the Scroll's scroll rect
		/// </summary>
		public ScrollRect scrollRect => _scrollRect;

		/// <summary>
		/// The size of the visible portion of the Scroll
		/// </summary>
		public float ScrollRectSize
		{
			get
			{
				if (_scrollDirection == ScrollDirectionEnum.Vertical)
					return rectTransform.rect.height;
				else
					return rectTransform.rect.width;
			}
		}

		/// <summary>
		/// The first padder before the visible items
		/// </summary>
		public LayoutElement FirstPadder => _firstPadder;

		/// <summary>
		/// The last padder after the visible items
		/// </summary>
		public LayoutElement LastPadder => _lastPadder;

		/// <summary>
		/// Access to the scroll rect container
		/// </summary>
		public RectTransform Container => _container;

		public SimpleList<UIScrollItemLayout> ActiveCells => _activeCells;

		public void Initialize()
		{
			if (_initialized)
				return;

			GameObject container;

			// destroy any content objects if they exist. Likely there will be
			// one at design time because Unity gives errors if it can't find one.
			if (_scrollRect.content != null)
			{
				if (preserveTemplate)
				{
					_scrollRect.content.gameObject.SetActive(false);
				}
				else
				{
					DestroyImmediate(_scrollRect.content.gameObject);
				}
			}

			// Create a new active cell container with a layout group

			container = new GameObject(CONTAINER_NAME, typeof(RectTransform));
			container.transform.SetParent(rectTransform);

			var isVertical = _scrollDirection == ScrollDirectionEnum.Vertical;

			_layoutGroup = isVertical
				? container.AddComponent<VerticalLayoutGroup>()
				: container.AddComponent<HorizontalLayoutGroup>();

			_container = container.GetComponent<RectTransform>();

			// set the containers anchor and pivot
			if (isVertical)
			{
				_container.anchorMin = new Vector2(0, 1);
				_container.anchorMax = Vector2.one;
				_container.pivot = new Vector2(0.5f, 1f);
			}
			else
			{
				_container.anchorMin = Vector2.zero;
				_container.anchorMax = new Vector2(0, 1f);
				_container.pivot = new Vector2(0, 0.5f);
			}

			_container.offsetMax = Vector2.zero;
			_container.offsetMin = Vector2.zero;
			_container.localPosition = Vector3.zero;
			_container.localRotation = Quaternion.identity;
			_container.localScale = Vector3.one;

			_scrollRect.content = _container;

			// cache the scrollbar if it exists
			if (isVertical)
				_scrollbar = _scrollRect.verticalScrollbar;
			else
				_scrollbar = _scrollRect.horizontalScrollbar;

			_defaultPadding = new RectOffset(padding.left, padding.right, padding.top, padding.bottom);

			// cache the layout group and set up its spacing and padding
			_layoutGroup.spacing = spacing;
			_layoutGroup.padding = padding;
			_layoutGroup.childAlignment = TextAnchor.UpperLeft;

			_layoutGroup.childForceExpandHeight = !isVertical;
			_layoutGroup.childForceExpandWidth = isVertical;
			_layoutGroup.childControlHeight = true;
			_layoutGroup.childControlWidth = true;

			// force the Scroll to scroll in the direction we want
			_scrollRect.horizontal = _scrollDirection == ScrollDirectionEnum.Horizontal;
			_scrollRect.vertical = isVertical;

			// create the padder objects
			container = new GameObject("First Padder", typeof(RectTransform), typeof(LayoutElement));
			container.transform.SetParent(_container, false);
			_firstPadder = container.GetComponent<LayoutElement>();

			container = new GameObject("Last Padder", typeof(RectTransform), typeof(LayoutElement));
			container.transform.SetParent(_container, false);
			_lastPadder = container.GetComponent<LayoutElement>();

			// set up the last values for updates
			_lastScrollRectSize = ScrollRectSize;
			_lastLoop = _loop;
			_lastScrollbarVisibility = _scrollbarVisibility;

			_initialized = true;
		}

		private void UpdatePaddingByMode()
		{
			if (paddingMode == PaddingMode.None)
				return;

			var halfViewport = ViewportSize * 0.5f;

			var offset = paddingMode switch
			{
				PaddingMode.Center => _cellSizeArray.FirstOrDefault(),
				PaddingMode.CenterByContent => _cellOffsetArray.LastOrDefault(),
				_ => 0f
			};

			offset *= 0.5f;

			offset = Mathf.Clamp(offset, 0, halfViewport);
			var newVal = Mathf.RoundToInt(halfViewport - offset);

			var isHorizontalDirection = _scrollDirection == ScrollDirectionEnum.Horizontal;

			if (newVal <= 0)
				newVal = isHorizontalDirection ? _defaultPadding.left : _defaultPadding.top;

			padding.left = _defaultPadding.left;
			padding.top = _defaultPadding.top;
			padding.right = _defaultPadding.right;
			padding.bottom = _defaultPadding.bottom;

			if (isHorizontalDirection)
				padding.left = newVal;
			else
				padding.top = newVal;

			if (!_forceUpdatePaddingRequest && _layoutGroup.padding == padding)
				return;

			// Хак, чтобы обновить на OnEnable
			_forceUpdatePaddingRequest = false;

			_layoutGroup.padding = padding;
			_reloadDataRequest = true;
		}

		public void Resize(bool keepPosition = true)
		{
			_Resize(keepPosition);
		}

		/// <summary>
		/// Updates the spacing on the Scroll
		/// </summary>
		/// <param name="value">new spacing value</param>
		public void UpdateSpacing(float value)
		{
			_updateSpacing = false;
			_layoutGroup.spacing = value;
			ReloadData(NormalizedScrollPosition);
		}

		/// <summary>
		/// Create a cell, or recycle one if it already exists
		/// </summary>
		/// <param name="template">The prefab to use to create the cell</param>
		/// <returns></returns>
		public UIScrollItemLayout GetCell(UIScrollItemLayout template)
		{
			// see if there is a view to recycle
			var cell = _GetRecycledCell(template);
			if (cell == null)
			{
				// no recyleable cell found, so we create a new view
				// and attach it to our container
				cell = UIFactory.CreateLayout(template, _container, "[ScrollItem] ");
				cell.transform.Reset();

				_incrementer ??= new Incrementer();

				cell.InstanceIndex = _incrementer.Get();

				// call the instantiated callback
				cellInstantiated?.Invoke(this, cell);
			}
			else
			{
				// if (cell)
				// 	cell.SetActive(true);

				// call the reused callback
				cellReused?.Invoke(this, cell);
			}

			return cell;
		}

		/// <summary>
		/// This resets the internal size list and refreshes the cells
		/// </summary>
		/// <param name="scrollPositionFactor">The percentage of the Scroll to start at between 0 and 1, 0 being the start of the Scroll</param>
		public void ReloadData(float scrollPositionFactor = 0)
		{
			_reloadDataRequest = false;

			// recycle all the active items so
			// that we are sure to get fresh views
			_RecycleAllCells();

			// if we have a delegate handling our data, then
			// call the resize
			if (_delegate != null)
				_Resize(false);

			if (_scrollRect == null || rectTransform == null || _container == null)
			{
				_scrollPosition = 0f;
				return;
			}

			_scrollPosition = Mathf.Clamp(scrollPositionFactor * ActiveCellsSize, 0, ActiveCellsSize);
			if (_scrollDirection == ScrollDirectionEnum.Vertical)
			{
				// set the vertical position
				_scrollRect.verticalNormalizedPosition = 1f - scrollPositionFactor;
			}
			else
			{
				// set the horizontal position
				_scrollRect.horizontalNormalizedPosition = scrollPositionFactor;
			}

			_RefreshActive();
		}

		public void RefreshActive()
		{
			_RefreshActive();
		}

		public void ResetScroll()
		{
			if (_scrollDirection == ScrollDirectionEnum.Vertical)
			{
				_scrollRect.verticalNormalizedPosition = 0;
			}
			else
			{
				_scrollRect.horizontalNormalizedPosition = 0;
			}
		}

		/// <summary>
		/// This calls the RefreshCells method on each active cell.
		/// If you override the Cell.Refresh method in your items
		/// then you can update the UI without having to reload the data.
		/// Note: this will not change the cell sizes, you will need
		/// to call ReloadData for that to work.
		/// </summary>
		public void RefreshActiveCells()
		{
			for (var i = 0; i < _activeCells.Count; i++)
				_activeCells[i].Refresh();
		}

		/// <summary>
		/// Removes all items, both active and recycled from the Scroll.
		/// This will call garbage collection.
		/// </summary>
		public void ClearAll()
		{
			ClearActive();
			ClearRecycled();
		}

		/// <summary>
		/// Removes all the active cells. This should only be used if you want
		/// to get rid of items because of settings set by Unity that cannot be
		/// changed at runtime. This will call garbage collection.
		/// </summary>
		public void ClearActive()
		{
			for (var i = 0; i < _activeCells.Count; i++)
			{
				DestroyImmediate(_activeCells[i].gameObject);
			}

			_activeCells.Clear();
		}

		/// <summary>
		/// Removes all the recycled cells. This should only be used after you
		/// load in a completely different set of cells that will not use the
		/// recycled views. This will call garbage collection.
		/// </summary>
		public void ClearRecycled()
		{
			for (var i = 0; i < _recycledCells.Count; i++)
			{
				DestroyImmediate(_recycledCells[i].gameObject);
			}

			_recycledCells.Clear();
		}

		/// <summary>
		/// Turn looping on or off. This is just a helper function so
		/// you don't have to keep track of the state of the looping
		/// in your own scripts.
		/// </summary>
		public void ToggleLoop()
		{
			Loop = !_loop;
		}

		/// <summary>
		/// Toggle whether the loop jump calculation is used. Loop jumps
		/// give the appearance of a continuous stream of items, when in
		/// reality it is just a set of three groups of items.
		/// Loop jumps can cause issues if you are trying to change the size of
		/// a cell manually (like for expanding / collapsing) around the
		/// borders of the cell groups where the jump occurs.
		/// </summary>
		/// <param name="ignore"></param>
		public void IgnoreLoopJump(bool ignore)
		{
			_ignoreLoopJump = ignore;
		}

		/// <summary>
		/// Sets the scroll position and refresh the active items.
		/// Normally the refreshing would occur the next frame as Unity
		/// picks up the change in the ScrollRect's position.
		/// If you need to handle active items immediately after setting
		/// the scroll position, use this method instead of setting
		/// the ScrollPosition property directly.
		/// </summary>
		/// <param name="scrollPosition"></param>
		public void SetScrollPositionImmediately(float scrollPosition)
		{
			ScrollPosition = scrollPosition;
			_RefreshActive();
		}

		public enum LoopJumpDirectionEnum
		{
			Closest,
			Up,
			Down
		}

		public float GetCellRelativePosition(
			int dataIndex,
			float ScrollOffset = 0,
			float cellOffset = 0,
			bool useSpacing = true,
			LoopJumpDirectionEnum loopJumpDirection = LoopJumpDirectionEnum.Closest)
		{
			var cellOffsetPosition = 0f;

			if (cellOffset != 0)
			{
				// get the cell's size
				var cellSize = _delegate?.GetCellSize(this, dataIndex) ?? 0;

				if (useSpacing)
				{
					// if using spacing add spacing from one side
					cellSize += spacing;

					// if this is not a bounday cell, then add spacing from the other side
					if (dataIndex > 0 && dataIndex < (NumberOfItems - 1))
						cellSize += spacing;
				}

				// calculate the position based on the size of the cell and the offset within that cell
				cellOffsetPosition = cellSize * cellOffset;
			}

			if (Math.Abs(ScrollOffset - 1f) < float.Epsilon)
				cellOffsetPosition += padding.bottom;

			// cache the offset for quicker calculation
			var offset = -(ScrollOffset * ScrollRectSize) + cellOffsetPosition;

			var newScrollPosition = 0f;

			if (_loop)
			{
				#region Loop

				// if looping, then we need to determine the closest jump position.
				// we do that by checking all three sets of data locations, and returning the closest one

				var numberOfItems = NumberOfItems;

				// get the scroll positions for each data set.
				// Note: we are calculating the position based on the cell index, not the data index here

				var set1CellIndex = _loopFirstCellIndex - (numberOfItems - dataIndex);
				var set2CellIndex = _loopFirstCellIndex + dataIndex;
				var set3CellIndex = _loopFirstCellIndex + numberOfItems + dataIndex;

				var set1Position = GetScrollPositionForCellIndex(set1CellIndex, CellPositionEnum.Before) + offset;
				var set2Position = GetScrollPositionForCellIndex(set2CellIndex, CellPositionEnum.Before) + offset;
				var set3Position = GetScrollPositionForCellIndex(set3CellIndex, CellPositionEnum.Before) + offset;

				// get the offsets of each scroll position from the current scroll position
				var set1Diff = (Mathf.Abs(_scrollPosition - set1Position));
				var set2Diff = (Mathf.Abs(_scrollPosition - set2Position));
				var set3Diff = (Mathf.Abs(_scrollPosition - set3Position));

				var setOffset = -(ScrollOffset * ScrollRectSize);

				var currentSet = 0;
				var currentCellIndex = 0;
				var nextCellIndex = 0;

				if (loopJumpDirection == LoopJumpDirectionEnum.Up || loopJumpDirection == LoopJumpDirectionEnum.Down)
				{
					currentCellIndex = GetCellIndexAtPosition(_scrollPosition - setOffset + 0.0001f);

					if (currentCellIndex < numberOfItems)
					{
						currentSet = 1;
						nextCellIndex = dataIndex;
					}
					else if (currentCellIndex >= numberOfItems && currentCellIndex < (numberOfItems * 2))
					{
						currentSet = 2;
						nextCellIndex = dataIndex + numberOfItems;
					}
					else
					{
						currentSet = 3;
						nextCellIndex = dataIndex + (numberOfItems * 2);
					}
				}

				switch (loopJumpDirection)
				{
					case LoopJumpDirectionEnum.Closest:

						// choose the smallest offset from the current position (the closest position)
						if (set1Diff < set2Diff)
						{
							if (set1Diff < set3Diff)
							{
								newScrollPosition = set1Position;
							}
							else
							{
								newScrollPosition = set3Position;
							}
						}
						else
						{
							if (set2Diff < set3Diff)
							{
								newScrollPosition = set2Position;
							}
							else
							{
								newScrollPosition = set3Position;
							}
						}

						break;

					case LoopJumpDirectionEnum.Up:

						if (nextCellIndex < currentCellIndex)
						{
							newScrollPosition = (currentSet == 1
								? set1Position
								: (currentSet == 2 ? set2Position : set3Position));
						}
						else
						{
							if (currentSet == 1 && (currentCellIndex == dataIndex))
							{
								newScrollPosition = set1Position - _singleLoopGroupSize;
							}
							else
							{
								newScrollPosition = (currentSet == 1
									? set3Position
									: (currentSet == 2 ? set1Position : set2Position));
							}
						}

						break;

					case LoopJumpDirectionEnum.Down:

						if (nextCellIndex > currentCellIndex)
						{
							newScrollPosition = (currentSet == 1
								? set1Position
								: (currentSet == 2 ? set2Position : set3Position));
						}
						else
						{
							if (currentSet == 3 && (currentCellIndex == nextCellIndex))
							{
								newScrollPosition = set3Position + _singleLoopGroupSize;
							}
							else
							{
								newScrollPosition = (currentSet == 1
									? set2Position
									: (currentSet == 2 ? set3Position : set1Position));
							}
						}

						break;
				}

				if (useSpacing)
				{
					newScrollPosition -= spacing;
				}

				#endregion
			}
			else
			{
				// not looping, so just get the scroll position from the dataIndex
				newScrollPosition = GetScrollPositionForDataIndex(dataIndex, CellPositionEnum.Before) + offset;

				// clamp the scroll position to a valid location
				newScrollPosition = Mathf.Clamp(newScrollPosition - (useSpacing ? spacing : 0), 0, ActiveCellsSize);
			}

			return newScrollPosition;
		}

		public void TweenTo(int dataIndex, TweenType tweenType = TweenType.immediate, float time = 0,
			Action onComplete = null,
			bool completeCachedTween = true)
		{
			if (gameObject.activeInHierarchy)
			{
				JumpToDataIndex(dataIndex, cellTweenToNormalizedOffset, cellCenterNormalizedOffset,
					tweenType: tweenType, tweenTime: time, jumpComplete: onComplete,
					completeCachedTween: completeCachedTween);
			}
			else
			{
				_cachedTween = () =>
				{
					JumpToDataIndex(dataIndex, cellTweenToNormalizedOffset, cellCenterNormalizedOffset,
						tweenType: tweenType, tweenTime: time, jumpComplete: onComplete,
						completeCachedTween: completeCachedTween);
				};
			}
		}

		public void ClearTween()
		{
			_cachedTween = null;
		}

		private IEnumerator _cachedRoutine;

		/// <summary>
		/// Jump to a position in the Scroll based on a dataIndex. This overload allows you
		/// to specify a specific offset within a cell as well.
		/// </summary>
		/// <param name="dataIndex">he data index to jump to</param>
		/// <param name="ScrollOffset">The offset from the start (top / left) of the Scroll in the range 0..1.
		/// Outside this range will jump to the location before or after the Scroll's viewable area</param>
		/// <param name="cellOffset">The offset from the start (top / left) of the cell in the range 0..1</param>
		/// <param name="useSpacing">Whether to calculate in the spacing of the Scroll in the jump</param>
		/// <param name="tweenType">What easing to use for the jump</param>
		/// <param name="tweenTime">How long to interpolate to the jump point</param>
		/// <param name="jumpComplete">This delegate is fired when the jump completes</param>
		public void JumpToDataIndex(
			int dataIndex,
			float ScrollOffset = 0,
			float cellOffset = 0,
			bool useSpacing = true,
			TweenType tweenType = TweenType.immediate,
			float tweenTime = 0f,
			Action jumpComplete = null,
			LoopJumpDirectionEnum loopJumpDirection = LoopJumpDirectionEnum.Closest,
			bool forceCalculateRange = false,
			bool completeCachedTween = true)
		{
			var newScrollPosition =
				GetCellRelativePosition(dataIndex, ScrollOffset, cellOffset, useSpacing, loopJumpDirection);

			if (_cachedRoutine != null)
			{
				StopCoroutine(_cachedRoutine);

				if (completeCachedTween)
				{
					_tweenComplete?.Invoke();
					_tweenComplete = null;
				}

				_cachedRoutine = null;
			}

			// ignore the jump if the scroll position hasn't changed
			if (Math.Abs(newScrollPosition - _scrollPosition) < float.Epsilon)
			{
				jumpComplete?.Invoke();
				return;
			}

			_cachedRoutine = TweenPosition(tweenType, tweenTime, ScrollPosition, newScrollPosition, jumpComplete,
				forceCalculateRange);
			StartCoroutine(_cachedRoutine);
		}

		/// <summary>
		/// Snaps the Scroll on command. This is called internally when snapping is set to true and the velocity
		/// has dropped below the threshold. You can use this to manually snap whenever you like.
		/// </summary>
		public void Snap()
		{
			if (NumberOfItems == 0)
				return;

			// set snap jumping to true so other events won't process while tweening
			_snapJumping = true;

			// stop the Scroll
			LinearVelocity = 0;

			// cache the current inertia state and turn off inertia
			_snapInertia = _scrollRect.inertia;
			_scrollRect.inertia = false;

			// calculate the snap position
			var snapPosition = GetSnapPosition();

			// get the cell index of cell at the watch location
			_snapCellIndex = GetCellIndexAtPosition(snapPosition);

			// get the data index of the cell at the watch location
			_snapDataIndex = _snapCellIndex % NumberOfItems;

			// jump the snapped cell to the jump offset location and center it on the cell offset
			JumpToDataIndex(_snapDataIndex, cellTweenToNormalizedOffset, cellCenterNormalizedOffset, snapUseItemSpacing,
				snapTweenType,
				snapTweenTime,
				SnapJumpComplete);

			scrollBeganSnapping?.Invoke(this, _snapDataIndex);
		}

		/// <summary>
		/// Get position that scroll will be snapped to.
		/// </summary>
		public float GetSnapPosition()
		{
			return ScrollPosition + (ScrollRectSize * Mathf.Clamp01(snapWatchOffset));
		}

		/// <summary>
		/// Get index of central cell in the viewport.
		/// </summary>
		public int GetMiddleCellDataIndex()
		{
			var pos = ScrollPosition + (ScrollRectSize * 0.5f);
			var cellIndex = GetCellIndexAtPosition(pos);
			return cellIndex % NumberOfItems;
		}

		/// <summary>
		/// Execute certain action on items that are currently present in the viewport.
		/// </summary>
		public void ExecuteOnVieweditems<T>(Action<T> action) where T : UIScrollItemLayout
		{
			for (var i = StartDataIndex; i <= EndDataIndex; i++)
			{
				var item = GetCellAtDataIndex(i) as T;
				if (item != null)
				{
					action?.Invoke(item);
				}
			}
		}

		/// <summary>
		/// Gets the scroll position in pixels from the start of the Scroll based on the cellIndex
		/// </summary>
		/// <param name="cellIndex">The cell index to look for. This is used instead of dataIndex in case of looping</param>
		/// <param name="insertPosition">Do we want the start or end of the cell's position</param>
		/// <returns></returns>
		public float GetScrollPositionForCellIndex(int cellIndex, CellPositionEnum insertPosition, bool clamp = false)
		{
			if (NumberOfItems == 0) return 0;
			if (cellIndex < 0) cellIndex = 0;

			if (cellIndex == 0 && insertPosition == CellPositionEnum.Before)
			{
				return 0;
			}
			else
			{
				if (cellIndex < _cellOffsetArray.Count)
				{
					// the index is in the range of cell offsets

					if (insertPosition == CellPositionEnum.Before)
					{
						// return the previous cell's offset + the spacing between cells
						var offset = clamp
							? Mathf.Clamp(_cellOffsetArray[cellIndex - 1], 0, ActiveCellsSize)
							: _cellOffsetArray[cellIndex - 1];

						return offset + spacing +
							(_scrollDirection == ScrollDirectionEnum.Vertical ? padding.top : padding.left);
					}
					else
					{
						// return the offset of the cell (offset is after the cell)
						var offset = clamp
							? Mathf.Clamp(_cellOffsetArray[cellIndex], 0, ActiveCellsSize)
							: _cellOffsetArray[cellIndex];

						return offset + (_scrollDirection == ScrollDirectionEnum.Vertical ? padding.top : padding.left);
					}
				}
				else
				{
					// get the start position of the last cell (the offset of the second to last cell)
					return _cellOffsetArray[_cellOffsetArray.Count - 2];
				}
			}
		}

		/// <summary>
		/// Gets the scroll position in pixels from the start of the Scroll based on the dataIndex
		/// </summary>
		/// <param name="dataIndex">The data index to look for</param>
		/// <param name="insertPosition">Do we want the start or end of the cell's position</param>
		/// <returns></returns>
		public float GetScrollPositionForDataIndex(int dataIndex, CellPositionEnum insertPosition)
		{
			return GetScrollPositionForCellIndex(_loop ? _delegate.GetNumberOfItems(this) + dataIndex : dataIndex,
				insertPosition);
		}

		/// <summary>
		/// Gets the index of a cell at a given position
		/// </summary>
		/// <param name="position">The pixel offset from the start of the Scroll</param>
		/// <returns></returns>
		public int GetCellIndexAtPosition(float position) =>
			// call the overrloaded method on the entire range of the list
			_GetCellIndexAtPosition(position, 0, _cellOffsetArray.Count - 1);

		/// <summary>
		/// Get a cell for a particular data index. If the cell is not currently
		/// in the visible range, then this method will return null.
		/// Note: this is against MVC principles and will couple your controller to the view
		/// more than this paradigm would suggest. Generally speaking, the view can have knowledge
		/// about the controller, but the controller should not know anything about the view.
		/// Use this method sparingly if you are trying to adhere to strict MVC design.
		/// </summary>
		/// <param name="dataIndex">The data index of the cell to return</param>
		/// <returns></returns>
		public UIScrollItemLayout GetCellAtDataIndex(int dataIndex)
		{
			for (var i = 0; i < _activeCells.Count; i++)
			{
				if (_activeCells[i].DataIndex == dataIndex)
					return _activeCells[i];
			}

			return null;
		}

		#endregion Public

		#region Private

		private const string CONTAINER_NAME = "Content";

		/// <summary>
		/// Set after the Scroll is first created. This allwos
		/// us to ignore OnValidate changes at the start
		/// </summary>
		private bool _initialized = false;

		/// <summary>
		/// Set when the spacing is changed in the inspector. Since we cannot
		/// make changes during the OnValidate, we have to use this flag to
		/// later call the _UpdateSpacing method from Update()
		/// </summary>
		private bool _updateSpacing = false;

		/// <summary>
		/// Cached reference to the scrollbar if it exists
		/// </summary>
		private Scrollbar _scrollbar;

		/// <summary>
		/// Cached reference to the active cell container
		/// </summary>
		private RectTransform _container;

		/// <summary>
		/// Cached reference to the layout group that handles view positioning
		/// </summary>
		private HorizontalOrVerticalLayoutGroup _layoutGroup;

		/// <summary>
		/// Reference to the delegate that will tell this Scroll information
		/// about the underlying data
		/// </summary>
		private IScrollListDelegate _delegate;

		/// <summary>
		/// To specify cell creation index
		/// </summary>
		private Incrementer _incrementer;

		private bool _valueChangedSubscribed;

		/// <summary>
		/// Flag to tell the Scroll to reload the data
		/// </summary>
		private bool _reloadDataRequest;

		/// <summary>
		/// Flag to tell the Scroll to refresh the active list of cells
		/// </summary>
		private bool _refreshActive;

		/// <summary>
		/// List of views that have been recycled
		/// </summary>
		private SimpleList<UIScrollItemLayout> _recycledCells = new SimpleList<UIScrollItemLayout>();

		/// <summary>
		/// Cached reference to the element used to offset the first visible cell
		/// </summary>
		private LayoutElement _firstPadder;

		/// <summary>
		/// Cached reference to the element used to keep the cells at the correct size
		/// </summary>
		private LayoutElement _lastPadder;

		/// <summary>
		/// Internal list of cell sizes. This is created when the data is reloaded
		/// to speed up processing.
		/// </summary>
		private SimpleList<float> _cellSizeArray = new SimpleList<float>();

		/// <summary>
		/// Internal list of cell offsets. Each cell offset is an accumulation
		/// of the offsets previous to it.
		/// This is created when the data is reloaded to speed up processing.
		/// </summary>
		private SimpleList<float> _cellOffsetArray = new SimpleList<float>();

		/// <summary>
		/// The Scrolls position
		/// </summary>
		private float _scrollPosition;

		/// <summary>
		/// The list of cells that are currently being displayed
		/// </summary>
		private SimpleList<UIScrollItemLayout> _activeCells = new();

		/// <summary>
		/// The index of the first cell that is being displayed
		/// </summary>
		private int _activeCellsStartIndex;

		/// <summary>
		/// The index of the last cell that is being displayed
		/// </summary>
		private int _activeCellsEndIndex;

		/// <summary>
		/// The index of the first element of the middle section of cell sizes.
		/// Used only when looping
		/// </summary>
		private int _loopFirstCellIndex;

		/// <summary>
		/// The index of the last element of the middle seciton of cell sizes.
		/// used only when looping
		/// </summary>
		private int _loopLastCellIndex;

		/// <summary>
		/// The scroll position of the first element of the middle seciotn of cells.
		/// Used only when looping
		/// </summary>
		private float _loopFirstScrollPosition;

		/// <summary>
		/// The scroll position of the last element of the middle section of cells.
		/// Used only when looping
		/// </summary>
		private float _loopLastScrollPosition;

		/// <summary>
		/// The position that triggers the Scroll to jump to the end of the middle section
		/// of cells. This keeps the Scroll in the middle section as much as possible.
		/// </summary>
		private float _loopFirstJumpTrigger;

		/// <summary>
		/// The position that triggers the Scroll to jump to the start of the middle section
		/// of cells. This keeps the Scroll in the middle section as much as possible.
		/// </summary>
		private float _loopLastJumpTrigger;

		/// <summary>
		/// The cached value of the last scroll rect size. This is checked every frame to see
		/// if the scroll rect has resized. If so, it will refresh.
		/// </summary>
		private float _lastScrollRectSize;

		/// <summary>
		/// The cached value of the last loop setting. This is checked every frame to see
		/// if looping was toggled. If so, it will refresh.
		/// </summary>
		private bool _lastLoop;

		/// <summary>
		/// The cell index we are snapping to
		/// </summary>
		private int _snapCellIndex;

		/// <summary>
		/// The data index we are snapping to
		/// </summary>
		private int _snapDataIndex;

		/// <summary>
		/// Whether we are currently jumping due to a snap
		/// </summary>
		private bool _snapJumping;

		/// <summary>
		/// What the previous inertia setting was before the snap jump.
		/// We cache it here because we need to turn off inertia while
		/// manually tweeing.
		/// </summary>
		private bool _snapInertia;

		/// <summary>
		/// The cached value of the last scrollbar visibility setting. This is checked every
		/// frame to see if the scrollbar visibility needs to be changed.
		/// </summary>
		private ScrollbarVisibilityEnum _lastScrollbarVisibility;

		/// <summary>
		/// The number of items in one third of the allocated Scroll space
		/// </summary>
		private float _singleLoopGroupSize;

		/// <summary>
		/// The snap value to store before the user begins dragging
		/// </summary>
		private bool _snapBeforeDrag;

		/// <summary>
		/// The loop value to store before the user begins dragging.
		/// </summary>
		private bool _loopBeforeDrag;

		/// <summary>
		/// Flag to ignore the jump loop that gives the illusion
		/// of a continuous stream of items
		/// </summary>
		private bool _ignoreLoopJump;

		/// <summary>
		/// The number of fingers that are dragging the ScrollRect.
		/// Used in OnBeginDrag and OnEndDrag
		/// </summary>
		private int _dragFingerCount;

		/// <summary>
		/// Internal variable to disable tweening while in progress. This is set by
		/// OnBeginDrag under certain conditions.
		/// </summary>
		private bool _interruptTween;

		/// <summary>
		/// Stores the last drag position in order to calculate if we need to
		/// do a force snap on OnEndDrag.
		/// </summary>
		private Vector2 _dragPrevPos;

		/// <summary>
		/// Where in the list we are
		/// </summary>
		private enum ListPositionEnum
		{
			First,
			Last
		}

		/// <summary>
		/// Cached tween in case it was triggered before gameObject become active
		/// </summary>
		private Action _cachedTween;

		/// <summary>
		/// This function will create an internal list of sizes and offsets to be used in all calculations.
		/// It also sets up the loop triggers and positions and initializes the cells.
		/// </summary>
		/// <param name="keepPosition">If true, then the Scroll will try to go back to the position it was at before the resize</param>
		private void _Resize(bool keepPosition)
		{
			// cache the original position
			var originalScrollPosition = _scrollPosition;

			// clear out the list of cell sizes and create a new list
			_cellSizeArray.Clear();
			var offset = _AddCellSizes();

			// if looping, we need to create three sets of size data
			if (_loop)
			{
				var cellCount = _cellSizeArray.Count;

				// if the items don't entirely fill up the scroll area,
				// make some more size entries to fill it up
				if (offset < ScrollRectSize)
				{
					var additionalRounds = Mathf.CeilToInt((float) Mathf.CeilToInt(ScrollRectSize / offset) / 2.0f) * 2;
					_DuplicateCellSizes(additionalRounds, cellCount);
					_loopFirstCellIndex = cellCount * (1 + (additionalRounds / 2));
				}
				else
				{
					_loopFirstCellIndex = cellCount;
				}

				_loopLastCellIndex = _loopFirstCellIndex + cellCount - 1;

				// create two more copies of the cell sizes
				_DuplicateCellSizes(2, cellCount);
			}

			// calculate the offsets of each cell
			_CalculateCellOffsets();

			UpdatePaddingByMode();

			// set the size of the active cell container based on the number of cells there are and each of their sizes
			if (_scrollDirection == ScrollDirectionEnum.Vertical)
				_container.sizeDelta = new Vector2(_container.sizeDelta.x,
					_cellOffsetArray.LastOrDefault() + padding.top + padding.bottom);
			else
				_container.sizeDelta = new Vector2(_cellOffsetArray.LastOrDefault() + padding.left + padding.right,
					_container.sizeDelta.y);

			// if looping, set up the loop positions and triggers
			if (_loop)
			{
				_loopFirstScrollPosition = GetScrollPositionForCellIndex(_loopFirstCellIndex, CellPositionEnum.Before) +
					(spacing * 0.5f);
				_loopLastScrollPosition = GetScrollPositionForCellIndex(_loopLastCellIndex, CellPositionEnum.After) -
					ScrollRectSize +
					(spacing * 0.5f);

				_loopFirstJumpTrigger = _loopFirstScrollPosition - ScrollRectSize;
				_loopLastJumpTrigger = _loopLastScrollPosition + ScrollRectSize;
			}

			// create the visibile items
			_ResetVisibleCells();

			// if we need to maintain our original position
			if (keepPosition)
			{
				ScrollPosition = originalScrollPosition;
			}
			else
			{
				if (_loop)
				{
					ScrollPosition = _loopFirstScrollPosition;
				}
				else
				{
					ScrollPosition = 0;
				}
			}

			// set up the visibility of the scrollbar
			ScrollbarVisibility = _scrollbarVisibility;
		}

		/// <summary>
		/// Creates a list of cell sizes for faster access
		/// </summary>
		/// <returns></returns>
		private float _AddCellSizes()
		{
			var offset = 0f;
			_singleLoopGroupSize = 0;
			// add a size for each row in our data based on how many the delegate tells us to create
			for (var i = 0; i < NumberOfItems; i++)
			{
				// add the size of this cell based on what the delegate tells us to use. Also add spacing if this cell isn't the first one
				_cellSizeArray.Add(_delegate.GetCellSize(this, i) + (i == 0 ? 0 : _layoutGroup.spacing));
				_singleLoopGroupSize += _cellSizeArray[^1];
				offset += _cellSizeArray[^1];
			}

			return offset;
		}

		/// <summary>
		/// Create a copy of the cell sizes. This is only used in looping
		/// </summary>
		/// <param name="numberOfTimes">How many times the copy should be made</param>
		/// <param name="cellCount">How many items to copy</param>
		private void _DuplicateCellSizes(int numberOfTimes, int cellCount)
		{
			for (var i = 0; i < numberOfTimes; i++)
			{
				for (var j = 0; j < cellCount; j++)
				{
					_cellSizeArray.Add(_cellSizeArray[j] + (j == 0 ? _layoutGroup.spacing : 0));
				}
			}
		}

		/// <summary>
		/// Calculates the offset of each cell, accumulating the values from previous items
		/// </summary>
		private void _CalculateCellOffsets()
		{
			_cellOffsetArray.Clear();
			var offset = 0f;
			for (var i = 0; i < _cellSizeArray.Count; i++)
			{
				offset += _cellSizeArray[i];
				_cellOffsetArray.Add(offset);
			}
		}

		/// <summary>
		/// Get a recycled cell with a given identifier if available
		/// </summary>
		/// <param name="template">The prefab to check for</param>
		/// <returns></returns>
		private UIScrollItemLayout _GetRecycledCell(UIScrollItemLayout template)
		{
			for (var i = 0; i < _recycledCells.Count; i++)
			{
				var cell = _recycledCells[i];

				if (cell.cellIdentifier == template.cellIdentifier)
				{
					// the cell was found, so we use this recycled one.
					// we also remove it from the recycled list
					_recycledCells.RemoveAt(i);
					return cell;
				}
			}

			return null;
		}

		/// <summary>
		/// This sets up the visible items, adding and recycling as necessary
		/// </summary>
		private void _ResetVisibleCells()
		{
			int startIndex;
			int endIndex;

			// calculate the range of the visible items
			_CalculateCurrentActiveCellRange(out startIndex, out endIndex);

			// go through each previous active cell and recycle it if it no longer falls in the range
			var i = 0;
			var remainingCellIndices = new SimpleList<int>();
			while (i < _activeCells.Count)
			{
				if (_activeCells[i].CellIndex < startIndex || _activeCells[i].CellIndex > endIndex)
				{
					_RecycleCell(_activeCells[i]);
				}
				else
				{
					// this cell index falls in the new range, so we add its
					// index to the reusable list
					remainingCellIndices.Add(_activeCells[i].CellIndex);
					i++;
				}
			}

			if (remainingCellIndices.Count == 0)
			{
				// there were no previous active items remaining,
				// this list is either brand new, or we jumped to
				// an entirely different part of the list.
				// just add all the new cells

				for (i = startIndex; i <= endIndex; i++)
				{
					_AddCell(i, ListPositionEnum.Last);
				}
			}
			else
			{
				// we are able to reuse some of the previous
				// cells

				// first add the views that come before the
				// previous list, going backward so that the
				// new views get added to the front
				for (i = endIndex; i >= startIndex; i--)
				{
					if (i < remainingCellIndices.First())
					{
						_AddCell(i, ListPositionEnum.First);
					}
				}

				// next add teh views that come after the
				// previous list, going forward and adding
				// at the end of the list
				for (i = startIndex; i <= endIndex; i++)
				{
					if (i > remainingCellIndices.Last())
					{
						_AddCell(i, ListPositionEnum.Last);
					}
				}
			}

			// update the start and end indices
			_activeCellsStartIndex = startIndex;
			_activeCellsEndIndex = endIndex;

			// adjust the padding elements to offset the cells correctly
			_SetPadders();
		}

		/// <summary>
		/// Recycles all the active cells
		/// </summary>
		private void _RecycleAllCells()
		{
			while (_activeCells.Count > 0)
				_RecycleCell(_activeCells[0]);
			_activeCellsStartIndex = 0;
			_activeCellsEndIndex = 0;
		}

		/// <summary>
		/// Recycles one cell
		/// </summary>
		/// <param name="cell"></param>
		private void _RecycleCell(UIScrollItemLayout cell)
		{
			cellWillRecycled?.Invoke(cell);

			// remove the cell from the active list
			_activeCells.Remove(cell);

			// add the cell to the recycled list
			_recycledCells.Add(cell);

			// move the GameObject to the recycled container
			//cell.transform.SetParent(_recycledCellContainer);

			// deactivate the cell (this is more efficient than moving the to a new parent like the above commented lines)
			//itemLayout.transform.gameObject.SetActive(false);

			// reset the cell's properties
			cell.DataIndex = 0;
			cell.CellIndex = 0;
			cell.Active = false;

			cellVisibilityChanged?.Invoke(cell);
		}

		/// <summary>
		/// Creates a cell, or recycles if it can
		/// </summary>
		/// <param name="cellIndex">The index of the cell</param>
		/// <param name="listPosition">Whether to add the cell to the beginning or the end</param>
		private void _AddCell(int cellIndex, ListPositionEnum listPosition)
		{
			if (NumberOfItems == 0) return;

			// get the dataIndex. Modulus is used in case of looping so that the first set of items are ignored
			var dataIndex = cellIndex % NumberOfItems;
			// request a cell from the delegate
			var cell = _delegate.GetCell(this, dataIndex, cellIndex);

			// set the cell's properties
			cell.CellIndex = cellIndex;
			cell.DataIndex = dataIndex;
			cell.Active = true;

			// add the cell to the active container
			cell.transform.SetParent(_container, false);
			cell.transform.localScale = Vector3.one;

			// add a layout element to the cell
			if (!cell.TryGetComponent(out LayoutElement layoutElement))
				layoutElement = cell.AddComponent<LayoutElement>();

			// set the size of the layout element
			if (_scrollDirection == ScrollDirectionEnum.Vertical)
				layoutElement.minHeight = _cellSizeArray[cellIndex] - (cellIndex > 0 ? _layoutGroup.spacing : 0);
			else
				layoutElement.minWidth = _cellSizeArray[cellIndex] - (cellIndex > 0 ? _layoutGroup.spacing : 0);

			// add the cell to the active list
			if (listPosition == ListPositionEnum.First)
				_activeCells.Insert(0, cell);
			else
				_activeCells.Add(cell);

			// set the hierarchy position of the cell in the container
			if (listPosition == ListPositionEnum.Last)
				cell.transform.SetSiblingIndex(_container.childCount - 2);
			else if (listPosition == ListPositionEnum.First)
				cell.transform.SetSiblingIndex(1);

			// call the visibility change delegate if available
			cellVisibilityChanged?.Invoke(cell);
		}

		/// <summary>
		/// This function adjusts the two padders that control the first cell's
		/// offset and the overall size of each cell.
		/// </summary>
		private void _SetPadders()
		{
			if (NumberOfItems == 0) return;

			// calculate the size of each padder
			var firstSize = _cellOffsetArray[_activeCellsStartIndex] - _cellSizeArray[_activeCellsStartIndex];
			var lastSize = _cellOffsetArray.Last() - _cellOffsetArray[_activeCellsEndIndex];

			if (_scrollDirection == ScrollDirectionEnum.Vertical)
			{
				// set the first padder and toggle its visibility
				_firstPadder.minHeight = firstSize;
				_firstPadder.gameObject.SetActive(_firstPadder.minHeight > 0);

				// set the last padder and toggle its visibility
				_lastPadder.minHeight = lastSize;
				_lastPadder.gameObject.SetActive(_lastPadder.minHeight > 0);
			}
			else
			{
				// set the first padder and toggle its visibility
				_firstPadder.minWidth = firstSize;
				_firstPadder.gameObject.SetActive(_firstPadder.minWidth > 0);

				// set the last padder and toggle its visibility
				_lastPadder.minWidth = lastSize;
				_lastPadder.gameObject.SetActive(_lastPadder.minWidth > 0);
			}
		}

		/// <summary>
		/// This function is called if the Scroll is scrolled, updating the active list of items
		/// </summary>
		private void _RefreshActive()
		{
			//_refreshActive = false;

			int startIndex;
			int endIndex;
			var velocity = Vector2.zero;

			// if looping, check to see if we scrolled past a trigger
			if (_loop && !_ignoreLoopJump)
			{
				if (_scrollPosition < _loopFirstJumpTrigger)
				{
					velocity = _scrollRect.velocity;
					ScrollPosition = _loopLastScrollPosition - (_loopFirstJumpTrigger - _scrollPosition) + spacing;
					_scrollRect.velocity = velocity;
				}
				else if (_scrollPosition > _loopLastJumpTrigger)
				{
					velocity = _scrollRect.velocity;
					ScrollPosition = _loopFirstScrollPosition + (_scrollPosition - _loopLastJumpTrigger) - spacing;
					_scrollRect.velocity = velocity;
				}
			}

			// get the range of visibile items
			_CalculateCurrentActiveCellRange(out startIndex, out endIndex);

			// if the index hasn't changed, ignore and return
			if (startIndex == _activeCellsStartIndex && endIndex == _activeCellsEndIndex) return;

			// recreate the visibile items
			_ResetVisibleCells();
		}

		/// <summary>
		/// Determines which items can be seen
		/// </summary>
		/// <param name="startIndex">The index of the first cell visible</param>
		/// <param name="endIndex">The index of the last cell visible</param>
		private void _CalculateCurrentActiveCellRange(out int startIndex, out int endIndex)
		{
			startIndex = 0;
			endIndex = 0;

			// get the positions of the Scroll
			var startPosition = _scrollPosition - lookAheadBefore;
			var endPosition = _scrollPosition +
				(_scrollDirection == ScrollDirectionEnum.Vertical
					? rectTransform.rect.height
					: rectTransform.rect.width) +
				lookAheadAfter;

			// calculate each index based on the positions
			startIndex = GetCellIndexAtPosition(startPosition);
			endIndex = GetCellIndexAtPosition(endPosition);
		}

		/// <summary>
		/// Gets the index of a cell at a given position based on a subset range.
		/// This function uses a recursive binary sort to find the index faster.
		/// </summary>
		/// <param name="position">The pixel offset from the start of the Scroll</param>
		/// <param name="startIndex">The first index of the range</param>
		/// <param name="endIndex">The last index of the rnage</param>
		/// <returns></returns>
		private int _GetCellIndexAtPosition(float position, int startIndex, int endIndex)
		{
			// if the range is invalid, then we found our index, return the start index
			if (startIndex >= endIndex) return startIndex;

			// determine the middle point of our binary search
			var middleIndex = (startIndex + endIndex) / 2;

			// if the middle index is greater than the position, then search the last
			// half of the binary tree, else search the first half
			var pad = _scrollDirection == ScrollDirectionEnum.Vertical ? padding.top : padding.left;
			if ((_cellOffsetArray[middleIndex] + pad) >= (position + (pad == 0 ? 0 : 1.00001f)))
				return _GetCellIndexAtPosition(position, startIndex, middleIndex);

			return _GetCellIndexAtPosition(position, middleIndex + 1, endIndex);
		}

		/// <summary>
		/// This event is fired when the user begins dragging on the Scroll.
		/// We can disable looping or snapping while dragging if desired.
		/// <param name="data">The event data for the drag</param>
		/// </summary>
		public void OnBeginDrag(PointerEventData data)
		{
			_dragFingerCount++;
			if (_dragFingerCount > 1) return;

			// capture the snapping and set it to false if desired
			_snapBeforeDrag = snapping;
			if (!snapWhileDragging)
			{
				snapping = false;
			}

			// capture the looping and set it to false if desired
			_loopBeforeDrag = _loop;
			if (!loopWhileDragging)
			{
				_loop = false;
			}

			if (IsTweening && interruptTweeningOnDrag)
			{
				_interruptTween = true;
			}

			beginDrag?.Invoke(data);
		}

		/// <summary>
		/// This event is fired while the user is dragging the ScrollRect.
		/// We use it to capture the drag position that will later be used in the OnEndDrag method.
		/// </summary>
		/// <param name="data">The event data for the drag</param>
		public void OnDrag(PointerEventData data)
		{
			_dragPrevPos = data.position;
		}

		/// <summary>
		/// This event is fired when the user ends dragging on the Scroll.
		/// We can re-enable looping or snapping while dragging if desired.
		/// <param name="data">The event data for the drag</param>
		/// </summary>
		public void OnEndDrag(PointerEventData data)
		{
			_dragFingerCount--;

			if (_dragFingerCount < 0)
				_dragFingerCount = 0;

			// reset the snapping and looping to what it was before the drag
			snapping = _snapBeforeDrag;
			_loop = _loopBeforeDrag;

			if (forceSnapOnEndDrag && snapping && _dragPrevPos == data.position)
			{
				Snap();
			}

			endedDrag?.Invoke(data);
		}

		private void Update()
		{
			if (!_initialized)
				return;

			if (_updateSpacing)
			{
				UpdateSpacing(spacing);
				_reloadDataRequest = false;
			}

			if (_reloadDataRequest)
			{
				// if the reload flag is true, then reload the data
				ReloadData();
			}

			// if the scroll rect size has changed and looping is on,
			// or the loop setting has changed, then we need to resize
			if (
				(_loop && _lastScrollRectSize != ScrollRectSize)
				||
				(_loop != _lastLoop)
			)
			{
				_Resize(true);
				_lastScrollRectSize = ScrollRectSize;

				_lastLoop = _loop;
			}

			// update the scroll bar visibility if it has changed
			if (_lastScrollbarVisibility != _scrollbarVisibility)
			{
				ScrollbarVisibility = _scrollbarVisibility;
				_lastScrollbarVisibility = _scrollbarVisibility;
			}

			// determine if the Scroll has started or stopped scrolling
			// and call the delegate if so.
			if (LinearVelocity != 0 && !IsScrolling)
			{
				IsScrolling = true;
				scrollScrollingChanged?.Invoke(this, true);
			}
			else if (LinearVelocity == 0 && IsScrolling)
			{
				IsScrolling = false;
				scrollScrollingChanged?.Invoke(this, false);
			}

			if (_cachedTween != null &&
			    gameObject.activeInHierarchy)
			{
				_cachedTween.Invoke();
				_cachedTween = null;
			}
		}

		/// <summary>
		/// Reacts to changes in the inspector
		/// </summary>
		protected internal override void OnValidate()
		{
			base.OnValidate();

			// if spacing changed, update it
			if (_initialized && Math.Abs(spacing - _layoutGroup.spacing) > float.Epsilon)
				_updateSpacing = true;

			if (!_scrollRect)
				return;

			_scrollRect.vertical = _scrollDirection == ScrollDirectionEnum.Vertical;
			_scrollRect.horizontal = _scrollDirection == ScrollDirectionEnum.Horizontal;
		}

		/// <summary>
		/// Fired at the end of the frame.
		/// </summary>
		private void LateUpdate()
		{
			// if maxVelocity is not zero, we can set the speed cap based on the scroll direction
			if (maxVelocity > 0)
			{
				if (_scrollDirection == ScrollDirectionEnum.Horizontal)
				{
					Velocity = new Vector2(Mathf.Clamp(Mathf.Abs(Velocity.x), 0, maxVelocity) * Mathf.Sign(Velocity.x),
						Velocity.y);
				}
				else
				{
					Velocity = new Vector2(Velocity.x,
						Mathf.Clamp(Mathf.Abs(Velocity.y), 0, maxVelocity) * Mathf.Sign(Velocity.y));
				}
			}
		}

		protected override void OnEnable()
		{
			base.OnEnable();

			_forceUpdatePaddingRequest = true;

			if (_valueChangedSubscribed)
				return;

			// when the Scroll is enabled, add a listener to the onValueChanged handler
			_scrollRect.onValueChanged.AddListener(_ScrollRect_OnValueChanged);
			_valueChangedSubscribed = true;
		}

		protected override void OnDisable()
		{
			base.OnDisable();

			if (!_valueChangedSubscribed)
				return;

			// when the Scroll is disabled, remove the listener
			_scrollRect.onValueChanged.RemoveListener(_ScrollRect_OnValueChanged);
			_valueChangedSubscribed = false;
		}

		/// <summary>
		/// Handler for when the Scroll changes value
		/// </summary>
		/// <param name="val">The scroll rect's value</param>
		private void _ScrollRect_OnValueChanged(Vector2 val)
		{
			if (!_initialized)
				return;

			// set the internal scroll position
			if (_scrollDirection == ScrollDirectionEnum.Vertical)
				_scrollPosition = (1f - val.y) * ActiveCellsSize;
			else
				_scrollPosition = val.x * ActiveCellsSize;
			//_refreshActive = true;
			_scrollPosition = Mathf.Clamp(_scrollPosition, 0, ActiveCellsSize);

			// call the handler if it exists
			scrollScrolled?.Invoke(this, val, _scrollPosition);

			// if the snapping is turned on, handle it
			if (snapping && !_snapJumping)
			{
				// if the speed has dropped below the threshhold velocity
				if (Mathf.Abs(LinearVelocity) <= snapVelocityThreshold && LinearVelocity != 0)
				{
					// Make sure the Scroll is not on the boundary if not looping
					var normalized = NormalizedScrollPosition;

					if (_loop || (!_loop && normalized is > 0 and < 1))
						// Call the snap function
						Snap();
				}
			}

			_RefreshActive();
		}

		/// <summary>
		/// This is fired by the tweener when the snap tween is completed
		/// </summary>
		private void SnapJumpComplete()
		{
			// reset the snap jump to false and restore the inertia state
			_snapJumping = false;
			_scrollRect.inertia = _snapInertia;

			UIScrollItemLayout itemLayout = null;
			for (var i = 0; i < _activeCells.Count; i++)
			{
				if (_activeCells[i].DataIndex == _snapDataIndex)
				{
					itemLayout = _activeCells[i];
					break;
				}
			}

			// fire the Scroll snapped delegate
			scrollSnapped?.Invoke(this, _snapCellIndex, _snapDataIndex, itemLayout);
		}

		[ContextMenu("Reset ScrollRect")]
		protected override void Reset()
		{
			_scrollRect = GetComponent<ScrollRect>();

			base.Reset();
		}

		#endregion Private

		#region Tweening

		/// <summary>
		/// The easing type
		/// </summary>
		///
		/// <remarks>
		/// https://easings.net/
		/// </remarks>
		public enum TweenType
		{
			immediate,
			linear,
			spring,
			easeInQuad,
			easeOutQuad,
			easeInOutQuad,
			easeInCubic,
			easeOutCubic,
			easeInOutCubic,
			easeInQuart,
			easeOutQuart,
			easeInOutQuart,
			easeInQuint,
			easeOutQuint,
			easeInOutQuint,
			easeInSine,
			easeOutSine,
			easeInOutSine,
			easeInExpo,
			easeOutExpo,
			easeInOutExpo,
			easeInCirc,
			easeOutCirc,
			easeInOutCirc,
			easeInBounce,
			easeOutBounce,
			easeInOutBounce,
			easeInBack,
			easeOutBack,
			easeInOutBack,
			easeInElastic,
			easeOutElastic,
			easeInOutElastic
		}

		private float _tweenTimeLeft;
		private Action _tweenComplete;

		private RectOffset _defaultPadding;

		/// <summary>
		/// Moves the scroll position over time between two points given an easing function. When the
		/// tween is complete it will fire the jumpComplete delegate.
		/// </summary>
		/// <param name="tweenType">The type of easing to use</param>
		/// <param name="time">The amount of time to interpolate</param>
		/// <param name="start">The starting scroll position</param>
		/// <param name="end">The ending scroll position</param>
		/// <param name="jumpComplete">The action to fire when the tween is complete</param>
		/// <returns></returns>
		IEnumerator TweenPosition(TweenType tweenType, float time, float start, float end, Action tweenComplete,
			bool forceCalculateRange)
		{
			_tweenComplete = tweenComplete;

			if (!(tweenType == TweenType.immediate || time == 0))
			{
				// zero out the velocity
				_scrollRect.velocity = Vector2.zero;

				// fire the delegate for the tween start
				IsTweening = true;
				scrollTweeningChanged?.Invoke(this, true);

				_tweenTimeLeft = 0;
				var newPosition = 0f;

				// while the tween has time left, use an easing function
				while (_tweenTimeLeft < time)
				{
					switch (tweenType)
					{
						case TweenType.linear:
							newPosition = linear(start, end, (_tweenTimeLeft / time));
							break;
						case TweenType.spring:
							newPosition = spring(start, end, (_tweenTimeLeft / time));
							break;
						case TweenType.easeInQuad:
							newPosition = easeInQuad(start, end, (_tweenTimeLeft / time));
							break;
						case TweenType.easeOutQuad:
							newPosition = easeOutQuad(start, end, (_tweenTimeLeft / time));
							break;
						case TweenType.easeInOutQuad:
							newPosition = easeInOutQuad(start, end, (_tweenTimeLeft / time));
							break;
						case TweenType.easeInCubic:
							newPosition = easeInCubic(start, end, (_tweenTimeLeft / time));
							break;
						case TweenType.easeOutCubic:
							newPosition = easeOutCubic(start, end, (_tweenTimeLeft / time));
							break;
						case TweenType.easeInOutCubic:
							newPosition = easeInOutCubic(start, end, (_tweenTimeLeft / time));
							break;
						case TweenType.easeInQuart:
							newPosition = easeInQuart(start, end, (_tweenTimeLeft / time));
							break;
						case TweenType.easeOutQuart:
							newPosition = easeOutQuart(start, end, (_tweenTimeLeft / time));
							break;
						case TweenType.easeInOutQuart:
							newPosition = easeInOutQuart(start, end, (_tweenTimeLeft / time));
							break;
						case TweenType.easeInQuint:
							newPosition = easeInQuint(start, end, (_tweenTimeLeft / time));
							break;
						case TweenType.easeOutQuint:
							newPosition = easeOutQuint(start, end, (_tweenTimeLeft / time));
							break;
						case TweenType.easeInOutQuint:
							newPosition = easeInOutQuint(start, end, (_tweenTimeLeft / time));
							break;
						case TweenType.easeInSine:
							newPosition = easeInSine(start, end, (_tweenTimeLeft / time));
							break;
						case TweenType.easeOutSine:
							newPosition = easeOutSine(start, end, (_tweenTimeLeft / time));
							break;
						case TweenType.easeInOutSine:
							newPosition = easeInOutSine(start, end, (_tweenTimeLeft / time));
							break;
						case TweenType.easeInExpo:
							newPosition = easeInExpo(start, end, (_tweenTimeLeft / time));
							break;
						case TweenType.easeOutExpo:
							newPosition = easeOutExpo(start, end, (_tweenTimeLeft / time));
							break;
						case TweenType.easeInOutExpo:
							newPosition = easeInOutExpo(start, end, (_tweenTimeLeft / time));
							break;
						case TweenType.easeInCirc:
							newPosition = easeInCirc(start, end, (_tweenTimeLeft / time));
							break;
						case TweenType.easeOutCirc:
							newPosition = easeOutCirc(start, end, (_tweenTimeLeft / time));
							break;
						case TweenType.easeInOutCirc:
							newPosition = easeInOutCirc(start, end, (_tweenTimeLeft / time));
							break;
						case TweenType.easeInBounce:
							newPosition = easeInBounce(start, end, (_tweenTimeLeft / time));
							break;
						case TweenType.easeOutBounce:
							newPosition = easeOutBounce(start, end, (_tweenTimeLeft / time));
							break;
						case TweenType.easeInOutBounce:
							newPosition = easeInOutBounce(start, end, (_tweenTimeLeft / time));
							break;
						case TweenType.easeInBack:
							newPosition = easeInBack(start, end, (_tweenTimeLeft / time));
							break;
						case TweenType.easeOutBack:
							newPosition = easeOutBack(start, end, (_tweenTimeLeft / time));
							break;
						case TweenType.easeInOutBack:
							newPosition = easeInOutBack(start, end, (_tweenTimeLeft / time));
							break;
						case TweenType.easeInElastic:
							newPosition = easeInElastic(start, end, (_tweenTimeLeft / time));
							break;
						case TweenType.easeOutElastic:
							newPosition = easeOutElastic(start, end, (_tweenTimeLeft / time));
							break;
						case TweenType.easeInOutElastic:
							newPosition = easeInOutElastic(start, end, (_tweenTimeLeft / time));
							break;
					}

					// set the scroll position to the tweened position
					ScrollPosition = newPosition;

					// increase the time elapsed
					_tweenTimeLeft += Time.unscaledDeltaTime;

					yield return null;
				}
			}

			if (_interruptTween)
			{
				// the tween was interrupted so we need to set the flag and call the tweening changed delegate.
				// note that we don't set the end position or call the tweenComplete delegate.

				_interruptTween = false;

				// reset the snapJumping and scroller inertia
				_snapJumping = false;
				_scrollRect.inertia = _snapInertia;

				IsTweening = false;
				scrollTweeningChanged?.Invoke(this, false);
			}
			else

			{
				// the time has expired, so we make sure the final scroll position
				// is the actual end position.
				ScrollPosition = end;

				if (forceCalculateRange || tweenType == TweenType.immediate || time == 0)
				{
					_RefreshActive();
				}

				// the tween jump is complete, so we fire the delegate
				tweenComplete?.Invoke();
				_tweenComplete = null;

				// fire the delegate for the tween ending
				IsTweening = false;
				scrollTweeningChanged?.Invoke(this, false);

				_cachedRoutine = null;
			}
		}

		private float linear(float start, float end, float val)
		{
			return Mathf.Lerp(start, end, val);
		}

		private static float spring(float start, float end, float val)
		{
			val = Mathf.Clamp01(val);
			val = (Mathf.Sin(val * Mathf.PI * (0.2f + 2.5f * val * val * val)) * Mathf.Pow(1f - val, 2.2f) + val) *
				(1f + (1.2f * (1f - val)));
			return start + (end - start) * val;
		}

		private static float easeInQuad(float start, float end, float val)
		{
			end -= start;
			return end * val * val + start;
		}

		private static float easeOutQuad(float start, float end, float val)
		{
			end -= start;
			return -end * val * (val - 2) + start;
		}

		private static float easeInOutQuad(float start, float end, float val)
		{
			val /= .5f;
			end -= start;
			if (val < 1) return end / 2 * val * val + start;
			val--;
			return -end / 2 * (val * (val - 2) - 1) + start;
		}

		private static float easeInCubic(float start, float end, float val)
		{
			end -= start;
			return end * val * val * val + start;
		}

		private static float easeOutCubic(float start, float end, float val)
		{
			val--;
			end -= start;
			return end * (val * val * val + 1) + start;
		}

		private static float easeInOutCubic(float start, float end, float val)
		{
			val /= .5f;
			end -= start;
			if (val < 1) return end / 2 * val * val * val + start;
			val -= 2;
			return end / 2 * (val * val * val + 2) + start;
		}

		private static float easeInQuart(float start, float end, float val)
		{
			end -= start;
			return end * val * val * val * val + start;
		}

		private static float easeOutQuart(float start, float end, float val)
		{
			val--;
			end -= start;
			return -end * (val * val * val * val - 1) + start;
		}

		private static float easeInOutQuart(float start, float end, float val)
		{
			val /= .5f;
			end -= start;
			if (val < 1) return end / 2 * val * val * val * val + start;
			val -= 2;
			return -end / 2 * (val * val * val * val - 2) + start;
		}

		private static float easeInQuint(float start, float end, float val)
		{
			end -= start;
			return end * val * val * val * val * val + start;
		}

		private static float easeOutQuint(float start, float end, float val)
		{
			val--;
			end -= start;
			return end * (val * val * val * val * val + 1) + start;
		}

		private static float easeInOutQuint(float start, float end, float val)
		{
			val /= .5f;
			end -= start;
			if (val < 1) return end / 2 * val * val * val * val * val + start;
			val -= 2;
			return end / 2 * (val * val * val * val * val + 2) + start;
		}

		private static float easeInSine(float start, float end, float val)
		{
			end -= start;
			return -end * Mathf.Cos(val / 1 * (Mathf.PI / 2)) + end + start;
		}

		private static float easeOutSine(float start, float end, float val)
		{
			end -= start;
			return end * Mathf.Sin(val / 1 * (Mathf.PI / 2)) + start;
		}

		private static float easeInOutSine(float start, float end, float val)
		{
			end -= start;
			return -end / 2 * (Mathf.Cos(Mathf.PI * val / 1) - 1) + start;
		}

		private static float easeInExpo(float start, float end, float val)
		{
			end -= start;
			return end * Mathf.Pow(2, 10 * (val / 1 - 1)) + start;
		}

		private static float easeOutExpo(float start, float end, float val)
		{
			end -= start;
			return end * (-Mathf.Pow(2, -10 * val / 1) + 1) + start;
		}

		private static float easeInOutExpo(float start, float end, float val)
		{
			val /= .5f;
			end -= start;
			if (val < 1) return end / 2 * Mathf.Pow(2, 10 * (val - 1)) + start;
			val--;
			return end / 2 * (-Mathf.Pow(2, -10 * val) + 2) + start;
		}

		private static float easeInCirc(float start, float end, float val)
		{
			end -= start;
			return -end * (Mathf.Sqrt(1 - val * val) - 1) + start;
		}

		private static float easeOutCirc(float start, float end, float val)
		{
			val--;
			end -= start;
			return end * Mathf.Sqrt(1 - val * val) + start;
		}

		private static float easeInOutCirc(float start, float end, float val)
		{
			val /= .5f;
			end -= start;
			if (val < 1) return -end / 2 * (Mathf.Sqrt(1 - val * val) - 1) + start;
			val -= 2;
			return end / 2 * (Mathf.Sqrt(1 - val * val) + 1) + start;
		}

		private static float easeInBounce(float start, float end, float val)
		{
			end -= start;
			var d = 1f;
			return end - easeOutBounce(0, end, d - val) + start;
		}

		private static float easeOutBounce(float start, float end, float val)
		{
			val /= 1f;
			end -= start;
			if (val < (1 / 2.75f))
			{
				return end * (7.5625f * val * val) + start;
			}
			else if (val < (2 / 2.75f))
			{
				val -= (1.5f / 2.75f);
				return end * (7.5625f * (val) * val + .75f) + start;
			}
			else if (val < (2.5 / 2.75))
			{
				val -= (2.25f / 2.75f);
				return end * (7.5625f * (val) * val + .9375f) + start;
			}
			else
			{
				val -= (2.625f / 2.75f);
				return end * (7.5625f * (val) * val + .984375f) + start;
			}
		}

		private static float easeInOutBounce(float start, float end, float val)
		{
			end -= start;
			var d = 1f;
			if (val < d / 2) return easeInBounce(0, end, val * 2) * 0.5f + start;
			else return easeOutBounce(0, end, val * 2 - d) * 0.5f + end * 0.5f + start;
		}

		private static float easeInBack(float start, float end, float val)
		{
			end -= start;
			val /= 1;
			var s = 1.70158f;
			return end * (val) * val * ((s + 1) * val - s) + start;
		}

		private static float easeOutBack(float start, float end, float val)
		{
			var s = 1.70158f;
			end -= start;
			val = (val / 1) - 1;
			return end * ((val) * val * ((s + 1) * val + s) + 1) + start;
		}

		private static float easeInOutBack(float start, float end, float val)
		{
			var s = 1.70158f;
			end -= start;
			val /= .5f;
			if ((val) < 1)
			{
				s *= (1.525f);
				return end / 2 * (val * val * (((s) + 1) * val - s)) + start;
			}

			val -= 2;
			s *= (1.525f);
			return end / 2 * ((val) * val * (((s) + 1) * val + s) + 2) + start;
		}

		private static float easeInElastic(float start, float end, float val)
		{
			end -= start;

			var d = 1f;
			var p = d * .3f;
			float s = 0;
			float a = 0;

			if (val == 0) return start;
			val = val / d;
			if (val == 1) return start + end;

			if (a == 0f || a < Mathf.Abs(end))
			{
				a = end;
				s = p / 4;
			}
			else
			{
				s = p / (2 * Mathf.PI) * Mathf.Asin(end / a);
			}

			val = val - 1;
			return -(a * Mathf.Pow(2, 10 * val) * Mathf.Sin((val * d - s) * (2 * Mathf.PI) / p)) + start;
		}

		private static float easeOutElastic(float start, float end, float val)
		{
			end -= start;

			var d = 1f;
			var p = d * .3f;
			float s = 0;
			float a = 0;

			if (val == 0) return start;

			val = val / d;
			if (val == 1) return start + end;

			if (a == 0f || a < Mathf.Abs(end))
			{
				a = end;
				s = p / 4;
			}
			else
			{
				s = p / (2 * Mathf.PI) * Mathf.Asin(end / a);
			}

			return (a * Mathf.Pow(2, -10 * val) * Mathf.Sin((val * d - s) * (2 * Mathf.PI) / p) + end + start);
		}

		private static float easeInOutElastic(float start, float end, float val)
		{
			end -= start;

			var d = 1f;
			var p = d * .3f;
			float s = 0;
			float a = 0;

			if (val == 0) return start;

			val = val / (d / 2);
			if (val == 2) return start + end;

			if (a == 0f || a < Mathf.Abs(end))
			{
				a = end;
				s = p / 4;
			}
			else
			{
				s = p / (2 * Mathf.PI) * Mathf.Asin(end / a);
			}

			if (val < 1)
			{
				val = val - 1;
				return -0.5f * (a * Mathf.Pow(2, 10 * val) * Mathf.Sin((val * d - s) * (2 * Mathf.PI) / p)) + start;
			}

			val = val - 1;
			return a * Mathf.Pow(2, -10 * val) * Mathf.Sin((val * d - s) * (2 * Mathf.PI) / p) * 0.5f + end + start;
		}

		#endregion Tweening
	}

	#region Delegates

	/// <summary>
	/// This delegate handles the visibility changes of cells
	/// </summary>
	/// <param name="cell">The cell that changed visibility</param>
	public delegate void CellVisibilityChangedDelegate(UIScrollItemLayout cell);

	/// <summary>
	/// This delegate will be fired just before the cell is recycled
	/// </summary>
	/// <param name="cell"></param>
	public delegate void CellWillRecycleDelegate(UIScrollItemLayout cell);

	/// <summary>
	/// This delegate handles the scrolling callback of the ScrollRect.
	/// </summary>
	/// <param name="layout">The Scroll that called the delegate</param>
	/// <param name="val">The scroll value of the scroll rect</param>
	/// <param name="scrollPosition">The scroll position in pixels from the start of the Scroll</param>
	public delegate void ScrollScrolledDelegate(UIScrollLayout layout, Vector2 val,
		float scrollPosition);

	/// <summary>
	/// This delegate handles the snapping of the Scroll.
	/// </summary>
	/// <param name="layout">The Scroll that called the delegate</param>
	/// <param name="itemIndex">The index of the item view snapped on (this may be different than the data index in case of looping)</param>
	/// <param name="dataIndex">The index of the data the view snapped on</param>
	public delegate void ScrollSnappedDelegate(UIScrollLayout layout, int itemIndex, int dataIndex,
		UIScrollItemLayout itemLayout);

	/// <summary>
	/// This delegate handles the began snapping of the Scroll.
	/// </summary>
	/// <param name="layout">The Scroll that called the delegate</param>
	/// <param name="itemIndex">The index of the item view began snapped</param>
	public delegate void ScrollBeganSnappingDelegate(UIScrollLayout layout, int itemIndex);

	/// <summary>
	/// This delegate handles the change in state of the Scroll (scrolling or not scrolling)
	/// </summary>
	/// <param name="layout">The Scroll that changed state</param>
	/// <param name="scrolling">Whether or not the Scroll is scrolling</param>
	public delegate void ScrollScrollingChangedDelegate(UIScrollLayout layout, bool scrolling);

	/// <summary>
	/// This delegate handles the change in state of the Scroll (jumping or not jumping)
	/// </summary>
	/// <param name="layout">The Scroll that changed state</param>
	/// <param name="tweening">Whether or not the Scroll is tweening</param>
	public delegate void ScrollTweeningChangedDelegate(UIScrollLayout layout, bool tweening);

	/// <summary>
	/// This delegate is called when a item view is created for the first time (not reused)
	/// </summary>
	/// <param name="layout">The Scroll that created the cell</param>
	/// <param name="cell">The cell that was created</param>
	public delegate void CellInstantiatedDelegate(UIScrollLayout layout, UIScrollItemLayout cell);

	/// <summary>
	/// This delegate is called when a cell is reused from the recycled cell list
	/// </summary>
	/// <param name="layout">The Scroll that reused the cell</param>
	/// <param name="cell">The cell that was resused</param>
	public delegate void CellReusedDelegate(UIScrollLayout layout, UIScrollItemLayout cell);

	/// <summary>
	/// This delegate is called when a begin drag...
	/// </summary>
	/// <param name="PointerEventData">The Scroll that reused the cell</param>
	public delegate void BeginDragDelegate(PointerEventData eventData);

	/// <summary>
	/// This delegate is called when a end drag...
	/// </summary>
	/// <param name="PointerEventData">The Scroll that reused the cell</param>
	public delegate void EndDragDelegate(PointerEventData eventData);

	#endregion
}
