using DG.Tweening;
using Fusumity.Collections;
using Fusumity.Utility;
using Sapientia.Collections;
using Sirenix.OdinInspector;
using System;
using UnityEngine;
using ZenoTween;
using ZenoTween.Participant.Callbacks;
using ZenoTween.Utility;

namespace UI
{
	public abstract class TweenStateSwitcher<TState> : StateSwitcher<TState>
	{
		private bool _inactiveWarningLogged;

		[NonSerialized]
		private Tween _tween;

		[SerializeField, HideLabel, FoldoutGroup("Default State")]
		private AnimationSequence _default;

		[Space, LabelText("State To Enable"), DictionaryDrawerSettings(KeyLabel = "State", ValueLabel = "Sequence")]
		[SerializeField]
		private SerializableDictionary<TState, AnimationSequence> _dictionary;

		protected override bool UseEquals { get => true; }

		private void Awake() => ClearTweens();
		private void OnEnable() => _inactiveWarningLogged = false;
		private void OnDestroy() => ClearTweens();

		protected override void OnStateSwitched(TState state)
		{
			Tween tween;
			AnimationSequence animationSequence;

			// На неактивном GameObject твин может не запуститься или работать некорректно.
			// Точное поведение не исследовалось, поэтому сразу применяем конечное состояние
			if (!gameObject.IsActive())
			{
				if (!_inactiveWarningLogged)
				{
					Debug.LogWarning(
						"Cannot play DOTween because GameObject is inactive. " +
						"Applying final tween state immediately");
					_inactiveWarningLogged = true;
				}

				animationSequence = _dictionary.GetValueOrDefaultSafe(state, _default);
				if (animationSequence.IsNullOrEmpty())
					return;

				tween = CreateTween(animationSequence);
				if (tween == null)
					return;

				var origin = AnimationTweenCallback.immediate;
				AnimationTweenCallback.immediate = true;
				tween.KillWithCallbacks();
				AnimationTweenCallback.immediate = origin;

				return;
			}

			_tween?.KillSafe();

			animationSequence = _dictionary.GetValueOrDefaultSafe(state, _default);
			if (animationSequence.IsNullOrEmpty())
				return;

			tween = CreateTween(animationSequence);
			if (tween == null)
				return;

			if (_immediate)
			{
				AnimationTweenCallback.immediate = true;
				try
				{
					tween.Complete(true);
				}
				finally
				{
					AnimationTweenCallback.immediate = false;
					tween.KillSafe();
				}
			}
			else
			{
				_tween = tween;
				tween.OnKill(() =>
				{
					if (ReferenceEquals(_tween, tween))
						_tween = null;
				});
				tween.Play();
			}

#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				DG.DOTweenEditor.DOTweenEditorPreview.Stop(true, true);
				DG.DOTweenEditor.DOTweenEditorPreview.PrepareTweenForPreview(tween);
				DG.DOTweenEditor.DOTweenEditorPreview.Start(EditorPreviewUpdate);
			}

			void EditorPreviewUpdate()
			{
				if (tween.IsPlaying() && !Application.isPlaying)
					UnityEditor.EditorUtility.SetDirty(this);
				else if (DOTween.TotalPlayingTweens() == 0)
					DG.DOTweenEditor.DOTweenEditorPreview.Stop(false, true);
			}
#endif
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
	}
}
