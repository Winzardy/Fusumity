using Content;
using Content.ScriptableObjects;
using Content.ScriptableObjects.Editor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Sapientia.Extensions;
using UnityEditor;
using UnityEngine;

namespace Content.Editor
{
	public sealed class ContentUsedAuditWindow : EditorWindow
	{
		internal const string WINDOW_NAME = "Сontent Used Audit";
		private const string NAME = "Used Audit";

		private readonly HashSet<ContentEntryScriptableObject> _selected = new();
		private readonly HashSet<ContentEntryScriptableObject> _selectedDisabled = new();
		private ContentUsageAuditResult _result;
		private Vector2 _scrollPosition;
		private bool _showDisabledReferences = true;
		private bool _showWarnings;

		[MenuItem(ContentMenuConstants.VALIDATION_MENU + NAME, priority = 12)]
		public static void OpenWindow()
		{
			var window = GetWindow<ContentUsedAuditWindow>(WINDOW_NAME);
			window.minSize = new Vector2(760, 420);
			window.Show();
		}

		private void OnGUI()
		{
			DrawToolbar();

			EditorGUILayout.HelpBox(
				"Аудит считает используемыми конфиги, достижимые из singleton/database настроек, prefab-ов, которые обходит Content Validation, и прямых вызовов ContentManager. Runtime-ссылки из сервера, save-data и строк требуют ручной проверки",
				MessageType.Info);

			if (_result == null)
			{
				EditorGUILayout.HelpBox("Click Run Audit to build the usage graph", MessageType.None);
				return;
			}

			DrawSummary();
			_scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
			DrawDisabledReferences();
			DrawWarnings();
			DrawUnusedAssets();
			EditorGUILayout.EndScrollView();
		}

		private void DrawToolbar()
		{
			using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
			{
				if (GUILayout.Button("Run Audit", EditorStyles.toolbarButton, GUILayout.Width(90)))
					RunAudit();

				using (new EditorGUI.DisabledScope(_result == null || !_result.IsComplete || _result.Unused.Count == 0))
				{
					if (GUILayout.Button("Select All", EditorStyles.toolbarButton, GUILayout.Width(100)))
					{
						_selected.Clear();
						_selected.UnionWith(_result.Unused);
					}

					if (GUILayout.Button("Clear Selection", EditorStyles.toolbarButton, GUILayout.Width(100)))
						_selected.Clear();
				}

				using (new EditorGUI.DisabledScope(_result == null))
				{
					if (GUILayout.Button("Copy Report", EditorStyles.toolbarButton, GUILayout.Width(140)))
						CopyReport();
				}

				GUILayout.FlexibleSpace();

				using (new EditorGUI.DisabledScope(
					_result == null ||
					!_result.IsComplete ||
					_result.DisabledReferences.Count > 0 ||
					EditorApplication.isPlayingOrWillChangePlaymode ||
					_selected.Count == 0))
				{
					if (GUILayout.Button("Disable Selected", EditorStyles.toolbarButton, GUILayout.Width(160)))
						DisableSelected();
				}
			}
		}

		private void DrawSummary()
		{
			var message = !_result.IsComplete
				? "The audit is incomplete or contains warnings, disabling is blocked"
				: $"Enabled unique configs: {_result.CandidateCount}    Used: {_result.UsedCount}    Unused: {_result.Unused.Count}    Disabled but referenced: {_result.DisabledReferences.Count}    Roots: {_result.RootCount}";
			var messageType = !_result.IsComplete
				? MessageType.Warning
				: _result.DisabledReferences.Count > 0
					? MessageType.Error
					: MessageType.None;

			EditorGUILayout.HelpBox(message, messageType);
		}

		private void DrawDisabledReferences()
		{
			if (_result.DisabledReferences.Count == 0)
				return;

			EditorGUILayout.HelpBox(
				"Enabled Content configs or their validated prefabs reference disabled Content, disabling is blocked",
				MessageType.Error);
			if (EditorApplication.isPlayingOrWillChangePlaymode)
				EditorGUILayout.HelpBox("Exit Play Mode to apply fixes", MessageType.Warning);
			_showDisabledReferences = EditorGUILayout.Foldout(
				_showDisabledReferences,
				$"Disabled but Referenced: {_result.DisabledReferences.Count}",
				true);
			if (!_showDisabledReferences)
				return;

			using (new EditorGUILayout.HorizontalScope())
			{
				if (GUILayout.Button("Select All", GUILayout.Width(100)))
				{
					_selectedDisabled.Clear();
					_selectedDisabled.UnionWith(_result.DisabledReferences.Select(reference => reference.Target));
				}

				if (GUILayout.Button("Clear", GUILayout.Width(100)))
					_selectedDisabled.Clear();

				GUILayout.FlexibleSpace();
				using (new EditorGUI.DisabledScope(
					_selectedDisabled.Count == 0 ||
					EditorApplication.isPlayingOrWillChangePlaymode))
				{
					if (GUILayout.Button("Fix Selected", GUILayout.Width(140)))
					{
						FixSelectedDisabled();
						GUIUtility.ExitGUI();
					}
				}
			}

			foreach (var reference in _result.DisabledReferences)
			{
				if (!reference.Target)
					continue;

				var selected = _selectedDisabled.Contains(reference.Target);
				var nextSelected = selected;
				var originalColor = GUI.color;
				var cardColor = selected
					? originalColor
					: Color.Lerp(originalColor, Color.gray, 0.22f);
				GUI.color = cardColor;
				using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
				{
					using (new EditorGUILayout.HorizontalScope())
					{
						EditorGUILayout.ObjectField(
							reference.Target,
							typeof(ContentEntryScriptableObject),
							false);
						GUILayout.Label(
							reference.Target.ValueType?.Name ?? "Unknown",
							EditorStyles.miniLabel,
							GUILayout.Width(220));
						GUI.color = originalColor;
						nextSelected = EditorGUILayout.Toggle(selected, GUILayout.Width(18));
						GUI.color = cardColor;
					}

					EditorGUILayout.LabelField(AssetDatabase.GetAssetPath(reference.Target), EditorStyles.miniLabel);
					EditorGUILayout.LabelField("Referenced by", EditorStyles.boldLabel);
					using (new EditorGUI.IndentLevelScope())
					{
						foreach (var source in reference.Sources)
						{
							if (!source)
								continue;

							EditorGUILayout.ObjectField(source, typeof(UnityEngine.Object), false);
							EditorGUILayout.LabelField(AssetDatabase.GetAssetPath(source), EditorStyles.miniLabel);
						}
					}
				}
				GUI.color = originalColor;

				if (nextSelected != selected)
				{
					if (nextSelected)
						_selectedDisabled.Add(reference.Target);
					else
						_selectedDisabled.Remove(reference.Target);
				}
			}
		}

		private void DrawWarnings()
		{
			if (_result.Warnings.Count == 0)
				return;

			_showWarnings = EditorGUILayout.Foldout(_showWarnings, $"Warnings: {_result.Warnings.Count}", true);
			if (!_showWarnings)
				return;

			using (new EditorGUI.IndentLevelScope())
			{
				foreach (var warning in _result.Warnings)
					EditorGUILayout.HelpBox(warning, MessageType.Warning);
			}
		}

		private void DrawUnusedAssets()
		{
			if (!_result.IsComplete)
				return;

			if (_result.Unused.Count == 0)
			{
				EditorGUILayout.HelpBox("No unused enabled configs were found", MessageType.Info);
				return;
			}

			foreach (var asset in _result.Unused)
			{
				if (!asset)
					continue;

				using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
				{
					using (new EditorGUILayout.HorizontalScope())
					{
						var selected = _selected.Contains(asset);
						var nextSelected = EditorGUILayout.Toggle(selected, GUILayout.Width(18));
						if (nextSelected != selected)
						{
							if (nextSelected)
								_selected.Add(asset);
							else
								_selected.Remove(asset);
						}

						EditorGUILayout.ObjectField(asset, typeof(ContentEntryScriptableObject), false);
						GUILayout.Label(asset.ValueType?.Name ?? "Unknown", EditorStyles.miniLabel, GUILayout.Width(220));
					}

					EditorGUILayout.LabelField(AssetDatabase.GetAssetPath(asset), EditorStyles.miniLabel);
				}
			}
		}

		private void RunAudit()
		{
			_result = ContentUsageAudit.Run();
			_selected.RemoveWhere(asset => !asset || !_result.Unused.Contains(asset));
			var disabledTargets = new HashSet<ContentEntryScriptableObject>(
				_result.DisabledReferences.Select(reference => reference.Target));
			_selectedDisabled.RemoveWhere(asset => !asset || !disabledTargets.Contains(asset));
			Repaint();
		}

		private void CopyReport()
		{
			if (_result == null)
				return;

			var builder = new StringBuilder();
			if (_result.DisabledReferences.Count > 0)
			{
				builder.AppendLine("Disabled but Referenced");
				foreach (var reference in _result.DisabledReferences)
				{
					builder.AppendLine(AssetDatabase.GetAssetPath(reference.Target));
					foreach (var source in reference.Sources)
					{
						builder.Append("\t<- ");
						builder.AppendLine(AssetDatabase.GetAssetPath(source));
					}
				}
				builder.AppendLine();
			}

			builder.AppendLine("Unused Enabled Content");
			foreach (var asset in _result.Unused)
			{
				builder.Append(asset.ValueType?.Name ?? "Unknown");
				builder.Append('\t');
				builder.AppendLine(AssetDatabase.GetAssetPath(asset));
			}

			EditorGUIUtility.systemCopyBuffer = builder.ToString();
		}

		private void FixSelectedDisabled()
		{
			if (_result == null ||
				!_result.IsComplete ||
				EditorApplication.isPlayingOrWillChangePlaymode)
				return;

			var disabledTargets = new HashSet<ContentEntryScriptableObject>(
				_result.DisabledReferences.Select(reference => reference.Target));
			var assets = _selectedDisabled
				.Where(asset => asset && !asset.Enabled && disabledTargets.Contains(asset))
				.ToArray();
			if (assets.Length == 0)
				return;

			if (!EditorUtility.DisplayDialog(
				"Fix Disabled Content",
				$"Enabled will be restored for {assets.Length} referenced configs and they will be registered in their Content databases\n\nContinue",
				"Fix",
				"Cancel"))
				return;

			Undo.IncrementCurrentGroup();
			var undoGroup = Undo.GetCurrentGroup();
			Undo.SetCurrentGroupName("Enable referenced Content configs");

			foreach (var asset in assets)
			{
				var database = ContentDatabaseEditorUtility.GetDatabase(asset);
				Undo.RecordObject(asset, "Enable referenced Content config");
				Undo.RecordObject(database, "Register referenced Content config");
				asset.enabled = true;
				ContentDatabaseEditorUtility.AddToDatabase(asset, false);
				EditorUtility.SetDirty(asset);
			}

			AssetDatabase.SaveAssets();
			ContentEditorCache.ClearAndRefreshScrObjs();
			Undo.CollapseUndoOperations(undoGroup);
			_selected.Clear();
			_selectedDisabled.Clear();
			_result = null;
			Repaint();
		}

		private void DisableSelected()
		{
			if (_result == null ||
				!_result.IsComplete ||
				_result.DisabledReferences.Count > 0 ||
				EditorApplication.isPlayingOrWillChangePlaymode)
				return;

			var assets = _selected
				.Where(asset => asset &&
					asset.Enabled &&
					!asset.HasContentGeneration() &&
					_result.Unused.Contains(asset))
				.ToArray();
			if (assets.Length == 0)
				return;

			if (!EditorUtility.DisplayDialog(
				"Disable Unused Content",
				$"Enabled will be cleared for {assets.Length} configs and they will be removed from their Content databases\n\nContinue",
				"Disable",
				"Cancel"))
				return;

			Undo.IncrementCurrentGroup();
			var undoGroup = Undo.GetCurrentGroup();
			Undo.SetCurrentGroupName("Disable unused Content configs");

			foreach (var asset in assets)
			{
				Undo.RecordObject(asset, "Disable unused Content config");
				asset.enabled = false;
				EditorUtility.SetDirty(asset);
				ContentDatabaseEditorUtility.RemoveToDatabase(asset, false);
			}

			AssetDatabase.SaveAssets();
			ContentEditorCache.ClearAndRefreshScrObjs();
			Undo.CollapseUndoOperations(undoGroup);
			_selected.Clear();
			_result = null;
			Repaint();
		}
	}

	internal sealed class ContentUsageAuditResult
	{
		public bool IsComplete { get; }
		public int CandidateCount { get; }
		public int UsedCount { get; }
		public int RootCount { get; }
		public IReadOnlyList<ContentEntryScriptableObject> Unused { get; }
		public IReadOnlyList<DisabledContentReference> DisabledReferences { get; }
		public IReadOnlyList<string> Warnings { get; }

		public ContentUsageAuditResult(
			bool isComplete,
			int candidateCount,
			int usedCount,
			int rootCount,
			IReadOnlyList<ContentEntryScriptableObject> unused,
			IReadOnlyList<DisabledContentReference> disabledReferences,
			IReadOnlyList<string> warnings)
		{
			IsComplete = isComplete;
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
		public IReadOnlyList<UnityEngine.Object> Sources { get; }

		public DisabledContentReference(
			ContentEntryScriptableObject target,
			IReadOnlyList<UnityEngine.Object> sources)
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

		public static ContentUsageAuditResult Run()
		{
			ContentEditorCache.ClearAndRefreshScrObjs();

			var warnings = new List<string>();
			var allContent = ContentEditorCache.GetAssets<ContentScriptableObject>()
				.Where(asset => asset)
				.Distinct()
				.ToArray();
			var registeredContent = CollectRegisteredContent(allContent, warnings);
			var allEntries = allContent
				.OfType<ContentEntryScriptableObject>()
				.ToArray();

			var candidates = allEntries
				.Where(asset => asset.Enabled &&
					registeredContent.Contains(asset) &&
					!asset.HasContentGeneration())
				.ToArray();
			var candidateSet = new HashSet<ContentEntryScriptableObject>(candidates);
			var edges = candidates.ToDictionary(
				asset => asset,
				_ => new HashSet<ContentEntryScriptableObject>());
			var roots = new HashSet<ContentEntryScriptableObject>();
			var disabledReferences = new Dictionary<ContentEntryScriptableObject, HashSet<UnityEngine.Object>>();

			var guidToContent = BuildGuidMap(allEntries, warnings);

			foreach (var asset in allContent)
			{
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

			ScanContentManagerRoots(candidates, roots, warnings);

			var reachable = CollectReachable(roots, edges);
			var unused = candidates
				.Where(asset => !reachable.Contains(asset))
				.OrderBy(AssetDatabase.GetAssetPath, StringComparer.OrdinalIgnoreCase)
				.ToArray();

			return new ContentUsageAuditResult(
				warnings.Count == 0,
				candidates.Length,
				reachable.Count,
				roots.Count,
				unused,
				BuildDisabledReferences(disabledReferences),
				warnings);
		}

		private static IReadOnlyList<DisabledContentReference> BuildDisabledReferences(
			IReadOnlyDictionary<ContentEntryScriptableObject, HashSet<UnityEngine.Object>> references)
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
			IDictionary<ContentEntryScriptableObject, HashSet<UnityEngine.Object>> references,
			ContentEntryScriptableObject target,
			UnityEngine.Object source)
		{
			if (!target || target.Enabled || !source)
				return;
			if (!references.TryGetValue(target, out var sources))
			{
				sources = new HashSet<UnityEngine.Object>();
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
			ICollection<string> warnings)
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

			foreach (var guid in AssetDatabase.FindAssets("t:MonoScript", new[] {"Assets"}))
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
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
			private readonly IDictionary<ContentEntryScriptableObject, HashSet<UnityEngine.Object>> _disabledReferences;

			public ContentReferenceScanner(
				ContentScriptableObject context,
				ContentEntryScriptableObject owner,
				IReadOnlyDictionary<SerializableGuid, ContentEntryScriptableObject> guidToContent,
				IReadOnlyDictionary<ContentEntryScriptableObject, HashSet<ContentEntryScriptableObject>> edges,
				ISet<ContentEntryScriptableObject> roots,
				IDictionary<ContentEntryScriptableObject, HashSet<UnityEngine.Object>> disabledReferences)
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

				if (_owner == null)
				{
					_roots.Add(target);
					return;
				}

				if (_edges.TryGetValue(_owner, out var targets))
					targets.Add(target);
			}
		}

	}
}
