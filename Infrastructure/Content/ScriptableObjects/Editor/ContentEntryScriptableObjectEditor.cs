using Sapientia.Extensions;
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
		private const string GENERATE_CONSTANTS_LABEL = "Generate Constants";
		private const string GENERATE_CONSTANTS_TOOLTIP_FORMAT = "Сгенерировать константы для типа: {0}";

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

			var buttonLabel = new GUIContent(GENERATE_CONSTANTS_LABEL, GENERATE_CONSTANTS_TOOLTIP_FORMAT.Format(type.GetNiceName()));
			if (SirenixEditorGUI.SDFIconButton(rect, buttonLabel, SdfIconType.Magic, IconAlignment.RightEdge, style))
			{
				ContentConstantGenerator.Generate(type, ContentDatabaseEditorUtility.GetScriptableObjectsByType(type), fullLog: true);
			}
		}
	}
}
