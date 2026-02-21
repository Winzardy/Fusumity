using System;
using System.Collections.Generic;
using Sapientia.Collections;
using Sapientia.Extensions;
using Sapientia.Pooling;

namespace Content.ScriptableObjects
{
	public static class ClientContentImporterUtility
	{
		public static IList<IContentEntry> Extract(IList<ContentDatabaseScriptableObject> databases)
		{
			var entries = new List<IContentEntry>();

			using (ListPool<ContentDatabaseScriptableObject>.Get(out var sortedDatabase))
			{
				sortedDatabase.AddRange(databases);
				sortedDatabase.Sort(Sort);

				foreach (var database in sortedDatabase)
					Fill(database, entries);

				return entries;
			}

			static int Sort(ContentDatabaseScriptableObject a, ContentDatabaseScriptableObject b)
			{
				if (b.priority && a.priority)
					return b.priority.value.CompareTo(a.priority.value);

				if (b.priority)
					return 1;

				if (a.priority)
					return -1;

				return 0;
			}
		}

		public static void Fill(this ContentDatabaseScriptableObject database, List<IContentEntry> list, bool clone = false,
			Func<IContentEntry, bool> predicate = null)
		{
			if (TryImport(database, clone, out var entry))
			{
				if (predicate?.Invoke(entry) ?? true)
					list.Add(entry);
			}

			foreach (var
#if UNITY_EDITOR
					(
#endif
					scriptableObject
#if UNITY_EDITOR
					, index)
#endif
				in database.scriptableObjects
#if UNITY_EDITOR
					.WithIndex()
#endif
			)
			{
#if UNITY_EDITOR
				if (scriptableObject == null)
				{
					ContentDebug.LogError($"ScriptableObject by index [ {index} ] is null while importing database [ {database.name} ]",
						database);
					continue;
				}
#endif
				if (TryImport(scriptableObject, clone, out entry))
				{
					if (predicate?.Invoke(entry) ?? true)
						list.Add(entry);
				}
			}
		}

		private static bool TryImport(ContentScriptableObject target, bool clone, out IContentEntry entry)
		{
			if (target != null)
			{
				entry = target.Import(clone);
				return entry != null;
			}

			entry = null;
			return false;
		}
	}
}
