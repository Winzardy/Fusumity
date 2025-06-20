using System;
using Sapientia;
using Sapientia.Extensions;
using UnityEngine.Serialization;

namespace Audio
{
	[Serializable]
	public struct AudioEventRequest
	{
		public string id;

		public bool loop;
		public int repeat;

		[FormerlySerializedAs("rollOnRepeat")]
		public bool rerollOnRepeat;

		public Toggle<float> fadeIn;
		public Toggle<float> fadeOut;

		public bool IsEmpty => id.IsNullOrEmpty();

		public AudioEventRequest(string id = null) : this() => this.id = id;
	}
}
