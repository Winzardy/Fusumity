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
		[NonSerialized]
		private Tween _tween;

		[NonSerialized]
		private readonly Dictionary<TState, Tween> _cached = new();

		[NonSerialized]
		private Dictionary<TState, AnimationSequence> _mappedSequences;

		[SerializeField, HideLabel, FoldoutGroup("Default State")]
		private AnimationSequence _default;

		[SerializeField, BoxGroup("Options:"), Tooltip("Trigger state switching if default state value is provided.")]
		private bool _switchIfDefaultValue;

		[SerializeField, BoxGroup("Options:")]
		private bool _useCache;

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
				var instantTween = mappedSequences
					.GetValueOrDefaultSafe(state, _default)
					.ToTween(this);

				instantTween.Complete(true);
				instantTween.Kill();

				return;
			}

			var tween = _useCache ? PlayTweenCached(state) : PlayTween(state);
			if (Forced)
			{
				AnimationTweenCallback.immediate = true;
				tween.GotoWithCallbacks(1);
				AnimationTweenCallback.immediate = false;
			}
#if UNITY_EDITOR
			if (tween == null)
				return;

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

		private Tween PlayTween(TState state)
		{
			_tween?.KillSafe();

			var sequence = mappedSequences.GetValueOrDefaultSafe(state, _default);

			if (sequence.IsNullOrEmpty())
				return null;

			_tween = sequence.ToTween(this);

			return _tween;
		}

		private Tween PlayTweenCached(TState state) //TODO: Not working properly atm.
		{
			if (!_cached.TryGetValue(state, out var tween) || !tween.active)
			{
				var sequence = mappedSequences.GetValueOrDefaultSafe(state, _default);

				if (sequence.IsNullOrEmpty())
					return null;

				_cached[state] = tween = sequence
					.ToTween(this)
					.SetAutoKill(false);
			}

			if (tween.playedOnce)
				tween.Restart();
			else
				tween.Play();

			return tween;
		}

#if UNITY_EDITOR
		private bool ShowIfClearButton() => _cached.Count > 0;

		[Button, ShowIf(nameof(ShowIfClearButton))]
#endif
		private void Clear()
		{
			if (_useCache)
			{
				foreach (var tween in _cached.Values)
					tween?.KillSafe();

				_cached.Clear();
			}
			else
			{
				_tween?.KillSafe();
			}
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
