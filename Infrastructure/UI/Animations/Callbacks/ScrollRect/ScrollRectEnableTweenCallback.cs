using System;
using Sirenix.OdinInspector;
using UnityEngine.UI;

namespace ZenoTween.Participant.Callbacks.UI
{
	[Serializable]
	[TypeRegistryItem(Icon = SdfIconType.List, CategoryPath = UIAnimationTweenConstants.CALLBACK_CATEGORY_PATH + "/Scroll Rect")]
	public class ScrollRectEnableTweenCallback : AnimationTweenCallback
	{
		public ScrollRect scrollRect;
		public bool horizontal;
		public bool vertical;

		protected override void OnCallback()
		{
			scrollRect.horizontal = horizontal;
			scrollRect.vertical = vertical;
		}
	}
}
