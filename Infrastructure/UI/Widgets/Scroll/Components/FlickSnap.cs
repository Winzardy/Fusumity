using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.Scroll.FlickSnap
{
	/// <summary>
	/// Добавляет к скроллу поведения "Flick Snap", есть критичные различия от обычного "Snapping",
	///
	/// Поведение "Flick Snap" когда кроме обычного Snapping элемента, по свайпу нужно переместиться к следующему или предыдущему элементу в шаг 1.
	/// </summary>
	public class FlickSnap : UIScrollLayoutComponent, IBeginDragHandler, IEndDragHandler
	{
		public UIScrollLayout.TweenType snapTweenType;
		public float snapTweenTime;

		[Space, Tooltip("Порог до которого ничего не будет происходить)")]
		public float threshold = 75;

		private bool _isDragging = false;
		private Vector2 _dragStartPosition = Vector2.zero;
		private int? _currentIndex;

		public void OnBeginDrag(PointerEventData data)
		{
			if (_layout.IsTweening)
				return;

			_isDragging = true;

			_dragStartPosition = data.position;

			if(!_currentIndex.HasValue)
				_currentIndex = _layout.StartDataIndex;
		}

		public void OnEndDrag(PointerEventData data)
		{
			if (!_isDragging)
				return;

			_isDragging = false;

			var offset = data.position - _dragStartPosition;
			var delta = _layout.ScrollDirection == UIScrollLayout.ScrollDirectionEnum.Vertical ? offset.y : -offset.x;

			if (delta == 0)
				return;

			var index = _currentIndex!.Value;

			if (Mathf.Abs(delta) > threshold)
				index = delta < 0 ? index - 1 : index + 1;

			var lastIndex = _layout.NumberOfItems - 1;

			index = Mathf.Clamp(index, 0, lastIndex);
			_layout.JumpToDataIndex(index, tweenType: snapTweenType, tweenTime: snapTweenTime);
			_currentIndex = index;
		}
	}
}
