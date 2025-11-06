using System;
using DG.Tweening;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace ZenoTween.Participant.Tweens.UI
{
	[Serializable]
	[TypeRegistryItem(Icon = SdfIconType.DiamondHalf)]
	[UnityEngine.Scripting.APIUpdating.MovedFrom(
		true,
		sourceNamespace: "AnimationSequence.Participant.Tweens.UI",
		sourceAssembly: "UI")]
	public class GraphicAlphaAnimationTween : AnimationTween
	{
		[NotNull]
		public Graphic graphic;

		[PropertyRange(0, 1)]
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
			var tween = graphic
			   .DOFade(alpha, duration)
			   .SetEase(ease);

			if (useStartAlpha)
				tween.From(startAlpha);

			return tween;
		}

		protected override void OnValidate(GameObject owner)
		{
			if (!graphic)
				graphic = owner.GetComponentInChildren<Graphic>();
		}
	}
}
