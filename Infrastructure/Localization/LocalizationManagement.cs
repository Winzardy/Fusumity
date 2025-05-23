using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Loc = I2.Loc.LocalizationManager;

namespace Localizations
{
	public class LocalizationManagement : IDisposable
	{
		private string _languageCode;

		public string CurrentLanguage => Loc.CurrentLanguage;

		public event Action<string> CurrentLanguageUpdated;

		public LocalizationManagement() => Loc.OnLocalizeEvent += OnLocalizedEvent;

		public void Dispose() => Loc.OnLocalizeEvent -= OnLocalizedEvent;

		private void OnLocalizedEvent()
		{
			_languageCode = Loc.CurrentLanguageCode;

			//Хак чтобы решить проблему с получением перевода
			//https://www.notion.so/winzardy/Bug-11c1c74f154380788a9ce4a8de23e68e?pvs=4
			CheckValidateAndInvokeUpdated();
		}

		internal bool Contains(string key) => Loc.TryGetTranslation(key, out _);

		internal string Get(string key, string defaultValue = null) => Loc.GetTranslation(key) ?? defaultValue ?? key;

		internal void SetLanguage(string language, bool rememberLanguage, bool force)
			=> Loc.SetLanguageAndCode(language, GetLanguageCode(language), rememberLanguage, force);

		internal string GetLanguageCode(string language) => Loc.GetLanguageCode(language);

		internal List<string> GetAllLanguagesCode(bool allowRegions, bool skipDisabled)
			=> Loc.GetAllLanguagesCode(allowRegions, skipDisabled);

		internal List<string> GetAllLanguages(bool skipDisabled) => Loc.GetAllLanguages(skipDisabled);

		private void CheckValidateAndInvokeUpdated()
		{
			if (Validate())
				InvokeCurrentLanguageUpdated();
			else
				InvokeCurrentLanguageUpdatedAsync().Forget();
		}

		private async UniTaskVoid InvokeCurrentLanguageUpdatedAsync()
		{
			Loc.LocalizeAll(true);

			await UniTask.NextFrame();

			CheckValidateAndInvokeUpdated();
		}

		private bool Validate()
		{
			if (Loc.GetTermData(LocKeys.VERSION) == null)
			{
				Debug.LogError("Error found term for validate...");
				return true;
			}

			return Get(LocKeys.VERSION) != LocKeys.VERSION;
		}

		private void InvokeCurrentLanguageUpdated() => CurrentLanguageUpdated?.Invoke(_languageCode);

#if UNITY_EDITOR
		internal static string GetEditor(string key, string language = null) =>
			Loc.GetTranslation(key, overrideLanguage: language);

		internal static string CurrentLanguageEditor => Loc.CurrentLanguage;

		internal static List<string> GetAllLanguagesEditor(bool skipDisabled) => Loc.GetAllLanguages(skipDisabled);

		internal static List<string> GetAllKeysEditor() => Loc.GetTermsList();
#endif
	}
}
