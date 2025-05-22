#if CLIENT
using System;
using System.Collections.Generic;
using Sapientia.Collections;
using Sapientia.Reflection;

namespace Content.ScriptableObjects
{
	internal sealed partial class ScriptableContentEntry<T>
	{
		public SerializableDictionary<SerializableGuid, MemberReflectionReference<IUniqueContentEntry>> nested;
		public override IReadOnlyDictionary<SerializableGuid, MemberReflectionReference<IUniqueContentEntry>> Nested => nested;

		public bool RegisterNestedEntry(in SerializableGuid nestedGuid, MemberReflectionReference<IUniqueContentEntry> reference)
			=> nested.TryAdd(nestedGuid, reference);

		public void ClearNestedCollection() => nested.Clear();
	}

	public partial interface IScriptableContentEntry
	{
		/// <summary>
		/// <see cref="ScriptableContentEntry{T}.nested"/>
		/// </summary>
		public const string NESTED_FIELD_NAME = "nested";

		public bool RegisterNestedEntry(in SerializableGuid nestedGuid, MemberReflectionReference<IUniqueContentEntry> reference);
		public void ClearNestedCollection();
	}

	[Serializable]
	public struct NestedPair
	{
		public SerializableGuid guid;
		public MemberReflectionReference<IUniqueContentEntry> reference;
	}
}
#endif
