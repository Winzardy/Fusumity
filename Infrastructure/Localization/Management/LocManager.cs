using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Sapientia;

namespace Localization
{
	public partial class LocManager : StaticProvider<LocalizationResolver>
	{
		private static LocalizationResolver Resolver
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _instance;
		}

		public static bool IsInitialized
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => Resolver != null;
		}


		public static string CurrentLocaleCode => Resolver.CurrentLocaleCode;

		/// <inheritdoc cref="LocalizationResolver.CurrentLanguage"/>
		public static string CurrentLanguage => Resolver.CurrentLanguage;

		// Нет обработки нулевого LocalizationResolver, так как такой нужны явно нет
		public static event Action<string> CurrentLocaleCodeUpdated
		{
			add => Resolver.CurrentLocaleCodeUpdated += value;
			remove => Resolver.CurrentLocaleCodeUpdated -= value;
		}

		public static bool Has(string key) => Resolver.Has(key);

		/// <returns>Перевод слова по ключу (текущего выбранного языка)</returns>
		public static string Get(string key, string defaultValue = null) => Resolver.Get(key, defaultValue);

		public static void SetLanguage(string localeCode)
			=> Resolver.SetLanguage(localeCode);

		public static IEnumerable<string> GetAllLocaleCodes()
			=> Resolver.GetAllLocaleCodes();

		public static IEnumerable<string> GetAllLanguages()
			=> Resolver.GetAllLanguages();
	}
}
