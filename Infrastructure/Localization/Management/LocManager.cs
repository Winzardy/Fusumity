using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Sapientia;

namespace Localization
{
	public partial class LocManager : StaticProvider<LocalizationResolver>
	{
		// ReSharper disable once InconsistentNaming
		private static LocalizationResolver resolver
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _instance;
		}

		public static bool IsInitialized
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => resolver != null;
		}


		public static string CurrentLocaleCode => resolver.CurrentLocaleCode;

		/// <inheritdoc cref="LocalizationResolver.CurrentLanguage"/>
		public static string CurrentLanguage => resolver.CurrentLanguage;

		// Нет обработки нулевого LocalizationResolver, так как такой нужны явно нет
		public static event Action<string> CurrentLocaleCodeUpdated
		{
			add => resolver.CurrentLocaleCodeUpdated += value;
			remove => resolver.CurrentLocaleCodeUpdated -= value;
		}

		public static bool Has(string key) => resolver.Has(key);

		/// <returns>Перевод слова по ключу (текущего выбранного языка)</returns>
		public static string Get(string key, string defaultValue = null) => resolver.Get(key, defaultValue);

		public static void SetLanguage(string localeCode)
			=> resolver.SetLanguage(localeCode);

		public static IEnumerable<string> GetAllLocaleCodes()
			=> resolver.GetAllLocaleCodes();

		public static IEnumerable<string> GetAllLanguages()
			=> resolver.GetAllLanguages();
	}
}
