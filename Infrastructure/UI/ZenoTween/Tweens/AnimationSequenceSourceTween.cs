using System;
using DG.Tweening;
using JetBrains.Annotations;
using Sirenix.OdinInspector;

namespace ZenoTween.Participant.Tweens
{
	[Serializable]
	[TypeRegistryItem(Icon = SdfIconType.FileEarmarkPlayFill, CategoryPath = CATEGORY_PATH)]
	public class AnimationSequenceSourceTween : AnimationTween
	{
		[NotNull]
		public AnimationSequenceSource source;

		protected override Tween Create()
		{
			return source.sequence.ToTween(source, _inheritedSpeed * speed);
		}
	}
}
