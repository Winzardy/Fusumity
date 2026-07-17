using System;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;
using UnityObject = UnityEngine.Object;

namespace AssetManagement
{
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
			ComponentType = componentType;
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

		AssetReference IAssetReference.AssetReference => assetReference;
		int IAssetReference.ReleaseDelayMs => releaseDelayMs;

		public T editorAsset
		{
#if UNITY_EDITOR
			get => assetReference?.editorAsset;
			set
			{
				assetReference ??= new AssetReferenceT<T>(string.Empty);
				this.SetEditorAsset(value);
			}
#else
			get => null;
#endif
		}

		public string AssetGuid { get => assetReference?.AssetGUID; }

		public static implicit operator bool(AssetReference<T> value) => !value.IsEmptyOrInvalid();

		public static bool operator ==(AssetReference<T> a, AssetReference<T> b) => a.SameAsset(b);
		public static bool operator !=(AssetReference<T> a, AssetReference<T> b) => !(a == b);
		public override bool Equals(object obj) => this == obj as AssetReference<T>;
		public override int GetHashCode() => assetReference.GetHashCode();
		public override string ToString() => assetReference.ToString();
	}

	/// <remarks>
	/// AssetReference занят Unity
	/// </remarks>>
	[Serializable]
	public class AnyAssetReference : IAssetReference
	{
		[FormerlySerializedAs("_assetReference")]
		public AssetReference assetReference;

		[FormerlySerializedAs("_releaseDelayMs")]
		public int releaseDelayMs;

		AssetReference IAssetReference.AssetReference => assetReference;
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

		public static implicit operator bool(AnyAssetReference value) => !value.IsEmptyOrInvalid();

		public static bool operator ==(AnyAssetReference a, AnyAssetReference b) => a.SameAsset(b);
		public static bool operator !=(AnyAssetReference a, AnyAssetReference b) => !(a == b);
		public override bool Equals(object obj) => this == obj as AnyAssetReference;
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
		AssetReference AssetReference { get; }
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
}
