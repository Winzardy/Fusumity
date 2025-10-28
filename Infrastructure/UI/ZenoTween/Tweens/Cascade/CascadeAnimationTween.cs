using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Fusumity.Attributes;
using Fusumity.Utility;
using Sapientia.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace ZenoTween.Participant.Tweens
{
	[Serializable]
	public abstract class CascadeAnimationTween : AnimationTween
	{
		public new const string CATEGORY_PATH = AnimationTween.CATEGORY_PATH + "/" + "Cascade";

		[DarkCardBox]
		[PropertyOrder(10)]
		public Transform root;

		[Space]
		[DarkCardBox]
		[PropertyOrder(11)]
		public float duration = 0.5f;

		[DarkCardBox]
		[PropertyOrder(12)]
		public bool durationPerChild;

		[DarkCardBox]
		[PropertyOrder(13)]
		public Ease ease = Ease.Linear;

		[DarkCardBox]
		[PropertyOrder(14)]
		public Type childrenType = Type.Append;

		protected sealed override Tween Create()
		{
			var childActiveCount = EnumerateChildren(root)
				.Count();

			if (childActiveCount <= 0)
				return null;

			var inner = DOTween.Sequence();

			var d = durationPerChild ? duration / childActiveCount : duration;
			foreach (var (child, i) in EnumerateChildren(root).WithIndex())
			{
				var childTween = CreateByChild(child, d);

				switch (childrenType)
				{
					case Type.Join:
						inner.Join(childTween);
						break;
					case Type.Append:
						inner.Append(childTween);
						break;
					case Type.Prepend:
						inner.Prepend(childTween);
						break;
				}
			}

			inner.SetEase(ease);
			return inner;
		}

		protected abstract Tween CreateByChild(Transform childTransform, float duration);

		private IEnumerable<Transform> EnumerateChildren(Transform target)
		{
			if (target == null)
				yield break;

			if (target.childCount == 0)
				yield break;

			for (int i = 0; i < target.childCount; i++)
			{
				var child = target.GetChild(i);
				if (child.TryGetComponent(out LayoutElement layoutElement))
				{
					if (layoutElement.ignoreLayout)
						continue;
				}

				yield return child;
			}
		}
	}
}
