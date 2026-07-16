using System;
using System.Collections.Generic;
using System.IO;
using Content.ScriptableObjects;
using Content.ScriptableObjects.Editor;
using Fusumity.Attributes;
using Fusumity.Editor;
using Fusumity.Editor.Utility;
using Fusumity.Utility;
using Sapientia.Utility;
using Sapientia.Extensions;
using Sapientia.Pooling;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Content.Editor
{
	public class ContentBrowserWindow : OdinMenuEditorWindow
	{
		public const string WINDOW_NAME = " Content Browser";

		// Флаг для пересборки дерева с актуальным списком баз
		private bool _forceRefresh;
		private bool _selectFirstAfterRefresh;
		private string _selectPinnedKeyAfterRefresh;
		private string _selectOriginalPathAfterRefresh;
		private string _selectAssetGuidAfterRefresh;
		private bool _selectPendingMenuItemScheduled;
		private readonly List<string> _navigationHistory = new();
		private readonly Dictionary<ContentScriptableObject, OdinMenuItem> _assetMenuItems = new();
		private readonly Dictionary<string, OdinMenuItem> _categoryMenuItems = new();
		private readonly Dictionary<string, OdinMenuItem> _pinnedMenuItems = new();
		private readonly Dictionary<string, double> _lastPinnedAtByKey = new();
		private int _navigationHistoryIndex = -1;
		private OdinMenuItem _lastNavigationHistoryItem;
		private string _lastNavigationHistoryPath;
		private bool _skipNextNavigationHistoryRecord;
		private UnityEngine.Object _lastProjectSelection;

		private const string PINNED_GROUP = "Pinned";
		private const string PINNED_PREF_KEY = "ContentBrowser.Pinned";
		private const string AUTO_SYNC_PREF_KEY = "ContentBrowser.AutoSyncProjectSelection";
		private const string GROUP_CONFIGS_BY_ID_PATH_PREF_KEY = "ContentBrowser.GroupConfigsByIdPath";
		private const string SORT_DISABLED_CONFIGS_LAST_PREF_KEY = "ContentBrowser.SortDisabledConfigsLast";
		private const string PINNED_CATEGORY_PREFIX = "category:";
		private const string NEW_CONFIG_MENU_ITEM_PREFIX = "New";
		private const string SCRIPTABLE_OBJECT_SUFFIX = "ScriptableObject";
		private const string CONFIG_SUFFIX = "Config";
		private const string DATABASE_SUFFIX = "Database";
		private const string CATEGORY_SUFFIX = "'s";
		private const string BREADCRUMB_LINK_COLOR = "FFFFFF";

		private const int MAX_NAVIGATION_HISTORY_COUNT = 64;
		private const double SELECT_ORIGINAL_AFTER_UNPIN_SECONDS = 5d;

		private const float NAVIGATION_SCROLLBAR_HIT_WIDTH = 18f;
		private const float NAVIGATION_SCROLLBAR_RESIZE_GAP = 4f;
		private const float INSPECTOR_BOTTOM_PADDING = 10f;
		private const string DOCUMENTATION_TOOLTIP_FORMAT = "Открыть документацию для типа: {0}";
		private const string GENERATE_CONSTANTS_TOOLTIP_FORMAT = "Сгенерировать константы для типа: {0}";

		// Ключи закреплённых баз/конфигов/категорий (хранятся в EditorPrefs)
		private HashSet<string> _pinned;
		private HashSet<string> Pinned => _pinned ??= LoadPinned();

		private static readonly GUIContent OPEN_ORIGINAL_PAGE_TOOLTIP = new(string.Empty, "Открыть оригинальную страницу");
		private static readonly GUIContent OPEN_PINNED_PAGE_TOOLTIP = new(string.Empty, "Открыть закреплённую страницу");
		private static readonly GUIContent SETTINGS_TOOLTIP = new(string.Empty, "Настройки Content Browser");
		private static readonly GUIContent BACK_TOOLTIP = new(string.Empty, "Back");
		private static readonly GUIContent FORWARD_TOOLTIP = new(string.Empty, "Forward");
		private static readonly GUIContent DELETE_ASSET_TOOLTIP = new(string.Empty, "Удалить выбранный Asset");
		private static readonly GUIContent CATEGORY_DELETE_MODE_TOOLTIP = new(string.Empty, "Выбрать конфиги для удаления");
		private static readonly GUIContent APPLY_CATEGORY_DELETE_TOOLTIP = new(string.Empty, "Удалить выбранные конфиги");
		private static readonly GUIContent CANCEL_CATEGORY_DELETE_TOOLTIP = new(string.Empty, "Отменить удаление");
		private static readonly GUIContent PINNED_DELETE_MODE_TOOLTIP = new(string.Empty, "Выбрать закрепления для удаления");
		private static readonly GUIContent APPLY_PINNED_DELETE_TOOLTIP = new(string.Empty, "Убрать выбранные закрепления");
		private static readonly GUIContent CANCEL_PINNED_DELETE_TOOLTIP = new(string.Empty, "Отменить удаление");
		private static readonly GUIContent CLEAR_PINNED_TOOLTIP = new(string.Empty, "Очистить все закрепления");
		private static readonly GUIContent BREADCRUMB_SEPARATOR = new(" / ");
		private static readonly GUIContent NAVIGATION_SUFFIX_MEASURE_CONTENT = new();
		private static readonly Dictionary<GUIStyle, GUIStyle> _navigationSuffixStyles = new();

		private OdinMenuItem _breadcrumbLeaf;
		private readonly List<OdinMenuItem> _breadcrumbNodes = new();
		private readonly List<GUIContent> _breadcrumbContents = new();

		private static readonly Color APPLY_DELETE_ENABLED_COLOR = new(0.35f, 0.8f, 0.35f, 1f);
		private static readonly Color CANCEL_DELETE_COLOR = new(1f, 0.05f, 0.05f, 1f);

		private static Type[] _creatableConfigTypes;
		private static readonly Dictionary<Type, Type> _contentEntryValueTypes = new();

		private static bool? _autoSyncProjectSelection;
		private static bool? _groupConfigsByIdPath;
		private static bool? _sortDisabledConfigsLast;

		private enum NavigationScrollTarget
		{
			None,
			Top,
			Bottom
		}

		private static bool AutoSyncProjectSelection
		{
			get
			{
				if (!_autoSyncProjectSelection.HasValue)
					_autoSyncProjectSelection = EditorPrefs.GetBool(AUTO_SYNC_PREF_KEY, true);

				return _autoSyncProjectSelection.Value;
			}
			set
			{
				_autoSyncProjectSelection = value;
				EditorPrefs.SetBool(AUTO_SYNC_PREF_KEY, value);
			}
		}

		private static bool GroupByIdPath
		{
			get
			{
				if (!_groupConfigsByIdPath.HasValue)
					_groupConfigsByIdPath = EditorPrefs.GetBool(GROUP_CONFIGS_BY_ID_PATH_PREF_KEY, false);

				return _groupConfigsByIdPath.Value;
			}
			set
			{
				_groupConfigsByIdPath = value;
				EditorPrefs.SetBool(GROUP_CONFIGS_BY_ID_PATH_PREF_KEY, value);
			}
		}

		private static bool SortByEnabled
		{
			get
			{
				if (!_sortDisabledConfigsLast.HasValue)
					_sortDisabledConfigsLast = EditorPrefs.GetBool(SORT_DISABLED_CONFIGS_LAST_PREF_KEY, true);

				return _sortDisabledConfigsLast.Value;
			}
			set
			{
				_sortDisabledConfigsLast = value;
				EditorPrefs.SetBool(SORT_DISABLED_CONFIGS_LAST_PREF_KEY, value);
			}
		}

		[MenuItem(ContentMenuConstants.TOOLS_MENU + "Browser", priority = 10)]
		public static void OpenWindow()
		{
			var window = GetWindow<ContentBrowserWindow>();
			window.CreateTitle();
			window.position = GUIHelper.GetEditorWindowRect().AlignCenter(1000, 650);
			window.Show();
		}

		public static void OpenAsset(ContentScriptableObject asset)
		{
			if (asset == null)
				return;

			var window = GetWindow<ContentBrowserWindow>();
			window.CreateTitle();
			window.Show();
			window.Focus();
			window.SelectAsset(asset);
		}

		private static bool TryGetSelectedProjectAsset(out ContentScriptableObject asset)
		{
			asset = Selection.activeObject as ContentScriptableObject;
			return asset != null; // && AssetDatabase.Contains(asset);
		}

		protected override void Initialize()
		{
			CreateTitle();
		}

		protected override void OnEnable()
		{
			base.OnEnable();

			CreateTitle();
			_lastProjectSelection = Selection.activeObject;

			Selection.selectionChanged -= OnProjectSelectionChanged;
			Selection.selectionChanged += OnProjectSelectionChanged;
		}

		private bool _creating;

		private void OnValidate() => CreateTitle();

		private void CreateTitle()
		{
			titleContent = new GUIContent(WINDOW_NAME, EditorIcons.FileCabinet.Active);
		}

		protected override void OnDestroy()
		{
			Selection.selectionChanged -= OnProjectSelectionChanged;

			base.OnDestroy();
		}

		private void OnProjectSelectionChanged()
		{
			if (this == null)
				return;

			var projectSelection = Selection.activeObject;
			if (projectSelection == _lastProjectSelection)
				return;

			_lastProjectSelection = projectSelection;

			if (!AutoSyncProjectSelection)
				return;

			if (TryGetSelectedProjectAsset(out var asset))
				SelectAsset(asset);
		}

		protected override OdinMenuTree BuildMenuTree()
		{
			var tree = new OdinMenuTree(supportsMultiSelect: false);
			_navigationSuffixStyles.Clear();

			// Встроенный поиск Odin по дереву (точный Contains вместо fuzzy — иначе guid даёт кучу ложных совпадений)
			tree.Config.DrawSearchToolbar = true;
			tree.Config.SearchFunction = SearchItem;
			tree.DefaultMenuStyle.IconSize = 18f;

			_assetMenuItems.Clear();
			_categoryMenuItems.Clear();
			_pinnedMenuItems.Clear();

			var modules = ContentBrowserInfo.GetModules(_forceRefresh, SortByEnabled);
			_forceRefresh = false;

			for (int i = 0; i < modules.Length; i++)
			{
				var module = modules[i];
				if (module.Db == null)
					continue;

				// Категория верхнего уровня — база контента (по клику открывается сама база)
				var dbName = Nicify(module.Name);
				var dbItem = new ContentMenuItem(tree, dbName, module.Db)
				{
					SdfIcon = IconFor(module.Db)
				};

				tree.AddMenuItemAtPath(string.Empty, dbItem);
				RegisterAssetMenuItem(module.Db, dbItem);

				// Группировка по типам внутри базы
				using (ListPool<Type>.Get(out var configTypes))
				using (HashSetPool<Type>.Get(out var uniqueConfigTypes))
				{
					CollectConfigTypes(module, configTypes, uniqueConfigTypes);
					if (configTypes.IsNullOrEmpty())
						continue;

					configTypes.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));

					for (int typeIndex = 0; typeIndex < configTypes.Count; typeIndex++)
					{
						var type = configTypes[typeIndex];
						List<ContentScriptableObject> configs = null;
						module.ConfigsByType?.TryGetValue(type, out configs);
						var hasConfigs = !configs.IsNullOrEmpty();

						// Single Content Entry — без категории, сразу под базой (выделяется иконкой и цветом)
						if (typeof(SingleContentEntryScriptableObject).IsAssignableFrom(type) &&
							!typeof(ContentDatabaseScriptableObject).IsAssignableFrom(type))
						{
							if (!hasConfigs)
								continue;

							for (int j = 0; j < configs.Count; j++)
								AddConfigLeaf(tree, dbName, configs[j], single: true);

							continue;
						}

						if (!CanCreateContentEntry(type))
							continue;

						var typeName = GetConfigDisplayName(type);
						var typePath = $"{dbName}/{typeName}";
						var page = new CategoryPage(this, module.Db, type, typePath, CategoryPinKey(module.Db, type), typeName);

						// Страница категории — список конфигов этого типа (рисуется при клике на категорию)
						if (hasConfigs)
							for (int j = 0; j < configs.Count; j++)
								page.AddItem(configs[j]);

						var categoryItem = new CategoryMenuItem(tree, typeName, page)
						{
							SdfIcon = SdfIconType.FolderFill
						};

						tree.AddMenuItemAtPath(dbName, categoryItem);
						RegisterCategoryMenuItem(page, categoryItem);
						AddCreateConfigLeaf(tree, typePath, page);

						if (!hasConfigs)
							continue;

						// Листья — конкретные конфиги (выключенные рисуются серым), при выборе открывается inline-редактор
						using (DictionaryPool<string, int>.Get(out var idPathGroupCounts))
						{
							if (GroupByIdPath)
								BuildIdPathGroupCounts(configs, idPathGroupCounts);

							for (int j = 0; j < configs.Count; j++)
							{
								var leafPath = GetConfigLeafPath(typePath, configs[j], idPathGroupCounts);
								AddConfigLeaf(tree, leafPath, configs[j], single: false);
								AddConfigToIdGroupPages(tree, page, leafPath, configs[j]);
							}
						}
					}
				}
			}

			// Расширяем поиск: ищем не только по имени, но и по Id / Guid / Guid вложенных Entry
			BuildSearchStrings(tree);

			// Закреплённые элементы — отдельной группой
			BuildPinnedSection(tree);

			tree.SortMenuItemsByName();
			if (SortByEnabled)
				MoveDisabledConfigsToBottom(tree.RootMenuItem);
			MoveCreateConfigItemsToBottom(tree.RootMenuItem);
			MoveMiscDatabaseToBottom(tree);

			// Группа "Pinned" всегда сверху (в поиске её копии скрываются — см. SearchItem)
			MovePinnedToTop(tree);
			BuildPinnedSeparator(tree);

			if (HasPendingMenuSelection())
				ScheduleSelectPendingMenuItem();
			else if (_selectFirstAfterRefresh)
				ScheduleSelectFirstMenuItem();

			return tree;
		}

		private static void CollectConfigTypes(ContentBrowserInfo.ModuleInfo module, List<Type> types, HashSet<Type> uniqueTypes)
		{
			if (module.ConfigsByType != null)
			{
				foreach (var pair in module.ConfigsByType)
					AddType(types, uniqueTypes, pair.Key);
			}

			foreach (var type in GetCreatableConfigTypes())
			{
				if (IsCreatableInDatabase(module.Db, type))
					AddType(types, uniqueTypes, type);
			}
		}

		private static Type[] GetCreatableConfigTypes()
		{
			if (_creatableConfigTypes != null)
				return _creatableConfigTypes;

			using (ListPool<Type>.Get(out var types))
			{
				foreach (var type in TypeCache.GetTypesDerivedFrom<ContentEntryScriptableObject>())
				{
					if (CanCreateContentEntry(type))
						types.Add(type);
				}

				_creatableConfigTypes = types.ToArray();
			}

			return _creatableConfigTypes;
		}

		private static void AddType(List<Type> types, HashSet<Type> uniqueTypes, Type type)
		{
			if (type != null && uniqueTypes.Add(type))
				types.Add(type);
		}

		private static bool IsCreatableInDatabase(ContentDatabaseScriptableObject database, Type type)
		{
			if (database == null || type == null)
				return false;

			if (database is MiscDatabaseScriptableObject)
				return !HasExplicitDatabaseFor(type.Namespace);

			return string.Equals(database.GetType().Namespace, type.Namespace, StringComparison.Ordinal);
		}

		private static bool HasExplicitDatabaseFor(string typeNamespace)
		{
			foreach (var database in ContentBrowserInfo.Databases)
			{
				if (database == null || database is MiscDatabaseScriptableObject)
					continue;

				if (string.Equals(database.GetType().Namespace, typeNamespace, StringComparison.Ordinal))
					return true;
			}

			return false;
		}

		private static bool CanCreateContentEntry(Type type)
		{
			return type != null &&
				typeof(ContentEntryScriptableObject).IsAssignableFrom(type) &&
				!type.IsAbstract &&
				!type.IsGenericTypeDefinition;
		}

		private static Type GetContentEntryValueType(Type type)
		{
			if (type == null)
				return null;

			if (_contentEntryValueTypes.TryGetValue(type, out var valueType))
				return valueType;

			foreach (var interfaceType in type.GetInterfaces())
			{
				if (interfaceType.IsGenericType &&
					interfaceType.GetGenericTypeDefinition() == typeof(IContentEntryScriptableObject<>))
				{
					valueType = interfaceType.GetGenericArguments()[0];
					break;
				}
			}

			_contentEntryValueTypes[type] = valueType;
			return valueType;
		}

		// Группа "Pinned" сверху — закреплённые базы/конфиги/категории
		private void BuildPinnedSection(OdinMenuTree tree)
		{
			if (Pinned.Count == 0)
				return;

			using (ListPool<PinnedEntry>.Get(out var items))
			{
				foreach (var key in Pinned)
				{
					if (TryGetPinnedEntry(key, out var item))
						items.Add(item);
				}

				if (items.Count == 0)
					return;

				items.Sort(ComparePinnedEntries);

				var page = new PinnedPage(this);
				var rows = new PinnedRow[items.Count];
				for (int i = 0; i < items.Count; i++)
					rows[i] = new PinnedRow(this, page, items[i].Key, items[i].Name, items[i].MenuPath, items[i].Asset);

				page.Items = rows;
				tree.Add(PINNED_GROUP, page, SdfIconType.PinAngleFill);

				for (int i = 0; i < items.Count; i++)
				{
					var pinned = items[i];
					OdinMenuItem item;

					if (pinned.Asset != null)
						item = new PinnedMenuItem(tree, pinned.Name, pinned.Asset, pinned.MenuPath, pinned.Key)
						{
							SdfIcon = pinned.Icon
						};
					else
						item = new PinnedCategoryMenuItem(tree, pinned.Name, pinned.Category, pinned.MenuPath, pinned.Key)
						{
							SdfIcon = pinned.Icon
						};

					tree.AddMenuItemAtPath(PINNED_GROUP, item);
					_pinnedMenuItems[pinned.Key] = item;
				}
			}
		}

		private static int ComparePinnedEntries(PinnedEntry x, PinnedEntry y)
		{
			if (SortByEnabled)
			{
				var disabledComparison = IsDisabledConfig(x.Asset).CompareTo(IsDisabledConfig(y.Asset));
				if (disabledComparison != 0)
					return disabledComparison;
			}

			return string.Compare(x.Name, y.Name, StringComparison.Ordinal);
		}

		private void BuildSearchStrings(OdinMenuTree tree)
		{
			foreach (var item in tree.EnumerateTree())
			{
				var path = item.GetFullPath();
				item.SearchString = item.Value is IUniqueContentEntrySource source
					? BuildSearchString(path, source)
					: path;
			}
		}

		private bool TryGetPinnedEntry(string key, out PinnedEntry entry)
		{
			entry = null;

			if (IsCategoryPinKey(key))
				return TryGetPinnedCategoryEntry(key, out entry);

			var so = key.ToAsset<ContentScriptableObject>();
			if (so == null)
				return false;

			var item = FindMenuItemWithAsset(so);
			entry = new PinnedEntry(key, item?.Name ?? DisplayNameFor(so), item?.GetFullPath(), IconFor(so), so, null);
			return true;
		}

		private bool TryGetPinnedCategoryEntry(string key, out PinnedEntry entry)
		{
			entry = null;

			if (_categoryMenuItems.TryGetValue(key, out var item) && item.Value is CategoryPage page)
			{
				entry = new PinnedEntry(key, item.Name, item.GetFullPath(), SdfIconType.Folder2Open, null, page);
				return true;
			}

			return false;
		}

		private OdinMenuItem FindMenuItemWithAsset(ContentScriptableObject asset)
		{
			return asset != null && _assetMenuItems.TryGetValue(asset, out var item) ? item : null;
		}

		private void RegisterAssetMenuItem(ContentScriptableObject asset, OdinMenuItem item)
		{
			if (asset != null && item != null && !_assetMenuItems.ContainsKey(asset))
				_assetMenuItems.Add(asset, item);
		}

		private void RegisterCategoryMenuItem(CategoryPage page, OdinMenuItem item)
		{
			if (page != null && item != null && !page.PinKey.IsNullOrEmpty() && !_categoryMenuItems.ContainsKey(page.PinKey))
				_categoryMenuItems.Add(page.PinKey, item);
		}

		private static string DisplayNameFor(ContentScriptableObject so)
		{
			return so is ContentDatabaseScriptableObject ? Nicify(so.name) : so.name;
		}

		private static string CategoryPinKey(ContentDatabaseScriptableObject db, Type type)
		{
			var dbGuid = db.ToGuid();
			var typeName = type.AssemblyQualifiedName;

			if (dbGuid.IsNullOrEmpty() || typeName.IsNullOrEmpty())
				return null;

			return $"{PINNED_CATEGORY_PREFIX}{dbGuid}:{typeName}";
		}

		private static bool IsCategoryPinKey(string key)
		{
			return !key.IsNullOrEmpty() && key.StartsWith(PINNED_CATEGORY_PREFIX, StringComparison.Ordinal);
		}

		private static void MovePinnedToTop(OdinMenuTree tree)
		{
			var root = tree.RootMenuItem.ChildMenuItems;
			for (int i = 0; i < root.Count; i++)
			{
				if (root[i].Name != PINNED_GROUP)
					continue;

				var item = root[i];
				root.RemoveAt(i);
				root.Insert(0, item);
				return;
			}
		}

		private static void MoveMiscDatabaseToBottom(OdinMenuTree tree)
		{
			var root = tree.RootMenuItem.ChildMenuItems;
			for (int i = 0; i < root.Count; i++)
			{
				if (root[i].Value is not MiscDatabaseScriptableObject)
					continue;

				var item = root[i];
				root.RemoveAt(i);
				root.Add(item);
				return;
			}
		}

		private static void BuildPinnedSeparator(OdinMenuTree tree)
		{
			var root = tree.RootMenuItem.ChildMenuItems;
			for (int i = 0; i < root.Count; i++)
			{
				if (root[i].Name != PINNED_GROUP)
					continue;

				root.Insert(i + 1, new SeparatorMenuItem(tree));
				return;
			}
		}

		private void ScheduleSelectFirstMenuItem()
		{
			_selectFirstAfterRefresh = false;
			EditorApplication.delayCall += SelectFirstMenuItem;
		}

		private bool HasPendingMenuSelection()
		{
			return !_selectPinnedKeyAfterRefresh.IsNullOrEmpty() ||
				!_selectOriginalPathAfterRefresh.IsNullOrEmpty() ||
				!_selectAssetGuidAfterRefresh.IsNullOrEmpty();
		}

		private void ScheduleSelectPendingMenuItem()
		{
			if (_selectPendingMenuItemScheduled)
				return;

			_selectPendingMenuItemScheduled = true;
			EditorApplication.delayCall += SelectPendingMenuItem;
		}

		private void SelectPendingMenuItem()
		{
			if (this == null)
				return;

			_selectPendingMenuItemScheduled = false;
			var pinnedKey = _selectPinnedKeyAfterRefresh;
			var originalPath = _selectOriginalPathAfterRefresh;
			var assetGuid = _selectAssetGuidAfterRefresh;
			_selectPinnedKeyAfterRefresh = null;
			_selectOriginalPathAfterRefresh = null;
			_selectAssetGuidAfterRefresh = null;

			OdinMenuItem item = null;
			if (!assetGuid.IsNullOrEmpty())
			{
				var asset = assetGuid.ToAsset<ContentScriptableObject>();
				if (asset != null)
					item = FindMenuItemWithAsset(asset);
			}

			item ??= FindPinnedMenuItem(pinnedKey);
			if (item == null && !originalPath.IsNullOrEmpty())
				item = MenuTree?.GetMenuItem(originalPath);

			item?.Select();
			Repaint();
		}

		private void SelectFirstMenuItem()
		{
			if (this == null)
				return;

			var item = GetFirstNavigationItem(MenuTree);
			item?.Select();
			Repaint();
		}

		private static OdinMenuItem GetFirstNavigationItem(OdinMenuTree tree)
		{
			if (tree == null)
				return null;

			var root = tree.RootMenuItem.ChildMenuItems;
			for (int i = 0; i < root.Count; i++)
			{
				if (root[i].Value is SeparatorMenuAction)
					continue;

				return root[i];
			}

			return null;
		}

		private OdinMenuItem FindPinnedMenuItem(string key)
		{
			return !key.IsNullOrEmpty() && _pinnedMenuItems.TryGetValue(key, out var item) ? item : null;
		}

		private static SdfIconType IconFor(ContentScriptableObject so)
		{
			return so switch
			{
				ContentDatabaseScriptableObject database => database is MiscDatabaseScriptableObject
					? SdfIconType.JournalX
					: IsSingleEntryDatabase(database)
						? SdfIconType.JournalText
						: SdfIconType.Journal,
				SingleContentEntryScriptableObject => SdfIconType.FileEarmarkTextFill,
				_ => SdfIconType.FileEarmarkText
			};
		}

		private static bool IsSingleEntryDatabase(ContentDatabaseScriptableObject database)
		{
			for (var type = database.GetType(); type != null; type = type.BaseType)
			{
				if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ContentDatabaseScriptableObject<>))
					return true;
			}

			return false;
		}

		private static HashSet<string> LoadPinned()
		{
			var set = new HashSet<string>();
			var raw = EditorPrefs.GetString(PINNED_PREF_KEY, string.Empty);
			if (!raw.IsNullOrEmpty())
			{
				foreach (var guid in raw.Split('|'))
				{
					if (!guid.IsNullOrEmpty())
						set.Add(guid);
				}
			}

			return set;
		}

		private bool IsPinned(ContentScriptableObject so)
		{
			return so != null && Pinned.Contains(so.ToGuid());
		}

		private bool IsPinned(CategoryPage page)
		{
			return page != null && !page.PinKey.IsNullOrEmpty() && Pinned.Contains(page.PinKey);
		}

		private void TogglePin(ContentScriptableObject so, OdinMenuItem selectedItem = null)
		{
			if (so == null)
				return;

			var guid = so.ToGuid();
			TogglePin(guid, GetOriginalMenuPath(selectedItem));
		}

		private void TogglePin(CategoryPage page, OdinMenuItem selectedItem = null)
		{
			if (page == null || page.PinKey.IsNullOrEmpty())
				return;

			TogglePin(page.PinKey, GetOriginalMenuPath(selectedItem));
		}

		private void TogglePin(string key, string originalMenuPath = null)
		{
			if (key.IsNullOrEmpty())
				return;

			if (Pinned.Remove(key))
			{
				_selectPinnedKeyAfterRefresh = null;
				_selectOriginalPathAfterRefresh = WasPinnedRecently(key) ? originalMenuPath : null;
				_lastPinnedAtByKey.Remove(key);
			}
			else
			{
				Pinned.Add(key);
				_lastPinnedAtByKey[key] = EditorApplication.timeSinceStartup;
				_selectPinnedKeyAfterRefresh = key;
				_selectOriginalPathAfterRefresh = originalMenuPath;
			}

			EditorPrefs.SetString(PINNED_PREF_KEY, string.Join("|", Pinned));
			ForceMenuTreeRebuild();
		}

		private bool WasPinnedRecently(string key)
		{
			return !key.IsNullOrEmpty() &&
				_lastPinnedAtByKey.TryGetValue(key, out var pinnedAt) &&
				EditorApplication.timeSinceStartup - pinnedAt <= SELECT_ORIGINAL_AFTER_UNPIN_SECONDS;
		}

		private bool TryRemovePinned(List<string> keys)
		{
			if (keys.IsNullOrEmpty())
				return false;

			var changed = false;
			for (int i = 0; i < keys.Count; i++)
			{
				var key = keys[i];
				if (key.IsNullOrEmpty())
					continue;

				if (Pinned.Remove(key))
				{
					changed = true;
					_lastPinnedAtByKey.Remove(key);
				}
			}

			if (!changed)
				return false;

			EditorPrefs.SetString(PINNED_PREF_KEY, string.Join("|", Pinned));
			_selectPinnedKeyAfterRefresh = null;
			_selectOriginalPathAfterRefresh = Pinned.Count > 0 ? PINNED_GROUP : null;
			_selectFirstAfterRefresh = Pinned.Count == 0;
			ForceMenuTreeRebuild();
			return true;
		}

		private bool TryClearPinned()
		{
			if (Pinned.Count == 0)
				return false;

			if (!EditorUtility.DisplayDialog(
				"Clear Pinned",
				"Are you sure you want to clear all pinned items?",
				"Clear",
				"Cancel"))
				return false;

			Pinned.Clear();
			_lastPinnedAtByKey.Clear();
			EditorPrefs.SetString(PINNED_PREF_KEY, string.Empty);
			_selectPinnedKeyAfterRefresh = null;
			_selectOriginalPathAfterRefresh = null;
			_selectFirstAfterRefresh = true;
			ForceMenuTreeRebuild();
			return true;
		}

		private void SelectAsset(ContentScriptableObject asset)
		{
			if (asset == null)
				return;

			var item = MenuTree == null ? null : FindMenuItemWithAsset(asset);
			if (item != null)
			{
				item.Select();
				Repaint();
				return;
			}

			_selectAssetGuidAfterRefresh = asset.ToGuid();
			_forceRefresh = true;
			ForceMenuTreeRebuild();
		}

		private void TrySelectMenuItem(string path)
		{
			var item = path.IsNullOrEmpty() ? null : MenuTree?.GetMenuItem(path);
			item?.Select();
		}

		private void TrySelectMenuItemWithObject(ContentScriptableObject asset)
		{
			var item = asset == null || MenuTree == null ? null : FindMenuItemWithAsset(asset);
			item?.Select();
		}

		private void TrySelectPinnedMenuItem(string key)
		{
			var item = FindPinnedMenuItem(key);
			item?.Select();
		}

		// Добавляет лист-конфиг в дерево. Single Content Entry выделяется иконкой (залитый файл)
		private void AddConfigLeaf(OdinMenuTree tree, string path, ContentScriptableObject so, bool single)
		{
			var leaf = new ContentMenuItem(tree, so.name, so)
			{
				SdfIcon = single ? SdfIconType.FileEarmarkTextFill : SdfIconType.FileEarmarkText
			};

			tree.AddMenuItemAtPath(path, leaf);
			RegisterAssetMenuItem(so, leaf);
		}

		private void AddConfigToIdGroupPages(OdinMenuTree tree, CategoryPage categoryPage, string leafPath, ContentScriptableObject config)
		{
			var typePath = categoryPage?.MenuPath;
			if (tree == null ||
				typePath.IsNullOrEmpty() ||
				leafPath.IsNullOrEmpty() ||
				config == null ||
				leafPath == typePath ||
				!leafPath.StartsWith(typePath + "/", StringComparison.Ordinal))
			{
				return;
			}

			var groupPath = typePath;
			var groupIdPath = string.Empty;
			var relativePath = leafPath[(typePath.Length + 1)..];
			foreach (var segment in relativePath.Split('/'))
			{
				if (segment.IsNullOrEmpty())
					continue;

				groupPath += "/" + segment;
				groupIdPath = groupIdPath.IsNullOrEmpty() ? segment : groupIdPath + "/" + segment;
				var item = tree.GetMenuItem(groupPath);
				if (item == null)
					continue;

				item.SdfIcon = SdfIconType.Folder2Open;

				if (item.Value is IdGroupPage page)
				{
					page.AddItem(config);
					continue;
				}

				if (item.Value != null)
					continue;

				page = new IdGroupPage(this,
					categoryPage.Database,
					groupPath,
					groupIdPath,
					config.GetType(),
					categoryPage.CreateMenuItemName);
				page.AddItem(config);
				item.Value = page;
				AddCreateConfigLeaf(tree, groupPath, page);
			}
		}

		private static void BuildIdPathGroupCounts(List<ContentScriptableObject> configs, Dictionary<string, int> groupCounts)
		{
			if (configs.IsNullOrEmpty())
				return;

			for (int i = 0; i < configs.Count; i++)
			{
				if (!TryGetConfigId(configs[i], out var id))
					continue;

				var lastSeparator = id.LastIndexOf('/');
				if (lastSeparator <= 0)
					continue;

				for (var separator = id.IndexOf('/'); separator >= 0; separator = id.IndexOf('/', separator + 1))
				{
					if (separator <= 0)
						continue;

					AddIdPathGroupCount(groupCounts, id[..separator]);

					if (separator == lastSeparator)
						break;
				}
			}
		}

		private static void AddIdPathGroupCount(Dictionary<string, int> groupCounts, string groupPath)
		{
			if (groupPath.IsNullOrEmpty())
				return;

			groupCounts.TryGetValue(groupPath, out var count);
			groupCounts[groupPath] = count + 1;
		}

		private static string GetConfigLeafPath(string typePath, ContentScriptableObject config, Dictionary<string, int> groupCounts)
		{
			if (groupCounts == null || groupCounts.Count == 0 || !TryGetConfigId(config, out var id))
				return typePath;

			var lastSeparator = id.LastIndexOf('/');
			if (lastSeparator <= 0)
				return typePath;

			using (StringBuilderPool.Get(out var pathBuilder))
			{
				pathBuilder.EnsureCapacity(typePath.Length + id.Length);
				pathBuilder.Append(typePath);

				var segmentStart = 0;
				for (var separator = id.IndexOf('/'); separator >= 0; separator = id.IndexOf('/', separator + 1))
				{
					if (separator > segmentStart &&
						groupCounts.TryGetValue(id[..separator], out var count) &&
						count > 1)
					{
						pathBuilder.Append('/').Append(id, segmentStart, separator - segmentStart);
					}

					if (separator == lastSeparator)
						break;

					segmentStart = separator + 1;
				}

				return pathBuilder.ToString();
			}
		}

		private static bool TryGetConfigId(ContentScriptableObject config, out string id)
		{
			id = config is IUniqueContentEntrySource source ? source.Id : null;
			return !id.IsNullOrEmpty() && id.IndexOf('/') >= 0;
		}

		private static void AddCreateConfigLeaf(OdinMenuTree tree, string path, ConfigRowsPage page)
		{
			tree.Add($"{path}/{page.CreateMenuItemName}", new CreateConfigAction(page), SdfIconType.Plus);
		}

		private static void MoveCreateConfigItemsToBottom(OdinMenuItem item)
		{
			var children = item?.ChildMenuItems;
			if (children == null || children.Count == 0)
				return;

			for (int i = 0; i < children.Count; i++)
				MoveCreateConfigItemsToBottom(children[i]);

			using (ListPool<OdinMenuItem>.Get(out var createItems))
			{
				for (int i = children.Count - 1; i >= 0; i--)
				{
					if (children[i].Value is not CreateConfigAction)
						continue;

					createItems.Add(children[i]);
					children.RemoveAt(i);
				}

				for (int i = createItems.Count - 1; i >= 0; i--)
					children.Add(createItems[i]);
			}
		}

		private static void MoveDisabledConfigsToBottom(OdinMenuItem item)
		{
			var children = item?.ChildMenuItems;
			if (children == null || children.Count == 0)
				return;

			for (int i = 0; i < children.Count; i++)
				MoveDisabledConfigsToBottom(children[i]);

			using (ListPool<OdinMenuItem>.Get(out var disabledConfigs))
			{
				for (int i = children.Count - 1; i >= 0; i--)
				{
					if (!IsDisabledConfig(children[i].Value as ContentScriptableObject))
						continue;

					disabledConfigs.Add(children[i]);
					children.RemoveAt(i);
				}

				for (int i = disabledConfigs.Count - 1; i >= 0; i--)
					children.Add(disabledConfigs[i]);
			}
		}

		private static bool IsDisabledConfig(ContentScriptableObject config)
			=> config != null && config is not ContentDatabaseScriptableObject && !config.Enabled;

		private static string GetCreateConfigMenuItemName(string typeName)
		{
			return typeName.IsNullOrEmpty() ? NEW_CONFIG_MENU_ITEM_PREFIX : $"{NEW_CONFIG_MENU_ITEM_PREFIX} {typeName}";
		}

		// При поиске Odin строит плоский список в порядке дерева и НЕ сортирует его (кастомная SearchFunction).
		// Поднимаем базы и категории над конфигами. Сортируем и до, и после отрисовки — чтобы не было кадра в pre-order
		protected override void DrawMenu()
		{
			SortSearchResults();
			var scrollTarget = GetNavigationScrollTarget(Event.current);
			if (scrollTarget != NavigationScrollTarget.None)
				Event.current.Use();

			base.DrawMenu();

			if (scrollTarget != NavigationScrollTarget.None)
				ScrollNavigation(scrollTarget);

			if (SortSearchResults())
				Repaint();
		}

		private NavigationScrollTarget GetNavigationScrollTarget(Event currentEvent)
		{
			if (currentEvent == null ||
				currentEvent.type != EventType.MouseDown ||
				currentEvent.button != 0 ||
				currentEvent.clickCount < 2 ||
				MenuTree == null ||
				!MenuTree.Config.DrawScrollView)
			{
				return NavigationScrollTarget.None;
			}

			var rect = GetNavigationScrollbarHitRect();
			if (!rect.Contains(currentEvent.mousePosition))
				return NavigationScrollTarget.None;

			return currentEvent.mousePosition.y < rect.y + rect.height * 0.5f
				? NavigationScrollTarget.Top
				: NavigationScrollTarget.Bottom;
		}

		private Rect GetNavigationScrollbarHitRect()
		{
			var config = MenuTree.Config;
			var width = Mathf.Max(NAVIGATION_SCROLLBAR_HIT_WIDTH,
				GUI.skin.verticalScrollbar.fixedWidth + NAVIGATION_SCROLLBAR_RESIZE_GAP);
			var y = config.DrawSearchToolbar ? config.SearchToolbarHeight : 0f;
			var x = Mathf.Max(0f, MenuWidth - width);
			return new Rect(x, y, MenuWidth - x, Mathf.Max(0f, position.height - y));
		}

		private void SetGroupByIdPath(bool value)
		{
			GroupByIdPath = value;
			ForceMenuTreeRebuild();
		}

		private void SetSortByEnabled(bool value)
		{
			SortByEnabled = value;
			ForceMenuTreeRebuild();
		}

		private void ScrollNavigation(NavigationScrollTarget target)
		{
			var tree = MenuTree;
			if (tree == null)
				return;

			if (target == NavigationScrollTarget.Top)
			{
				var scrollPos = tree.Config.ScrollPos;
				scrollPos.y = 0f;
				tree.Config.ScrollPos = scrollPos;
			}
			else
			{
				tree.ScrollToMenuItem(GetNavigationScrollEdgeItem(tree, target));
			}

			Repaint();
		}

		private static OdinMenuItem GetNavigationScrollEdgeItem(OdinMenuTree tree, NavigationScrollTarget target)
		{
			if (tree == null)
				return null;

			if (tree.DrawInSearchMode)
				return GetFlatNavigationScrollEdgeItem(tree, target);

			var root = tree.RootMenuItem.ChildMenuItems;
			if (root == null || root.Count == 0)
				return null;

			if (target == NavigationScrollTarget.Top)
			{
				for (int i = 0; i < root.Count; i++)
					if (IsNavigationScrollItem(root[i]))
						return root[i];

				return null;
			}

			for (int i = root.Count - 1; i >= 0; i--)
			{
				var item = GetLastExpandedNavigationItem(root[i]);
				if (IsNavigationScrollItem(item))
					return item;
			}

			return null;
		}

		private static OdinMenuItem GetFlatNavigationScrollEdgeItem(OdinMenuTree tree, NavigationScrollTarget target)
		{
			var flat = tree.FlatMenuTree;
			if (flat == null || flat.Count == 0)
				return null;

			if (target == NavigationScrollTarget.Top)
			{
				for (int i = 0; i < flat.Count; i++)
					if (IsNavigationScrollItem(flat[i]))
						return flat[i];

				return null;
			}

			for (int i = flat.Count - 1; i >= 0; i--)
				if (IsNavigationScrollItem(flat[i]))
					return flat[i];

			return null;
		}

		private static OdinMenuItem GetLastExpandedNavigationItem(OdinMenuItem item)
		{
			if (item == null)
				return null;

			var children = item.ChildMenuItems;
			if (item.Toggled && children != null)
			{
				for (int i = children.Count - 1; i >= 0; i--)
				{
					var child = GetLastExpandedNavigationItem(children[i]);
					if (child != null)
						return child;
				}
			}

			return item;
		}

		private static bool IsNavigationScrollItem(OdinMenuItem item)
		{
			return item != null && item.Value is not SeparatorMenuAction;
		}

		// Стабильная корзинная сортировка результатов поиска: база(0) -> категория(1) -> конфиг(2)
		// Возвращает true, если порядок реально поменялся
		private bool SortSearchResults()
		{
			var tree = MenuTree;
			if (tree == null || tree.Config.SearchTerm.IsNullOrEmpty())
				return false;

			var flat = tree.FlatMenuTree;
			if (flat == null || flat.Count < 2)
				return false;

			// Уже отсортировано? (приоритет не убывает)
			var sorted = true;
			for (int i = 1; i < flat.Count; i++)
			{
				if (SearchPriority(flat[i - 1]) > SearchPriority(flat[i]))
				{
					sorted = false;
					break;
				}
			}

			if (sorted)
				return false;

			using (ListPool<OdinMenuItem>.Get(out var buffer))
			{
				for (int priority = 0; priority <= 2; priority++)
				{
					for (int i = 0; i < flat.Count; i++)
					{
						if (SearchPriority(flat[i]) == priority)
							buffer.Add(flat[i]);
					}
				}

				flat.Clear();
				flat.AddRange(buffer);
			}

			return true;
		}

		private static int SearchPriority(OdinMenuItem item)
		{
			return item.Value switch
			{
				ContentDatabaseScriptableObject => 0,
				ConfigRowsPage => 1,
				_ => 2
			};
		}

		protected override void OnBeginDrawEditors()
		{
			var selected = MenuTree?.Selection;
			var selectedItem = selected is {Count: > 0} ? selected[0] : null;
			var selectedValue = selected?.SelectedValue;
			if (selectedValue is CreateConfigAction createConfigAction)
			{
				createConfigAction.Execute(_lastNavigationHistoryItem);
				selected = MenuTree?.Selection;
				selectedItem = selected is {Count: > 0} ? selected[0] : null;
				selectedValue = selected?.SelectedValue;
			}

			TrackNavigationHistory(selectedItem);

			SirenixEditorGUI.BeginHorizontalToolbar();

			DrawNavigationHistoryButtons();

			var isConfig = selectedValue is ContentEntryScriptableObject;

			// Пин — для базы, категории или конфига (закреплённое показывается сверху отдельной группой)
			if (selectedValue is ContentScriptableObject pinnable)
			{
				if (SirenixEditorGUI.ToolbarButton(IsPinned(pinnable) ? SdfIconType.PinAngleFill : SdfIconType.PinAngle))
					TogglePin(pinnable, selectedItem);
			}
			else if (selectedValue is CategoryPage categoryPage)
			{
				if (SirenixEditorGUI.ToolbarButton(IsPinned(categoryPage) ? SdfIconType.PinAngleFill : SdfIconType.PinAngle))
					TogglePin(categoryPage, selectedItem);
			}

			if (TryGetOriginalMenuPath(selectedItem, out var originalMenuPath))
			{
				if (DrawToolbarButton(SdfIconType.BoxArrowInLeft, OPEN_ORIGINAL_PAGE_TOOLTIP))
					TrySelectMenuItem(originalMenuPath);
			}
			else if (TryGetPinnedKey(selectedValue, out var pinnedKey))
			{
				if (DrawToolbarButton(SdfIconType.BoxArrowInRight, OPEN_PINNED_PAGE_TOOLTIP))
					TrySelectPinnedMenuItem(pinnedKey);
			}

			GUILayout.Space(3);

			// Кликабельный путь "База / Категория / Имя" — клик по базе/категории открывает их страницы
			if (selectedItem != null)
				DrawBreadcrumb(selectedItem);

			GUILayout.FlexibleSpace();

			if (selectedValue is ContentEntryScriptableObject selectedAsset)
			{
				DrawGenerateConstantsButton(selectedAsset.GetType(), selectedAsset.ValueType);
				DrawDocumentationButton(selectedAsset.GetType(), selectedAsset.ValueType);
				DrawReferenceSearchButton(selectedAsset);
				DrawDeleteButton(selectedAsset, selectedItem);
			}
			else if (selectedValue is CategoryPage selectedCategoryPage)
			{
				DrawGenerateConstantsButton(selectedCategoryPage.ConfigType, selectedCategoryPage.ValueType);
				DrawDocumentationButton(selectedCategoryPage.ConfigType, selectedCategoryPage.ValueType);
			}

			DrawSettingsDropdown();

			GUILayout.Space(1);

			SirenixEditorGUI.EndHorizontalToolbar();

			SirenixEditorGUI.BeginIndentedHorizontal();

			_hierarchyMode = EditorGUIUtility.hierarchyMode;
			if (isConfig)
			{
				EditorGUIUtility.hierarchyMode = true;
				GUILayout.Space(16);
			}
		}

		private bool _hierarchyMode;

		protected override void OnEndDrawEditors()
		{
			EditorGUIUtility.hierarchyMode = _hierarchyMode;
			SirenixEditorGUI.EndIndentedHorizontal();
			// Нижний отступ, чтобы инспектор не упирался в границу окна
			GUILayout.Space(INSPECTOR_BOTTOM_PADDING);
		}

		private void DrawSettingsDropdown()
		{
			if (!DrawToolbarButton(SdfIconType.GearFill, SETTINGS_TOOLTIP))
				return;

			var menu = new GenericMenu();
			menu.AddItem(new GUIContent("Sync Project Selection"), AutoSyncProjectSelection,
				() => AutoSyncProjectSelection = !AutoSyncProjectSelection);
			menu.AddItem(new GUIContent("Group by Id"), GroupByIdPath,
				() => SetGroupByIdPath(!GroupByIdPath));
			menu.AddItem(new GUIContent("Sort By Enabled"), SortByEnabled,
				() => SetSortByEnabled(!SortByEnabled));
			menu.AddItem(new GUIContent("Force Rebuild Browser"), false, ForceRebuild);
			menu.ShowAsContext();
		}

		private void DrawReferenceSearchButton(ContentScriptableObject asset)
		{
			if (asset == null)
				return;

			var tooltip = new GUIContent(string.Empty, ContentSearchProvider.GetFindReferencesTooltip(asset));
			if (DrawToolbarButton(SdfIconType.FileEarmarkBreakFill, tooltip))
				ContentSearchProvider.OpenReferenceSearch(asset);
		}

		private static void DrawDocumentationButton(Type configType, Type valueType)
		{
			if (!TryGetDocumentationUrl(configType, valueType, out var url))
				return;

			if (DrawToolbarButton(SdfIconType.JournalBookmarkFill, GetDocumentationTooltip(valueType)))
				Help.BrowseURL(url);
		}

		private static bool TryGetDocumentationUrl(Type configType, Type valueType, out string url)
		{
			url = string.Empty;

			if (configType != null && configType.HasAttribute<DocumentationAttribute>())
			{
				url = configType.GetAttribute<DocumentationAttribute>().URL;
				return !url.IsNullOrEmpty();
			}

			if (configType != null && configType.HasAttribute<HelpURLAttribute>())
			{
				url = configType.GetAttribute<HelpURLAttribute>().URL;
				return !url.IsNullOrEmpty();
			}

			if (valueType == null || !valueType.HasAttribute<DocumentationAttribute>())
				return false;

			url = valueType.GetAttribute<DocumentationAttribute>().URL;
			return !url.IsNullOrEmpty();
		}

		private void DrawGenerateConstantsButton(Type configType, Type valueType)
		{
			if (!HasContentGeneration(configType, valueType))
				return;

			EditorGUI.BeginDisabledGroup(_creating);

			if (DrawToolbarButton(SdfIconType.Magic, GetGenerateConstantsTooltip(valueType)))
				GenerateConstants(valueType);

			EditorGUI.EndDisabledGroup();
		}

		private static GUIContent GetDocumentationTooltip(Type type) =>
			new(string.Empty, DOCUMENTATION_TOOLTIP_FORMAT.Format(GetTooltipTypeName(type)));

		private static GUIContent GetGenerateConstantsTooltip(Type type) =>
			new(string.Empty, GENERATE_CONSTANTS_TOOLTIP_FORMAT.Format(GetTooltipTypeName(type)));

		private static string GetTooltipTypeName(Type type) => type != null ? type.GetNiceName() : "Unknown";

		private static bool HasContentGeneration(Type configType, Type valueType)
		{
			return valueType != null && valueType.HasAttribute<ConstantsAttribute>() ||
				configType != null && configType.HasAttribute<ConstantsAttribute>();
		}

		private static void GenerateConstants(Type type)
		{
			if (type == null)
				return;

			ContentConstantGenerator.Generate(type, ContentDatabaseEditorUtility.GetScriptableObjectsByType(type), fullLog: true);
		}

		private void DrawDeleteButton(ContentEntryScriptableObject asset, OdinMenuItem selectedItem)
		{
			EditorGUI.BeginDisabledGroup(_creating || asset == null);

			if (DrawToolbarButton(SdfIconType.TrashFill, DELETE_ASSET_TOOLTIP))
				TryDeleteAsset(asset, selectedItem);

			EditorGUI.EndDisabledGroup();
		}

		private static bool DrawToolbarButton(SdfIconType icon, GUIContent tooltip)
		{
			var clicked = SirenixEditorGUI.ToolbarButton(icon);
			GUI.Label(GUILayoutUtility.GetLastRect(), tooltip);
			return clicked;
		}

		private static bool DrawToolbarButton(string label, GUIContent tooltip)
		{
			var clicked = SirenixEditorGUI.ToolbarButton(label);
			GUI.Label(GUILayoutUtility.GetLastRect(), tooltip);
			return clicked;
		}

		private static bool DrawColoredToolbarButton(SdfIconType icon, Color color)
		{
			var style = SirenixGUIStyles.IconButton;
			var normalColor = style.normal.textColor;
			var hoverColor = style.hover.textColor;
			var activeColor = style.active.textColor;
			var focusedColor = style.focused.textColor;

			style.normal.textColor = color;
			style.hover.textColor = color;
			style.active.textColor = color;
			style.focused.textColor = color;

			try
			{
				return SirenixEditorGUI.ToolbarButton(icon);
			}
			finally
			{
				style.normal.textColor = normalColor;
				style.hover.textColor = hoverColor;
				style.active.textColor = activeColor;
				style.focused.textColor = focusedColor;
			}
		}

		private static bool DrawApplyDeleteButton(bool enabled, GUIContent tooltip)
		{
			EditorGUI.BeginDisabledGroup(!enabled);
			var clicked = enabled
				? DrawColoredToolbarButton(SdfIconType.Check, APPLY_DELETE_ENABLED_COLOR)
				: SirenixEditorGUI.ToolbarButton(SdfIconType.Check);
			GUI.Label(GUILayoutUtility.GetLastRect(), tooltip);
			EditorGUI.EndDisabledGroup();
			return clicked;
		}

		private static bool DrawCancelDeleteButton(GUIContent tooltip)
		{
			var clicked = DrawColoredToolbarButton(SdfIconType.X, CANCEL_DELETE_COLOR);
			GUI.Label(GUILayoutUtility.GetLastRect(), tooltip);
			return clicked;
		}

		private void TryDeleteAsset(ContentEntryScriptableObject asset, OdinMenuItem selectedItem)
		{
			TryDeleteAsset(asset, GetParentMenuPath(selectedItem));
		}

		private bool TryDeleteAsset(ContentEntryScriptableObject asset, string parentPath)
		{
			if (asset == null)
				return false;

			using (ListPool<ContentEntryScriptableObject>.Get(out var assets))
			{
				assets.Add(asset);
				return TryDeleteAssets(assets, parentPath, asset.GetType());
			}
		}

		private bool TryDeleteAssets(List<ContentEntryScriptableObject> assets, string parentPath, Type assetType)
		{
			if (assets.IsNullOrEmpty())
				return false;

			var typeName = GetConfigDisplayName(assetType);
			if (!EditorUtility.DisplayDialog(
				$"Delete {typeName}",
				GetDeleteMessage(assets, typeName),
				"Delete",
				"Cancel"))
				return false;

			var anyDeleted = false;
			var pinnedChanged = false;
			for (int i = 0; i < assets.Count; i++)
			{
				var asset = assets[i];
				if (asset == null)
					continue;

				var assetPath = AssetDatabase.GetAssetPath(asset);
				if (assetPath.IsNullOrEmpty())
					continue;

				var guid = asset.ToGuid();
				if (!AssetDatabase.DeleteAsset(assetPath))
					continue;

				anyDeleted = true;
				if (!guid.IsNullOrEmpty() && Pinned.Remove(guid))
				{
					pinnedChanged = true;
					_lastPinnedAtByKey.Remove(guid);
				}
			}

			if (!anyDeleted)
				return false;

			if (pinnedChanged)
				EditorPrefs.SetString(PINNED_PREF_KEY, string.Join("|", Pinned));

			Selection.activeObject = null;
			AssetDatabase.SaveAssets();

			_selectOriginalPathAfterRefresh = parentPath;
			_forceRefresh = true;
			ForceMenuTreeRebuild();
			return true;
		}

		private static string GetDeleteMessage(List<ContentEntryScriptableObject> assets, string typeName)
		{
			return assets.Count == 1
				? $"Are you sure you want to delete \"{assets[0].name}\"?"
				: $"Are you sure you want to delete the selected {typeName}s ({assets.Count})?";
		}

		private static string GetParentMenuPath(OdinMenuItem item)
		{
			var path = GetOriginalMenuPath(item);
			if (path.IsNullOrEmpty())
				path = item?.GetFullPath();

			if (path.IsNullOrEmpty())
				return null;

			var separatorIndex = path.LastIndexOf('/');
			return separatorIndex > 0 ? path[..separatorIndex] : null;
		}

		private void ForceRebuild()
		{
			_forceRefresh = true;
			_selectFirstAfterRefresh = true;
			ForceMenuTreeRebuild();
		}

		private void PromptCreateContentEntry(ContentDatabaseScriptableObject database,
			Type configType,
			ContentScriptableObject groupAsset,
			string idGroupPath = null)
		{
			if (_creating || !CanCreateContentEntry(configType))
				return;

			var folder = GetCreateFolder(database, groupAsset);
			if (folder.IsNullOrEmpty())
				return;

			var defaultAssetName = GetDefaultAssetName(configType);
			CreateConfigNameWindow.Open(GetCreateConfigMenuItemName(GetConfigDisplayName(configType)), defaultAssetName, folder,
				(assetName, assetFolder) => CreateContentEntry(configType, assetFolder, assetName, idGroupPath));
		}

		private void CreateContentEntry(Type configType, string folder, string assetName, string idGroupPath)
		{
			if (_creating || !CanCreateContentEntry(configType) || folder.IsNullOrEmpty())
				return;

			assetName = SanitizeAssetName(assetName);
			if (assetName.IsNullOrEmpty())
				return;

			_creating = true;
			try
			{
				AssetDatabaseUtility.EnsureOrCreateFolder(folder);

				var assetPath = GetUniqueAssetPath(folder, assetName);
				var asset = ScriptableObject.CreateInstance(configType);
				if (asset == null)
					return;

				AssetDatabase.CreateAsset(asset, assetPath);
				AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
				AssetDatabase.SaveAssets();

				var created = AssetDatabase.LoadAssetAtPath<ContentEntryScriptableObject>(assetPath);
				if (created != null)
				{
					if (created.NeedSync())
					{
						created.Sync(true);
						AssetDatabase.SaveAssetIfDirty(created);
					}

					if (!idGroupPath.IsNullOrEmpty() && created is IUniqueContentEntrySource source)
					{
						created.SetId(CombineContentIdPath(idGroupPath, source.Id));
						EditorUtility.SetDirty(created);
						AssetDatabase.SaveAssetIfDirty(created);
						ContentEditorCache.RefreshByValueType(created.ValueType);
						ContentAutoConstantsGenerator.ForceInvokeWithDelay(created.GetType());
					}

					_selectAssetGuidAfterRefresh = created.ToGuid();
					Selection.activeObject = created;
					EditorGUIUtility.PingObject(created);
				}

				_forceRefresh = true;
				ForceMenuTreeRebuild();
			}
			finally
			{
				_creating = false;
			}
		}

		private static string CombineContentIdPath(string groupPath, string id)
		{
			groupPath = groupPath?.Trim('/');
			id = id?.Trim('/');
			return groupPath.IsNullOrEmpty() ? id : id.IsNullOrEmpty() ? groupPath : $"{groupPath}/{id}";
		}

		private static string GetCreateFolder(ContentDatabaseScriptableObject database, ContentScriptableObject groupAsset)
		{
			var folder = GetAssetFolder(groupAsset);
			if (!folder.IsNullOrEmpty())
				return folder;

			folder = GetAssetFolder(database);
			return folder.IsNullOrEmpty() ? "Assets" : folder;
		}

		private static string GetAssetFolder(UnityEngine.Object asset)
		{
			if (asset == null)
				return null;

			var path = AssetDatabase.GetAssetPath(asset);
			if (path.IsNullOrEmpty())
				return null;

			if (AssetDatabase.IsValidFolder(path))
				return NormalizeAssetPath(path);

			return NormalizeAssetPath(Path.GetDirectoryName(path));
		}

		private static string GetDefaultAssetName(Type type)
		{
			var assetName = GetConfigTypeName(type);
			if (assetName.IsNullOrEmpty() && type != null)
				assetName = type.Name;
			if (assetName.IsNullOrEmpty())
				assetName = "Asset";

			return $"{assetName}_New";
		}

		private static string SanitizeAssetName(string assetName)
		{
			if (assetName.IsNullOrEmpty())
				return null;

			assetName = assetName.Trim();
			if (assetName.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
				assetName = assetName[..^".asset".Length];

			if (assetName.IsNullOrEmpty())
				return null;

			var invalidChars = Path.GetInvalidFileNameChars();
			using (StringBuilderPool.Get(out var builder))
			{
				builder.EnsureCapacity(assetName.Length);
				for (int i = 0; i < assetName.Length; i++)
				{
					var character = assetName[i];
					builder.Append(IsInvalidAssetNameCharacter(character, invalidChars) ? '_' : character);
				}

				var sanitized = builder.ToString().Trim();
				return sanitized.IsNullOrEmpty() ? null : sanitized;
			}
		}

		private static bool IsInvalidAssetNameCharacter(char character, char[] invalidChars)
		{
			return Array.IndexOf(invalidChars, character) >= 0 ||
				character is '/' or '\\' or ':' or '*' or '?' or '"' or '<' or '>' or '|';
		}

		private static string NormalizeCreateFolderPath(string folder)
		{
			if (folder.IsNullOrEmpty())
				return null;

			folder = NormalizeAssetPath(folder.Trim())?.TrimEnd('/');
			if (folder == "Assets" || folder.StartsWith("Assets/", StringComparison.Ordinal))
				return folder;

			var dataPath = NormalizeAssetPath(Application.dataPath);
			if (string.Equals(folder, dataPath, StringComparison.Ordinal))
				return "Assets";

			return folder.StartsWith(dataPath + "/", StringComparison.Ordinal)
				? "Assets" + folder[dataPath.Length..]
				: null;
		}

		private static string GetUniqueAssetPath(string folder, string assetName)
		{
			var assetPath = $"{folder}/{assetName}.asset";
			if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) == null)
				return assetPath;

			for (int index = 2;; index++)
			{
				assetPath = $"{folder}/{assetName}_{index}.asset";
				if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) == null)
					return assetPath;
			}
		}

		private static string NormalizeAssetPath(string path)
		{
			return path?.Replace('\\', '/');
		}

		private void DrawNavigationHistoryButtons()
		{
			EditorGUI.BeginDisabledGroup(!CanNavigateHistory(-1));
			if (DrawToolbarButton(SdfIconType.ArrowLeft, BACK_TOOLTIP))
				NavigateHistory(-1);
			EditorGUI.EndDisabledGroup();

			EditorGUI.BeginDisabledGroup(!CanNavigateHistory(1));
			if (DrawToolbarButton(SdfIconType.ArrowRight, FORWARD_TOOLTIP))
				NavigateHistory(1);
			EditorGUI.EndDisabledGroup();
		}

		private void TrackNavigationHistory(OdinMenuItem item)
		{
			if (!_skipNextNavigationHistoryRecord && ReferenceEquals(item, _lastNavigationHistoryItem))
				return;

			var path = item?.GetFullPath();
			if (path.IsNullOrEmpty())
				return;

			_lastNavigationHistoryItem = item;

			if (_skipNextNavigationHistoryRecord)
			{
				_skipNextNavigationHistoryRecord = false;
				_lastNavigationHistoryPath = path;
				return;
			}

			if (path == _lastNavigationHistoryPath)
				return;

			if (_navigationHistoryIndex >= 0 &&
				_navigationHistoryIndex < _navigationHistory.Count &&
				_navigationHistory[_navigationHistoryIndex] == path)
			{
				_lastNavigationHistoryPath = path;
				return;
			}

			if (_navigationHistoryIndex < _navigationHistory.Count - 1)
				_navigationHistory.RemoveRange(_navigationHistoryIndex + 1, _navigationHistory.Count - _navigationHistoryIndex - 1);

			_navigationHistory.Add(path);
			if (_navigationHistory.Count > MAX_NAVIGATION_HISTORY_COUNT)
				_navigationHistory.RemoveAt(0);

			_navigationHistoryIndex = _navigationHistory.Count - 1;
			_lastNavigationHistoryPath = path;
		}

		private bool CanNavigateHistory(int direction)
		{
			var index = _navigationHistoryIndex + direction;
			return index >= 0 && index < _navigationHistory.Count;
		}

		private void NavigateHistory(int direction)
		{
			var index = _navigationHistoryIndex + direction;
			if (index < 0 || index >= _navigationHistory.Count)
				return;

			var path = _navigationHistory[index];
			var item = path.IsNullOrEmpty() ? null : MenuTree?.GetMenuItem(path);
			if (item == null)
				return;

			_navigationHistoryIndex = index;
			_lastNavigationHistoryPath = path;
			_skipNextNavigationHistoryRecord = true;
			item.Select();
			Repaint();
		}

		private static GUIStyle _linkStyle;

		// Стиль текста-ссылки (подчёркнутый, цветной — через rich text)
		private static GUIStyle LinkStyle
		{
			get
			{
				_linkStyle ??= new GUIStyle(EditorStyles.label)
				{
					richText = true,
					alignment = TextAnchor.MiddleLeft,
					padding = new RectOffset(0, 0, 0, 0)
				};

				return _linkStyle;
			}
		}

		// Рисует кликабельную цепочку предков "База / Категория / Имя" (rich-text строки кэшируются по листу)
		private void DrawBreadcrumb(OdinMenuItem leaf)
		{
			RebuildBreadcrumbCache(leaf);

			for (int i = 0; i < _breadcrumbNodes.Count; i++)
			{
				var node = _breadcrumbNodes[i];

				// Последний элемент — текущий выбранный, не ссылка
				if (i == _breadcrumbNodes.Count - 1)
				{
					GUILayout.Label(_breadcrumbContents[i], LinkStyle, GUILayout.ExpandWidth(false));
					continue;
				}

				if (GUILayout.Button(_breadcrumbContents[i], LinkStyle, GUILayout.ExpandWidth(false)))
					node.Select();

				EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);

				GUILayout.Label(BREADCRUMB_SEPARATOR, LinkStyle, GUILayout.ExpandWidth(false));
			}
		}

		// Пересобирает строки цепочки только при смене листа — иначе rich-text интерполяция на каждый узел каждый кадр
		private void RebuildBreadcrumbCache(OdinMenuItem leaf)
		{
			if (ReferenceEquals(_breadcrumbLeaf, leaf))
				return;

			_breadcrumbLeaf = leaf;
			_breadcrumbNodes.Clear();
			_breadcrumbContents.Clear();

			using (ListPool<OdinMenuItem>.Get(out var chain))
			{
				// Собираем путь от листа к корню
				for (var node = leaf; node != null; node = node.Parent)
				{
					if (!node.Name.IsNullOrEmpty())
						chain.Add(node);
				}

				// В порядке отрисовки: База -> Категория -> Имя (последний — текущий, без ссылки)
				for (int i = chain.Count - 1; i >= 0; i--)
				{
					var node = chain[i];
					var link = i != 0;
					_breadcrumbNodes.Add(node);
					_breadcrumbContents.Add(new GUIContent(link
						? $"<color=#{BREADCRUMB_LINK_COLOR}><u>{node.Name}</u></color>"
						: $"<color=#{BREADCRUMB_LINK_COLOR}>{node.Name}</color>"));
				}
			}
		}

		// Убирает технические суффиксы и приводит имя к читаемому виду
		private static string Nicify(string raw)
		{
			if (raw.IsNullOrEmpty())
				return raw;

			var stripped = TrimTypeSuffix(raw, SCRIPTABLE_OBJECT_SUFFIX);
			stripped = TrimTypeSuffix(stripped, DATABASE_SUFFIX);

			if (stripped.IsNullOrEmpty())
				stripped = raw;

			return ObjectNames.NicifyVariableName(stripped);
		}

		private static string GetConfigTypeName(Type type)
		{
			if (type == null)
				return null;

			var raw = type.Name;
			var stripped = TrimTypeSuffix(raw, SCRIPTABLE_OBJECT_SUFFIX);
			stripped = TrimTypeSuffix(stripped, CONFIG_SUFFIX);

			return stripped.IsNullOrEmpty() ? raw : stripped;
		}

		private static string GetConfigDisplayName(Type type)
		{
			var name = GetConfigTypeName(type);
			return name.IsNullOrEmpty() ? "Asset" : ObjectNames.NicifyVariableName(name);
		}

		private static string TrimTypeSuffix(string value, string suffix)
		{
			return !value.IsNullOrEmpty() && value.EndsWith(suffix, StringComparison.Ordinal)
				? value[..^suffix.Length]
				: value;
		}

		// Точный поиск (подстрока, без учёта регистра) по заранее собранной строке
		private static bool SearchItem(OdinMenuItem item)
		{
			// Закреплённые копии, страница "Pinned" и пункты "New ..." в поиске не участвуют (мусорят выдачу)
			if (item is PinnedMenuItem || item is PinnedCategoryMenuItem || item.Value is PinnedPage or SeparatorMenuAction or CreateConfigAction)
				return false;

			var term = item.MenuTree?.Config.SearchTerm;
			if (term.IsNullOrEmpty())
				return true;

			return Match(item.SearchString, term);
		}

		private static bool Match(string haystack, string term)
		{
			return !haystack.IsNullOrEmpty() && haystack.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
		}

		private static bool TryGetOriginalMenuPath(OdinMenuItem item, out string path)
		{
			path = item switch
			{
				PinnedMenuItem pinned => pinned.OriginalMenuPath,
				PinnedCategoryMenuItem pinned => pinned.OriginalMenuPath,
				_ => null
			};

			return !path.IsNullOrEmpty();
		}

		private bool TryGetPinnedKey(object value, out string key)
		{
			key = value switch
			{
				ContentScriptableObject so when IsPinned(so) => so.ToGuid(),
				CategoryPage page when IsPinned(page) => page.PinKey,
				_ => null
			};

			return !key.IsNullOrEmpty();
		}

		private static string GetOriginalMenuPath(OdinMenuItem item)
		{
			if (TryGetOriginalMenuPath(item, out var path))
				return path;

			return item?.GetFullPath();
		}

		private static void DrawDatabaseSuffix(OdinMenuItem item)
		{
			DrawNavigationSuffix(item, DATABASE_SUFFIX, addWhenMissing: true, separatedBySpace: true);
		}

		private static void DrawCategorySuffix(OdinMenuItem item)
		{
			DrawNavigationSuffix(item, CATEGORY_SUFFIX, addWhenMissing: true, separatedBySpace: false);
		}

		private static void DrawNavigationSuffix(OdinMenuItem item, string suffix, bool addWhenMissing, bool separatedBySpace)
		{
			if (item == null || suffix.IsNullOrEmpty() || Event.current.type != EventType.Repaint)
				return;

			var menuStyle = item.Style;
			if (menuStyle == null)
				return;

			var style = item.IsSelected ? menuStyle.SelectedLabelStyle : menuStyle.DefaultLabelStyle;
			var name = item.SmartName;
			var labelRect = item.LabelRect;
			if (style == null || name.IsNullOrEmpty() || labelRect.width <= 0f)
				return;

			var hasSuffix = name.EndsWith(suffix, StringComparison.Ordinal);
			if (!hasSuffix && !addWhenMissing)
				return;

			var prefix = hasSuffix ? name[..^suffix.Length].TrimEnd() : name;
			var prefixWidth = MeasureNavigationText(style, prefix);
			var suffixX = labelRect.x + prefixWidth;
			var suffixContent = suffix;

			if (hasSuffix && separatedBySpace)
				suffixX += MeasureNavigationText(style, " ");
			else if (!hasSuffix && separatedBySpace)
				suffixContent = $" {suffix}";

			if (suffixX >= labelRect.xMax)
				return;

			var suffixRect = new Rect(suffixX, labelRect.y, labelRect.xMax - suffixX, labelRect.height);
			var suffixStyle = GetNavigationSuffixStyle(style);
			GUI.Label(suffixRect, suffixContent, suffixStyle);
		}

		private static float MeasureNavigationText(GUIStyle style, string text)
		{
			NAVIGATION_SUFFIX_MEASURE_CONTENT.text = text;
			return style.CalcSize(NAVIGATION_SUFFIX_MEASURE_CONTENT).x;
		}

		private static GUIStyle GetNavigationSuffixStyle(GUIStyle source)
		{
			if (_navigationSuffixStyles.TryGetValue(source, out var style))
				return style;

			style = new GUIStyle(source)
			{
				fontSize = source.fontSize > 0 ? Mathf.Max(1, source.fontSize - 1) : 11
			};

			var color = GetNavigationSuffixColor();
			style.normal.textColor = color;
			style.hover.textColor = color;
			style.active.textColor = color;
			style.focused.textColor = color;
			style.onNormal.textColor = color;
			style.onHover.textColor = color;
			style.onActive.textColor = color;
			style.onFocused.textColor = color;
			_navigationSuffixStyles.Add(source, style);
			return style;
		}

		private static Color GetNavigationSuffixColor()
		{
			return EditorGUIUtility.isProSkin
				? new Color(0.55f, 0.55f, 0.55f, 0.5f)
				: new Color(0.45f, 0.45f, 0.45f, 0.5f);
		}

		// Строка поиска конфига: имя + Id + Guid + Guid'ы вложенных Entry
		private static string BuildSearchString(string name, IUniqueContentEntrySource source)
		{
			using (StringBuilderPool.Get(out var builder))
			{
				builder.Append(name);

				if (!source.Id.IsNullOrEmpty())
					builder.Append(' ').Append(source.Id);

				builder.Append(' ').Append(source.Guid.ToString());

				var entry = source.UniqueContentEntry;
				if (entry?.Nested != null)
				{
					foreach (var nestedGuid in entry.Nested.Keys)
						builder.Append(' ').Append(nestedGuid.ToString());
				}

				return builder.ToString();
			}
		}

		/// <summary>
		/// Пункт дерева для конфига — выключенный контент (Enabled == false) рисуется приглушённым
		/// </summary>
		private class ContentMenuItem : OdinMenuItem
		{
			private readonly ContentScriptableObject _config;

			public ContentMenuItem(OdinMenuTree tree, string name, ContentScriptableObject config)
				: base(tree, name, config)
			{
				_config = config;
			}

			public override void DrawMenuItem(int indentLevel)
			{
				if (_config == null || _config.Enabled)
				{
					base.DrawMenuItem(indentLevel);
					return;
				}

				var prev = GUI.color;
				GUI.color = new Color(prev.r, prev.g, prev.b, prev.a * 0.5f);
				base.DrawMenuItem(indentLevel);
				GUI.color = prev;
			}

			protected override void OnDrawMenuItem(Rect rect, Rect triangleRect)
			{
				base.OnDrawMenuItem(rect, triangleRect);

				if (_config is ContentDatabaseScriptableObject)
					DrawDatabaseSuffix(this);
			}
		}

		/// <summary>
		/// Закреплённая копия (в группе "Pinned") — помечена отдельным типом, чтобы пропускать её в поиске
		/// </summary>
		private class PinnedMenuItem : ContentMenuItem
		{
			public string OriginalMenuPath { get; }
			public string PinKey { get; }

			public PinnedMenuItem(OdinMenuTree tree, string name, ContentScriptableObject config, string originalMenuPath, string pinKey)
				: base(tree, name, config)
			{
				OriginalMenuPath = originalMenuPath;
				PinKey = pinKey;
			}
		}

		private class CategoryMenuItem : OdinMenuItem
		{
			public CategoryMenuItem(OdinMenuTree tree, string name, CategoryPage page)
				: base(tree, name, page)
			{
			}

			protected override void OnDrawMenuItem(Rect rect, Rect triangleRect)
			{
				base.OnDrawMenuItem(rect, triangleRect);
				DrawCategorySuffix(this);
			}
		}

		/// <summary>
		/// Закреплённая копия категории — помечена отдельным типом, чтобы пропускать её в поиске
		/// </summary>
		private class PinnedCategoryMenuItem : CategoryMenuItem
		{
			public string OriginalMenuPath { get; }
			public string PinKey { get; }

			public PinnedCategoryMenuItem(OdinMenuTree tree, string name, CategoryPage page, string originalMenuPath, string pinKey)
				: base(tree, name, page)
			{
				OriginalMenuPath = originalMenuPath;
				PinKey = pinKey;
			}
		}

		private class SeparatorMenuAction
		{
		}

		private class CreateConfigAction
		{
			private readonly ConfigRowsPage _page;
			private bool _executing;

			public CreateConfigAction(ConfigRowsPage page)
			{
				_page = page;
			}

			public void Execute(OdinMenuItem returnItem)
			{
				if (_executing)
					return;

				_executing = true;
				if (returnItem != null && returnItem.Value is not CreateConfigAction)
					returnItem.Select();
				else
					_page?.Select();

				EditorApplication.delayCall += Create;
			}

			private void Create()
			{
				_page?.CreateNew();
				_executing = false;
			}
		}

		private class CreateConfigNameWindow : OdinEditorWindow
		{
			private const float WIDTH = 430f;
			private const float HEIGHT = 58f;

			[ShowInInspector]
			[InlineProperty]
			[HideLabel]
			[PropertyOrder(0)]
			private AssetFullPath _assetPath;

			[OnInspectorGUI]
			private void OnInspectorGUI()
			{
				GUILayout.Space(2);
			}

			private Action<string, string> _onSubmit;

			public static void Open(string title, string defaultAssetName, string defaultFolder, Action<string, string> onSubmit)
			{
				var window = CreateInstance<CreateConfigNameWindow>();
				window.titleContent = new GUIContent(title);
				window._assetPath = new AssetFullPath
				{
					path = defaultFolder,
					name = defaultAssetName
				};
				window._onSubmit = onSubmit;
				window.minSize = new Vector2(WIDTH, HEIGHT);
				window.maxSize = new Vector2(WIDTH, HEIGHT);
				window.position = GUIHelper.GetEditorWindowRect().AlignCenter(WIDTH, HEIGHT);
				window.ShowUtility();
				window.Focus();
			}

			[ButtonGroup("Actions")]
			[Button("Cancel")]
			[PropertyOrder(10)]
			private void Cancel()
			{
				Close();
			}

			[ButtonGroup("Actions")]
			[Button("Create")]
			[EnableIf(nameof(CanSubmit))]
			[PropertyOrder(10)]
			private void Submit()
			{
				var assetName = SanitizeAssetName(_assetPath.name);
				var folder = NormalizeCreateFolderPath(_assetPath.path);
				if (assetName.IsNullOrEmpty() || folder.IsNullOrEmpty())
					return;

				var onSubmit = _onSubmit;
				Close();
				EditorApplication.delayCall += () => onSubmit?.Invoke(assetName, folder);
			}

			private bool CanSubmit()
			{
				var folder = NormalizeCreateFolderPath(_assetPath.path);
				return !SanitizeAssetName(_assetPath.name).IsNullOrEmpty() &&
					!folder.IsNullOrEmpty() &&
					AssetDatabase.IsValidFolder(folder);
			}

			[InlineProperty]
			[Serializable]
			private struct AssetFullPath
			{
				private const string EXTENSION = ".asset";

				[HorizontalGroup("box/2")]
				[DarkCardBox("box")]
				[HideLabel, FolderPath]
				public string path;

				[HorizontalGroup("box/2", width: 0.35f)]
				[HideLabel, SuffixLabel(EXTENSION)]
				public string name;

				public override string ToString() => Path.Combine(path, name + EXTENSION);
			}
		}

		/// <summary>
		/// Визуальный разделитель между закреплёнными пунктами и основной навигацией
		/// </summary>
		private class SeparatorMenuItem : OdinMenuItem
		{
			public SeparatorMenuItem(OdinMenuTree tree)
				: base(tree, string.Empty, new SeparatorMenuAction())
			{
			}

			public override void DrawMenuItem(int indentLevel)
			{
				var rect = GUILayoutUtility.GetRect(1f, 8, GUILayout.ExpandWidth(true));

				var isDark = EditorGUIUtility.isProSkin;
				var darkColor = isDark
					? new Color(49 / 255f, 49 / 255f, 49 / 255f)
					: new Color(200 / 255f, 200 / 255f, 200 / 255f);

				var lightColor = isDark
					? new Color(73 / 255f, 73 / 255f, 73 / 255f)
					: new Color(230 / 255f, 230 / 255f, 230 / 255f);

				EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1),
					darkColor);
				EditorGUI.DrawRect(rect, new Color(0f, 0f, 0f, 0.2f));
				var bottom = new Rect(rect.x, rect.yMax - 0.5f, rect.width, 1f);
				EditorGUI.DrawRect(bottom, darkColor);
				bottom.y += 0.5f;
				EditorGUI.DrawRect(bottom, lightColor);

				rect.y -= 2.5f;
				GUI.Label(rect, "...", EditorStyles.centeredGreyMiniLabel);
			}
		}

		private class PinnedEntry
		{
			public string Key { get; }
			public string Name { get; }
			public string MenuPath { get; }
			public SdfIconType Icon { get; }
			public ContentScriptableObject Asset { get; }
			public CategoryPage Category { get; }

			public PinnedEntry(string key, string name, string menuPath, SdfIconType icon, ContentScriptableObject asset, CategoryPage category)
			{
				Key = key;
				Name = name;
				MenuPath = menuPath;
				Icon = icon;
				Asset = asset;
				Category = category;
			}
		}

		/// <summary>
		/// Страница закреплённых элементов — быстрые переходы к базам, категориям и конфигам
		/// </summary>
		private class PinnedPage
		{
			private readonly ContentBrowserWindow _window;
			private HashSet<string> _deleteSelection;

			public bool DeleteMode { get; private set; }
			private bool HasDeleteSelection => _deleteSelection is {Count: > 0};

			public bool IsSelectedForDelete(string key)
			{
				return !key.IsNullOrEmpty() && _deleteSelection != null && _deleteSelection.Contains(key);
			}

			public void SetSelectedForDelete(string key, bool selected)
			{
				if (key.IsNullOrEmpty())
					return;

				if (selected)
					(_deleteSelection ??= new HashSet<string>()).Add(key);
				else
					_deleteSelection?.Remove(key);

				_window.Repaint();
			}

			private void DrawTitleBarButtons()
			{
				if (DeleteMode)
				{
					DrawDeleteModeButtons();
					return;
				}

				EditorGUI.BeginDisabledGroup(_window._creating || Items.IsNullOrEmpty());
				if (DrawToolbarButton(SdfIconType.TrashFill, PINNED_DELETE_MODE_TOOLTIP))
					BeginDeleteMode();

				if (DrawToolbarButton("Clear All", CLEAR_PINNED_TOOLTIP))
					_window.TryClearPinned();
				EditorGUI.EndDisabledGroup();
			}

			private void DrawDeleteModeButtons()
			{
				var canApply = !_window._creating && HasDeleteSelection;
				if (DrawApplyDeleteButton(canApply, APPLY_PINNED_DELETE_TOOLTIP))
					ApplyDelete();

				if (DrawCancelDeleteButton(CANCEL_PINNED_DELETE_TOOLTIP))
					CancelDeleteMode();
			}

			private void BeginDeleteMode()
			{
				DeleteMode = true;
				_deleteSelection?.Clear();
				_window.Repaint();
			}

			private void CancelDeleteMode()
			{
				DeleteMode = false;
				_deleteSelection?.Clear();
				_window.Repaint();
			}

			private void ApplyDelete()
			{
				if (!HasDeleteSelection)
					return;

				using (ListPool<string>.Get(out var keys))
				{
					if (!Items.IsNullOrEmpty())
					{
						for (int i = 0; i < Items.Length; i++)
						{
							if (IsSelectedForDelete(Items[i].PinKey))
								keys.Add(Items[i].PinKey);
						}
					}

					if (_window.TryRemovePinned(keys))
						CancelDeleteMode();
				}
			}

			[ShowInInspector]
			[ListDrawerSettings(OnTitleBarGUI = nameof(DrawTitleBarButtons), NumberOfItemsPerPage = 100, HideAddButton = true, HideRemoveButton = true, DraggableItems = false)]
			public PinnedRow[] Items { get; set; }

			public PinnedPage(ContentBrowserWindow window)
			{
				_window = window;
			}
		}

		/// <summary>
		/// Строка списка закреплённых элементов — чистое имя и кнопка перехода
		/// </summary>
		[Serializable]
		private struct PinnedRow
		{
			private readonly ContentBrowserWindow _window;
			private readonly PinnedPage _page;
			private readonly string _key;
			private readonly string _displayName;
			private readonly string _menuPath;
			private readonly ContentScriptableObject _asset;

			public PinnedRow(ContentBrowserWindow window, PinnedPage page, string key, string displayName, string menuPath, ContentScriptableObject asset)
			{
				_window = window;
				_page = page;
				_key = key;
				_displayName = displayName;
				_menuPath = menuPath;
				_asset = asset;
			}

			public string PinKey => _key;

			[ShowInInspector, HideLabel]
			[HorizontalGroup]
			public string Name => _displayName;

			[ShowInInspector, HideLabel]
			[HorizontalGroup(22f)]
			[PropertySpace(2)]
			[ShowIf(nameof(ShowDeleteToggle))]
			public bool Delete { get => _page != null && _page.IsSelectedForDelete(_key); set => _page?.SetSelectedForDelete(_key, value); }

			[HorizontalGroup(22f)]
			[PropertySpace(2)]
			[HideIf(nameof(ShowDeleteToggle))]
			[Button("→")]
			public void Open()
			{
				_window.TrySelectMenuItem(_menuPath);

				if (_menuPath.IsNullOrEmpty() && _asset != null)
					_window.TrySelectMenuItemWithObject(_asset);
			}

			private bool ShowDeleteToggle => _page is {DeleteMode: true};
		}

		private abstract class ConfigRowsPage
		{
			private readonly ContentBrowserWindow _window;
			private HashSet<ContentEntryScriptableObject> _deleteSelection;
			private readonly GUIContent _createTooltip;
			private readonly string _idGroupPath;

			public string MenuPath { get; }
			public string CreateMenuItemName { get; }
			public ContentDatabaseScriptableObject Database { get; }
			public Type ConfigType { get; }
			public Type ValueType { get; }
			public bool DeleteMode { get; private set; }
			private ContentScriptableObject GroupAsset =>
				_idGroupPath.IsNullOrEmpty() || Items.Count == 0 ? null : Items[0].Asset;
			private bool HasDeleteSelection => _deleteSelection is {Count: > 0};

			public void CreateNew()
			{
				_window.PromptCreateContentEntry(Database, ConfigType, GroupAsset, _idGroupPath);
			}

			public void Select()
			{
				_window.TrySelectMenuItem(MenuPath);
			}

			public bool IsSelectedForDelete(ContentScriptableObject asset)
			{
				return asset is ContentEntryScriptableObject entry && _deleteSelection != null && _deleteSelection.Contains(entry);
			}

			public void SetSelectedForDelete(ContentScriptableObject asset, bool selected)
			{
				if (asset is not ContentEntryScriptableObject entry)
					return;

				if (selected)
					(_deleteSelection ??= new HashSet<ContentEntryScriptableObject>()).Add(entry);
				else
					_deleteSelection?.Remove(entry);

				_window.Repaint();
			}

			protected void DrawTitleBarButtons()
			{
				if (DeleteMode)
				{
					DrawDeleteModeButtons();
					return;
				}

				EditorGUI.BeginDisabledGroup(_window._creating);
				if (DrawToolbarButton(SdfIconType.Plus, _createTooltip))
					CreateNew();

				if (DrawToolbarButton(SdfIconType.TrashFill, CATEGORY_DELETE_MODE_TOOLTIP))
					BeginDeleteMode();
				EditorGUI.EndDisabledGroup();
			}

			private void DrawDeleteModeButtons()
			{
				var canApply = !_window._creating && HasDeleteSelection;
				if (DrawApplyDeleteButton(canApply, APPLY_CATEGORY_DELETE_TOOLTIP))
					ApplyDelete();

				if (DrawCancelDeleteButton(CANCEL_CATEGORY_DELETE_TOOLTIP))
					CancelDeleteMode();
			}

			private void BeginDeleteMode()
			{
				DeleteMode = true;
				_deleteSelection?.Clear();
				_window.Repaint();
			}

			private void CancelDeleteMode()
			{
				DeleteMode = false;
				_deleteSelection?.Clear();
				_window.Repaint();
			}

			private void ApplyDelete()
			{
				if (!HasDeleteSelection)
					return;

				using (ListPool<ContentEntryScriptableObject>.Get(out var assets))
				{
					if (Items.Count > 0)
					{
						for (int i = 0; i < Items.Count; i++)
						{
							if (Items[i].Asset is ContentEntryScriptableObject asset && IsSelectedForDelete(asset))
								assets.Add(asset);
						}
					}

					if (_window.TryDeleteAssets(assets, MenuPath, ConfigType))
						CancelDeleteMode();
				}
			}

			[ShowInInspector, Searchable]
			[ContentBrowserToggleInlineEditorsOnAltFoldout]
			[ListDrawerSettings(OnTitleBarGUI = nameof(DrawTitleBarButtons), NumberOfItemsPerPage = 100, HideAddButton = true, HideRemoveButton = true, DraggableItems = false)]
			// Setter нужен Odin, иначе getter-only свойство блокирует toolbar и кнопки строк
			public List<CategoryRow> Items { get; set; } = new();

			protected ConfigRowsPage(ContentBrowserWindow window,
				ContentDatabaseScriptableObject database,
				Type configType,
				string menuPath,
				string createMenuItemName,
				string idGroupPath = null)
			{
				_window = window;
				Database = database;
				ConfigType = configType;
				ValueType = GetContentEntryValueType(configType);
				MenuPath = menuPath;
				CreateMenuItemName = createMenuItemName;
				_idGroupPath = idGroupPath;

				var tooltip = idGroupPath.IsNullOrEmpty()
					? createMenuItemName
					: $"{createMenuItemName} ({idGroupPath}/...)";
				_createTooltip = new GUIContent(string.Empty, tooltip);
			}

			public void AddItem(ContentScriptableObject asset)
			{
				Items.Add(new CategoryRow(_window, this, asset));
			}
		}

		/// <summary>
		/// Страница Id-группы — плоский список всех конфигов внутри ветки
		/// </summary>
		private sealed class IdGroupPage : ConfigRowsPage
		{
			public IdGroupPage(ContentBrowserWindow window,
				ContentDatabaseScriptableObject database,
				string menuPath,
				string idGroupPath,
				Type configType,
				string createMenuItemName)
				: base(window, database, configType, menuPath, createMenuItemName, idGroupPath)
			{
			}
		}

		/// <summary>
		/// Страница категории — таблица со списком конфигов выбранного типа
		/// </summary>
		private sealed class CategoryPage : ConfigRowsPage
		{
			public string PinKey { get; }

			[ShowInInspector, ReadOnly, PropertyOrder(-99), PropertySpace(0, 3)]
			[ShowIf(nameof(ShowScript))]
			public MonoScript Script => ConfigType.FindMonoScript();

			private static bool ShowScript => ContentEntryMonoScriptVisibilityMenu.IsEnable;

			public CategoryPage(ContentBrowserWindow window,
				ContentDatabaseScriptableObject database,
				Type configType,
				string menuPath,
				string pinKey,
				string displayTypeName)
				: base(window, database, configType, menuPath, GetCreateConfigMenuItemName(displayTypeName))
			{
				PinKey = pinKey;
			}
		}

		/// <summary>
		/// Строка списка категории — ObjectField конфига, дата создания и кнопка перехода
		/// </summary>
		[Serializable]
		private struct CategoryRow : IContentBrowserInlineButtonHandler, IContentBrowserInlineToggleHandler
		{
			private readonly ContentBrowserWindow _window;
			private readonly ConfigRowsPage _page;
			private readonly ContentScriptableObject _asset;

			public CategoryRow(ContentBrowserWindow window, ConfigRowsPage page, ContentScriptableObject asset)
			{
				_window = window;
				_page = page;
				_asset = asset;
			}

			// ObjectField остаётся кликабельным, но новое значение не записывается в строку
			[ShowInInspector, HideLabel]
			[ContentBrowserInlineEditor("→")]
			public ContentScriptableObject Asset { get => _asset; set { } }

			public void OnContentBrowserInlineButton()
			{
				if (_asset != null)
					_window.TrySelectMenuItemWithObject(_asset);
			}

			bool IContentBrowserInlineToggleHandler.ShowContentBrowserInlineToggle => _page is {DeleteMode: true};
			bool IContentBrowserInlineToggleHandler.ContentBrowserInlineToggle { get => _page != null && _page.IsSelectedForDelete(_asset); set => _page?.SetSelectedForDelete(_asset, value); }
		}
	}

	internal interface IContentBrowserInlineButtonHandler
	{
		void OnContentBrowserInlineButton();
	}

	internal interface IContentBrowserInlineToggleHandler
	{
		bool ShowContentBrowserInlineToggle { get; }
		bool ContentBrowserInlineToggle { get; set; }
	}

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	internal sealed class ContentBrowserToggleInlineEditorsOnAltFoldoutAttribute : Attribute
	{
	}

	internal sealed class ContentBrowserToggleInlineEditorsOnAltFoldoutAttributeDrawer :
		OdinAttributeDrawer<ContentBrowserToggleInlineEditorsOnAltFoldoutAttribute>
	{
		protected override void DrawPropertyLayout(GUIContent label)
		{
			var wasExpanded = Property.State.Expanded;

			CallNextDrawer(label);

			if (wasExpanded != Property.State.Expanded && Event.current.alt)
				ContentBrowserInlineEditorAttributeDrawer.SetAll(Property.State.Expanded);
		}
	}

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	internal sealed class ContentBrowserInlineEditorAttribute : Attribute
	{
		public string ButtonLabel { get; }

		public ContentBrowserInlineEditorAttribute(string buttonLabel)
		{
			ButtonLabel = buttonLabel;
		}
	}

	internal sealed class ContentBrowserInlineEditorAttributeDrawer :
		OdinAttributeDrawer<ContentBrowserInlineEditorAttribute>
	{
		private const string CONTROL_ID = "ContentBrowserInlineEditor";
		private const float SIDE_CONTROL_WIDTH = 22f;
		private const float SIDE_CONTROL_SPACING = 4f;

		// Паддинги toggle в delete mode: можно подкрутить, если стиль Unity начнёт растягивать строку
		private const float DELETE_TOGGLE_PADDING_LEFT = 3f;
		private const float DELETE_TOGGLE_PADDING_RIGHT = 3f;
		private const float DELETE_TOGGLE_PADDING_TOP = 1f;
		private const float DELETE_TOGGLE_PADDING_BOTTOM = 1f;
		private const float PREVIEW_ICON_SIZE = 13f;

		private static readonly Color _iconOverlayBackground = EditorGUIUtility.isProSkin
			? new Color(50 / 255f, 50 / 255f, 50 / 255f)
			: new Color(240f / 255f, 240f / 255f, 240f / 255f);

		private static readonly GUIStyle _style = new(SirenixGUIStyles.CardStyle)
		{
			padding = new RectOffset(5, 3, 2, 3),
			margin = new RectOffset
			(
				SirenixGUIStyles.CardStyle.margin.left + 3,
				SirenixGUIStyles.CardStyle.margin.right + 3,
				SirenixGUIStyles.CardStyle.margin.top + 2,
				SirenixGUIStyles.CardStyle.margin.bottom
			)
		};

		private bool _showDetailed;
		private OdinEditor _inlineEditor;
		private int _setVersion;
		private IContentEntrySource _overlayIconSource;
		private Sprite _overlayIconSprite;

		public static int SetVersion { get; private set; }
		public static bool SetDetailed { get; private set; }

		public static void SetAll(bool showDetailed)
		{
			SetDetailed = showDetailed;
			SetVersion++;
			GUIHelper.RequestRepaint();
		}

		protected override void DrawPropertyLayout(GUIContent label)
		{
			if (Property.ValueEntry.WeakSmartValue is not ContentScriptableObject asset)
				return;

			ApplySetRequest();

			var originalIndent = EditorGUI.indentLevel;
			var originalEnabled = GUI.enabled;
			var useIndent = false;

			if (!EditorGUIUtility.hierarchyMode && asset != null)
			{
				EditorGUI.indentLevel += 1;
				useIndent = true;
			}

			GUI.SetNextControlName(CONTROL_ID);
			var headerRect = DrawReadonlyObjectField(label, asset);
			TryCreateEditor(asset);

			if (_inlineEditor != null)
				DrawInlineEditor(headerRect, useIndent);

			EditorGUI.indentLevel = originalIndent;
			GUI.enabled = originalEnabled;
		}

		private void ApplySetRequest()
		{
			if (_setVersion == SetVersion)
				return;

			_showDetailed = SetDetailed;
			_setVersion = SetVersion;
		}

		private Rect DrawReadonlyObjectField(GUIContent label, ContentScriptableObject asset)
		{
			var rect = EditorGUILayout.GetControlRect();
			var fieldRect = rect;
			fieldRect.width -= SIDE_CONTROL_WIDTH + SIDE_CONTROL_SPACING;
			var sideControlRect = rect.AlignRight(SIDE_CONTROL_WIDTH);

			var originalEnabled = GUI.enabled;
			var originalChanged = GUI.changed;
			var assetType = asset != null ? asset.GetType() : typeof(ContentScriptableObject);

			GUI.enabled = false;
			EditorGUI.BeginChangeCheck();
			EditorGUI.ObjectField(fieldRect, label ?? GUIContent.none, asset, assetType, false);

			if (EditorGUI.EndChangeCheck())
				GUI.changed = originalChanged;

			GUI.enabled = true;
			DrawAssetIconOverlay(fieldRect, label, asset);
			DrawSideControl(sideControlRect);

			GUI.enabled = originalEnabled;

			return rect;
		}

		private void DrawAssetIconOverlay(Rect rect, GUIContent label, ContentScriptableObject asset)
		{
			if (Event.current.type != EventType.Repaint || !asset)
				return;

			if (asset is not IContentEntrySource source)
				return;

			if (!ReferenceEquals(_overlayIconSource, source))
			{
				_overlayIconSource = source;
				_overlayIconSprite = ContentPreviewUtility.GetPreviewIcon(source);
			}

			var sprite = _overlayIconSprite;
			if (!sprite || !sprite.texture)
				return;

			// Повторяем математику EditorGUI.ObjectField -> PrefixLabel
			Rect objectFieldRect;
			if (label != null && (!label.text.IsNullOrEmpty() || label.image != null))
			{
				objectFieldRect = rect;
				objectFieldRect.xMin += EditorGUIUtility.labelWidth + 2f;
			}
			else
			{
				objectFieldRect = EditorGUI.IndentedRect(rect);
			}

			var iconRect = new Rect(
				objectFieldRect.x + 2f,
				objectFieldRect.y + (objectFieldRect.height - PREVIEW_ICON_SIZE) * 0.5f,
				PREVIEW_ICON_SIZE,
				PREVIEW_ICON_SIZE);

			EditorGUI.DrawRect(iconRect, _iconOverlayBackground);

			var prevColor = GUI.color;
			GUI.color = new Color(1f, 1f, 1f, 0.5f);
			FusumityEditorGUILayout.DrawObjectFieldIconSprite(iconRect, sprite);
			GUI.color = prevColor;
		}

		private void DrawSideControl(Rect rect)
		{
			if (Property.Parent?.ValueEntry?.WeakSmartValue is IContentBrowserInlineToggleHandler toggleHandler &&
				toggleHandler.ShowContentBrowserInlineToggle)
			{
				DrawInlineToggle(rect, toggleHandler);
				return;
			}

			if (GUI.Button(rect, Attribute.ButtonLabel))
				InvokeButtonHandler();
		}

		private static void DrawInlineToggle(Rect rect, IContentBrowserInlineToggleHandler toggleHandler)
		{
			var toggleRect = new Rect(
				rect.x + DELETE_TOGGLE_PADDING_LEFT,
				rect.y + DELETE_TOGGLE_PADDING_TOP,
				rect.width - DELETE_TOGGLE_PADDING_LEFT - DELETE_TOGGLE_PADDING_RIGHT,
				rect.height - DELETE_TOGGLE_PADDING_TOP - DELETE_TOGGLE_PADDING_BOTTOM);

			EditorGUI.BeginChangeCheck();
			var value = GUI.Toggle(toggleRect, toggleHandler.ContentBrowserInlineToggle, GUIContent.none);
			if (EditorGUI.EndChangeCheck())
				toggleHandler.ContentBrowserInlineToggle = value;
		}

		private void DrawInlineEditor(Rect headerRect, bool useIndent)
		{
			var labelWidthInEditor = GUIHelper.BetterLabelWidth - 4f;

			GUIHelper.PushColor(Color.white);
			{
				var foldoutPosition = headerRect.AlignBottom(EditorGUIUtility.singleLineHeight);
				foldoutPosition.width = SirenixEditorGUI.FoldoutWidth;

				if (!EditorGUIUtility.hierarchyMode)
				{
					var offset = SirenixEditorGUI.FoldoutWidth + 3;
					foldoutPosition.x -= offset;
					foldoutPosition.width += offset;
				}

				var originalEnabled = GUI.enabled;
				GUI.enabled = true;
				_showDetailed = SirenixEditorGUI.Foldout(foldoutPosition, _showDetailed, GUIContent.none);
				GUI.enabled = originalEnabled;

				if (SirenixEditorGUI.BeginFadeGroup(this, _showDetailed))
				{
					var originalColor = GUI.color;
					GUI.color = Color.black.WithAlpha(0.666f);

					var originalHierarchyMode = EditorGUIUtility.hierarchyMode;
					EditorGUIUtility.hierarchyMode = false;

					var originalIndent = EditorGUI.indentLevel;
					if (useIndent)
						EditorGUI.indentLevel -= 1;

					SirenixEditorGUI.BeginIndentedVertical(_style);
					{
						GUIHelper.PushHierarchyMode(false);
						GUIHelper.PushLabelWidth(labelWidthInEditor);
						{
							GUI.color = originalColor;
							DrawEditorInspector();
						}
						GUIHelper.PopLabelWidth();
						GUIHelper.PopHierarchyMode();

						EditorGUI.indentLevel = originalIndent;
					}

					SirenixEditorGUI.EndIndentedVertical();
					EditorGUIUtility.hierarchyMode = originalHierarchyMode;
				}

				SirenixEditorGUI.EndFadeGroup();
			}
			GUIHelper.PopColor();
		}

		private void InvokeButtonHandler()
		{
			if (Property.Parent?.ValueEntry?.WeakSmartValue is IContentBrowserInlineButtonHandler handler)
				handler.OnContentBrowserInlineButton();
		}

		private void DrawEditorInspector()
		{
			var originalForceHideMonoScriptInEditor = OdinEditor.ForceHideMonoScriptInEditor;
			var originalDrawAssetReference = FusumityEditorGUIHelper.drawAssetReference;
			var originalDrawInlineEditor = FusumityEditorGUIHelper.drawInlineEditor;
			var originalAllowInlineEditorIdEditing = FusumityEditorGUIHelper.allowInlineEditorIdEditing;
			var originalEnabled = GUI.enabled;

			OdinEditor.ForceHideMonoScriptInEditor = false;
			FusumityEditorGUIHelper.drawAssetReference = false;
			FusumityEditorGUIHelper.drawInlineEditor = true;
			FusumityEditorGUIHelper.allowInlineEditorIdEditing = true;
			GUI.enabled = true;

			try
			{
				_inlineEditor.OnInspectorGUI();
			}
			finally
			{
				GUI.enabled = originalEnabled;
				FusumityEditorGUIHelper.allowInlineEditorIdEditing = originalAllowInlineEditorIdEditing;
				FusumityEditorGUIHelper.drawInlineEditor = originalDrawInlineEditor;
				FusumityEditorGUIHelper.drawAssetReference = originalDrawAssetReference;
				OdinEditor.ForceHideMonoScriptInEditor = originalForceHideMonoScriptInEditor;
			}
		}

		private void TryCreateEditor(ContentScriptableObject asset)
		{
			if (asset != null && _inlineEditor != null && _inlineEditor.target == asset)
				return;

			if (_inlineEditor != null)
			{
				OdinEditor.DestroyImmediate(_inlineEditor);
				_inlineEditor = null;
			}

			if (asset != null)
				_inlineEditor = (OdinEditor) OdinEditor.CreateEditor(asset);
		}
	}
}
