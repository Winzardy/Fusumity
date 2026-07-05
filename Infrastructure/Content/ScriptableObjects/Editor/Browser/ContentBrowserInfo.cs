using System;
using System.Collections.Generic;
using Content.ScriptableObjects;
using Fusumity.Editor.Utility;
using Sapientia.Collections;

namespace Content.Editor
{
	/// <summary>
	/// Сбор баз контента и группировка их конфигов по типам для Content Browser
	/// </summary>
	public static class ContentBrowserInfo
	{
		private static ContentDatabaseScriptableObject[] _databases;

		public static ContentDatabaseScriptableObject[] Databases
		{
			get
			{
				_databases ??= AssetDatabaseUtility.GetAssets<ContentDatabaseScriptableObject>();
				return _databases;
			}
		}

		public static ModuleInfo[] GetModules(bool refresh = false)
		{
			if (refresh)
				_databases = null;

			var databases = Databases;
			var modules = new ModuleInfo[databases.Length];

			for (int i = 0; i < databases.Length; i++)
				modules[i] = new ModuleInfo(databases[i]);

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
				ConfigsByType = GroupByType(db);
			}

			private static Dictionary<Type, List<ContentScriptableObject>> GroupByType(ContentDatabaseScriptableObject db)
			{
				if (db.scriptableObjects.IsNullOrEmpty())
					return null;

				var dict = new Dictionary<Type, List<ContentScriptableObject>>();

				for (int i = 0; i < db.scriptableObjects.Count; i++)
				{
					var so = db.scriptableObjects[i];
					if (so == null)
						continue;

					var type = so.GetType();
					if (!dict.TryGetValue(type, out var list))
					{
						list = new List<ContentScriptableObject>();
						dict.Add(type, list);
					}

					list.Add(so);
				}

				return dict;
			}
		}
	}
}
