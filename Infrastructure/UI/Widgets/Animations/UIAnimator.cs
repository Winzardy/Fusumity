using System;
using System.Collections.Generic;
using DG.Tweening;
using Fusumity.Utility;
using Sapientia.Collections;
using Sapientia.Pooling;
using ZenoTween.Participant.Callbacks;
using ZenoTween.Utility;

namespace UI
{
	public class AnimationLayer
	{
		public const string SEPARATOR = "/";
		public const string VISIBILITY_NAME = "visibility";

		public string name;
		public string clipName;

		public Tween tween;
		public WidgetAnimationArgs args;
	}

	public class UIAnimator<TLayout> : IUIAnimator<TLayout>
		where TLayout : UIBaseLayout
	{
		private string _lastKey;

		protected TLayout _layout;

		//TODO: есть мысля сделать Blackboard чтобы в инспекторе можно было указывать переменные
		//В добавок нужно будет сделать структуру которая принимает в себя Blackboard и достает от туда значения \
		//и в которой можно выбрать тип значения (value): Static, Variable, Function

		private Dictionary<string, AnimationLayer> _layers;
		private Dictionary<string, Func<Sequence>> _keyToSequenceCreator;

		public string LastKey => _lastKey;

		public UIAnimator(TLayout layout) : this()
		{
			SetupLayout(layout);
		}

		public UIAnimator()
		{
			_layers               = DictionaryPool<string, AnimationLayer>.Get();
			_keyToSequenceCreator = DictionaryPool<string, Func<Sequence>>.Get();

			OnInitialized();
		}

		public virtual void Dispose()
		{
#if UNITY_EDITOR
			if (_layout)
				_layout.DebugRequestedAnimation -= DebugPlay;
#endif

			_layout = null;

			StaticObjectPoolUtility.ReleaseAndSetNullSafe(ref _keyToSequenceCreator);

			foreach (var layer in _layers.Values)
				layer.tween?.KillSafe();

			StaticObjectPoolUtility.ReleaseAndSetNull(ref _layers);

			OnDispose();

			GC.SuppressFinalize(this);
		}

		public bool SetupLayout(TLayout layout)
		{
			if (_layout && _layout == layout)
				return false;

#if UNITY_EDITOR
			if (_layout)
				_layout.DebugRequestedAnimation -= DebugPlay;

#endif
			_layout = layout;

			Fill(_keyToSequenceCreator);

#if UNITY_EDITOR
			_layout.debugAnimationKeys      =  _keyToSequenceCreator.Keys.ToArray();
			_layout.DebugRequestedAnimation += DebugPlay;
#endif
			return true;
		}

		public void Play(in WidgetAnimationArgs args, bool immediate = false)
			=> Play(in args, immediate, false);

		public void Stop(string key, bool complete = false)
		{
			if (key.IsVisibleKey())
			{
				//Важное предупреждение, потому что на висибл анимации завязано много калбеков,
				//останавливать можно только в редких кейсах
				GUIDebug.LogWarning($"Visible animation stopped! [ {key} ]", _layout);
			}

			var tryGetTweenByKey = TryGetTweenByKey(key, out var tween);

			if (!tryGetTweenByKey)
				return;

			tween.KillSafe(complete);
		}

		public void Pause(string key)
		{
			if (key.IsVisibleKey())
			{
				GUIDebug.LogError("Can't pause visible animation");
				return;
			}

			if (!TryGetTweenByKey(key, out var tween))
				return;

			tween.Pause();
		}

		public void Resume(string key)
		{
			if (key.IsVisibleKey())
			{
				GUIDebug.LogError("Can't resume visible animation");
				return;
			}

			if (!TryGetTweenByKey(key, out var tween))
				return;

			tween.Play();
		}

		private void Play(in WidgetAnimationArgs args, bool immediate, bool debug)
		{
			if (args.IsEmpty)
				return;

			_lastKey = args.key;

			var (layerKey, clipName) = UIAnimatorUtility.Split(args.key);

			if (!_layers.TryGetValue(layerKey, out var layer))
			{
				layer = new AnimationLayer
				{
					name = layerKey
				};
				_layers[layerKey] = layer;
			}
			else if (layer.tween != null)
			{
				if (layer.tween.IsActive())
					layer.args.endCallback?.Invoke();

				layer.args = null;

				layer.tween.KillSafe();
				layer.tween = null;
			}

			layer.args     = args;
			layer.clipName = clipName;

			Sequence sequence = null;

			if (_layout)
			{
				if (_keyToSequenceCreator.TryGetValue(args.key, out var sequenceCreator))
					sequence = sequenceCreator.Invoke();
				else if (GUIDebug.Logging.Widget.Animator.notFoundSequence)
					GUIDebug.LogWarning($"Not found sequence by key [ {args.key} ]!", _layout);
			}

			if (sequence == null)
			{
				args.startCallback?.Invoke();

				if (layerKey == AnimationLayer.VISIBILITY_NAME)
					SetVisible(args.key == AnimationType.OPENING);

				args.endCallback?.Invoke();

				return;
			}

			sequence.SetTarget(_layout);

			if (!debug && !immediate)
			{
				sequence.PrependCallback(OnPrepend);
				sequence.AppendCallback(OnAppend);
			}

			if (immediate)
			{
				layer.args     = null;
				layer.clipName = null;

				args.startCallback?.Invoke();

				//Есть важный момент что этот метод отрабатывает при выключении виджета.
				//Идея в том что у верстки "могут быть" элементы участвующие в анимации за пределами корня,
				//и без форс выставлении позиции в последовательности может зашакалить визуал.
				//Попробую удешевить этот кейс.
				//TODO: Словил баг при котором этот метод не отработал, но после перезагрузки клиента заработал...
				AnimationTweenCallback.immediate = true;
				sequence.GotoWithCallbacks(1);
				AnimationTweenCallback.immediate = false;
				sequence.KillSafe();

				args.endCallback?.Invoke();

				if (!debug && layerKey == AnimationLayer.VISIBILITY_NAME)
					SetVisible(args.key == AnimationType.OPENING);
			}
			else
				layer.tween = sequence;

			void OnPrepend()
			{
				if (layer.args.key == AnimationType.OPENING)
					SetVisible(true);

				layer.args.startCallback?.Invoke();
			}

			void OnAppend()
			{
				if (layer.args.key == AnimationType.CLOSING)
					SetVisible(false);

				layer.args.endCallback?.Invoke();
			}
		}

		public bool IsPlaying()
		{
			foreach (var layer in _layers.Values)
			{
				if (layer.tween == null)
					continue;
				if (layer.tween.IsPlaying())
					return true;
			}

			return false;
		}

		public bool IsPlaying(string layerName)
		{
			if (_layers.TryGetValue(layerName, out var layer))
			{
				if (layer.tween == null)
					return false;

				return layer.tween.IsPlaying();
			}

			return false;
		}

		public float GetDuration()
		{
			var duration = 0f;
			foreach (var layer in _layers.Values)
			{
				if (layer.tween == null)
					continue;
				if (layer.tween.IsPlaying())
				{
					var f = layer.tween.Duration();
					if (f > duration)
						duration = f;
				}
			}

			return duration;
		}

		public float GetDuration(string layerName)
		{
			if (_layers.TryGetValue(layerName, out var layer))
			{
				if (layer.tween == null)
					return 0;

				return layer.tween.IsPlaying() ? layer.tween.Duration() : 0;
			}

			return 0;
		}

		private void Fill(Dictionary<string, Func<Sequence>> keyToSequenceFactory)
		{
			keyToSequenceFactory[AnimationType.OPENING] = CreateOpeningSequence;
			keyToSequenceFactory[AnimationType.CLOSING] = CreateClosingSequence;

			if (_layout.UseLayoutAnimations && !_layout.customSequences.IsNullOrEmpty())
			{
				foreach (var pair in _layout.customSequences)
				{
					_keyToSequenceCreator[pair.key] = Create;

					Sequence Create()
					{
						Sequence sequence = null;
						OnCreateCustomSequence(pair.key, ref sequence);
						pair.sequence?.Participate(ref sequence);
						return sequence;
					}
				}
			}

			OnFill(keyToSequenceFactory);
		}

		protected virtual void OnFill(Dictionary<string, Func<Sequence>> keyToSequenceFactory)
		{
		}

		private Sequence CreateOpeningSequence()
		{
			Sequence sequence = null;

			if (_layout.OpeningBlendMode == AnimationSequenceBlendMode.Additive || !_layout.UseLayoutAnimations)
				OnCreateOpeningSequence(ref sequence);

			if (_layout.UseLayoutAnimations)
				_layout.openingSequence?.Participate(ref sequence);

			return sequence;
		}

		protected virtual void OnCreateOpeningSequence(ref Sequence sequence)
		{
		}

		private Sequence CreateClosingSequence()
		{
			Sequence sequence = null;

			if (_layout.ClosingBlendMode == AnimationSequenceBlendMode.Additive || !_layout.UseLayoutAnimations)
				OnCreateClosingSequence(ref sequence);

			if (_layout.UseLayoutAnimations)
				_layout.closingSequence?.Participate(ref sequence);

			return sequence;
		}

		protected virtual void OnCreateClosingSequence(ref Sequence sequence)
		{
		}

		protected virtual void OnCreateCustomSequence(string key, ref Sequence sequence)
		{
		}

		protected virtual void SetVisible(bool active)
		{
			_layout.SetActive(active);
		}

		protected virtual void OnInitialized()
		{
		}

		private bool TryGetTweenByKey(string key, out Tween tween)
		{
			tween = null;

			var (layerKey, clipName) = UIAnimatorUtility.Split(key);

			if (!_layers.TryGetValue(layerKey, out var layer))
				return false;

			if (layer.clipName != clipName)
				return false;

			tween = layer.tween;

			if (tween == null)
				return false;

			tween = layer.tween;
			return true;
		}

		protected virtual void OnDispose()
		{
		}

#if UNITY_EDITOR

		private void DebugPlay(string key) => Play(key, false, true);
#endif
	}
}
