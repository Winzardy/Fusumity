using System.IO;
using Sapientia.Extensions;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Content.ScriptableObjects.Editor
{
	public abstract class ContentScriptedImporter<TScriptableObject, TValue> : ScriptedImporter
		where TScriptableObject : ContentEntryScriptableObject<TValue>
	{
		public sealed override void OnImportAsset(AssetImportContext ctx)
		{
			var id = GetId(ctx);
			if (id.IsNullOrEmpty())
			{
				ctx.LogImportError($"Failed to import [ {ctx.assetPath} ]: id is null or empty");
				return;
			}

			if (!TryCreateValue(ctx, out var value))
			{
				ctx.LogImportError($"Failed to import [ {ctx.assetPath} ]: unable to create content value");
				return;
			}

			var path = ctx.assetPath;
			var guid = AssetDatabase.AssetPathToGUID(path);

			var asset = ScriptableObject.CreateInstance<TScriptableObject>();
			var serializableGuid = SerializableGuid.Parse(guid);
			if (!asset.ForceCreateEntry(serializableGuid, id))
				asset.SetId(id);

			asset.SetValue(value, false);

			ctx.AddObjectToAsset(typeof(TScriptableObject).Name, asset);
			ctx.SetMainObject(asset);
		}

		protected abstract bool TryCreateValue(AssetImportContext ctx, out TValue value);
		protected virtual string GetId(AssetImportContext ctx) => Path.GetFileNameWithoutExtension(ctx.assetPath);
	}
}
