using System;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;

namespace AssetManagement
{
	using UnityObject = UnityEngine.Object;

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
	public sealed class AssetReferenceRequiredComponentAttribute : Attribute
	{
		public Type ComponentType { get; }
		public string ComponentTypeName { get; }
		public bool IncludeChildren { get; }

		public AssetReferenceRequiredComponentAttribute(string componentTypeName, bool includeChildren = true)
		{
			ComponentTypeName = componentTypeName;
			IncludeChildren = includeChildren;
		}

		public AssetReferenceRequiredComponentAttribute(Type componentType, bool includeChildren = true)
		{
			ComponentType   = componentType;
			IncludeChildren = includeChildren;
		}
	}

	[Serializable]
	public class AssetRef<T> : IAssetRef<T>
		where T : UnityObject
	{
		[FormerlySerializedAs("_assetReference")]
		public AssetReferenceT<T> assetReference;

		[FormerlySerializedAs("_releaseDelayMs")]
		public int releaseDelayMs;

		AssetReference IAssetRef.AssetReference => assetReference;
		int IAssetRef.ReleaseDelayMs => releaseDelayMs;

		public T editorAsset
		{
#if UNITY_EDITOR
			get => assetReference?.editorAsset;
			set { this.SetEditorAsset(value); }
#else
			get => null;
#endif
		}

		public string AssetGuid { get => assetReference.AssetGUID; }

		public static implicit operator bool(AssetRef<T> entry) => !entry.IsEmptyOrInvalid();

		public static bool operator ==(AssetRef<T> a, AssetRef<T> b) => a.SameAsset(b);
		public static bool operator !=(AssetRef<T> a, AssetRef<T> b) => !(a == b);
		public override bool Equals(object obj) => this == obj as AssetRef<T>;
		public override int GetHashCode() => assetReference.GetHashCode();
		public override string ToString() => assetReference.ToString();
	}

	[Serializable]
	public class AssetRef : IAssetRef
	{
		[FormerlySerializedAs("_assetReference")]
		public AssetReference assetReference;

		[FormerlySerializedAs("_releaseDelayMs")]
		public int releaseDelayMs;

		AssetReference IAssetRef.AssetReference => assetReference;
		int IAssetRef.ReleaseDelayMs => releaseDelayMs;

		public UnityObject editorAsset
		{
#if UNITY_EDITOR
			get => assetReference?.editorAsset;
			set => this.SetEditorAsset(value);
#else
			get => null;
#endif
		}

		public static implicit operator bool(AssetRef entry) => !entry.IsEmptyOrInvalid();

		public static bool operator ==(AssetRef a, AssetRef b) => a.SameAsset(b);
		public static bool operator !=(AssetRef a, AssetRef b) => !(a == b);
		public override bool Equals(object obj) => this == obj as AssetRef;
		public override int GetHashCode() => assetReference.GetHashCode();
		public override string ToString() => assetReference.ToString();
	}

	public interface IAssetRef
	{
#if UNITY_EDITOR
		/// <summary>
		/// <see cref="AssetRef{T}.editorAsset"/>
		/// </summary>
		public const string CUSTOM_EDITOR_NAME = "editorAsset";
#endif
		public AssetReference AssetReference { get; }
		public string AssetGuid { get => AssetReference.AssetGUID; }

		public int ReleaseDelayMs => 0;

		public UnityObject EditorAsset =>
#if UNITY_EDITOR
			AssetReference?.editorAsset;
#else
			null;
#endif
	}

	public interface IAssetRef<T> : IAssetRef where T : UnityObject
	{
	}

	public static class AssetReferenceExtensions
	{
		public static bool SameAsset(this IAssetRef a, IAssetRef b)
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
