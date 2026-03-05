using AssetManagement.AddressableAssets;
using Cysharp.Threading.Tasks;
using Fusumity.Reactive;
using Fusumity.Utility;
using Sapientia.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Sapientia.Collections;
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
		private bool _initialized;
		private HashMap<LocTableReference, AsyncOperationHandle<StringTable>> _refToHandle;

		private Locale _currentLocale;

		public bool IsInitialized { get => _initialized; }

		public string CurrentLocaleCode { get { return _currentLocale.Identifier.Code; } }

		/// <summary>
		/// Display Name, то что мы показываем пользователю
		/// </summary>
		public string CurrentLanguage { get { return GetDisplayName(); } }

		public event Action<string> CurrentLocaleCodeUpdated;
		public event Action LocaleUpdated;

		public LocalizationResolver(in LocTableReference tableRef)
		{
			_refToHandle = new HashMap<LocTableReference, AsyncOperationHandle<StringTable>>();
			_refToHandle.SetOrAdd(tableRef, default);
		}

		public async UniTask InitializeAsync(CancellationToken token = default)
		{
			await LocalizationSettings.InitializationOperation
				.WithCancellation(token);
			await InitializeLocaleAsync(token);

			LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;

			_initialized = true;
		}

		public async UniTask AddTableAsync(LocTableReference tableRef, CancellationToken token = default)
		{
			if (!_refToHandle.Contains(tableRef))
			{
				_refToHandle.SetOrAdd(tableRef, default);
				await InitializeLocaleAsync(token);
			}
			else
			{
				LocalizationDebug.LogWarning("Already  exists a locale with ID: " + tableRef.id);
			}
		}

		public void Dispose()
		{
			LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
		}

		internal bool Has(string key) => TryGet(key, out _);

		internal bool TryGet(string key, out string value)
		{
			if (!key.IsNullOrEmpty())
			{
				if (!_refToHandle.IsNullOrEmpty())
				{
					foreach (ref var handle in _refToHandle)
					{
						if (handle.IsDefault() || !handle.IsDone)
							continue;

						var entry = handle.Result.GetEntry(key);
						if (entry != null)
						{
							value = entry.Value;
							return true;
						}
					}
				}
			}

			value = null;
			return false;
		}

		internal string Get(string key, string defaultValue = null)
		{
			if (key.IsNullOrEmpty())
				throw LocalizationDebug.NullException("Key cannot be null or empty!");

			if (!TryGet(key, out var value))
				return defaultValue ?? $"#{key.ToUpper()}#".ColorText(UnityColorUtility.ERROR);

			return value;
		}

		internal void SetLanguage(string localeCode)
			=> LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale(localeCode);

		internal IEnumerable<string> GetAllLocaleCodes()
			=> LocalizationSettings.AvailableLocales.Locales.Select(x => x.Identifier.Code);

		internal IEnumerable<string> GetAllLanguages()
			=> LocalizationSettings.AvailableLocales.Locales.Select(x => x.LocaleName);

		private void OnSelectedLocaleChanged(Locale locale)
			=> SetLocaleAsync(locale, UnityLifecycle.ApplicationCancellationToken).Forget();

		private async UniTask InitializeLocaleAsync(CancellationToken token = default) => await SetLocaleAsync(null, token);

		private async UniTask SetLocaleAsync(Locale locale, CancellationToken token = default)
		{
			locale ??= LocalizationSettings.SelectedLocale;
			var full = _currentLocale != locale;
			foreach (var reference in _refToHandle.Keys)
			{
				var currentHandle = _refToHandle[reference];

				// Тут пропускаем загрузку если и так загружено...
				if (currentHandle.IsValid() && currentHandle.Status == AsyncOperationStatus.Succeeded && !full)
					continue;

				var handle = LocalizationSettings.StringDatabase.GetTableAsync(reference.id, locale);

				await handle
					.WithCancellation(token);

				if (!handle.IsValid() || handle.Status != AsyncOperationStatus.Succeeded)
				{
					LocalizationDebug.LogError(
						$"Not found table by name [ {reference} ] for locale [ {locale.LocaleName} ]",
						this);

					handle.ReleaseSafe();
					continue;
				}

				if (currentHandle.IsValid())
					currentHandle.ReleaseSafe();

				_refToHandle.SetOrAdd(reference, handle);
			}

			_currentLocale = locale;
			CurrentLocaleCodeUpdated?.Invoke(_currentLocale.Identifier.Code);
			LocaleUpdated?.Invoke();

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
