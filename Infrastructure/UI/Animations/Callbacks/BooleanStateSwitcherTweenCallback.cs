using System;
using Sirenix.OdinInspector;
using UI;
using UnityEngine;

namespace ZenoTween.Participant.Callbacks
{
	[Serializable]
	[TypeRegistryItem(Icon = SdfIconType.Toggles)]
	[UnityEngine.Scripting.APIUpdating.MovedFrom(
		true,
		sourceNamespace: "AnimationSequence.Participant.Callbacks",
		sourceAssembly: "Generic")]
	public class BooleanSwitcherTweenCallback : AnimationTweenCallback
	{
		[Space]
		public StateSwitcher<bool> target;

		[LabelText("Active")]
		public bool active;

		protected override void OnCallback()
		{
			if (!target)
			{
				Debug.LogError("BooleanSwitcherTweenCallback: Target is null!");
				return;
			}

			target.Switch(active);
		}

		protected override void OnValidate(GameObject owner)
		{
			if (!target && owner)
				target = owner.GetComponent<StateSwitcher<bool>>();
		}
	}
}
