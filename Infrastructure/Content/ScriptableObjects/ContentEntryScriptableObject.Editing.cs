namespace Content.ScriptableObjects
{
	using UnityObject = UnityEngine.Object;

	public static class ContentEntryScriptableObjectExtensions
	{
		public static void Edit<TValue>(this IContentEntryScriptableObject<TValue> scriptableObject,
			ContentEditing<TValue> editing,
			bool save = true)
		{
			ref var value = ref scriptableObject.EditValue;
			editing(ref value);

			if (!save)
				return;

#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty((UnityObject) scriptableObject);
			UnityEditor.AssetDatabase.SaveAssetIfDirty((UnityObject) scriptableObject);
#endif
		}
	}

	public delegate void ContentEditing<T>(ref T value);
}
