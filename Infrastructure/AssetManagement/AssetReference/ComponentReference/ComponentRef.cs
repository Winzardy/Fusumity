using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;

namespace AssetManagement
{
	using UnityObject = UnityEngine.Object;

	[Serializable]
	public class ComponentRef<T> : ComponentRef, IAssetRef<T>
		where T : Component
	{
		[SerializeField]
		[FormerlySerializedAs("_assetReference")]
		private ComponentReference<T> assetReference;

		public override AssetReference AssetReference => assetReference;
		public static implicit operator bool(ComponentRef<T> entry) => !entry.IsEmptyOrInvalid();

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

		public override string ToString() => assetReference.ToString();
	}

	[Serializable]
	public abstract class ComponentRef : IAssetRef
	{
		[SerializeField]
		private int _releaseDelayMs;

		public abstract AssetReference AssetReference { get; }

		int IAssetRef.ReleaseDelayMs => _releaseDelayMs;

		public virtual Type AssetType => null;
		public UnityObject EditorAsset =>
#if UNITY_EDITOR
			AssetReference.editorAsset;
#else
			null;
#endif

		public static implicit operator bool(ComponentRef entry) => !entry.IsEmptyOrInvalid();

		public static bool operator ==(ComponentRef a, ComponentRef b) => a.SameAsset(b);
		public static bool operator !=(ComponentRef a, ComponentRef b) => !(a == b);
		public override bool Equals(object obj) => this == obj as ComponentRef;
		public override int GetHashCode() => AssetReference.GetHashCode();
		public override string ToString() => AssetReference.ToString();
	}
}
