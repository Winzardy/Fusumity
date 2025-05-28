using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Content.Management;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Sapientia.Collections;
using Sapientia.Extensions;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

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

			public Formatting formatting = Formatting.None;

			[Space]
			[FolderPath]
			public string[] onlyFolders;
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

					if (list.Count > 0)
						jsonObject.Add(name, list);
				}

				EditorUtility.DisplayProgressBar(displayProgressTitle, "Json File", 0.9f);

				var settings = new JsonSerializerSettings(ContentNewtonsoftJsonImporter.Settings)
				{
					ContractResolver = new ContentContractResolver(args.onlyFolders),
					Formatting = args.formatting
				};
				var text = jsonObject.ToJson(settings);
				var path = args.path;

				var fullPath = Path.Combine(Application.dataPath.Remove(ASSETS_FOLDER_NAME), path);
				var directory = Path.GetDirectoryName(fullPath);
				if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
					Directory.CreateDirectory(directory);

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
				ContentDebug.Log($"{prefix} content json file by path: {fullPath}", textAsset);
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

	public class ContentContractResolver : DefaultContractResolver
	{
		private static string[] _onlyFolders;
		private static Dictionary<Type, HashSet<string>> _typeToAllowedFieldNames;

		public ContentContractResolver(string[] onlyFolders)
		{
			CreateMap(_onlyFolders != onlyFolders);
			_onlyFolders = onlyFolders;
		}

		protected override JsonContract CreateContract(Type objectType)
		{
			if (typeof(IContentReference).IsAssignableFrom(objectType) ||
			    typeof(IContentEntry).IsAssignableFrom(objectType))
				return base.CreateContract(objectType);

			if (objectType == typeof(SerializableGuid))
				return base.CreateContract(objectType);

			if (_typeToAllowedFieldNames != null && !_typeToAllowedFieldNames.ContainsKey(objectType))
			{
				var contract = base.CreateObjectContract(objectType);
				contract.Properties.Clear();
				return contract;
			}

			return base.CreateContract(objectType);
		}
		protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
		{
			var props = new List<JsonProperty>();

			if (_typeToAllowedFieldNames != null)
			{
				if (!_typeToAllowedFieldNames.ContainsKey(type))
					return props;
			}

			var fields = type
			   .GetFields(BindingFlags.Instance | BindingFlags.Public)
			   .Where(f => !typeof(Object).IsAssignableFrom(f.FieldType))
			   .Where(f => !f.Name.Contains("k__BackingField"))
			   .Where(f => !f.IsDefined(typeof(NonSerializedAttribute), true));

			if (_typeToAllowedFieldNames != null)
			{
				fields = fields
				   .Where(f => _typeToAllowedFieldNames[type].Contains(f.Name));
			}

			foreach (var field in fields)
			{
				var prop = base.CreateProperty(field, memberSerialization);
				prop.Readable = true;
				prop.Writable = true;
				props.Add(prop);
			}

			return props;
		}

		private void CreateMap(bool force = false)
		{
			// При Reloading Domain всеравно сброситься
			if (_typeToAllowedFieldNames != null && !force)
				return;

			_typeToAllowedFieldNames = new();

			if (_onlyFolders == null)
				return;

			var paths = _onlyFolders
			   .SelectMany(unityPath => Directory.GetFiles(GetUnityFolderPath(unityPath), "*.cs", SearchOption.AllDirectories))
			   .ToList();

			foreach (var folderPath in paths)
			{
				var code = File.ReadAllText(folderPath);
				var tree = CSharpSyntaxTree.ParseText(code);
				var root = tree.GetCompilationUnitRoot();

				var classes = root.DescendantNodes()
				   .OfType<ClassDeclarationSyntax>();
				foreach (var syntax in classes)
				{
					var fullName = GetFullTypeName(syntax);

					if (string.IsNullOrEmpty(fullName))
						continue;

					var type = AppDomain.CurrentDomain
					   .GetAssemblies()
					   .SelectMany(SafeGetTypes)
					   .FirstOrDefault(t => t.FullName == fullName);

					if (type == null)
						continue;

					if (!_typeToAllowedFieldNames.TryGetValue(type, out var fieldSet))
						fieldSet = _typeToAllowedFieldNames[type] = new HashSet<string>();

					foreach (var fieldDecl in syntax.Members.OfType<FieldDeclarationSyntax>())
					{
						foreach (var v in fieldDecl.Declaration.Variables)
						{
							fieldSet.Add(v.Identifier.Text);
						}
					}
				}
			}
		}

		private static string GetUnityFolderPath(string unityPath)
		{
			return Path.Combine(Application.dataPath.Remove("/Assets"), unityPath);
		}

		private static string GetFullTypeName(ClassDeclarationSyntax syntax)
		{
			var names = new Stack<string>();
			var current = syntax.Parent;

			while (current is TypeDeclarationSyntax parentType)
			{
				names.Push(parentType.Identifier.Text);
				current = current.Parent;
			}

			var ns = string.Empty;
			if (current is NamespaceDeclarationSyntax nsDecl)
				ns = nsDecl.Name + ".";

			names.Push(syntax.Identifier.Text);
			return ns + string.Join("+", names);
		}

		private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
		{
			try
			{
				return assembly.GetTypes();
			}
			catch (ReflectionTypeLoadException e)
			{
				return e.Types.Where(t => t != null);
			}
		}
	}
}
