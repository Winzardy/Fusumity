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

			_single.handle?.Release();

			if (_imageToHandle.IsNullOrEmpty())
				return;

			foreach (var entry in _imageToHandle.Values)
				entry?.Release();

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

				handle.Release();
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

				_single.handle.Release();
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
			if(image == null)
				return;

			if (_single.image == image)
			{
				_single.handle?.Release();
				_single = default;
				return;
			}

			if (_imageToHandle.IsNullOrEmpty())
				return;

			if (!_imageToHandle.TryGetValue(image, out var handle))
				return;

			handle?.Release();
			_imageToHandle.Remove(image);
		}

		private bool TryReuseHandle(Image image, SpriteAssignerHandle handle, Action callback, bool disableDuringLoad)
		{
			if (handle.IsLoaded)
			{
				image.sprite = handle.Sprite;
				callback?.Invoke();
				return true;
			}

			if (!handle.IsLoading)
				return false;

			handle.AddCallback(PrepareCallback(image, callback, disableDuringLoad));
			return true;
		}

		private void StartLoading(Image image, SpriteAssignerHandle handle, Action callback, bool disableDuringLoad)
		{
			handle.AddCallback(PrepareCallback(image, callback, disableDuringLoad));
			LoadAndPlaceAsync(image, handle).Forget();
		}

		private static Action PrepareCallback(Image image, Action callback, bool disableDuringLoad)
		{
			if (!disableDuringLoad)
				return callback;

			image.enabled = false;
			callback += () => image.enabled = true;
			return callback;
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
			handle.cts = new CancellationTokenSource();
			var cts = handle.cts;
			var token = cts.Token;

			OnStartLoading();

			try
			{
				var sprite = await handle.spriteRef.LoadAsync(token);

				if (token.IsCancellationRequested || _disposed || image == null || !IsCurrentHandle(image, handle))
					return;

				image.sprite = sprite;
				handle.MarkLoaded(sprite);
				handle.InvokeCallback();
			}
			catch (OperationCanceledException)
			{
			}
			catch (Exception) when (token.IsCancellationRequested || _disposed || image == null || !IsCurrentHandle(image, handle))
			{
			}
			finally
			{
				OnEndLoading();
				handle.ReleaseLoadingCts(cts);
			}

			void OnStartLoading() => _spinner?.Show(cts);
			void OnEndLoading() => _spinner?.Hide(cts);
		}
	}

	public sealed class SpriteAssignerHandle
	{
		public IAssetReference<Sprite> spriteRef;
		public CancellationTokenSource cts;

		private Action _callback;
		private bool _loaded;
		private Sprite _sprite;

		public bool IsLoaded { get => _loaded; }
		public bool IsLoading { get => cts != null && !cts.IsCancellationRequested; }
		public Sprite Sprite { get => _sprite; }

		public SpriteAssignerHandle(IAssetReference<Sprite> spriteRef)
		{
			this.spriteRef = spriteRef;
			cts = null;
		}

		public bool SameAsset(IAssetReference<Sprite> target) => spriteRef.SameAsset(target);
		public void AddCallback(Action callback) => _callback += callback;
		public void MarkLoaded(Sprite sprite)
		{
			_sprite = sprite;
			_loaded = true;
		}

		public void InvokeCallback()
		{
			var callback = _callback;
			_callback = null;
			callback?.Invoke();
		}

		public void ReleaseLoadingCts(CancellationTokenSource loadingCts)
		{
			if (!ReferenceEquals(cts, loadingCts))
				return;

			AsyncUtility.Release(ref cts);
		}

		public void Release()
		{
			_callback = null;
			_loaded = false;
			_sprite = null;

			AsyncUtility.TriggerAndSetNull(ref cts);

			spriteRef?.Release();
			spriteRef = null;
		}
	}
}
