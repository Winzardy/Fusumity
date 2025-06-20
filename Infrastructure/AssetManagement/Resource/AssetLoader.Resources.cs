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
		/// Ресурс обязательно нужно отпустить (release) после использования. (при отмене отпускается автоматически) <see cref="Release(IResourceReferenceEntry)"/>
		/// </summary>
		/// <typeparam name="T">Тип ресурса</typeparam>
		[Obsolete("Not usually used Resources (Unity), only rare cases when it is really necessary...")]
		public static async UniTask<T> LoadResourceAsync<T>(IResourceReferenceEntry entry, CancellationToken cancellationToken = default)
			where T : Object =>
			await _instance.LoadResourceAsync<T>(entry, cancellationToken);

		/// <summary>
		/// Загрузить ресурс в память по пути (текстура, геймобж, текст и т.д).
		/// Ресурс обязательно нужно отпустить (release) после использования. (при отмене отпускается автоматически) <see cref="Release(IResourceReferenceEntry)"/>
		/// </summary>
		/// <typeparam name="T">Тип ресурса</typeparam>
		[Obsolete("Not usually used Resources (Unity), only rare cases when it is really necessary...")]
		public static async UniTask<T> LoadResourceAsync<T>(string path, CancellationToken cancellationToken = default)
			where T : Object =>
			await _instance.LoadResourceAsync<T>(path, cancellationToken);

		/// <summary>
		/// Отпустить ресурс
		/// </summary>
		public static void Release(IResourceReferenceEntry entry) => _instance.Release(entry);

		/// <summary>
		/// Отпустить ресурс
		/// </summary>
		public static void ReleaseResource(string path) => _instance.ReleaseResource(path);
	}
}
