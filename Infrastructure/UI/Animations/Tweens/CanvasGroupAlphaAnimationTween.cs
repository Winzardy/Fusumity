using DG.Tweening;
using Fusumity.Attributes.Odin;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace ZenoTween.Participant.Tweens.UI
{
	[Serializable]
	[TypeRegistryItem(Icon = SdfIconType.GridFill, CategoryPath = UIAnimationTweenConstants.TWEEN_CATEGORY_PATH)]
	[UnityEngine.Scripting.APIUpdating.MovedFrom(
		true,
		sourceNamespace: "AnimationSequence.Participant.Tweens.UI",
		sourceAssembly: "UI")]
	public class CanvasGroupAlphaAnimationTween : AnimationTween
	{
		public CanvasGroup group;

		[PropertyRange(0, 1)]
		[InlineToggle(nameof(useStartAlpha), "From")]
		public float alpha;
		public bool useStartAlpha = false;
		[ShowIf(nameof(useStartAlpha))]
		[PropertyRange(0, 1)]
		public float startAlpha;

		[Space]
		public float duration = 0.5f;

		public Ease ease = Ease.Linear;

		protected override Tween Create()
		{
			var tween = group
			   .DOFade(alpha, duration)
			   .SetEase(ease);

			if (useStartAlpha)
				tween.From(startAlpha);

			return tween;
		}
	}
}
