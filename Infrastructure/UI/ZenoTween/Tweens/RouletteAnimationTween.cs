using System;
using DG.Tweening;
using Fusumity;
using Fusumity.Utility;
using Sirenix.OdinInspector;
using UnityEngine;

namespace ZenoTween.Participant.Tweens
{
	[Serializable]
	[TypeRegistryItem(CategoryPath = CATEGORY_PATH, Icon = SdfIconType.Hypnotize)]
	public class RouletteAnimationTween : AnimationTween
	{
		public const string INDEX = "index";
		public const string SEGMENTS = "segments";

		public BlackboardSource blackboard;

		public Transform pivot;

		[Space]
		public RotateMode rotateMode = RotateMode.Fast;

		public int fullRotations = 0;

		[PropertyRange(0, 0.5f)]
		public float offsetBorder;

		[Header("Debug")]
		[PropertyRange(0, nameof(maxIndexSegmentsEditor))]
		public int debugIndex = 0;
		public int debugSegments = 6;

		[Space]
		public float duration = 0.5f;

		public Ease ease = Ease.Linear;

		protected override Tween Create()
		{
			var index = blackboard.Get(INDEX, debugIndex);
			var total = blackboard.Get(SEGMENTS, debugSegments);
			var segmentAngle = -360f / total;
			var randomOffset = segmentAngle * UnityRandomizer<float>.Default.Next(-.5f + offsetBorder, .5f - offsetBorder);
			var targetAngle =
				index * segmentAngle +
				randomOffset +
				fullRotations * 360f;
			var tween = pivot.DOLocalRotate(
				new Vector3(0, 0, -targetAngle),
				duration,
				rotateMode);
			tween.SetEase(ease);
			return tween;
		}

		private int maxIndexSegmentsEditor => debugSegments - 1;
	}
}
