using UnityEditor;
using UnityEditor.UI;

namespace UI.Editor
{
	[CustomEditor(typeof(CustomRawImage), false)]
	public class CustomRawImageEditor : RawImageEditor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			
			serializedObject.Update();

			var serializedProperty = serializedObject.FindProperty("_preserveAspect");
			EditorGUILayout.PropertyField(serializedProperty);
			
			serializedObject.ApplyModifiedProperties();
		}
	}
}
