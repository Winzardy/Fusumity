using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Fusumity.Collections;
using Fusumity.Utility;
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

		[NonSerialized]
		private Tween _tween;

		protected override bool UseEquals => true;
		private void OnDestroy() => Clear();

		protected override void OnStateSwitched(TState state)
		{
			if (!_cached.TryGetValue(state, out var tween) || !tween.active)
				_cached[state] = tween = _dictionary.GetValueOrDefault(state, _default)
				   .ToTween()
				   .SetAutoKill(false);

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
				if (tween.IsPlaying())
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
			_tween?.KillSafe();
			_tween = null;
		}
	}
}
