using System;
using Sirenix.OdinInspector;
using UnityEngine.UI;

namespace ZenoTween.Participant.Callbacks.UI
{
	[Serializable]
	[TypeRegistryItem(Icon = SdfIconType.X, CategoryPath = UIAnimationTweenConstants.CALLBACK_CATEGORY_PATH)]
	public class SetRaycastTargetGraphicTweenCallback : AnimationTweenCallback
	{
		public Graphic graphic;
		public bool raycastTarget;

		protected override void OnCallback() => graphic.raycastTarget = raycastTarget;
	}
}
