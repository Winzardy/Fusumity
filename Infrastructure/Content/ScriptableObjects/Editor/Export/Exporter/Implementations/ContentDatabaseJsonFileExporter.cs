using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Content.Management;
using Fusumity.Utility;
using JetBrains.Annotations;
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
				path = "Builds/",
				name = "Content"
			};

			public Formatting formatting = Formatting.None;

			[PropertySpace(2, 10)]
			public bool revealInFinder = true;

			[ToggleGroup(nameof(useDeserializeTesting), "Deserialize Testing")]
			public bool useDeserializeTesting;

			[ToggleGroup(nameof(useDeserializeTesting), "Deserialize Testing")]
			[ShowInInspector, LabelText("Result")]
			[SerializeReference, Indent(-1)]
			[Searchable]
			public List<IContentEntry> deserializeResult;

			public string BuildOutputPath { get; set; }
		}

		protected override void OnExport(ref Args args)
		{
			var contentJsonObject = new ContentJsonFormat();
			var dbs = args.Databases;

			var typeFiltering = ContentDatabaseExport.Settings.typeFiltering;
			foreach (var (database, index) in dbs.WithIndex())
			{
				var moduleName = database.ToLabel();
				using (ListPool<IContentEntry>.Get(out var list))
				{
					EditorUtility.DisplayProgressBar(ContentDatabaseExport.DISPLAY_PROGRESS_TITLE, moduleName, index / (float) dbs.Count);

					database.Fill(list, true, Filter);

					using (ListPool<IContentEntry>.Get(out var nested))
					{
						database.Fill(nested); // TODO: вернуть Filter, убрал на время пока
						for (int j = 0; j < nested.Count; j++)
							if (!nested[j].Nested.IsNullOrEmpty())
								foreach (var (_, member) in nested[j].Nested)
								{
									var uniqueContentEntry = member.Resolve(nested[j]);
									if (Filter(uniqueContentEntry))
										list.Add(uniqueContentEntry);
								}
					}

					if (list.Count > 0)
						contentJsonObject.Add(moduleName, list);
				}
			}

			EditorUtility.DisplayProgressBar(ContentDatabaseExport.DISPLAY_PROGRESS_TITLE, "Json File", 0.9f);

			var settings = new JsonSerializerSettings(ContentJsonImporter.serializerSettings)
			{
				ContractResolver = new ContentNewtonsoftContractResolver(typeFiltering),
				Formatting = args.formatting
			};

			var serializer = JsonSerializer.Create(settings);
			var root = JObject.FromObject(contentJsonObject, serializer);

			using (StringBuilderPool.Get(out var sb))
			{
				using (var sw = new StringWriter(sb))
				using (var writer = new JsonTextWriter(sw))
				{
					writer.Formatting = args.formatting;
					serializer.Serialize(writer, root);
				}

				var text = sb.ToString();

				string path;
				string fullPath;
				string folderPath;
				if (args.BuildOutputPath.IsNullOrEmpty())
				{
					path = args.path;
					fullPath = Path.Combine(Application.dataPath.Remove(ASSETS_FOLDER_NAME), path);
					folderPath = Path.GetDirectoryName(fullPath);
				}
				else
				{
					folderPath = Path.GetDirectoryName(args.BuildOutputPath);
					var jsonFileName = args.path.name + JsonFullPath.EXTENSION;
					fullPath = Path.Combine(folderPath!, jsonFileName);
					path = null;
				}

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

				if (args.revealInFinder)
					EditorUtility.RevealInFinder(fullPath);

				TextAsset textAsset = null;
				if (fullPath.Contains(ASSETS_FOLDER_NAME) && path != null)
				{
					AssetDatabase.ImportAsset(path);
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

			bool Filter(IContentEntry e) => ContentNewtonsoftContractResolver.IsAllowedType(e.ValueType, typeFiltering);
		}
	}

	[InlineProperty]
	[Serializable]
	public struct JsonFullPath
	{
		public const string EXTENSION = ".json";

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
