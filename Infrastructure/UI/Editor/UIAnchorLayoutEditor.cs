using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace UI.Editor
{
	[CustomEditor(typeof(UIAnchorLayout), false)]
	public class UIAnchorLayoutEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			var originEnabled = GUI.enabled;
			GUI.enabled = false;
			var obj = target as UIAnchorLayout;
			EditorGUILayout.ObjectField("Rect Transform", obj!.rectTransform, typeof(RectTransform), false);
			GUI.enabled = originEnabled;
			EditorGUILayout.HelpBox("Точка привязки для размещения UI-элементов", MessageType.Info);
		}
	}
}
