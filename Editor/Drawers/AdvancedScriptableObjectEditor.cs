using Content;
using Sapientia.Extensions;
using Sapientia.Extensions.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor
{
	[CustomEditor(typeof(AdvancedScriptableObject), true)]
	[CanEditMultipleObjects]
	public class AdvancedScriptableObjectEditor : OdinEditor
	{
		protected const string ASSET_LABEL = "Asset";
		protected const string MONO_SCRIPT_PROPERTY_NAME = "m_Script";

		protected const string DOCUMENTATION_LABEL = "Documentation";
		protected const string OPEN_IN_IDE_LABEL = "Open in IDE";

		protected override bool ShouldHideOpenButton() => true;

		protected override void OnHeaderGUI()
		{
			base.OnHeaderGUI();

			if (TryGetDocumentationUrl(out var url))
				DrawDocumentationButton(url);
		}

		public override void OnInspectorGUI()
		{
			DrawAssetReference();
			DrawDefaultInspector();
		}

		protected virtual void DrawAssetReference()
		{
			if (!FusumityEditorGUIHelper.drawAssetReference)
				return;

			var originEnabled = GUI.enabled;
			GUI.enabled = false;
			{
				EditorGUILayout.ObjectField(ASSET_LABEL, target, target.GetType(), false);
			}
			GUI.enabled = originEnabled;
		}

		protected void DrawScriptReference()
		{
			var originEnabled = GUI.enabled;
			GUI.enabled = false;
			{
				var scriptProperty = serializedObject.FindProperty(MONO_SCRIPT_PROPERTY_NAME);
				EditorGUILayout.PropertyField(scriptProperty, true);
			}
			GUI.enabled = originEnabled;
		}

		protected virtual bool TryGetDocumentationUrl(out string url)
		{
			url = string.Empty;

			var type = serializedObject.targetObject.GetType();
			if (type.HasAttribute<DocumentationAttribute>())
			{
				url = type.GetAttribute<DocumentationAttribute>().URL;
				return true;
			}

			if (type.HasAttribute<HelpURLAttribute>())
			{
				url = type.GetAttribute<HelpURLAttribute>().URL;
				return true;
			}

			return false;
		}

		private void DrawDocumentationButton(string url)
		{
			if (url.IsNullOrEmpty())
				return;

			var rect = GUILayoutUtility.GetLastRect();
			rect = rect.AlignRight(125).AlignBottom(18);
			rect = rect.AddPosition(-5, -6);

			var style = new GUIStyle(SirenixGUIStyles.MiniButton);

			if (SirenixEditorGUI.SDFIconButton(rect, DOCUMENTATION_LABEL, SdfIconType.JournalBookmarkFill, IconAlignment.RightEdge, style))
				Help.BrowseURL(url);
		}
	}
}
