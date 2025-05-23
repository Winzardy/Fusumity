using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace ZenoTween.Participant.Tweens
{
	[Serializable]
	[UnityEngine.Scripting.APIUpdating.MovedFrom(
		true,
		sourceNamespace: "AnimationSequence.Participant.Tweens",
		sourceAssembly: "Generic")]
	public class PivotToTargetAnimationTween : AnimationTween
	{
		public RectTransform root;
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
