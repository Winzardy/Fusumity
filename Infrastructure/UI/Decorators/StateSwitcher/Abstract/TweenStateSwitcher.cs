using DG.Tweening;
using Fusumity.Collections;
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
	public abstract class TweenStateSwitcher<TState> : StateSwitcher<TState>
	{
		[NonSerialized]
		private readonly Dictionary<TState, Tween> _cached = new();

		[SerializeField, HideLabel, FoldoutGroup("Default State")]
		private AnimationSequence _default;

		[Space, LabelText("State To Enable"), DictionaryDrawerSettings(KeyLabel = "State", ValueLabel = "Sequence")]
		[SerializeField]
		private SerializableDictionary<TState, AnimationSequence> _dictionary;

		protected override bool UseEquals { get => true; }

		private void Awake() => Clear();

		private void OnDestroy() => Clear();

		protected override void OnStateSwitched(TState state)
		{
			Tween tween;

			// На неактивном GameObject твин может не запуститься или работать некорректно.
			// Точное поведение не исследовалось, поэтому сразу применяем конечное состояние
			if (!gameObject.IsActive())
			{
				Debug.LogWarning(
					"Cannot play DOTween because GameObject is inactive. " +
					"Applying final tween state immediately");

				tween = _dictionary.GetValueOrDefaultSafe(state, _default).ToTween(this);
				tween.KillWithCallbacks();

				return;
			}

			if (!_cached.TryGetValue(state, out tween) || !tween.active)
			{
				_cached[state] = tween = _dictionary.GetValueOrDefaultSafe(state, _default)
					.ToTween(this)
					.SetAutoKill(false);
			}

			if (_immediate)
			{
				AnimationTweenCallback.immediate = true;
				tween.GotoWithCallbacks(1);
				AnimationTweenCallback.immediate = false;
			}
			else
			{
				if (tween.playedOnce)
					tween.Restart();
				else
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
					DG.DOTweenEditor.DOTweenEditorPreview.Stop(false, true);
			}
#endif
		}

#if UNITY_EDITOR
		private bool ShowIfClearButton() => _cached.Count > 0;

		[Button, ShowIf(nameof(ShowIfClearButton))]
#endif
		private void Clear()
		{
			foreach (var tween in _cached.Values)
				tween?.KillSafe();

			_cached.Clear();
		}

		public override bool IsTransitioning()
		{
			foreach (var tween in _cached.Values)
			{
				if (tween.IsPlayingSafe())
					return true;
			}

			return false;
		}
	}
}
