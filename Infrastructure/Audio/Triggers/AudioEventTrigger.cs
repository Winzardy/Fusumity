using System;
using Sapientia;
using Sapientia.Extensions;
using UnityEngine;
using UnityEngine.Serialization;

namespace Audio
{
	[Serializable]
	public struct AudioEventTriggerArgs
	{
		public string id;

		public bool loop;
		public int repeat;

		[FormerlySerializedAs("rollOnRepeat")]
		public bool rerollOnRepeat;

		public Toggle<float> fadeIn;
		public Toggle<float> fadeOut;

		public bool IsEmpty => id.IsNullOrEmpty();

		public AudioEventTriggerArgs(string id = null) : this() => this.id = id;

		#region Debug

		[NonSerialized]
		public bool disableSpatialWarning;

		#endregion
	}

	public abstract class AudioEventTrigger : MonoBehaviour
	{
	}
}
