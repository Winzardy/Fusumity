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
		private (Image image, IAssetReferenceEntry assetRef) _single;
		private Dictionary<Image, IAssetReferenceEntry> _imageToAssetRef;

		private bool _disposed;

		public void Dispose()
		{
			_disposed = true;

			_single.assetRef?.Release();

			if (_imageToAssetRef.IsNullOrEmpty())
				return;

			foreach (var entry in _imageToAssetRef.Values)
				entry.Release();

			_imageToAssetRef?.ReleaseToStaticPool();
			_imageToAssetRef = null;
		}

		public void TrySetSprite(Image image, IAssetReferenceEntry entry, Action callback = null, bool disableDuringLoad = false)
		{
			if (image == null || entry.IsEmptyOrInvalid())
				return;

			if (disableDuringLoad)
			{
				image.enabled = false;
				callback += () => image.enabled = true;
			}

			SetSprite(image, entry, callback);
		}

		public void SetSprite(IEnumerable<Image> images, IAssetReferenceEntry entry)
		{
			foreach (var placeholder in images)
				SetSprite(placeholder, entry);
		}

		public void SetSprite(Image image, IAssetReferenceEntry entry, Action callback = null)
		{
			if (_single.image != null)
			{
				if (TryUpdateSingle(image, entry, callback))
					return;
			}
			else
			{
				_single = (image, assetRef: entry);
				LoadAndPlaceAsync(image, entry, callback).Forget();
				return;
			}

			_imageToAssetRef ??= DictionaryPool<Image, IAssetReferenceEntry>.Get();

			if (_imageToAssetRef.TryGetValue(image, out var entryByPlaceholder))
			{
				//Какой смысл если там и так такой ассет
				if (entryByPlaceholder == entry)
				{
					callback?.Invoke();
					return;
				}

				entryByPlaceholder?.Release();
			}

			_imageToAssetRef[image] = entry;
			LoadAndPlaceAsync(image, entry, callback).Forget();
		}

		private bool TryUpdateSingle(Image image, IAssetReferenceEntry entry, Action callback = null)
		{
			if (_single.image == image)
			{
				//Какой смысл если там и так такой ассет
				if (_single.assetRef == entry)
				{
					callback?.Invoke();
					return true;
				}

				_single.assetRef?.Release();
				_single.assetRef = entry;
				LoadAndPlaceAsync(image, entry, callback)
					.Forget();
				return true;
			}

			return false;
		}

		public void SetSpinner(ISpinner spinner)
		{
			_spinner = spinner;
		}

		public void TryCancelOrClear(Image image)
		{
			if (_single.image == image)
			{
				_single.assetRef?.Release();
				_single.assetRef = null;

				_single.image = null;
				return;
			}

			if (_imageToAssetRef.IsNullOrEmpty())
				return;

			if (!_imageToAssetRef.TryGetValue(image, out var entryByPlaceholder))
				return;

			entryByPlaceholder?.Release();
			_imageToAssetRef.Remove(image);
		}

		private async UniTaskVoid LoadAndPlaceAsync(Image image, IAssetReferenceEntry entry, Action callback = null)
		{
			_spinner?.SetActive(true);
			var sprite = await entry.LoadAsync<Sprite>();

			if (_disposed || !image)
			{
				_spinner?.SetActive(false);
				return;
			}

			image.sprite = sprite;
			callback?.Invoke();
			_spinner?.SetActive(false);
		}
	}
}
