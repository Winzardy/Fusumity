#if CLIENT
using System.Collections.Generic;
using Sapientia.Collections;
using Sapientia.Reflection;

namespace Content.ScriptableObjects
{
	internal sealed partial class ScriptableSingleContentEntry<T>
	{
		public SerializableDictionary<SerializableGuid, MemberReflectionReference<IUniqueContentEntry>> nested;
		public override IReadOnlyDictionary<SerializableGuid, MemberReflectionReference<IUniqueContentEntry>> Nested => nested;

		public bool RegisterNestedEntry(in SerializableGuid nestedGuid, MemberReflectionReference<IUniqueContentEntry> reference)
			=> nested.TryAdd(nestedGuid, reference);

		public void ClearNestedCollection() => nested.Clear();
	}
}
#endif
