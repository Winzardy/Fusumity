using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Fusumity.Attributes;
using Fusumity.Attributes.Odin;
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

		[HideInInspector]
		public bool durationPerChild;

		[DarkCardBox]
		[PropertyOrder(10)]
		public Transform root;

		[Space]
		[DarkCardBox]
		[PropertyOrder(11)]
		[InlineToggle(nameof(durationPerChild), "per child")]
		public float duration = 0.5f;

		[DarkCardBox]
		[PropertyOrder(14)]
		public Ease childrenEase = Ease.Linear;

		[DarkCardBox]
		[PropertyOrder(15)]
		public Type childrenType = Type.Append;

		[DarkCardBox]
		[PropertyOrder(16)]
		public bool reverseChildOrder = false;

		[DarkCardBox]
		[PropertyOrder(17)]
		public Ease ease = Ease.Linear;

		protected sealed override Tween Create()
		{
			var childActiveCount = EnumerateChildren(root)
				.Count();

			if (childActiveCount <= 0)
				return null;

			var inner = DOTween.Sequence();

			var totalDuration = GetDuration(duration);
			var d = durationPerChild ? totalDuration / childActiveCount : totalDuration;
			foreach (var (child, i) in EnumerateChildren(root).WithIndex())
			{
				var childTween = CreateByChild(child, d);
				childTween.SetEase(childrenEase);
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

			if (reverseChildOrder)
			{
				for (int i = target.childCount - 1; i >= 0; i--)
				{
					if (TryGetCascadeChild(target.GetChild(i), out var child))
						yield return child;
				}
			}
			else
			{
				for (int i = 0; i < target.childCount; i++)
				{
					if (TryGetCascadeChild(target.GetChild(i), out var child))
						yield return child;
				}
			}
		}

		private bool TryGetCascadeChild(Transform candidate, out Transform child)
		{
			if (candidate == null)
			{
				child = null;
				return false;
			}

			if (!candidate.IsActive())
			{
				child = null;
				return false;
			}

			if (candidate.TryGetComponent(out LayoutElement layoutElement))
			{
				if (layoutElement.ignoreLayout)
				{
					child = null;
					return false;
				}
			}

			if (candidate.TryGetComponent(out CascadeAnimationElement element))
			{
				child = element.child;
				return true;
			}

			child = candidate;
			return true;
		}
	}
}
