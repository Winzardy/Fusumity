using UnityEditor;
using UnityEditor.Build.Content;
using UnityEngine;

namespace Fusumity.Editor.Utility
{
	public static class ComponentUtility
	{
		public static ulong GetComponentLocalId(this Component component)
		{
			ObjectIdentifier.TryGetObjectIdentifier(component, out var objectIdentifier);
			if (objectIdentifier.localIdentifierInFile != 0)
				return (ulong) objectIdentifier.localIdentifierInFile;

			// Scene Objects has no `localIdentifierInFile` until the scene was saved
			// (And even after the saving the `localIdentifierInFile` often doesn't exist)
			// `GlobalObjectId.GetGlobalObjectIdSlow` method forces to set the ObjectIdentifier to the scene Object
			// And the `targetObjectId` is equal to the `localIdentifierInFile`
			return GlobalObjectId.GetGlobalObjectIdSlow(component).targetObjectId;
		}
	}
}
