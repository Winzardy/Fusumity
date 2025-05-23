using System;
using Sapientia.Extensions;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AssetManagement
{
	/// <summary>
	/// Особенности работы с <see cref="Resources"/>
	/// <para>1. Ресурс нужно поместить в папку <b>Resources</b></para>
	/// 2. Путь файла будет его путь в самой папке, например: <code>Assets/Resources/Player/Player.prefab -> Player/Player</code>
	/// <para>3. Проблемы с загрузкой ресурса если его папка находится в отдельном <b>.asmdef</b></para>
	/// </summary>
	/// <typeparam name="T">Тип ресурса (GameObject, Component, Sprite и т.д)</typeparam>
	[Serializable]
	[Obsolete("Not usually used Resources (Unity), only rare cases when it is really necessary... " +
		"Look away AssetReferenceEntry")]
	public class ResourceReferenceEntry<T> : IResourceReferenceEntry
		where T : Object
	{
		[SerializeField]
		private string _path;

		public string Path => _path;

		public T editorAsset =>
#if UNITY_EDITOR
			!_path.IsNullOrEmpty() ? Resources.Load<T>(Path) : null;
#else
			null;
#endif
		public static implicit operator ResourceReferenceEntry<T>(string path) => new ResourceReferenceEntry<T>()
		{
			_path = path
		};
	}

	public interface IResourceReferenceEntry
	{
		public string Path { get; }

		public Object editorAsset =>
#if UNITY_EDITOR
			!Path.IsNullOrEmpty() ? Resources.Load(Path) : null;
#else
			null;
#endif
	}
}
