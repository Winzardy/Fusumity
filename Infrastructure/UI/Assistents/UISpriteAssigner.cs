using AssetManagement;
using Cysharp.Threading.Tasks;
using Sapientia.Collections;
using Sapientia.Pooling;
using System;
using System.Collections.Generic;
using System.Threading;
using Sapientia.Utility;
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
		private (Image image, SpriteAssignerHandle handle) _single;
		private Dictionary<Image, SpriteAssignerHandle> _imageToHandle;

		private bool _disposed;

		public void Dispose()
		{
			_disposed = true;

			_single.handle.Release();

			if (_imageToHandle.IsNullOrEmpty())
				return;

			foreach (var entry in _imageToHandle.Values)
				entry.Release();

			StaticObjectPoolUtility.ReleaseAndSetNull(ref _imageToHandle);
		}

		public void TrySetSprite(Image image, IAssetReferenceEntry<Sprite> entry, Action callback = null, bool disableDuringLoad = false)
		{
			if (image == null || entry.IsEmptyOrInvalid())
				return;

			TryCancelOrClear(image);

			if (disableDuringLoad)
			{
				image.enabled = false;
				callback += () => image.enabled = true;
			}

			SetSprite(image, entry, callback);
		}

		public void SetSprite(IEnumerable<Image> images, IAssetReferenceEntry<Sprite> entry)
		{
			foreach (var placeholder in images)
				SetSprite(placeholder, entry);
		}

		public void SetSprite(Image image, IAssetReferenceEntry<Sprite> entry, Action callback = null)
		{
			if (_single.image != null)
			{
				if (TryUpdateSingle(image, entry, callback))
					return;
			}
			else
			{
				_single = (image, handle: new SpriteAssignerHandle(entry));
				LoadAndPlaceAsync(image, _single.handle, callback)
					.Forget();
				return;
			}

			_imageToHandle ??= DictionaryPool<Image, SpriteAssignerHandle>.Get();

			if (_imageToHandle.TryGetValue(image, out var pair))
			{
				//Какой смысл если там и так такой ассет
				if (pair.spriteEntry.Equals(entry))
				{
					callback?.Invoke();
					return;
				}

				pair.Release();
			}

			_imageToHandle[image] = new SpriteAssignerHandle(entry);
			LoadAndPlaceAsync(image, _imageToHandle[image], callback).Forget();
		}

		private bool TryUpdateSingle(Image image, IAssetReferenceEntry<Sprite> entry, Action callback = null)
		{
			if (_single.image == image)
			{
				//Какой смысл если там и так такой ассет
				if (_single.handle.spriteEntry.Equals(entry))
				{
					callback?.Invoke();
					return true;
				}

				_single.handle.Release();
				_single.handle = new SpriteAssignerHandle(entry);
				LoadAndPlaceAsync(image, _single.handle, callback)
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
				_single.handle.Release();
				_single.image = null;
				return;
			}

			if (_imageToHandle.IsNullOrEmpty())
				return;

			if (!_imageToHandle.TryGetValue(image, out var handle))
				return;

			handle.Release();
			_imageToHandle.Remove(image);
		}

		private async UniTaskVoid LoadAndPlaceAsync(Image image, SpriteAssignerHandle handle, Action callback = null)
		{
			_spinner?.Show(handle.spriteEntry);

			handle.cts = new CancellationTokenSource();
			var sprite = await handle.spriteEntry.LoadAsync(handle.cts.Token);

			if (handle.cts.IsCancellationRequested || _disposed || !image)
			{
				_spinner?.Hide(handle.spriteEntry);
				return;
			}

			image.sprite = sprite;
			callback?.Invoke();
			_spinner?.Hide(handle.spriteEntry);
		}
	}

	public struct SpriteAssignerHandle
	{
		public IAssetReferenceEntry<Sprite> spriteEntry;
		public CancellationTokenSource cts;

		public SpriteAssignerHandle(IAssetReferenceEntry<Sprite> spriteEntry)
		{
			this.spriteEntry = spriteEntry;
			cts = null;
		}

		public void Release()
		{
			spriteEntry?.Release();
			spriteEntry = null;

			AsyncUtility.TriggerAndSetNull(ref cts);
		}
	}
}
