#if UNITY_EDITOR
using System.Collections.Generic;

namespace Localization
{
	public partial class LocManager
	{
		public static string GetEditor(string key, string localeCode = null) =>
			LocalizationResolver.GetEditor(key, localeCode);

		public static string CurrentLocaleCodeEditor =>
			LocalizationResolver.CurrentLocaleCodeEditor;

		public static string CurrentLanguageEditor =>
			LocalizationResolver.GetLanguageEditor(CurrentLocaleCodeEditor);

		public static string GetLanguageEditor(string localeCode) =>
			LocalizationResolver.GetLanguageEditor(localeCode);

		public static IEnumerable<string> GetAllLocalCodesEditor() =>
			LocalizationResolver.GetAllLocalCodesEditor();

		public static IEnumerable<string> GetAllKeysEditor() =>
			LocalizationResolver.GetAllKeysEditor();
	}
}
#endif
