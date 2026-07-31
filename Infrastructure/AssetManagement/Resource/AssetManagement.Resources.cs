using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sapientia.Extensions;
using Sapientia.Utility;
using UnityEngine;

namespace AssetManagement
{
	using UnityObject = UnityEngine.Object;

	//Данное решение для редких кейсов!!!
	public partial class AssetProvider
	{
		private const string ASSET_IS_NULL_MESSAGE = "Asset is null...";
		private const int UNLOAD_UNUSED_ASSETS_THRESHOLD = 5;

		private static int _unloadUnusedAssetsRequestCount;

		private Dictionary<string, ResourceContainer> _keyToResourceContainer = new(2);

		/// <summary>
		/// Загрузить ресурс (текстура, геймобж, текст и т.д). <br/>
		/// Ресурс обязательно нужно отпустить (release) после использования. (при отмене отпускается автоматически) <see cref="Release(IAssetReference)"/>
		/// </summary>
		/// <typeparam name="T">Тип ресурса</typeparam>
		[Obsolete("Not usually used Resources (Unity), only rare cases when it is really necessary...")]
		public async UniTask<T> LoadResourceAsync<T>(IResourceReference entry, CancellationToken cancellationToken = default,
			IProgress<float> progress = null)
			where T : UnityObject
		{
			return await LoadResourceAsync<T>(entry.Path, cancellationToken, progress);
		}

		/// <summary>
		/// Загрузить ресурс по пути(текстура, геймобж, текст и т.д). <br/>
		/// Ресурс обязательно нужно отпустить (release) после использования. (при отмене отпускается автоматически) <see cref="Release(IAssetReference)"/>
		/// </summary>
		/// <typeparam name="T">Тип ресурса</typeparam>
		[Obsolete("Not usually used Resources (Unity), only rare cases when it is really necessary...")]
		public async UniTask<T> LoadResourceAsync<T>(string path, CancellationToken cancellationToken,
			IProgress<float> progress = null)
			where T : UnityObject
		{
			var container = AcquireResourceContainerByPath<T>(path);
			try
			{
				return await container.LoadAsync<T>(cancellationToken, progress);
			}
			catch
			{
				container.Release();
				throw;
			}
		}

		[Obsolete("Not usually used Resources (Unity), only rare cases when it is really necessary...")]
		public IAssetContainer AcquireAssetContainer<T>(IResourceReference reference)
			where T : UnityObject
		{
			if (reference == null)
				throw new ArgumentNullException(nameof(reference));

			return AcquireResourceContainerByPath<T>(reference.Path);
		}

		[Obsolete("Not usually used Resources (Unity), only rare cases when it is really necessary...")]
		public IAssetContainer AcquireResourceContainer<T>(string path)
			where T : UnityObject =>
			AcquireResourceContainerByPath<T>(path);

		/// <summary>
		/// Синхронно загрузить ресурс. Блокирует поток до готовности (<see cref="Resources.Load"/>). <br/>
		/// Только для редких кейсов! Ресурс обязательно нужно отпустить (release) <see cref="Release(IResourceReference)"/>
		/// </summary>
		/// <typeparam name="T">Тип ресурса</typeparam>
		[Obsolete("Not usually used Resources (Unity), only rare cases when it is really necessary...")]
		public T LoadResource<T>(IResourceReference entry)
			where T : UnityObject
		{
			ThrowIfSyncLoadingUnsupported();

			return LoadResource<T>(entry.Path);
		}

		/// <summary>
		/// Синхронно загрузить ресурс по пути. См. <see cref="LoadResource{T}(IResourceReference)"/>
		/// </summary>
		/// <typeparam name="T">Тип ресурса</typeparam>
		[Obsolete("Not usually used Resources (Unity), only rare cases when it is really necessary...")]
		public T LoadResource<T>(string path)
			where T : UnityObject
		{
			ThrowIfSyncLoadingUnsupported();

			//Уже загружен/грузится — переиспользуем контейнер
			if (_keyToResourceContainer.TryGetValue(path, out var container))
			{
				container.Retain();
				try
				{
					return container.GetResource<T>();
				}
				catch
				{
					container.Release();
					throw;
				}
			}

			var asset = Resources.Load<T>(path);

			if (asset == null)
			{
				AssetManagementDebug.LogError($"Failed to load resource: {path} is invalid");
				throw new OperationCanceledException(ASSET_IS_NULL_MESSAGE);
			}

			_keyToResourceContainer[path] = new ResourceContainer(this, path, asset);

			return asset;
		}

		/// <summary>
		/// Отпустить ресурс
		/// </summary>
		public void Release(IResourceReference reference)
		{
			ReleaseResource(reference.Path);
		}

		public void ReleaseResource(string path)
		{
			if (_keyToResourceContainer == null)
				return;

			if (!_keyToResourceContainer.TryGetValue(path, out var container))
				return;

			ReleaseResourceContainer(container);
		}

		private void DisposeResources()
		{
			ReleaseAllResources();

			_keyToResourceContainer = null;
		}

		private void ReleaseAllResources()
		{
			foreach (var container in _keyToResourceContainer.Values)
				container.Shutdown();

			_keyToResourceContainer.Clear();
		}

		private ResourceContainer AcquireResourceContainerByPath<T>(string path)
			where T : UnityObject
		{
			if (_keyToResourceContainer == null)
				throw new ObjectDisposedException(nameof(AssetProvider));

			if (path.IsNullOrEmpty())
				throw new ArgumentException("Resource path must not be empty", nameof(path));

			if (_keyToResourceContainer.TryGetValue(path, out var container))
			{
				container.Retain();
				return container;
			}

			var request = Resources.LoadAsync<T>(path);
			if (request == null)
			{
				AssetManagementDebug.LogError($"Failed to load resource: {path} is invalid");
				throw new OperationCanceledException("Failed to load asset");
			}

			container = new ResourceContainer(this, path, request);
			_keyToResourceContainer.Add(path, container);
			return container;
		}

		private void ReleaseResourceContainer(ResourceContainer container)
		{
			if (_keyToResourceContainer == null || container == null)
				return;

			if (container.Key is not string path ||
				!_keyToResourceContainer.TryGetValue(path, out var current) ||
				!ReferenceEquals(current, container))
				return;

			container.ReleaseUsage();
		}

		private void RemoveResourceContainer(ResourceContainer container)
		{
			if (_keyToResourceContainer == null || container == null || container.Key is not string path ||
				!_keyToResourceContainer.TryGetValue(path, out var current) ||
				!ReferenceEquals(current, container))
				return;

			_keyToResourceContainer.Remove(path);
		}

		private sealed class ResourceContainer : IAssetContainer
		{
			private AssetProvider _owner;
			private string _path;

			private int _usages;
			private ResourceRequest _request;

			//Ассет, загруженный синхронно (без ResourceRequest)
			private UnityObject _syncAsset;

			private CancellationTokenSource _cts;
			private CancellationTokenSource _disposeCts;

			//Итоговый ассет из async-запроса или синхронной загрузки
			public object Key => _path;
			public object Asset => _request != null && _request.asset != null ? _request.asset : _syncAsset;
			public bool IsLoaded => Asset is UnityObject asset && asset != null;
			public int ReferenceCount => _usages;

			public ResourceContainer(AssetProvider owner, string path, ResourceRequest initialRequest, int usages = 1)
			{
				_owner = owner;
				_path = path;
				_usages = usages;

				SetRequestInternal(initialRequest);
			}

			//Контейнер для синхронно загруженного ресурса
			public ResourceContainer(AssetProvider owner, string path, UnityObject asset, int usages = 1)
			{
				_owner     = owner;
				_path      = path;
				_usages    = usages;
				_syncAsset = asset;
				_cts       = new();
			}

			public void Retain()
			{
				if (_owner == null)
					throw new ObjectDisposedException(nameof(ResourceContainer));

				_usages++;
				AsyncUtility.TriggerAndSetNull(ref _disposeCts);
				_cts ??= new CancellationTokenSource();
			}

			public void Release()
			{
				_owner?.ReleaseResourceContainer(this);
			}

			public void ReleaseUsage()
			{
				if (_usages <= 0)
					return;

				_usages--;

				if (_usages > 0)
					return;

				BeginUnload();
			}

			public async UniTask<T> LoadAsync<T>(CancellationToken cancellationToken = default,
				IProgress<float> progress = null)
			{
				if (_usages <= 0 || _cts == null || AsyncUtility.AnyCancellation(cancellationToken, _cts.Token))
					throw new OperationCanceledException(cancellationToken);

				AsyncUtility.TriggerAndSetNull(ref _disposeCts);

				if (Asset is T loadedAsset)
				{
					progress?.Report(1f);
					return loadedAsset;
				}

				if (!typeof(UnityObject).IsAssignableFrom(typeof(T)))
					throw new InvalidCastException($"Resource type [ {typeof(T)} ] must inherit UnityEngine.Object");

				if (_request == null)
				{
					var request = Resources.LoadAsync(_path, typeof(T));
					if (request == null)
						throw new OperationCanceledException($"Failed to load resource by path [ {_path} ]");

					SetRequestInternal(request);
				}

				using var linked = _cts.Link(cancellationToken);
				var (isCanceled, asset) = await _request.ToUniTask(progress, cancellationToken: linked.Token)
				   .SuppressCancellationThrow();

				if (isCanceled)
					linked.Token.ThrowIfCancellationRequested();

				if (ReferenceEquals(asset, null))
				{
					AssetManagementDebug.LogError(ASSET_IS_NULL_MESSAGE);
					throw new OperationCanceledException(ASSET_IS_NULL_MESSAGE);
				}

				progress?.Report(1f);
				return (T) (object) asset;
			}

			//Синхронное получение: если ассета ещё нет (async-запрос в полёте) — грузим синхронно
			public T GetResource<T>()
				where T : UnityObject
			{
				AsyncUtility.TriggerAndSetNull(ref _disposeCts);

				var asset = Asset;
				if (asset != null)
					return (T) asset;

				_syncAsset = Resources.Load<T>(_path);
				if (_syncAsset == null)
					throw new OperationCanceledException($"Failed to load resource by path [ {_path} ]");

				return (T) _syncAsset;
			}

			public void Shutdown()
			{
				_owner = null;
				_usages = 0;
				BeginUnload();
			}

			private void BeginUnload()
			{
				if (_request != null && !_request.isDone)
				{
					//Нет другого способа остановить подгрузку ресурса
					//Приходится дождаться завершения и выгрузить результат
					if (_disposeCts == null)
						WaitLoadResourceAndUnloadAsync().Forget();

					return;
				}

				UnloadAsset();
				CompleteUnload();
			}

			//Нет другого способа остановить подгрузку ресурса...
			//придется через такой костыль подождать и выгрузить после...
			private async UniTaskVoid WaitLoadResourceAndUnloadAsync()
			{
				var disposeCts = new CancellationTokenSource();
				_disposeCts = disposeCts;

				var isCanceled = await UniTask.WaitUntil(() => _request.isDone, cancellationToken: disposeCts.Token)
					.SuppressCancellationThrow();

				if (!ReferenceEquals(_disposeCts, disposeCts))
					return;

				_disposeCts.Dispose();
				_disposeCts = null;

				if (isCanceled)
					return;

				UnloadAsset();
				CompleteUnload();
			}

			private void UnloadAsset()
			{
				var asset = Asset as UnityObject;

				if (asset != null)
				{
					if (asset is GameObject or Component or AssetBundle)
						RequestUnloadUnusedAssets();
					else
						Resources.UnloadAsset(asset);
				}

				_request   = null;
				_syncAsset = null;
				AsyncUtility.TriggerAndSetNull(ref _cts);
			}

			private void CompleteUnload()
			{
				var owner = _owner;
				owner?.RemoveResourceContainer(this);

				_owner = null;
				_path = null;
			}

			private void SetRequestInternal(ResourceRequest request)
			{
				_request = request;
				_cts ??= new CancellationTokenSource();
			}

			private void RequestUnloadUnusedAssets()
			{
				_unloadUnusedAssetsRequestCount++;

				if (_unloadUnusedAssetsRequestCount <= UNLOAD_UNUSED_ASSETS_THRESHOLD)
					return;

				Resources.UnloadUnusedAssets();
				_unloadUnusedAssetsRequestCount = 0;
			}
		}
	}
}
