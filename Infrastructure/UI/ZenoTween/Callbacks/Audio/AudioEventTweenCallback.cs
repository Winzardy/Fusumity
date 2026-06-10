using System;
using Audio;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace ZenoTween.Participant.Callbacks.Audio
{
	[Serializable]
	[TypeRegistryItem(Icon = SdfIconType.MusicNote, CategoryPath = CATEGORY_PATH)]
	[UnityEngine.Scripting.APIUpdating.MovedFrom(
		true,
		sourceNamespace: "AnimationSequence.Participant.Callbacks.Audio",
		sourceAssembly: "Animations.Audio")]
	public class AudioEventTweenCallback : AnimationTweenCallback
	{
		[HideLabel]
		public AudioEventRequest audioEvent;

		[Tooltip("Для позиционирования звука!")]
		[LabelText("Position")]
		[CanBeNull]
		public Transform transform;

		protected override void OnCallback()
		{
			if (!immediate)
			{
				if (transform == null)
					audioEvent.Play();
				else
					audioEvent.Play(transform);
			}
		}
	}
}
