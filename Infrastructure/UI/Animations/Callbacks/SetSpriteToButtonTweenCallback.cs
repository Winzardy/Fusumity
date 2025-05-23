using System;
using Sirenix.OdinInspector;
using UnityEngine.UI;

namespace ZenoTween.Participant.Callbacks.UI
{
	[Serializable]
	[TypeRegistryItem(Icon = SdfIconType.Images)]
	[UnityEngine.Scripting.APIUpdating.MovedFrom(
		true,
		sourceNamespace: "AnimationSequence.Participant.Callbacks.UI",
		sourceAssembly: "UI")]
	public class SetSpriteToButtonTweenCallback : AnimationTweenCallback
	{
		public Button target;

		[HideLabel]
		public SpriteState spriteState;

		protected override void OnCallback() => target.spriteState = spriteState;
	}
}
