using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

namespace AssetManagement
{
	[Serializable]
	public class ComponentReferenceEntry<T> : ComponentReferenceEntry, IAssetReferenceEntry<T>
		where T : Component
	{
		[SerializeField]
		private ComponentReference<T> _assetReference;

		public override AssetReference AssetReference => _assetReference;
		public static implicit operator bool(ComponentReferenceEntry<T> entry) => !entry.IsEmpty();

		public GameObject editorAsset
		{
#if UNITY_EDITOR
			get => _assetReference.editorAsset;
			set { this.SetEditorAsset(value); }
#else
			get => null;
#endif
		}

		public override Type AssetType => typeof(T);
	}

	[Serializable]
	public abstract class ComponentReferenceEntry : IAssetReferenceEntry
	{
		[SerializeField]
		private int _releaseDelayMs;

		public abstract AssetReference AssetReference { get; }

		int IAssetReferenceEntry.ReleaseDelayMs => _releaseDelayMs;
		public static implicit operator bool(ComponentReferenceEntry entry) => !entry.IsEmpty();

		public virtual Type AssetType => null;
		public Object EditorAsset =>
#if UNITY_EDITOR
			AssetReference.editorAsset;
#else
			null;
#endif
	}
}
