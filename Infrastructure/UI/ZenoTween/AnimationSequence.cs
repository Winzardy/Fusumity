using System;
using DG.Tweening;
using Sapientia;
using Sapientia.Collections;
using Sirenix.OdinInspector;
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

		[Tooltip("Включить/выключить кастомный режим скалирования от времени " +
			"(по умолчанию зависит от настройки: DOTween.defaultTimeScaleIndependent)")]
		public Toggle<TimeScaleMode> timeScale = new()
		{
			enable = false,
			value = DOTween.defaultTimeScaleIndependent ? TimeScaleMode.Unscaled : TimeScaleMode.Scaled
		};

		protected override Tween Create() => ToTween();

		public override void Participate(ref Sequence sequence, object target = null)
		{
			base.Participate(ref sequence, target);

			//Важно понимать что данный метод переопределяет режим для всех! Защиту от дурака позже сделаю...
			if (timeScale)
				sequence.SetUpdate(timeScale == TimeScaleMode.Unscaled);
		}

		public Tween ToTween(object target = null) => participants.ToTween(target);

		protected internal override bool IsEmpty() => participants.IsNullOrEmpty();

#if UNITY_EDITOR
		public override void PlayEditor()
		{
			foreach (var participant in participants)
				participant.PlayEditor();
		}
#endif
	}
}
