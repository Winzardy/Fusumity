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

		public static ModuleInfo[] GetModules(bool refresh = false)
		{
			if (refresh)
				ContentEditorCache.ClearAndRefreshScrObjs();

			var modulesByDatabase = new Dictionary<ContentDatabaseScriptableObject, ModuleInfo>();

			foreach (var database in Databases)
			{
				if (database != null)
					modulesByDatabase.Add(database, new ModuleInfo(database));
			}

			foreach (var config in ContentEditorCache.GetAssets<ContentScriptableObject>())
			{
				if (config == null || config is ContentDatabaseScriptableObject)
					continue;

				var database = ContentDatabaseEditorUtility.GetDatabase(config);
				if (database != null && modulesByDatabase.TryGetValue(database, out var module))
					module.Add(config);
			}

			var modules = new ModuleInfo[modulesByDatabase.Count];
			modulesByDatabase.Values.CopyTo(modules, 0);

			for (int i = 0; i < modules.Length; i++)
				modules[i].Sort();

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

			public void Sort()
			{
				foreach (var configs in ConfigsByType.Values)
					configs.Sort((x, y) => x.CreationTime.CompareTo(y.CreationTime));
			}
		}
	}
}
