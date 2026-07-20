using System;
using System.Text;
using Fusumity.Editor;
using Fusumity.Utility;
using Sapientia.Pooling;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Content.Editor
{
	internal sealed class ContentValidationReportWindow : OdinEditorWindow
	{
		private const string WINDOW_NAME = "Content Validation Report";
		private const float COPY_BUTTON_WIDTH = 18;

		[Flags]
		private enum SeverityFilter
		{
			Errors = 1 << 0,
			Warnings = 1 << 1
		}

		private const SeverityFilter ALL_SEVERITIES = SeverityFilter.Errors | SeverityFilter.Warnings;

		private SeverityFilter _severityFilter = ALL_SEVERITIES;
		private ContentValidationReport _report;
		private int _reportGeneration = -1;

		[ShowInInspector, Searchable, PropertyOrder(0)]
		[ShowIf(nameof(ShowErrors))]
		[LabelText("@ErrorsLabel")]
		[ListDrawerSettings(
			OnTitleBarGUI = nameof(DrawErrorsTitleBar),
			NumberOfItemsPerPage = 100,
			HideAddButton = true,
			HideRemoveButton = true,
			DraggableItems = false)]
		private ContentValidationReportRow[] _errorRows = Array.Empty<ContentValidationReportRow>();

		[ShowInInspector, Searchable, PropertyOrder(1), PropertySpace(4)]
		[ShowIf(nameof(ShowWarnings))]
		[LabelText("@WarningsLabel")]
		[ListDrawerSettings(
			OnTitleBarGUI = nameof(DrawWarningsTitleBar),
			NumberOfItemsPerPage = 100,
			HideAddButton = true,
			HideRemoveButton = true,
			DraggableItems = false)]
		private ContentValidationReportRow[] _warningRows = Array.Empty<ContentValidationReportRow>();

		private bool ShowErrors { get => (_severityFilter & SeverityFilter.Errors) != 0; }
		private bool ShowWarnings { get => (_severityFilter & SeverityFilter.Warnings) != 0; }

		private string SeverityFilterLabel
		{
			get => _severityFilter switch
			{
				SeverityFilter.Errors => nameof(SeverityFilter.Errors),
				SeverityFilter.Warnings => nameof(SeverityFilter.Warnings),
				_ => $"{nameof(SeverityFilter.Errors)}, {nameof(SeverityFilter.Warnings)}"
			};
		}

		private string ErrorsLabel { get => $"✕ Errors ({_errorRows.Length})"; }
		private string WarningsLabel { get => $"⚠ Warnings ({_warningRows.Length})"; }

		internal static void ShowAfterValidation()
		{
			EditorApplication.delayCall -= OpenAfterValidation;
			EditorApplication.delayCall += OpenAfterValidation;
		}

		private static void OpenAfterValidation()
		{
			if (Application.isBatchMode || ContentValidator.LastReport == null)
				return;

			var window = GetWindow<ContentValidationReportWindow>(WINDOW_NAME);
			window.minSize = new Vector2(760, 420);
			window.RebuildRows(ContentValidator.LastReport);
			window.Show();
			window.Repaint();
		}

		protected override void OnEnable()
		{
			base.OnEnable();

			if (ContentValidator.LastReport != null)
			{
				RebuildRows(ContentValidator.LastReport);
				return;
			}

			EditorApplication.delayCall -= CloseWithoutReport;
			EditorApplication.delayCall += CloseWithoutReport;
		}

		protected override void OnDestroy()
		{
			EditorApplication.delayCall -= CloseWithoutReport;
			ContentValidator.ClearLastReport();
			base.OnDestroy();
		}

		private void CloseWithoutReport()
		{
			if (this && ContentValidator.LastReport == null)
				Close();
		}

		protected override void OnBeginDrawEditors()
		{
			SirenixEditorGUI.BeginHorizontalToolbar();
			if (SirenixEditorGUI.ToolbarButton(" Re-validate"))
			{
				ContentValidator.Validate();
				GUIUtility.ExitGUI();
			}

			GUILayout.FlexibleSpace();

			if (GUILayout.Button(SeverityFilterLabel, EditorStyles.toolbarDropDown, GUILayout.Width(170)))
			{
				var menu = new GenericMenu();
				menu.AddItem(new GUIContent(nameof(SeverityFilter.Errors)), ShowErrors,
					() => ToggleSeverity(SeverityFilter.Errors));
				menu.AddItem(new GUIContent(nameof(SeverityFilter.Warnings)), ShowWarnings,
					() => ToggleSeverity(SeverityFilter.Warnings));
				menu.ShowAsContext();
			}

			SirenixEditorGUI.EndHorizontalToolbar();

			var report = ContentValidator.LastReport;
			if (report == null)
			{
				SirenixEditorGUI.InfoMessageBox("Run Content Validation to create a report");
				return;
			}

			DrawSummary(report);
		}

		private void ToggleSeverity(SeverityFilter severity)
		{
			var severityFilter = (_severityFilter ^ severity) & ALL_SEVERITIES;
			if (severityFilter == 0)
				return;

			_severityFilter = severityFilter;
			Repaint();
		}

		private void DrawErrorsTitleBar()
		{
			DrawCopyButton(_errorRows, "Copy all errors");
		}

		private void DrawWarningsTitleBar()
		{
			DrawCopyButton(_warningRows, "Copy all warnings");
		}

		private static void DrawCopyButton(ContentValidationReportRow[] rows, string tooltip)
		{
			using (new EditorGUI.DisabledScope(rows.Length == 0))
			{
				if (SirenixEditorGUI.ToolbarButton(SdfIconType.Clipboard))
					CopyRows(rows);

				var rect = GUILayoutUtility.GetLastRect();
				GUI.Label(rect, new GUIContent(string.Empty, tooltip), GUIStyle.none);
				EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
			}
		}

		private static void CopyRows(ContentValidationReportRow[] rows)
		{
			using (StringBuilderPool.Get(out var builder))
			{
				for (var i = 0; i < rows.Length; i++)
				{
					if (i > 0)
						builder.AppendLine().AppendLine();

					rows[i].AppendTo(builder);
				}

				GUIUtility.systemCopyBuffer = builder.ToString();
			}
		}

		private static void DrawSummary(ContentValidationReport report)
		{
			var messageType = !report.IsComplete
				? MessageType.Warning
				: report.IsValid
					? report.WarningCount > 0 ? MessageType.Warning : MessageType.Info
					: MessageType.Error;
			var status = !report.IsComplete
				? "Validation is running"
				: report.WasCanceled
					? "Validation canceled"
					: report.IsValid
						? "Validation passed"
						: "Validation failed";

			EditorGUILayout.HelpBox(
				$"{status}    Errors: {report.ErrorCount}    Warnings: {report.WarningCount}",
				messageType);
		}

		private void RebuildRows(ContentValidationReport report)
		{
			if (report == null)
			{
				_report = null;
				_reportGeneration = -1;
				_errorRows = Array.Empty<ContentValidationReportRow>();
				_warningRows = Array.Empty<ContentValidationReportRow>();
				return;
			}

			if (ReferenceEquals(_report, report) && _reportGeneration == report.Generation)
				return;

			using (ListPool<ContentValidationReportRow>.Get(out var errorRows))
			using (ListPool<ContentValidationReportRow>.Get(out var warningRows))
			{
				var entries = report.Entries;
				for (var i = 0; i < entries.Count; i++)
				{
					var entry = entries[i];
					var row = new ContentValidationReportRow(entry);
					if (entry.Severity == ContentValidationSeverity.Error)
						errorRows.Add(row);
					else
						warningRows.Add(row);
				}

				_errorRows = errorRows.ToArray();
				_warningRows = warningRows.ToArray();
			}

			_report = report;
			_reportGeneration = report.Generation;
		}

		[Serializable]
		internal readonly struct ContentValidationReportRow
		{
			private readonly string _message;
			private readonly string _path;
			private readonly Object _context;

			public string Message { get => _message; }

			public string Path { get => _path; }

			public Object Context { get => _context; }

			public ContentValidationReportRow(ContentValidationReportEntry entry)
			{
				_message = entry.Message;
				_context = entry.Context;
				_path = entry.Path;
			}

			internal void CopyToClipboard()
			{
				using (StringBuilderPool.Get(out var builder))
				{
					AppendTo(builder);
					GUIUtility.systemCopyBuffer = builder.ToString();
				}
			}

			internal void AppendTo(StringBuilder builder)
			{
				builder
					.Append(_context ? _context.name : "null")
					.Append(" path: ")
					.AppendLine(_path)
					.Append(_message);
			}
		}
	}

	internal sealed class ContentValidationReportRowDrawer :
		OdinValueDrawer<ContentValidationReportWindow.ContentValidationReportRow>
	{
		private const float SPACING = 4;
		private const float MIN_ASSET_WIDTH = 180;
		private const float MAX_ASSET_WIDTH = 320;
		private const float PATH_LABEL_WIDTH = 34;
		private const float COPY_BUTTON_WIDTH = 18;

		private bool _expanded;

		protected override void DrawPropertyLayout(GUIContent label)
		{
			var row = ValueEntry.SmartValue;
			var rowRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);

			var foldoutRect = rowRect;
			foldoutRect.width = SirenixEditorGUI.FoldoutWidth;
			_expanded = SirenixEditorGUI.Foldout(foldoutRect, _expanded, GUIContent.none);

			var assetRect = rowRect;
			assetRect.xMin = foldoutRect.xMax + SPACING;
			assetRect.width = Mathf.Clamp(rowRect.width * 0.35f, MIN_ASSET_WIDTH, MAX_ASSET_WIDTH);
			GUIHelper.PushGUIEnabled(false);
			EditorGUI.ObjectField(assetRect, GUIContent.none, row.Context, typeof(Object), false);
			GUIHelper.PopGUIEnabled();

			var pathLabelRect = rowRect;
			pathLabelRect.xMin = assetRect.xMax + SPACING;
			pathLabelRect.width = PATH_LABEL_WIDTH;
			EditorGUI.LabelField(pathLabelRect, "path:", EditorStyles.miniLabel);

			var pathRect = rowRect;
			pathRect.xMin = pathLabelRect.xMax;
			pathRect.xMax -= COPY_BUTTON_WIDTH + SPACING;
			EditorGUI.LabelField(pathRect, row.Path, EditorStyles.miniLabel);

			var copyRect = rowRect;
			copyRect.xMin = copyRect.xMax - COPY_BUTTON_WIDTH;

			if (FusumityEditorGUILayout.SuffixSDFButton(copyRect, SdfIconType.Clipboard))
				row.CopyToClipboard();


			GUI.Label(copyRect, new GUIContent(string.Empty, "Copy validation entry"), GUIStyle.none);
			EditorGUIUtility.AddCursorRect(copyRect, MouseCursor.Link);

			if (SirenixEditorGUI.BeginFadeGroup(this, _expanded))
			{
				EditorGUILayout.BeginHorizontal();
				GUILayout.Space(SirenixEditorGUI.FoldoutWidth + SPACING);
				EditorGUILayout.BeginVertical();
				FusumityEditorGUILayout.BeginCardBox(Color.black
					.WithAlpha(0.666f));
				GUILayout.Label(GUIHelper.TempContent(row.Message), SirenixGUIStyles.MultiLineLabel);
				FusumityEditorGUILayout.EndCardBox();
				EditorGUILayout.EndVertical();
				EditorGUILayout.EndHorizontal();
			}

			SirenixEditorGUI.EndFadeGroup();
		}
	}
}
