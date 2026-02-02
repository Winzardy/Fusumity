using Sapientia.Extensions;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.Localization.Plugins.Google;
using UnityEngine;

namespace Localization.Editor
{
	public static class LocalizationToolsMenu
	{
		private const string DOC_URL = "https://www.notion.so/winzardy/Localization-38343db9d7f845f4b4361163736075e6?source=copy_link";

		public const string TOOLS_MENU_PATH = "Tools/Localization/";
		public const string CONSTANTS_MENU_PATH = "Tools/Localization/Constants/";

		[MenuItem(TOOLS_MENU_PATH + "\ud83d\uddc2\ufe0f Documentation", priority = 0)]
		public static void OpenDocumentation() => Application.OpenURL(DOC_URL);

		[MenuItem(TOOLS_MENU_PATH + "Settings", priority = 40)]
		private static void OpenGenerateSettings()
		{
			SettingsService.OpenProjectSettings(LocalizationConstantGeneratorSettings.SETTINGS_PROVIDER_ROOT_PATH);
		}

		[MenuItem(TOOLS_MENU_PATH + "Google Sheets/Open in Browser", priority = 80)]
		private static void OpenInBrowser()
		{
			foreach (var table in LocalizationEditorSettings.GetStringTableCollections())
			{
				foreach (var extension in table.Extensions)
				{
					if (extension is not GoogleSheetsExtension googleExt)
						continue;

					if (googleExt.SpreadsheetId.IsNullOrEmpty())
						continue;

					var url = $"https://docs.google.com/spreadsheets/d/{googleExt.SpreadsheetId}";
					Application.OpenURL(url);
					return;
				}
			}

			LocalizationDebug.LogError("Google Sheets Import is not configured");
		}

		[MenuItem(TOOLS_MENU_PATH + "Google Sheets/Pull", priority = 100)]
		private static void Pull()
		{
			foreach (var table in LocalizationEditorSettings.GetStringTableCollections())
			{
				foreach (var extension in table.Extensions)
				{
					if (extension is GoogleSheetsExtension googleExt)
						PullIntoStringTableCollection(table, googleExt);
				}
			}

			void PullIntoStringTableCollection(StringTableCollection table, GoogleSheetsExtension googleExt)
			{
				var sheets = new GoogleSheets(googleExt.SheetsServiceProvider)
				{
					SpreadSheetId = googleExt.SpreadsheetId
				};

				sheets.PullIntoStringTableCollection(
					googleExt.SheetId,
					table,
					googleExt.Columns
				);

				LocalizationDebug.Log($"Pulled from Google Sheets for table [ {table.name} ]", table);

				if(LocalizationAutoGenerationMenu.IsEnable)
					GenerateConstants();
			}
		}

		[MenuItem(CONSTANTS_MENU_PATH + "Generate", priority = 80)]
		private static void GenerateConstants()
		{
			LocalizationConstantGenerator.Generate(LocManager.GetAllKeysEditor());
		}
	}

	[InitializeOnLoad]
	public static class LocalizationAutoGenerationMenu
	{
		public const string PATH = LocalizationToolsMenu.TOOLS_MENU_PATH + "Google Sheets/Auto Generate Constants On Pull";

		private static bool _enable;

		public static bool IsEnable => _enable;

		[MenuItem(PATH, priority = 81)]
		private static void Toggle() => Toggle(!_enable);

		static LocalizationAutoGenerationMenu()
		{
			_enable = EditorPrefs.GetBool(PATH, false);
			EditorApplication.delayCall += () => { Toggle(_enable); };
		}

		private static void Toggle(bool enabled)
		{
			Menu.SetChecked(PATH, enabled);
			EditorPrefs.SetBool(PATH, enabled);
			_enable = enabled;
			UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
		}
	}
}
