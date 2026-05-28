using Sirenix.OdinInspector;
using System;
using UI;
using UnityEngine;

namespace ZenoTween.Participant.Callbacks
{
	[Serializable]
	[TypeRegistryItem(Icon = SdfIconType.Toggles, CategoryPath = UIAnimationTweenConstants.CALLBACK_CATEGORY_PATH)]
	public class StringSwitcherTweenCallback : AnimationTweenCallback
	{
		[Space]
		public StateSwitcher<string> target;

		[LabelText("State")]
		public string state;

		protected override void OnCallback()
		{
			if (target == null)
			{
				Debug.LogError("StringSwitcherTweenCallback: Target is null!");
				return;
			}

			target.Switch(state);
		}

		protected override void OnValidate(GameObject owner)
		{
			if (target == null && owner != null)
				target = owner.GetComponent<StateSwitcher<string>>();
		}
	}
}
