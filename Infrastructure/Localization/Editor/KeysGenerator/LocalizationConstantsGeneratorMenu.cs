using Sapientia.Extensions;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.Localization.Plugins.Google;
using UnityEditor.Localization.Reporting;
using UnityEngine;

namespace Localization.Editor
{
	public static class LocalizationConstantsGeneratorMenu
	{
		private const string DOC_URL = "https://www.notion.so/winzardy/Localization-38343db9d7f845f4b4361163736075e6?source=copy_link";

		private const string TOOLS_MENU_PATH = "Tools/Localization/";
		internal const string CONSTANTS_MENU_PATH = "Tools/Localization/Constants/";

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
			}
		}

		[MenuItem(CONSTANTS_MENU_PATH + "Generate", priority = 80)]
		private static void GenerateConstants()
		{
			LocalizationConstantGenerator.Generate(LocManager.GetAllKeysEditor());
		}
	}
}
