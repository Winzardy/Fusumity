namespace Content.ScriptableObjects
{
	using UnityObject = UnityEngine.Object;

	public static class ContentEntryScriptableObjectExtensions
	{
		public static void SetValue<TValue>(this IContentEntryScriptableObject<TValue> scriptableObject, in TValue value, bool save = true)
		{
			scriptableObject.EditValue = value;

			if (!save)
				return;

#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty((UnityObject)scriptableObject);
			UnityEditor.AssetDatabase.SaveAssetIfDirty((UnityObject)scriptableObject);
#endif
		}

		/// <summary>
		/// Use that to change specific fields in value.
		/// </summary>
		public static void EditValue<TValue>(this IContentEntryScriptableObject<TValue> scriptableObject,
			ContentEditing<TValue> editing,
			bool save = true)
		{
			ref var value = ref scriptableObject.EditValue;
			editing(ref value);

			if (!save)
				return;

#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty((UnityObject)scriptableObject);
			UnityEditor.AssetDatabase.SaveAssetIfDirty((UnityObject)scriptableObject);
#endif
		}
	}

	public delegate void ContentEditing<T>(ref T value);
}
