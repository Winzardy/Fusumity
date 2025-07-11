#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Localization;
using UnityEngine.Localization.Tables;

namespace Localization
{
	public partial class LocalizationResolver
	{
		private const string DEFAULT_LOCALE_CODE = "ru";

		internal static string GetEditor(string key, string localeCode = null)
		{
			var collection = LocalizationEditorSettings.GetStringTableCollection(DEFAULT_TABLE_NAME);

			if (!collection)
				return null;

			var locale = localeCode != null
				? LocalizationEditorSettings.GetLocale(localeCode)
				: LocalizationEditorSettings.GetLocale(DEFAULT_LOCALE_CODE);

			if (!locale)
				locale = LocalizationEditorSettings.ActiveLocalizationSettings.GetSelectedLocale();

			if (!locale)
				locale = LocalizationEditorSettings.ActiveLocalizationSettings.GetAvailableLocales().Locales.FirstOrDefault();

			if (!locale)
				return null;

			var table = collection.GetTable(locale.Identifier) as StringTable;

			return table ? table.GetEntry(key)?.Value : null;
		}

		internal static string CurrentLocaleCodeEditor
		{
			get
			{
				var locale = LocalizationEditorSettings.ActiveLocalizationSettings.GetSelectedLocale();
				return locale ? locale.Identifier.Code : GetAllLocalCodesEditor().FirstOrDefault();
			}
		}

		internal static string GetLanguageEditor(string localeCode)
		{
			var locale = LocalizationEditorSettings.GetLocale(localeCode);
			return locale ? locale.LocaleName : localeCode;
		}

		internal static IEnumerable<string> GetAllLocalCodesEditor()
			=> LocalizationEditorSettings.ActiveLocalizationSettings.GetAvailableLocales().Locales.Select(x => x.Identifier.Code);

		internal static IEnumerable<string> GetAllKeysEditor()
		{
			var tableCollection = LocalizationEditorSettings.GetStringTableCollection(DEFAULT_TABLE_NAME);

			if (!tableCollection)
				yield break;

			foreach (var entry in tableCollection.SharedData.Entries)
				yield return entry.Key;
		}
	}
}
#endif
