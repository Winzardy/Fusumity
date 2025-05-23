using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace UI.Editor
{
	public class RectOffsetAttributeDrawer : OdinAttributeDrawer<RectOffsetAttribute, Vector4>
	{
		private bool _isVisible;

		protected override void DrawPropertyLayout(GUIContent label)
		{
			var rect = ValueEntry.SmartValue;

			_isVisible = SirenixEditorGUI.Foldout(_isVisible, label);

			var originalIndent = EditorGUI.indentLevel;
			EditorGUI.indentLevel++;

			if (SirenixEditorGUI.BeginFadeGroup(this, _isVisible))
			{
				rect.x = SirenixEditorFields.FloatField("Left", rect.x);
				rect.y = SirenixEditorFields.FloatField("Bottom", rect.y);
				rect.z = SirenixEditorFields.FloatField("Right", rect.z);
				rect.w = SirenixEditorFields.FloatField("Top", rect.w);
			}

			SirenixEditorGUI.EndFadeGroup();

			ValueEntry.SmartValue = rect;
			EditorGUI.indentLevel = originalIndent;
		}
	}
}
