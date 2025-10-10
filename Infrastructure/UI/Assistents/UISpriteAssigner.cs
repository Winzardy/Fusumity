using AssetManagement;
using Cysharp.Threading.Tasks;
using Sapientia.Collections;
using Sapientia.Pooling;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	/// <summary>
	/// Отвечает за подгрузку спрайта и установку его в image placeholder (<see cref="Image"/>)
	/// </summary>
	public class UISpriteAssigner : IDisposable
	{
		private ISpinner _spinner;

		//Чтобы не аллоцировать Dictionary для единичных случаев!
		private (Image placeholder, IAssetReferenceEntry entry) _single;
		private Dictionary<Image, IAssetReferenceEntry> _placeholderToEntry;

		public void Dispose()
		{
			_single.entry?.Release();

			if (_placeholderToEntry.IsNullOrEmpty())
				return;

			foreach (var entry in _placeholderToEntry.Values)
				entry.Release();

			_placeholderToEntry?.ReleaseToStaticPool();
			_placeholderToEntry = null;
		}

		public void TrySetSprite(Image image, IAssetReferenceEntry entry, Action callback = null, bool disableDuringLoad = false)
		{
			if (image == null || entry.IsEmpty())
				return;

			if (disableDuringLoad)
			{
				image.enabled = false;
				callback += () => image.enabled = true;
			}

			SetSprite(image, entry, callback);
		}

		public void SetSprite(IEnumerable<Image> placeholders, IAssetReferenceEntry entry)
		{
			foreach (var placeholder in placeholders)
				SetSprite(placeholder, entry);
		}

		public void SetSprite(Image placeholder, IAssetReferenceEntry entry, Action callback = null)
		{
			if (_single.placeholder != null)
			{
				if (TryUpdateSingle(placeholder, entry, callback))
					return;
			}
			else
			{
				_single = (placeholder, entry);
				LoadAndPlaceAsync(placeholder, entry, callback).Forget();
				return;
			}

			_placeholderToEntry ??= DictionaryPool<Image, IAssetReferenceEntry>.Get();

			if (_placeholderToEntry.TryGetValue(placeholder, out var entryByPlaceholder))
			{
				//Какой смысл если там и так такой ассет
				if (entryByPlaceholder == entry)
				{
					callback?.Invoke();
					return;
				}

				entryByPlaceholder?.Release();
			}

			_placeholderToEntry[placeholder] = entry;
			LoadAndPlaceAsync(placeholder, entry, callback).Forget();
		}

		private bool TryUpdateSingle(Image placeholder, IAssetReferenceEntry entry, Action callback = null)
		{
			if (_single.placeholder == placeholder)
			{
				//Какой смысл если там и так такой ассет
				if (_single.entry == entry)
				{
					callback?.Invoke();
					return true;
				}

				_single.entry?.Release();
				_single.entry = entry;
				LoadAndPlaceAsync(placeholder, entry, callback).Forget();
				return true;
			}

			return false;
		}

		public void SetSpinner(ISpinner spinner)
		{
			_spinner = spinner;
		}

		public void TryCancelOrClear(Image placeholder)
		{
			if (_single.placeholder == placeholder)
			{
				_single.entry?.Release();
				_single.entry = null;

				_single.placeholder = null;
				return;
			}

			if (_placeholderToEntry.IsNullOrEmpty())
				return;

			if (!_placeholderToEntry.TryGetValue(placeholder, out var entryByPlaceholder))
				return;

			entryByPlaceholder?.Release();
			_placeholderToEntry.Remove(placeholder);
		}

		private async UniTaskVoid LoadAndPlaceAsync(Image placeholder, IAssetReferenceEntry entry, Action callback = null)
		{
			_spinner?.SetActive(true);
			var sprite = await entry.LoadAsync<Sprite>();
			placeholder.sprite = sprite;
			callback?.Invoke();
			_spinner?.SetActive(false);
		}
	}
}
