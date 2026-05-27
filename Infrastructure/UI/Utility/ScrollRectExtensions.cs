using DG.Tweening;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public enum ScrollRectTargetAlignment
	{
		Start,
		StartWithContentPadding,

		/// <summary>
		/// Центрируем относительно Viewport
		/// </summary>
		Center,
	}

	public static class ScrollRectExtensions
	{
		public static Tween MoveTo([CanBeNull] this ScrollRect scrollRect, float normalizedPos, float duration)
		{
			if (scrollRect == null)
				return null;

			if (scrollRect.horizontal)
				return scrollRect.DOHorizontalNormalizedPos(normalizedPos, duration);
			else
				return scrollRect.DOVerticalNormalizedPos(1 - normalizedPos, duration);
		}

		public static Tween MoveTo([CanBeNull] this ScrollRect scrollRect, Vector2 normalizedPos, float duration)
		{
			if (scrollRect == null)
				return null;

			return scrollRect.DONormalizedPos(normalizedPos, duration);
		}

		// TODO: пока работает только в одном направлении, если будет scrollRect с двумя осями нужно доработать
		public static Tween MoveTo([CanBeNull] this ScrollRect scrollRect,
			RectTransform target,
			float duration,
			ScrollRectTargetAlignment alignment = ScrollRectTargetAlignment.Start,
			float offset = 0,
			bool includeChildrenInBounds = false)
		{
			if (scrollRect == null)
				return null;

			var content = scrollRect.content;
			if (content == null)
				return null;

			var viewport = scrollRect.viewport != null
				? scrollRect.viewport
				: scrollRect.GetComponent<RectTransform>();

			if (viewport == null)
				return null;

			var isHorizontal = scrollRect.horizontal;
			var normalizedPos = GetNormalizedPos(content, viewport, target, alignment, offset, includeChildrenInBounds, isHorizontal);

			if (scrollRect.TryGetComponent(out ScrollRectCapturer capturer))
				capturer.SetNormalizePosition(new Vector2
				(
					isHorizontal ? normalizedPos : 0,
					isHorizontal ? 0 : normalizedPos
				));

			return isHorizontal
				? scrollRect.DOHorizontalNormalizedPos(normalizedPos, Mathf.Max(0, duration))
				: scrollRect.DOVerticalNormalizedPos(normalizedPos, Mathf.Max(0, duration));
		}

		private static float GetNormalizedPos(RectTransform content,
			RectTransform viewport,
			RectTransform target,
			ScrollRectTargetAlignment alignment,
			float offset = 0,
			bool includeChildrenInBounds = true,
			bool horizontalOrVertical = true)
		{
			var hiddenSize = horizontalOrVertical
				? content.rect.width - viewport.rect.width
				: content.rect.height - viewport.rect.height;
			if (hiddenSize <= 0)
				return horizontalOrVertical ? 0 : 1;

			var bounds = GetBounds(content, target, includeChildrenInBounds);
			var targetOffset = horizontalOrVertical
				? GetHorizontalOffset(content, viewport, target, bounds, alignment)
				: GetVerticalOffset(content, viewport, target, bounds, alignment);

			var normalizedPos = Mathf.Clamp01((targetOffset + offset) / hiddenSize);
			normalizedPos = horizontalOrVertical ? normalizedPos : 1 - normalizedPos;
			return normalizedPos;
		}

		private static float GetHorizontalOffset(RectTransform content,
			RectTransform viewport,
			RectTransform target,
			Bounds bounds,
			ScrollRectTargetAlignment alignment)
		{
			return alignment switch
			{
				ScrollRectTargetAlignment.Center => GetHorizontalCenterOffset(content, viewport, target),
				ScrollRectTargetAlignment.StartWithContentPadding => GetStartOffset(content, bounds) - GetStartOffsetByLayoutGroup(content),
				_ => GetStartOffset(content, bounds)
			};
		}

		private static float GetVerticalOffset(RectTransform content,
			RectTransform viewport,
			RectTransform target,
			Bounds bounds,
			ScrollRectTargetAlignment alignment)
		{
			return alignment switch
			{
				ScrollRectTargetAlignment.Center => GetVerticalCenterOffset(content, viewport, target),
				ScrollRectTargetAlignment.StartWithContentPadding => GetStartOffset(content, bounds, false) -
					GetStartOffsetByLayoutGroup(content, false),
				_ => GetStartOffset(content, bounds, false)
			};
		}

		private static float GetStartOffset(RectTransform content,
			Bounds bounds,
			bool horizontalOrVertical = true)
		{
			return horizontalOrVertical
				? bounds.min.x - content.rect.xMin
				: content.rect.yMax - bounds.max.y;
		}

		private static float GetStartOffsetByLayoutGroup(RectTransform content, bool horizontalOrVertical = true)
		{
			if (!content.TryGetComponent<HorizontalOrVerticalLayoutGroup>(out var layoutGroup))
				return 0;

			return horizontalOrVertical ? layoutGroup.padding.left : layoutGroup.padding.top;
		}

		private static float GetHorizontalCenterOffset(RectTransform content, RectTransform viewport, RectTransform target)
		{
			var targetCenterInViewport = GetTargetCenterInViewport(viewport, target);
			var deltaToViewportCenter = targetCenterInViewport.x - viewport.rect.center.x;
			var currentOffset = content.rect.xMin - content.anchoredPosition.x;
			return currentOffset + deltaToViewportCenter;
		}

		private static float GetVerticalCenterOffset(RectTransform content, RectTransform viewport, RectTransform target)
		{
			var targetCenterInViewport = GetTargetCenterInViewport(viewport, target);
			var deltaToViewportCenter = viewport.rect.center.y - targetCenterInViewport.y;
			var currentOffset = content.anchoredPosition.y + content.rect.yMax;
			return currentOffset + deltaToViewportCenter;
		}

		private static Vector2 GetTargetCenterInViewport(RectTransform viewport, RectTransform target)
		{
			var worldCenter = target.TransformPoint(target.rect.center);
			return viewport.InverseTransformPoint(worldCenter);
		}

		private static Bounds GetBounds(RectTransform content, RectTransform target, bool includeChildrenInBounds)
		{
			if (includeChildrenInBounds)
				return UnityEngine.RectTransformUtility.CalculateRelativeRectTransformBounds(content, target);

			var worldCorners = new Vector3[4];
			target.GetWorldCorners(worldCorners);

			var min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
			var max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

			for (int i = 0; i < worldCorners.Length; i++)
			{
				var localCorner = content.InverseTransformPoint(worldCorners[i]);
				min = Vector3.Min(min, localCorner);
				max = Vector3.Max(max, localCorner);
			}

			var bounds = new Bounds();
			bounds.SetMinMax(min, max);
			return bounds;
		}
	}
}
