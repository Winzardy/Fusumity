using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AssetManagement.AddressableAssets;
using Cysharp.Threading.Tasks;
using Fusumity.Reactive;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Localization
{
	public partial class LocalizationResolver : IDisposable
	{
		private const string DEFAULT_TABLE_NAME = "Default";

		private StringTable _table;
		private Locale _locale;
		private AsyncOperationHandle<StringTable> _handle;

		public string CurrentLocaleCode => _locale.Identifier.Code;
		public string CurrentLanguage => _locale.LocaleName;

		public event Action<string> CurrentLocaleCodeUpdated;

		public async UniTask InitializeAsync(CancellationToken token, params string[] tableNames)
		{
			await LocalizationSettings.InitializationOperation
			   .WithCancellation(token);
			await SetLocale(token);

			LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
		}

		public void Dispose()
		{
			LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
		}

		internal bool Has(string key)
		{
			if (_table == null)
				return false;

			return _table.GetEntry(key) != null;
		}

		internal string Get(string key, string defaultValue = null)
		{
			if (!_table)
				return defaultValue;

			var entry = _table.GetEntry(key);

			if (entry == null)
				return defaultValue; // ?? key;

			return entry.Value;
		}

		internal void SetLanguage(string localeCode)
			=> LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale(localeCode);

		internal IEnumerable<string> GetAllLocaleCodes()
			=> LocalizationSettings.AvailableLocales.Locales.Select(x => x.Identifier.Code);

		internal IEnumerable<string> GetAllLanguages()
			=> LocalizationSettings.AvailableLocales.Locales.Select(x => x.LocaleName);

		private void OnSelectedLocaleChanged(Locale locale)
			=> SetLocale(locale, UnityLifecycle.ApplicationCancellationToken).Forget();

		private async UniTask SetLocale(CancellationToken token = default) => await SetLocale(null, token);

		private async UniTask SetLocale(Locale locale, CancellationToken token = default)
		{
			var handle = LocalizationSettings.StringDatabase.GetTableAsync(DEFAULT_TABLE_NAME, locale);

			await handle
			   .WithCancellation(token);

			if (handle.Status != AsyncOperationStatus.Succeeded)
			{
				handle.ReleaseSafe();
				return;
			}

			_handle.ReleaseSafe();

			_handle = handle;
			_table = handle.Result;

			if (!_table)
				LocalizationDebug.LogError($"Not found table by name [ {DEFAULT_TABLE_NAME} ] for locale [ {_locale.LocaleName} ]", this);

			_locale = locale;
			CurrentLocaleCodeUpdated?.Invoke(locale.Identifier.Code);
		}
	}
}
