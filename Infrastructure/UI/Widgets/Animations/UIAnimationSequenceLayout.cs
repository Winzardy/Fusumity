using Sirenix.OdinInspector;
using UnityEngine;
using ZenoTween;

namespace UI
{
	public class UIAnimationSequenceLayout : UIBaseLayout
	{
		[BoxGroup("Animation Sequence"), HideLabel, InlineProperty]
		public AnimationSequence animationSequence;
	}
}
