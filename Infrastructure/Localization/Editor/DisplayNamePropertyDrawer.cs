using UnityEditor;
using UnityEngine;

namespace Localization.Editor
{
	[CustomPropertyDrawer(typeof(DisplayName))]
	public class DisplayNameMetadataDrawer : PropertyDrawer
	{
		private GUIContent _label = new("Display Name");

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var nameProp = property.FindPropertyRelative(nameof(DisplayName.name));
			EditorGUI.PropertyField(position, nameProp, _label);
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
			=> EditorGUI.GetPropertyHeight(property.FindPropertyRelative(nameof(DisplayName.name)));
	}
}
