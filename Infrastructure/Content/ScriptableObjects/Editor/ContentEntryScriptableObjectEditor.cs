using UnityEditor;

namespace Content.ScriptableObjects.Editor
{
	[CustomEditor(typeof(ContentEntryScriptableObject), true)]
	[CanEditMultipleObjects]
	public sealed class ContentEntryScriptableObjectEditor : ContentScriptableObjectEditor
	{
		public override void OnInspectorGUI() => DrawContentEntryInspector();
	}
}
