using DG.Tweening;
using Fusumity.Attributes.Odin;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace ZenoTween.Participant.Tweens
{
	[Serializable]
	[TypeRegistryItem(Icon = SdfIconType.ArrowUpRightSquare, CategoryPath = AnimationTween.CATEGORY_PATH)]
	[UnityEngine.Scripting.APIUpdating.MovedFrom(
		true,
		sourceNamespace: "AnimationSequence.Participant.Tweens",
		sourceAssembly: "Generic")]
	public class PivotToTargetAnimationTween : AnimationTween
	{
		public RectTransform root;

		[InlineToggle(nameof(useStartTarget), "From")]
		public RectTransform target;
		public bool useStartTarget;
		[ShowIf(nameof(useStartTarget))]
		public RectTransform startTarget;

		[Space]
		public float duration = 0.5f;

		public Ease ease = Ease.Linear;

		protected override Tween Create()
		{
			var tween = root.DOPivot(target.pivot, duration).SetEase(ease);

			if (useStartTarget)
				tween.From(startTarget.pivot);

			return tween;
		}

		protected override void OnValidate(GameObject owner)
		{
			if (!root)
				owner.TryGetComponent(out root);
		}
	}
}
