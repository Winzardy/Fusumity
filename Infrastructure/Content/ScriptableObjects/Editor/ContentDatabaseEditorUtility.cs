using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AssetManagement.AddressableAssets.Editor;
using Content.Editor;
using Fusumity.Editor.Utility;
using Sapientia;
using Sapientia.Collections;
using Sapientia.Extensions;
using Sapientia.Extensions.Reflection;
using Sapientia.Pooling;
using Sirenix.Utilities;
using UI;
using UnityEditor;
using UnityEngine;

namespace Content.ScriptableObjects.Editor
{
	using CollectionsUtility = CollectionsExt;

	public static class ContentDatabaseEditorUtility
	{
		private const string ADDRESSABLE_GROUP = "Content Database Group";
		private const string ADDRESSABLE_NAME_FORMAT = "Database/{0}";

		public const string DEFAULT_NAME_ENDING = "Database";

		private static List<ContentDatabaseScriptableObject> _cache;

		public static List<ContentDatabaseScriptableObject> Databases
			=> _cache ??= AssetDatabaseUtility.GetAssets<ContentDatabaseScriptableObject>()
				.ToList();

		public static void Create<T>(string name = null, string addressableName = null) where T : ContentDatabaseScriptableObject
		{
			var database = AssetDatabaseUtility.GetAsset<T>(error: false);
			name ??= typeof(T).Name.Replace("ScriptableObject", string.Empty);
			if (database)
			{
				ContentDebug.LogError($"[ {name} ] already exits", database);
				Ping();
				return;
			}

			var path = string.Empty;
			var selection = Selection.activeObject;
			if (selection)
			{
				path = AssetDatabase.GetAssetPath(selection);

				if (!AssetDatabase.IsValidFolder(path))
					path = Path.GetDirectoryName(path);
			}

			database = AssetDatabaseUtility.CreateScriptableObject<T>(path, name);

			_cache?.Add(database);

			addressableName ??= name.Remove(DEFAULT_NAME_ENDING);
			database.MakeAddressable(
				ADDRESSABLE_GROUP,
				ADDRESSABLE_NAME_FORMAT.Format(addressableName),
				ContentDatabaseScriptableObject.LABEL,
				true
			);

			database.SyncContent();

			EditorUtility.SetDirty(AssetManagementEditorUtility.GetGroup(ADDRESSABLE_GROUP));

			Ping();

			void Ping()
			{
				EditorUtility.FocusProjectWindow();
				EditorGUIUtility.PingObject(database);
				Selection.activeObject = database;
			}
		}

		public static void SyncContent()
		{
			var scriptableObjects = AssetDatabaseUtility.GetAssets<ContentScriptableObject>()
				.ToList();
			var dbs = AssetDatabaseUtility.GetAssets<ContentDatabaseScriptableObject>();
			ContentDatabaseScriptableObject misc = null;
			try
			{
				foreach (var (database, index) in dbs.WithIndex())
				{
					EditorUtility.DisplayProgressBar("Update Content", database.name, index / (float) dbs.Length);
					if (database is MiscDatabaseScriptableObject)
					{
						misc = database;
						continue;
					}

					database.SyncContent(false, scriptableObjects);
				}

				misc?.SyncContent(false, scriptableObjects);
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}

			AssetDatabase.SaveAssets();
			ContentEditorCache.Refresh();
			ContentEntryEditorUtility.ClearCache();
		}

		public static bool Validate(this ContentDatabaseScriptableObject database, out string message)
		{
			message = null;

			if (!database.TryGetAddressableEntry(out var entry))
			{
				message = "Not found addressable entry!";
				return false;
			}

			if (!entry.labels.Contains(ContentDatabaseScriptableObject.LABEL))
			{
				message = $"Not found addressable label [ {ContentDatabaseScriptableObject.LABEL} ]!";
				return false;
			}

			if (entry.parentGroup.name != ADDRESSABLE_GROUP)
			{
				message = $"Invalid parent group [ {entry.parentGroup.name}] need [ {ADDRESSABLE_GROUP} ]!";
				return false;
			}

			return true;
		}

		public static void SyncContent<T>(this IEnumerable<T> enumerable)
			where T : ContentDatabaseScriptableObject
		{
			var scriptableObjects = AssetDatabaseUtility.GetAssets<ContentScriptableObject>().ToList();
			ContentDatabaseScriptableObject misc = null;
			foreach (var database in enumerable)
			{
				if (database is MiscDatabaseScriptableObject)
				{
					misc = database;
					continue;
				}

				database.SyncContent(true, scriptableObjects);
			}

			misc?.SyncContent(true, scriptableObjects);
		}

		public static void SyncContent(this ContentDatabaseScriptableObject database,
			bool saveAssets = false,
			List<ContentScriptableObject> scriptableObjects = null)
		{
			if (database is MiscDatabaseScriptableObject misc)
			{
				if (!misc.SyncDatabase(ref scriptableObjects) || !TryValidate(database))
				{
					ContentDebug.LogError($"Could not update [ {database.GetType().Name} ]", database);
					return;
				}
			}
			else
			{
				var remove = scriptableObjects != null;
				scriptableObjects ??= AssetDatabaseUtility.GetAssets<ContentScriptableObject>().ToList();

				if (!database.SyncDatabase(ref scriptableObjects, remove) || !TryValidate(database))
				{
					ContentDebug.LogError($"Could not update [ {database.GetType().Name} ]", database);
					return;
				}
			}

			database.OnUpdateContent();
			EditorUtility.SetDirty(database);

			if (saveAssets)
				AssetDatabase.SaveAssetIfDirty(database);

			if (ContentDebug.Logging.database)
			{
				var message = $"[ {database.GetType().Name} ] content is updated";

				if (!database.scriptableObjects.IsNullOrEmpty())
				{
					var collection = database.scriptableObjects.GetCompositeString();
					message += $":{collection}";
				}

				ContentDebug.Log(message, database);
			}
		}

		/// <param name="remove">Нужно ли удалять используемые ScriptableObject из списка который передали</param>
		private static bool SyncDatabase(this ContentDatabaseScriptableObject database,
			ref List<ContentScriptableObject> scriptableObjects,
			bool remove)
		{
			var moduleName = database.GetType().Namespace;

			var collisionsMap = new Dictionary<Type, HashSet<string>>();
			bool collided = false;

			using (ListPool<ContentScriptableObject>.Get(out var all))
			{
				using (DictionaryPool<Type, List<IUniqueContentEntryScriptableObject>>.Get(out var dictionary))
				{
					foreach (var scriptableObject in scriptableObjects)
					{
						if (scriptableObject is ContentDatabaseScriptableObject)
						{
							if (scriptableObject is IContentEntrySource)
								Refresh(scriptableObject);
							continue;
						}

						scriptableObject.Sync();

						var type = scriptableObject.GetType();
						if (type.Namespace == moduleName)
						{
							if (!ValidateByCollisions(scriptableObject, scriptableObjects, ref collisionsMap))
								collided = true;

							if (!collided && TryValidate(scriptableObject))
							{
								if (!scriptableObject.Enabled)
									continue;

								all.Add(scriptableObject);
								scriptableObject.SyncedUpdate();

								Refresh(scriptableObject);
								TryAddToGenerator(scriptableObject, dictionary);
							}
						}
					}

					if (!collided)
					{
						database.scriptableObjects = new List<ContentScriptableObject>(all);

						if (remove)
							foreach (var scriptableObject in all)
								scriptableObjects.Remove(scriptableObject);

						foreach (var (type, content) in dictionary)
							ContentConstantGenerator.Generate(type, content);
					}
				}
			}

			return !collided;
		}

		private static bool SyncDatabase(this MiscDatabaseScriptableObject database,
			ref List<ContentScriptableObject> scriptableObjects)
		{
			var collisionsMap = new Dictionary<Type, HashSet<string>>();
			bool collided = false;

			using (ListPool<ContentScriptableObject>.Get(out var all))
			{
				using (DictionaryPool<Type, List<IUniqueContentEntryScriptableObject>>.Get(out var dictionary))
				{
					foreach (var scriptableObject in scriptableObjects)
					{
						if (scriptableObject is ContentDatabaseScriptableObject)
						{
							if (scriptableObject is IContentEntrySource)
								Refresh(scriptableObject);
							continue;
						}

						if (!ValidateByCollisions(scriptableObject, scriptableObjects, ref collisionsMap))
							collided = true;

						if (!collided && TryValidate(scriptableObject))
						{
							if (!scriptableObject.Enabled)
								continue;

							all.Add(scriptableObject);
							Refresh(scriptableObject);
							TryAddToGenerator(scriptableObject, dictionary);
						}
					}

					if (!collided)
					{
						database.scriptableObjects = new(all);

						foreach (var (type, content) in dictionary)
							ContentConstantGenerator.Generate(type, content);
					}
				}
			}

			return !collided;
		}

		public static void TryRunRegenerateConstantsByAuto(bool force = false)
		{
			if (!ContentAutoConstantsGeneratorMenu.IsEnable && !force)
				return;

			var dbs = AssetDatabaseUtility.GetAssets<ContentDatabaseScriptableObject>();
			var scriptableObjects = AssetDatabaseUtility.GetAssets<ContentScriptableObject>();

			foreach (var db in dbs)
			{
				var moduleName = db.GetType().Namespace;

				var collisionsMap = new Dictionary<Type, HashSet<string>>();
				var collided = false;

				using (DictionaryPool<Type, List<IUniqueContentEntryScriptableObject>>.Get(out var dictionary))
				{
					foreach (var scriptableObject in scriptableObjects)
					{
						if (scriptableObject == db)
							continue;

						var type = scriptableObject.GetType();
						var typeNamespace = type.Namespace;
						if (typeNamespace == moduleName)
						{
							if (!ValidateByCollisions(scriptableObject, scriptableObjects, ref collisionsMap))
								collided = true;

							if (!collided && TryValidate(scriptableObject))
							{
								if (!scriptableObject.Enabled)
									continue;

								TryAddToGenerator(scriptableObject, dictionary);
							}
						}
						else
						{
							GUIDebug.LogWarning(
								$"Constant generation skipped for asset '{scriptableObject.name}' " +
								$"[{scriptableObject.GetType().FullName}] — namespace mismatch: " +
								$"actual = '{typeNamespace ?? "<null>"}', " +
								$"expected = '{moduleName}'",
								scriptableObject);
						}
					}

					if (!collided)
					{
						foreach (var (type, content) in dictionary)
							ContentConstantGenerator.Generate(type, content);
					}
					else
						ContentDebug.LogError($"Could not regenerate constants for database by name [ {db.GetType().Name} ]", db);
				}
			}
		}

		public static void TryRegenerateConstants(Type type)
		{
			if (!ContentAutoConstantsGeneratorMenu.IsEnable)
				return;

			var dbs = AssetDatabaseUtility.GetAssets<ContentDatabaseScriptableObject>();
			var scriptableObjects = AssetDatabaseUtility.GetAssets(type)
				.Cast<IUniqueContentEntryScriptableObject>();

			foreach (var db in dbs)
			{
				var moduleName = db.GetType().Namespace;

				var collisionsMap = new HashSet<string>();
				var collided = false;

				using (ListPool<IUniqueContentEntryScriptableObject>.Get(out var content))
				{
					foreach (var scriptableObject in scriptableObjects)
					{
						var scriptableObjectType = scriptableObject.GetType();
						if (scriptableObjectType.Namespace == moduleName)
						{
							if (!ValidateByCollisions(scriptableObject, scriptableObjects, ref collisionsMap))
								collided = true;

							if (!collided && TryValidate(scriptableObject))
							{
								if (!scriptableObject.Enabled)
									continue;

								content.Add(scriptableObject);
							}
						}
					}

					if (!collided)
					{
						ContentConstantGenerator.Generate(type, content);
					}
					else
						ContentDebug.LogError($"Could not regenerate constants for database by name [ {db.GetType().Name} ]", db);
				}
			}
		}

		private static bool TryValidate(IContentScriptableObject scriptableObject)
		{
			if (scriptableObject is IValidatable validatable)
				return validatable.Validate();

			return true;
		}

		private static bool ValidateByCollisions(
			ContentScriptableObject scriptableObject,
			IEnumerable<ContentScriptableObject> all,
			ref Dictionary<Type, HashSet<string>> collisionsMap)
		{
			if (scriptableObject is not IUniqueContentEntryScriptableObject uniqueScriptableObject)
				return true;

			var force = false;

			if (scriptableObject is IContentEntryScriptableObject contentScriptableObject)
				force = typeof(IExternallyIdentifiable).IsAssignableFrom(contentScriptableObject.ValueType);

			if (!force && !uniqueScriptableObject.UseCustomId)
				return true;

			var ids = all.OfType<IIdentifiable>();
			return ValidateByCollisions(uniqueScriptableObject, ids, ref collisionsMap);
		}

		private static bool ValidateByCollisions(
			ContentScriptableObject scriptableObject,
			IEnumerable<IIdentifiable> all,
			ref Dictionary<Type, HashSet<string>> collisionsMap)
		{
			if (scriptableObject is not IIdentifiable identifiable)
				return true;

			return ValidateByCollisions(identifiable, all, ref collisionsMap);
		}

		private static bool ValidateByCollisions(
			IIdentifiable source,
			IEnumerable<IIdentifiable> all,
			ref Dictionary<Type, HashSet<string>> collisionsMap)
		{
			var type = source.GetType();

			if (!collisionsMap.TryGetValue(type, out var checker))
			{
				checker = new HashSet<string>();
				collisionsMap[type] = checker;
			}

			return ValidateByCollisions(source, all, ref checker);
		}

		private static bool ValidateByCollisions(
			IIdentifiable source,
			IEnumerable<IIdentifiable> all,
			ref HashSet<string> hashSet)
		{
			if (hashSet.Add(source.Id))
				return true;

			try
			{
				var instances = all
					.ToList()
					.FindAll(x => x.Id == source.Id);

				ContentDebug.LogError(
					$"Detected duplicate id: [ {source.Id} ] " +
					$"for scriptableObject of type: [ {source.GetType().Name} ]", source);

				foreach (var collided in instances)
				{
					if (collided is ScriptableObject scriptableObject)
						ContentDebug.LogWarning($"Collided scriptableObject: [ {scriptableObject.name} ]", scriptableObject);
				}

				return false;
			}
			catch (Exception e)
			{
				ContentDebug.LogError(
					$"{e.Message}", source);

				return false;
			}
		}

		private static void TryAddToGenerator(ContentScriptableObject scriptableObject,
			Dictionary<Type, List<IUniqueContentEntryScriptableObject>> dictionary)
		{
			if (scriptableObject is not IUniqueContentEntryScriptableObject uniqueContentEntryScriptableObject)
				return;

			TryAddToGenerator(uniqueContentEntryScriptableObject, dictionary);
		}

		private static void TryAddToGenerator(IUniqueContentEntryScriptableObject scriptableObject,
			Dictionary<Type, List<IUniqueContentEntryScriptableObject>> dictionary)
		{
			var valueType = scriptableObject.ValueType;

			if (!valueType.HasAttribute<ConstantsAttribute>())
				return;

			if (!dictionary.ContainsKey(valueType))
				dictionary.Add(valueType, new List<IUniqueContentEntryScriptableObject>());

			dictionary[valueType].Add(scriptableObject);
		}

		public static string ToLabel(this ContentDatabaseScriptableObject database)
			=> database.name.Remove(DEFAULT_NAME_ENDING);

		private static void Refresh(ContentScriptableObject scriptableObject)
		{
			var originRefreshEnabled = ContentDebug.Logging.Nested.refresh;
			ContentDebug.Logging.Nested.refresh = false;
			scriptableObject.Refresh();
			ContentDebug.Logging.Nested.refresh = originRefreshEnabled;
		}
	}
}
