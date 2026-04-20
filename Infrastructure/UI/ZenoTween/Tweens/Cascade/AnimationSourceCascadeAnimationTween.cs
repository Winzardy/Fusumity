using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace ZenoTween.Participant.Tweens
{
	[Serializable]
	[TypeRegistryItem(Icon = SdfIconType.FileEarmarkPlayFill, CategoryPath = CATEGORY_PATH)]
	public class AnimationSourceCascadeAnimationTween : CascadeAnimationTween
	{
		//TODO: убрать duration из настроек так как он игнорируется...
		protected override Tween CreateByChild(Transform childTransform, float _)
		{
			if (!childTransform.TryGetComponent(out AnimationSequenceSource childSource))
				return null;

			return childSource.sequence.ToTween(childSource);
		}
	}
}
