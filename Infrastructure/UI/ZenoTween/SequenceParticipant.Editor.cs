#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Sapientia.Extensions;
using UnityEngine;

namespace ZenoTween
{
	public class EditorAnimationTweenPlayAttribute : Attribute
	{
	}


	public abstract partial class SequenceParticipant
	{
		public const int BUTTON_SIZE_WIDTH_EDITOR = 90;

		[NonSerialized]
		internal GameObject _ownerEditor;

		public virtual bool EditorPreviewActive => false;

		public abstract void PlayEditor();

		public virtual void PlayEditor(bool reset = false, bool loop = false) => PlayEditor();

		public virtual void StopEditor(bool? reset = null)
		{
		}
	}

	public static partial class SequenceParticipantExt
	{
		public static bool TryPlayEditor(this IEnumerable<SequenceParticipantByKey> participants, string target, bool reset = false,
			bool loop = false)
		{
			if (target.IsNullOrEmpty())
				return false;

			foreach (var (key, sequence) in participants)
			{
				if (key != target)
					continue;

				sequence.PlayEditor(reset, loop);
				return true;
			}

			return false;
		}

		public static bool TryStopEditor(this IEnumerable<SequenceParticipantByKey> participants, string target, bool? reset = null)
		{
			if (target.IsNullOrEmpty())
				return false;

			foreach (var (key, sequence) in participants)
			{
				if (key != target)
					continue;

				sequence.StopEditor(reset);
				return true;
			}

			return false;
		}
	}
}

#endif
