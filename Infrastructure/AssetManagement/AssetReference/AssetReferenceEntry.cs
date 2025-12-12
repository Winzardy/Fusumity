using System;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;

namespace AssetManagement
{
	using UnityObject = UnityEngine.Object;

	[Serializable]
	public class AssetReferenceEntry<T> : IAssetReferenceEntry<T>
		where T : UnityObject
	{
		[FormerlySerializedAs("_assetReference")]
		public AssetReferenceT<T> assetReference;

		[FormerlySerializedAs("_releaseDelayMs")]
		public int releaseDelayMs;

		AssetReference IAssetReferenceEntry.AssetReference => assetReference;
		int IAssetReferenceEntry.ReleaseDelayMs => releaseDelayMs;

		public T editorAsset
		{
#if UNITY_EDITOR
			get => assetReference?.editorAsset;
			set { this.SetEditorAsset(value); }
#else
			get => null;
#endif
		}

		public static implicit operator bool(AssetReferenceEntry<T> entry) => !entry.IsEmptyOrInvalid();

		public static bool operator ==(AssetReferenceEntry<T> a, AssetReferenceEntry<T> b) => a.SameAsset(b);
		public static bool operator !=(AssetReferenceEntry<T> a, AssetReferenceEntry<T> b) => !(a == b);
		public override bool Equals(object obj) => this == obj as AssetReferenceEntry<T>;
		public override int GetHashCode() => assetReference.GetHashCode();
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

		public UnityObject editorAsset
		{
#if UNITY_EDITOR
			get => assetReference?.editorAsset;
			set => this.SetEditorAsset(value);
#else
			get => null;
#endif
		}

		public static implicit operator bool(AssetReferenceEntry entry) => !entry.IsEmptyOrInvalid();

		public static bool operator ==(AssetReferenceEntry a, AssetReferenceEntry b) => a.SameAsset(b);
		public static bool operator !=(AssetReferenceEntry a, AssetReferenceEntry b) => !(a == b);
		public override bool Equals(object obj) => this == obj as AssetReferenceEntry;
		public override int GetHashCode() => assetReference.GetHashCode();
	}

	public interface IAssetReferenceEntry
	{
#if UNITY_EDITOR
		/// <summary>
		/// <see cref="AssetReferenceEntry{T}.editorAsset"/>
		/// </summary>
		public const string CUSTOM_EDITOR_NAME = "editorAsset";
#endif
		public AssetReference AssetReference { get; }

		public int ReleaseDelayMs => 0;

		public UnityObject EditorAsset =>
#if UNITY_EDITOR
			AssetReference?.editorAsset;
#else
			null;
#endif
	}

	public interface IAssetReferenceEntry<T> : IAssetReferenceEntry where T : UnityObject
	{
	}

	public static class AssetReferenceExtensions
	{
		public static bool SameAsset(this IAssetReferenceEntry a, IAssetReferenceEntry b)
		{
			if (ReferenceEquals(a, b))
				return true;

			if (a is null || b is null)
				return false;

			return SameAsset(a.AssetReference, b.AssetReference);
		}

		public static bool SameAsset(this AssetReference a, AssetReference b)
		{
			var aKey = (string) a.RuntimeKey;
			var bKey = (string) b.RuntimeKey;

			return string.Equals(aKey, bKey, StringComparison.OrdinalIgnoreCase);
		}
	}
}
