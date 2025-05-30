using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Content.Management;
using Fusumity.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sapientia.Collections;
using Sapientia.Extensions;
using Sapientia.Pooling;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Content.ScriptableObjects.Editor
{
	public partial class ContentDatabaseJsonFileExporter : BaseContentDatabaseExporter<ContentDatabaseJsonFileExporter.Args>
	{
		private string ASSETS_FOLDER_NAME = "Assets";
		private static readonly string SINGLE_KEY = IContentEntry.DEFAULT_SINGLE_ID.ToLower();

		public partial class Args : IContentDatabaseExporterArgs
		{
			public List<ContentDatabaseScriptableObject> Databases { get; set; }
			public Type ExporterType => typeof(ContentDatabaseJsonFileExporter);

			public JsonFullPath path = new()
			{
				path = "Builds/", //TODO: Перенести Builder в Fusumity и брать константу Builder.FOLDER
				name = "Content"
			};

			[PropertySpace(2)]
			public Formatting formatting = Formatting.None;
		}

		protected override void OnExport(ref Args args)
		{
			var jsonObject = new Dictionary<string, Dictionary<string, Dictionary<string, object>>>();
			var dbs = args.Databases;

			var typeFiltering = ContentDatabaseExport.Settings.typeFiltering;
			foreach (var (database, index) in dbs.WithIndex())
			{
				var name = database.ToLabel();
				EditorUtility.DisplayProgressBar(ContentDatabaseExport.DISPLAY_PROGRESS_TITLE, name, index / (float) dbs.Count);
				var list = new List<IContentEntry>();
				database.Fill(list, true, Filter);

				using (ListPool<IContentEntry>.Get(out var nested))
				{
					database.Fill(nested, false, Filter);
					for (int j = 0; j < nested.Count; j++)
						if (nested[j].Nested != null)
							foreach (var member in nested[j].Nested)
							{
								var uniqueContentEntry = member.Value.Resolve(nested[j]);
								if (Filter(uniqueContentEntry))
									list.Add(uniqueContentEntry);
							}
				}

				var grouped = list
				   .Where(Filter)
				   .GroupBy(e => e.ValueType.FullName)
				   .ToDictionary(
						g => g.Key, // key: тип
						g => g.ToDictionary(
							e => e is IUniqueContentEntry unique
								? unique.Guid.ToString()
								: SINGLE_KEY,
							e => e.RawValue
						)
					);

				if (list.Count > 0)
					jsonObject.Add(name, grouped);
			}

			EditorUtility.DisplayProgressBar(ContentDatabaseExport.DISPLAY_PROGRESS_TITLE, "Json File", 0.9f);

			var settings = new JsonSerializerSettings(ContentNewtonsoftJsonImporter.Settings)
			{
				ContractResolver = new ContentNewtonsoftContractResolver(typeFiltering),
				Formatting = args.formatting
			};

			var root = new JObject();
			var serializer = JsonSerializer.Create(settings);

			foreach (var (moduleName, typeMap) in jsonObject)
			{
				var moduleObject = new JObject();

				foreach (var (typeName, entries) in typeMap)
				{
					var typeObject = new JObject();

					foreach (var (key, rawValue) in entries)
					{
						if (rawValue != null)
							typeObject[key] = JToken.FromObject(rawValue, serializer);
						else
							typeObject[key] = null;
					}

					moduleObject[typeName] = typeObject;
				}

				root[moduleName] = moduleObject;
			}

			using (StringBuilderPool.Get(out var sb))
			{
				using (var sw = new StringWriter(sb))
				using (var writer = new JsonTextWriter(sw))
				{
					writer.Formatting = args.formatting;
					serializer.Serialize(writer, root);
				}

				var text = sb.ToString();
				var path = args.path;

				var fullPath = Path.Combine(Application.dataPath.Remove(ASSETS_FOLDER_NAME), path);
				var folderPath = Path.GetDirectoryName(fullPath);

				if (!Directory.Exists(folderPath))
					Directory.CreateDirectory(folderPath!);

				var exists = File.Exists(fullPath);
				var writing = true;
				if (exists)
				{
					var prevText = File.ReadAllText(fullPath);
					writing = prevText != text;
				}

				if (writing)
					File.WriteAllText(fullPath, text);

				TextAsset textAsset = null;
				if (fullPath.Contains(ASSETS_FOLDER_NAME))
				{
					AssetDatabase.ImportAsset(fullPath);
					textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
				}

				var prefix = exists ? writing ? "Updated" : "Not changed" : "Created";
				ContentDebug.Log($"{prefix} content json file by path: {fullPath.UnderlineText()}", textAsset);
			}

			bool Filter(IContentEntry e)
			{
				return ContentNewtonsoftContractResolver.IsAllowedType(e.ValueType, typeFiltering);
			}
		}
	}

	[InlineProperty]
	[Serializable]
	public struct JsonFullPath
	{
		private const string EXTENSION = ".json";

		[HorizontalGroup]
		[HideLabel, FolderPath]
		public string path;

		[HorizontalGroup(width: 0.35f)]
		[HideLabel, SuffixLabel(EXTENSION)]
		public string name;

		public static implicit operator string(in JsonFullPath path) => path.ToString();
		public override string ToString() => Path.Combine(path, name + EXTENSION);
	}
}
