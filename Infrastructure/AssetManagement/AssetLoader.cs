using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sapientia;
using UnityEngine;

namespace AssetManagement
{
	using UnityObject = UnityEngine.Object;

	public partial class AssetLoader : StaticWrapper<AssetProvider>
	{
		// ReSharper disable once InconsistentNaming
		private static AssetProvider provider
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _instance;
		}

		public static bool IsInitialized
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _instance is {IsInitialized: true};
		}

		/// <summary>
		/// Загрузить ассет в память (текстура, геймобж, текст и т.д). <br/>
		/// Ассет обязательно нужно отпустить (release) после использования. (при отмене отпускается автоматически) <see cref="Release(IAssetReference)"/>
		/// </summary>
		/// <typeparam name="T">Тип ассета</typeparam>
		public static async UniTask<T> LoadAssetAsync<T>(IAssetReference reference,
			CancellationToken cancellationToken = default, IProgress<float> progress = null)
		{
			if (!IsInitialized)
				throw AssetManagementDebug.OperationCanceledException(cancellationToken);

			return await provider.LoadAssetAsync<T>(reference, cancellationToken, progress);
		}

		/// <summary>
		/// Загрузить GameObject и получить у него выбранный компонент. <br/>
		/// Чтобы подгрузить GameObject используйте <see cref="LoadAssetAsync{T}(IAssetReference,System.Threading.CancellationToken)"/> <br/>
		/// Ассет обязательно нужно отпустить (release) после использования. (при отмене отпускается автоматически) <see cref="Release(IAssetReference)"/>
		/// </summary>
		/// <typeparam name="T">Тип компонента</typeparam>
		public static async UniTask<T> LoadComponentAsync<T>(ComponentReference reference,
			CancellationToken cancellationToken = default, IProgress<float> progress = null)
			where T : Component
		{
			if (!IsInitialized)
				throw AssetManagementDebug.OperationCanceledException(cancellationToken);

			return await provider.LoadComponentAsync<T>(reference, cancellationToken, progress);
		}

		/// <summary>
		/// Загрузить GameObject и получить у него выбранный компонент. <br/>
		/// Чтобы подгрузить GameObject используйте <see cref="LoadAssetAsync{T}(IAssetReference,System.Threading.CancellationToken)"/> <br/>
		/// Ассет обязательно нужно отпустить (release) после использования. (при отмене отпускается автоматически) <see cref="Release(IAssetReference)"/>
		/// </summary>
		/// <typeparam name="T">Тип компонента</typeparam>
		public static async UniTask<T> LoadComponentAsync<T>(IAssetReference reference,
			CancellationToken cancellationToken = default, IProgress<float> progress = null)
			where T : Component
		{
			if (!IsInitialized)
				throw AssetManagementDebug.OperationCanceledException(cancellationToken);

			return await provider.LoadComponentAsync<T>(reference, cancellationToken, progress);
		}

		/// <summary>
		/// Загрузить ассет в память по пути (текстура, геймобж, текст и т.д).
		/// Ассет обязательно нужно отпустить (release) после использования. (при отмене отпускается автоматически) <see cref="Release(string)"/>
		/// </summary>
		/// <typeparam name="T">Тип ассета</typeparam>
		public static async UniTask<T> LoadAssetAsync<T>(string path, CancellationToken cancellationToken = default,
			IProgress<float> progress = null)
		{
			if (!IsInitialized)
				throw AssetManagementDebug.OperationCanceledException(cancellationToken);

			return await provider.LoadAssetAsync<T>(path, cancellationToken, progress);
		}

		/// <summary>
		/// Загрузить GameObject и получить у него выбранный компонент. <br/>
		/// Чтобы подгрузить GameObject используйте <see cref="LoadAssetAsync{T}(string,System.Threading.CancellationToken)"/> <br/>
		/// Ассет обязательно нужно отпустить (release) после использования. (при отмене отпускается автоматически) <see cref="Release(string)"/>
		/// </summary>
		/// <typeparam name="T">Тип компонента</typeparam>
		public static async UniTask<T> LoadComponentAsync<T>(string path, CancellationToken cancellationToken = default,
			IProgress<float> progress = null)
			where T : Component
		{
			if (!IsInitialized)
				throw AssetManagementDebug.OperationCanceledException(cancellationToken);

			return await provider.LoadComponentAsync<T>(path, cancellationToken, progress);
		}

		/// <summary>
		/// Загрузить все ассеты по лейблу (Label у Addressable). <br/>
		/// Ассеты обязательно нужно отпустить (release) после использования. (при отмене отпускается автоматически) <see cref="ReleaseAssets"/>
		/// </summary>
		/// <typeparam name="T">Тип ассетов</typeparam>
		public static async UniTask<IList<T>> LoadAssetsAsync<T>(AssetLabelReference reference,
			CancellationToken cancellationToken = default, IProgress<float> progress = null)
		{
			if (!IsInitialized)
				throw AssetManagementDebug.OperationCanceledException(cancellationToken);

			return await provider.LoadAssetsAsync<T>(reference, cancellationToken, progress);
		}

		/// <summary>
		/// Загрузить все ассеты по тегу (Label у Addressable). <br/>
		/// Ассеты обязательно нужно отпустить (release) после использования. (при отмене отпускается автоматически) <see cref="ReleaseAssets"/>
		/// </summary>
		/// <typeparam name="T">Тип ассетов</typeparam>
		public static async UniTask<IList<T>> LoadAssetsAsync<T>(string tag,
			CancellationToken cancellationToken = default, IProgress<float> progress = null)
		{
			if (!IsInitialized)
				throw AssetManagementDebug.OperationCanceledException(cancellationToken);

			return await provider.LoadAssetsAsync<T>(tag, cancellationToken, progress);
		}

		/// <summary>
		/// Загрузить все ассеты по тегу (Label у Addressable). <br/>
		/// Ассеты обязательно нужно отпустить (release) после использования. (при отмене отпускается автоматически) <see cref="ReleaseAssets"/>
		/// </summary>
		/// <typeparam name="T">Тип ассетов</typeparam>
		public static async UniTask<IList<T>> LoadAssetsAsync<T>(IEnumerable tags,
			CancellationToken cancellationToken = default, IProgress<float> progress = null)
		{
			if (!IsInitialized)
				throw AssetManagementDebug.OperationCanceledException(cancellationToken);

			return await provider.LoadAssetsAsync<T>(tags, cancellationToken, progress);
		}

		/// <summary>
		/// Отпустить ассет из памяти
		/// Что значит Отпустить ассет из памяти?
		/// Значит что до этого был запрос на данный ассет и система его подгрузила, но система не знает когда он
		/// больше не нужен чтобы выгрузить, поэтому нужно сообщить системе чтобы она отпустила
		/// </summary>
		// TODO: добавить Release Mode (delay, trigger (например смена локации))
		public static void Release(IAssetReference reference, int? delayMs = 0) => provider.Release(reference, delayMs);

		/// <summary>
		/// Отпустить ассет из памяти по пути
		/// </summary>
		public static void Release(string path, int delayMs = 0) => provider.Release(path, delayMs);

		/// <summary>
		/// Отпустить ассет из памяти
		/// </summary>
		public static void ReleaseAssets(AssetLabelReference reference) => provider.ReleaseAssets(reference);

		/// <summary>
		/// Отпустить ассет из памяти по тегу
		/// </summary>
		public static void ReleaseAssets(string tag) => provider.ReleaseAssets(tag);
	}
}
