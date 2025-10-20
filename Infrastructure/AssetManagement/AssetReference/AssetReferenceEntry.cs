using System;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace AssetManagement
{
	[Serializable]
	public class AssetReferenceEntry<T> : IAssetReferenceEntry, IAssetReferenceEntry<T>
		where T : Object
	{
		[FormerlySerializedAs("_assetReference")]
		public AssetReferenceT<T> assetReference;

		[FormerlySerializedAs("_releaseDelayMs")]
		public int releaseDelayMs;

		AssetReference IAssetReferenceEntry.AssetReference => assetReference;
		int IAssetReferenceEntry.ReleaseDelayMs => releaseDelayMs;

		public static implicit operator bool(AssetReferenceEntry<T> entry) => !entry.IsEmptyOrInvalid();

		public T editorAsset
		{
#if UNITY_EDITOR
			get => assetReference?.editorAsset;
			set { this.SetEditorAsset(value); }
#else
			get => null;
#endif
		}
	}

	[Serializable]
	public class AssetReferenceEntry : IAssetReferenceEntry
	{
		[FormerlySerializedAs("_assetReference")]
		public AssetReference assetReference;

		[FormerlySerializedAs("_releaseDelayMs")]
		public int releaseDelayMs;

		AssetReference IAssetReferenceEntry.AssetReference => assetReference;
		int IAssetReferenceEntry.ReleaseDelayMs => releaseDelayMs;

		public static implicit operator bool(AssetReferenceEntry entry) => !entry.IsEmptyOrInvalid();

		public Object editorAsset
		{
#if UNITY_EDITOR
			get => assetReference?.editorAsset;
			set { this.SetEditorAsset(value); }
#else
			get => null;
#endif
		}
	}

	public interface IAssetReferenceEntry
	{
#if UNITY_EDITOR
		/// <summary>
		/// <see cref="AssetReferenceEntry{T}.AssetReferenceEditor"/>
		/// </summary>
		public const string CUSTOM_EDITOR_NAME = "editorAsset";
#endif
		public AssetReference AssetReference { get; }

		public int ReleaseDelayMs => 0;

		public Object EditorAsset =>
#if UNITY_EDITOR
			AssetReference?.editorAsset;
#else
			null;
#endif
	}

	public interface IAssetReferenceEntry<T> : IAssetReferenceEntry where T : Object
	{
	}
}
