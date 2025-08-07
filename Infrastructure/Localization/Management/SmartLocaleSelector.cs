using System;
using System.Linq;
using Fusumity.Utility;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Scripting;

namespace Localization
{
	[Preserve]
	public class SmartLocaleSelector : IStartupLocaleSelector
	{
		private const string SYSTEM_SAVE_KEY = "system_selected_locale";
		private const string PLAYER_SAVE_KEY = "player_selected_locale";

		private SystemLocaleSelector _systemLocaleSelector;

		public Locale GetStartupLocale(ILocalesProvider availableLocales)
		{
			_systemLocaleSelector ??= FindOrCreateSystemLocaleSelector();
			var systemLocale = _systemLocaleSelector.GetStartupLocale(availableLocales);
			if (systemLocale != null)
			{
				var systemLocaleCode = systemLocale.Identifier.Code;
				if (LocalSave.Has(SYSTEM_SAVE_KEY))
				{
					var systemSaveData = LocalSave.Load<LocaleSaveData>(SYSTEM_SAVE_KEY);

					if (systemSaveData.localeCode != systemLocaleCode)
					{
						SystemSave(systemLocaleCode);
						return systemLocale;
					}

					if (LocalSave.Has(PLAYER_SAVE_KEY))
					{
						var saveData = LocalSave.Load<LocaleSaveData>(PLAYER_SAVE_KEY);

						if (systemSaveData.timestamp > saveData.timestamp)
							return systemLocale;

						var savedLocale = availableLocales.Locales
						   .FirstOrDefault(l => l.Identifier.Code == saveData.localeCode);
						return savedLocale ? savedLocale : systemLocale;
					}
				}
				else
				{
					SystemSave(systemLocaleCode);
				}

				return systemLocale;
			}

			if (LocalSave.Has(PLAYER_SAVE_KEY))
			{
				var saveData = LocalSave.Load<LocaleSaveData>(PLAYER_SAVE_KEY);

				var savedLocale = availableLocales.Locales
				   .FirstOrDefault(l => l.Identifier.Code == saveData.localeCode);
				if (savedLocale != null)
					return savedLocale;
			}

			return null;
		}

		private SystemLocaleSelector FindOrCreateSystemLocaleSelector()
		{
			foreach (var selector in LocalizationSettings.StartupLocaleSelectors)
			{
				if (selector is SystemLocaleSelector systemLocaleSelector)
					return systemLocaleSelector;
			}

			return new SystemLocaleSelector();
		}

		public static void PlayerSave(string localeCode)
		{
			LocalSave.Save(PLAYER_SAVE_KEY, new LocaleSaveData(localeCode, CurrentTimestamp));
		}

		private static void SystemSave(string localeCode)
		{
			LocalSave.Save(SYSTEM_SAVE_KEY, new LocaleSaveData(localeCode, CurrentTimestamp));
		}

		private static long CurrentTimestamp => DateTimeOffset.UtcNow.Ticks;
	}

	[Serializable]
	public struct LocaleSaveData
	{
		public string localeCode;
		public long timestamp;

		public LocaleSaveData(string localeCode, long timestamp)
		{
			this.localeCode = localeCode;
			this.timestamp = timestamp;
		}
	}
}
