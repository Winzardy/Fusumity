using UnityEngine;

namespace AssetManagement
{
	using UnityObject = Object;
	using UnityComponent = Component;

	public partial class AssetLoader
	{
		/// <summary>
		/// Синхронно загрузить ассет. Блокирует поток до готовности. <br/>
		/// Только для редких кейсов! Вызывает хич на главном потоке и не поддерживается на WebGL <br/>
		/// Обычно используйте <see cref="LoadAssetAsync{T}(IAssetReference,System.Threading.CancellationToken,System.IProgress{float})"/> <br/>
		/// Ассет обязательно нужно отпустить (release) после использования <see cref="Release(IAssetReference)"/>
		/// </summary>
		/// <typeparam name="T">Тип ассета</typeparam>
		public static T LoadAsset<T>(IAssetReference reference) => provider.LoadAsset<T>(reference);

		/// <summary>
		/// Синхронно загрузить ассет по пути. См. <see cref="LoadAsset{T}(IAssetReference)"/>
		/// </summary>
		/// <typeparam name="T">Тип ассета</typeparam>
		public static T LoadAsset<T>(string path) where T : UnityObject => provider.LoadAsset<T>(path);

		/// <summary>
		/// Синхронно загрузить GameObject и получить у него выбранный компонент. См. <see cref="LoadAsset{T}(IAssetReference)"/>
		/// </summary>
		/// <typeparam name="T">Тип компонента</typeparam>
		public static T LoadComponent<T>(ComponentReference reference)
			where T : UnityComponent => provider.LoadComponent<T>(reference);

		/// <summary>
		/// Синхронно загрузить GameObject и получить у него выбранный компонент. См. <see cref="LoadAsset{T}(IAssetReference)"/>
		/// </summary>
		/// <typeparam name="T">Тип компонента</typeparam>
		public static T LoadComponent<T>(IAssetReference reference)
			where T : UnityComponent => provider.LoadComponent<T>(reference);

		/// <summary>
		/// Синхронно загрузить GameObject и получить у него выбранный компонент по пути. См. <see cref="LoadAsset{T}(IAssetReference)"/>
		/// </summary>
		/// <typeparam name="T">Тип компонента</typeparam>
		public static T LoadComponent<T>(string path)
			where T : UnityComponent => provider.LoadComponent<T>(path);
	}
}
