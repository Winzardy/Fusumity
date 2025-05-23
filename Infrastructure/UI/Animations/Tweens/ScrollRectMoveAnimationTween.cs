using System;
using DG.Tweening;
using Fusumity.Attributes.Odin;
using Sapientia;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace ZenoTween.Participant.Tweens.UI
{
	[Serializable]
	[TypeRegistryItem(Icon = SdfIconType.SortDown)]
	[UnityEngine.Scripting.APIUpdating.MovedFrom(
		true,
		sourceNamespace: "AnimationSequence.Participant.Tweens.UI",
		sourceAssembly: "UI")]
	public class ScrollRectMoveAnimationTween : AnimationTween
	{
		public ScrollRect scroll;

		[Minimum(0),Maximum(1)]
		public Vector2 to;

		public Toggle<Vector2> from;

		[Space]
		public float duration = 0.5f;

		public Ease ease = Ease.Linear;

		protected override Tween Create()
		{
			var tween = DOTween.To(
					() => scroll.normalizedPosition,
					value => scroll.normalizedPosition = value,
					to,
					duration
				)
			   .SetEase(ease);

			if (from.enable)
			{
				scroll.normalizedPosition = from.value;
				tween.From(from);
			}

			return tween;
		}
	}
}
