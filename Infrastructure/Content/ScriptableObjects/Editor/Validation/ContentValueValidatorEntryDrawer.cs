using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Content.ScriptableObjects.Editor
{
	public class ContentValueValidatorEntryDrawer : OdinValueDrawer<ContentValidationSettings.ContentValueValidatorEntry>
	{
		private const float TOGGLE_WIDTH = 18f;

		protected override void DrawPropertyLayout(GUIContent label)
		{
			var value = ValueEntry.SmartValue;
			var validatorProperty = Property.Children[nameof(ContentValidationSettings.ContentValueValidatorEntry.validator)];

			EditorGUILayout.BeginHorizontal();
			validatorProperty.Draw(label);

			var rect = EditorGUILayout.GetControlRect(false,
				EditorGUIUtility.singleLineHeight,
				GUILayout.Width(TOGGLE_WIDTH));
			rect.y += 1f;

			EditorGUI.BeginChangeCheck();
			var toggle = GUI.Toggle(rect, !value.disable, GUIContent.none);
			if (EditorGUI.EndChangeCheck())
			{
				value.disable = !toggle;
				ValueEntry.SmartValue = value;
				Property.MarkSerializationRootDirty();
			}

			EditorGUILayout.EndHorizontal();
		}
	}
}
