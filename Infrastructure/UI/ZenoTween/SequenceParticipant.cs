using System;
using System.Collections.Generic;
using DG.Tweening;
using Fusumity.Attributes;
using Sapientia.Collections;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace ZenoTween
{
	//Открыт конкрус на лучшее название!)
	[Serializable]
	[ShowMonoScriptForReference]
	[MovedFrom(true, sourceNamespace: "UI", sourceAssembly: "UI")]
	public abstract partial class SequenceParticipant
	{
		public abstract void Participate(ref Sequence sequence);

		public void Validate(GameObject owner)
		{
#if UNITY_EDITOR
			_ownerEditor = owner;
#endif
			OnValidate(owner);
		}

		protected virtual void OnValidate(GameObject owner)
		{
		}

		protected internal virtual bool IsEmpty() => false;
	}

	public static partial class SequenceParticipantExt
	{
		public static bool IsNullOrEmpty(this SequenceParticipant participant)
		{
			if (participant == null)
				return true;

			return participant.IsEmpty();
		}

		public static Tween ToTween(this ICollection<SequenceParticipant> participants)
		{
			Sequence sequence = null;

			if (!participants.IsNullOrEmpty())
			{
				sequence = DOTween.Sequence();

				foreach (var participant in participants)
					participant.Participate(ref sequence);
			}

			return sequence;
		}
	}
}
