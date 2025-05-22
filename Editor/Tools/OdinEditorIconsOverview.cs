using System.Reflection;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

public class OdinEditorIconsOverview : EditorWindow
{
	private Vector2 _scroll;

	[MenuItem("Tools/Odin/Inspector/EditorIcons Overview")]
	public static void ShowWindow() => GetWindow<OdinEditorIconsOverview>("EditorIcons Overview");

	private void OnGUI()
	{
		_scroll = GUILayout.BeginScrollView(_scroll);
		var fields = typeof(EditorIcons).GetProperties(BindingFlags.Static | BindingFlags.Public);
		foreach (var field in fields)
		{
			if (field.GetValue(null) is not EditorIcon {ActiveGUIContent: not null} editorIcon)
				continue;

			GUILayout.BeginHorizontal();
			GUILayout.Label(editorIcon.ActiveGUIContent.image, GUILayout.Width(32), GUILayout.Height(32));
			GUILayout.Label(field.Name, GUILayout.Height(32));
			GUILayout.EndHorizontal();
		}

		GUILayout.EndScrollView();
	}
}
