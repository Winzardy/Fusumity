using System;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using JetBrains.Annotations;
using Sapientia;
using Sirenix.OdinInspector;
using UnityEngine;

namespace ZenoTween.Participant.Tweens
{
	[Serializable]
	[TypeRegistryItem(Icon = SdfIconType.BoxArrowInUpRight,
		CategoryPath = CATEGORY_PATH)]
	[UnityEngine.Scripting.APIUpdating.MovedFrom(
		true,
		sourceNamespace: "AnimationSequence.Participant.Tweens",
		sourceAssembly: "Generic")]
	public class MoveToTargetAnimationTween : AnimationTween
	{
		[NotNull]
		public Transform root;

		[NotNull]
		public Transform to;

		public Toggle<Transform> from;

		public bool useLocal;

		[Space]
		public float duration = 0.5f;

		[PropertySpace(0,10)]
		public Ease ease = Ease.Linear;

		[FoldoutGroup("Obsolete"), Obsolete]
		public Transform target;

		[FoldoutGroup("Obsolete"), Obsolete]
		public bool useStartTarget;

		[ShowIf(nameof(useStartTarget))]
		[FoldoutGroup("Obsolete"), Obsolete]
		public Transform startTarget;

		protected override Tween Create()
		{
			var tween = MoveTween().SetEase(ease);

			if (from.enable)
				tween.From(useLocal ? from.value.localPosition : from.value.position);
			else if (useStartTarget)
				tween.From(useLocal ? startTarget.localPosition : startTarget.position);

			return tween;
		}

		private TweenerCore<Vector3, Vector3, VectorOptions> MoveTween()
		{
			if (!to && target)
				return useLocal ? root.DOLocalMove(target.localPosition, duration) : root.DOMove(target.position, duration);

			return useLocal ? root.DOLocalMove(to.localPosition, duration) : root.DOMove(to.position, duration);
		}

		protected override void OnValidate(GameObject owner)
		{
			if (!root)
				owner.TryGetComponent(out root);
		}
	}
}
