using System;
using System.Collections.Generic;

namespace Content.ScriptableObjects
{
	public static class ClientContentImporterUtility
	{
		public static IList<IContentEntry> Extract(IList<ContentDatabaseScriptableObject> data)
		{
			var entries = new List<IContentEntry>();

			foreach (var database in data)
				Fill(database, entries);

			return entries;
		}

		public static void Fill(this ContentDatabaseScriptableObject database, List<IContentEntry> list, bool clone = false,
			Func<IContentEntry, bool> predicate = null)
		{
			if (TryImport(database, clone, out var entry))
			{
				if (predicate?.Invoke(entry) ?? true)
					list.Add(entry);
			}

			foreach (var scriptableObject in database.scriptableObjects)
			{
				if (TryImport(scriptableObject, clone, out entry))
				{
					if (predicate?.Invoke(entry) ?? true)
						list.Add(entry);
				}
			}
		}

		private static bool TryImport(ContentScriptableObject target, bool clone, out IContentEntry entry)
		{
			entry = target.Import(clone);
			return entry != null;
		}
	}
}
