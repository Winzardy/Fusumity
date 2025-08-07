// using System;
// using System.Linq;
// using Fusumity.Utility;
// using UnityEngine;
// using UnityEngine.Localization;
// using UnityEngine.Localization.Settings;
// using UnityEngine.Scripting;
//
// namespace Localization
// {
// 	[Preserve]
// 	public class LocalSaveSelector : SystemLocaleSelector, IDisposable
// 	{
// 		private const string SAVE_KEY = "selected_locale_code";
//
// 		public void Dispose()
// 		{
// 			LocManager.CurrentLocaleCodeUpdated -= OnCurrentLocaleCodeUpdated;
// 		}
//
// 		protected override SystemLanguage GetApplicationSystemLanguage()
// 		{
// 			return base.GetApplicationSystemLanguage();
// 		}
//
// 		public Locale GetStartupLocale(ILocalesProvider availableLocales)
// 		{
// 			LocManager.CurrentLocaleCodeUpdated += OnCurrentLocaleCodeUpdated;
//
// 			if (LocalSave.Has(SAVE_KEY))
// 			{
// 				var savedCode = PlayerPrefs.GetString(SAVE_KEY);
// 				var savedLocale = availableLocales.Locales
// 				   .FirstOrDefault(l => l.Identifier.Code == savedCode);
// 				if (savedLocale != null)
// 					return savedLocale;
// 			}
//
// 			return LocalizationSettings.SelectedLocale;
// 		}
//
// 		private void OnCurrentLocaleCodeUpdated(string localCode) => LocalSave.Save(SAVE_KEY, localCode);
// 	}
// }
