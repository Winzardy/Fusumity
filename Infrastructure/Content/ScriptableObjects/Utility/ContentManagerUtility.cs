using Content.ScriptableObjects;

namespace Content
{
	public static class ContentManagerUtility
	{
		public static bool TryGetScriptableObject<TScriptableObject, TValue>(string id, out TScriptableObject value)
			where TScriptableObject : ContentEntryScriptableObject<TValue>
		{
			var entry = ContentManager.GetEntry<TValue>(id);
			if (entry.Context is TScriptableObject scriptableObject)
			{
				value = scriptableObject;
				return true;
			}

			value = null;
			return false;
		}

		public static bool TryGetScriptableObject<TScriptableObject, TValue>(out TScriptableObject value)
			where TScriptableObject : SingleContentEntryScriptableObject<TValue>
		{
			var entry = ContentManager.GetEntry<TValue>();
			if (entry.Context is TScriptableObject scriptableObject)
			{
				value = scriptableObject;
				return true;
			}

			value = null;
			return false;
		}
	}
}
