using System;
using Sapientia.Extensions;

namespace Localization
{
	[Serializable]
	public struct LocTableReference
	{
		public string id;

		public LocTableReference(string id) => this.id = id;

		public static implicit operator string(LocTableReference reference) => reference.id;
		public static implicit operator LocTableReference(string name) => new(name);

		public static implicit operator bool(LocTableReference reference) =>
			!reference.id.IsNullOrEmpty();

		public override int GetHashCode() => id?.GetHashCode() ?? 0;

		public override string ToString() => id;
	}
}
