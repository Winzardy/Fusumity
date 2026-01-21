using DG.Tweening;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using System;

namespace ZenoTween.Participant.Callbacks
{
	[Serializable]
	[TypeRegistryItem(Icon = SdfIconType.Clock)]
	public class Interval : SequenceParticipant
	{
		public enum Type
		{
			Append,
			Prepend
		}

		public float duration = 1;

		[HorizontalGroup(nameof(Interval)), PropertyOrder(99), PropertySpace(5)]
		public Type type;

		public override void Participate([CanBeNull] ref Sequence sequence, object target = null)
		{
			if (sequence == null)
			{
				sequence = DOTween.Sequence();
				if (target != null)
					sequence.SetTarget(target);
			}

			switch (type)
			{
				case Type.Append:
					sequence.AppendInterval(duration);
					break;
				case Type.Prepend:
					sequence.PrependInterval(duration);
					break;
			}			
		}

#if UNITY_EDITOR
		public override void PlayEditor()
		{
			//Do nothing.
		}
#endif
	}
}
