using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Sapientia.Collections;
using Sapientia.Pooling;
using ZenoTween.Participant.Callbacks;
using ZenoTween.Utility;

namespace UI
{
	public abstract class BaseWidgetAnimator<TLayout> : BaseWidgetAnimator<TLayout, UIWidget>
		where TLayout : UIBaseLayout
	{
	}

	public class AnimationLayer
	{
		public const string SEPARATOR = "/";
		public const string VISIBILITY_NAME = "visibility";

		public string name;
		public string clipName;

		public Tween tween;
		public WidgetAnimationArgs args;
	}

	public abstract class BaseWidgetAnimator<TLayout, TWidget> : IWidgetAnimator<TLayout>
		where TLayout : UIBaseLayout
		where TWidget : class, IWidget
	{
		private string _lastKey;

		protected TWidget _widget;
		protected TLayout _layout;

		//TODO: есть мысля сделать Blackboard чтобы в инспекторе можно было указывать переменные
		//В добавок нужно будет сделать структуру которая принимает в себя Blackboard и достает от туда значения \
		//и в которой можно выбрать тип значения (value): Static, Variable, Function

		private Dictionary<string, AnimationLayer> _layers;
		private Dictionary<string, Func<Sequence>> _keyToSequenceCreator;

		public string LastKey => _lastKey;

		public BaseWidgetAnimator()
		{
			_layers = DictionaryPool<string, AnimationLayer>.Get();
			_keyToSequenceCreator = DictionaryPool<string, Func<Sequence>>.Get();

			OnInitialized();
		}

		public void Dispose()
		{
#if UNITY_EDITOR
			if (_layout)
				_layout.DebugRequestedAnimation -= DebugPlay;
#endif

			_layout = null;
			_widget = null;

			_keyToSequenceCreator?.ReleaseToStaticPool();
			_keyToSequenceCreator = null;

			foreach (var layer in _layers.Values)
				layer.tween?.KillSafe();

			_layers.ReleaseToStaticPool();
			_layers = null;

			OnDispose();

			GC.SuppressFinalize(this);
		}

		void IWidgetAnimator.Setup(UIWidget owner)
		{
			if (owner is not TWidget widget)
				throw new Exception($"Invalid widget type [ {typeof(TWidget)} ]");

			_widget = widget;
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
			_layout.debugAnimationKeys = _keyToSequenceCreator.Keys.ToArray();
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

			var (layerKey, clipName) = WidgetAnimatorExt.Split(args.key);

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

			layer.args = args;
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
					SetVisible(args.key == WidgetAnimationType.OPENING);

				args.endCallback?.Invoke();

				return;
			}

			if (!debug && !immediate)
			{
				sequence.PrependCallback(OnPrepend);
				sequence.AppendCallback(OnAppend);
			}

			if (immediate)
			{
				layer.args = null;
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
					SetVisible(args.key == WidgetAnimationType.OPENING);
			}
			else
				layer.tween = sequence;

			void OnPrepend()
			{
				if (layer.args.key == WidgetAnimationType.OPENING)
					SetVisible(true);

				layer.args.startCallback?.Invoke();
			}

			void OnAppend()
			{
				if (layer.args.key == WidgetAnimationType.CLOSING)
					SetVisible(false);

				layer.args.endCallback?.Invoke();
			}
		}

		private void Fill(Dictionary<string, Func<Sequence>> keyToSequenceFactory)
		{
			keyToSequenceFactory[WidgetAnimationType.OPENING] = CreateOpeningSequence;
			keyToSequenceFactory[WidgetAnimationType.CLOSING] = CreateClosingSequence;

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

		private void SetVisible(bool active) => _widget?.SetVisible(active);

		protected virtual void OnInitialized()
		{
		}

		private bool TryGetTweenByKey(string key, out Tween tween)
		{
			tween = null;

			var (layerKey, clipName) = WidgetAnimatorExt.Split(key);

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

	public static class WidgetAnimatorExt
	{
		public static bool IsVisibleKey(this string key)
			=> key is WidgetAnimationType.OPENING or WidgetAnimationType.CLOSING;

		/// <summary>
		/// Основная идея чтобы с помощью ключа (пример: "state/clipName") получить слой в котором проигрывается анимация и название анимации (клипа)
		/// Нужно чтобы при вызове другой анимации на этом же слое, он завершил текущую анимацию на этом слое.
		/// Если у ключа нет сепаратора ("/") то анимация проигрывается на 'нулевом' слое.
		/// </summary>
		/// <returns>Имя слоя и имя клипа</returns>
		public static (string layer, string clip) Split(this string key)
		{
			var split = key.Split(AnimationLayer.SEPARATOR);

			return split.Length == 1 ? (string.Empty, split[0]) : (split[0], split[1]);
		}
	}
}
