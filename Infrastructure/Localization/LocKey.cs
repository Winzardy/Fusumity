using System;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;

namespace Localization
{
	[Serializable]
	public struct LocKey : IEquatable<LocKey>
	{
		[FormerlySerializedAs(("id")), LabelText("LocKey"), LocKey]
		public string value;

		public LocKey(string value) => this.value = value;

		public static implicit operator string(LocKey key) => key.value;
		public static implicit operator bool(LocKey key) => key.IsEmpty();
		public static implicit operator LocKey(string value) => new(value);

		public static LocKey operator |(LocKey a, LocKey b)
			=> a.IsEmpty() ? b : a;

		public override string ToString() => value;
		public bool Equals(LocKey other) => value == other.value;

		public override bool Equals(object obj) => obj is LocKey other && Equals(other);

		public override int GetHashCode() => value != null ? value.GetHashCode() : 0;
	}
}
