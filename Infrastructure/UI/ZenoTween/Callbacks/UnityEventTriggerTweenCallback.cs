using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace ZenoTween.Participant.Callbacks
{
	[Serializable]
	[TypeRegistryItem(Icon = SdfIconType.Exclamation, CategoryPath = CATEGORY_PATH)]
	public class UnityEventTriggerTweenCallback : AnimationTweenCallback
	{
		public UnityEvent unityEvent;

		protected override void OnCallback()
		{
			if (unityEvent == null)
			{
				Debug.LogError("Event is null!");
				return;
			}

			unityEvent.Invoke();
		}
	}
}
