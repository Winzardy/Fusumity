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
	public partial class AssetManagement
	{
		private const string ASSET_IS_NULL_MESSAGE = "Asset is null...";
		private const int UNLOAD_UNUSED_ASSETS_THRESHOLD = 5;

		private static int _unloadUnusedAssetsRequestCount;

		private Dictionary<string, ResourceContainer> _keyToResourceContainer = new(2);

		/// <summary>
		/// Загрузить ресурс (текстура, геймобж, текст и т.д). <br/>
		/// Ресурс обязательно нужно отпустить (release) после использования. (при отмене отпускается автоматически) <see cref="Release(IAssetReferenceEntry)"/>
		/// </summary>
		/// <typeparam name="T">Тип ресурса</typeparam>
		[Obsolete("Not usually used Resources (Unity), only rare cases when it is really necessary...")]
		public async UniTask<T> LoadResourceAsync<T>(IResourceReferenceEntry entry, CancellationToken cancellationToken = default)
			where T : UnityObject
		{
			return await LoadResourceAsync<T>(entry.Path, cancellationToken);
		}

		/// <summary>
		/// Загрузить ресурс по пути(текстура, геймобж, текст и т.д). <br/>
		/// Ресурс обязательно нужно отпустить (release) после использования. (при отмене отпускается автоматически) <see cref="Release(IAssetReferenceEntry)"/>
		/// </summary>
		/// <typeparam name="T">Тип ресурса</typeparam>
		[Obsolete("Not usually used Resources (Unity), only rare cases when it is really necessary...")]
		public async UniTask<T> LoadResourceAsync<T>(string path, CancellationToken cancellationToken)
			where T : UnityObject
		{
			var usedAsset = await FindOrWaitUsedResourceByPathAsync<T>(path, cancellationToken);

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
			   .WithCancellation(cancellationToken)
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

			return (T) asset;
		}

		/// <summary>
		/// Отпустить ресурс
		/// </summary>
		public void Release(IResourceReferenceEntry entry)
		{
			ReleaseResource(entry.Path);
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

		private async UniTask<T> FindOrWaitUsedResourceByPathAsync<T>(string path, CancellationToken cancellationToken)
			where T : UnityObject
		{
			if (_keyToResourceContainer.TryGetValue(path, out var container))
				return await container.GetResourceAsync<T>(cancellationToken);

			return null;
		}

		private class ResourceContainer : IDisposable
		{
			private string _path;

			private int _usages;
			private ResourceRequest _request;

			private CancellationTokenSource _cts;
			private CancellationTokenSource _disposeCts;

			public ResourceContainer(string path, ResourceRequest initialRequest, int usages = 1)
			{
				_path = path;
				_usages = usages;

				SetRequestInternal(initialRequest);
			}

			public void Dispose()
			{
				if (_request == null)
					return;

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

			public async UniTask<T> GetResourceAsync<T>(CancellationToken cancellationToken)
				where T : UnityObject
			{
				_usages++;

				if (AsyncUtility.AnyCancellation(cancellationToken, _cts.Token))
				{
					Release();
					cancellationToken.ThrowIfCancellationRequested();
				}

				AsyncUtility.Trigger(ref _disposeCts);

				if (_request == null)
					SetRequestInternal(Resources.LoadAsync<T>(_path));

				using var linked = _cts.Link(cancellationToken);
				var (isCanceled, asset) = await _request.WithCancellation(linked.Token)
				   .SuppressCancellationThrow();

				if (isCanceled)
				{
					Release();
					cancellationToken.ThrowIfCancellationRequested();
				}

				return (T) asset;
			}

			//Нет другого способа остановить подгрузку ресурса...
			//придется через такой костыль подождать и выгрузить после...
			private async UniTaskVoid WaitLoadResourceAndUnloadAsync()
			{
				_disposeCts = new CancellationTokenSource();

				await UniTask.WaitUntil(() => _request.isDone, cancellationToken: _disposeCts.Token);

				UnloadAsset();

				AsyncUtility.Trigger(ref _disposeCts);
			}

			private void UnloadAsset()
			{
				if (_request.asset is GameObject or Component or AssetBundle)
					RequestUnloadUnusedAssets();
				else
					Resources.UnloadAsset(_request.asset);

				_request = null;
				AsyncUtility.Trigger(ref _cts);
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
