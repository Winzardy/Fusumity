using System;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using JetBrains.Annotations;
using Sapientia;
using Sirenix.OdinInspector;
using UnityEngine;

namespace ZenoTween.Participant.Tweens
{
	[Serializable]
	[TypeRegistryItem(Icon = SdfIconType.ArrowsMove, CategoryPath = CATEGORY_PATH)]
	public class RectTransformSizeAnimationTween : AnimationTween
	{
		public enum ValueMode
		{
			RectTransform = 0,
			Vector2 = 1
		}

		[NotNull]
		public RectTransform root;

		[LabelText("To Mode")]
		public ValueMode toMode = ValueMode.RectTransform;

		[ShowIf(nameof(toMode), ValueMode.RectTransform)]
		public RectTransform to;

		[ShowIf(nameof(toMode), ValueMode.Vector2)]
		[InlineButton(nameof(SetCurrentForToValue), "Current")]
		public Vector2 toValue;

		[LabelText("From Mode")]
		public ValueMode fromMode = ValueMode.RectTransform;

		[ShowIf(nameof(fromMode), ValueMode.RectTransform)]
		public Toggle<RectTransform> from;

		[ShowIf(nameof(fromMode), ValueMode.Vector2)]
		[InlineButton(nameof(SetCurrentForFromValue), "Current")]
		public Toggle<Vector2> fromValue;

		[LabelText("Width Clamp (X)")]
		[Tooltip("Optional min/max limits for width while tween is playing. Disabled means no clamp.")]
		public Toggle<OptionalRange<float>> useX = new(new OptionalRange<float>());

		[LabelText("Height Clamp (Y)")]
		[Tooltip("Optional min/max limits for height while tween is playing. Disabled means no clamp.")]
		public Toggle<OptionalRange<float>> useY = new(new OptionalRange<float>());

		[Space]
		public float duration = 0.5f;

		[PropertySpace(0, 10)]
		public Ease ease = Ease.Linear;

		protected override Tween Create()
		{
			var toSize = GetToSize();
			var tween = Tween(toSize).SetEase(ease);

			if (TryGetFromSize(out var fromSize))
				tween.From(fromSize);

			return tween;
		}

		protected override void OnValidate(GameObject owner)
		{
			if (!root)
				owner.TryGetComponent(out root);
		}

		private TweenerCore<Vector2, Vector2, VectorOptions> Tween(Vector2 toSize)
		{
			return DOTween.To(
				() => root.rect.size,
				newSize =>
				{
					if (useX)
					{
						var valueX = Clamp(newSize.x, in useX.value);
						root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, valueX);
					}

					if (useY)
					{
						var valueY = Clamp(newSize.y, in useY.value);
						root.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, valueY);
					}
				},
				toSize,
				duration
			);
		}

		private Vector2 GetToSize()
		{
			switch (toMode)
			{
				case ValueMode.Vector2:
					return toValue;

				case ValueMode.RectTransform:
				default:
					return to.rect.size;
			}
		}

		private bool TryGetFromSize(out Vector2 fromSize)
		{
			switch (fromMode)
			{
				case ValueMode.Vector2:
					if (fromValue)
					{
						fromSize = fromValue.value;
						return true;
					}

					break;

				case ValueMode.RectTransform:
				default:
					if (from && from.value != null)
					{
						fromSize = from.value.rect.size;
						return true;
					}

					break;
			}

			fromSize = default;
			return false;
		}

		private void SetCurrentForToValue()
		{
			if (!root)
				return;

			toValue = root.rect.size;
		}

		private void SetCurrentForFromValue()
		{
			if (!root)
				return;

			fromValue.value = root.rect.size;
		}

		private static float Clamp(float value, in OptionalRange<float> range)
		{
			if (range.min)
				value = Mathf.Max(value, range.min);

			if (range.max)
				value = Mathf.Min(value, range.max);

			return value;
		}
	}
}
