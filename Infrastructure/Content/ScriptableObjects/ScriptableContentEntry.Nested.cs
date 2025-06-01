using System.Collections.Generic;
using Fusumity.Collections;
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
		public bool RegisterNestedEntry(in SerializableGuid nestedGuid, MemberReflectionReference<IUniqueContentEntry> reference);
		public void ClearNestedCollection();
	}
}
