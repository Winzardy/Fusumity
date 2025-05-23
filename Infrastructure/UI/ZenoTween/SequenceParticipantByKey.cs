using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace ZenoTween
{
	[Serializable]
	public struct SequenceParticipantByKey
	{
		[TableColumnWidth(60)]
		public string key;

		[SerializeReference]
		public SequenceParticipant sequence;

		public void Deconstruct(out string key, out SequenceParticipant sequence)
		{
			key = this.key;
			sequence = this.sequence;
		}
	}
}
