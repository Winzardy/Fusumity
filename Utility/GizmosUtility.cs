#if UNITY_EDITOR

using Sapientia;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Utility
{
	public static class GizmosUtility
	{
		private static readonly GizmoTextArgs _defaultTextArgs = GizmoTextArgs.Default();

		public static void DrawLabel(string text, Vector3 worldPos) => DrawLabel(text, worldPos, _defaultTextArgs);
		public static void DrawLabel(string text, Vector3 worldPos, GizmoTextArgs args)
		{
			var fontStyle = new GUIStyle(GUI.skin.label);
			fontStyle.fontSize = args.fontSize;
			fontStyle.fontStyle = args.fontStyle;
			fontStyle.alignment = args.alignment;

			if (args.distanceScale)
			{
				fontStyle.fontSize = Mathf.Clamp((int)(args.fontSize / HandleUtility.GetHandleSize(worldPos)), 1, 128);
			}

			if (args.bgColor != Color.clear)
			{
				Handles.BeginGUI();
				{
					var size = fontStyle.CalcSize(new GUIContent(text));
					var screenPos = HandleUtility.WorldToGUIPoint(worldPos);
					var rect = new Rect(screenPos.x - size.x / 2 - 2, screenPos.y - size.y / 2 - 2, size.x + 4, size.y + 4);
					var oldColor = GUI.color;

					GUI.color = args.bgColor;
					GUI.DrawTexture(rect, Texture2D.whiteTexture);
					GUI.color = oldColor;
				}
				Handles.EndGUI();
			}

			var color = GUI.color;
			GUI.color = args.textColor;
			Handles.Label(worldPos, text, fontStyle);
			GUI.color = color;
		}

		public static void DrawSector(Color color, float degrees, Range<float> range, Matrix4x4 matrix, int numSegments = 16)
		{
			var gizmoMatrix = Gizmos.matrix;
			var gizmoColor = Gizmos.color;

			var radians = degrees * Mathf.Deg2Rad;

			Vector3[] points = new Vector3[2 + 2 * numSegments];
			for (int i = 0; i <= numSegments; ++i)
			{
				var angle = -0.5f * radians + i * radians / numSegments;
				points[i] = new Vector3(
					range.min * Mathf.Sin(angle),
					0,
					range.min * Mathf.Cos(angle));
			}

			for (int i = 0; i <= numSegments; ++i)
			{
				var angle = -0.5f * radians + (numSegments - i) * radians / numSegments;
				points[numSegments + i + 1] = new Vector3(
					range.max * Mathf.Sin(angle),
					0,
					range.max * Mathf.Cos(angle));
			}

			Gizmos.color = color;
			Gizmos.matrix = matrix;

			DrawLineLoop(points);

			Gizmos.matrix = gizmoMatrix;
			Gizmos.color = gizmoColor;
		}

		public static void DrawRectangle(Color color, Rect rect, Matrix4x4 matrix, float elevation = 0)
		{
			var gizmoMatrix = Gizmos.matrix;
			var gizmoColor = Gizmos.color;

			var points = new[]
			{
				new Vector3(rect.xMin, elevation, rect.yMin),
				new Vector3(rect.xMin, elevation, rect.yMax),
				new Vector3(rect.xMax, elevation, rect.yMax),
				new Vector3(rect.xMax, elevation, rect.yMin),
			};

			Gizmos.color = color;
			Gizmos.matrix = matrix;

			DrawLineLoop(points);

			Gizmos.matrix = gizmoMatrix;
			Gizmos.color = gizmoColor;
		}


		private static void DrawLineLoop(Vector3[] points)
		{
			for (int i = 0; i < points.Length; i++)
			{
				Gizmos.DrawLine(points[i], points[(i + 1) % points.Length]);
			}
		}
	}

	public struct GizmoTextArgs
	{
		public Color textColor;
		public Color bgColor;
		public int fontSize;
		public FontStyle fontStyle;
		public TextAnchor alignment;
		public bool distanceScale;

		public static GizmoTextArgs Default()
		{
			return new GizmoTextArgs
			{
				textColor = Color.black,
				bgColor = Color.white,
				fontSize = 10,
				fontStyle = FontStyle.Normal,
				alignment = TextAnchor.MiddleCenter,
				distanceScale = false
			};
		}
	}
}
#endif
