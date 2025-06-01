using System;
using System.Collections.Generic;
using System.IO;
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
	public class ContentDatabaseJsonFileExporter : BaseContentDatabaseExporter<ContentDatabaseJsonFileExporter.Args>
	{
		private string ASSETS_FOLDER_NAME = "Assets";

		public class Args : IContentDatabaseExporterArgs
		{
			public List<ContentDatabaseScriptableObject> Databases { get; set; }
			public Type ExporterType => typeof(ContentDatabaseJsonFileExporter);

			public JsonFullPath path = new()
			{
				path = "Builds/", //TODO: Перенести Builder в Fusumity и брать константу Builder.FOLDER
				name = "Content"
			};

			[PropertySpace(2, 10)]
			public Formatting formatting = Formatting.None;

			[ToggleGroup(nameof(useDeserializeTesting), "Deserialize Testing")]
			public bool useDeserializeTesting;

			[ToggleGroup(nameof(useDeserializeTesting), "Deserialize Testing")]
			[ShowInInspector, LabelText("Result")]
			[SerializeReference, ReadOnly]
			public List<IContentEntry> deserializeResult;
		}

		protected override void OnExport(ref Args args)
		{
			var json = new ContentJsonFormat();
			var dbs = args.Databases;

			var typeFiltering = ContentDatabaseExport.Settings.typeFiltering;
			foreach (var (database, index) in dbs.WithIndex())
			{
				var moduleName = database.ToLabel();
				var list = new List<IContentEntry>();

				EditorUtility.DisplayProgressBar(ContentDatabaseExport.DISPLAY_PROGRESS_TITLE, moduleName, index / (float) dbs.Count);

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

				if (list.Count > 0)
					json.Add(moduleName, list);
			}

			EditorUtility.DisplayProgressBar(ContentDatabaseExport.DISPLAY_PROGRESS_TITLE, "Json File", 0.9f);

			var settings = new JsonSerializerSettings(ContentJsonImporter.serializerSettings)
			{
				ContractResolver = new ContentNewtonsoftContractResolver(typeFiltering),
				Formatting = args.formatting
			};

			var root = new JObject();
			var serializer = JsonSerializer.Create(settings);

			foreach (var (moduleName, typeMap) in json)
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

				if (args.useDeserializeTesting)
				{
					var testJson = text.FromJson<ContentJsonFormat>(settings);
					var list = new List<IContentEntry>();
					testJson.Fill(list);
					args.deserializeResult = list.Count > 0 ? list : null;
				}
				else
				{
					args.deserializeResult = null;
				}
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
