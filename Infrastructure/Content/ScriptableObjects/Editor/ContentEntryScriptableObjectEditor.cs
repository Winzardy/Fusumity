using Sirenix.OdinInspector;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Content.ScriptableObjects.Editor
{
	[CustomEditor(typeof(ContentEntryScriptableObject), true)]
	[CanEditMultipleObjects]
	public sealed class ContentEntryScriptableObjectEditor : ContentScriptableObjectEditor
	{
		public override void OnInspectorGUI()
		{
			DrawContentEntryInspector();
		}

		protected override void OnHeaderGUI()
		{
			base.OnHeaderGUI();

			DrawGenerateConstantButton();
		}

		private void DrawGenerateConstantButton()
		{
			var scrObj = target as ContentEntryScriptableObject;

			if (scrObj == null)
				return;

			if (!scrObj.HasContentGeneration())
				return;

			var rect = GUILayoutUtility.GetLastRect();
			rect = rect.AlignRight(150).AlignBottom(18);
			rect = rect.AddPosition(!_documentationButtonDrawn ? -5 : -DOCUMENTATION_BUTTON_WIDTH - 5 - 4, -6);

			var style = new GUIStyle(SirenixGUIStyles.MiniButton);
			var type = scrObj.ValueType;

			if (SirenixEditorGUI.SDFIconButton(rect, "Generate Constants", SdfIconType.Gear, IconAlignment.RightEdge, style))
			{
				ContentConstantGenerator.Generate(type, ContentDatabaseEditorUtility.GetScriptableObjectsByType(type), fullLog: true);
			}
		}
	}
}
