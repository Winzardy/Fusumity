using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Fusumity.Collections
{
	[Serializable]
	[StructLayout(LayoutKind.Explicit)]
	public struct SerializableGuid : IEquatable<SerializableGuid>
	{
		[FieldOffset(0)]
		[SerializeField]
		public long dummy1;
		[FieldOffset(8)]
		[SerializeField]
		public long dummy2;

		[FieldOffset(0)]
		[NonSerialized]
		public Guid guid;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SerializableGuid(long dummy1, long dummy2)
		{
			guid = Guid.Empty;
			this.dummy1 = dummy1;
			this.dummy2 = dummy2;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Guid(SerializableGuid serializableGuid)
		{
			return serializableGuid.guid;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator SerializableGuid(Guid guid)
		{
			return new SerializableGuid
			{
				guid = guid,
			};
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator string(SerializableGuid serializableGuid)
		{
			return serializableGuid.guid.ToString("N");
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator SerializableGuid(string guid)
		{
			return new SerializableGuid
			{
				guid = new Guid(guid),
			};
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(SerializableGuid a, string b) => a.guid.ToString() == b;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(SerializableGuid a, string b) => !(a == b);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(string a, SerializableGuid b) => b == a;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(string a, SerializableGuid b) => !(b == a);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(SerializableGuid a, Guid b) => a.guid == b;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(SerializableGuid a, Guid b) => !(a == b);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(Guid a, SerializableGuid b) => b == a;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(Guid a, SerializableGuid b) => !(b == a);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(SerializableGuid a, SerializableGuid b) => a.dummy1 == b.dummy1 && a.dummy2 == b.dummy2;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(SerializableGuid a, SerializableGuid b) => !(a == b);

		public bool Equals(SerializableGuid other)
		{
			return guid.Equals(other.guid);
		}

		public override bool Equals(object obj)
		{
			return obj is SerializableGuid other && Equals(other);
		}

		public override int GetHashCode()
		{
			return guid.GetHashCode();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override string ToString()
		{
			return (string)this;
		}
	}
}
