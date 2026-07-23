using Content.ScriptableObjects;
using Content.ScriptableObjects.Editor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Fusumity.Editor;
using Sapientia.Extensions;
using Sapientia.Pooling;
using Sapientia.Utility;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Content.Editor
{
	using UnityObject = UnityEngine.Object;

	public sealed class ContentUsedAuditWindow : OdinEditorWindow
	{
		internal const string WINDOW_NAME = "Content Used Audit";
		private const string NAME = "Used Audit";

		private const string AUDIT_DESCRIPTION =
			"Аудит считает используемыми конфиги, достижимые из singleton/database настроек, " +
			"prefab-ов, которые обходит Content Validation, и прямых вызовов ContentManager.\n\n" +
			"Runtime-ссылки из сервера, save-data и строк требуют ручной проверки";

		private static readonly Color DANGER_COLOR = new(0.90f, 0.40f, 0.40f, 1f);
		private static readonly Color APPLY_COLOR = new(0.40f, 0.85f, 0.45f, 1f);

		private readonly HashSet<ContentEntryScriptableObject> _selected = new();
		private readonly HashSet<ContentEntryScriptableObject> _selectedDisabled = new();
		private ContentUsageAuditResult _result;
		private bool _showHelp;

		// Предупреждения аудита — блокируют отключение, поэтому показываем отдельным списком сверху
		[ShowInInspector, PropertyOrder(-1)]
		[LabelText("@WarningsLabel")]
		[ShowIf(nameof(HasWarnings))]
		[ListDrawerSettings(IsReadOnly = true, ShowItemCount = false)]
		private List<string> _warnings = new();

		// Секция сломанных ссылок сверху — рисуется Odin-списком, как в Content Browser
		[ShowInInspector, Searchable, PropertyOrder(0)]
		[LabelText("@DisabledLabel")]
		[ShowIf(nameof(HasDisabledReferences))]
		[ListDrawerSettings(
			OnTitleBarGUI = nameof(DrawDisabledTitleBar),
			NumberOfItemsPerPage = 100,
			HideAddButton = true,
			HideRemoveButton = true,
			DraggableItems = false)]
		private List<DisabledRow> _disabledRows = new();

		// Основной список неиспользуемого контента с чекбоксами выделения
		[ShowInInspector, Searchable, PropertyOrder(1), PropertySpace(1)]
		[LabelText("@UnusedLabel")]
		[ShowIf(nameof(HasResult))]
		[ListDrawerSettings(
			OnTitleBarGUI = nameof(DrawUnusedTitleBar),
			NumberOfItemsPerPage = 100,
			HideAddButton = true,
			HideRemoveButton = true,
			DraggableItems = false)]
		private List<AuditRow> _unusedRows = new();

		private bool HasResult => _result != null;
		private bool HasDisabledReferences => _result is {DisabledReferences: {Count: > 0}};
		private bool HasWarnings => _warnings.Count > 0;

		private string WarningsLabel => $"Warnings {_warnings.Count}";

		// Счётчик unused/all configs заменяет собой стандартный заголовок "Items"
		private string UnusedLabel => _result == null
			? "Unused"
			: $"{_result.Unused.Count}/{_result.AllCount} (unused/all configs, eligible: {_result.CandidateCount}, used: {_result.UsedCount})";

		private string DisabledLabel => _result == null
			? "Disabled but Referenced"
			: $"Disabled but Referenced {_result.DisabledReferences.Count}";

		[MenuItem(ContentMenuConstants.VALIDATION_MENU + NAME, priority = 12)]
		public static void OpenWindow()
		{
			var window = GetWindow<ContentUsedAuditWindow>(WINDOW_NAME);
			window.minSize = new Vector2(760, 420);
			window.Show();
		}

		// Верхний тулбар: слева синяя основная кнопка Run Audit, дальше служебные, справа — справка
		protected override void OnBeginDrawEditors()
		{
			SirenixEditorGUI.BeginHorizontalToolbar();
			{
				if (SirenixEditorGUI.ToolbarButton(" Run Audit"))
				{
					RunAudit();
					GUIUtility.ExitGUI();
				}

				GUILayout.FlexibleSpace();

				// Справка-тугл: нажал — описание висит, отжал — скрылось
				_showHelp = SirenixEditorGUI.ToolbarToggle(_showHelp, EditorIcons.Info);
			}
			SirenixEditorGUI.EndHorizontalToolbar();

			if (_showHelp)
				SirenixEditorGUI.InfoMessageBox(AUDIT_DESCRIPTION);

			DrawStatusBar();
		}

		private void DrawStatusBar()
		{
			if (_result == null)
			{
				FusumityEditorGUILayout.DrawWarning("Press [Run Audit] in the top-left to start the audit", iconSize: 60, icon: EditorGUIUtility.IconContent("console.infoicon"));
				return;
			}

			if (!_result.IsComplete)
				SirenixEditorGUI.WarningMessageBox("Audit is incomplete or has warnings — disabling is blocked");
			else if (_result.DisabledReferences.Count > 0)
				SirenixEditorGUI.ErrorMessageBox("Enabled content references disabled content — disabling is blocked");

			if (EditorApplication.isPlayingOrWillChangePlaymode && _result.DisabledReferences.Count > 0)
				SirenixEditorGUI.WarningMessageBox("Exit Play Mode to apply fixes");
		}

		// Кнопки выделения и основное действие в заголовке списка неиспользуемого контента
		private void DrawUnusedTitleBar()
		{
			if (_result == null)
				return;

			using (new EditorGUI.DisabledScope(_result.Unused.Count == 0))
			{
				if (SirenixEditorGUI.ToolbarButton("All"))
				{
					_selected.Clear();
					_selected.UnionWith(_result.Unused);
				}

				if (SirenixEditorGUI.ToolbarButton("None"))
					_selected.Clear();
			}

			using (new EditorGUI.DisabledScope(
				_selected.Count == 0 ||
				_result.DisabledReferences.Count > 0 ||
				EditorApplication.isPlayingOrWillChangePlaymode))
			{
				if (ColoredToolbarButton($"Disable ({_selected.Count})", DANGER_COLOR))
				{
					DisableSelected();
					GUIUtility.ExitGUI();
				}
			}
		}

		// Кнопки выделения и исправление в заголовке списка сломанных ссылок
		private void DrawDisabledTitleBar()
		{
			if (_result == null)
				return;

			if (SirenixEditorGUI.ToolbarButton("All"))
				CollectDisabledTargets(_selectedDisabled);

			if (SirenixEditorGUI.ToolbarButton("None"))
				_selectedDisabled.Clear();

			using (new EditorGUI.DisabledScope(
				_selectedDisabled.Count == 0 ||
				EditorApplication.isPlayingOrWillChangePlaymode))
			{
				if (ColoredToolbarButton($"Fix ({_selectedDisabled.Count})", APPLY_COLOR))
				{
					FixSelectedDisabled();
					GUIUtility.ExitGUI();
				}
			}
		}

		// Пересобирает Odin-строки списков по результату аудита
		private void RebuildRows()
		{
			_unusedRows.Clear();
			_disabledRows.Clear();
			_warnings.Clear();

			if (_result == null)
				return;

			_warnings.AddRange(_result.Warnings);

			foreach (var asset in _result.Unused)
			{
				if (asset)
					_unusedRows.Add(new AuditRow(this, _selected, asset));
			}

			foreach (var reference in _result.DisabledReferences)
			{
				if (reference.Target)
					_disabledRows.Add(new DisabledRow(this, _selectedDisabled, reference));
			}
		}

		private static bool RowSelected(HashSet<ContentEntryScriptableObject> selection, ContentEntryScriptableObject asset)
			=> asset && selection != null && selection.Contains(asset);

		private static void RowSetSelected(
			ContentUsedAuditWindow window,
			HashSet<ContentEntryScriptableObject> selection,
			ContentEntryScriptableObject asset,
			bool value)
		{
			if (!asset || selection == null)
				return;

			if (value)
				selection.Add(asset);
			else
				selection.Remove(asset);

			if (window)
				window.Repaint();
		}

		private static bool ColoredToolbarButton(string label, Color color)
		{
			var prev = GUI.backgroundColor;
			GUI.backgroundColor = color;
			var clicked = SirenixEditorGUI.ToolbarButton(label);
			GUI.backgroundColor = prev;
			return clicked;
		}

		private static void SetEnabled(ContentEntryScriptableObject asset, bool value, string undoName)
		{
			var database = ContentDatabaseEditorUtility.GetDatabase(asset);
			Undo.RecordObject(database, undoName);

			// У импортёрных ассетов состоянием владеет импортёр — сам SO трогать нельзя, его перезапечёт реимпорт
			if (asset.IsImported)
			{
				var path = AssetDatabase.GetAssetPath(asset);
				var assetImporter = AssetImporter.GetAtPath(path);
				if (assetImporter is IContentScriptedImporter contentImporter)
				{
					Undo.RecordObject(assetImporter, undoName);
					contentImporter.Enabled = value;
					EditorUtility.SetDirty(assetImporter);
					assetImporter.SaveAndReimport();
				}

				return;
			}

			Undo.RecordObject(asset, undoName);
			asset.enabled = value;
			EditorUtility.SetDirty(asset);

			if (value)
				ContentDatabaseEditorUtility.AddToDatabase(asset, false);
			else
				ContentDatabaseEditorUtility.RemoveToDatabase(asset, false);
		}

		// Цели сломанных ссылок — выключенные конфиги, на которые ещё ссылаются
		private void CollectDisabledTargets(HashSet<ContentEntryScriptableObject> into)
		{
			into.Clear();
			if (_result == null)
				return;

			foreach (var reference in _result.DisabledReferences)
			{
				if (reference.Target)
					into.Add(reference.Target);
			}
		}

		// Общий пайплайн массового применения: диалог -> Undo-группа -> действие по каждому -> сохранение и сброс
		private void ApplyBatch(
			IReadOnlyList<ContentEntryScriptableObject> assets,
			string title,
			string message,
			string confirm,
			string undoName,
			Action<ContentEntryScriptableObject> apply)
		{
			if (assets.Count == 0)
				return;

			if (!EditorUtility.DisplayDialog(title, message, confirm, "Cancel"))
				return;

			Undo.IncrementCurrentGroup();
			var undoGroup = Undo.GetCurrentGroup();
			Undo.SetCurrentGroupName(undoName);

			for (int i = 0; i < assets.Count; i++)
				apply(assets[i]);

			AssetDatabase.SaveAssets();
			ContentEditorCache.ClearAndRefreshScrObjs();
			Undo.CollapseUndoOperations(undoGroup);
			_selected.Clear();
			_selectedDisabled.Clear();
			_result = null;
			RebuildRows();
			Repaint();
		}

		// Строка неиспользуемого конфига: ObjectField с inline-редактором + чекбокс выделения (как в Content Browser)
		[Serializable]
		private struct AuditRow : IContentBrowserInlineToggleHandler
		{
			private readonly ContentUsedAuditWindow _window;
			private readonly HashSet<ContentEntryScriptableObject> _selection;
			private readonly ContentEntryScriptableObject _asset;

			public AuditRow(ContentUsedAuditWindow window, HashSet<ContentEntryScriptableObject> selection, ContentEntryScriptableObject asset)
			{
				_window = window;
				_selection = selection;
				_asset = asset;
			}

			[ShowInInspector, HideLabel]
			[ContentBrowserInlineEditor("→")]
			public ContentScriptableObject Asset { get => _asset; set { } }

			bool IContentBrowserInlineToggleHandler.ShowContentBrowserInlineToggle => true;

			bool IContentBrowserInlineToggleHandler.ContentBrowserInlineToggle { get => RowSelected(_selection, _asset); set => RowSetSelected(_window, _selection, _asset, value); }
		}

		// Строка сломанной ссылки: тот же ряд + сворачиваемый список источников "Referenced by"
		[Serializable]
		private struct DisabledRow : IContentBrowserInlineToggleHandler
		{
			private readonly ContentUsedAuditWindow _window;
			private readonly HashSet<ContentEntryScriptableObject> _selection;
			private readonly ContentEntryScriptableObject _asset;
			private readonly UnityObject[] _sources;

			public DisabledRow(ContentUsedAuditWindow window, HashSet<ContentEntryScriptableObject> selection, DisabledContentReference reference)
			{
				_window = window;
				_selection = selection;
				_asset = reference.Target;
				_sources = reference.Sources?.Where(source => source).ToArray() ?? Array.Empty<UnityObject>();
			}

			[ShowInInspector, HideLabel, PropertyOrder(0)]
			[ContentBrowserInlineEditor("→")]
			public ContentScriptableObject Asset { get => _asset; set { } }

			[ShowInInspector, PropertyOrder(1), PropertySpace(2), ReadOnly]
			[LabelText("Referenced by")]
			[ListDrawerSettings(ShowItemCount = false, HideAddButton = true, HideRemoveButton = true, DraggableItems = false)]
			private UnityObject[] Sources => _sources;

			bool IContentBrowserInlineToggleHandler.ShowContentBrowserInlineToggle => true;

			bool IContentBrowserInlineToggleHandler.ContentBrowserInlineToggle { get => RowSelected(_selection, _asset); set => RowSetSelected(_window, _selection, _asset, value); }
		}

		private void RunAudit()
		{
			try
			{
				_result = ContentUsageAudit.Run(ReportProgress);
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}

			_selected.RemoveWhere(asset => !asset || !_result.Unused.Contains(asset));

			using (HashSetPool<ContentEntryScriptableObject>.Get(out var disabledTargets))
			{
				CollectDisabledTargets(disabledTargets);
				_selectedDisabled.RemoveWhere(asset => !asset || !disabledTargets.Contains(asset));
			}

			RebuildRows();
			Repaint();
		}

		private static void ReportProgress(float progress, string info)
		{
			EditorUtility.DisplayProgressBar(WINDOW_NAME, info, progress);
		}

		private void FixSelectedDisabled()
		{
			if (_result == null ||
				!_result.IsComplete ||
				EditorApplication.isPlayingOrWillChangePlaymode)
				return;

			using (HashSetPool<ContentEntryScriptableObject>.Get(out var targets))
			using (ListPool<ContentEntryScriptableObject>.Get(out var assets))
			{
				CollectDisabledTargets(targets);
				foreach (var asset in _selectedDisabled)
				{
					if (asset && !asset.Enabled && targets.Contains(asset))
						assets.Add(asset);
				}

				ApplyBatch(
					assets,
					"Fix Disabled Content",
					$"Enabled will be restored for {assets.Count} referenced configs and they will be registered in their Content databases\n\nContinue",
					"Fix",
					"Enable referenced Content configs",
					asset => SetEnabled(asset, true, "Enable referenced Content config"));
			}
		}

		private void DisableSelected()
		{
			if (_result == null ||
				!_result.IsComplete ||
				_result.DisabledReferences.Count > 0 ||
				EditorApplication.isPlayingOrWillChangePlaymode)
				return;

			using (ListPool<ContentEntryScriptableObject>.Get(out var assets))
			{
				foreach (var asset in _selected)
				{
					if (asset && asset.Enabled && !asset.HasContentGeneration() && _result.Unused.Contains(asset))
						assets.Add(asset);
				}

				ApplyBatch(
					assets,
					"Disable Unused Content",
					$"Enabled will be cleared for {assets.Count} configs and they will be removed from their Content databases\n\nContinue",
					"Disable",
					"Disable unused Content configs",
					asset => SetEnabled(asset, false, "Disable unused Content config"));
			}
		}
	}

	internal sealed class ContentUsageAuditResult
	{
		public bool IsComplete { get; }
		public int AllCount { get; }
		public int CandidateCount { get; }
		public int UsedCount { get; }
		public int RootCount { get; }
		public IReadOnlyList<ContentEntryScriptableObject> Unused { get; }
		public IReadOnlyList<DisabledContentReference> DisabledReferences { get; }
		public IReadOnlyList<string> Warnings { get; }

		public ContentUsageAuditResult(
			bool isComplete,
			int allCount,
			int candidateCount,
			int usedCount,
			int rootCount,
			IReadOnlyList<ContentEntryScriptableObject> unused,
			IReadOnlyList<DisabledContentReference> disabledReferences,
			IReadOnlyList<string> warnings)
		{
			IsComplete = isComplete;
			AllCount = allCount;
			CandidateCount = candidateCount;
			UsedCount = usedCount;
			RootCount = rootCount;
			Unused = unused;
			DisabledReferences = disabledReferences;
			Warnings = warnings;
		}
	}

	internal sealed class DisabledContentReference
	{
		public ContentEntryScriptableObject Target { get; }
		public IReadOnlyList<UnityObject> Sources { get; }

		public DisabledContentReference(
			ContentEntryScriptableObject target,
			IReadOnlyList<UnityObject> sources)
		{
			Target = target;
			Sources = sources;
		}
	}

	internal static class ContentUsageAudit
	{
		private static readonly Regex ContentManagerTypeRegex = new(
			@"ContentManager\s*\.\s*[A-Za-z_][A-Za-z0-9_]*\s*<\s*(?:global::)?(?<type>[A-Za-z_][A-Za-z0-9_.]*)\s*>",
			RegexOptions.Compiled);

		public static ContentUsageAuditResult Run(Action<float, string> onProgress = null)
		{
			onProgress?.Invoke(0f, "Refreshing content cache");
			ContentEditorCache.ClearAndRefreshScrObjs();

			var warnings = new List<string>();
			var allContent = ContentEditorCache.GetAssets<ContentScriptableObject>()
				.Where(asset => asset)
				.Distinct()
				.ToArray();
			onProgress?.Invoke(0.1f, "Collecting registered content");
			var registeredContent = CollectRegisteredContent(allContent, warnings);
			var allEntries = allContent
				.OfType<ContentEntryScriptableObject>()
				.ToArray();

			var candidates = allEntries
				.Where(asset => asset.Enabled &&
					registeredContent.Contains(asset) &&
					!asset.HasContentGeneration() &&
					// импортёрные конфиги нельзя выключать (состоянием владеет импортёр) — не считаем их лишними
					!asset.IsImported)
				.ToArray();
			var candidateSet = new HashSet<ContentEntryScriptableObject>(candidates);
			var edges = candidates.ToDictionary(
				asset => asset,
				_ => new HashSet<ContentEntryScriptableObject>());
			var roots = new HashSet<ContentEntryScriptableObject>();
			var disabledReferences = new Dictionary<ContentEntryScriptableObject, HashSet<UnityObject>>();

			onProgress?.Invoke(0.15f, "Building GUID map");
			var guidToContent = BuildGuidMap(allEntries, warnings);

			// Сканирование ссылок — 0.2..0.6
			for (int i = 0; i < allContent.Length; i++)
			{
				if (onProgress != null && (i & 31) == 0)
					onProgress(0.2f + 0.4f * i / allContent.Length, $"Scanning references ({i + 1}/{allContent.Length})");

				var asset = allContent[i];
				if (!asset.Enabled)
					continue;
				if (asset is not ContentDatabaseScriptableObject && !registeredContent.Contains(asset))
					continue;

				var owner = asset as ContentEntryScriptableObject;
				if (owner != null && !candidateSet.Contains(owner))
					owner = null;

				var scanner = new ContentReferenceScanner(
					asset,
					owner,
					guidToContent,
					edges,
					roots,
					disabledReferences);
				scanner.Scan(asset);
			}

			ScanContentManagerRoots(candidates, roots, warnings, onProgress);

			onProgress?.Invoke(0.98f, "Computing reachability");
			var reachable = CollectReachable(roots, edges);
			var unused = candidates
				.Where(asset => !reachable.Contains(asset))
				.OrderBy(AssetDatabase.GetAssetPath, StringComparer.OrdinalIgnoreCase)
				.ToArray();

			return new ContentUsageAuditResult(
				warnings.Count == 0,
				allEntries.Length,
				candidates.Length,
				reachable.Count,
				roots.Count,
				unused,
				BuildDisabledReferences(disabledReferences),
				warnings);
		}

		private static IReadOnlyList<DisabledContentReference> BuildDisabledReferences(
			IReadOnlyDictionary<ContentEntryScriptableObject, HashSet<UnityObject>> references)
		{
			return references
				.OrderBy(pair => AssetDatabase.GetAssetPath(pair.Key), StringComparer.OrdinalIgnoreCase)
				.Select(pair => new DisabledContentReference(
					pair.Key,
					pair.Value
						.OrderBy(AssetDatabase.GetAssetPath, StringComparer.OrdinalIgnoreCase)
						.ToArray()))
				.ToArray();
		}

		private static void AddDisabledReference(
			IDictionary<ContentEntryScriptableObject, HashSet<UnityObject>> references,
			ContentEntryScriptableObject target,
			UnityObject source)
		{
			if (!target || target.Enabled || !source)
				return;
			if (!references.TryGetValue(target, out var sources))
			{
				sources = new HashSet<UnityObject>();
				references[target] = sources;
			}

			sources.Add(source);
		}

		private static HashSet<ContentScriptableObject> CollectRegisteredContent(
			IEnumerable<ContentScriptableObject> allContent,
			ICollection<string> warnings)
		{
			var result = new HashSet<ContentScriptableObject>();

			foreach (var database in allContent.OfType<ContentDatabaseScriptableObject>())
			{
				if (database.scriptableObjects == null)
				{
					warnings.Add($"Null scriptableObjects list in database {AssetDatabase.GetAssetPath(database)}");
					continue;
				}

				foreach (var asset in database.scriptableObjects)
				{
					if (asset)
						result.Add(asset);
					else
						warnings.Add($"Null entry in database {AssetDatabase.GetAssetPath(database)}");
				}
			}

			return result;
		}

		private static Dictionary<SerializableGuid, ContentEntryScriptableObject> BuildGuidMap(
			IEnumerable<ContentEntryScriptableObject> candidates,
			ICollection<string> warnings)
		{
			var result = new Dictionary<SerializableGuid, ContentEntryScriptableObject>();
			var ambiguous = new HashSet<SerializableGuid>();

			foreach (var candidate in candidates)
			{
				if (candidate is not IUniqueContentEntrySource source)
					continue;

				Register(source.Guid, candidate);

				var nested = source.ContentEntry?.Nested;
				if (nested == null)
					continue;

				foreach (var guid in nested.Keys)
					Register(guid, candidate);
			}

			return result;

			void Register(SerializableGuid guid, ContentEntryScriptableObject candidate)
			{
				if (guid.IsEmpty() || ambiguous.Contains(guid))
					return;

				if (!result.TryGetValue(guid, out var previous) || previous == candidate)
				{
					result[guid] = candidate;
					return;
				}

				result.Remove(guid);
				ambiguous.Add(guid);
				warnings.Add(
					$"Duplicate Content GUID {guid}: {AssetDatabase.GetAssetPath(previous)} and {AssetDatabase.GetAssetPath(candidate)}");
			}
		}

		private static void ScanContentManagerRoots(
			IEnumerable<ContentEntryScriptableObject> candidates,
			ISet<ContentEntryScriptableObject> roots,
			ICollection<string> warnings,
			Action<float, string> onProgress = null)
		{
			var byTypeName = new Dictionary<string, HashSet<ContentEntryScriptableObject>>(StringComparer.Ordinal);
			foreach (var candidate in candidates)
			{
				var type = candidate.ValueType;
				if (type == null)
					continue;

				Add(type.Name, candidate);
				if (!type.FullName.IsNullOrEmpty())
					Add(type.FullName, candidate);
			}

			// Чтение всех скриптов — самая долгая фаза, 0.6..0.95
			var scripts = AssetDatabase.FindAssets("t:MonoScript", new[] {"Assets"});
			for (int i = 0; i < scripts.Length; i++)
			{
				if (onProgress != null && (i & 63) == 0)
					onProgress(0.6f + 0.35f * i / scripts.Length, $"Scanning code ({i + 1}/{scripts.Length})");

				var path = AssetDatabase.GUIDToAssetPath(scripts[i]);
				string text;
				try
				{
					text = File.ReadAllText(path);
				}
				catch (Exception exception)
				{
					warnings.Add($"Failed to read code at {path}: {exception.Message}");
					continue;
				}

				foreach (Match match in ContentManagerTypeRegex.Matches(text))
				{
					var typeName = match.Groups["type"].Value;
					if (byTypeName.TryGetValue(typeName, out var typedCandidates))
						roots.UnionWith(typedCandidates);
				}
			}

			void Add(string typeName, ContentEntryScriptableObject candidate)
			{
				if (!byTypeName.TryGetValue(typeName, out var typedCandidates))
				{
					typedCandidates = new HashSet<ContentEntryScriptableObject>();
					byTypeName[typeName] = typedCandidates;
				}

				typedCandidates.Add(candidate);
			}
		}

		private static HashSet<ContentEntryScriptableObject> CollectReachable(
			IEnumerable<ContentEntryScriptableObject> roots,
			IReadOnlyDictionary<ContentEntryScriptableObject, HashSet<ContentEntryScriptableObject>> edges)
		{
			var reachable = new HashSet<ContentEntryScriptableObject>();
			var queue = new Queue<ContentEntryScriptableObject>();

			foreach (var root in roots)
			{
				if (reachable.Add(root))
					queue.Enqueue(root);
			}

			while (queue.Count > 0)
			{
				var current = queue.Dequeue();
				if (!edges.TryGetValue(current, out var targets))
					continue;

				foreach (var target in targets)
				{
					if (reachable.Add(target))
						queue.Enqueue(target);
				}
			}

			return reachable;
		}

		private sealed class ContentReferenceScanner : IContentValueValidator
		{
			private readonly ContentScriptableObject _context;
			private readonly ContentEntryScriptableObject _owner;
			private readonly IReadOnlyDictionary<SerializableGuid, ContentEntryScriptableObject> _guidToContent;
			private readonly IReadOnlyDictionary<ContentEntryScriptableObject, HashSet<ContentEntryScriptableObject>> _edges;
			private readonly ISet<ContentEntryScriptableObject> _roots;
			private readonly IDictionary<ContentEntryScriptableObject, HashSet<UnityObject>> _disabledReferences;

			public ContentReferenceScanner(
				ContentScriptableObject context,
				ContentEntryScriptableObject owner,
				IReadOnlyDictionary<SerializableGuid, ContentEntryScriptableObject> guidToContent,
				IReadOnlyDictionary<ContentEntryScriptableObject, HashSet<ContentEntryScriptableObject>> edges,
				ISet<ContentEntryScriptableObject> roots,
				IDictionary<ContentEntryScriptableObject, HashSet<UnityObject>> disabledReferences)
			{
				_context = context;
				_owner = owner;
				_guidToContent = guidToContent;
				_edges = edges;
				_roots = roots;
				_disabledReferences = disabledReferences;
			}

			public void Scan(ContentScriptableObject target)
			{
				var warningCount = 0;
				ContentValidator.ValidateContentObject(
					target,
					target.GetType(),
					target.name,
					target,
					target,
					ContentValidator.GetEnabledValidators(),
					ref warningCount,
					additionalValidator: this);
			}

			public bool Validate(in ContentValidationContext context, out string message)
			{
				message = null;
				if (context.value is IContentReference reference)
				{
					if (!reference.IsEmpty() && !reference.IsSingle)
						Register(reference.Guid);
					return true;
				}

				if (context.value is SerializableGuid guid)
				{
					Register(guid);
					return true;
				}

				if (context.value is string id &&
					!id.IsNullOrEmpty() &&
					TryGetContentReferenceSource(context.contentReferenceAttribute, id, out var source))
				{
					Register(source);
					return true;
				}

				if (context.value is ContentEntryScriptableObject contentAsset &&
					!ReferenceEquals(contentAsset, _context))
					Register(contentAsset);

				return true;
			}

			private void Register(SerializableGuid guid)
			{
				if (_guidToContent.TryGetValue(guid, out var target))
					Register(target);
			}

			private static bool TryGetContentReferenceSource(
				ContentReferenceAttribute attribute,
				string id,
				out IContentEntrySource source)
			{
				source = null;
				if (attribute == null)
					return false;

				var valueType = attribute.Type;
				if (valueType == null && !attribute.TypeName.IsNullOrEmpty())
					ReflectionUtility.TryGetType(attribute.TypeName, out valueType);

				return valueType != null && ContentEditorCache.TryGetSource(valueType, id, out source);
			}

			private void Register(IContentEntrySource source)
			{
				if (source == null)
					return;

				ContentEditorCache.IsSourceDisabled(source, out var sourceObject);
				if (sourceObject is ContentEntryScriptableObject target)
					Register(target);
			}

			private void Register(ContentEntryScriptableObject target)
			{
				if (!target)
					return;
				if (!target.Enabled)
				{
					AddDisabledReference(_disabledReferences, target, _context);
					return;
				}

				if (!_edges.ContainsKey(target))
					return;

				if (_owner != null && _edges.TryGetValue(_owner, out var targets))
				{
					targets.Add(target);
					return;
				}

				_roots.Add(target);
			}
		}
	}
}
