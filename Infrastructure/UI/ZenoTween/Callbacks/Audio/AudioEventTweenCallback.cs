using System;
using Audio;
using Sirenix.OdinInspector;
using UnityEngine;

namespace ZenoTween.Participant.Callbacks.Audio
{
	[Serializable]
	[TypeRegistryItem(Icon = SdfIconType.MusicNote)]
	[UnityEngine.Scripting.APIUpdating.MovedFrom(
		true,
		sourceNamespace: "AnimationSequence.Participant.Callbacks.Audio",
		sourceAssembly: "Animations.Audio")]
	public class AudioEventTweenCallback : AnimationTweenCallback
	{
		[HideLabel]
		public AudioEventRequest audioEvent;

		[Tooltip("Для позиционирования звука!")]
		public Transform transform;

		protected override void OnCallback()
		{
			if (!immediate)
				audioEvent.Play(transform);
		}
	}
}
