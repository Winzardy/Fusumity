using DG.Tweening;
using Fusumity.Utility;
using Sapientia.Collections;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using ZenoTween;
using ZenoTween.Participant.Callbacks;
using ZenoTween.Utility;

namespace UI
{
	// cus serialized dictionary is broken af in combination with these switchers.
	public abstract class ReliableTweenStateSwitcher<TState> : StateSwitcher<TState>
	{
		private bool _inactiveWarningLogged;

		[NonSerialized]
		private Tween _tween;

		[NonSerialized]
		private Dictionary<TState, AnimationSequence> _mappedSequences;

		[SerializeField, HideLabel, FoldoutGroup("Default State")]
		private AnimationSequence _default;

		[SerializeField, BoxGroup("Options:"), Tooltip("Trigger state switching if default state value is provided.")]
		private bool _switchIfDefaultValue;

		[Space, LabelText("State To Enable")]
		[SerializeField]
		private StatePair[] _states;

		private Dictionary<TState, AnimationSequence> mappedSequences
		{
			get
			{
				if (_mappedSequences == null)
				{
					_mappedSequences = new Dictionary<TState, AnimationSequence>();
					if (!_states.IsNullOrEmpty())
					{
						for (int i = 0; i < _states.Length; i++)
						{
							var pair = _states[i];
							_mappedSequences.Add(pair.state, pair.sequence);
						}
					}
				}

				return _mappedSequences;
			}
		}

		protected override bool UseEquals { get => true; }

		private void Awake() => ClearTweens();
		private void OnEnable() => _inactiveWarningLogged = false;
		private void OnDisable() => ClearTweens();
		private void OnDestroy() => ClearTweens();

		protected override void OnStateSwitched(TState state)
		{
			if (EqualityComparer<TState>.Default.Equals(state, default) &&
				!_switchIfDefaultValue)
				return;

			// Твин ломается если его создавать при неактивном объекте
			if (!gameObject.IsActive())
			{
				if (!_inactiveWarningLogged)
				{
					Debug.LogWarning(
						"Cannot play DOTween because GameObject is inactive. " +
						"Applying final tween state immediately");
					_inactiveWarningLogged = true;
				}

				var animationSequence = mappedSequences.GetValueOrDefaultSafe(state, _default);
				if (animationSequence.IsNullOrEmpty())
					return;

				var instantTween = CreateTween(animationSequence);
				if (instantTween == null)
					return;

				var origin = AnimationTweenCallback.immediate;
				AnimationTweenCallback.immediate = true;
				instantTween.KillWithCallbacks();
				AnimationTweenCallback.immediate = origin;

				return;
			}

			var tween = PlayTween(state);
			if (tween == null)
				return;

			if (_immediate)
			{
				var origin =  AnimationTweenCallback.immediate;
				AnimationTweenCallback.immediate = true;
				try
				{
					tween.Complete(true);
				}
				finally
				{
					AnimationTweenCallback.immediate = origin;
					tween.KillSafe();
				}
			}
			else
			{
				tween.Play();
			}
#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				DG.DOTweenEditor.DOTweenEditorPreview.PrepareTweenForPreview(tween);
				DG.DOTweenEditor.DOTweenEditorPreview.Start(EditorPreviewUpdate);
			}

			void EditorPreviewUpdate()
			{
				if (tween.IsPlaying() && !Application.isPlaying)
					UnityEditor.EditorUtility.SetDirty(this);
				else if (DOTween.TotalPlayingTweens() == 0)
					DG.DOTweenEditor.DOTweenEditorPreview.Stop(false, false);
			}
#endif
		}

		private Tween PlayTween(TState state)
		{
			_tween?.KillSafe();

			var animationSequence = mappedSequences.GetValueOrDefaultSafe(state, _default);

			if (animationSequence.IsNullOrEmpty())
				return null;

			_tween = CreateTween(animationSequence);
			if (_tween != null)
			{
				var tween = _tween;
				tween.OnKill(() =>
				{
					if (ReferenceEquals(_tween, tween))
						_tween = null;
				});
			}

			return _tween;
		}

		private Tween CreateTween(AnimationSequence animationSequence)
		{
			var tween = animationSequence.ToTween(this);
			return tween?.SetLink(gameObject, LinkBehaviour.KillOnDestroy);
		}

#if UNITY_EDITOR
		private bool ShowIfClearButton() => _tween.IsActive();

		[Button, ShowIf(nameof(ShowIfClearButton))]
#endif
		private void ClearTweens()
		{
			_tween?.KillSafe();
			_tween = null;

			DOTween.Kill(this);
		}

		public override bool IsTransitioning()
			=> _tween.IsPlayingSafe();

		[Serializable]
		public struct StatePair
		{
			public TState state;

			[BoxGroup]
			public AnimationSequence sequence;
		}
	}
}
