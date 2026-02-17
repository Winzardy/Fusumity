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

		public bool x = true;

		[ShowIf(nameof(x))]
		public Toggle<float> maxX;

		public bool y = true;

		[ShowIf(nameof(y))]
		public Toggle<float> maxY;

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
					if (x)
					{
						root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
							maxX
								? newSize.x > maxX.value
									? maxX.value
									: newSize.x
								: newSize.x);
					}

					if (y)
						root.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
							maxY
								? newSize.y > maxY.value
									? maxY.value
									: newSize.y
								: newSize.y);
				},
				to.rect.size,
				duration
			);
		}
	}
}
