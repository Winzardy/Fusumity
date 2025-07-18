using System;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;

namespace Localization
{
	[Serializable]
	public struct LocKey
	{
		[FormerlySerializedAs(("id")), LabelText("LocKey"), LocKey]
		public string value;

		public LocKey(string value) => this.value = value;

		public static implicit operator string(LocKey key) => key.value;
		public static implicit operator LocKey(string value) => new (value);

		public override string ToString() => value;
	}
}
