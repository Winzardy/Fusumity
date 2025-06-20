using DG.Tweening;
using Fusumity.Attributes;
using Sirenix.OdinInspector;
using UI;
using UnityEngine;
using ZenoTween.Utility;

namespace ZenoTween
{
	public class AnimationSequencePulsar : Pulsar
	{
		[DisableShowMonoScriptForReference, BoxGroup]
		public AnimationSequence sequence;

		private Tween _tween;

		private void Start() => _tween = sequence.ToTween();

		private void OnDestroy() => _tween?.KillSafe();

		protected override void OnUpdate(float normalizedValue)
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
				return;
#endif
			_tween?.Goto(normalizedValue);
		}

#if UNITY_EDITOR
		[Button, HideInPlayMode]
		public void Reconstruct()
		{
			_tween?.KillSafe();
			_tween = sequence.ToTween();
		}
#endif
	}
}
