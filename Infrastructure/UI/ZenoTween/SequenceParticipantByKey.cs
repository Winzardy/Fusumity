using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace ZenoTween
{
	[Serializable]
	public struct SequenceParticipantByKey
	{
		[TableColumnWidth(30)]
		public string key;

		[SerializeReference]
		[TableColumnWidth(600)]
		public SequenceParticipant sequence;

		public void Deconstruct(out string key, out SequenceParticipant sequence)
		{
			key      = this.key;
			sequence = this.sequence;
		}
	}
}
