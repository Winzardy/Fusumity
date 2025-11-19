using System;
using Sirenix.OdinInspector;
using UI;
using UnityEngine.UI;

namespace ZenoTween.Participant.Callbacks.UI
{
	[Serializable]
	[TypeRegistryItem(Icon = SdfIconType.Images, CategoryPath = UIAnimationTweenConstants.CALLBACK_CATEGORY_PATH)]
	[UnityEngine.Scripting.APIUpdating.MovedFrom(
		true,
		sourceNamespace: "AnimationSequence.Participant.Callbacks.UI",
		sourceAssembly: "UI")]
	public class SetSpriteToCustomButtonTweenCallback : AnimationTweenCallback
	{
		public ImageSpriteButtonTransition target;

		[HideLabel]
		[BoxGroup]
		public SpriteState sprites;

		protected override void OnCallback() => target.SetState(sprites);
	}
}
