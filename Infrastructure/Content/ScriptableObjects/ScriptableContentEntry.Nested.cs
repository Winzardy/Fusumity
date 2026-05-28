using System.Collections.Generic;
using Fusumity.Collections;
using Sapientia.Reflection;

namespace Content.ScriptableObjects
{
	internal sealed partial class ScriptableContentEntry<T>
	{
		public SerializableDictionary<SerializableGuid, MemberReflectionReference<IUniqueContentEntry>> nested;

		public override IReadOnlyDictionary<SerializableGuid, MemberReflectionReference<IUniqueContentEntry>> Nested
			=> NestedMap;

		private SerializableDictionary<SerializableGuid, MemberReflectionReference<IUniqueContentEntry>> NestedMap
			=> nested ??= new();

		public bool RegisterNestedEntry(in SerializableGuid nestedGuid, in MemberReflectionReference<IUniqueContentEntry> reference)
		{
			if (NestedMap.TryAdd(nestedGuid, reference))
				return true;

			return NestedMap.TryGetValue(nestedGuid, out var registeredReference)
				&& registeredReference.Path == reference.Path;
		}

		public bool TryGetNestedEntryReference(in SerializableGuid nestedGuid,
			out MemberReflectionReference<IUniqueContentEntry> reference)
			=> NestedMap.TryGetValue(nestedGuid, out reference);

		public void SetNestedEntryReference(in SerializableGuid nestedGuid, in MemberReflectionReference<IUniqueContentEntry> reference)
			=> NestedMap[nestedGuid] = reference;

		public void ClearNestedCollection() => NestedMap.Clear();
	}

	public partial interface IScriptableContentEntry
	{
		bool RegisterNestedEntry(in SerializableGuid nestedGuid, in MemberReflectionReference<IUniqueContentEntry> reference);

		bool TryGetNestedEntryReference(in SerializableGuid nestedGuid,
			out MemberReflectionReference<IUniqueContentEntry> reference);

		void SetNestedEntryReference(in SerializableGuid nestedGuid,
			in MemberReflectionReference<IUniqueContentEntry> reference);

		void ClearNestedCollection();
	}
}
