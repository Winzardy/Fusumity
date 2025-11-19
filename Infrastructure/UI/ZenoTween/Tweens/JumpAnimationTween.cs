using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace ZenoTween.Participant.Tweens
{
	[Serializable]
	[TypeRegistryItem(Icon = SdfIconType.ArrowBarUp,
		CategoryPath = CATEGORY_PATH)]
	[UnityEngine.Scripting.APIUpdating.MovedFrom(
		true,
		sourceNamespace: "AnimationSequence.Participant.Tweens",
		sourceAssembly: "Generic")]
	public class JumpAnimationTween : AnimationTween
	{
		public Transform root;

		public bool useLocal;
		public Vector3 endValue;
		public float power = 1;
		public int numJumps = 1;
		public bool snapping;

		[Space]
		public float duration = 0.5f;

		public Ease ease = Ease.Linear;

		protected override Tween Create()
		{
			return useLocal
				? root.DOLocalJump(endValue, power, numJumps, duration, snapping)
				: root.DOJump(endValue, power, numJumps, duration, snapping);
		}

		protected override void OnValidate(GameObject owner)
		{
			if (!root)
				owner.TryGetComponent(out root);
		}
	}
}
