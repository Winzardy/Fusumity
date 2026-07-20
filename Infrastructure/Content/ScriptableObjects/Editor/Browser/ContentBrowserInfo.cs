using System;
using System.Collections.Generic;
using Content.ScriptableObjects;
using Content.ScriptableObjects.Editor;

namespace Content.Editor
{
	/// <summary>
	/// Сбор баз контента и группировка их конфигов по типам для Content Browser
	/// </summary>
	public static class ContentBrowserInfo
	{
		public static IEnumerable<ContentDatabaseScriptableObject> Databases
			=> ContentEditorCache.GetAssets<ContentDatabaseScriptableObject>();

		public static ModuleInfo[] GetModules(bool refresh = false, bool sortDisabledLast = false)
		{
			if (refresh)
				ContentEditorCache.ClearAndRefreshScrObjs();

			var modulesByDatabase = new Dictionary<ContentDatabaseScriptableObject, ModuleInfo>();

			var databaseByNamespace = new Dictionary<string, ContentDatabaseScriptableObject>();
			ContentDatabaseScriptableObject miscDatabase = null;

			foreach (var database in Databases)
			{
				if (database == null)
					continue;

				modulesByDatabase.Add(database, new ModuleInfo(database));

				if (database is MiscDatabaseScriptableObject)
					miscDatabase = database;
				else
					databaseByNamespace.TryAdd(database.GetType().Namespace ?? string.Empty, database);
			}

			foreach (var config in ContentEditorCache.GetAssets<ContentScriptableObject>())
			{
				if (config == null || config is ContentDatabaseScriptableObject)
					continue;

				if (!databaseByNamespace.TryGetValue(config.GetType().Namespace ?? string.Empty, out var database))
					database = miscDatabase;

				if (database != null && modulesByDatabase.TryGetValue(database, out var module))
					module.Add(config);
			}

			var modules = new ModuleInfo[modulesByDatabase.Count];
			modulesByDatabase.Values.CopyTo(modules, 0);

			for (int i = 0; i < modules.Length; i++)
				modules[i].Sort(sortDisabledLast);

			Array.Sort(modules, (x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));

			return modules;
		}

		/// <summary>
		/// Одна база контента и её конфиги, сгруппированные по типу
		/// </summary>
		public class ModuleInfo
		{
			public ContentDatabaseScriptableObject Db { get; }
			public string Name => Db.name;
			public Dictionary<Type, List<ContentScriptableObject>> ConfigsByType { get; }

			public ModuleInfo(ContentDatabaseScriptableObject db)
			{
				Db = db;
				ConfigsByType = new Dictionary<Type, List<ContentScriptableObject>>();
			}

			public void Add(ContentScriptableObject config)
			{
				var type = config.GetType();
				if (!ConfigsByType.TryGetValue(type, out var configs))
				{
					configs = new List<ContentScriptableObject>();
					ConfigsByType.Add(type, configs);
				}

				configs.Add(config);
			}

			public void Sort(bool disabledLast)
			{
				foreach (var configs in ConfigsByType.Values)
					configs.Sort(disabledLast
						? CompareDisabledLast
						: CompareByCreationTime);
			}

			private static int CompareDisabledLast(ContentScriptableObject x, ContentScriptableObject y)
			{
				var enabledComparison = y.Enabled.CompareTo(x.Enabled);
				return enabledComparison != 0 ? enabledComparison : CompareByCreationTime(x, y);
			}

			private static int CompareByCreationTime(ContentScriptableObject x, ContentScriptableObject y)
				=> x.CreationTime.CompareTo(y.CreationTime);
		}
	}
}
