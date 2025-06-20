#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Sapientia.Extensions;
using UnityEngine;

namespace ZenoTween
{
	public abstract partial class SequenceParticipant
	{
		public const int BUTTON_SIZE_WIDTH_EDITOR = 90;

		[NonSerialized]
		internal GameObject _ownerEditor;

		public abstract void PlayEditor();
	}

	public static partial class SequenceParticipantExt
	{
		public static bool TryPlayEditor(this IEnumerable<SequenceParticipantByKey> participants, string target)
		{
			if (target.IsNullOrEmpty())
				return false;

			foreach (var (key, sequence) in participants)
			{
				if (key != target)
					continue;

				sequence.PlayEditor();
				return true;
			}

			return false;
		}
	}
}

#endif
