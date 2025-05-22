using Fusumity.Utility;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Content.Editor
{
	public partial class ContentConstantGeneratorSettings
	{
		private const string SETTINGS_PROVIDER_ROOT_PATH = "Project/Content/";
		private const string SETTINGS_PROVIDER_LABEL = "Constant Generator";
		private const string SETTINGS_PROVIDER_PATH = SETTINGS_PROVIDER_ROOT_PATH + SETTINGS_PROVIDER_LABEL;

		private static OdinEditor _editor;
		private static GUIStyle _style;

		[SettingsProvider]
		public static SettingsProvider CreateProvider()
		{
			return new SettingsProvider(SETTINGS_PROVIDER_PATH, SettingsScope.Project)
			{
				label = SETTINGS_PROVIDER_LABEL,

				guiHandler = OnGUI
			};

			void OnGUI(string _)
			{
				CreateOrUpdateEditor();

				SirenixEditorGUI.BeginIndentedVertical(style);
				{
					_editor.OnInspectorGUI();
				}
				SirenixEditorGUI.EndIndentedVertical();
			}

			void CreateOrUpdateEditor()
			{
				if (!_editor)
				{
					CreateEditor();
				}
				else if (_editor.target != Asset)
				{
					_editor.Destroy();
					CreateEditor();
				}
			}

			void CreateEditor() => _editor = (OdinEditor) OdinEditor.CreateEditor(Asset, typeof(OdinEditor));
		}

		private static GUIStyle style
		{
			get
			{
				if (_style == null)
				{
					_style = new GUIStyle();
					var offset = _style.margin;
					offset.top += 3;
					offset.left += 10;
					offset.right += 5;
					_style.margin = offset;
				}

				return _style;
			}
		}
	}
}
