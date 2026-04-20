using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DG.Tweening;
using DG.DOTweenEditor;
using Sirenix.OdinInspector;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using Interval = ZenoTween.Participant.Callbacks.Interval;
using AnimationTweenCallback = ZenoTween.Participant.Callbacks.AnimationTweenCallback;
using AnimationSequenceSourceTween = ZenoTween.Participant.Tweens.AnimationSequenceSourceTween;
using ZenoTween.Utility;

namespace ZenoTween.Editor
{
	// WIP
	public class AnimationSequenceTimelineWindow : OdinEditorIconsOverview
	{
		private const string WINDOW_NAME = "Animation Timeline";
		private const float HEADER_HEIGHT = 24f;
		private const float ROW_HEIGHT = 26f;
		private const float LABEL_WIDTH = 320f;
		private const float MIN_MARKER_WIDTH = 2f;
		private const float DEFAULT_PIXELS_PER_SECOND = 120f;
		private const float MIN_TIMELINE_WIDTH = 480f;
		private const float PLAYHEAD_WIDTH = 2f;

		[SerializeField]
		private Object _context;

		[SerializeField]
		private bool _showCallbacks = true;

		[SerializeField]
		private bool _fitToWindow = true;

		[SerializeField]
		private float _pixelsPerSecond = DEFAULT_PIXELS_PER_SECOND;

		[SerializeField]
		private Vector2 _scroll;

		[SerializeField]
		private float _playheadTime;

		[NonSerialized]
		private AnimationSequence _sequence;

		[NonSerialized]
		private TimelineModel _model;

		[NonSerialized]
		private Tween _previewTween;

		[NonSerialized]
		private float _previewTweenDuration;

		[NonSerialized]
		private bool _previewPlaying;

		[NonSerialized]
		private bool _draggingPlayhead;

		private static readonly Color EVEN_ROW = new(0.17f, 0.17f, 0.17f, 0.18f);
		private static readonly Color ODD_ROW = new(0.17f, 0.17f, 0.17f, 0.08f);
		private static readonly Color GRID_COLOR = new(1f, 1f, 1f, 0.08f);
		private static readonly Color HEADER_BG = new(0.12f, 0.12f, 0.12f, 0.95f);
		private static readonly Color MARKER_COLOR = new(1f, 1f, 1f, 0.55f);

		private GUIStyle _labelStyle;
		private GUIStyle _subLabelStyle;
		private GUIStyle _barStyle;
		private GUIStyle _miniBadgeStyle;

		public static void Open(AnimationSequence sequence, Object context = null)
		{
			if (sequence == null)
				return;

			var window = GetWindow<AnimationSequenceTimelineWindow>(WINDOW_NAME);
			window.titleContent = new GUIContent(WINDOW_NAME, EditorIcons.List.Active);
			window.minSize = new Vector2(760, 320);
			window._sequence = sequence;
			window._context = context;
			window.Rebuild();
			window.RebuildPreview(keepPlayhead: false);
			window.Show();
			window.Focus();
		}

		private void OnEnable()
		{
			titleContent = new GUIContent(WINDOW_NAME);
			EnsureStyles();
		}

		private void OnDisable()
		{
			StopPreview(resetPlayhead: false, resetObjects: false);
		}

		private void OnGUI()
		{
			EnsureStyles();

			if (_sequence == null)
			{
				EditorGUILayout.HelpBox("Sequence is not available anymore. Reopen the viewer from Animation Sequence.", MessageType.Info);
				return;
			}

			Rebuild();
			DrawToolbar();

			if (_model == null || _model.rows.Count == 0)
			{
				EditorGUILayout.HelpBox("Sequence has no visible participants.", MessageType.Info);
				return;
			}

			if (_model.isInfinite)
			{
				EditorGUILayout.HelpBox(
					"Sequence contains infinite playback. Timeline shows the first pass and stretches endless bars to the visible area.",
					MessageType.Info);
			}

			var rows = _showCallbacks
				? _model.rows
				: _model.rows.Where(static x => !x.isCallback).ToList();

			if (rows.Count == 0)
			{
				EditorGUILayout.HelpBox("All visible rows are filtered out.", MessageType.Info);
				return;
			}

			var totalDuration = Mathf.Max(_model.visibleDuration, 0.0001f);
			var viewportRect = GUILayoutUtility.GetRect(10, 100000, 10, 100000, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
			var timelineWidth = _fitToWindow
				? Mathf.Max(MIN_TIMELINE_WIDTH, viewportRect.width - LABEL_WIDTH - 24f)
				: Mathf.Max(MIN_TIMELINE_WIDTH, totalDuration * _pixelsPerSecond);
			var contentWidth = LABEL_WIDTH + timelineWidth;
			var contentHeight = HEADER_HEIGHT + rows.Count * ROW_HEIGHT;
			var contentRect = new Rect(0, 0, contentWidth, contentHeight);

			_scroll = GUI.BeginScrollView(viewportRect, _scroll, contentRect);
			DrawTimeline(contentRect, timelineWidth, totalDuration, rows);
			HandleTimelineInput(new Rect(LABEL_WIDTH, 0, timelineWidth, contentHeight), Mathf.Max(totalDuration, _previewTweenDuration));
			GUI.EndScrollView();
		}

		private void DrawToolbar()
		{
			using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
			{
				GUI.enabled = _context != null;
				EditorGUILayout.ObjectField(_context, typeof(Object), true, GUILayout.Width(220));
				GUI.enabled = true;

				GUILayout.Space(8);
				_showCallbacks = GUILayout.Toggle(_showCallbacks, "Callbacks", EditorStyles.toolbarButton, GUILayout.Width(90));
				_fitToWindow = GUILayout.Toggle(_fitToWindow, "Fit", EditorStyles.toolbarButton, GUILayout.Width(50));

				using (new EditorGUI.DisabledScope(_fitToWindow))
				{
					EditorGUILayout.LabelField("Zoom", GUILayout.Width(36));
					_pixelsPerSecond = EditorGUILayout.Slider(_pixelsPerSecond, 40f, 320f, GUILayout.Width(220));
				}

				GUILayout.Space(8);

				if (GUILayout.Button("Preview", EditorStyles.toolbarButton, GUILayout.Width(62)))
					RebuildPreview(keepPlayhead: true);

				if (GUILayout.Button(_previewPlaying ? "Pause" : "Play", EditorStyles.toolbarButton, GUILayout.Width(52)))
					TogglePreviewPlayback();

				if (GUILayout.Button("Stop", EditorStyles.toolbarButton, GUILayout.Width(42)))
					StopPreview(resetPlayhead: true, resetObjects: true);

				var scrubDuration = Mathf.Max(HasPreview ? _previewTweenDuration : 0f, _model?.visibleDuration ?? 0.1f, 0.1f);
				EditorGUI.BeginChangeCheck();
				var nextPlayheadTime = EditorGUILayout.Slider(_playheadTime, 0f, scrubDuration, GUILayout.Width(220));
				if (EditorGUI.EndChangeCheck())
					SetPlayheadTime(nextPlayheadTime);

				if (GUILayout.Button("Rebuild", EditorStyles.toolbarButton, GUILayout.Width(70)))
				{
					Rebuild();
					if (HasPreview)
						RebuildPreview(keepPlayhead: true);
				}

				GUILayout.FlexibleSpace();
				EditorGUILayout.LabelField(_model?.summary ?? string.Empty, EditorStyles.miniLabel, GUILayout.Width(250));
			}
		}

		private void DrawTimeline(Rect contentRect, float timelineWidth, float totalDuration, List<TimelineRow> rows)
		{
			EditorGUI.DrawRect(new Rect(0, 0, contentRect.width, HEADER_HEIGHT), HEADER_BG);
			EditorGUI.DrawRect(new Rect(LABEL_WIDTH, 0, 1, contentRect.height), GRID_COLOR * 1.5f);

			DrawRuler(timelineWidth, totalDuration, rows.Count);

			for (int i = 0; i < rows.Count; i++)
			{
				var row = rows[i];
				var y = HEADER_HEIGHT + i * ROW_HEIGHT;
				var rowRect = new Rect(0, y, contentRect.width, ROW_HEIGHT);

				EditorGUI.DrawRect(rowRect, i % 2 == 0 ? EVEN_ROW : ODD_ROW);
				DrawRowLabel(row, rowRect);
				DrawRowBar(row, y, timelineWidth, totalDuration);
			}

			DrawPlayhead(timelineWidth, totalDuration, contentRect.height);
		}

		private void DrawRuler(float timelineWidth, float totalDuration, int rowCount)
		{
			var tickStep = GetTickStep(totalDuration, timelineWidth);
			var lineHeight = HEADER_HEIGHT + rowCount * ROW_HEIGHT;

			for (float time = 0; time <= totalDuration + tickStep * 0.5f; time += tickStep)
			{
				var x = LABEL_WIDTH + NormalizeTime(time, totalDuration) * timelineWidth;
				EditorGUI.DrawRect(new Rect(x, 0, 1, lineHeight), GRID_COLOR);
				GUI.Label(new Rect(x + 4, 2, 80, HEADER_HEIGHT - 4), FormatTime(time), EditorStyles.miniLabel);
			}

			GUI.Label(new Rect(8, 2, LABEL_WIDTH - 16, HEADER_HEIGHT - 4), "Participant", EditorStyles.boldLabel);
		}

		private void DrawRowLabel(TimelineRow row, Rect rowRect)
		{
			var indent = 14f * row.depth;
			var iconRect = new Rect(8 + indent, rowRect.y + 7, 10, 10);
			var labelRect = new Rect(iconRect.xMax + 6, rowRect.y + 3, LABEL_WIDTH - iconRect.xMax - 14, 16);
			var subRect = new Rect(labelRect.x, rowRect.y + 15, labelRect.width, 10);

			EditorGUI.DrawRect(iconRect, row.color);

			var label = row.label;
			if (!string.IsNullOrEmpty(row.badge))
				label = $"{label}  {row.badge}";

			if (GUI.Button(labelRect, new GUIContent(label, row.tooltip), GUIStyle.none) && row.target != null)
				EditorGUIUtility.PingObject(row.target);

			GUI.Label(labelRect, label, _labelStyle);

			if (!string.IsNullOrEmpty(row.details))
				GUI.Label(subRect, row.details, _subLabelStyle);
		}

		private void DrawRowBar(TimelineRow row, float y, float timelineWidth, float totalDuration)
		{
			var x = LABEL_WIDTH + NormalizeTime(row.start, totalDuration) * timelineWidth;
			var barY = y + 4f;
			var barHeight = ROW_HEIGHT - 8f;

			if (row.isMarker || Mathf.Approximately(row.duration, 0f))
			{
				var markerRect = new Rect(x, barY, MIN_MARKER_WIDTH, barHeight);
				EditorGUI.DrawRect(markerRect, row.color);
				EditorGUI.DrawRect(new Rect(markerRect.x + 1f, barY, 1f, barHeight), MARKER_COLOR);
				return;
			}

			var width = row.isInfinite
				? Mathf.Max(MIN_MARKER_WIDTH, timelineWidth - (x - LABEL_WIDTH))
				: Mathf.Max(MIN_MARKER_WIDTH, NormalizeTime(row.duration, totalDuration) * timelineWidth);

			var barRect = new Rect(x, barY, width, barHeight);
			EditorGUI.DrawRect(barRect, row.color);
			GUI.Box(barRect, GUIContent.none, _barStyle);

			if (row.isInfinite)
			{
				GUI.Label(barRect, new GUIContent("∞", row.tooltip), _miniBadgeStyle);
				return;
			}

			if (barRect.width > 42f)
				GUI.Label(barRect, new GUIContent(FormatTime(row.duration), row.tooltip), _miniBadgeStyle);
		}

		private void Rebuild()
		{
			_model = AnimationSequenceTimelineBuilder.Build(_sequence);
			_playheadTime = Mathf.Clamp(_playheadTime, 0f, Mathf.Max(_model.visibleDuration, _previewTweenDuration, 0f));
			if (_context == null)
				_context = AnimationSequenceTimelineBuilder.TryResolveBestContext(_sequence);
		}

		private bool HasPreview => _previewTween != null && _previewTween.IsActive();

		private void TogglePreviewPlayback()
		{
			if (_previewPlaying)
			{
				if (HasPreview)
					_previewTween.Pause();
				_previewPlaying = false;
				return;
			}

			if (!EnsurePreview())
				return;

			var previewTime = Mathf.Clamp(_playheadTime, 0f, Mathf.Max(_previewTweenDuration, 0f));
			_previewTween.GotoWithCallbacks(previewTime);
			_previewTween.Play();
			_previewPlaying = true;
		}

		private bool EnsurePreview()
		{
			if (HasPreview)
				return true;

			Rebuild();

			StopPreview(resetPlayhead: false, resetObjects: false);

			_previewTween = _sequence.ToEditorPreviewSequence(_context);
			if (_previewTween == null)
				return false;

			_previewTween.SetAutoKill(false);
			_previewTweenDuration = _previewTween.Duration(includeLoops: true);
			DOTweenEditorPreview.PrepareTweenForPreview(_previewTween);
			DOTweenEditorPreview.Start(PreviewEditorUpdate);
			_previewTween.Pause();
			ApplyPlayheadToPreview();
			return true;
		}

		private void RebuildPreview(bool keepPlayhead)
		{
			var playhead = keepPlayhead ? _playheadTime : 0f;
			StopPreview(resetPlayhead: false, resetObjects: true);
			Rebuild();
			_playheadTime = playhead;
			EnsurePreview();
		}

		private void StopPreview(bool resetPlayhead, bool resetObjects)
		{
			_previewPlaying = false;
			_draggingPlayhead = false;

			if (DOTweenEditorPreview.isPreviewing)
				DOTweenEditorPreview.Stop(resetObjects);

			_previewTween.KillSafe();
			_previewTween = null;
			_previewTweenDuration = 0f;

			if (resetPlayhead)
				_playheadTime = 0f;

			Repaint();
		}

		private void PreviewEditorUpdate()
		{
			if (_context != null)
				EditorUtility.SetDirty(_context);

			if (!HasPreview)
			{
				_previewPlaying = false;
				return;
			}

			if (_previewPlaying)
			{
				var maxTime = Mathf.Max(_previewTweenDuration, _model?.visibleDuration ?? 0f);
				_playheadTime = Mathf.Clamp(_previewTween.Elapsed(includeLoops: true), 0f, maxTime);

				if (!_previewTween.IsPlaying())
				{
					_playheadTime = Mathf.Clamp(_previewTween.Elapsed(includeLoops: true), 0f, maxTime);
					_previewPlaying = false;
				}
			}
			else
			{
			}

			Repaint();
		}

		private void SetPlayheadTime(float time, bool rebuildIfNeeded = true)
		{
			var maxTime = Mathf.Max(_model?.visibleDuration ?? 0f, _previewTweenDuration, 0f);
			_playheadTime = Mathf.Clamp(time, 0f, maxTime);

			if (rebuildIfNeeded && !EnsurePreview())
				return;

			ApplyPlayheadToPreview();
		}

		private void ApplyPlayheadToPreview()
		{
			if (!HasPreview)
				return;

			var previewTime = Mathf.Clamp(_playheadTime, 0f, Mathf.Max(_previewTweenDuration, 0f));
			_previewTween.GotoWithCallbacks(previewTime);
			_previewTween.Pause();

			if (_context != null)
				EditorUtility.SetDirty(_context);
		}

		private void HandleTimelineInput(Rect timelineRect, float totalDuration)
		{
			var evt = Event.current;
			if (evt == null)
				return;

			var interactRect = timelineRect;
			interactRect.yMin = 0f;

			if (evt.type == EventType.MouseDown && evt.button == 0 && interactRect.Contains(evt.mousePosition))
			{
				_draggingPlayhead = true;
				if (HasPreview)
					_previewTween.Pause();
				_previewPlaying = false;
				SetPlayheadTime(TimeFromMouse(evt.mousePosition.x, timelineRect, totalDuration));
				evt.Use();
			}
			else if (evt.type == EventType.MouseDrag && _draggingPlayhead)
			{
				SetPlayheadTime(TimeFromMouse(evt.mousePosition.x, timelineRect, totalDuration));
				evt.Use();
			}
			else if ((evt.type == EventType.MouseUp || evt.rawType == EventType.MouseUp) && evt.button == 0)
			{
				_draggingPlayhead = false;
			}
		}

		private void DrawPlayhead(float timelineWidth, float totalDuration, float contentHeight)
		{
			var x = LABEL_WIDTH + NormalizeTime(_playheadTime, totalDuration) * timelineWidth;
			var playheadRect = new Rect(x - PLAYHEAD_WIDTH * 0.5f, 0f, PLAYHEAD_WIDTH, contentHeight);
			EditorGUI.DrawRect(playheadRect, new Color(1f, 0.25f, 0.2f, 0.95f));
			GUI.Label(new Rect(x + 5f, 3f, 64f, HEADER_HEIGHT - 6f), FormatTime(_playheadTime), EditorStyles.whiteMiniLabel);
		}

		private static float TimeFromMouse(float mouseX, Rect timelineRect, float totalDuration)
		{
			var normalized = Mathf.InverseLerp(timelineRect.xMin, timelineRect.xMax, mouseX);
			return normalized * totalDuration;
		}

		private void EnsureStyles()
		{
			_labelStyle ??= new GUIStyle(EditorStyles.label)
			{
				fontSize = 11,
				richText = false,
				alignment = TextAnchor.MiddleLeft
			};

			_subLabelStyle ??= new GUIStyle(EditorStyles.miniLabel)
			{
				fontSize = 9,
				normal = { textColor = new Color(1f, 1f, 1f, 0.55f) }
			};

			_barStyle ??= new GUIStyle(EditorStyles.helpBox)
			{
				padding = new RectOffset(6, 6, 2, 2),
				margin = new RectOffset(0, 0, 0, 0)
			};

			_miniBadgeStyle ??= new GUIStyle(EditorStyles.miniBoldLabel)
			{
				alignment = TextAnchor.MiddleCenter,
				normal = { textColor = Color.white }
			};
		}

		private static float NormalizeTime(float value, float totalDuration)
		{
			if (float.IsInfinity(value))
				return 1f;

			if (totalDuration <= 0f)
				return 0f;

			return Mathf.Clamp01(value / totalDuration);
		}

		private static string FormatTime(float value)
		{
			if (float.IsPositiveInfinity(value))
				return "∞";

			if (value >= 10f)
				return $"{value:0.0}s";

			if (value >= 1f)
				return $"{value:0.00}s";

			return $"{value * 1000f:0}ms";
		}

		private static float GetTickStep(float duration, float width)
		{
			var targetStep = Mathf.Max(0.05f, duration / Mathf.Max(1f, width / 90f));
			float[] variants = { 0.05f, 0.1f, 0.25f, 0.5f, 1f, 2f, 5f, 10f };

			foreach (var variant in variants)
			{
				if (variant >= targetStep)
					return variant;
			}

			return 10f;
		}
	}

	internal static class AnimationSequenceTimelineBuilder
	{
		private const float MIN_SPEED = 0.01f;

		private static readonly MethodInfo CREATE_METHOD = typeof(AnimationTween)
			.GetMethod("Create", BindingFlags.Instance | BindingFlags.NonPublic);

		public static TimelineModel Build(AnimationSequence sequence)
		{
			var visited = new HashSet<AnimationSequence>();
			var rows = new List<TimelineRow>();
			var rawDuration = BuildSequence(sequence, rows, 0, visited);
			TransformChildren(rows, sequence.delay, sequence.speed);
			var duration = ApplyTweenEnvelope(rawDuration, sequence, standaloneRoot: true);
			var maxKnownTime = rows
				.Select(static x => x.start + (x.isInfinite ? 0f : x.duration))
				.DefaultIfEmpty(0f)
				.Max();

			var visibleDuration = float.IsInfinity(duration)
				? Mathf.Max(maxKnownTime, 1f)
				: Mathf.Max(duration, maxKnownTime, 0.1f);

			return new TimelineModel
			{
				rows = rows,
				duration = duration,
				visibleDuration = visibleDuration,
				isInfinite = float.IsInfinity(duration) || rows.Any(static x => x.isInfinite),
				summary = BuildSummary(rows.Count, duration)
			};
		}

		public static Object TryResolveBestContext(AnimationSequence sequence)
		{
			foreach (var participant in sequence.participants ?? Array.Empty<SequenceParticipant>())
			{
				var target = ResolveTarget(participant);
				if (target != null)
					return target;
			}

			return null;
		}

		private static float BuildSequence(AnimationSequence sequence, List<TimelineRow> rows, int depth,
			HashSet<AnimationSequence> visited)
		{
			if (sequence == null)
				return 0f;

			if (!visited.Add(sequence))
			{
				rows.Add(new TimelineRow
				{
					label = "Recursive Animation Sequence",
					details = "Cycle detected, nested content is skipped.",
					depth = depth,
					start = 0f,
					duration = 0f,
					color = new Color(0.85f, 0.3f, 0.25f),
					isMarker = true,
					tooltip = "Recursive reference"
				});
				return 0f;
			}

			var localStartIndex = rows.Count;
			var currentEnd = 0f;
			var lastInsertStart = 0f;
			var pendingEndAnchors = new List<TimelineRow>();
			var parentBoundRows = new List<TimelineRow>();

			foreach (var participant in sequence.participants ?? Array.Empty<SequenceParticipant>())
			{
				if (participant == null)
					continue;

				var layout = CreateLayout(participant, depth, visited);
				var mode = layout.mode;
				var start = 0f;

				switch (mode)
				{
					case TimelineInsertMode.Append:
						start = currentEnd;
						lastInsertStart = start;
						currentEnd = CombineDuration(currentEnd, start, layout.duration);
						break;

					case TimelineInsertMode.Join:
						start = lastInsertStart;
						lastInsertStart = start;
						currentEnd = CombineDuration(currentEnd, start, layout.duration);
						break;

					case TimelineInsertMode.Prepend:
						start = 0f;
						ShiftRows(rows, localStartIndex, layout.duration);
						currentEnd = AddDurations(currentEnd, layout.duration);
						lastInsertStart = 0f;
						break;

					case TimelineInsertMode.SequenceStart:
						start = 0f;
						break;

					case TimelineInsertMode.SequenceEnd:
						layout.row.anchorToEnd = true;
						pendingEndAnchors.Add(layout.row);
						break;
				}

				layout.row.start = start;
				rows.Add(layout.row);
				if (layout.row.stretchToParentDuration)
					parentBoundRows.Add(layout.row);

				foreach (var child in layout.children)
				{
					child.start = AddDurations(start, child.start);
					rows.Add(child);
				}
			}

			foreach (var row in pendingEndAnchors)
				row.start = currentEnd;

			foreach (var row in parentBoundRows)
			{
				row.duration = float.IsInfinity(currentEnd)
					? float.PositiveInfinity
					: Mathf.Max(0f, currentEnd - row.start);
				row.isInfinite = float.IsInfinity(row.duration);
			}

			visited.Remove(sequence);
			return currentEnd;
		}

		private static TimelineParticipantLayout CreateLayout(SequenceParticipant participant, int depth, HashSet<AnimationSequence> visited)
		{
			var row = new TimelineRow
			{
				label = GetParticipantLabel(participant),
				details = GetParticipantDetails(participant),
				depth = depth,
				duration = 0f,
				color = GetParticipantColor(participant),
				target = ResolveTarget(participant),
				isCallback = participant is AnimationTweenCallback,
				tooltip = GetTooltip(participant)
			};

			switch (participant)
			{
				case AnimationSequence sequence:
				{
					var children = new List<TimelineRow>();
					var innerDuration = BuildSequence(sequence, children, depth + 1, visited);
					row.stretchToParentDuration = sequence.IsLoop && sequence.lifetimeByParent;
					row.duration = row.stretchToParentDuration
						? 0f
						: ApplyTweenEnvelope(innerDuration, sequence, standaloneRoot: false);
					row.isInfinite = !row.stretchToParentDuration && float.IsInfinity(row.duration);
					row.badge = BuildTweenBadge(sequence);
					TransformChildren(children, sequence.delay, sequence.speed);

					return new TimelineParticipantLayout
					{
						row = row,
						children = children,
						duration = row.duration,
						mode = row.stretchToParentDuration ? TimelineInsertMode.Join : ToInsertMode(sequence.type)
					};
				}

				case AnimationSequenceSourceTween sourceTween:
				{
					var children = new List<TimelineRow>();
					var sourceDuration = 0f;
					if (sourceTween.source != null && sourceTween.source.sequence != null)
					{
						var sourceRawDuration = BuildSequence(sourceTween.source.sequence, children, depth + 1, visited);
						TransformChildren(children, sourceTween.source.sequence.delay, sourceTween.source.sequence.speed);
						sourceDuration = ApplyTweenEnvelope(sourceRawDuration, sourceTween.source.sequence, standaloneRoot: true);
					}

					row.stretchToParentDuration = sourceTween.IsLoop && sourceTween.lifetimeByParent;
					row.duration = sourceTween.UseType
						? ApplyTweenEnvelope(sourceDuration, sourceTween, standaloneRoot: false)
						: 0f;
					row.isInfinite = !row.stretchToParentDuration && float.IsInfinity(row.duration);
					row.badge = BuildTweenBadge(sourceTween);
					TransformChildren(children, sourceTween.delay, sourceTween.speed);

					return new TimelineParticipantLayout
					{
						row = row,
						children = children,
						duration = row.duration,
						mode = row.stretchToParentDuration ? TimelineInsertMode.Join : sourceTween.UseType ? ToInsertMode(sourceTween.type) : TimelineInsertMode.Join
					};
				}

				case AnimationTween tween:
				{
					row.stretchToParentDuration = tween.IsLoop && tween.lifetimeByParent;
					row.duration = tween.UseType
						? ApplyTweenEnvelope(TryGetRawDuration(tween), tween, standaloneRoot: false)
						: 0f;
					row.isInfinite = !row.stretchToParentDuration && float.IsInfinity(row.duration);
					row.badge = BuildTweenBadge(tween);

					return new TimelineParticipantLayout
					{
						row = row,
						children = new List<TimelineRow>(),
						duration = row.duration,
						mode = tween.UseType ? ToInsertMode(tween.type) : TimelineInsertMode.Join
					};
				}

				case Interval interval:
					row.duration = Mathf.Max(0f, interval.duration);
					row.isInfinite = float.IsInfinity(row.duration);
					return new TimelineParticipantLayout
					{
						row = row,
						children = new List<TimelineRow>(),
						duration = row.duration,
						mode = interval.type == Interval.Type.Prepend ? TimelineInsertMode.Prepend : TimelineInsertMode.Append
					};

				case AnimationTweenCallback callback:
					row.duration = 0f;
					row.isMarker = true;
					row.badge = callback.type.ToString();
					return new TimelineParticipantLayout
					{
						row = row,
						children = new List<TimelineRow>(),
						duration = 0f,
						mode = callback.type switch
						{
							AnimationTweenCallback.Type.Join => TimelineInsertMode.Join,
							AnimationTweenCallback.Type.Prepend => TimelineInsertMode.Prepend,
							AnimationTweenCallback.Type.OnStart => TimelineInsertMode.SequenceStart,
							AnimationTweenCallback.Type.OnComplete => TimelineInsertMode.SequenceEnd,
							AnimationTweenCallback.Type.OnKill => TimelineInsertMode.SequenceEnd,
							_ => TimelineInsertMode.Append
						}
					};

				default:
					row.duration = 0f;
					row.isMarker = true;
					return new TimelineParticipantLayout
					{
						row = row,
						children = new List<TimelineRow>(),
						duration = 0f,
						mode = TimelineInsertMode.Append
					};
			}
		}

		private static float TryGetRawDuration(AnimationTween tween)
		{
			if (tween == null || CREATE_METHOD == null)
				return 0f;

			try
			{
				if (CREATE_METHOD.Invoke(tween, null) is not Tween created || created == null)
					return 0f;

				var duration = created.Duration(includeLoops: true);
				created.Kill(false);
				return Mathf.Max(0f, duration);
			}
			catch
			{
				return 0f;
			}
		}

		private static float ApplyTweenEnvelope(float baseDuration, AnimationTween tween, bool standaloneRoot)
		{
			if (tween == null)
				return 0f;

			if (!standaloneRoot && tween.IsLoop && tween.lifetimeByParent)
				return 0f;

			if (tween.repeat == -1)
				return float.PositiveInfinity;

			var cycles = tween.repeat <= 1 ? 1 : tween.repeat;
			var delay = tween.delay;

			if (standaloneRoot && tween is AnimationSequence sequence && sequence.delayOnce)
				delay = sequence.delay;

			var total = delay + Mathf.Max(0f, baseDuration) * cycles;
			var speed = Mathf.Max(MIN_SPEED, tween.speed);
			return total / speed;
		}

		private static void TransformChildren(List<TimelineRow> children, float delay, float speed)
		{
			if (children == null || children.Count == 0)
				return;

			var scale = 1f / Mathf.Max(MIN_SPEED, speed);

			foreach (var child in children)
			{
				child.start = (delay + child.start) * scale;
				if (!child.isInfinite)
					child.duration *= scale;
			}
		}

		private static string BuildSummary(int rowCount, float duration)
		{
			var durationText = float.IsInfinity(duration)
				? "∞"
				: FormatTime(duration);
			return $"{rowCount} rows  |  total {durationText}";
		}

		private static void ShiftRows(List<TimelineRow> rows, int startIndex, float delta)
		{
			if (delta <= 0f || float.IsInfinity(delta))
				return;

			for (int i = startIndex; i < rows.Count; i++)
				rows[i].start = AddDurations(rows[i].start, delta);
		}

		private static float CombineDuration(float currentEnd, float start, float duration)
		{
			if (float.IsInfinity(currentEnd) || float.IsInfinity(duration))
				return float.PositiveInfinity;

			return Mathf.Max(currentEnd, start + duration);
		}

		private static float AddDurations(float a, float b)
		{
			if (float.IsInfinity(a) || float.IsInfinity(b))
				return float.PositiveInfinity;

			return a + b;
		}

		private static TimelineInsertMode ToInsertMode(AnimationTween.Type type)
		{
			return type switch
			{
				AnimationTween.Type.Prepend => TimelineInsertMode.Prepend,
				AnimationTween.Type.Append => TimelineInsertMode.Append,
				_ => TimelineInsertMode.Join
			};
		}

		private static string GetParticipantLabel(SequenceParticipant participant)
		{
			var baseName = participant.GetType().Name;
			baseName = baseName.Replace("AnimationTween", string.Empty);
			baseName = baseName.Replace("TweenCallback", " Callback");
			baseName = baseName.Replace("Tween", string.Empty);
			baseName = baseName.Replace("SequenceSource", "Sequence Source");
			baseName = ObjectNames.NicifyVariableName(baseName).Trim();

			var target = ResolveTarget(participant);
			return target != null ? $"{baseName} [{target.name}]" : baseName;
		}

		private static string GetParticipantDetails(SequenceParticipant participant)
		{
			return participant switch
			{
				AnimationSequence sequence when sequence.IsLoop && sequence.lifetimeByParent =>
					$"Nested sequence, {sequence.participants?.Length ?? 0} participants, bound to parent lifetime",
				AnimationSequenceSourceTween sourceTween when sourceTween.IsLoop && sourceTween.lifetimeByParent && sourceTween.source != null =>
					$"Source: {sourceTween.source.name}, bound to parent lifetime",
				AnimationSequence sequence => $"Nested sequence, {sequence.participants?.Length ?? 0} participants",
				AnimationSequenceSourceTween sourceTween when sourceTween.source != null => $"Source: {sourceTween.source.name}",
				AnimationTween tween when tween.IsLoop && tween.lifetimeByParent => "Loop is bound to parent lifetime and stretches to parent end",
				AnimationTween tween => $"Mode: {tween.type}",
				Interval interval => $"Interval: {interval.type}",
				AnimationTweenCallback callback => $"Callback: {callback.type}",
				_ => participant.GetType().Name
			};
		}

		private static string BuildTweenBadge(AnimationTween tween)
		{
			if (tween == null)
				return null;

			if (tween.IsLoop && tween.lifetimeByParent)
				return "parent";

			if (tween.repeat == -1)
				return "x∞";

			if (tween.repeat > 1)
				return $"x{tween.repeat}";

			if (!Mathf.Approximately(tween.speed, 1f))
				return $"{tween.speed:0.##}x";

			if (tween.delay > 0f)
				return FormatTime(tween.delay);

			return null;
		}

		private static string GetTooltip(SequenceParticipant participant)
		{
			return participant switch
			{
				AnimationSequence sequence when sequence.IsLoop && sequence.lifetimeByParent =>
					$"{participant.GetType().Name}\nDelay: {sequence.delay:0.###}s\nSpeed: {sequence.speed:0.###}\nLoop lifetime: parent-bound\nVisual duration: until parent end",
				AnimationSequenceSourceTween sourceTween when sourceTween.IsLoop && sourceTween.lifetimeByParent =>
					$"{participant.GetType().Name}\nDelay: {sourceTween.delay:0.###}s\nSpeed: {sourceTween.speed:0.###}\nLoop lifetime: parent-bound\nVisual duration: until parent end",
				AnimationTween tween when tween.IsLoop && tween.lifetimeByParent =>
					$"{participant.GetType().Name}\nDelay: {tween.delay:0.###}s\nSpeed: {tween.speed:0.###}\nLoop lifetime: parent-bound\nVisual duration: until parent end",
				AnimationTween tween => $"{participant.GetType().Name}\nDelay: {tween.delay:0.###}s\nSpeed: {tween.speed:0.###}\nRepeat: {tween.repeat}",
				Interval interval => $"{participant.GetType().Name}\nDuration: {interval.duration:0.###}s",
				AnimationTweenCallback callback => $"{participant.GetType().Name}\nType: {callback.type}",
				_ => participant.GetType().Name
			};
		}

		private static Color GetParticipantColor(SequenceParticipant participant)
		{
			return participant switch
			{
				AnimationSequence => new Color(0.34f, 0.63f, 0.96f, 0.85f),
				AnimationSequenceSourceTween => new Color(0.29f, 0.72f, 0.62f, 0.85f),
				AnimationTween => new Color(0.97f, 0.67f, 0.25f, 0.85f),
				Interval => new Color(0.55f, 0.55f, 0.55f, 0.9f),
				AnimationTweenCallback => new Color(0.86f, 0.41f, 0.71f, 0.95f),
				_ => new Color(0.8f, 0.8f, 0.8f, 0.85f)
			};
		}

		private static Object ResolveTarget(SequenceParticipant participant)
		{
			if (participant == null)
				return null;

			var fields = participant
				.GetType()
				.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
				.OrderBy(static field => GetTargetPriority(field.Name));

			foreach (var field in fields)
			{
				if (!typeof(Object).IsAssignableFrom(field.FieldType))
					continue;

				if (field.GetValue(participant) is Object obj && obj != null)
					return obj;
			}

			return null;
		}

		private static int GetTargetPriority(string fieldName)
		{
			return fieldName switch
			{
				"root" => 0,
				"rectTransform" => 1,
				"graphic" => 2,
				"source" => 3,
				"to" => 4,
				"target" => 5,
				_ => 100
			};
		}

		private static string FormatTime(float value)
		{
			if (float.IsPositiveInfinity(value))
				return "∞";

			if (value >= 10f)
				return $"{value:0.0}s";

			if (value >= 1f)
				return $"{value:0.00}s";

			return $"{value * 1000f:0}ms";
		}
	}

	internal sealed class TimelineModel
	{
		public List<TimelineRow> rows = new();
		public float duration;
		public float visibleDuration;
		public bool isInfinite;
		public string summary;
	}

	internal sealed class TimelineParticipantLayout
	{
		public TimelineRow row;
		public List<TimelineRow> children;
		public float duration;
		public TimelineInsertMode mode;
	}

	internal sealed class TimelineRow
	{
		public string label;
		public string details;
		public string badge;
		public string tooltip;
		public int depth;
		public float start;
		public float duration;
		public bool isInfinite;
		public bool isMarker;
		public bool isCallback;
		public bool anchorToEnd;
		public bool stretchToParentDuration;
		public Color color;
		public Object target;
	}

	internal enum TimelineInsertMode
	{
		Join,
		Append,
		Prepend,
		SequenceStart,
		SequenceEnd
	}
}
