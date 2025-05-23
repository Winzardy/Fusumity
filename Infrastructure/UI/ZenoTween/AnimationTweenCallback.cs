using System;
using DG.Tweening;
using Sirenix.OdinInspector;

namespace ZenoTween.Participant.Callbacks
{
	[Serializable]
	public abstract class AnimationTweenCallback : SequenceParticipant
	{
		/// <summary>
		/// Хак чтобы не вызывать некоторые калбеки при immediate
		/// </summary>
		public static bool immediate;

		public enum Type
		{
			OnStart = 0,
			OnComplete = 1,

			Join = 2,
			Append = 3,
			Prepend = 4,

			OnKill = 5,
		}

		[HorizontalGroup(nameof(AnimationTweenCallback)), PropertyOrder(99), PropertySpace(5)]
		public Type type = Type.Append;

		public override void Participate(ref Sequence sequence)
		{
			sequence ??= DOTween.Sequence();

			switch (type)
			{
				case Type.OnStart:
					sequence.OnStart(OnCallback);
					break;
				case Type.OnComplete:
					sequence.OnComplete(OnCallback);
					break;
				case Type.Join:
					sequence.JoinCallback(OnCallback);
					break;
				case Type.Append:
					sequence.AppendCallback(OnCallback);
					break;
				case Type.Prepend:
					sequence.PrependCallback(OnCallback);
					break;
				case Type.OnKill:
					sequence.OnKill(OnCallback);
					break;
			}
		}

#if UNITY_EDITOR
		[HorizontalGroup(nameof(AnimationTweenCallback), width: BUTTON_SIZE_WIDTH_EDITOR),
		 PropertyOrder(100), PropertySpace(6.5f), Button("Invoke", ButtonStyle.FoldoutButton)]
		public override void PlayEditor() => OnCallback();
#endif

		protected abstract void OnCallback();
	}
}
