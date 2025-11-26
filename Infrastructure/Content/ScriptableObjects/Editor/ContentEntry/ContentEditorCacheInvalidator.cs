using UnityEditor;

namespace Content.Editor
{
	[InitializeOnLoad]
	public static class ContentEditorCacheInvalidator
	{
		static ContentEditorCacheInvalidator()
		{
			ContentEditorCache.RegeneratedGuid += HandleRegeneratedGuid;
		}

		private static void HandleRegeneratedGuid(IUniqueContentEntry entry, string _, string __)
		{
			ContentEntryEditorUtility.ClearCache();
		}
	}
}
