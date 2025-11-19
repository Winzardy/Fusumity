using System;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using Sirenix.OdinInspector;
using UnityEngine;

namespace ZenoTween.Participant.Tweens
{
	[Serializable]
	[TypeRegistryItem(Icon = SdfIconType.ArrowClockwise,CategoryPath = CATEGORY_PATH)]
	[UnityEngine.Scripting.APIUpdating.MovedFrom(
		true,
		sourceNamespace: "AnimationSequence.Participant.Tweens",
		sourceAssembly: "Generic")]
	public class RotateAnimationTween : AnimationTween
	{
		public Transform root;

		[Space]
		public Vector3 endValue = new() {z = 360};

		public bool useStartValue = false;

		[ShowIf(nameof(useStartValue))]
		public Vector3 startValue;

		public RotateMode mode = RotateMode.FastBeyond360;
		public bool useLocal;

		[Space]
		public float duration = 2;

		public Ease ease = Ease.Linear;

		protected override Tween Create()
		{
			var tween = RotateTween()
			   .SetEase(ease);

			if (useStartValue)
				tween.From(startValue);

			return tween;
		}

		private TweenerCore<Quaternion, Vector3, QuaternionOptions> RotateTween()
		{
			return useLocal ? root.DOLocalRotate(endValue, duration, mode) : root.DORotate(endValue, duration, mode);
		}

		protected override void OnValidate(GameObject owner)
		{
			if (!root)
				owner.TryGetComponent(out root);
		}
	}
}
