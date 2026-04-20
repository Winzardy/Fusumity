using System;
using DG.Tweening;
using Fusumity.Attributes.Odin;
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

		[InlineToggle(nameof(from), "from")]
		public Vector3 position;

		[ShowIf(nameof(from))]
		public Vector3 fromValue;

		protected override Tween CreateByChild(Transform childTransform, float duration)
		{
			var tween = useLocal
				? childTransform.DOLocalMove(position, duration)
				: childTransform.DOMove(position, duration);

			if (from)
				tween.From(fromValue);

			return tween;
		}
	}
}
