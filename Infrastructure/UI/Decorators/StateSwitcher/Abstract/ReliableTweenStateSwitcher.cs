using DG.Tweening;
using Fusumity.Collections;
using Fusumity.Utility;
using Sapientia.Collections;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using UnityEngine;
using ZenoTween;
using ZenoTween.Utility;

namespace UI
{
	// cus serialized dictionary is broken af in combination with these switchers.
	public abstract class ReliableTweenStateSwitcher<TState> : StateSwitcher<TState>
	{
		[NonSerialized]
		private readonly Dictionary<TState, Tween> _cached = new();
		[NonSerialized]
		private Dictionary<TState, AnimationSequence> _mappedSequences;

		[SerializeField, HideLabel, FoldoutGroup("Default State")]
		private AnimationSequence _default;

		[SerializeField, Tooltip("Trigger state switching if default state value is provided.")]
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

		protected override bool UseEquals => true;

		private void Awake() => Clear();

		private void OnDestroy() => Clear();

		protected override void OnStateSwitched(TState state)
		{
			if (EqualityComparer<TState>.Default.Equals(state, default) &&
				!_switchIfDefaultValue)
				return;

			// Твин ломается если его создавать при неактивном объекте
			if (!gameObject.IsActive())
			{
				mappedSequences.GetValueOrDefaultSafe(state, _default)
				   .ToTween(this)
				   .Kill(true);

				return;
			}

			if (!_cached.TryGetValue(state, out var tween) || !tween.active)
			{
				_cached[state] = tween = mappedSequences.GetValueOrDefaultSafe(state, _default)
				   .ToTween(this)
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

		[Serializable]
		public struct StatePair
		{
			public TState state;
			[BoxGroup]
			public AnimationSequence sequence;
		}
	}
}
