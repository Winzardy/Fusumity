// using System;
// using System.Collections.Generic;
// using System.IO;
// using System.Linq;
// using System.Reflection;
// using Fusumity.Editor.Utility;
// using Fusumity.Utility;
// using Microsoft.CodeAnalysis;
// using Microsoft.CodeAnalysis.CSharp;
// using Microsoft.CodeAnalysis.CSharp.Syntax;
// using Sapientia.Extensions;
// using Sirenix.OdinInspector;
// using UnityEditor;
// using UnityEditor.Build;
// using UnityEngine;
//
// namespace Content.ScriptableObjects.Editor
// {
// 	/// <summary>
// 	/// Очень много мучался с этим скриптом, жалко удалить... Оставлю как память,
// 	/// в итоге пришел к тому чтобы вешать аттрибут на поля чтобы определять что серилизовать что нет
// 	/// (<see cref="ClientOnlyAttribute"/>)
// 	/// </summary>
// 	public partial class ContentDatabaseExport
// 	{
// 		private const string SCRIPT_EXTENSION = ".cs";
// 		private const string ASSET_FOLDER_NAME = "Assets";
//
// 		public partial class ProjectSettings
// 		{
// 			[NonSerialized]
// 			[FolderPath, PropertyOrder(10)]
// 			public string[] onlyFolders;
//
// 			[NonSerialized]
// 			[FolderPath, PropertyOrder(10), ShowIf(nameof(UseOnlyFolders))]
// 			public string[] ignoreFolders;
//
// 			[NonSerialized]
// 			[PropertyOrder(10), ShowIf(nameof(UseOnlyFolders))]
// 			public string[] ignoreDefines = {"CLIENT"};
//
// 			public bool UseOnlyFolders => onlyFolders is {Length: > 0};
// 		}
//
// 		private static Dictionary<Type, HashSet<string>> _typeToAllowedFieldNames;
//
// 		public static Dictionary<Type, HashSet<string>> TypeToAllowedFieldNames => _typeToAllowedFieldNames;
//
// 		internal static bool IsAllowedEntry(IContentEntry entry)
// 		{
// 			if (_typeToAllowedFieldNames == null)
// 				return true;
//
// 			return _typeToAllowedFieldNames.ContainsKey(entry.ValueType);
// 		}
//
// 		private void TryCreateAllowedMap(List<ContentDatabaseScriptableObject> databases)
// 		{
// 			return;
//
// 			EditorUtility.DisplayProgressBar(DISPLAY_PROGRESS_TITLE, "Creating Allowed Map", 0);
//
// 			// При Reloading Domain всеравно сброситься
// 			if (_typeToAllowedFieldNames != null)
// 				return;
//
// 			if (projectSettings.onlyFolders == null)
// 				return;
//
// 			_typeToAllowedFieldNames = new();
//
// 			var types = new HashSet<Type>();
// 			foreach (var database in databases)
// 			{
// 				foreach (var scriptableObject in database.scriptableObjects)
// 				{
// 					if (scriptableObject is IContentEntryScriptableObject t)
// 						types.Add(t.ValueType);
// 				}
// 			}
//
// 			var scripts = new HashSet<MonoScript>();
// 			foreach (var t in types)
// 			{
// 				var script = t.FindMonoScript();
// 				scripts.Add(script);
// 			}
//
// 			var scriptPaths = new HashSet<string>(scripts.Select(AssetDatabase.GetAssetPath));
//
// 			var paths = projectSettings.onlyFolders
// 			   .SelectMany(FilePathsByUnityFolder)
// 			   .Where(Filter)
// 			   .ToList();
//
// 			types.Clear();
// 			foreach (var path in paths)
// 			{
// 				foreach (var mType in MonoScriptUtility.GetTypes(path))
// 					types.Add(mType);
// 			}
//
// 			var allAllowedTypes = types
// 			   .Select(t => t.Assembly)
// 			   .Distinct()
// 			   .SelectMany(SafeGetTypes)
// 			   .GroupBy(t => t.FullName)
// 			   .ToDictionary(g => g.Key, g => g.First());
//
// 			types.Clear();
// 			foreach (var path in paths)
// 			{
// 				foreach (var mType in MonoScriptUtility.GetTypes(path))
// 					CollectTypes(mType, types, allAllowedTypes.Values);
// 			}
//
// 			scripts.Clear();
// 			foreach (var t in types)
// 			{
// 				var script = t.FindMonoScript();
// 				scripts.Add(script);
// 			}
//
// 			scriptPaths.Clear();
// 			scriptPaths = new HashSet<string>(scripts.Select(AssetDatabase.GetAssetPath));
//
// 			paths = projectSettings.onlyFolders
// 			   .SelectMany(FilePathsByUnityFolder)
// 			   .Where(Filter)
// 			   .Union(paths)
// 			   .ToList();
//
// 			foreach (var path in paths)
// 			{
// 				var monoScript = MonoScriptUtility.GetMonoScript(path);
// 				if (!monoScript)
// 				{
// 					ContentDebug.LogError("Failed to load script: " + path);
// 					continue;
// 				}
//
// 				var code = monoScript.text;
//
// 				var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
// 				var defines = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget)
// 				   .Split(';')
// 				   .Select(s => s.Trim())
// 				   .Where(s => !projectSettings.ignoreDefines.Contains(s))
// 				   .Where(s => !string.IsNullOrEmpty(s));
// 				var options = CSharpParseOptions.Default.WithPreprocessorSymbols(defines);
//
// 				var tree = CSharpSyntaxTree.ParseText(code, options: options);
// 				var root = tree.GetCompilationUnitRoot();
//
// 				var classes = root.DescendantNodes()
// 				   .Where(node =>
// 						node is ClassDeclarationSyntax
// 							or StructDeclarationSyntax
// 							or InterfaceDeclarationSyntax
// 							or RecordDeclarationSyntax
// 					)
// 				   .Cast<TypeDeclarationSyntax>();
// 				foreach (var syntax in classes)
// 				{
// 					var fullName = GetFullTypeName(syntax);
//
// 					if (fullName.IsNullOrEmpty())
// 						continue;
//
// 					if (!allAllowedTypes.TryGetValue(fullName, out var type))
// 					{
// 						ContentDebug.LogWarning("Not found type by name [ " + fullName + " ]");
// 						continue;
// 					}
//
// 					if (!_typeToAllowedFieldNames.TryGetValue(type, out var fieldSet))
// 						fieldSet = _typeToAllowedFieldNames[type] = new HashSet<string>();
//
// 					foreach (var fieldDecl in syntax.Members.OfType<FieldDeclarationSyntax>())
// 					{
// 						foreach (var v in fieldDecl.Declaration.Variables)
// 						{
// 							fieldSet.Add(v.Identifier.Text);
// 						}
// 					}
// 				}
// 			}
//
// 			var s = types.GetCompositeString(numerate: false, getter: x => x.FullName);
// 			s += "\n\n---\n\n";
// 			s += _typeToAllowedFieldNames.GetCompositeString(numerate: false,
// 				getter: x => $"[{x.Key.Name}] {x.Value.GetCompositeString()}\n---\n");
// 			Clipboard.Copy(s);
//
// 			MonoScriptUtility.ClearCache();
//
// 			bool Filter(string path)
// 			{
// 				if (Path.GetExtension(path) != SCRIPT_EXTENSION)
// 					return false;
//
// 				for (var i = 0; i < projectSettings.ignoreFolders.Length; i++)
// 				{
// 					if (path.StartsWith(projectSettings.ignoreFolders[i]))
// 						return false;
// 				}
//
// 				return scriptPaths.Contains(path);
// 			}
//
// 			IEnumerable<string> FilePathsByUnityFolder(string unityPath)
// 			{
// 				return Directory.GetFiles(GetUnityFolderPath(unityPath),
// 						"*" + SCRIPT_EXTENSION,
// 						SearchOption.AllDirectories)
// 				   .Select(IOPathToUnityPath);
// 			}
// 		}
//
// 		private static void CollectTypes(Type type, HashSet<Type> visited, IEnumerable<Type> allTypes)
// 		{
// 			const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
//
// 			if (type == null)
// 				return;
//
// 			if (!visited.Add(type))
// 				return;
//
// 			if (type.IsArray)
// 			{
// 				CollectTypes(type.GetElementType(), visited, allTypes);
// 				return;
// 			}
//
// 			if (type.IsGenericType)
// 			{
// 				foreach (var arg in type.GetGenericArguments())
// 					CollectTypes(arg, visited, allTypes);
// 			}
//
// 			foreach (var field in type.GetFields(flags))
// 			{
// 				if (field.IsDefined(typeof(NonSerializedAttribute), true))
// 					continue;
//
// 				CollectTypes(field.FieldType, visited, allTypes);
// 			}
//
// 			if (type.BaseType != null && type.BaseType != typeof(object))
// 			{
// 				CollectTypes(type.BaseType, visited, allTypes);
// 			}
//
// 			foreach (var t in allTypes)
// 			{
// 				if (type.IsAssignableFrom(t))
// 					CollectTypes(t, visited, allTypes);
// 			}
// 		}
//
// 		private static string IOPathToUnityPath(string ioPath)
// 		{
// 			return ASSET_FOLDER_NAME + ioPath.Remove(Application.dataPath);
// 		}
//
// 		private static string GetUnityFolderPath(string unityPath)
// 		{
// 			return Path.Combine(Application.dataPath.Remove("/" + ASSET_FOLDER_NAME), unityPath);
// 		}
//
// 		private static string GetFullTypeName(TypeDeclarationSyntax syntax)
// 		{
// 			var names = new Stack<string>();
// 			SyntaxNode? current = syntax;
//
// 			while (current is TypeDeclarationSyntax parentType)
// 			{
// 				names.Push(parentType.Identifier.Text);
// 				current = current.Parent;
// 			}
//
// 			string? ns = null;
//
// 			if (current is BaseNamespaceDeclarationSyntax nsDecl)
// 				ns = nsDecl.Name.ToString();
//
// 			var fullName = (ns != null ? ns + "." : "") + string.Join("+", names);
// 			return fullName.Replace("global::", "");
// 		}
//
// 		private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
// 		{
// 			try
// 			{
// 				return assembly.GetTypes();
// 			}
// 			catch (ReflectionTypeLoadException e)
// 			{
// 				return e.Types.Where(t => t != null);
// 			}
// 		}
// 	}
// }
