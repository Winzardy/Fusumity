using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace ZenoTween.Participant.Callbacks.UI
{
	[Serializable]
	[TypeRegistryItem(Icon = SdfIconType.CardImage)]
	[UnityEngine.Scripting.APIUpdating.MovedFrom(
		true,
		sourceNamespace: "AnimationSequence.Participant.Callbacks.UI",
		sourceAssembly: "UI")]
	public class SetSpriteToImageTweenCallback : AnimationTweenCallback
	{
		public Image target;

		public Sprite sprite;

		protected override void OnCallback() => target.sprite = sprite;
	}
}
