using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Sapientia.Utility;

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
			var usedAsset = await FindOrWaitUsedResourceByPathAsync<T>(path, cancellationToken, progress);

			if (!ReferenceEquals(usedAsset, null))
				return usedAsset;

			var request = Resources.LoadAsync<T>(path);

			if (request == null)
			{
				AssetManagementDebug.LogError($"Failed to load resource: {path} is invalid");
				throw new OperationCanceledException("Failed to load asset");
			}

			_keyToResourceContainer[path] = new ResourceContainer(path, request);

			var (isCanceled, asset) = await request
			   .ToUniTask(progress, cancellationToken: cancellationToken)
			   .SuppressCancellationThrow();

			if (ReferenceEquals(asset, null))
			{
				AssetManagementDebug.LogError(ASSET_IS_NULL_MESSAGE);
				throw new OperationCanceledException(ASSET_IS_NULL_MESSAGE);
			}

			if (isCanceled)
			{
				ReleaseResource(path);
				AssetManagementDebug.LogWarning($"Cancelled to load resource by path [ {path} ]");
				cancellationToken.ThrowIfCancellationRequested();
			}

			progress?.Report(1f);
			return (T) asset;
		}

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
				return container.GetResource<T>();

			var asset = Resources.Load<T>(path);

			if (asset == null)
			{
				AssetManagementDebug.LogError($"Failed to load resource: {path} is invalid");
				throw new OperationCanceledException(ASSET_IS_NULL_MESSAGE);
			}

			_keyToResourceContainer[path] = new ResourceContainer(path, asset);

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
			if (!_keyToResourceContainer.TryGetValue(path, out var container))
				return;

			container.Release();
		}

		private void DisposeResources()
		{
			ReleaseAllResources();

			_keyToResourceContainer = null;
		}

		private void ReleaseAllResources()
		{
			foreach (var container in _keyToResourceContainer.Values)
				container.Dispose();

			_keyToResourceContainer.Clear();
		}

		private async UniTask<T> FindOrWaitUsedResourceByPathAsync<T>(string path, CancellationToken cancellationToken,
			IProgress<float> progress = null)
			where T : UnityObject
		{
			if (_keyToResourceContainer.TryGetValue(path, out var container))
				return await container.GetResourceAsync<T>(cancellationToken, progress);

			return null;
		}

		private class ResourceContainer : IDisposable
		{
			private string _path;

			private int _usages;
			private ResourceRequest _request;

			//Ассет, загруженный синхронно (без ResourceRequest)
			private UnityObject _syncAsset;

			private CancellationTokenSource _cts;
			private CancellationTokenSource _disposeCts;

			//Итоговый ассет из async-запроса или синхронной загрузки
			private UnityObject Asset => _request != null ? _request.asset : _syncAsset;

			public ResourceContainer(string path, ResourceRequest initialRequest, int usages = 1)
			{
				_path = path;
				_usages = usages;

				SetRequestInternal(initialRequest);
			}

			//Контейнер для синхронно загруженного ресурса
			public ResourceContainer(string path, UnityObject asset, int usages = 1)
			{
				_path      = path;
				_usages    = usages;
				_syncAsset = asset;
				_cts       = new();
			}

			public void Dispose()
			{
				if (_request == null)
				{
					//Синхронно загруженный ресурс
					if (_syncAsset != null)
						UnloadAsset();

					return;
				}

				if (_request.isDone)
				{
					UnloadAsset();
					return;
				}

				//Нет другого способа остановить подгрузку ресурса...
				//придется через такой костыль подождать и выгрузить после...
				WaitLoadResourceAndUnloadAsync().Forget();
			}

			public void Release()
			{
				_usages--;

				if (_usages > 0)
					return;

				Dispose();
			}

			public async UniTask<T> GetResourceAsync<T>(CancellationToken cancellationToken, IProgress<float> progress = null)
				where T : UnityObject
			{
				_usages++;

				if (AsyncUtility.AnyCancellation(cancellationToken, _cts.Token))
				{
					Release();
					cancellationToken.ThrowIfCancellationRequested();
				}

				AsyncUtility.TriggerAndSetNull(ref _disposeCts);

				if (_request == null)
					SetRequestInternal(Resources.LoadAsync<T>(_path));

				using var linked = _cts.Link(cancellationToken);
				var (isCanceled, asset) = await _request.ToUniTask(progress, cancellationToken: linked.Token)
				   .SuppressCancellationThrow();

				if (isCanceled)
				{
					Release();
					cancellationToken.ThrowIfCancellationRequested();
				}

				progress?.Report(1f);
				return (T) asset;
			}

			//Синхронное получение: если ассета ещё нет (async-запрос в полёте) — грузим синхронно
			public T GetResource<T>()
				where T : UnityObject
			{
				_usages++;

				AsyncUtility.TriggerAndSetNull(ref _disposeCts);

				var asset = Asset;
				if (asset != null)
					return (T) asset;

				_syncAsset = Resources.Load<T>(_path);
				return (T) _syncAsset;
			}

			//Нет другого способа остановить подгрузку ресурса...
			//придется через такой костыль подождать и выгрузить после...
			private async UniTaskVoid WaitLoadResourceAndUnloadAsync()
			{
				_disposeCts = new CancellationTokenSource();

				await UniTask.WaitUntil(() => _request.isDone, cancellationToken: _disposeCts.Token);

				UnloadAsset();

				AsyncUtility.TriggerAndSetNull(ref _disposeCts);
			}

			private void UnloadAsset()
			{
				var asset = Asset;

				if (asset is GameObject or Component or AssetBundle)
					RequestUnloadUnusedAssets();
				else
					Resources.UnloadAsset(asset);

				_request   = null;
				_syncAsset = null;
				AsyncUtility.TriggerAndSetNull(ref _cts);
			}

			private void SetRequestInternal(ResourceRequest request)
			{
				_request = request;
				_cts = new();
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
