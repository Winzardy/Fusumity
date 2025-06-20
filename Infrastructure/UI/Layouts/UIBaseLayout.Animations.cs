using System;
using System.Collections.Generic;
using ZenoTween;
using UnityEngine;
#if UNITY_EDITOR
#endif

namespace UI
{
	/// <summary>
	/// Определяет как соберется финальная анимация виджета.
	/// Например у окна есть дефолтная анимация для открытия, заданная через код,
	/// но так же анимацию могли собрать через верстку (<see cref="UIBaseLayout"/>).
	/// С помощью выбранного типа можно либо дополнить существующую анимацию, либо переопдределить
	/// </summary>
	public enum AnimationSequenceBlendMode
	{
		Additive,
		Override
	}

	public abstract partial class UIBaseLayout
	{
		public virtual AnimationSequenceBlendMode OpeningBlendMode => AnimationSequenceBlendMode.Additive;

		[SerializeReference]
		public SequenceParticipant openingSequence;

		public virtual AnimationSequenceBlendMode ClosingBlendMode => AnimationSequenceBlendMode.Additive;

		[SerializeReference]
		public SequenceParticipant closingSequence;

		public List<SequenceParticipantByKey> customSequences;

		/// <summary>
		/// Отвечает за обработку логики анимаций в виджете, если включен то при назначении Layout в виджет
		/// будет произведена попытка назначить стоковый аниматор для виджета
		/// и если анимациии (Sequence) не пустые использовать их в виджете (открытие, закрытие и т.д)
		/// </summary>
		public virtual bool UseLayoutAnimations => false;

		//TODO: перенести это в Overlay

		#region Debug

#if UNITY_EDITOR
		public virtual bool HideDebugAnimationInEditor => !Application.isPlaying || DebugRequestedAnimation == null;
		public event Action<string> DebugRequestedAnimation;

		[NonSerialized]
		public string[] debugAnimationKeys;

		[NonSerialized]
		public string debugCurrentKey;

		public void PlayAnimation() => DebugRequestedAnimation?.Invoke(debugCurrentKey);
#endif

		#endregion
	}
}
