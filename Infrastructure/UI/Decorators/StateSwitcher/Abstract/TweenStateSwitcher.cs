using System;
using System.Collections.Generic;
using DG.Tweening;
using Fusumity.Collections;
using Fusumity.Utility;
using Sapientia.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using ZenoTween;
using ZenoTween.Utility;

namespace UI
{
	public abstract class TweenStateSwitcher<TState> : StateSwitcher<TState>
	{
		[NonSerialized]
		private readonly Dictionary<TState, Tween> _cached = new();

		[SerializeField, HideLabel, BoxGroup("Default State")]
		private AnimationSequence _default;

		[Space, LabelText("State To Enable"), DictionaryDrawerSettings(KeyLabel = "State", ValueLabel = "Sequence")]
		[SerializeField]
		private SerializableDictionary<TState, AnimationSequence> _dictionary;

		protected override bool UseEquals => true;

		private void Awake() => Clear();

		private void OnDestroy() => Clear();

		protected override void OnStateSwitched(TState state)
		{
			// Твин ломается если его создавать при неактивном объекте
			if (!gameObject.IsActive())
			{
				_dictionary.GetValueOrDefaultSafe(state, _default)
				   .ToTween()
				   .Kill(true);

				return;
			}


			if (!_cached.TryGetValue(state, out var tween) || !tween.active)
			{
				_cached[state] = tween = _dictionary.GetValueOrDefaultSafe(state, _default)
				   .ToTween()
				   .SetAutoKill(false);
			}

			if (tween.playedOnce)
				tween.Restart();
			else
				tween.Play();

#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				if (DG.DOTweenEditor.DOTweenEditorPreview.isPreviewing)
					DG.DOTweenEditor.DOTweenEditorPreview.Stop();
				DG.DOTweenEditor.DOTweenEditorPreview.PrepareTweenForPreview(tween);
				DG.DOTweenEditor.DOTweenEditorPreview.Start(EditorPreviewUpdate);
			}

			void EditorPreviewUpdate()
			{
				if (tween.IsPlaying() && !Application.isPlaying)
					UnityEditor.EditorUtility.SetDirty(this);
				else
					DG.DOTweenEditor.DOTweenEditorPreview.Stop();
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
	}
}
