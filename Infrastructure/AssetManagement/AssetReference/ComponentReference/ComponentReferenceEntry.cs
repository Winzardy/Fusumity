using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace AssetManagement
{
	[Serializable]
	public class ComponentReferenceEntry<T> : ComponentReferenceEntry, IAssetReferenceEntry<T>
		where T : Component
	{
		[SerializeField]
		[FormerlySerializedAs("_assetReference")]
		private ComponentReference<T> assetReference;

		public override AssetReference AssetReference => assetReference;
		public static implicit operator bool(ComponentReferenceEntry<T> entry) => !entry.IsEmptyOrInvalid();

		public GameObject editorAsset
		{
#if UNITY_EDITOR
			get => assetReference.editorAsset;
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

		public virtual Type AssetType => null;
		public Object EditorAsset =>
#if UNITY_EDITOR
			AssetReference.editorAsset;
#else
			null;
#endif

		public static implicit operator bool(ComponentReferenceEntry entry) => !entry.IsEmptyOrInvalid();

		public static bool operator ==(ComponentReferenceEntry a, ComponentReferenceEntry b) => a.SameAsset(b);
		public static bool operator !=(ComponentReferenceEntry a, ComponentReferenceEntry b) => !(a == b);
		public override bool Equals(object obj) => this == obj as ComponentReferenceEntry;
		public override int GetHashCode() => AssetReference.GetHashCode();
	}
}
