using System;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;

namespace AssetManagement
{
	using UnityObject = UnityEngine.Object;
	using UnityAssetReference =  UnityEngine.AddressableAssets.AssetReference;

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
	public class AssetReference<T> : IAssetReference<T>
		where T : UnityObject
	{
		[FormerlySerializedAs("_assetReference")]
		public AssetReferenceT<T> assetReference;

		[FormerlySerializedAs("_releaseDelayMs")]
		public int releaseDelayMs;

		UnityAssetReference IAssetReference.AssetReference => assetReference;
		int IAssetReference.ReleaseDelayMs => releaseDelayMs;

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

		public static implicit operator bool(AssetReference<T> value) => !value.IsEmptyOrInvalid();

		public static bool operator ==(AssetReference<T> a, AssetReference<T> b) => a.SameAsset(b);
		public static bool operator !=(AssetReference<T> a, AssetReference<T> b) => !(a == b);
		public override bool Equals(object obj) => this == obj as AssetReference<T>;
		public override int GetHashCode() => assetReference.GetHashCode();
		public override string ToString() => assetReference.ToString();
	}

	[Serializable]
	public class AssetReference : IAssetReference
	{
		[FormerlySerializedAs("_assetReference")]
		public UnityAssetReference assetReference;

		[FormerlySerializedAs("_releaseDelayMs")]
		public int releaseDelayMs;

		UnityAssetReference IAssetReference.AssetReference => assetReference;
		int IAssetReference.ReleaseDelayMs => releaseDelayMs;

		public UnityObject editorAsset
		{
#if UNITY_EDITOR
			get => assetReference?.editorAsset;
			set => this.SetEditorAsset(value);
#else
			get => null;
#endif
		}

		public static implicit operator bool(AssetReference value) => !value.IsEmptyOrInvalid();

		public static bool operator ==(AssetReference a, AssetReference b) => a.SameAsset(b);
		public static bool operator !=(AssetReference a, AssetReference b) => !(a == b);
		public override bool Equals(object obj) => this == obj as AssetReference;
		public override int GetHashCode() => assetReference.GetHashCode();
		public override string ToString() => assetReference.ToString();
	}

	public interface IAssetReference
	{
#if UNITY_EDITOR
		/// <summary>
		/// <see cref="AssetReference{T}.editorAsset"/>
		/// </summary>
		const string CUSTOM_EDITOR_NAME = "editorAsset";
#endif
		UnityAssetReference AssetReference { get; }
		string AssetGuid { get => AssetReference.AssetGUID; }

		int ReleaseDelayMs => 0;

		UnityObject EditorAsset =>
#if UNITY_EDITOR
			AssetReference?.editorAsset;
#else
			null;
#endif
	}

	public interface IAssetReference<T> : IAssetReference where T : UnityObject
	{
	}

	public static class AssetReferenceExtensions
	{
		public static bool SameAsset(this IAssetReference a, IAssetReference b)
		{
			if (ReferenceEquals(a, b))
				return true;

			if (a is null || b is null)
				return false;

			var aKey = (string) a.AssetReference.RuntimeKey;
			var bKey = (string) b.AssetReference.RuntimeKey;

			return string.Equals(aKey, bKey, StringComparison.OrdinalIgnoreCase);
		}
	}
}
