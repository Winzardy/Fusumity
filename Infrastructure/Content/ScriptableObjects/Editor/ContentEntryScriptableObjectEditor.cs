using Sapientia.Extensions;
using AssetManagement.AddressableAssets.Editor;
using Content.Editor;
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
		private const string FIND_REFERENCES_LABEL = "Find Reference";
		private const float HEADER_BUTTON_RIGHT_PADDING = 5f;
		private const float HEADER_BUTTON_SPACING = 4f;
		private const float HEADER_BUTTON_Y_OFFSET = -6f;
		private const float HEADER_BUTTON_HEIGHT = 18f;
		private const float GENERATE_CONSTANTS_BUTTON_WIDTH = 150f;
		private const float FIND_REFERENCES_BUTTON_WIDTH = 125f;

		private static GUIStyle _headerButtonStyle;
		private static GUIStyle HeaderButtonStyle => _headerButtonStyle ??= new GUIStyle(SirenixGUIStyles.MiniButton);

		public override void OnInspectorGUI()
		{
			DrawContentEntryInspector();
		}

		protected override void OnHeaderGUI()
		{
			base.OnHeaderGUI();

			DrawHeaderButtons();

			if (target.IsAddressable())
			{
				target.RemoveFromAddressables();
				ContentDebug.LogWarning($"'{target.name}' cannot be Addressable and was automatically removed");
			}
		}

		private void DrawHeaderButtons()
		{
			var scrObj = target as ContentEntryScriptableObject;
			if (scrObj == null)
				return;

			var rightOffset = HEADER_BUTTON_RIGHT_PADDING;
			if (_documentationButtonDrawn)
				rightOffset += DOCUMENTATION_BUTTON_WIDTH + HEADER_BUTTON_SPACING;

			DrawGenerateConstantButton(scrObj, ref rightOffset);
			DrawFindReferencesButton(scrObj, ref rightOffset);
		}

		private void DrawGenerateConstantButton(ContentEntryScriptableObject scrObj, ref float rightOffset)
		{
			if (!scrObj.HasContentGeneration())
				return;

			var type = scrObj.ValueType;
			var buttonLabel = new GUIContent(GENERATE_CONSTANTS_LABEL, GENERATE_CONSTANTS_TOOLTIP_FORMAT.Format(type.GetNiceName()));
			if (DrawHeaderButton(buttonLabel, SdfIconType.Magic, GENERATE_CONSTANTS_BUTTON_WIDTH, ref rightOffset))
			{
				ContentConstantGenerator.Generate(type, ContentDatabaseEditorUtility.GetScriptableObjectsByType(type), fullLog: true);
			}
		}

		private void DrawFindReferencesButton(ContentEntryScriptableObject scrObj, ref float rightOffset)
		{
			var buttonLabel = new GUIContent(FIND_REFERENCES_LABEL, ContentSearchProvider.GetFindReferencesTooltip(scrObj));
			if (DrawHeaderButton(buttonLabel, SdfIconType.FileEarmarkBreakFill, FIND_REFERENCES_BUTTON_WIDTH, ref rightOffset))
				ContentSearchProvider.OpenReferenceSearch(scrObj);
		}

		private static bool DrawHeaderButton(GUIContent buttonLabel, SdfIconType icon, float width, ref float rightOffset)
		{
			var rect = GUILayoutUtility.GetLastRect();
			rect = rect.AlignRight(width).AlignBottom(HEADER_BUTTON_HEIGHT);
			rect = rect.AddPosition(-rightOffset, HEADER_BUTTON_Y_OFFSET);
			rightOffset += width + HEADER_BUTTON_SPACING;

			return SirenixEditorGUI.SDFIconButton(rect, buttonLabel, icon, IconAlignment.RightEdge, HeaderButtonStyle);
		}
	}
}
