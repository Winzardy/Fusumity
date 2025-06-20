using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace ZenoTween.Participant.Tweens
{
	[Serializable]
	[TypeRegistryItem(Icon = SdfIconType.LifePreserver)]
	[UnityEngine.Scripting.APIUpdating.MovedFrom(
		true,
		sourceNamespace: "AnimationSequence.Participant.Tweens",
		sourceAssembly: "Generic")]
	public class PunchAnimationTween : AnimationTween
	{
		public enum PunchType
		{
			Position,
			Rotation,
			Scale
		}

		public Transform root;

		[LabelText("Type")]
		public PunchType punchType;
		public Vector3 punch;
		public int vibrato = 10;
		public float elasticity = 1f;

		[Space]
		public float duration = 0.5f;

		public Ease ease = Ease.Linear;

		protected override Tween Create()
			=> punchType switch
			{
				PunchType.Position => root.DOPunchPosition(punch, duration, vibrato, elasticity),
				PunchType.Rotation => root.DOPunchRotation(punch, duration, vibrato, elasticity),
				PunchType.Scale => root.DOPunchScale(punch, duration, vibrato, elasticity),
				_ => throw new Exception("Oh ooh")
			};

		protected override void OnValidate(GameObject owner)
		{
			if (!root)
				owner.TryGetComponent(out root);
		}
	}
}
