using DG.Tweening;
using Sapientia;
using Sapientia.Collections;
using Sirenix.OdinInspector;
using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace ZenoTween
{
	[Serializable]
	[MovedFrom(true, sourceNamespace: "AnimationSequence", sourceAssembly: "Generic")]
	[TypeRegistryItem(Icon = SdfIconType.CollectionPlayFill, CategoryPath = "/")]
	public class AnimationSequence : AnimationTween
	{
		public enum TimeScaleMode
		{
			Unscaled,
			Scaled
		}

		[SerializeReference]
		public SequenceParticipant[] participants = new SequenceParticipant[0];

		[Tooltip("Пауза перед запуском сиквенса (не влияет на повторения в лупе)")]
		public bool delayOnce;

		[Tooltip("Включить/выключить кастомный режим скалирования от времени " +
			"(по умолчанию зависит от настройки: DOTween.defaultTimeScaleIndependent)")]
		public Toggle<TimeScaleMode> timeScale = new()
		{
			enable = false,
			value  = DOTween.defaultTimeScaleIndependent ? TimeScaleMode.Unscaled : TimeScaleMode.Scaled
		};

		protected override Tween Create() => participants.ToSequence(null); //root is ignored here, otherwise there will be a dead loop.

		public override void Participate(ref Sequence sequence, object target = null)
		{
			base.Participate(ref sequence, target);

			//Важно понимать что данный метод переопределяет режим для всех! Защиту от дурака позже сделаю...
			if (timeScale)
				sequence.SetUpdate(timeScale == TimeScaleMode.Unscaled);
		}

		public Tween ToTween(object target = null)
		{
			return ToSequence(target);
		}

		public Sequence ToSequence(object target = null)
		{
			if (IsEmpty())
				return null;

			var sequence = DOTween.Sequence();
			participants.Participate(ref sequence, target);

			ApplyTweenSettings(sequence, !delayOnce);

			if (delay > 0 && delayOnce)
			{
				// not the most reliable way, but whatever.
				sequence.Pause();
				DOVirtual.DelayedCall(delay, () =>
				{
					if (sequence.IsActive())
					{
						sequence.Play();
					}
				});
			}

			return sequence;
		}

		protected internal override bool IsEmpty() => participants.IsNullOrEmpty();

#if UNITY_EDITOR
		public Sequence ToEditorPreviewSequence(object target)
		{
			if (IsEmpty())
				return null;

			var innerSequence = DOTween.Sequence();
			participants.Participate(ref innerSequence, target);

			ApplyTweenSettings(innerSequence, !delayOnce);

			if (timeScale)
				innerSequence.SetUpdate(timeScale == TimeScaleMode.Unscaled);

			if (!delayOnce || delay <= 0f)
				return innerSequence;

			var bakedSequence = DOTween.Sequence();
			bakedSequence.AppendInterval(delay);
			bakedSequence.Append(innerSequence);

			if (timeScale)
				bakedSequence.SetUpdate(timeScale == TimeScaleMode.Unscaled);

			return bakedSequence;
		}
#endif
	}
}
