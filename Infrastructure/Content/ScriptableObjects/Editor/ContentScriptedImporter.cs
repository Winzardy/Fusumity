using System.IO;
using Fusumity.Attributes.Specific;
using Sapientia.Extensions;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Content.ScriptableObjects.Editor
{
	public abstract class ContentScriptedImporter<TScriptableObject, TValue> : ScriptedImporter
		where TScriptableObject : ContentEntryScriptableObject<TValue>
	{
		public bool enabled;

		public bool useCustomId;
		[ShowIf(nameof(useCustomId))]
		public string customId;

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

			asset.enabled = enabled;

			asset.SetValue(value, false);

			ctx.AddObjectToAsset(nameof(TScriptableObject), asset);
			ctx.SetMainObject(asset);
		}

		protected abstract bool TryCreateValue(AssetImportContext ctx, out TValue value);
		protected virtual string GetId(AssetImportContext ctx)
		{
			if (useCustomId)
				return customId;

			return Path.GetFileNameWithoutExtension(ctx.assetPath);
		}
	}
}
