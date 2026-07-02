using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Object = UnityEngine.Object;

namespace AssetManagement
{
	public partial class AssetLoader
	{
		/// <summary>
		/// Загрузить ресурс в память (текстура, геймобж, текст и т.д).
		/// Ресурс обязательно нужно отпустить (release) после использования. (при отмене отпускается автоматически) <see cref="Release(IResourceReference)"/>
		/// </summary>
		/// <typeparam name="T">Тип ресурса</typeparam>
		[Obsolete("Not usually used Resources (Unity), only rare cases when it is really necessary...")]
		public static async UniTask<T> LoadResourceAsync<T>(IResourceReference reference,
			CancellationToken cancellationToken = default, IProgress<float> progress = null)
			where T : Object =>
			await _instance.LoadResourceAsync<T>(reference, cancellationToken, progress);

		/// <summary>
		/// Загрузить ресурс в память по пути (текстура, геймобж, текст и т.д).
		/// Ресурс обязательно нужно отпустить (release) после использования. (при отмене отпускается автоматически) <see cref="Release(IResourceReference)"/>
		/// </summary>
		/// <typeparam name="T">Тип ресурса</typeparam>
		[Obsolete("Not usually used Resources (Unity), only rare cases when it is really necessary...")]
		public static async UniTask<T> LoadResourceAsync<T>(string path,
			CancellationToken cancellationToken = default, IProgress<float> progress = null)
			where T : Object =>
			await _instance.LoadResourceAsync<T>(path, cancellationToken, progress);

		/// <summary>
		/// Синхронно загрузить ресурс. Блокирует поток до готовности. <br/>
		/// Только для редких кейсов! Обычно используйте <see cref="LoadResourceAsync{T}(IResourceReference,System.Threading.CancellationToken,System.IProgress{float})"/> <br/>
		/// Ресурс обязательно нужно отпустить (release) после использования <see cref="Release(IResourceReference)"/>
		/// </summary>
		/// <typeparam name="T">Тип ресурса</typeparam>
		[Obsolete("Not usually used Resources (Unity), only rare cases when it is really necessary...")]
		public static T LoadResource<T>(IResourceReference reference)
			where T : Object => _instance.LoadResource<T>(reference);

		/// <summary>
		/// Синхронно загрузить ресурс по пути. См. <see cref="LoadResource{T}(IResourceReference)"/>
		/// </summary>
		/// <typeparam name="T">Тип ресурса</typeparam>
		[Obsolete("Not usually used Resources (Unity), only rare cases when it is really necessary...")]
		public static T LoadResource<T>(string path)
			where T : Object => _instance.LoadResource<T>(path);

		/// <summary>
		/// Отпустить ресурс
		/// </summary>
		public static void Release(IResourceReference reference) => _instance.Release(reference);

		/// <summary>
		/// Отпустить ресурс
		/// </summary>
		public static void ReleaseResource(string path) => _instance.ReleaseResource(path);
	}
}
