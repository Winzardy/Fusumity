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
using UnityEditor;
using UnityEngine;

namespace Content.ScriptableObjects.Editor
{
	[InitializeOnLoad]
	public static class ContentDatabaseCleanupOnStartup
	{
		static ContentDatabaseCleanupOnStartup()
		{
			EditorApplication.delayCall += () =>
			{
				foreach (var database in ContentDatabaseEditorUtility.Databases)
					ContentDatabaseEditorUtility.RequestCleanup(database);
			};
		}
	}

	public static class ContentDatabaseEditorUtility
	{
		private const string ADDRESSABLE_GROUP = "Content Runtime Database Group";
		private const string ADDRESSABLE_NAME_FORMAT = "Database/{0}";

		public const string DEFAULT_NAME_ENDING = "Database";

		private static bool _syncContentCalledThisFrame;

		private static HashSet<ContentDatabaseScriptableObject> _pendingCleanup = new();

		public static IEnumerable<ContentDatabaseScriptableObject> Databases
			=> ContentEditorCache.GetAssets<ContentDatabaseScriptableObject>();

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

			ContentEditorCache.Register(database);

			addressableName ??= name.Remove(DEFAULT_NAME_ENDING);
			database.MakeAddressable(
				ADDRESSABLE_GROUP,
				ADDRESSABLE_NAME_FORMAT.Format(addressableName),
				ContentDatabaseScriptableObject.ADDRESSABLE_DATABASE_LABEL,
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

		public static void AddToDatabase(ContentScriptableObject scriptableObject)
		{
			if (scriptableObject is ContentDatabaseScriptableObject)
				return;

			var database = GetDatabase(scriptableObject);
			Add(database);

			void Add(ContentDatabaseScriptableObject database)
			{
				if (database.scriptableObjects.Contains(scriptableObject))
					return;

				database.scriptableObjects.Add(scriptableObject);
				scriptableObject.SyncedUpdate();

				database.OnUpdateContent();
				EditorUtility.SetDirty(database);
				AssetDatabase.SaveAssetIfDirty(database);

				RequestCleanup(database);
			}
		}

		public static void RequestCleanup(ContentDatabaseScriptableObject database)
		{
			if (_pendingCleanup.Add(database))
			{
				EditorApplication.delayCall += () =>
				{
					database.Cleanup();
					_pendingCleanup.Remove(database);
				};
			}
		}

		public static void RemoveToDatabase(ContentScriptableObject scriptableObject)
		{
			if (scriptableObject is ContentDatabaseScriptableObject)
				return;

			var database = GetDatabase(scriptableObject);
			Remove(database);

			void Remove(ContentDatabaseScriptableObject database)
			{
				var remove = database.scriptableObjects.Remove(scriptableObject);

				if (!remove)
					return;

				database.OnUpdateContent();
				EditorUtility.SetDirty(database);
				AssetDatabase.SaveAssetIfDirty(database);
			}
		}

		public static ContentDatabaseScriptableObject GetDatabase(ContentScriptableObject scriptableObject)
		{
			MiscDatabaseScriptableObject miscDatabase = null;
			foreach (var database in Databases)
			{
				if (IsMatch(database, scriptableObject))
					return database;

				if (database is MiscDatabaseScriptableObject misc)
					miscDatabase = misc;
			}

			return miscDatabase;
		}

		public static void SyncContent()
		{
			if (_syncContentCalledThisFrame)
				return;

			_syncContentCalledThisFrame = true;
			EditorApplication.delayCall += ResetSyncContentCalledThisFrame;

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
			ContentEditorCache.ClearAndRefreshScrObjs();
			ContentEntryEditorUtility.ClearCache();
		}

		private static void ResetSyncContentCalledThisFrame()
		{
			_syncContentCalledThisFrame = false;
		}

		public static void ValidateDatabases()
		{
			foreach (var database in Databases)
				if (!database.Validate(out var message))
					ContentDebug.LogError($"Invalid database: {message}", database);
		}

		public static bool Validate(this ContentDatabaseScriptableObject database, out string message)
		{
			message = null;

			if (!database.TryGetAddressableEntry(out var entry))
			{
				message = "Not found addressable entry!";
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

						if (IsMatch(moduleName, scriptableObject))
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
								if (ContentAutoConstantsGeneratorMenu.IsEnable)
								{
									TryAddToGenerator(scriptableObject, dictionary);
								}
							}
						}
					}

					if (!collided)
					{
						database.scriptableObjects = new List<ContentScriptableObject>(all);

						if (remove)
							foreach (var scriptableObject in all)
								scriptableObjects.Remove(scriptableObject);

						if (ContentAutoConstantsGeneratorMenu.IsEnable)
						{
							foreach (var (type, content) in dictionary)
								ContentConstantGenerator.Generate(type, content);
						}

						database.Sort();
					}
				}
			}

			return !collided;
		}

		private static bool IsMatch(ContentDatabaseScriptableObject database, ContentScriptableObject scriptableObject)
		{
			var moduleName = database.GetType().Namespace;
			return IsMatch(moduleName, scriptableObject);
		}

		private static bool IsMatch(string moduleName, ContentScriptableObject scriptableObject)
		{
			var type = scriptableObject.GetType();
			return type.Namespace == moduleName;
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
							if (ContentAutoConstantsGeneratorMenu.IsEnable)
							{
								TryAddToGenerator(scriptableObject, dictionary);
							}
						}
					}

					if (!collided)
					{
						database.scriptableObjects = new List<ContentScriptableObject>(all);

						if (ContentAutoConstantsGeneratorMenu.IsEnable)
						{
							foreach (var (type, content) in dictionary)
								ContentConstantGenerator.Generate(type, content);
						}

						database.Sort();
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
							ContentDebug.LogWarning(
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
								if (!scriptableObject.enabled)
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

		public static IEnumerable<IUniqueContentEntryScriptableObject> GetScriptableObjectsByType(Type type)
		{
			foreach (var source in ContentEditorCache.GetAllSourceByValueType(type))
			{
				if (source.ContentEntry is IScriptableContentEntry scriptableContentEntry)
				{
					if (scriptableContentEntry.ScriptableObject is IUniqueContentEntryScriptableObject so)
						yield return so;
				}
			}
		}

		private static bool TryValidate(IContentScriptableObject scriptableObject)
		{
			if (scriptableObject is IValidatable validatable)
				return validatable.Validate(out _);

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

			if (valueType.HasAttribute<ConstantsAttribute>())
			{
				if (!dictionary.ContainsKey(valueType))
					dictionary.Add(valueType, new List<IUniqueContentEntryScriptableObject>());

				dictionary[valueType].Add(scriptableObject);
			}
			else if (scriptableObject.GetType().HasAttribute<ConstantsAttribute>())
			{
				if (!dictionary.ContainsKey(valueType))
					dictionary.Add(valueType, new List<IUniqueContentEntryScriptableObject>());

				dictionary[valueType].Add(scriptableObject);
			}
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

		public static TScrobject GetScrobjectFromDb<TScrobject, TDatabase>(string scrobjectId, bool useCache = true)
			where TScrobject : ContentScriptableObject
			where TDatabase : ContentDatabaseScriptableObject
		{
			if (scrobjectId.IsNullOrEmpty())
				return null;

			var db = useCache ? EditorAssetsCache.GetCachedAsset<TDatabase>() : AssetDatabaseUtility.GetAsset<TDatabase>();

			foreach (var scrobj in db.scriptableObjects)
			{
				if (scrobj is not IIdentifiable identifiable)
					continue;

				if (identifiable.Id == scrobjectId)
				{
					var cast = scrobj as TScrobject;
					if (cast == null)
					{
						Debug.LogError(
							$"Could not find valid scrobject of type [ {typeof(TScrobject).Name} ] " +
							$"with id [ {scrobjectId} ] in database of type [ {typeof(TDatabase).Name} ]." +
							$"\nDuplicate ids could be in place.");
					}

					return cast;
				}
			}

			return null;
		}
	}
}
