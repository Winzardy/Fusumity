using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AssetManagement.AddressableAssets;
using Cysharp.Threading.Tasks;
using Fusumity.Reactive;
using Fusumity.Utility;
using Sapientia.Extensions;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Localization
{
	/// <remarks>
	/// ⚠️ Важно: Нет поддержки работы с несколькими Table. Текущие потребности проекта и не требует. Будет в будущем
	/// </remarks>
	public partial class LocalizationResolver : IDisposable
	{
		private LocTableReference _tableReference;

		private StringTable _table;
		private Locale _currentLocale;
		private AsyncOperationHandle<StringTable> _handle;

		public string CurrentLocaleCode => _currentLocale.Identifier.Code;

		/// <summary>
		/// Display Name, то что мы показываем пользователю
		/// </summary>
		public string CurrentLanguage => GetDisplayName();

		public event Action<string> CurrentLocaleCodeUpdated;

		public LocalizationResolver(in LocTableReference tableReference)
		{
			_tableReference = tableReference;
		}

		public async UniTask InitializeAsync(CancellationToken token = default)
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

			if (key.IsNullOrEmpty())
				throw LocalizationDebug.NullException("Key cannot be null or empty!");

			var entry = _table.GetEntry(key);

			if (entry == null)
			{
				LocalizationDebug.LogWarning($"Could not find valid localization for [ {CurrentLanguage}/{key} ]");
				return defaultValue ?? $"#{key.ToUpper()}#".ColorText(Color.red);
			}

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
			var handle = LocalizationSettings.StringDatabase.GetTableAsync(_tableReference.id, locale);

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
				LocalizationDebug.LogError($"Not found table by name [ {_tableReference} ] for locale [ {_currentLocale.LocaleName} ]",
					this);

			_currentLocale = LocalizationSettings.SelectedLocale;
			CurrentLocaleCodeUpdated?.Invoke(_currentLocale.Identifier.Code);

			SmartLocaleSelector.PlayerSave(_currentLocale.Identifier.Code);
		}

		private string GetDisplayName()
		{
			return _currentLocale.Metadata.HasMetadata<DisplayName>()
				? _currentLocale.Metadata.GetMetadata<DisplayName>().name
				: _currentLocale.LocaleName;
		}
	}

	[Serializable]
	[Metadata(AllowedTypes = MetadataType.Locale, AllowMultiple = false, MenuItem = "Display Name")]
	public class DisplayName : IMetadata
	{
		public string name;
	}
}
