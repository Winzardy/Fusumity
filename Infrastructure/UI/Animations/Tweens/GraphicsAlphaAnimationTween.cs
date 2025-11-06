using System;
using DG.Tweening;
using JetBrains.Annotations;
using Sapientia.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace ZenoTween.Participant.Tweens.UI
{
	[Serializable]
	[TypeRegistryItem(Icon = SdfIconType.Grid)]
	[UnityEngine.Scripting.APIUpdating.MovedFrom(
		true,
		sourceNamespace: "AnimationSequence.Participant.Tweens.UI",
		sourceAssembly: "UI")]
	public class GraphicsAlphaAnimationTween : AnimationTween
	{
		[FormerlySerializedAs("graphic")]
		[NotNull]
		public Graphic[] graphics;

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
			Sequence sequence = null;

			if (!graphics.IsNullOrEmpty())
			{
				sequence = DOTween.Sequence();

				foreach (var graphic in graphics)
				{
					var tween = graphic
					   .DOFade(alpha, duration).SetEase(ease);

					if (useStartAlpha)
						tween.From(startAlpha);

					sequence.Join(tween);
				}
			}

			return sequence;
		}
	}
}
