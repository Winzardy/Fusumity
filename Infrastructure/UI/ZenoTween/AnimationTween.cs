using System;
using DG.Tweening;
using UnityEditor;
using UnityEngine;
using ZenoTween.Utility;
#if UNITY_EDITOR
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
			Prepend = 2
		}

		[Tooltip("<b>" + nameof(Type.Join) +
			"</b> - вставляет заданный tween в ту же временную позицию последнего tween, обратного вызова или интервала, добавленного в Sequence. Обратите внимание, что в случае Join после интервала время вставки будет временем начала интервала, а не его окончания<br>" +
			"<b>" + nameof(Type.Append) + "</b> - добавляет заданный твин в конец Sequence<br>" +
			"<b>" + nameof(Type.Prepend) + "</b> - добавляет заданный tween в начало Sequence, продвигая вперед другой вложенный контент")]
		public Type type = Type.Join;

		[Tooltip("Задержка перед проигрыванием твина")]
		public float delay = 0;

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

		public bool IsLoop => repeat == -1;
		public bool UseType => !(IsLoop && lifetimeByParent);

		public override void Participate(ref Sequence sequence, object target = null)
		{
			var tween = Create();

			if (tween == null)
				return;

			if (sequence == null)
			{
				sequence = DOTween.Sequence();
				if (target != null)
					sequence.SetTarget(target);
			}

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

			switch (type)
			{
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

		protected void ApplyTweenSettings(in Tween tween)
		{
			if (delay > 0)
				tween.SetDelay(delay);

			if (repeat != 0)
			{
				tween.SetAutoKill(repeat > 0);
				tween.SetLoops(repeat, repeatType);
			}
		}

		protected abstract Tween Create();

		#region Debug Preview

#if UNITY_EDITOR
		public bool EditorTweenActive => _editorTween != null && _editorTween.IsActive();

		private Tween _editorTween;

		private bool _editorReset = true;
		private bool _loop = false;

		public override void PlayEditor()
		{
			PlayTweenEditor();

			//Останавливает залупленные твины)
			StopTweenEditor(false);
		}

		public void PlayTweenEditor(bool reset = false, bool loop = false)
		{
			if (DOTweenEditorPreview.isPreviewing)
				DOTweenEditorPreview.Stop(true);

			_editorTween = Create();

			if (_editorTween == null)
				return;

			_loop = loop;
			_editorReset = reset;

			if (delay > 0)
				_editorTween.SetDelay(delay);

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
#endif

		#endregion Debug Preview
	}
}
