using Fusumity.Utility;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace UI.Editor
{
	[CustomEditor(typeof(RadialLayoutGroup))]
	public class RadialLayoutGroupTool : OdinEditor
	{
		private Vector2? pos;

		private void OnSceneGUI()
		{
			var current = (RadialLayoutGroup) target;

			if (!current.IsActive())
				return;

			var originalColor = Handles.color;
			var color = new Color(133 / 256f, 235 / 256f, 126 / 256f, 1);
			Handles.color = color;

			Vector3[] corners = new Vector3[4];
			var rectTransform = current.RectTransformEditor;
			rectTransform.GetWorldCorners(corners);


			var size = corners[2] - corners[0];
			var rect = rectTransform.rect;
			var paddingScale = rect.width != 0 ? size.x / rect.width : size.y / rect.height;
			var normalized = new Vector3 {x = Mathf.Cos(current.startAngle), y = Mathf.Sin(current.startAngle)};
			var localPosition = new Vector3 {x = normalized.x * size.x / 2, y = normalized.y * size.y / 2};
			localPosition += normalized * current.radialPadding * paddingScale;
			var center = (corners[0] + corners[2]) / 2;

			var position = center + localPosition;

			Handles.FreeMoveHandle(position, 5, Vector3.zero, Handles.CircleHandleCap);

			var handleSize = HandleUtility.GetHandleSize(position);
			var style = new GUIStyle(GUI.skin.label);
			style.alignment = TextAnchor.UpperCenter;
			var f = 768 / handleSize;
			style.fontSize = Mathf.Clamp((int) f, 1, 128);
			style.normal.textColor = color.WithAlpha(0.5f);
			Handles.Label(position + Vector3.down * 6.5f, "Start", style);

			Handles.color = originalColor;
		}
	}
}
