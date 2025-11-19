using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace ZenoTween.Participant.Tweens
{
	[Serializable]
	[TypeRegistryItem(Icon = SdfIconType.BoxArrowInUpRight, CategoryPath = CATEGORY_PATH)]
	public class MoveCascadeAnimationTween : CascadeAnimationTween
	{
		public bool useLocal;
		public bool from;
		public Vector3 position;

		protected override Tween CreateByChild(Transform childTransform, float duration)
		{
			var endValue = from
				? useLocal
					? childTransform.localPosition
					: childTransform.position
				: position;
			var doLocalJump = useLocal
				? childTransform.DOLocalMove(endValue, duration)
				: childTransform.DOMove(endValue, duration);

			if (from)
				doLocalJump.From(position);

			return doLocalJump;
		}
	}
}
