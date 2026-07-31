#if UNITY_EDITOR
using AssetManagement.AddressableAssets.Editor;
using Sapientia.Extensions;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;
using UnityObject = UnityEngine.Object;
using UnityComponent = UnityEngine.Component;
using UnityAssetReference = UnityEngine.AddressableAssets.AssetReference;

namespace AssetManagement
{
	public static class AssetReferenceEditorExtensions
	{
		public static void SetEditorAsset<T>(this IAssetReference reference, T value, string address = null, string group = null,
			string label = null, bool createGroupIfNonExistent = true) where T : UnityObject
		{
			if (reference == null)
				return;

			UnityObject asset = value;

			if (value is UnityComponent component)
				asset = component.gameObject;

			if (asset && !asset.IsAddressable())
				asset.MakeAddressable(group, address, label, createGroupIfNonExistent);

			if (reference.AssetReference == null)
				throw new System.InvalidOperationException(
					$"{reference.GetType().Name}.AssetReference is null while assigning " +
					$"editor asset of type [ {typeof(T).Name} ].");

			reference.AssetReference.SetEditorAsset(asset);
			reference.AssetReference.SetEditorSubObject(asset);
		}

		public static AssetReference<T> CreateAssetReferenceEntry<T>(this T asset, string address = null, string group = null,
			string label = null) where T : UnityObject
		{
			var entry = new AssetReference<T>
			{
				assetReference = new UnityAssetReference(null)
			};

			entry.SetEditorAsset(asset, address, group, label);
			return entry;
		}

		public static T GetEditorAsset<T>(this AssetReference<T> reference)
			where T : UnityObject
		{
			if (reference == null)
				return null;

			if (typeof(T).IsSubclassOf(typeof(UnityComponent)))
			{
				if (reference.assetReference.editorAsset is GameObject gameObject)
					if (gameObject.TryGetComponent(out T component))
						return component;

				return null;
			}

			if (typeof(T) == typeof(Sprite))
				return GetSprite<T>(reference.assetReference);

			if (reference.assetReference.editorAsset is T asset)
				return asset;

			return null;
		}

		private static T GetSprite<T>(UnityAssetReference reference)
			where T : UnityObject
		{
			if (reference == null || reference.AssetGUID.IsNullOrEmpty())
				return null;

			var type = typeof(T);
			var assetPath = AssetDatabase.GUIDToAssetPath(reference.AssetGUID);
			if (assetPath.IsNullOrEmpty())
				return null;

			if (!reference.SubObjectName.IsNullOrEmpty())
			{
				var subAsset = GetSpriteSubAsset<T>(reference, assetPath);
				if (subAsset != null)
					return subAsset;

				AssetManagementDebug.LogWarning($"Assigned editorAsset sub asset '{reference.SubObjectName}' was not found at '{assetPath}'. EditorAsset will be null");
				return null;
			}

			var asset = AssetDatabase.LoadAssetAtPath(assetPath, type);

			if (asset == null)
				AssetManagementDebug.LogWarning($"Assigned editorAsset does not match type {type}. EditorAsset will be null");

			return (T) asset;
		}

		private static T GetSpriteSubAsset<T>(UnityAssetReference reference, string assetPath)
			where T : UnityObject
		{
			foreach (var subAsset in AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath))
				if (subAsset is T typedSubAsset && subAsset.name == reference.SubObjectName)
					return typedSubAsset;

			var asset = AssetDatabase.LoadAssetAtPath(assetPath, typeof(T));
			if (asset is T typedAsset && asset.name == reference.SubObjectName)
				return typedAsset;

			if (AssetDatabase.GetMainAssetTypeAtPath(assetPath) != typeof(SpriteAtlas))
				return null;

			var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(assetPath);
			var sprite = atlas ? atlas.GetSprite(reference.SubObjectName) : null;
			return sprite as T;
		}
	}
}

#endif
