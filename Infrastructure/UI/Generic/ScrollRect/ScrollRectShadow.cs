using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
	//TODO: Loop как обрабатывать...
	[RequireComponent(typeof(ScrollRect))]
	public class ScrollRectShadow : UIBehaviour
	{
		[ReadOnly]
		public ScrollRect scrollRect;

		[Space]
		public Shadow[] shadows;

		private float _topPoint;
		private float _bottomPoint;
		private float _leftPoint;
		private float _rightPoint;

		private bool _contentSmallerThanViewportX;
		private bool _contentSmallerThanViewportY;

		protected override void OnEnable()
		{
			base.OnEnable();

			UpdateShadows();
			scrollRect.onValueChanged.AddListener(OnValueChanged);
		}

		protected override void OnDisable()
		{
			base.OnDisable();

			scrollRect.onValueChanged.RemoveListener(OnValueChanged);
		}

		private void OnValueChanged(Vector2 _) => UpdateShadows();

		protected override void OnRectTransformDimensionsChange() => UpdateShadows();

		private void UpdateShadows()
		{
			RecalculatePoints();

			foreach (var shadow in shadows)
				UpdateShadow(shadow);
		}

		private void UpdateShadow(Shadow shadow)
		{
			float alpha = 0;

			var graphic = shadow.graphic;
			var rect = graphic.rectTransform.rect;

			var size = 0f;
			var targetPoint = 0f;
			var contentSmallerThanViewport = true;

			switch (shadow.edge)
			{
				case Edge.Top:
					size = rect.height;
					targetPoint = _topPoint;
					contentSmallerThanViewport = _contentSmallerThanViewportY;

					break;
				case Edge.Bottom:
					size = rect.height;
					targetPoint = _bottomPoint;
					contentSmallerThanViewport = _contentSmallerThanViewportY;

					break;
				case Edge.Left:
					size = rect.width;
					targetPoint = _leftPoint;
					contentSmallerThanViewport = _contentSmallerThanViewportX;

					break;

				case Edge.Right:
					size = rect.width;
					targetPoint = _rightPoint;
					contentSmallerThanViewport = _contentSmallerThanViewportX;

					break;
			}

			if (contentSmallerThanViewport)
			{
				if (targetPoint < 0)
				{
					var abs = Mathf.Abs(targetPoint);
					alpha = abs / size;
				}
			}
			else
			{
				alpha = targetPoint / size;
			}

			alpha = Mathf.Clamp(alpha, 0f, 1f);
			alpha *= shadow.multiplier;

			var clampedAlpha = Mathf.Clamp(shadow.reverseAlpha ? 1 - alpha : alpha, 0, 1);
			graphic.SetAlpha(clampedAlpha);
		}

		private void RecalculatePoints()
		{
			CalculateStartAndEndPoint(false,
				out _topPoint,
				out _bottomPoint,
				out _contentSmallerThanViewportY);

			//TODO: Где-то косяк в расчете по вертикали...
			if (!_contentSmallerThanViewportY)
				(_topPoint, _bottomPoint) = (_bottomPoint, _topPoint);

			CalculateStartAndEndPoint(true,
				out _leftPoint,
				out _rightPoint,
				out _contentSmallerThanViewportX);
		}

		private void CalculateStartAndEndPoint(
			bool isHorizontal,
			out float startPoint,
			out float endPoint,
			out bool contentSmallerThanViewport)
		{
			var viewport = scrollRect.viewport;
			var content = scrollRect.content;

			var viewportSize = isHorizontal ? viewport.rect.width : viewport.rect.height;
			var contentSize = isHorizontal ? content.rect.width : content.rect.height;

			var normalizedPosition = isHorizontal ? scrollRect.normalizedPosition.x : scrollRect.normalizedPosition.y;

			var point = normalizedPosition * contentSize;

			contentSmallerThanViewport = viewportSize >= contentSize;

			if (contentSmallerThanViewport)
			{
				if (isHorizontal)
					point = content.anchoredPosition.x;
				else
					point = -content.anchoredPosition.y;
			}

			startPoint = point;
			var offset = contentSmallerThanViewport ? viewportSize - contentSize : contentSize;
			endPoint = offset - startPoint;
		}

		[Serializable]
		public class Shadow
		{
			public Edge edge;
			public Graphic graphic;
			public bool reverseAlpha;
			public float multiplier = 1;
		}

		public enum Edge
		{
			Top,
			Bottom,
			Left,
			Right,
		}

#if UNITY_EDITOR
		protected override void Reset()
		{
			base.Reset();

			scrollRect = GetComponent<ScrollRect>();
		}
#endif
	}
}
