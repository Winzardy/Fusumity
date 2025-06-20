using ZenoTween;
using ZenoTween.Participant.Tweens;
using DG.Tweening;
using UnityEngine;

namespace UI
{
	public class UISpinnerLayout : UIBaseLayout
	{
		public bool useAnimations = true;
		public override bool UseLayoutAnimations => useAnimations;

		[Space]
		[SerializeReference]
		public ZenoTween.AnimationSequence loopSequence = new()
		{
			participants = new SequenceParticipant[]
			{
				new RotateAnimationTween
				{
					duration = 30f,
					endValue = Vector3.zero,
					mode = RotateMode.FastBeyond360,
					useStartValue = true,
					startValue = new() {z = 360}
				}
			}
		};

		protected internal override void OnValidate()
		{
			base.OnValidate();

			if (Application.isPlaying)
				return;

			openingSequence?.Validate(gameObject);
			closingSequence?.Validate(gameObject);
			loopSequence?.Validate(gameObject);
		}
	}
}
