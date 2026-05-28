using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace ZenoTween.Participant.Callbacks.UI
{
	[Serializable]
	[TypeRegistryItem(Icon = SdfIconType.Images, CategoryPath = UIAnimationTweenConstants.CALLBACK_CATEGORY_PATH)]
	public class SetImageColorTweenCallback : AnimationTweenCallback
	{
		public Image target;
		public Color color;

		protected override void OnCallback() => target.color = color;
	}
}
