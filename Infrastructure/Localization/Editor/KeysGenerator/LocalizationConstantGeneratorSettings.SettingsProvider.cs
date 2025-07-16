using Fusumity.Editor;
using Fusumity.Utility;
using Sirenix.OdinInspector.Editor;
using UnityEditor;

namespace Localization.Editor
{
	public partial class LocalizationConstantGeneratorSettings
	{
		[MenuItem(LocalizationConstantsGeneratorMenu.CONSTANTS_MENU_PATH + "Settings", priority = 60)]
		private static void OpenGenerateSettings()
		{
			SettingsService.OpenProjectSettings(SETTINGS_PROVIDER_PATH);
		}

		internal const string SETTINGS_PROVIDER_ROOT_PATH = "Project/Localization/";
		private const string SETTINGS_PROVIDER_LABEL = "Constant Generator";
		public const string SETTINGS_PROVIDER_PATH = SETTINGS_PROVIDER_ROOT_PATH + SETTINGS_PROVIDER_LABEL;

		private static OdinEditor _editor;

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
				if (CreateOrUpdateEditor())
					return;

				using (new FusumityEditorGUILayout.SettingsProviderScope())
				{
					_editor.OnInspectorGUI();
				}
			}

			// Возвращает true, если нельзя отрисовывать редактор
			bool CreateOrUpdateEditor()
			{
				if (!Asset)
					return true;

				if (!_editor)
				{
					CreateEditor();
					return true;
				}
				else if (_editor.target != Asset)
				{
					_editor.Destroy();
					CreateEditor();
					return true;
				}

				return false;
			}

			void CreateEditor() => _editor = (OdinEditor) OdinEditor.CreateEditor(Asset, typeof(OdinEditor));
		}
	}
}
