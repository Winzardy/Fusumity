using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
		private int _navigationHistoryIndex = -1;
		private OdinMenuItem _lastNavigationHistoryItem;
		private string _lastNavigationHistoryPath;
		private bool _skipNextNavigationHistoryRecord;
		private UnityEngine.Object _lastProjectSelection;

		private const string PINNED_GROUP = "Pinned";
		private const string PINNED_PREF_KEY = "ContentBrowser.Pinned";
		private const string AUTO_SYNC_PREF_KEY = "ContentBrowser.AutoSyncProjectSelection";
		private const string PINNED_CATEGORY_PREFIX = "category:";
		private const string NEW_CONFIG_MENU_ITEM_PREFIX = "New";
		private const string SCRIPTABLE_OBJECT_SUFFIX = "ScriptableObject";
		private const string CONFIG_SUFFIX = "Config";
		private const string DATABASE_SUFFIX = "Database";
		private const string BREADCRUMB_LINK_COLOR = "FFFFFF";

		private const int MAX_NAVIGATION_HISTORY_COUNT = 64;

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
		private static readonly GUIContent BREADCRUMB_SEPARATOR = new(" / ");

		private OdinMenuItem _breadcrumbLeaf;
		private readonly List<OdinMenuItem> _breadcrumbNodes = new();
		private readonly List<GUIContent> _breadcrumbContents = new();

		private static readonly Color APPLY_DELETE_ENABLED_COLOR = new(0.35f, 0.8f, 0.35f, 1f);
		private static readonly Color CANCEL_DELETE_COLOR = new(1f, 0.05f, 0.05f, 1f);

		private static Type[] _creatableConfigTypes;

		private static bool? _autoSyncProjectSelection;

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

			// Встроенный поиск Odin по дереву (точный Contains вместо fuzzy — иначе guid даёт кучу ложных совпадений)
			tree.Config.DrawSearchToolbar = true;
			tree.Config.SearchFunction = SearchItem;
			tree.DefaultMenuStyle.IconSize = 18f;

			_assetMenuItems.Clear();
			_categoryMenuItems.Clear();
			_pinnedMenuItems.Clear();

			var modules = ContentBrowserInfo.GetModules(_forceRefresh);
			_forceRefresh = false;

			for (int i = 0; i < modules.Length; i++)
			{
				var module = modules[i];
				if (module.Db == null)
					continue;

				// Категория верхнего уровня — база контента (по клику открывается сама база)
				var dbName = Nicify(module.Name);
				tree.Add(dbName, module.Db, IconFor(module.Db));
				RegisterAssetMenuItem(module.Db, tree.GetMenuItem(dbName));

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

						var displayTypeName = GetConfigDisplayName(type);
						var typeName = CategoryNameFor(displayTypeName);
						var typePath = $"{dbName}/{typeName}";

						// Страница категории — список конфигов этого типа (рисуется при клике на категорию)
						using (ListPool<CategoryRow>.Get(out var items))
						{
							var page = new CategoryPage(this, module.Db, type, typePath, CategoryPinKey(module.Db, type), displayTypeName);

							if (hasConfigs)
								for (int j = 0; j < configs.Count; j++)
									items.Add(new CategoryRow(this, page, configs[j]));

							page.Items = items.ToArray();
							tree.Add(typePath, page, SdfIconType.Folder2Open);
							RegisterCategoryMenuItem(page, tree.GetMenuItem(typePath));
							AddCreateConfigLeaf(tree, typePath, page);
						}

						if (!hasConfigs)
							continue;

						// Листья — конкретные конфиги (выключенные рисуются серым), при выборе открывается inline-редактор
						for (int j = 0; j < configs.Count; j++)
							AddConfigLeaf(tree, typePath, configs[j], single: false);
					}
				}
			}

			// Расширяем поиск: ищем не только по имени, но и по Id / Guid / Guid вложенных Entry
			BuildSearchStrings(tree);

			// Закреплённые элементы — отдельной группой
			BuildPinnedSection(tree);

			tree.SortMenuItemsByName();
			MoveCreateConfigItemsToBottom(tree.RootMenuItem);

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

			foreach (var interfaceType in type.GetInterfaces())
			{
				if (interfaceType.IsGenericType &&
					interfaceType.GetGenericTypeDefinition() == typeof(IContentEntryScriptableObject<>))
				{
					return interfaceType.GetGenericArguments()[0];
				}
			}

			return null;
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

				items.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));

				var rows = new PinnedRow[items.Count];
				for (int i = 0; i < items.Count; i++)
					rows[i] = new PinnedRow(this, items[i].Name, items[i].MenuPath, items[i].Asset);

				tree.Add(PINNED_GROUP, new PinnedPage(rows), SdfIconType.PinAngleFill);

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

		private static string CategoryNameFor(string typeName)
		{
			return typeName.IsNullOrEmpty() ? typeName : $"{typeName}'s";
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
				ContentDatabaseScriptableObject database => IsSingleEntryDatabase(database)
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
				_selectOriginalPathAfterRefresh = originalMenuPath;
			}
			else
			{
				Pinned.Add(key);
				_selectPinnedKeyAfterRefresh = key;
				_selectOriginalPathAfterRefresh = originalMenuPath;
			}

			EditorPrefs.SetString(PINNED_PREF_KEY, string.Join("|", Pinned));
			ForceMenuTreeRebuild();
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

		private static void AddCreateConfigLeaf(OdinMenuTree tree, string path, CategoryPage page)
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

		private static string GetCreateConfigMenuItemName(string typeName)
		{
			return typeName.IsNullOrEmpty() ? NEW_CONFIG_MENU_ITEM_PREFIX : $"{NEW_CONFIG_MENU_ITEM_PREFIX} {typeName}";
		}

		// При поиске Odin строит плоский список в порядке дерева и НЕ сортирует его (кастомная SearchFunction).
		// Поднимаем базы и категории над конфигами. Сортируем и до, и после отрисовки — чтобы не было кадра в pre-order
		protected override void DrawMenu()
		{
			SortSearchResults();
			base.DrawMenu();

			if (SortSearchResults())
				Repaint();
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
				CategoryPage => 1,
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
				if (SirenixEditorGUI.ToolbarButton(SdfIconType.BoxArrowInLeft))
					TrySelectMenuItem(originalMenuPath);

				var lastRect = GUILayoutUtility.GetLastRect();
				GUI.Label(lastRect, OPEN_ORIGINAL_PAGE_TOOLTIP);
			}
			else if (TryGetPinnedKey(selectedValue, out var pinnedKey))
			{
				if (SirenixEditorGUI.ToolbarButton(SdfIconType.BoxArrowInRight))
					TrySelectPinnedMenuItem(pinnedKey);

				var lastRect = GUILayoutUtility.GetLastRect();
				GUI.Label(lastRect, OPEN_PINNED_PAGE_TOOLTIP);
			}

			GUILayout.Space(3);

			// Кликабельный путь "База / Категория / Имя" — клик по базе/категории открывает их страницы
			if (selectedItem != null)
				DrawBreadcrumb(selectedItem);

			GUILayout.FlexibleSpace();

			if (selectedValue is ContentEntryScriptableObject selectedAsset)
			{
				DrawGenerateConstantsButton(selectedAsset);
				DrawDocumentationButton(selectedAsset);
				DrawReferenceSearchButton(selectedAsset);
				DrawDeleteButton(selectedAsset, selectedItem);
			}
			else if (selectedValue is CategoryPage selectedCategoryPage)
			{
				DrawGenerateConstantsButton(selectedCategoryPage);
				DrawDocumentationButton(selectedCategoryPage);
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
			var clicked = SirenixEditorGUI.ToolbarButton(SdfIconType.GearFill);
			var rect = GUILayoutUtility.GetLastRect();
			GUI.Label(rect, SETTINGS_TOOLTIP);

			if (!clicked)
				return;

			var menu = new GenericMenu();
			menu.AddItem(new GUIContent("Sync Project Selection"), AutoSyncProjectSelection,
				() => AutoSyncProjectSelection = !AutoSyncProjectSelection);
			menu.AddItem(new GUIContent("Force Rebuild Browser"), false, ForceRebuild);
			menu.ShowAsContext();
		}

		private void DrawReferenceSearchButton(ContentScriptableObject asset)
		{
			if (asset == null)
				return;

			if (SirenixEditorGUI.ToolbarButton(SdfIconType.FileEarmarkBreakFill))
				ContentSearchProvider.OpenReferenceSearch(asset);

			var rect = GUILayoutUtility.GetLastRect();
			GUI.Label(rect, new GUIContent(string.Empty, ContentSearchProvider.GetFindReferencesTooltip(asset)));
		}

		private void DrawDocumentationButton(ContentEntryScriptableObject asset)
		{
			if (!TryGetDocumentationUrl(asset, out var url))
				return;

			DrawDocumentationButton(url, asset.ValueType);
		}

		private void DrawDocumentationButton(CategoryPage page)
		{
			if (!TryGetDocumentationUrl(page, out var url))
				return;

			DrawDocumentationButton(url, page.ValueType);
		}

		private static void DrawDocumentationButton(string url, Type type)
		{
			if (SirenixEditorGUI.ToolbarButton(SdfIconType.JournalBookmarkFill))
				Help.BrowseURL(url);

			var rect = GUILayoutUtility.GetLastRect();
			GUI.Label(rect, GetDocumentationTooltip(type));
		}

		private static bool TryGetDocumentationUrl(ContentEntryScriptableObject asset, out string url)
		{
			url = string.Empty;

			if (asset == null)
				return false;

			return TryGetDocumentationUrl(asset.GetType(), asset.ValueType, out url);
		}

		private static bool TryGetDocumentationUrl(CategoryPage page, out string url)
		{
			url = string.Empty;

			return page != null && TryGetDocumentationUrl(page.ConfigType, page.ValueType, out url);
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

		private void DrawGenerateConstantsButton(ContentEntryScriptableObject asset)
		{
			if (asset == null || !HasContentGeneration(asset.GetType(), asset.ValueType))
				return;

			var type = asset.ValueType;
			EditorGUI.BeginDisabledGroup(_creating);

			if (SirenixEditorGUI.ToolbarButton(SdfIconType.Magic))
				GenerateConstants(type);

			var rect = GUILayoutUtility.GetLastRect();
			GUI.Label(rect, GetGenerateConstantsTooltip(type));

			EditorGUI.EndDisabledGroup();
		}

		private void DrawGenerateConstantsButton(CategoryPage page)
		{
			if (page == null || !HasContentGeneration(page.ConfigType, page.ValueType))
				return;

			var type = page.ValueType;
			EditorGUI.BeginDisabledGroup(_creating);

			if (SirenixEditorGUI.ToolbarButton(SdfIconType.Magic))
				GenerateConstants(type);

			var rect = GUILayoutUtility.GetLastRect();
			GUI.Label(rect, GetGenerateConstantsTooltip(type));

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

			if (SirenixEditorGUI.ToolbarButton(SdfIconType.TrashFill))
				TryDeleteAsset(asset, selectedItem);

			var rect = GUILayoutUtility.GetLastRect();
			GUI.Label(rect, DELETE_ASSET_TOOLTIP);

			EditorGUI.EndDisabledGroup();
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
					pinnedChanged = true;
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

		private void PromptCreateContentEntry(ContentDatabaseScriptableObject database, Type configType, ContentScriptableObject sibling)
		{
			if (_creating || !CanCreateContentEntry(configType))
				return;

			var folder = GetCreateFolder(database, sibling);
			if (folder.IsNullOrEmpty())
				return;

			var defaultAssetName = GetDefaultAssetName(configType);
			CreateConfigNameWindow.Open(GetCreateConfigMenuItemName(GetConfigDisplayName(configType)), defaultAssetName, folder,
				(assetName, assetFolder) => CreateContentEntry(configType, assetFolder, assetName));
		}

		private void CreateContentEntry(Type configType, string folder, string assetName)
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

				var created = AssetDatabase.LoadAssetAtPath<ContentScriptableObject>(assetPath);
				if (created != null)
				{
					if (created.NeedSync())
					{
						created.Sync(true);
						AssetDatabase.SaveAssetIfDirty(created);
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

		private static string GetCreateFolder(ContentDatabaseScriptableObject database, ContentScriptableObject sibling)
		{
			var folder = GetAssetFolder(sibling);
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
			var builder = new StringBuilder(assetName.Length);
			for (int i = 0; i < assetName.Length; i++)
			{
				var character = assetName[i];
				builder.Append(IsInvalidAssetNameCharacter(character, invalidChars) ? '_' : character);
			}

			var sanitized = builder.ToString().Trim();
			return sanitized.IsNullOrEmpty() ? null : sanitized;
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
			if (SirenixEditorGUI.ToolbarButton(SdfIconType.ArrowLeft))
				NavigateHistory(-1);

			var lastRect = GUILayoutUtility.GetLastRect();
			GUI.Label(lastRect, BACK_TOOLTIP);
			EditorGUI.EndDisabledGroup();

			EditorGUI.BeginDisabledGroup(!CanNavigateHistory(1));
			if (SirenixEditorGUI.ToolbarButton(SdfIconType.ArrowRight))
				NavigateHistory(1);

			lastRect = GUILayoutUtility.GetLastRect();
			GUI.Label(lastRect, FORWARD_TOOLTIP);
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
			// Закреплённые копии и сама страница "Pinned" в поиске не участвуют (есть оригиналы)
			if (item is PinnedMenuItem || item is PinnedCategoryMenuItem || item.Value is PinnedPage or SeparatorMenuAction)
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

		// Строка поиска конфига: имя + Id + Guid + Guid'ы вложенных Entry
		private static string BuildSearchString(string name, IUniqueContentEntrySource source)
		{
			var sb = new StringBuilder(name);

			if (!source.Id.IsNullOrEmpty())
				sb.Append(' ').Append(source.Id);

			sb.Append(' ').Append(source.Guid.ToString());

			var entry = source.UniqueContentEntry;
			if (entry?.Nested != null)
			{
				foreach (var nestedGuid in entry.Nested.Keys)
					sb.Append(' ').Append(nestedGuid.ToString());
			}

			return sb.ToString();
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

		/// <summary>
		/// Закреплённая копия категории — помечена отдельным типом, чтобы пропускать её в поиске
		/// </summary>
		private class PinnedCategoryMenuItem : OdinMenuItem
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
			private readonly CategoryPage _page;
			private bool _executing;

			public CreateConfigAction(CategoryPage page)
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
			[ShowInInspector]
			[ListDrawerSettings(NumberOfItemsPerPage = 100, HideAddButton = true, HideRemoveButton = true, DraggableItems = false)]
			public PinnedRow[] Items { get; set; }

			public PinnedPage(PinnedRow[] items)
			{
				Items = items;
			}
		}

		/// <summary>
		/// Строка списка закреплённых элементов — чистое имя и кнопка перехода
		/// </summary>
		[Serializable]
		private struct PinnedRow
		{
			private readonly ContentBrowserWindow _window;
			private readonly string _displayName;
			private readonly string _menuPath;
			private readonly ContentScriptableObject _asset;

			public PinnedRow(ContentBrowserWindow window, string displayName, string menuPath, ContentScriptableObject asset)
			{
				_window = window;
				_displayName = displayName;
				_menuPath = menuPath;
				_asset = asset;
			}

			[ShowInInspector, HideLabel]
			[HorizontalGroup]
			public string Name => _displayName;

			[HorizontalGroup(22f)]
			[PropertySpace(2)]
			[Button("→")]
			public void Open()
			{
				_window.TrySelectMenuItem(_menuPath);

				if (_menuPath.IsNullOrEmpty() && _asset != null)
					_window.TrySelectMenuItemWithObject(_asset);
			}
		}

		/// <summary>
		/// Страница категории — таблица со списком конфигов выбранного типа
		/// </summary>
		private class CategoryPage
		{
			private readonly ContentBrowserWindow _window;
			private HashSet<ContentEntryScriptableObject> _deleteSelection;

			// Заголовок таблицы (резолвится через "$Header" в LabelText, отдельно не рисуется)
			public string MenuPath { get; }
			public string PinKey { get; }
			public string CreateMenuItemName { get; }
			public ContentDatabaseScriptableObject Database { get; }
			public Type ConfigType { get; }
			public Type ValueType { get; }
			public ContentScriptableObject FirstAsset => Items.IsNullOrEmpty() ? null : Items[0].Asset;
			public bool DeleteMode { get; private set; }
			private bool HasDeleteSelection => _deleteSelection is {Count: > 0};
			private readonly GUIContent _createTooltip;

			public void CreateNew()
			{
				_window.PromptCreateContentEntry(Database, ConfigType, FirstAsset);
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

			private void DrawTitleBarButtons()
			{
				if (DeleteMode)
				{
					DrawDeleteModeButtons();
					return;
				}

				EditorGUI.BeginDisabledGroup(_window._creating);
				if (SirenixEditorGUI.ToolbarButton(SdfIconType.Plus))
					CreateNew();

				var lastRect = GUILayoutUtility.GetLastRect();
				GUI.Label(lastRect, _createTooltip);

				if (SirenixEditorGUI.ToolbarButton(SdfIconType.TrashFill))
					BeginDeleteMode();

				lastRect = GUILayoutUtility.GetLastRect();
				GUI.Label(lastRect, CATEGORY_DELETE_MODE_TOOLTIP);
				EditorGUI.EndDisabledGroup();
			}

			private void DrawDeleteModeButtons()
			{
				var canApply = !_window._creating && HasDeleteSelection;
				EditorGUI.BeginDisabledGroup(!canApply);
				var applyClicked = canApply
					? DrawColoredToolbarButton(SdfIconType.Check, APPLY_DELETE_ENABLED_COLOR)
					: SirenixEditorGUI.ToolbarButton(SdfIconType.Check);

				var lastRect = GUILayoutUtility.GetLastRect();
				GUI.Label(lastRect, APPLY_CATEGORY_DELETE_TOOLTIP);
				EditorGUI.EndDisabledGroup();

				if (applyClicked)
					ApplyDelete();

				if (DrawColoredToolbarButton(SdfIconType.X, CANCEL_DELETE_COLOR))
					CancelDeleteMode();

				lastRect = GUILayoutUtility.GetLastRect();
				GUI.Label(lastRect, CANCEL_CATEGORY_DELETE_TOOLTIP);
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
					if (!Items.IsNullOrEmpty())
					{
						for (int i = 0; i < Items.Length; i++)
						{
							if (Items[i].Asset is ContentEntryScriptableObject asset && IsSelectedForDelete(asset))
								assets.Add(asset);
						}
					}

					if (_window.TryDeleteAssets(assets, MenuPath, ConfigType))
						CancelDeleteMode();
				}
			}

			[ShowInInspector]
			[Searchable]
			[ContentBrowserToggleInlineEditorsOnAltFoldout]
			[ListDrawerSettings(OnTitleBarGUI = nameof(DrawTitleBarButtons), NumberOfItemsPerPage = 100, HideAddButton = true, HideRemoveButton = true, DraggableItems = false)]
			public CategoryRow[] Items { get; set; }

			public CategoryPage(ContentBrowserWindow window,
				ContentDatabaseScriptableObject database,
				Type configType,
				string menuPath,
				string pinKey,
				string displayTypeName)
			{
				_window = window;
				Database = database;
				ConfigType = configType;
				ValueType = GetContentEntryValueType(configType);
				MenuPath = menuPath;
				PinKey = pinKey;
				CreateMenuItemName = GetCreateConfigMenuItemName(displayTypeName);
				_createTooltip = new GUIContent(string.Empty, CreateMenuItemName);
			}
		}

		/// <summary>
		/// Строка списка категории — ObjectField конфига, дата создания и кнопка перехода
		/// </summary>
		[Serializable]
		private struct CategoryRow : IContentBrowserInlineButtonHandler, IContentBrowserInlineToggleHandler
		{
			private readonly ContentBrowserWindow _window;
			private readonly CategoryPage _page;
			private readonly ContentScriptableObject _asset;

			public CategoryRow(ContentBrowserWindow window, CategoryPage page, ContentScriptableObject asset)
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
				_overlayIconSprite = ContentEntryIconUtility.GetPreviewIcon(source);
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
			FusumityGUIEditorLayout.DrawObjectFieldIconSprite(iconRect, sprite);
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
			if (asset != null)
			{
				if (_inlineEditor == null)
				{
					_inlineEditor = (OdinEditor) OdinEditor.CreateEditor(asset);
				}
				else if (_inlineEditor.target != asset)
				{
					OdinEditor.DestroyImmediate(_inlineEditor);
					_inlineEditor = null;

					_inlineEditor = (OdinEditor) OdinEditor.CreateEditor(asset);
				}
			}
			else if (_inlineEditor != null)
			{
				OdinEditor.DestroyImmediate(_inlineEditor);
				_inlineEditor = null;
			}
		}
	}
}
