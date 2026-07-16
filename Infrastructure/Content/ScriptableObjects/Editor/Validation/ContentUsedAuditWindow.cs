using Content;
using Content.ScriptableObjects;
using Content.ScriptableObjects.Editor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Sapientia.Extensions;
using UnityEditor;
using UnityEngine;

namespace Content.Editor
{
	public sealed class ContentUsedAuditWindow : EditorWindow
	{
		private const string WINDOW_NAME = "Сontent Used Audit";
		private const string NAME = "Usability Audit";

		private readonly HashSet<ContentEntryScriptableObject> _selected = new();
		private ContentUsageAuditResult _result;
		private Vector2 _scrollPosition;
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
				"Аудит считает используемыми конфиги, достижимые из singleton/database настроек, внешних Unity assets и прямых вызовов ContentManager. Runtime-ссылки из сервера, save-data и строк требуют ручной проверки",
				MessageType.Info);

			if (_result == null)
			{
				EditorGUILayout.HelpBox("Click Run Audit to build the usage graph", MessageType.None);
				return;
			}

			DrawSummary();
			DrawWarnings();
			DrawUnusedAssets();
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

					if (GUILayout.Button("Copy List", EditorStyles.toolbarButton, GUILayout.Width(140)))
						CopyReport();
				}

				GUILayout.FlexibleSpace();

				using (new EditorGUI.DisabledScope(_result == null || !_result.IsComplete || _selected.Count == 0))
				{
					if (GUILayout.Button("Disable Selected", EditorStyles.toolbarButton, GUILayout.Width(160)))
						DisableSelected();
				}
			}
		}

		private void DrawSummary()
		{
			var message = _result.IsComplete
				? $"Enabled unique configs: {_result.CandidateCount}    Used: {_result.UsedCount}    Unused: {_result.Unused.Count}    Roots: {_result.RootCount}"
				: "The audit is incomplete or contains warnings, disabling is blocked";

			EditorGUILayout.HelpBox(message, _result.IsComplete ? MessageType.None : MessageType.Warning);
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

			_scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
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
			EditorGUILayout.EndScrollView();
		}

		private void RunAudit()
		{
			_result = ContentUsageAudit.Run();
			_selected.RemoveWhere(asset => !asset || !_result.Unused.Contains(asset));
			Repaint();
		}

		private void CopyReport()
		{
			if (_result == null)
				return;

			var builder = new StringBuilder();
			foreach (var asset in _result.Unused)
			{
				builder.Append(asset.ValueType?.Name ?? "Unknown");
				builder.Append('\t');
				builder.AppendLine(AssetDatabase.GetAssetPath(asset));
			}

			EditorGUIUtility.systemCopyBuffer = builder.ToString();
		}

		private void DisableSelected()
		{
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
		public IReadOnlyList<string> Warnings { get; }

		public ContentUsageAuditResult(
			bool isComplete,
			int candidateCount,
			int usedCount,
			int rootCount,
			IReadOnlyList<ContentEntryScriptableObject> unused,
			IReadOnlyList<string> warnings)
		{
			IsComplete = isComplete;
			CandidateCount = candidateCount;
			UsedCount = usedCount;
			RootCount = rootCount;
			Unused = unused;
			Warnings = warnings;
		}
	}

	internal static class ContentUsageAudit
	{
		private static readonly Regex SerializableGuidRegex = new(
			@"low:[ \t]*(-?\d+)[ \t]*\r?\n[ \t]*high:[ \t]*(-?\d+)",
			RegexOptions.Compiled);

		private static readonly Regex UnityGuidRegex = new(
			@"guid:[ \t]*([a-fA-F0-9]{32})",
			RegexOptions.Compiled);

		private static readonly Regex ContentManagerTypeRegex = new(
			@"ContentManager\s*\.\s*[A-Za-z_][A-Za-z0-9_]*\s*<\s*(?:global::)?(?<type>[A-Za-z_][A-Za-z0-9_.]*)\s*>",
			RegexOptions.Compiled);

		private static readonly HashSet<string> SerializedAssetExtensions = new(StringComparer.OrdinalIgnoreCase)
		{
			".asset",
			".prefab",
			".unity",
			".controller",
			".overrideController",
			".playable",
			".anim"
		};

		public static ContentUsageAuditResult Run()
		{
			ContentEditorCache.ClearAndRefreshScrObjs();

			var warnings = new List<string>();
			var allContent = ContentEditorCache.GetAssets<ContentScriptableObject>()
				.Where(asset => asset)
				.Distinct()
				.ToArray();
			var registeredContent = CollectRegisteredContent(allContent, warnings);

			var candidates = allContent
				.OfType<ContentEntryScriptableObject>()
				.Where(asset => asset.Enabled &&
					registeredContent.Contains(asset) &&
					!asset.HasContentGeneration())
				.ToArray();
			var candidateSet = new HashSet<ContentEntryScriptableObject>(candidates);
			var edges = candidates.ToDictionary(
				asset => asset,
				_ => new HashSet<ContentEntryScriptableObject>());
			var roots = new HashSet<ContentEntryScriptableObject>();

			var guidToCandidate = BuildGuidMap(candidates, warnings);
			var unityGuidToCandidate = BuildUnityGuidMap(candidates, warnings);

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
					guidToCandidate,
					edges,
					roots,
					warnings);
				scanner.Scan(asset);
			}

			var contentPaths = new HashSet<string>(
				allContent.Select(AssetDatabase.GetAssetPath),
				StringComparer.OrdinalIgnoreCase);

			if (!ScanSerializedRoots(contentPaths, guidToCandidate, unityGuidToCandidate, roots, warnings))
			{
				return new ContentUsageAuditResult(
					false,
					candidates.Length,
					0,
					roots.Count,
					Array.Empty<ContentEntryScriptableObject>(),
					warnings);
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
				warnings);
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

		private static Dictionary<string, ContentEntryScriptableObject> BuildUnityGuidMap(
			IEnumerable<ContentEntryScriptableObject> candidates,
			ICollection<string> warnings)
		{
			var result = new Dictionary<string, ContentEntryScriptableObject>(StringComparer.OrdinalIgnoreCase);
			var ambiguous = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			foreach (var candidate in candidates)
			{
				var path = AssetDatabase.GetAssetPath(candidate);
				var guid = AssetDatabase.AssetPathToGUID(path);
				if (guid.IsNullOrEmpty())
				{
					warnings.Add($"Unity GUID was not found for {path}");
					continue;
				}

				if (ambiguous.Contains(guid))
					continue;

				if (result.TryGetValue(guid, out var previous) && previous != candidate)
				{
					warnings.Add($"Duplicate Unity GUID {guid}: {AssetDatabase.GetAssetPath(previous)} and {path}");
					result.Remove(guid);
					ambiguous.Add(guid);
					continue;
				}

				result[guid] = candidate;
			}

			return result;
		}

		private static bool ScanSerializedRoots(
			HashSet<string> contentPaths,
			IReadOnlyDictionary<SerializableGuid, ContentEntryScriptableObject> guidToCandidate,
			IReadOnlyDictionary<string, ContentEntryScriptableObject> unityGuidToCandidate,
			ISet<ContentEntryScriptableObject> roots,
			ICollection<string> warnings)
		{
			var paths = AssetDatabase.GetAllAssetPaths()
				.Where(path => path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
				.Where(path => SerializedAssetExtensions.Contains(Path.GetExtension(path)))
				.Where(path => !contentPaths.Contains(path))
				.ToArray();

			try
			{
				for (var i = 0; i < paths.Length; i++)
				{
					var path = paths[i];
					if (EditorUtility.DisplayCancelableProgressBar(
						"Usability Audit",
						path,
						paths.Length == 0 ? 1 : i / (float) paths.Length))
					{
						warnings.Add("The audit was canceled by the user");
						return false;
					}

					string text;
					try
					{
						text = File.ReadAllText(path);
					}
					catch (Exception exception)
					{
						warnings.Add($"Failed to read {path}: {exception.Message}");
						continue;
					}

					foreach (Match match in SerializableGuidRegex.Matches(text))
					{
						if (!long.TryParse(match.Groups[1].Value, out var low) ||
							!long.TryParse(match.Groups[2].Value, out var high))
							continue;

						if (guidToCandidate.TryGetValue(new SerializableGuid(low, high), out var target))
							roots.Add(target);
					}

					foreach (Match match in UnityGuidRegex.Matches(text))
					{
						if (unityGuidToCandidate.TryGetValue(match.Groups[1].Value, out var target))
							roots.Add(target);
					}
				}
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}

			return true;
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

		private sealed class ContentReferenceScanner
		{
			private readonly ContentScriptableObject _context;
			private readonly ContentEntryScriptableObject _owner;
			private readonly IReadOnlyDictionary<SerializableGuid, ContentEntryScriptableObject> _guidToCandidate;
			private readonly IReadOnlyDictionary<ContentEntryScriptableObject, HashSet<ContentEntryScriptableObject>> _edges;
			private readonly ISet<ContentEntryScriptableObject> _roots;
			private readonly ICollection<string> _warnings;
			private readonly HashSet<object> _visited = new(ReferenceEqualityComparer.Instance);

			public ContentReferenceScanner(
				ContentScriptableObject context,
				ContentEntryScriptableObject owner,
				IReadOnlyDictionary<SerializableGuid, ContentEntryScriptableObject> guidToCandidate,
				IReadOnlyDictionary<ContentEntryScriptableObject, HashSet<ContentEntryScriptableObject>> edges,
				ISet<ContentEntryScriptableObject> roots,
				ICollection<string> warnings)
			{
				_context = context;
				_owner = owner;
				_guidToCandidate = guidToCandidate;
				_edges = edges;
				_roots = roots;
				_warnings = warnings;
			}

			public void Scan(object target)
			{
				Scan(target, target?.GetType(), _context.name);
			}

			private void Scan(object target, Type targetType, string path)
			{
				if (target == null || targetType == null)
					return;

				if (target is IContentReference reference)
				{
					if (!reference.IsEmpty() && !reference.IsSingle)
						Register(reference.Guid);
					return;
				}

				if (target is SerializableGuid guid)
				{
					Register(guid);
					return;
				}

				if (target is ContentEntryScriptableObject contentAsset && !ReferenceEquals(target, _context))
				{
					Register(contentAsset);
					return;
				}

				if (IsTerminal(targetType))
					return;

				if (target is UnityEngine.Object && !ReferenceEquals(target, _context))
					return;

				if (targetType.IsClass && !_visited.Add(target))
					return;

				if (target is IDictionary dictionary)
				{
					foreach (DictionaryEntry entry in dictionary)
					{
						Scan(entry.Key, entry.Key?.GetType(), $"{path}[key]");
						Scan(entry.Value, entry.Value?.GetType(), $"{path}[{entry.Key}]");
					}
					return;
				}

				if (target is IEnumerable enumerable && target is not string)
				{
					var index = 0;
					foreach (var item in enumerable)
					{
						Scan(item, item?.GetType(), $"{path}[{index}]");
						index++;
					}
					return;
				}

				foreach (var field in GetSerializableFields(targetType))
				{
					if (field.Name == nameof(ContentDatabaseScriptableObject.scriptableObjects))
						continue;

					try
					{
						var value = field.GetValue(target);
						Scan(value, value?.GetType() ?? field.FieldType, $"{path}.{field.Name}");
					}
					catch (Exception exception)
					{
						_warnings.Add(
							$"Failed to read {AssetDatabase.GetAssetPath(_context)}::{path}.{field.Name}: {exception.Message}");
					}
				}
			}

			private void Register(SerializableGuid guid)
			{
				if (_guidToCandidate.TryGetValue(guid, out var target))
					Register(target);
			}

			private void Register(ContentEntryScriptableObject target)
			{
				if (!target)
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

		private static IEnumerable<FieldInfo> GetSerializableFields(Type type)
		{
			while (type != null && type != typeof(object))
			{
				foreach (var field in type.GetFields(
					BindingFlags.Instance |
					BindingFlags.Public |
					BindingFlags.NonPublic |
					BindingFlags.DeclaredOnly))
				{
					if (IsSerializableField(field))
						yield return field;
				}

				type = type.BaseType;
			}
		}

		private static bool IsSerializableField(FieldInfo field)
		{
			if (field.IsStatic || field.IsInitOnly || field.IsLiteral || field.IsNotSerialized)
				return false;

			return field.IsPublic ||
				field.GetCustomAttribute<SerializeField>() != null ||
				field.GetCustomAttribute<SerializeReference>() != null;
		}

		private static bool IsTerminal(Type type)
		{
			type = Nullable.GetUnderlyingType(type) ?? type;

			return type.IsPrimitive ||
				type.IsEnum ||
				type == typeof(string) ||
				type == typeof(decimal) ||
				type == typeof(DateTime) ||
				type == typeof(TimeSpan) ||
				type == typeof(Guid);
		}

		private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
		{
			public static readonly ReferenceEqualityComparer Instance = new();

			public new bool Equals(object x, object y) => ReferenceEquals(x, y);
			public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
		}
	}
}
