using System;
using System.Diagnostics.CodeAnalysis;
using Sirenix.OdinInspector;

namespace ZenoTween.Participant.Callbacks
{
	[Serializable]
	[TypeRegistryItem(Icon = SdfIconType.PlayBtnFill, CategoryPath = CATEGORY_PATH)]
	public class PlayAnimationSequenceSourceTweenCallback : AnimationTweenCallback
	{
		[NotNull]
		public AnimationSequenceSource source;

		protected override void OnCallback() => source.Play();
	}
}
