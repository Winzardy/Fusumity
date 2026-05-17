using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace AssetManagement
{
	using UnityObject = UnityEngine.Object;
	using UnityAssetReference = UnityEngine.AddressableAssets.AssetReference;

	[Serializable]
	public class ComponentReference<T> : ComponentReference, IAssetReference<T>
		where T : Component
	{
		[SerializeField]
		[FormerlySerializedAs("_assetReference")]
		private ComponentReferenceT<T> assetReference;

		public override UnityAssetReference AssetReference => assetReference;
		public static implicit operator bool(ComponentReference<T> value) => !value.IsEmptyOrInvalid();

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
	public abstract class ComponentReference : IAssetReference
	{
		[SerializeField]
		private int _releaseDelayMs;

		public abstract UnityAssetReference AssetReference { get; }

		int IAssetReference.ReleaseDelayMs => _releaseDelayMs;

		public virtual Type AssetType => null;
		public UnityObject EditorAsset =>
#if UNITY_EDITOR
			AssetReference.editorAsset;
#else
			null;
#endif

		public static implicit operator bool(ComponentReference value) => !value.IsEmptyOrInvalid();

		public static bool operator ==(ComponentReference a, ComponentReference b) => a.SameAsset(b);
		public static bool operator !=(ComponentReference a, ComponentReference b) => !(a == b);
		public override bool Equals(object obj) => this == obj as ComponentReference;
		public override int GetHashCode() => AssetReference.GetHashCode();
		public override string ToString() => AssetReference.ToString();
	}
}
