using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace ZenoTween.Participant.Callbacks
{
	[Serializable]
	[TypeRegistryItem(Icon = SdfIconType.Toggles)]
	[UnityEngine.Scripting.APIUpdating.MovedFrom(
		true,
		sourceNamespace: "AnimationSequence.Participant.Callbacks",
		sourceAssembly: "Generic")]
	public class GameObjectActivationTweenCallback : AnimationTweenCallback
	{
		[Space]
		public GameObject target;

		public bool active;

		protected override void OnCallback()
		{
			if (!target)
			{
				Debug.LogError("Target is null!");
				return;
			}

			target.SetActive(active);
		}

		protected override void OnValidate(GameObject owner)
		{
			if (!target)
				target = owner;
		}
	}
}
