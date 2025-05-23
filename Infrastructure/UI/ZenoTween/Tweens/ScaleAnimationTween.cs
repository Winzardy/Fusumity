using System;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using Sirenix.OdinInspector;
using UnityEngine;

namespace ZenoTween.Participant.Tweens
{
	[Serializable]
	[TypeRegistryItem(Icon = SdfIconType.ArrowsFullscreen)]
	[UnityEngine.Scripting.APIUpdating.MovedFrom(
		true,
		sourceNamespace: "AnimationSequence.Participant.Tweens",
		sourceAssembly: "Generic")]
	public class ScaleAnimationTween : AnimationTween
	{
		public Transform root;

		[Space]
		public Vector3 endValue = new(1.1f, 1.1f, 1.1f);

		public bool useStartValue = false;

		[ShowIf(nameof(useStartValue))]
		public Vector3 startValue;

		[Space]
		public float duration = 0.5f;

		public Ease ease = Ease.Linear;

		protected override Tween Create()
		{
			var tween = ScaleTween()
			   .SetEase(ease);

			if (useStartValue)
				tween.From(startValue);

			return tween;
		}

		private TweenerCore<Vector3, Vector3, VectorOptions> ScaleTween()
		{
			//По дефолтну Local) можно сделать мировой скейл...
			return root.DOScale(endValue, duration);
		}

		protected override void OnValidate(GameObject owner)
		{
			if (!root)
				owner.TryGetComponent(out root);
		}
	}
}
