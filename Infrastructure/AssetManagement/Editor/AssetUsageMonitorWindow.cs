using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AssetManagement.AddressableAssets.Editor;
using Fusumity.Editor;
using Sapientia.Extensions;
using Sapientia.Pooling;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace AssetManagement.Editor
{
	using UnityObject = UnityEngine.Object;

	public sealed class AssetUsageMonitorWindow : EditorWindow
	{
		private const string MENU_PATH = "Tools/Assets/Usage Monitor";
		private const string WINDOW_NAME = " Asset Usage Monitor";
		private const int DEFAULT_HISTORY_SECONDS = 30;
		private const int MAX_HISTORY_SECONDS = 60;
		private const double SAMPLE_INTERVAL = 0.05d;
		private const double IDLE_REPAINT_INTERVAL = 0.25d;

		private const float HEADER_HEIGHT = 24f;
		private const float ROW_HEIGHT = 24f;
		private const float DEFAULT_LABEL_WIDTH = 340f;
		private const float MIN_LABEL_WIDTH = 200f;
		private const float MIN_TIMELINE_WIDTH = 240f;
		private const float SPLITTER_HALF_WIDTH = 2.5f;
		private const float MIN_WINDOW_WIDTH = 720f;
		private const float USAGES_BADGE_LEFT_PADDING = 4.5f;
		private const float USAGES_BADGE_RIGHT_PADDING = 4.5f;
		private const float USAGES_BADGE_VALUE_GAP = 1f;
		private const string UNKNOWN_GROUP = "Other";
		private const string UNKNOWN_TYPE = "Unknown";
		private const string TOOLTIP_LABEL_COLOR = "#808080";
		private const string TOOLTIP_LABEL_OPEN = "<color=" + TOOLTIP_LABEL_COLOR + ">";
		private const string USAGES_LABEL = "usages:";
		private const string HEADER_SUMMARY_FORMAT = "{0} assets   •   {1} active   •   {2} usages";

		private static readonly string[] HISTORY_LABELS = {"15 sec", "30 sec", "60 sec"};
		private static readonly int[] HISTORY_VALUES = {15, 30, 60};
		private static readonly string[] RULER_LABELS =
		{
			"now", "-5s", "-10s", "-15s", "-20s", "-25s", "-30s",
			"-35s", "-40s", "-45s", "-50s", "-55s", "-60s"
		};
		private static readonly GUIContent USAGES_LABEL_CONTENT = new(USAGES_LABEL);
		private static readonly GUIContent GROUP_CONTENT = new("Group");
		private static readonly GUIContent TYPE_CONTENT = new("Type");
		private static readonly GUIContent TIME_CONTENT = new("Time");
		private static readonly GUIContent NAME_CONTENT = new("Name");
		private static readonly GUILayoutOption[] GROUPING_POPUP_OPTIONS = {GUILayout.Width(96f)};
		private static readonly GUILayoutOption[] SORTING_POPUP_OPTIONS = {GUILayout.Width(88f)};
		private static readonly GUILayoutOption[] HISTORY_POPUP_OPTIONS = {GUILayout.Width(64f)};
		private static readonly GUILayoutOption[] VIEWPORT_OPTIONS =
		{
			GUILayout.ExpandWidth(true),
			GUILayout.ExpandHeight(true)
		};

		private static readonly Color HEADER_COLOR = new(0.12f, 0.12f, 0.12f, 1f);
		private static readonly Color EVEN_ROW_COLOR = new(1f, 1f, 1f, 0.025f);
		private static readonly Color ODD_ROW_COLOR = new(1f, 1f, 1f, 0.055f);
		private static readonly Color GROUP_ROW_COLOR = new(0.14f, 0.14f, 0.14f, 1f);
		private static readonly Color TYPE_ROW_COLOR = new(0.17f, 0.17f, 0.17f, 1f);
		private static readonly Color HOVER_COLOR = new(0.3f, 0.55f, 0.85f, 0.12f);
		private static readonly Color GRID_COLOR = new(1f, 1f, 1f, 0.09f);
		private static readonly Color RULER_LABEL_BACKGROUND_COLOR = new(0.32f, 0.32f, 0.32f, 0.75f);
		private static readonly Color ACTIVE_COLOR = new(0.2f, 0.75f, 1f, 0.95f);
		private static readonly Color LOADING_COLOR = new(1f, 0.68f, 0.18f, 0.95f);
		private static readonly Color RELEASED_COLOR = new(0.48f, 0.58f, 0.65f, 0.72f);
		private static readonly Color PENDING_COLOR = new(0.48f, 0.58f, 0.65f, 0.35f);
		private static readonly Color TOOLTIP_BACKGROUND_COLOR = new(0.13f, 0.13f, 0.13f, 0.98f);
		private static readonly Color TOOLTIP_BORDER_COLOR = new(0.35f, 0.35f, 0.35f, 1f);
		private static readonly Color HEADER_SUMMARY_TEXT_COLOR = new(0.55f, 0.55f, 0.55f, 1f);

		[NonSerialized]
		private Dictionary<string, AssetRecord> _records;

		[NonSerialized]
		private Dictionary<string, CurrentAssetState> _currentStates;

		[NonSerialized]
		private List<IAssetContainerState> _containerStates;

		[NonSerialized]
		private Dictionary<int, AssetMetadata> _assetMetadata;

		[NonSerialized]
		private Dictionary<string, AssetMetadata> _keyMetadata;

		[NonSerialized]
		private Dictionary<HeaderCacheKey, HeaderSummary> _headerSummaries;

		[NonSerialized]
		private List<AssetRecord> _orderedRecords;

		[NonSerialized]
		private List<DisplayRow> _displayRows;

		[NonSerialized]
		private List<string> _recordsToRemove;

		[NonSerialized]
		private HashSet<string> _collapsedGroups;

		[NonSerialized]
		private HashSet<string> _collapsedTypes;

		[NonSerialized]
		private GUIStyle _rulerLabelStyle;

		[NonSerialized]
		private GUIStyle _countBadgeStyle;

		[NonSerialized]
		private GUIStyle _countBadgeValueStyle;

		[NonSerialized]
		private GUIStyle _countBadgeCardStyle;

		[NonSerialized]
		private GUIStyle _groupHeaderStyle;

		[NonSerialized]
		private GUIStyle _typeHeaderStyle;

		[NonSerialized]
		private GUIStyle _summaryStyle;

		[NonSerialized]
		private GUIStyle _toolbarLabelStyle;

		[NonSerialized]
		private GUIContent _scratchContent;

		[NonSerialized]
		private GUIContent _tooltipContent;

		[NonSerialized]
		private GUIStyle _tooltipStyle;

		[NonSerialized]
		private string _hoveredTooltip;

		[NonSerialized]
		private Rect _hoveredRowRect;

		[NonSerialized]
		private Comparison<AssetRecord> _recordComparison;

		[SerializeField]
		private Vector2 _scroll;

		[SerializeField]
		private int _historySeconds = DEFAULT_HISTORY_SECONDS;

		[SerializeField]
		private float _labelWidth = DEFAULT_LABEL_WIDTH;

		[SerializeField]
		private GroupingMode _grouping = GroupingMode.Group | GroupingMode.Type;

		[SerializeField]
		private SortingMode _sorting = SortingMode.Time;

		private double _nextSampleTime;
		private double _nextRepaintTime;
		private bool _wasEmpty;
		private bool _displayDirty = true;

		[MenuItem(MENU_PATH)]
		private static void Open()
		{
			var window = GetWindow<AssetUsageMonitorWindow>(WINDOW_NAME);
			window.CreateTitle();
			window.minSize = new Vector2(MIN_WINDOW_WIDTH, 260f);
			window.Show();
			window.Focus();
		}

		private void OnEnable()
		{
			CreateTitle();
			EnsureHistoryValue();
			EnsureGroupingMode();
			EnsureSortingMode();
			EnsureData();
			EditorApplication.update += OnEditorUpdate;
			Sample(EditorApplication.timeSinceStartup);
		}

		private void CreateTitle()
		{
			titleContent = new GUIContent(WINDOW_NAME, EditorIcons.LoadingBar.Active);
		}

		private void OnDisable()
		{
			EditorApplication.update -= OnEditorUpdate;
			ReleaseData();
		}

		private void OnEditorUpdate()
		{
			var currentTime = EditorApplication.timeSinceStartup;
			if (currentTime < _nextSampleTime)
				return;

			_nextSampleTime = currentTime + SAMPLE_INTERVAL;
			Sample(currentTime);

			// Частый Repaint видимого окна глушит тултипы по всему редактору:
			// пустое окно не перерисовываем вовсе, вне плеймода — редко
			var isEmpty = _records.Count == 0;
			var becameEmpty = isEmpty && !_wasEmpty;
			_wasEmpty = isEmpty;

			if (isEmpty && !becameEmpty)
				return;

			if (!becameEmpty && !EditorApplication.isPlaying)
			{
				if (currentTime < _nextRepaintTime)
					return;

				_nextRepaintTime = currentTime + IDLE_REPAINT_INTERVAL;
			}

			Repaint();
		}

		private void Sample(double currentTime)
		{
			EnsureData();
			_currentStates.Clear();

			if (EditorApplication.isPlaying && AssetLoader.IsInitialized)
			{
				AssetLoader.CollectAssetContainerStates(_containerStates);

				foreach (var state in _containerStates)
					CollectCurrentState(state);
			}

			foreach (var record in _records.Values)
				record.seen = false;

			foreach (var (key, state) in _currentStates)
			{
				if (!_records.TryGetValue(key, out var record))
				{
					// Загруженный контейнер без usages (release pending) тоже показываем,
					// иначе короткие загрузки (например звук клика) не попадают в монитор вовсе
					if (state.consumers <= 0 && !state.isLoaded)
						continue;

					var metadata = state.metadata.IsValid
						? state.metadata
						: GetKeyMetadata(key);
					record = new AssetRecord(key, metadata);
					_records.Add(key, record);
				}
				else if (state.metadata.IsValid)
					record.UpdateMetadata(state.metadata);

				record.seen = true;
				record.releaseRemaining = state.releaseRemaining;
				record.Sample(currentTime, state.consumers, state.isLoaded);
			}

			foreach (var record in _records.Values)
			{
				if (!record.seen)
				{
					record.releaseRemaining = null;
					record.Close(currentTime, false);
				}
			}

			Prune(currentTime);
			_displayDirty = true;
		}

		private void CollectCurrentState(IAssetContainerState state)
		{
			var consumers = Math.Max(0, state.UsageCount);
			var containerKey = state.Key?.ToString();

			if (state.IsLoaded)
			{
				if (state.Asset is UnityObject asset && asset != null)
				{
					var rawKey = containerKey ?? asset.GetInstanceID().ToString();
					var metadata = GetAssetMetadata(asset, rawKey);
					_keyMetadata[rawKey] = metadata;

					// Ключ записи — всегда ключ контейнера: он стабилен между фазами загрузки и перезагрузками,
					// в отличие от ключей по assetPath/instanceId (для бандловых и суб-ассетов они меняются → дубли)
					var key = containerKey ?? GetAssetKey(asset, metadata);
					AddCurrentState(key, consumers, true, metadata, state.ReleaseRemainingSeconds);
					return;
				}

				if (CollectAssetCollection(state.Asset, consumers))
					return;
			}

			if (containerKey == null)
				return;

			var keyMetadata = GetKeyMetadata(containerKey);
			AddCurrentState(containerKey, consumers, state.IsLoaded, keyMetadata, state.ReleaseRemainingSeconds);
		}

		private bool CollectAssetCollection(object value, int consumers)
		{
			if (value is string)
				return false;

			var collected = false;
			if (value is IList assetList)
			{
				for (var i = 0; i < assetList.Count; i++)
					collected |= CollectCollectionAsset(assetList[i], consumers);

				return collected;
			}

			if (value is not IEnumerable assets)
				return false;

			foreach (var item in assets)
				collected |= CollectCollectionAsset(item, consumers);

			return collected;
		}

		private bool CollectCollectionAsset(object value, int consumers)
		{
			if (value is not UnityObject asset || asset == null)
				return false;

			var metadata = GetAssetMetadata(asset);
			var key = GetAssetKey(asset, metadata);
			AddCurrentState(key, consumers, true, metadata);
			return true;
		}

		private static string GetAssetKey(UnityObject asset, AssetMetadata metadata)
		{
			if (metadata.assetPath.IsNullOrEmpty())
				return $"{asset.name} [{asset.GetInstanceID()}]";

			// Объект не из AssetDatabase (бандловый плеймод): instanceId меняется между загрузками — ключим по пути
			if (!AssetDatabase.Contains(asset) || AssetDatabase.IsMainAsset(asset))
				return metadata.assetPath;

			return $"{metadata.assetPath}#{asset.GetInstanceID()}";
		}

		private void AddCurrentState(string key, int consumers, bool isLoaded, AssetMetadata metadata,
			double? releaseRemaining = null)
		{
			if (!_currentStates.TryGetValue(key, out var state))
				state = new CurrentAssetState(isLoaded, metadata);

			state.consumers += consumers;
			state.isLoaded |= isLoaded;
			if (metadata.IsValid)
				state.metadata = metadata;

			// При слиянии нескольких контейнеров в одну запись показываем максимальный остаток до выгрузки
			if (releaseRemaining.HasValue)
				state.releaseRemaining = state.releaseRemaining.HasValue
					? Math.Max(state.releaseRemaining.Value, releaseRemaining.Value)
					: releaseRemaining;

			_currentStates[key] = state;
		}

		private AssetMetadata GetAssetMetadata(UnityObject asset, string fallbackKey = null)
		{
			var instanceId = asset.GetInstanceID();
			if (_assetMetadata.TryGetValue(instanceId, out var metadata))
				return metadata;

			fallbackKey ??= instanceId.ToString();
			metadata = ResolveAssetMetadata(asset, fallbackKey);
			_assetMetadata[instanceId] = metadata;
			return metadata;
		}

		private AssetMetadata GetKeyMetadata(string key)
		{
			if (_keyMetadata.TryGetValue(key, out var metadata))
				return metadata;

			metadata = ResolveAssetMetadata(null, key);
			_keyMetadata[key] = metadata;
			return metadata;
		}

		private static AssetMetadata ResolveAssetMetadata(UnityObject asset, string fallbackKey)
		{
			var assetPath = asset != null ? AssetDatabase.GetAssetPath(asset) : null;
			if (assetPath.IsNullOrEmpty())
				assetPath = ResolveAssetName(fallbackKey);

			if (assetPath == fallbackKey && fallbackKey is {Length: 32})
			{
				var guidPath = AssetDatabase.GUIDToAssetPath(fallbackKey);
				if (!guidPath.IsNullOrEmpty())
					assetPath = guidPath;
			}

			var displayName = asset != null ? asset.name : null;
			if (displayName.IsNullOrEmpty() && !assetPath.IsNullOrEmpty())
				displayName = Path.GetFileNameWithoutExtension(assetPath);
			if (displayName.IsNullOrEmpty())
				displayName = fallbackKey;

			var assetType = asset != null ? asset.GetType() : null;
			if (assetType == null && assetPath is {Length: > 0})
				assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);

			var typeName = ResolveAssetTypeName(assetType, assetPath);

			var groupName = ResolveGroupName(assetPath);
			var assetIcon = !assetPath.IsNullOrEmpty()
				? AssetDatabase.GetCachedIcon(assetPath)
				: asset != null
					? AssetPreview.GetMiniThumbnail(asset)
					: null;
			return new AssetMetadata(assetPath, displayName, typeName, groupName, assetIcon);
		}

		private static string ResolveAssetTypeName(Type assetType, string assetPath)
		{
			if (assetPath is {Length: > 0} &&
				assetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
				return "Prefab";

			if (assetType == null)
				return UNKNOWN_TYPE;

			if (typeof(ScriptableObject).IsAssignableFrom(assetType))
				return "ScriptableObject";

			if (typeof(AudioClip).IsAssignableFrom(assetType) ||
				assetType.Namespace?.StartsWith("UnityEngine.Audio", StringComparison.Ordinal) == true)
				return "Audio";

			if (typeof(Sprite).IsAssignableFrom(assetType))
				return "Sprite";

			return ObjectNames.NicifyVariableName(assetType.Name);
		}

		private static string ResolveGroupName(string assetPath)
		{
			if (assetPath.IsNullOrEmpty())
				return UNKNOWN_GROUP;

			if (assetPath.StartsWith("Assets/Resources/", StringComparison.OrdinalIgnoreCase) ||
				assetPath.IndexOf("/Resources/", StringComparison.OrdinalIgnoreCase) >= 0)
				return "Resources";

			var groupName = AssetManagementEditorUtility.GetAddressableGroupName(assetPath);
			return groupName.IsNullOrEmpty() ? "Non-Addressable" : groupName;
		}

		private void Prune(double currentTime)
		{
			var cutoff = currentTime - MAX_HISTORY_SECONDS;
			_recordsToRemove.Clear();

			foreach (var (key, record) in _records)
			{
				record.Prune(cutoff);
				if (!record.IsVisible)
					_recordsToRemove.Add(key);
			}

			foreach (var key in _recordsToRemove)
			{
				if (_records.Remove(key, out var record))
					record.Dispose();
			}
		}

		private void OnGUI()
		{
			EnsureData();
			EnsureStyles();
			DrawToolbar();

			var currentTime = EditorApplication.timeSinceStartup;
			if (_displayDirty)
			{
				BuildDisplayRows(currentTime);
				_displayDirty = false;
			}

			if (_displayRows.Count == 0)
			{
				DrawEmptyState();
				return;
			}

			var viewportRect = GUILayoutUtility.GetRect(
				MIN_WINDOW_WIDTH,
				100000f,
				100f,
				100000f,
				VIEWPORT_OPTIONS);

			var contentWidth = Mathf.Max(viewportRect.width - 16f, MIN_WINDOW_WIDTH);
			var contentHeight = HEADER_HEIGHT + _displayRows.Count * ROW_HEIGHT;
			var contentRect = new Rect(0f, 0f, contentWidth, contentHeight);

			_scroll = GUI.BeginScrollView(viewportRect, _scroll, contentRect);
			_hoveredTooltip = null;
			DrawTimeline(contentRect, currentTime);
			GUI.EndScrollView();

			DrawHoverTooltip(viewportRect);
		}

		// Кастомная отрисовка тултипа: встроенные тултипы IMGUI не переживают частый Repaint окна
		private void DrawHoverTooltip(Rect viewportRect)
		{
			if (_hoveredTooltip == null || Event.current.type != EventType.Repaint)
				return;

			// Курсор вне окна: mousePosition в событиях остаётся протухшим, ховер фантомный
			if (mouseOverWindow != this)
				return;

			_tooltipContent.text = _hoveredTooltip;
			_tooltipContent.tooltip = null;
			_tooltipContent.image = null;

			var size = _tooltipStyle.CalcSize(_tooltipContent);
			var rowTop = _hoveredRowRect.y - _scroll.y + viewportRect.y;
			var rowBottom = rowTop + _hoveredRowRect.height;

			var x = Mathf.Clamp(
				Event.current.mousePosition.x + 16f,
				viewportRect.x + 4f,
				Mathf.Max(viewportRect.x + 4f, viewportRect.xMax - size.x - 4f));
			var y = rowBottom + 4f;
			if (y + size.y > viewportRect.yMax - 4f)
				y = rowTop - size.y - 4f;
			y = Mathf.Max(y, viewportRect.y + 4f);

			var tooltipRect = new Rect(x, y, size.x, size.y);
			var borderRect = new Rect(
				tooltipRect.x - 1f,
				tooltipRect.y - 1f,
				tooltipRect.width + 2f,
				tooltipRect.height + 2f);
			EditorGUI.DrawRect(borderRect, TOOLTIP_BORDER_COLOR);
			EditorGUI.DrawRect(tooltipRect, TOOLTIP_BACKGROUND_COLOR);
			GUI.Label(tooltipRect, _tooltipContent, _tooltipStyle);
		}

		private void DrawToolbar()
		{
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			GUILayout.Label("Grouping", _toolbarLabelStyle);

			DrawGroupingPopup();

			GUILayout.Space(8f);
			GUILayout.Label("Sorting", _toolbarLabelStyle);

			DrawSortingPopup();

			GUILayout.FlexibleSpace();
			GUILayout.Label("History", _toolbarLabelStyle);

			var selectedIndex = Array.IndexOf(HISTORY_VALUES, _historySeconds);
			selectedIndex = Mathf.Max(0, selectedIndex);
			selectedIndex = EditorGUILayout.Popup(
				selectedIndex,
				HISTORY_LABELS,
				EditorStyles.toolbarPopup,
				HISTORY_POPUP_OPTIONS);
			var historySeconds = HISTORY_VALUES[selectedIndex];
			if (_historySeconds != historySeconds)
			{
				_historySeconds = historySeconds;
				_displayDirty = true;
			}

			EditorGUILayout.EndHorizontal();
		}

		private void DrawGroupingPopup()
		{
			var content = GetScratchContent(GetGroupingLabel());
			var rect = GUILayoutUtility.GetRect(
				content,
				EditorStyles.toolbarPopup,
				GROUPING_POPUP_OPTIONS);

			if (!EditorGUI.DropdownButton(rect, content, FocusType.Passive, EditorStyles.toolbarPopup))
				return;

			var menu = new GenericMenu();
			menu.AddItem(
				GROUP_CONTENT,
				(_grouping & GroupingMode.Group) != 0,
				() => ToggleGrouping(GroupingMode.Group));
			menu.AddItem(
				TYPE_CONTENT,
				(_grouping & GroupingMode.Type) != 0,
				() => ToggleGrouping(GroupingMode.Type));
			menu.DropDown(rect);
		}

		private string GetGroupingLabel()
		{
			return _grouping switch
			{
				GroupingMode.None => "None",
				GroupingMode.Group => "Group",
				GroupingMode.Type => "Type",
				_ => "Group, Type"
			};
		}

		private void ToggleGrouping(GroupingMode mode)
		{
			_grouping ^= mode;
			_displayDirty = true;
			Repaint();
		}

		private void DrawSortingPopup()
		{
			var content = GetScratchContent(GetSortingLabel());
			var rect = GUILayoutUtility.GetRect(
				content,
				EditorStyles.toolbarPopup,
				SORTING_POPUP_OPTIONS);

			if (!EditorGUI.DropdownButton(rect, content, FocusType.Passive, EditorStyles.toolbarPopup))
				return;

			var menu = new GenericMenu();
			menu.AddItem(
				TIME_CONTENT,
				(_sorting & SortingMode.Time) != 0,
				() => ToggleSorting(SortingMode.Time));
			menu.AddItem(
				NAME_CONTENT,
				(_sorting & SortingMode.Name) != 0,
				() => ToggleSorting(SortingMode.Name));
			menu.DropDown(rect);
		}

		private string GetSortingLabel()
		{
			return _sorting switch
			{
				SortingMode.None => "None",
				SortingMode.Time => "Time",
				SortingMode.Name => "Name",
				_ => "Time, Name"
			};
		}

		private void ToggleSorting(SortingMode mode)
		{
			_sorting ^= mode;
			_displayDirty = true;
			Repaint();
		}

		private static void DrawEmptyState()
		{
			if (!EditorApplication.isPlaying)
			{
				FusumityEditorGUILayout.DrawWarning("Play Mode Only", iconSize: 60);
				return;
			}

			if (!AssetLoader.IsInitialized)
			{
				FusumityEditorGUILayout.DrawWarning("AssetLoader is not initialized", iconSize: 60);
				return;
			}

			FusumityEditorGUILayout.DrawWarning(
				"No assets are currently in use",
				iconSize: 60,
				icon: EditorGUIUtility.IconContent("console.infoicon"));
		}

		private void DrawTimeline(Rect contentRect, double currentTime)
		{
			// Окно могли сузить — держим колонку в допустимых пределах
			_labelWidth = Mathf.Clamp(
				_labelWidth,
				MIN_LABEL_WIDTH,
				Mathf.Max(MIN_LABEL_WIDTH, contentRect.width - MIN_TIMELINE_WIDTH));

			var timelineRect = new Rect(
				_labelWidth,
				0f,
				Mathf.Max(1f, contentRect.width - _labelWidth),
				contentRect.height);

			EditorGUI.DrawRect(new Rect(0f, 0f, contentRect.width, HEADER_HEIGHT), HEADER_COLOR);
			EditorGUI.DrawRect(new Rect(_labelWidth, 0f, 1f, contentRect.height), GRID_COLOR * 1.5f);

			HandleLabelSplitter(contentRect);
			DrawRuler(timelineRect);

			for (var i = 0; i < _displayRows.Count; i++)
			{
				var displayRow = _displayRows[i];
				var rowRect = new Rect(0f, HEADER_HEIGHT + i * ROW_HEIGHT, contentRect.width, ROW_HEIGHT);

				switch (displayRow.kind)
				{
					case DisplayRowKind.Group:
						DrawHeaderRow(displayRow, rowRect, timelineRect, true);
						break;
					case DisplayRowKind.Type:
						DrawHeaderRow(displayRow, rowRect, timelineRect, false);
						break;
					case DisplayRowKind.Asset:
						DrawAssetRow(displayRow.record, rowRect, timelineRect, currentTime, i);
						break;
				}
			}
		}

		// Перетаскиваемый разделитель между колонкой имён и таймлайном
		private void HandleLabelSplitter(Rect contentRect)
		{
			var splitterRect = new Rect(
				_labelWidth - SPLITTER_HALF_WIDTH,
				0f,
				SPLITTER_HALF_WIDTH * 2f,
				contentRect.height);
			EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeHorizontal);

			var controlId = GUIUtility.GetControlID(FocusType.Passive);
			var currentEvent = Event.current;

			switch (currentEvent.GetTypeForControl(controlId))
			{
				case EventType.MouseDown when currentEvent.button == 0 &&
					splitterRect.Contains(currentEvent.mousePosition):
					GUIUtility.hotControl = controlId;
					currentEvent.Use();
					break;

				case EventType.MouseDrag when GUIUtility.hotControl == controlId:
					_labelWidth = Mathf.Clamp(
						currentEvent.mousePosition.x,
						MIN_LABEL_WIDTH,
						Mathf.Max(MIN_LABEL_WIDTH, contentRect.width - MIN_TIMELINE_WIDTH));
					currentEvent.Use();
					Repaint();
					break;

				case EventType.MouseUp when GUIUtility.hotControl == controlId:
					GUIUtility.hotControl = 0;
					currentEvent.Use();
					break;
			}
		}

		private void DrawHeaderRow(DisplayRow displayRow, Rect rowRect, Rect timelineRect, bool isGroup)
		{
			EditorGUI.DrawRect(rowRect, isGroup ? GROUP_ROW_COLOR : TYPE_ROW_COLOR);
			EditorGUIUtility.AddCursorRect(rowRect, MouseCursor.Link);

			var collapsed = isGroup
				? _collapsedGroups.Contains(displayRow.key)
				: _collapsedTypes.Contains(displayRow.key);
			var isNestedGroup = isGroup && (_grouping & GroupingMode.Type) != 0;
			var indent = isNestedGroup ? 24f : 8f;
			var labelRect = new Rect(
				indent,
				rowRect.y + 2f,
				_labelWidth - indent - 8f,
				rowRect.height - 4f);
			var headerStyle = isNestedGroup ? _typeHeaderStyle : _groupHeaderStyle;

			GUI.Label(new Rect(labelRect.x, labelRect.y, 18f, labelRect.height), collapsed ? "▶" : "▼", headerStyle);
			labelRect.xMin += 20f;
			GUI.Label(labelRect, displayRow.name, headerStyle);

			var summaryRect = new Rect(
				timelineRect.x + 17.5f,
				rowRect.y + 2f,
				timelineRect.width - 24f,
				rowRect.height - 4f);
			GUI.Label(
				summaryRect,
				displayRow.summary,
				_summaryStyle);

			if (Event.current is {type: EventType.MouseDown, button: 0} &&
				rowRect.Contains(Event.current.mousePosition))
			{
				var collapsedSet = isGroup ? _collapsedGroups : _collapsedTypes;
				if (!collapsedSet.Add(displayRow.key))
					collapsedSet.Remove(displayRow.key);

				_displayDirty = true;
				Event.current.Use();
				Repaint();
			}
		}

		private void DrawAssetRow(AssetRecord record, Rect rowRect, Rect timelineRect, double currentTime, int rowIndex)
		{
			EditorGUI.DrawRect(rowRect, rowIndex % 2 == 0 ? EVEN_ROW_COLOR : ODD_ROW_COLOR);
			var isHovered = rowRect.Contains(Event.current.mousePosition);
			if (isHovered)
				EditorGUI.DrawRect(rowRect, HOVER_COLOR);

			DrawRecordLabel(record, rowRect);
			DrawSegments(record, rowRect, timelineRect, currentTime);
			EditorGUIUtility.AddCursorRect(rowRect, MouseCursor.Link);

			if (isHovered)
			{
				_hoveredTooltip = record.BuildTooltip(currentTime, _historySeconds);
				_hoveredRowRect = rowRect;
			}

			if (isHovered && Event.current is {type: EventType.MouseDown, button: 0})
			{
				PingAsset(record);
				Event.current.Use();
			}
		}

		private void DrawRuler(Rect timelineRect)
		{
			const string TITLE = "Timeline";
			GUI.Label(new Rect(8f, 2f, _labelWidth - 16f, HEADER_HEIGHT - 4f), TITLE, EditorStyles.boldLabel);

			var rulerStep = _historySeconds > 30 ? 10 : 5;
			for (var secondsAgo = 0; secondsAgo <= _historySeconds; secondsAgo += rulerStep)
			{
				var normalized = (float) secondsAgo / _historySeconds;
				var x = Mathf.Lerp(timelineRect.xMax, timelineRect.xMin, normalized);

				EditorGUI.DrawRect(new Rect(x, 0f, 1f, timelineRect.height), GRID_COLOR);

				var label = RULER_LABELS[secondsAgo / 5];
				var labelContent = GetScratchContent(label);
				var labelWidth = _rulerLabelStyle.CalcSize(labelContent).x + 10f;
				var labelX = Mathf.Clamp(
					x - labelWidth * 0.5f,
					timelineRect.x + 2f,
					timelineRect.xMax - labelWidth - 2f);
				var labelRect = new Rect(labelX, 3f, labelWidth, HEADER_HEIGHT - 6f);
				EditorGUI.DrawRect(labelRect, RULER_LABEL_BACKGROUND_COLOR);
				GUI.Label(labelRect, label, _rulerLabelStyle);
			}
		}

		private void DrawRecordLabel(AssetRecord record, Rect rowRect)
		{
			var badgeWidth = 0f;
			var badgeLabelWidth = 0f;
			var badgeValueWidth = 0f;

			string badgeValue = null;
			if (record.IsActive)
			{
				badgeValue = record.ConsumersText;
				badgeLabelWidth = _countBadgeStyle.CalcSize(USAGES_LABEL_CONTENT).x;
				badgeValueWidth = _countBadgeValueStyle.CalcSize(GetScratchContent(badgeValue)).x;
				badgeWidth =
					USAGES_BADGE_LEFT_PADDING +
					badgeLabelWidth +
					USAGES_BADGE_VALUE_GAP +
					badgeValueWidth +
					USAGES_BADGE_RIGHT_PADDING;
			}

			var iconRect = new Rect(38f, rowRect.y + 4f, 16f, 16f);
			var labelRect = new Rect(
				60f,
				rowRect.y + 2f,
				_labelWidth - 68f - badgeWidth,
				rowRect.height - 4f);

			if (record.assetIcon != null)
				GUI.DrawTexture(iconRect, record.assetIcon, ScaleMode.ScaleToFit, true);

			GUI.Label(labelRect, record.name, EditorStyles.label);

			if (badgeValue == null)
				return;

			var badgeRect = new Rect(
				_labelWidth - badgeWidth - 6f,
				rowRect.y + 3f,
				badgeWidth,
				rowRect.height - 6f);

			var originalColor = GUI.color;
			GUI.color = new Color(0f, 0f, 0f, 0.666f);
			GUI.Box(badgeRect, GUIContent.none, _countBadgeCardStyle);
			GUI.color = originalColor;

			var contentX = badgeRect.x + USAGES_BADGE_LEFT_PADDING;
			GUI.Label(
				new Rect(contentX, badgeRect.y, badgeLabelWidth, badgeRect.height),
				USAGES_LABEL,
				_countBadgeStyle);
			GUI.Label(
				new Rect(
					contentX + badgeLabelWidth + USAGES_BADGE_VALUE_GAP,
					badgeRect.y,
					badgeValueWidth,
					badgeRect.height),
				badgeValue,
				_countBadgeValueStyle);
		}

		private static void PingAsset(AssetRecord record)
		{
			var assetPath = record.assetPath;
			if (assetPath.IsNullOrEmpty() && record.key is {Length: 32})
				assetPath = AssetDatabase.GUIDToAssetPath(record.key);
			if (assetPath.IsNullOrEmpty() && record.key.StartsWith("Assets/", StringComparison.Ordinal))
				assetPath = record.key;
			if (assetPath.IsNullOrEmpty())
				return;

			var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
			UnityObject target = null;

			foreach (var asset in assets)
			{
				if (asset == null)
					continue;

				target ??= asset;
				if (asset.name == record.name &&
					ResolveAssetTypeName(asset.GetType(), assetPath) == record.typeName)
				{
					target = asset;
					break;
				}
			}

			target ??= AssetDatabase.LoadMainAssetAtPath(assetPath);
			if (target != null)
				EditorGUIUtility.PingObject(target);
		}

		private void DrawSegments(AssetRecord record, Rect rowRect, Rect timelineRect, double currentTime)
		{
			var cutoff = currentTime - _historySeconds;
			var lineY = rowRect.y + rowRect.height * 0.5f - 4f;

			// Периоды ожидания выгрузки (загружен, но без usages) — тонкие приглушённые полосы, история сохраняется
			DrawSegmentList(record.pendingSegments, record, timelineRect, currentTime, cutoff, lineY + 2f, 4f, true);
			DrawSegmentList(record.segments, record, timelineRect, currentTime, cutoff, lineY, 8f, false);
		}

		private void DrawSegmentList(List<UsageSegment> segments, AssetRecord record, Rect timelineRect,
			double currentTime, double cutoff, float y, float height, bool isPendingBar)
		{
			foreach (var segment in segments)
			{
				var endTime = segment.IsActive ? currentTime : segment.endTime;
				if (endTime < cutoff)
					continue;

				var startX = TimeToPosition(Math.Max(segment.startTime, cutoff), cutoff, timelineRect);
				var endX = TimeToPosition(endTime, cutoff, timelineRect);
				var width = Mathf.Max(2f, endX - startX);

				var color = isPendingBar
					? PENDING_COLOR
					: segment.IsActive
						? record.isLoaded ? ACTIVE_COLOR : LOADING_COLOR
						: RELEASED_COLOR;

				EditorGUI.DrawRect(new Rect(startX, y, width, height), color);
			}
		}

		private float TimeToPosition(double time, double cutoff, Rect timelineRect)
		{
			var normalized = Mathf.Clamp01((float) ((time - cutoff) / _historySeconds));
			return timelineRect.x + timelineRect.width * normalized;
		}

		private void BuildDisplayRows(double currentTime)
		{
			_orderedRecords.Clear();
			_displayRows.Clear();

			foreach (var record in _records.Values)
			{
				if (record.IsVisibleSince(currentTime - _historySeconds))
					_orderedRecords.Add(record);
			}

			_orderedRecords.Sort(_recordComparison);

			if ((_grouping & GroupingMode.Type) != 0)
			{
				BuildTypeRows(0, _orderedRecords.Count);
				return;
			}

			if ((_grouping & GroupingMode.Group) != 0)
			{
				BuildGroupRows(null, 0, _orderedRecords.Count);
				return;
			}

			AddAssetRows(0, _orderedRecords.Count);
		}

		private void BuildTypeRows(int start, int end)
		{
			var typeStart = start;
			while (typeStart < end)
			{
				var typeName = _orderedRecords[typeStart].typeName;
				var typeEnd = typeStart + 1;
				while (typeEnd < end &&
					_orderedRecords[typeEnd].typeName == typeName)
					typeEnd++;

				_displayRows.Add(BuildHeaderRow(
					DisplayRowKind.Type,
					typeName,
					typeName,
					typeStart,
					typeEnd));

				if (!_collapsedTypes.Contains(typeName))
				{
					if ((_grouping & GroupingMode.Group) != 0)
						BuildGroupRows(typeName, typeStart, typeEnd);
					else
						AddAssetRows(typeStart, typeEnd);
				}

				typeStart = typeEnd;
			}
		}

		private void BuildGroupRows(string typeName, int start, int end)
		{
			var groupStart = start;
			while (groupStart < end)
			{
				var groupName = _orderedRecords[groupStart].groupName;
				var groupEnd = groupStart + 1;
				while (groupEnd < end &&
					_orderedRecords[groupEnd].groupName == groupName)
					groupEnd++;

				var groupKey = typeName == null
					? groupName
					: _orderedRecords[groupStart].groupTypeKey;
				_displayRows.Add(BuildHeaderRow(
					DisplayRowKind.Group,
					groupKey,
					groupName,
					groupStart,
					groupEnd));

				if (!_collapsedGroups.Contains(groupKey))
					AddAssetRows(groupStart, groupEnd);

				groupStart = groupEnd;
			}
		}

		private void AddAssetRows(int start, int end)
		{
			for (var i = start; i < end; i++)
				_displayRows.Add(DisplayRow.Asset(_orderedRecords[i]));
		}

		private DisplayRow BuildHeaderRow(DisplayRowKind kind, string key, string name, int start, int end)
		{
			var activeCount = 0;
			var usages = 0;
			for (var i = start; i < end; i++)
			{
				var record = _orderedRecords[i];
				if (record.IsActive)
					activeCount++;
				usages += record.consumers;
			}

			var assetCount = end - start;
			var cacheKey = new HeaderCacheKey(kind, key);
			if (!_headerSummaries.TryGetValue(cacheKey, out var summary) ||
				!summary.Matches(assetCount, activeCount, usages))
			{
				summary = new HeaderSummary(
					assetCount,
					activeCount,
					usages,
					string.Format(HEADER_SUMMARY_FORMAT, assetCount, activeCount, usages));
				_headerSummaries[cacheKey] = summary;
			}

			return DisplayRow.Header(kind, key, name, summary.text);
		}

		private int CompareRecords(AssetRecord a, AssetRecord b)
		{
			if ((_grouping & GroupingMode.Type) != 0)
			{
				var typeComparison = string.Compare(a.typeName, b.typeName, StringComparison.OrdinalIgnoreCase);
				if (typeComparison != 0)
					return typeComparison;
			}

			if ((_grouping & GroupingMode.Group) != 0)
			{
				var groupComparison = string.Compare(a.groupName, b.groupName, StringComparison.OrdinalIgnoreCase);
				if (groupComparison != 0)
					return groupComparison;
			}

			if ((_sorting & SortingMode.Time) != 0)
			{
				var timeComparison = b.LastUsageTime.CompareTo(a.LastUsageTime);
				if (timeComparison != 0)
					return timeComparison;
			}

			if ((_sorting & SortingMode.Name) != 0)
			{
				var nameComparison = string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase);
				if (nameComparison != 0)
					return nameComparison;
			}

			return string.Compare(a.key, b.key, StringComparison.Ordinal);
		}

		private void EnsureData()
		{
			_records ??= DictionaryPool<string, AssetRecord>.Get();
			_currentStates ??= DictionaryPool<string, CurrentAssetState>.Get();
			_containerStates ??= ListPool<IAssetContainerState>.Get();
			_assetMetadata ??= DictionaryPool<int, AssetMetadata>.Get();
			_keyMetadata ??= DictionaryPool<string, AssetMetadata>.Get();
			_headerSummaries ??= DictionaryPool<HeaderCacheKey, HeaderSummary>.Get();
			_orderedRecords ??= ListPool<AssetRecord>.Get();
			_displayRows ??= ListPool<DisplayRow>.Get();
			_recordsToRemove ??= ListPool<string>.Get();
			_collapsedGroups ??= HashSetPool<string>.Get();
			_collapsedTypes ??= HashSetPool<string>.Get();
			_recordComparison ??= CompareRecords;
		}

		private void ReleaseData()
		{
			if (_records != null)
			{
				foreach (var record in _records.Values)
					record.Dispose();
			}

			StaticObjectPoolUtility.ReleaseAndSetNullSafe(ref _records);
			StaticObjectPoolUtility.ReleaseAndSetNullSafe(ref _currentStates);
			StaticObjectPoolUtility.ReleaseAndSetNullSafe(ref _containerStates);
			StaticObjectPoolUtility.ReleaseAndSetNullSafe(ref _assetMetadata);
			StaticObjectPoolUtility.ReleaseAndSetNullSafe(ref _keyMetadata);
			StaticObjectPoolUtility.ReleaseAndSetNullSafe(ref _headerSummaries);
			StaticObjectPoolUtility.ReleaseAndSetNullSafe(ref _orderedRecords);
			StaticObjectPoolUtility.ReleaseAndSetNullSafe(ref _displayRows);
			StaticObjectPoolUtility.ReleaseAndSetNullSafe(ref _recordsToRemove);
			StaticObjectPoolUtility.ReleaseAndSetNullSafe(ref _collapsedGroups);
			StaticObjectPoolUtility.ReleaseAndSetNullSafe(ref _collapsedTypes);

			_recordComparison = null;
			_displayDirty = true;
		}

		private void EnsureHistoryValue()
		{
			if (Array.IndexOf(HISTORY_VALUES, _historySeconds) < 0)
				_historySeconds = DEFAULT_HISTORY_SECONDS;
		}

		private void EnsureGroupingMode()
		{
			_grouping &= GroupingMode.All;
		}

		private void EnsureSortingMode()
		{
			_sorting &= SortingMode.All;
		}

		private void EnsureStyles()
		{
			_rulerLabelStyle ??= new GUIStyle(EditorStyles.miniLabel)
			{
				alignment = TextAnchor.MiddleCenter
			};

			_countBadgeStyle ??= new GUIStyle(EditorStyles.miniLabel)
			{
				alignment = TextAnchor.MiddleLeft,
				fontSize = 9,
				normal = {textColor = Color.gray}
			};

			_countBadgeValueStyle ??= new GUIStyle(EditorStyles.label)
			{
				alignment = TextAnchor.MiddleLeft
			};

			_countBadgeCardStyle ??= new GUIStyle(FusumityEditorGUILayout.CardStyle)
			{
				margin = new RectOffset(),
				padding = new RectOffset()
			};

			_groupHeaderStyle ??= new GUIStyle(EditorStyles.boldLabel)
			{
				fontSize = 12,
				alignment = TextAnchor.MiddleLeft
			};

			_typeHeaderStyle ??= new GUIStyle(EditorStyles.miniBoldLabel)
			{
				alignment = TextAnchor.MiddleLeft
			};

			_summaryStyle ??= new GUIStyle(EditorStyles.miniLabel)
			{
				alignment = TextAnchor.MiddleRight,
				normal = {textColor = HEADER_SUMMARY_TEXT_COLOR}
			};

			_toolbarLabelStyle ??= new GUIStyle(EditorStyles.miniLabel)
			{
				alignment = TextAnchor.MiddleLeft,
				contentOffset = new Vector2(0f, 1.5f),
				normal = {textColor = Color.gray}
			};

			_scratchContent ??= new GUIContent();
			_tooltipContent ??= new GUIContent();

			_tooltipStyle ??= new GUIStyle(EditorStyles.label)
			{
				richText = true,
				wordWrap = false,
				fontSize = 11,
				alignment = TextAnchor.UpperLeft,
				padding = new RectOffset(10, 10, 8, 8)
			};
		}

		private GUIContent GetScratchContent(string text, string tooltip = null)
		{
			_scratchContent.text = text;
			_scratchContent.tooltip = tooltip;
			_scratchContent.image = null;
			return _scratchContent;
		}

		private static string ResolveAssetName(string key)
		{
			return AssetLoader.ResolveAssetPath(key);
		}

		private struct CurrentAssetState
		{
			public int consumers;
			public bool isLoaded;
			public AssetMetadata metadata;
			public double? releaseRemaining;

			public CurrentAssetState(bool isLoaded, AssetMetadata metadata)
			{
				consumers = 0;
				this.isLoaded = isLoaded;
				this.metadata = metadata;
				releaseRemaining = null;
			}
		}

		private sealed class AssetRecord
		{
			public readonly string key;
			public readonly List<UsageSegment> segments;

			// Периоды ожидания выгрузки (usages == 0, но контейнер жив) — отдельная история, рисуется приглушённо
			public readonly List<UsageSegment> pendingSegments;

			public string name;
			public string assetPath;
			public string typeName;
			public string groupName;
			public string groupTypeKey;
			public Texture assetIcon;
			public int consumers;
			public bool isLoaded;
			public bool seen;
			public double? releaseRemaining;

			public bool IsActive => consumers > 0;
			public bool IsPending => HasActivePendingSegment;
			public bool IsVisible => IsActive || segments.Count > 0 || pendingSegments.Count > 0;
			public string ConsumersText
			{
				get
				{
					if (_consumersTextValue == consumers)
						return _consumersText;

					_consumersTextValue = consumers;
					_consumersText = consumers.ToString();
					return _consumersText;
				}
			}

			public double LastUsageTime =>
				segments.Count > 0
					? segments[segments.Count - 1].startTime
					: pendingSegments.Count > 0
						? pendingSegments[pendingSegments.Count - 1].startTime
						: double.MinValue;

			private int _consumersTextValue = int.MinValue;
			private string _consumersText;

			public AssetRecord(string key, AssetMetadata metadata)
			{
				this.key = key;
				segments = ListPool<UsageSegment>.Get();
				pendingSegments = ListPool<UsageSegment>.Get();
				UpdateMetadata(metadata);
			}

			public void Dispose()
			{
				ListPool<UsageSegment>.Release(segments);
				ListPool<UsageSegment>.Release(pendingSegments);
			}

			public void UpdateMetadata(AssetMetadata metadata)
			{
				if (!metadata.IsValid)
					return;

				var rebuildGroupTypeKey = groupName != metadata.groupName || typeName != metadata.typeName;
				name = metadata.displayName;
				assetPath = metadata.assetPath;
				typeName = metadata.typeName;
				groupName = metadata.groupName;
				assetIcon = metadata.assetIcon;

				if (rebuildGroupTypeKey)
					groupTypeKey = $"{groupName}\n{typeName}";
			}

			public void Sample(double currentTime, int currentConsumers, bool currentlyLoaded)
			{
				consumers = currentConsumers;
				isLoaded = currentlyLoaded;

				if (currentConsumers <= 0)
				{
					Close(currentTime, currentlyLoaded);
					return;
				}

				ClosePendingSegment(currentTime);

				if (!HasActiveSegment)
					segments.Add(new UsageSegment(currentTime, currentConsumers));
				else
					UpdateActiveSegment(currentConsumers);
			}

			public void Close(double currentTime, bool currentlyLoaded)
			{
				consumers = 0;
				isLoaded = currentlyLoaded;

				// Контейнер всё ещё загружен без usages — открываем период ожидания выгрузки (release pending)
				if (currentlyLoaded)
				{
					if (!HasActivePendingSegment)
						pendingSegments.Add(new UsageSegment(currentTime, 0));
				}
				else
					ClosePendingSegment(currentTime);

				if (!HasActiveSegment)
					return;

				var index = segments.Count - 1;
				var segment = segments[index];
				segment.endTime = currentTime;
				segments[index] = segment;
			}

			public void Prune(double cutoff)
			{
				PruneSegments(segments, cutoff);
				PruneSegments(pendingSegments, cutoff);
			}

			private static void PruneSegments(List<UsageSegment> list, double cutoff)
			{
				for (var i = list.Count - 1; i >= 0; i--)
				{
					var segment = list[i];
					if (!segment.IsActive && segment.endTime < cutoff)
						list.RemoveAt(i);
				}
			}

			public bool IsVisibleSince(double cutoff)
			{
				if (IsActive || IsPending)
					return true;

				foreach (var segment in segments)
				{
					if (segment.IsActive || segment.endTime >= cutoff)
						return true;
				}

				foreach (var segment in pendingSegments)
				{
					if (segment.IsActive || segment.endTime >= cutoff)
						return true;
				}

				return false;
			}

			public string BuildTooltip(double currentTime, int historySeconds)
			{
				var maxConsumers = consumers;
				var cutoff = currentTime - historySeconds;

				foreach (var segment in segments)
				{
					var endTime = segment.IsActive ? currentTime : segment.endTime;
					if (endTime < cutoff)
						continue;

					maxConsumers = Math.Max(maxConsumers, segment.maxConsumers);
				}

				var lifetime = 0d;
				if (segments.Count > 0)
				{
					var lastSegment = segments[segments.Count - 1];
					var lifetimeEnd = lastSegment.IsActive ? currentTime : lastSegment.endTime;
					lifetime = Math.Max(0d, lifetimeEnd - lastSegment.startTime);
				}

				var state = IsActive
					? isLoaded ? "Loaded" : "Loading"
					: IsPending ? "Releasing" : "Released";

				using (StringBuilderPool.Get(out var builder))
				{
					builder.Append(name)
					   .Append("\n\n" + TOOLTIP_LABEL_OPEN + "State:</color> ").Append(state);

					// Обратный отсчёт до реальной выгрузки контейнера
					if (!IsActive && IsPending && releaseRemaining.HasValue)
						builder.Append(" (").Append(releaseRemaining.Value.ToString("0.0")).Append(" s left)");

					builder.Append("\n" + TOOLTIP_LABEL_OPEN + "Lifetime:</color> ")
					   .Append(lifetime.ToString("0.00")).Append(" s")
					   .Append("\n" + TOOLTIP_LABEL_OPEN + "Usages:</color> ").Append(consumers)
					   .Append("\n" + TOOLTIP_LABEL_OPEN + "Peak usages:</color> ").Append(maxConsumers)
					   .Append("\n\n" + TOOLTIP_LABEL_OPEN + "Group:</color> ").Append(groupName)
					   .Append("\n" + TOOLTIP_LABEL_OPEN + "Type:</color> ").Append(typeName)
					   .Append("\n" + TOOLTIP_LABEL_OPEN + "Path:</color> ").Append(assetPath);
					return builder.ToString();
				}
			}

			private bool HasActiveSegment =>
				segments.Count > 0 && segments[segments.Count - 1].IsActive;

			private bool HasActivePendingSegment =>
				pendingSegments.Count > 0 && pendingSegments[pendingSegments.Count - 1].IsActive;

			private void ClosePendingSegment(double currentTime)
			{
				if (!HasActivePendingSegment)
					return;

				var index = pendingSegments.Count - 1;
				var segment = pendingSegments[index];
				segment.endTime = currentTime;
				pendingSegments[index] = segment;
			}

			private void UpdateActiveSegment(int currentConsumers)
			{
				var index = segments.Count - 1;
				var segment = segments[index];
				segment.maxConsumers = Math.Max(segment.maxConsumers, currentConsumers);
				segments[index] = segment;
			}
		}

		private readonly struct AssetMetadata
		{
			public readonly string assetPath;
			public readonly string displayName;
			public readonly string typeName;
			public readonly string groupName;
			public readonly Texture assetIcon;

			public bool IsValid => !displayName.IsNullOrEmpty();

			public AssetMetadata(
				string assetPath,
				string displayName,
				string typeName,
				string groupName,
				Texture assetIcon)
			{
				this.assetPath = assetPath;
				this.displayName = displayName;
				this.typeName = typeName.IsNullOrEmpty() ? UNKNOWN_TYPE : typeName;
				this.groupName = groupName.IsNullOrEmpty() ? UNKNOWN_GROUP : groupName;
				this.assetIcon = assetIcon;
			}
		}

		private enum DisplayRowKind
		{
			Group,
			Type,
			Asset
		}

		private readonly struct HeaderCacheKey : IEquatable<HeaderCacheKey>
		{
			private readonly DisplayRowKind _kind;
			private readonly string _key;

			public HeaderCacheKey(DisplayRowKind kind, string key)
			{
				_kind = kind;
				_key = key;
			}

			public bool Equals(HeaderCacheKey other) =>
				_kind == other._kind &&
				string.Equals(_key, other._key, StringComparison.Ordinal);

			public override bool Equals(object obj) =>
				obj is HeaderCacheKey other && Equals(other);

			public override int GetHashCode() =>
				((int) _kind * 397) ^ (_key?.GetHashCode() ?? 0);
		}

		private readonly struct HeaderSummary
		{
			public readonly string text;

			private readonly int _assetCount;
			private readonly int _activeCount;
			private readonly int _usages;

			public HeaderSummary(int assetCount, int activeCount, int usages, string text)
			{
				_assetCount = assetCount;
				_activeCount = activeCount;
				_usages = usages;
				this.text = text;
			}

			public bool Matches(int assetCount, int activeCount, int usages) =>
				_assetCount == assetCount &&
				_activeCount == activeCount &&
				_usages == usages;
		}

		[Flags]
		private enum GroupingMode
		{
			None = 0,
			Group = 1 << 0,
			Type = 1 << 1,
			All = Group | Type
		}

		[Flags]
		private enum SortingMode
		{
			None = 0,
			Time = 1 << 0,
			Name = 1 << 1,
			All = Time | Name
		}

		private readonly struct DisplayRow
		{
			public readonly DisplayRowKind kind;
			public readonly string key;
			public readonly string name;
			public readonly AssetRecord record;
			public readonly string summary;

			private DisplayRow(
				DisplayRowKind kind,
				string key,
				string name,
				AssetRecord record,
				string summary)
			{
				this.kind = kind;
				this.key = key;
				this.name = name;
				this.record = record;
				this.summary = summary;
			}

			public static DisplayRow Header(
				DisplayRowKind kind,
				string key,
				string name,
				string summary) =>
				new(kind, key, name, null, summary);

			public static DisplayRow Asset(AssetRecord record) =>
				new(DisplayRowKind.Asset, null, null, record, null);
		}

		private struct UsageSegment
		{
			public readonly double startTime;
			public double endTime;
			public int maxConsumers;

			public bool IsActive => endTime < 0d;

			public UsageSegment(double startTime, int consumers)
			{
				this.startTime = startTime;
				endTime = -1d;
				maxConsumers = consumers;
			}
		}
	}
}
