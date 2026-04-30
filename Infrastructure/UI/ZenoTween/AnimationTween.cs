using System;
using DG.Tweening;
using UnityEngine;
using ZenoTween.Utility;
#if UNITY_EDITOR
using UnityEditor;
using DG.DOTweenEditor;
using Sirenix.Utilities.Editor;
#endif

namespace ZenoTween
{
	[Serializable]
	public abstract class AnimationTween : SequenceParticipant
	{
		public const string CATEGORY_PATH = "Tween";

		public enum Type
		{
			Join = 0,
			Append = 1,
			Prepend = 2,
			Immediate = 3,
		}

		public enum ImmediateType
		{
			Join = 0,
			Append = 1,
			Prepend = 2,
		}

		[Tooltip("<b>" + nameof(Type.Join) +
			"</b> - вставляет заданный tween в ту же временную позицию последнего tween, обратного вызова или интервала, добавленного в Sequence. Обратите внимание, что в случае Join после интервала время вставки будет временем начала интервала, а не его окончания<br>" +
			"<b>" + nameof(Type.Append) + "</b> - добавляет заданный твин в конец Sequence<br>" +
			"<b>" + nameof(Type.Prepend) + "</b> - добавляет заданный tween в начало Sequence, продвигая вперед другой вложенный контент")]
		public Type type = Type.Join;

		[Tooltip("Задержка перед проигрыванием твина")]
		public float delay = 0;

		[Tooltip("Множитель скорости проигрывания твина. 1 = обычная скорость")]
		public float speed = 1f;

		[Tooltip(
			"Количество повторений. Можно установить '-1' что равно <u>бесконечно</u> (опасно, так как требует самому завершить анимацию)")]
		public int repeat = 0;

		[Tooltip("<b>" + nameof(LoopType.Restart) + "</b> - каждый цикл цикла начинается заново<br>" +
			"<b>" + nameof(LoopType.Yoyo) + "</b> - эффект йо-йо сначала цикл вперед, потом этот же цикл в обратную сторону<br>" +
			"<b>" + nameof(LoopType.Incremental) +
			"</b> - Непрерывно увеличивает tween в конце каждого цикла цикла (A к B, B к B+(A-B) и т. д.), таким образом, всегда двигаясь \"вперед\". " +
			"В случае String tween работает только если tween установлен как относительный")]
		public LoopType repeatType = LoopType.Incremental;

		[Tooltip("Связывает жизненный цикл зацикленного твина с жизненным циклом родителя," +
			" разница в том что твин становится не частью родителя, а лишь слушает его события и реагирует")]
		//TODO: требует доработки, так же странное название, которое бы означало что заданный твин не является частью Sequence
		//TODO: а лишь запускается/останавливается вместе с родительским Sequence
		public bool lifetimeByParent;

		public ImmediateType immediateType;

		public bool IsLoop { get => repeat == -1; }
		public bool UseType { get => !(IsLoop && lifetimeByParent); }
		public bool UseRepeat { get => type != Type.Immediate && repeat > 0; }

		public override void Participate(ref Sequence sequence, object target = null)
		{
			var tweenSpeed = sequence?.timeScale ?? speed;
			var tween = Create(tweenSpeed);

			if (tween == null)
				return;

			if (sequence == null)
			{
				sequence = DOTween.Sequence();
				if (target != null)
					sequence.SetTarget(target);
			}

			if (type != Type.Immediate)
			{
				ApplyTweenSettings(tween);
				if (IsLoop && lifetimeByParent)
				{
					sequence.JoinCallback(() =>
					{
#if UNITY_EDITOR
						DOTweenEditorPreview.PrepareTweenForPreview(tween);
#endif
						tween.Play();
					});
					sequence.OnComplete(() => tween.KillSafe());
					sequence.OnKill(() => tween.KillSafe());

					return;
				}
			}

			switch (type)
			{
				case Type.Immediate:
					switch (immediateType)
					{
						case ImmediateType.Join:
							sequence.JoinCallback(Immediate);
							break;
						case ImmediateType.Append:
							sequence.AppendCallback(Immediate);
							break;
						case ImmediateType.Prepend:
							sequence.PrependCallback(Immediate);
							break;
					}

					void Immediate()
					{
#if UNITY_EDITOR
						DOTweenEditorPreview.PrepareTweenForPreview(tween);
#endif
						tween.Complete(true);
					}

					break;
				case Type.Join:
					sequence.Join(tween);
					break;
				case Type.Append:
					sequence.Append(tween);
					break;
				case Type.Prepend:
					sequence.Prepend(tween);
					break;
			}
		}

		protected void ApplyTweenSettings(in Tween tween, bool useDelay = true)
		{
			// not calling Participate here, to ensure valid order and
			// correct loop behaviour (loops on nested tweens are not allowed in DoTween).
			if (delay > 0 && useDelay)
				tween.SetDelay(delay);

			// Работает только на Sequence
			if (!Mathf.Approximately(speed, 1f))
				tween.timeScale *= speed;

			if (repeat != 0)
			{
				tween.SetAutoKill(repeat > 0);
				tween.SetLoops(repeat, repeatType);
			}
		}

		protected float GetDuration(float duration)
		{
			var totalSpeed = speed;
			totalSpeed *= _inheritedSpeed;
			if (totalSpeed <= 0f)
			{
				totalSpeed = 1f;
				Debug.LogWarning("Speed must be greater than 0!"
#if UNITY_EDITOR
					, _ownerEditor
#endif
				);
			}

			return duration / totalSpeed;
		}

		protected float _inheritedSpeed = 1f;

		protected Tween Create(float inheritedSpeed)
		{
			_inheritedSpeed = inheritedSpeed;
			return Create();
		}

		protected abstract Tween Create();

		#region Debug Preview

#if UNITY_EDITOR

		private Tween _editorTween;

		private bool _editorReset = true;
		private bool _loop = false;
		private float _duration = 1;

		public bool EditorReset { get => _editorReset; }
		public override bool EditorPreviewActive => _editorTween != null && _editorTween.IsActive();
		public bool EditorTweenActive { get => _editorTween != null && _editorTween.IsActive(); }
		public float EditorTweenPosition { get => _editorTween?.position ?? 0; }
		public float EditorTweenFullPosition { get => _editorTween != null ? _duration : 1; }

		public override void PlayEditor()
		{
			PlayTweenEditor();

			//Останавливает залупленные твины)
			StopTweenEditor(false);
		}

		public override void PlayEditor(bool reset = false, bool loop = false) => PlayTweenEditor(reset, loop);

		public void PlayTweenEditor(bool reset = false, bool loop = false)
		{
			if (DOTweenEditorPreview.isPreviewing)
				DOTweenEditorPreview.Stop(true);

			_editorTween = Create();

			if (_editorTween == null)
				return;

			_loop        = loop;
			_editorReset = reset;

			if (delay > 0)
				_editorTween.SetDelay(delay);

			var speedScale = Mathf.Max(0f, speed);
			if (!Mathf.Approximately(speedScale, 1f))
				_editorTween.timeScale = speedScale;

			_duration = _editorTween.Duration();

			DOTweenEditorPreview.PrepareTweenForPreview(_editorTween);
			DOTweenEditorPreview.Start(_TweenUpdateEditor);
		}

		private void _TweenUpdateEditor()
		{
			if (_ownerEditor)
				EditorUtility.SetDirty(_ownerEditor);

			if (!DOTweenEditorPreview.isPreviewing)
				StopTweenEditor(_editorReset);

			if (_editorTween.IsPlaying())
				return;

			if (_loop)
			{
				PlayTweenEditor(true, true);
			}
			else
			{
				StopTweenEditor(_editorReset);
			}
		}

		public void StopTweenEditor(bool? reset)
		{
			_loop = false;

			DOTweenEditorPreview.Stop(reset ?? _editorReset);

			_editorTween.KillSafe();
			_editorTween = null;

			GUIHelper.RequestRepaint();
		}

		public override void StopEditor(bool? reset = null) => StopTweenEditor(reset);
#endif

		#endregion Debug Preview
	}
}
