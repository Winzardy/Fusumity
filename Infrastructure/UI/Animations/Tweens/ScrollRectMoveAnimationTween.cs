using System;
using DG.Tweening;
using Fusumity.Attributes.Odin;
using Sapientia;
using Sirenix.OdinInspector;
using UI;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace ZenoTween.Participant.Tweens.UI
{
	[Serializable]
	[TypeRegistryItem(Icon = SdfIconType.SortDown, CategoryPath = UIAnimationTweenConstants.TWEEN_CATEGORY_PATH)]
	[UnityEngine.Scripting.APIUpdating.MovedFrom(
		true,
		sourceNamespace: "AnimationSequence.Participant.Tweens.UI",
		sourceAssembly: "UI")]
	public class ScrollRectMoveAnimationTween : AnimationTween
	{
		public ScrollRect scroll;

		public bool useToWatcher;

		[Minimum(0), Maximum(1),
		 DisableIf(nameof(useToWatcher)),
		 InlineToggle(nameof(useToWatcher), "watcher")]
		public Vector2 to;

		[FormerlySerializedAs("toWatcher")]
		[ShowIf(nameof(useToWatcher))]
		[Indent, LabelText("Watcher"), InlineButton(nameof(DisableToWatcher), "disable")]
		public ScrollRectCapturer toCapturer;

		public bool useFromWatcher;

		[DisableIf(nameof(useFromWatcher)), InlineToggle(nameof(useFromWatcher), "watcher")]
		public Toggle<Vector2> from;

		[FormerlySerializedAs("fromWatcher")]
		[ShowIf(nameof(useFromWatcher))]
		[Indent, LabelText("Watcher"), InlineButton(nameof(DisableFromWatcher), "disable")]
		public ScrollRectCapturer fromCapturer;

		[Space]
		public float duration = 0.5f;

		public Ease ease = Ease.Linear;

		protected override Tween Create()
		{
			var toNormPos = useToWatcher ? toCapturer.GetNormalizedPosition() : to;
			var tween = DOTween.To(
					() => scroll.normalizedPosition,
					value => scroll.normalizedPosition = value,
					toNormPos,
					duration
				)
				.SetEase(ease);

			if (from.enable || useFromWatcher)
			{
				var fromNormPos = useFromWatcher ? fromCapturer.GetNormalizedPosition() : from.value;
				scroll.normalizedPosition = fromNormPos;
				tween.From(fromNormPos);
			}

			return tween;
		}

		private void DisableFromWatcher() => useFromWatcher = false;
		private void DisableToWatcher() => useToWatcher = false;
	}
}
