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
			var owner = _target ?? source;
			var tween = source.sequence.ToTween(owner, _inheritedSpeed * speed);
			return BindTweenToOwner(tween, owner);
		}
	}
}
