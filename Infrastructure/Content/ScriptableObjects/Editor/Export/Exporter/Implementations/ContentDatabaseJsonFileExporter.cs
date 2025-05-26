using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Sapientia.Collections;
using Sapientia.Extensions;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Content.ScriptableObjects.Editor
{
	public class ContentDatabaseJsonFileExporter : BaseContentDatabaseExporter<ContentDatabaseJsonFileExporter.Args>
	{
		public class Args : IContentDatabaseExporterArgs
		{
			public List<ContentDatabaseScriptableObject> Databases { get; set; }

			public JsonFullPath path = new()
			{
				path = "Assets/Content/",
				name = "Content"
			};

			public Formatting formatting = Formatting.None;
		}

		protected override void OnExport(ref Args args)
		{
			try
			{
				var jsonObject = new Dictionary<string, List<IContentEntry>>();
				var dbs = args.Databases;
				var displayProgressTitle = "Export Content";

				foreach (var (database, index) in dbs.WithIndex())
				{
					var name = database.ToLabel();
					EditorUtility.DisplayProgressBar(displayProgressTitle, name, index / (float) dbs.Count);
					var list = new List<IContentEntry>();
					database.Fill(list, true);
					jsonObject.Add(name, list);
				}

				EditorUtility.DisplayProgressBar(displayProgressTitle, "Json File", 0.9f);
				var text = jsonObject.ToJson(new JsonSerializerSettings
				{
					ContractResolver = new CustomContractResolver(),
					Formatting = args.formatting,
					TypeNameHandling = TypeNameHandling.Auto
				});

				string unityAssetPath = args.path;
				var path = Path.Combine(Application.dataPath, unityAssetPath.Remove("Assets/"));
				var directory = Path.GetDirectoryName(path);
				if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
					Directory.CreateDirectory(directory);

				var exists = File.Exists(path);
				var writing = true;
				if (exists)
				{
					var prevText = File.ReadAllText(path);
					writing = prevText != text;
				}

				if (writing)
					File.WriteAllText(path, text);

				AssetDatabase.ImportAsset(unityAssetPath);
				var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(unityAssetPath);
				var prefix = exists ? writing ? "Updated" : "Not changed" : "Created";
				ContentDebug.Log($"{prefix} content json file by path: {path}", textAsset);
			}
			finally
			{
				EditorUtility.ClearProgressBar();
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

	public class CustomContractResolver : DefaultContractResolver
	{
		protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
		{
			var fields = type
			   .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			   .Where(f => !typeof(UnityEngine.Object).IsAssignableFrom(f.FieldType))
			   .Where(f => !f.Name.Contains("k__BackingField"))
			   .Where(f => !f.IsDefined(typeof(NonSerializedAttribute), true))
			   .ToList();

			var props = new List<JsonProperty>();
			foreach (var field in fields)
			{
				var prop = base.CreateProperty(field, memberSerialization);
				prop.Readable = true;
				prop.Writable = true;
				props.Add(prop);
			}

			return props;
		}
	}
}
