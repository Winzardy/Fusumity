using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Sapientia;

namespace Localizations
{
	public class Localization : StaticProvider<LocalizationManagement>
	{
		private static LocalizationManagement management
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _instance;
		}

		public static bool IsInitialized
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _instance != null;
		}

		public static string CurrentLanguage
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => management.CurrentLanguage;
		}

		public static event Action<string> CurrentLanguageUpdated
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			add => management.CurrentLanguageUpdated += value;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			remove => management.CurrentLanguageUpdated -= value;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Contains(string key) => management.Contains(key);

		/// <returns>
		/// Перевод слова по ключу (текущего выбранного языка)
		/// </returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string Get(string key, string defaultValue = null)
			=> management.Get(key, defaultValue);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void SetLanguage(string language, bool rememberLanguage, bool force)
			=> management.SetLanguage(language, rememberLanguage, force);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string GetLanguageCode(string language)
			=> management.GetLanguageCode(language);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static List<string> GetAllLanguagesCode(bool allowRegions = false, bool skipDisabled = true)
			=> management.GetAllLanguagesCode(allowRegions, skipDisabled);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static List<string> GetAllLanguages(bool skipDisabled = true) => management.GetAllLanguages(skipDisabled);

#if UNITY_EDITOR
		public static string GetEditor(string key, string language = null) =>
			LocalizationManagement.GetEditor(key, language);

		public static string CurrentLanguageEditor =>
			LocalizationManagement.CurrentLanguageEditor;

		public static List<string> GetAllLanguagesEditor(bool skipDisabled = true) =>
			LocalizationManagement.GetAllLanguagesEditor(skipDisabled);

		public static List<string> GetAllKeysEditor() =>
			LocalizationManagement.GetAllKeysEditor();
#endif
	}
}
