using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sapientia.Extensions;
using UnityEngine;

namespace AssetManagement
{
	public partial class AssetProvider : IDisposable
	{
		private bool _initialized;

		public AssetProvider() => _initialized = true;

		public void Dispose()
		{
			_initialized = false;

			DisposeAddressable();
			DisposeResources();
		}

		/// <summary>
		/// Загрузить ассет (текстура, геймобж, текст и т.д). <br/>
		/// Ассет обязательно нужно отпустить (release) после использования. (при отмене отпускается автоматически) <see cref="Release(IAssetReference)"/>
		/// </summary>
		/// <typeparam name="T">Тип ассета</typeparam>
		public async UniTask<T> LoadAssetAsync<T>(IAssetReference reference, CancellationToken cancellationToken = default)
		{
			var assetReference = reference.AssetReference;

			if (typeof(Component).IsAssignableFrom(typeof(T)))
				return await LoadComponentAsync<T>(assetReference, cancellationToken);

			return await LoadAssetAsync<T>(assetReference, cancellationToken);
		}

		/// <summary>
		/// Загрузить GameObject и получить у него выбранный компонент. <br/>
		/// Чтобы подгрузить GameObject используйте <see cref="LoadAssetAsync{T}(IAssetReference,System.Threading.CancellationToken)"/> <br/>
		/// Ассет обязательно нужно отпустить (release) после использования. (при отмене отпускается автоматически) <see cref="Release(IAssetReference)"/>
		/// </summary>
		/// <typeparam name="T">Тип компонента</typeparam>
		public async UniTask<T> LoadComponentAsync<T>(ComponentReference reference, CancellationToken cancellationToken)
			where T : Component
		{
			var assetReference = reference.AssetReference;
			return await LoadComponentAsync<T>(assetReference, cancellationToken);
		}

		/// <summary>
		/// Загрузить GameObject и получить у него выбранный компонент. <br/>
		/// Чтобы подгрузить GameObject используйте <see cref="LoadAssetAsync{T}(IAssetReference,System.Threading.CancellationToken)"/> <br/>
		/// Ассет обязательно нужно отпустить (release) после использования. (при отмене отпускается автоматически) <see cref="Release(IAssetReference)"/>
		/// </summary>
		/// <typeparam name="T">Тип компонента</typeparam>
		public async UniTask<T> LoadComponentAsync<T>(IAssetReference reference, CancellationToken cancellationToken)
			where T : Component
		{
			var assetReference = reference.AssetReference;
			return await LoadComponentAsync<T>(assetReference, cancellationToken);
		}

		/// <summary>
		/// Загрузить ассет по пути (текстура, геймобж, текст и т.д).
		/// Ассет обязательно нужно отпустить (release) после использования. (при отмене отпускается автоматически) <see cref="Release(string)"/>
		/// </summary>
		/// <typeparam name="T">Тип ассета</typeparam>
		public async UniTask<T> LoadAssetAsync<T>(string path, CancellationToken cancellationToken)
		{
			return await LoadAssetAsyncByKey<T>(path, cancellationToken);
		}

		/// <summary>
		/// Загрузить GameObject и получить у него выбранный компонент. <br/>
		/// Чтобы подгрузить GameObject используйте <see cref="LoadAsync{T}(string,System.Threading.CancellationToken)"/> <br/>
		/// Ассет обязательно нужно отпустить (release) после использования. (при отмене отпускается автоматически) <see cref="Release(string)"/>
		/// </summary>
		/// <typeparam name="T">Тип компонента</typeparam>
		public async UniTask<T> LoadComponentAsync<T>(string path, CancellationToken cancellationToken)
			where T : Component
		{
			return await LoadComponentByKeyAsync<T>(path, cancellationToken);
		}

		/// <summary>
		/// Загрузить все ассеты (Label у Addressable). <br/>
		/// Ассеты обязательно нужно отпустить (release) после использования. (при отмене отпускается автоматически) <see cref="ReleaseAssets"/>
		/// </summary>
		/// <typeparam name="T">Тип ассетов</typeparam>
		public async UniTask<IList<T>> LoadAssetsAsync<T>(AssetLabelReference reference, CancellationToken cancellationToken)
		{
			var labelReference = reference.Reference;
			return await LoadAssetsAsync<T>(labelReference, cancellationToken);
		}

		/// <summary>
		/// Загрузить все ассеты по тегу (Label у Addressable). <br/>
		/// Ассеты обязательно нужно отпустить (release) после использования. (при отмене отпускается автоматически) <see cref="ReleaseAssets"/>
		/// </summary>
		/// <typeparam name="T">Тип ассетов</typeparam>
		public async UniTask<IList<T>> LoadAssetsAsync<T>(string tag, CancellationToken cancellationToken)
		{
			return await LoadAssetsAsyncByKey<T>(tag, cancellationToken);
		}

		/// <summary>
		/// Загрузить все ассеты по тегу (Label у Addressable). <br/>
		/// Ассеты обязательно нужно отпустить (release) после использования. (при отмене отпускается автоматически) <see cref="ReleaseAssets"/>
		/// </summary>
		/// <typeparam name="T">Тип ассетов</typeparam>
		public async UniTask<IList<T>> LoadAssetsAsync<T>(IEnumerable tags, CancellationToken cancellationToken)
		{
			return await LoadAssetsAsyncByKey<T>(tags, cancellationToken);
		}

		/// <summary>
		/// Отпустить ассет
		/// </summary>
		public void Release(IAssetReference entry, int? delayMs = 0)
		{
			if (delayMs.HasValue)
			{
				WaitDelayAndReleaseAsync(entry, delayMs.Value.Max(entry.ReleaseDelayMs)).Forget();
				return;
			}

			if (entry.ReleaseDelayMs > 0)
			{
				WaitDelayAndReleaseAsync(entry, entry.ReleaseDelayMs).Forget();
				return;
			}

			Release(entry.AssetReference);
		}

		/// <summary>
		/// Отпустить ассет
		/// </summary>
		public void Release(string path, int delayMs = 0)
		{
			if (delayMs > 0)
			{
				WaitDelayAndReleaseAsync(path, delayMs).Forget();
				return;
			}

			ReleaseAssetByKey(path);
		}

		/// <summary>
		/// Отпустить ассеты по лейблу
		/// </summary>
		public void ReleaseAssets(AssetLabelReference entry)
		{
			ReleaseAssets(entry.Reference);
		}

		/// <summary>
		/// Отпустить ассеты по тегу
		/// </summary>
		public void ReleaseAssets(string tag)
		{
			ReleaseAssetsByKey(tag);
		}

		public void ReleaseAll()
		{
			ReleaseAllAddressable();
		}

		/// <summary>
		/// Отпустить ассет с задержкой
		/// </summary>
		private async UniTask WaitDelayAndReleaseAsync(IAssetReference reference, int delayMs)
		{
			await UniTask.Delay(delayMs, DelayType.UnscaledDeltaTime);

			Release(reference.AssetReference);
		}

		/// <summary>
		/// Отпустить ассет с задержкой
		/// </summary>
		private async UniTask WaitDelayAndReleaseAsync(string path, int delayMs)
		{
			await UniTask.Delay(delayMs, DelayType.UnscaledDeltaTime);

			ReleaseAssetByKey(path);
		}
	}
}
