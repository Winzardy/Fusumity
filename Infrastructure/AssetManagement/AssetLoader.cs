using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sapientia;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AssetManagement
{
	public partial class AssetLoader : StaticProvider<AssetProvider>
	{
		private static AssetProvider Provider
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _instance;
		}

		public static bool IsInitialized
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _instance != null;
		}

		/// <summary>
		/// Загрузить ассет в память (текстура, геймобж, текст и т.д). <br/>
		/// Ассет обязательно нужно отпустить (release) после использования. (при отмене отпускается автоматически) <see cref="Release(IAssetReferenceEntry)"/>
		/// </summary>
		/// <typeparam name="T">Тип ассета</typeparam>
		public static async UniTask<T> LoadAssetAsync<T>(IAssetReferenceEntry entry,
			CancellationToken cancellationToken = default)
			=> await Provider.LoadAssetAsync<T>(entry, cancellationToken);

		/// <summary>
		/// Загрузить GameObject и получить у него выбранный компонент. <br/>
		/// Чтобы подгрузить GameObject используйте <see cref="LoadAssetAsync{T}(IAssetReferenceEntry,System.Threading.CancellationToken)"/> <br/>
		/// Ассет обязательно нужно отпустить (release) после использования. (при отмене отпускается автоматически) <see cref="Release(IAssetReferenceEntry)"/>
		/// </summary>
		/// <typeparam name="T">Тип компонента</typeparam>
		public static async UniTask<T> LoadComponentAsync<T>(ComponentReferenceEntry entry,
			CancellationToken cancellationToken = default)
			where T : Component => await Provider.LoadComponentAsync<T>(entry, cancellationToken);

		/// <summary>
		/// Загрузить GameObject и получить у него выбранный компонент. <br/>
		/// Чтобы подгрузить GameObject используйте <see cref="LoadAssetAsync{T}(IAssetReferenceEntry,System.Threading.CancellationToken)"/> <br/>
		/// Ассет обязательно нужно отпустить (release) после использования. (при отмене отпускается автоматически) <see cref="Release(IAssetReferenceEntry)"/>
		/// </summary>
		/// <typeparam name="T">Тип компонента</typeparam>
		public static async UniTask<T> LoadComponentAsync<T>(IAssetReferenceEntry entry,
			CancellationToken cancellationToken = default)
			where T : Component => await Provider.LoadComponentAsync<T>(entry, cancellationToken);

		/// <summary>
		/// Загрузить ассет в память по пути (текстура, геймобж, текст и т.д).
		/// Ассет обязательно нужно отпустить (release) после использования. (при отмене отпускается автоматически) <see cref="Release(string)"/>
		/// </summary>
		/// <typeparam name="T">Тип ассета</typeparam>
		public static async UniTask<T> LoadAssetAsync<T>(string path, CancellationToken cancellationToken = default)
			where T : Object => await Provider.LoadAssetAsync<T>(path, cancellationToken);

		/// <summary>
		/// Загрузить GameObject и получить у него выбранный компонент. <br/>
		/// Чтобы подгрузить GameObject используйте <see cref="LoadAssetAsync{T}(string,System.Threading.CancellationToken)"/> <br/>
		/// Ассет обязательно нужно отпустить (release) после использования. (при отмене отпускается автоматически) <see cref="Release(string)"/>
		/// </summary>
		/// <typeparam name="T">Тип компонента</typeparam>
		public static async UniTask<T> LoadComponentAsync<T>(string path, CancellationToken cancellationToken = default)
			where T : Component => await Provider.LoadComponentAsync<T>(path, cancellationToken);

		/// <summary>
		/// Загрузить все ассеты по лейблу (Label у Addressable). <br/>
		/// Ассеты обязательно нужно отпустить (release) после использования. (при отмене отпускается автоматически) <see cref="ReleaseAssets"/>
		/// </summary>
		/// <typeparam name="T">Тип ассетов</typeparam>
		public static async UniTask<IList<T>> LoadAssetsAsync<T>(AssetLabelReferenceEntry entry,
			CancellationToken cancellationToken = default) =>
			await Provider.LoadAssetsAsync<T>(entry, cancellationToken);

		/// <summary>
		/// Загрузить все ассеты по тегу (Label у Addressable). <br/>
		/// Ассеты обязательно нужно отпустить (release) после использования. (при отмене отпускается автоматически) <see cref="ReleaseAssets"/>
		/// </summary>
		/// <typeparam name="T">Тип ассетов</typeparam>
		public static async UniTask<IList<T>> LoadAssetsAsync<T>(string tag,
			CancellationToken cancellationToken = default) =>
			await Provider.LoadAssetsAsync<T>(tag, cancellationToken);

		/// <summary>
		/// Отпустить ассет из памяти
		/// Что значит Отпустить ассет из памяти?
		/// Значит что до этого был запрос на данный ассет и система его подгрузила, но система не знает когда он
		/// больше не нужен чтобы выгрузить, поэтому нужно сообщить системе чтобы она отпустила
		/// </summary>
		// TODO: добавить Release Mode (delay, trigger (например смена локации))
		public static void Release(IAssetReferenceEntry entry, int? delayMs = 0) => Provider.Release(entry, delayMs);

		/// <summary>
		/// Отпустить ассет из памяти по пути
		/// </summary>
		public static void Release(string path, int delayMs = 0) => Provider.Release(path, delayMs);

		/// <summary>
		/// Отпустить ассет из памяти
		/// </summary>
		public static void ReleaseAssets(AssetLabelReferenceEntry entry) => Provider.ReleaseAssets(entry);

		/// <summary>
		/// Отпустить ассет из памяти по тегу
		/// </summary>
		public static void ReleaseAssets(string tag) => Provider.ReleaseAssets(tag);
	}
}
