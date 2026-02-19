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
		[NotNull]
		public RectTransform root;

		[NotNull]
		public RectTransform to;

		public Toggle<RectTransform> from;

		[LabelText("X")]
		public Toggle<OptionalRange<float>> useX = new(new OptionalRange<float>());
		[LabelText("Y")]
		public Toggle<OptionalRange<float>> useY = new(new OptionalRange<float>());

		[Space]
		public float duration = 0.5f;

		[PropertySpace(0, 10)]
		public Ease ease = Ease.Linear;

		protected override Tween Create()
		{
			var tween = Tween().SetEase(ease);

			if (from.enable)
				tween.From(from.value.rect.size);

			return tween;
		}

		protected override void OnValidate(GameObject owner)
		{
			if (!root)
				owner.TryGetComponent(out root);
		}

		private TweenerCore<Vector2, Vector2, VectorOptions> Tween()
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
				to.rect.size,
				duration
			);
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
