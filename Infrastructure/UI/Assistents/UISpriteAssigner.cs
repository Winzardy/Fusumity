using System;
using System.Collections.Generic;
using AssetManagement;
using Cysharp.Threading.Tasks;
using Sapientia.Collections;
using Sapientia.Pooling;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	/// <summary>
	/// Отвечает за подгрузку спрайта и установку его в image (<see cref="Image"/>)
	/// </summary>
	public class UISpriteAssigner : IDisposable
	{
		private ISpinner _spinner;

		//Чтобы не аллоцировать Dictionary для единичных случаев!
		private (Image image, SpriteAssignerHandle handle) _single;
		private Dictionary<Image, SpriteAssignerHandle> _imageToHandle;

		private bool _disposed;

		public UISpriteAssigner()
		{
		}

		public UISpriteAssigner(ISpinner spinner)
		{
			_spinner = spinner;
		}

		public void Dispose()
		{
			_disposed = true;

			ReleaseHandle(_single.image, _single.handle);
			_single = default;

			if (_imageToHandle == null)
				return;

			foreach (var (image, handle) in _imageToHandle)
				ReleaseHandle(image, handle);

			StaticObjectPoolUtility.ReleaseAndSetNull(ref _imageToHandle);
		}

		public void TrySetSprite(Image image, IAssetReference<Sprite> iconRef, Action callback = null, bool disableDuringLoad = true)
		{
			if (image == null || iconRef.IsEmptyOrInvalid())
				return;

			SetSprite(image, iconRef, callback, disableDuringLoad);
		}

		public void SetSprite(IEnumerable<Image> images, IAssetReference<Sprite> iconRef)
		{
			foreach (var image in images)
				SetSprite(image, iconRef);
		}

		public void SetSprite(Image image, IAssetReference<Sprite> iconRef, Action callback = null, bool disableDuringLoad = true)
		{
			if (_single.image != null)
			{
				if (TryUpdateSingle(image, iconRef, callback, disableDuringLoad))
					return;
			}
			else if (_imageToHandle.IsNullOrEmpty() || !_imageToHandle.ContainsKey(image))
			{
				_single = (image, handle: new SpriteAssignerHandle(iconRef));
				StartLoading(image, _single.handle, callback, disableDuringLoad);
				return;
			}

			_imageToHandle ??= DictionaryPool<Image, SpriteAssignerHandle>.Get();

			if (_imageToHandle.TryGetValue(image, out var handle))
			{
				//Какой смысл если там и так такой ассет
				if (handle.SameAsset(iconRef) && TryReuseHandle(image, handle, callback, disableDuringLoad))
					return;

				ReleaseHandle(image, handle);
			}

			handle = new SpriteAssignerHandle(iconRef);
			_imageToHandle[image] = handle;
			StartLoading(image, handle, callback, disableDuringLoad);
		}

		private bool TryUpdateSingle(Image image, IAssetReference<Sprite> spriteRef, Action callback = null,
			bool disableDuringLoad = true)
		{
			if (_single.image == image)
			{
				//Какой смысл если там и так такой ассет
				if (_single.handle.SameAsset(spriteRef) &&
					TryReuseHandle(image, _single.handle, callback, disableDuringLoad))
					return true;

				ReleaseHandle(image, _single.handle);
				_single.handle = new SpriteAssignerHandle(spriteRef);
				StartLoading(image, _single.handle, callback, disableDuringLoad);
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
			if (image == null)
				return;

			if (_single.image == image)
			{
				ReleaseHandle(image, _single.handle);
				_single = default;
				return;
			}

			if (_imageToHandle.IsNullOrEmpty())
				return;

			if (!_imageToHandle.TryGetValue(image, out var handle))
				return;

			ReleaseHandle(image, handle);
			_imageToHandle.Remove(image);
		}

		private bool TryReuseHandle(Image image, SpriteAssignerHandle handle, Action callback, bool disableDuringLoad)
		{
			if (handle.IsLoaded)
			{
				image.sprite = handle.Sprite;
				if (disableDuringLoad)
					image.enabled = true;
				handle.RestoreImageState(image);
				callback?.Invoke();
				return true;
			}

			if (!handle.IsLoading)
				return false;

			ApplyLoadingState(image, handle, disableDuringLoad);
			handle.AddCallback(callback);
			return true;
		}

		private void StartLoading(Image image, SpriteAssignerHandle handle, Action callback, bool disableDuringLoad)
		{
			ApplyLoadingState(image, handle, disableDuringLoad);
			handle.AddCallback(callback);
			LoadAndPlaceAsync(image, handle).Forget();
		}

		private void ReleaseHandle(Image image, SpriteAssignerHandle handle)
		{
			if (handle == null)
				return;

			handle.RestoreImageState(image);

			if (handle.IsLoading)
				_spinner?.Hide(handle);

			handle.Release();
		}

		private static void ApplyLoadingState(Image image, SpriteAssignerHandle handle, bool disableDuringLoad)
		{
			if (!disableDuringLoad)
			{
				handle.RestoreImageState(image);
				return;
			}

			handle.DisableImage(image);
		}

		private bool IsCurrentHandle(Image image, SpriteAssignerHandle handle)
		{
			if (_single.image == image)
				return ReferenceEquals(_single.handle, handle);

			return !_imageToHandle.IsNullOrEmpty() &&
				_imageToHandle.TryGetValue(image, out var currentHandle) &&
				ReferenceEquals(currentHandle, handle);
		}

		private async UniTaskVoid LoadAndPlaceAsync(Image image, SpriteAssignerHandle handle)
		{
			var spriteRef = handle.spriteRef;
			var assetLoaded = false;

			handle.state = SpriteAssignerHandle.State.Loading;
			_spinner?.Show(handle);

			try
			{
				// Не отменяем загрузку: один Addressables handle может ожидаться несколькими UI
				var sprite = await spriteRef.LoadAsync();
				assetLoaded = true;

				if (_disposed || image == null || !IsCurrentHandle(image, handle))
					return;

				image.sprite = sprite;
				handle.SetSprite(sprite);
				assetLoaded = false;
				handle.RestoreImageState(image);
				handle.InvokeCallback();
			}
			catch (OperationCanceledException)
			{
			}
			catch (Exception) when (_disposed || image == null || !IsCurrentHandle(image, handle))
			{
			}
			finally
			{
				if (assetLoaded)
					spriteRef.Release();

				if (handle.state == SpriteAssignerHandle.State.Loading && !_disposed && image != null && IsCurrentHandle(image, handle))
					handle.RestoreImageState(image);

				_spinner?.Hide(handle);
				if (handle.state == SpriteAssignerHandle.State.Loading)
					handle.state = SpriteAssignerHandle.State.Idle;
			}
		}
	}

	internal sealed class SpriteAssignerHandle
	{
		internal enum State
		{
			Idle,
			Loading,
			Loaded
		}

		public IAssetReference<Sprite> spriteRef;
		public State state;

		private Action _callback;
		private Sprite _sprite;
		private bool _restoreImageEnable;

		public bool IsLoaded { get => state == State.Loaded; }
		public bool IsLoading { get => state == State.Loading; }
		public Sprite Sprite { get => _sprite; }

		public SpriteAssignerHandle(IAssetReference<Sprite> spriteRef)
		{
			this.spriteRef = spriteRef;
		}

		public bool SameAsset(IAssetReference<Sprite> target) => spriteRef.SameAsset(target);
		public void AddCallback(Action callback) => _callback += callback;

		public void SetSprite(Sprite sprite)
		{
			_sprite = sprite;
			state = State.Loaded;
		}

		public void DisableImage(Image image)
		{
			image.enabled = false;
			_restoreImageEnable = true;
		}

		public void RestoreImageState(Image image)
		{
			if (!_restoreImageEnable || image == null)
				return;

			image.enabled = true;
			_restoreImageEnable = false;
		}

		public void InvokeCallback()
		{
			var callback = _callback;
			_callback = null;
			callback?.Invoke();
		}

		public void Release()
		{
			var releaseAsset = IsLoaded;

			state = State.Idle;

			_callback = null;
			_sprite = null;
			_restoreImageEnable = false;

			if (releaseAsset)
				spriteRef?.Release();

			spriteRef = null;
		}
	}
}
