using UnityEditor;

namespace UI.Editor
{
	[CustomEditor(typeof(EmptyGraphic), false)]
	public class EmptyGraphicEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			EditorGUILayout.HelpBox("Catches raycasts for free (almost)", MessageType.Info);
		}
	}
}
