using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace ZenoTween.Participant.Callbacks.UI
{
	[Serializable]
	[TypeRegistryItem(Icon = SdfIconType.CircleSquare)]
	[UnityEngine.Scripting.APIUpdating.MovedFrom(
		true,
		sourceNamespace: "AnimationSequence.Participant.Callbacks.UI",
		sourceAssembly: "UI")]
	public class SetMaterialToImageTweenCallback : AnimationTweenCallback
	{
		public Image target;

		public Material material;

		protected override void OnCallback() => target.material = material;
	}
}
