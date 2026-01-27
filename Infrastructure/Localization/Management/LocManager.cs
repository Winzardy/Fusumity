using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Sapientia;

namespace Localization
{
	public partial class LocManager : StaticAccessor<LocalizationResolver>
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

		public static event Action LocaleUpdated
		{
			add => resolver.LocaleUpdated += value;
			remove => resolver.LocaleUpdated -= value;
		}

		public static bool Has(string key) => resolver.Has(key);

		/// <returns>Перевод слова по ключу (текущего выбранного языка)</returns>
		public static string Get(string key, string defaultValue = null) => resolver.Get(key, defaultValue);
		public static string GetFormatted(string key, params (string tag, string value)[] toReplace)
		{
			var localized = Get(key);
			for (int i = 0; i < toReplace.Length; i++)
			{
				var tuple = toReplace[i];
				localized = localized.Replace(tuple.tag, tuple.value);
			}

			return localized;
		}

		public static void SetLanguage(string localeCode)
			=> resolver.SetLanguage(localeCode);

		public static IEnumerable<string> GetAllLocaleCodes()
			=> resolver.GetAllLocaleCodes();

		public static IEnumerable<string> GetAllLanguages()
			=> resolver.GetAllLanguages();
	}
}
