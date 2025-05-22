using UnityEditor;

namespace Content.ScriptableObjects.Editor
{
	[CustomEditor(typeof(SingleContentEntryScriptableObject), true)]
	public sealed class SingleContentEntryScriptableObjectEditor : ContentScriptableObjectEditor
	{
		public override void OnInspectorGUI() => DrawContentEntryInspector();
	}
}
